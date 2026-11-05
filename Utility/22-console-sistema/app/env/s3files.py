"""File S3 dell'ambiente: elenco per cartelle, dimensioni, scarico, backup, caricamento, cancellazione.

I bucket sono quelli dichiarati dagli output dello stack (`s3.bucket_output`), mai un nome scritto a
mano: cosi' la linguetta non puo' finire sul bucket di un altro ambiente o del legacy. I bucket in
`s3.sola_lettura` (l'archivio della retention, e in produzione anche i file) non accettano
caricamenti ne' cancellazioni.

Il backup scarica un bucket o un prefisso in `data/s3/<ambiente>/<bucket>/`, con un indice delle
versioni gia' scaricate (ETag): rilanciato, scarica solo cio' che e' nuovo o cambiato.
"""
from __future__ import annotations

import json
import logging
import mimetypes
import re
import threading
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path, PurePosixPath

from botocore.exceptions import ClientError

from app.env.awsutil import Aws, Cancelled, OperationError, Progress, check_cancel, no_progress, stack_info
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)

INDEX_NAME = ".indice-s3.json"


@dataclass
class Bucket:
    output: str            # nome dell'output dello stack
    name: str
    read_only: bool


@dataclass
class Entry:
    key: str
    is_dir: bool
    size: int = 0
    modified: datetime | None = None
    storage_class: str = ""
    etag: str = ""

    @property
    def name(self) -> str:
        return PurePosixPath(self.key.rstrip("/")).name + ("/" if self.is_dir else "")


def buckets(aws: Aws) -> list[Bucket]:
    s = aws.settings
    out = stack_info(aws).outputs
    result = []
    for name in s.s3.bucket_output:
        if out.get(name):
            result.append(Bucket(name, out[name], name in s.s3.sola_lettura))
        else:
            log.warning("Output %s assente nello stack %s: bucket non mostrato", name, s.stack)
    return result


def find_bucket(aws: Aws, name: str) -> Bucket:
    for b in buckets(aws):
        if b.name == name:
            return b
    raise OperationError(f"Il bucket {name} non e' fra quelli dell'ambiente {aws.settings.name}")


def list_level(aws: Aws, bucket: str, prefix: str) -> tuple[list[Entry], bool]:
    """Un livello di "cartella": sottocartelle e oggetti. Il booleano dice se l'elenco e' troncato."""
    find_bucket(aws, bucket)
    limit = aws.settings.s3.max_elementi_elenco
    s3 = aws.client("s3")
    entries: list[Entry] = []
    truncated = False
    for page in s3.get_paginator("list_objects_v2").paginate(Bucket=bucket, Prefix=prefix, Delimiter="/"):
        entries += [Entry(p["Prefix"], True) for p in page.get("CommonPrefixes", [])]
        entries += [Entry(o["Key"], False, o["Size"], o["LastModified"], o.get("StorageClass", ""),
                          o.get("ETag", "").strip('"'))
                    for o in page.get("Contents", []) if o["Key"] != prefix]
        if len(entries) >= limit:
            truncated = True
            break
    return entries, truncated


def walk(aws: Aws, bucket: str, prefix: str, cancel=None):
    """Tutti gli oggetti sotto un prefisso (ricorsivo)."""
    s3 = aws.client("s3")
    for page in s3.get_paginator("list_objects_v2").paginate(Bucket=bucket, Prefix=prefix):
        check_cancel(cancel)
        for o in page.get("Contents", []):
            yield Entry(o["Key"], False, o["Size"], o["LastModified"], o.get("StorageClass", ""),
                        o.get("ETag", "").strip('"'))


def summary(aws: Aws, bucket: str, prefix: str, cancel=None, progress: Progress = no_progress) -> dict:
    """Numero di oggetti, byte totali e per classe di storage sotto un prefisso."""
    find_bucket(aws, bucket)
    n, total, by_class = 0, 0, {}
    for e in walk(aws, bucket, prefix, cancel):
        n += 1
        total += e.size
        by_class[e.storage_class or "STANDARD"] = by_class.get(e.storage_class or "STANDARD", 0) + e.size
        if n % 1000 == 0:
            progress("conteggio", 0, 0, f"{n} oggetti, {total / 1e6:.1f} MB")
    return {"oggetti": n, "byte": total, "per_classe": by_class}


def expand(aws: Aws, bucket: str, keys: list[str], cancel=None) -> list[Entry]:
    """Chiavi scelte nell'elenco -> oggetti (una cartella vale tutto il suo contenuto)."""
    out: list[Entry] = []
    s3 = aws.client("s3")
    for key in keys:
        if key.endswith("/"):
            out += list(walk(aws, bucket, key, cancel))
        else:
            head = s3.head_object(Bucket=bucket, Key=key)
            out.append(Entry(key, False, head["ContentLength"], head["LastModified"],
                             head.get("StorageClass", ""), head.get("ETag", "").strip('"')))
    return out


_BAD_CHARS = re.compile(r'[<>:"\\|?*\x00-\x1f]')


def _local_path(root: Path, key: str) -> Path:
    """Percorso locale di una chiave, senza uscire dalla cartella di destinazione.

    Controllo solo lessicale: niente `..`, niente radice, e i caratteri che Windows non ammette nei
    nomi (compresi `:` e `\\`, che farebbero saltare a un'unita' o a un'altra cartella) diventano `_`.
    Niente `resolve()`: con piu' thread che creano le stesse cartelle su Windows da' risultati diversi.
    """
    parts = [_BAD_CHARS.sub("_", p) for p in key.split("/") if p not in ("", ".", "..")]
    if not parts:
        raise OperationError(f"Chiave non scaricabile: {key!r}")
    return root.joinpath(*parts)


def download(aws: Aws, bucket: str, keys: list[str], dest: Path, cancel=None,
             progress: Progress = no_progress, workers: int = 8) -> dict:
    """Scarica oggetti o cartelle in `dest`, rispettando la struttura delle chiavi."""
    objs = expand(aws, bucket, keys, cancel)
    return _fetch(aws, bucket, objs, dest, {}, cancel, progress, workers)


def backup(aws: Aws, bucket: str, prefix: str, cancel=None, progress: Progress = no_progress,
           workers: int = 8) -> tuple[Path, dict]:
    """Copia incrementale di un bucket (o di un prefisso) in data/s3/<ambiente>/<bucket>/."""
    find_bucket(aws, bucket)
    root = aws.settings.work_path / "s3" / aws.settings.name / bucket
    root.mkdir(parents=True, exist_ok=True)
    index_file = root / INDEX_NAME
    index = json.loads(index_file.read_text(encoding="utf-8")) if index_file.exists() else {}
    objs = list(walk(aws, bucket, prefix, cancel))
    try:
        res = _fetch(aws, bucket, objs, root, index, cancel, progress, workers)
    finally:
        index_file.write_text(json.dumps(index, indent=0), encoding="utf-8")
    remote = {o.key for o in objs}
    gone = sorted(k for k in index if k.startswith(prefix) and k not in remote)
    res["non_piu_nel_bucket"] = len(gone)
    if gone:
        log.info("%d file in locale non sono piu' nel bucket (non cancellati in locale): %s%s", len(gone),
                 ", ".join(gone[:5]), "…" if len(gone) > 5 else "")
    return root, res


def _fetch(aws: Aws, bucket: str, objs: list[Entry], dest: Path, index: dict, cancel, progress: Progress,
           workers: int) -> dict:
    s3 = aws.client("s3")
    total = sum(o.size for o in objs)
    done = {"byte": 0, "scaricati": 0, "gia_presenti": 0, "errori": 0}
    lock = threading.Lock()

    def one(o: Entry) -> None:
        check_cancel(cancel)
        target = _local_path(dest, o.key)
        if index.get(o.key) == o.etag and target.exists() and target.stat().st_size == o.size:
            with lock:
                done["gia_presenti"] += 1
                done["byte"] += o.size
            return
        target.parent.mkdir(parents=True, exist_ok=True)
        part = target.with_name(target.name + ".partial")
        s3.download_file(bucket, o.key, str(part))
        part.replace(target)
        with lock:
            index[o.key] = o.etag
            done["scaricati"] += 1
            done["byte"] += o.size

    with ThreadPoolExecutor(max_workers=workers) as pool:
        futures = {pool.submit(one, o): o for o in objs}
        for f in as_completed(futures):
            try:
                f.result()
            except Cancelled:
                for other in futures:
                    other.cancel()
                raise
            except Exception as exc:  # noqa: BLE001 - un file in errore non ferma gli altri
                done["errori"] += 1
                log.error("Scarico di %s: %s", futures[f].key, exc)
            progress("scarico", done["byte"], total,
                     f"{done['scaricati'] + done['gia_presenti']}/{len(objs)} file, {done['byte'] / 1e6:.1f} MB")
    log.info("s3://%s: %d scaricati, %d gia' presenti, %d errori, %.1f MB in %s", bucket, done["scaricati"],
             done["gia_presenti"], done["errori"], total / 1e6, dest)
    return done


def _writable(aws: Aws, bucket: str) -> Bucket:
    b = find_bucket(aws, bucket)
    if b.read_only:
        raise OperationError(f"Il bucket {bucket} ({b.output}) e' in sola lettura per questa utility "
                             f"(s3.sola_lettura)")
    return b


def upload(aws: Aws, bucket: str, prefix: str, files: list[Path], cancel=None,
           progress: Progress = no_progress) -> int:
    """Carica file locali nel prefisso indicato; non sovrascrive senza dirlo (lo fa il chiamante)."""
    _writable(aws, bucket)
    s3 = aws.client("s3")
    for i, f in enumerate(files, 1):
        check_cancel(cancel)
        key = prefix + f.name
        ctype = mimetypes.guess_type(f.name)[0] or "application/octet-stream"
        s3.upload_file(str(f), bucket, key, ExtraArgs={"ContentType": ctype, "ServerSideEncryption": "AES256"})
        log.info("Caricato s3://%s/%s (%.1f KB, %s)", bucket, key, f.stat().st_size / 1024, ctype)
        progress("caricamento", i, len(files), key)
    return len(files)


def existing(aws: Aws, bucket: str, keys: list[str]) -> list[str]:
    s3 = aws.client("s3")
    found = []
    for key in keys:
        try:
            s3.head_object(Bucket=bucket, Key=key)
            found.append(key)
        except ClientError as exc:
            if exc.response["Error"]["Code"] not in ("404", "NoSuchKey", "NotFound"):
                raise
    return found


def delete(aws: Aws, objs: list[Entry], bucket: str, cancel=None, progress: Progress = no_progress) -> dict:
    """Cancella gli oggetti (gia' espansi), a lotti da 1000. Un errore non ferma gli altri."""
    _writable(aws, bucket)
    s3 = aws.client("s3")
    done = {"cancellati": 0, "errori": 0}
    keys = [o.key for o in objs]
    for i in range(0, len(keys), 1000):
        check_cancel(cancel)
        batch = keys[i:i + 1000]
        res = s3.delete_objects(Bucket=bucket, Delete={"Objects": [{"Key": k} for k in batch], "Quiet": True})
        errs = res.get("Errors", [])
        for e in errs:
            log.error("Cancellazione di %s: %s %s", e.get("Key"), e.get("Code"), e.get("Message"))
        done["errori"] += len(errs)
        done["cancellati"] += len(batch) - len(errs)
        progress("cancellazione", min(i + 1000, len(keys)), len(keys), f"{done['cancellati']} cancellati")
    log.info("s3://%s: %d oggetti cancellati, %d errori", bucket, done["cancellati"], done["errori"])
    return done


def presigned_url(aws: Aws, bucket: str, key: str, minutes: int = 60) -> str:
    from app.env.database import presigner  # noqa: PLC0415 - evita di caricare psycopg per un link

    find_bucket(aws, bucket)
    return presigner(aws).generate_presigned_url("get_object", Params={"Bucket": bucket, "Key": key},
                                                 ExpiresIn=minutes * 60)
