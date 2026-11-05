"""Dati archiviati dalla retention: elenco, confronto con l'inventario, anteprima, ricerca, esportazione.

Gli archivi delle policy `archive_cold` sono oggetti JSON Lines compressi nel bucket di archivio,
con chiave `archivio/<schema>/<tabella>/<anno>/<mese>/<istante>-<policy>-<lotto>.jsonl.gz`, e una riga
di inventario ciascuno in `privacy.privacy_archive_manifests`. Questa parte e' di **sola lettura**:
un archivio non si modifica, non si cancella (lo fa la policy decennale) e non si reinserisce nel
database (le righe sono uscite per una decisione di retention; rimetterle sarebbe annullarla).

Il confronto con l'inventario legge il DB e quindi apre l'accesso dal PC per la durata della lettura.
"""
from __future__ import annotations

import csv
import gzip
import io
import json
import logging
import re
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path

from app.env.awsutil import Aws, OperationError, Progress, check_cancel, no_progress, stack_info
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)

_KEY = re.compile(r"^(?P<prefix>.*?)(?P<schema>[a-z0-9_]+)/(?P<table>[a-z0-9_]+)/(?P<y>\d{4})/(?P<m>\d{2})/"
                  r"(?P<ts>\d{8}T\d{6}Z)-(?P<policy>[0-9a-fA-F-]{36})-(?P<part>\d+)\.jsonl\.gz$")


@dataclass
class ArchiveObject:
    key: str
    size: int
    modified: datetime | None
    storage_class: str = ""
    schema: str = "?"
    table: str = "?"
    written: str = ""          # istante nel nome (AAAAMMGGTHHMMSSZ)
    policy: str = ""
    part: int = 0

    @property
    def source(self) -> str:
        return f"{self.schema}.{self.table}"


@dataclass
class TableSummary:
    source: str
    objects: int = 0
    size: int = 0
    first: str = ""
    last: str = ""
    rows: int | None = None        # dall'inventario, se letto
    issues: int = 0


@dataclass
class Reconciliation:
    ok: int = 0
    without_manifest: list[str] = field(default_factory=list)   # oggetto senza riga di inventario
    missing: list[dict] = field(default_factory=list)           # riga attiva senza oggetto: GRAVE
    purged_but_present: list[dict] = field(default_factory=list)
    linked_assets: int = 0
    rows_by_source: dict[str, int] = field(default_factory=dict)
    origin: str = ""
    other_storage: dict[str, int] = field(default_factory=dict)   # righe su storage di altri ambienti

    def text(self) -> str:
        other = (" Su altri storage (non confrontati): "
                 + ", ".join(f"{k} {v}" for k, v in sorted(self.other_storage.items())) + "."
                 if self.other_storage else "")
        return (f"Storage dell'ambiente {self.origin or '?'}: {self.ok} archivi in regola; {len(self.missing)} righe "
                f"di inventario senza oggetto; {len(self.without_manifest)} oggetti senza inventario; "
                f"{len(self.purged_but_present)} segnati come rimossi ma ancora presenti; {self.linked_assets} file "
                f"collegati (non verificati qui).{other}")


def archive_bucket(aws: Aws) -> str:
    return stack_info(aws).need(aws.settings.archivi.bucket_output)[0]


def _parse(key: str, size: int, modified, storage_class: str) -> ArchiveObject:
    o = ArchiveObject(key, size, modified, storage_class)
    m = _KEY.match(key)
    if m:
        o.schema, o.table, o.written = m["schema"], m["table"], m["ts"]
        o.policy, o.part = m["policy"], int(m["part"])
    return o


def list_archives(aws: Aws, cancel=None, progress: Progress = no_progress) -> list[ArchiveObject]:
    bucket = archive_bucket(aws)
    s3 = aws.client("s3")
    out = []
    for page in s3.get_paginator("list_objects_v2").paginate(Bucket=bucket, Prefix=aws.settings.archivi.prefisso):
        check_cancel(cancel)
        for o in page.get("Contents", []):
            if not o["Key"].endswith("/"):
                out.append(_parse(o["Key"], o["Size"], o["LastModified"], o.get("StorageClass", "")))
        progress("elenco", 0, 0, f"{len(out)} archivi")
    log.info("Archivi in s3://%s/%s: %d oggetti, %.1f MB", bucket, aws.settings.archivi.prefisso, len(out),
             sum(o.size for o in out) / 1e6)
    return out


def by_table(objs: list[ArchiveObject], rec: Reconciliation | None = None) -> list[TableSummary]:
    groups: dict[str, TableSummary] = {}
    for o in objs:
        g = groups.setdefault(o.source, TableSummary(o.source))
        g.objects += 1
        g.size += o.size
        if o.written:
            g.first = min(g.first or o.written, o.written)
            g.last = max(g.last, o.written)
    if rec is not None:
        for src, rows in rec.rows_by_source.items():
            groups.setdefault(src, TableSummary(src)).rows = rows
        for m in rec.missing:
            groups.setdefault(f"{m['source_schema']}.{m['source_table']}",
                              TableSummary(f"{m['source_schema']}.{m['source_table']}")).issues += 1
        for key in rec.without_manifest:
            o = _parse(key, 0, None, "")
            if o.source in groups:
                groups[o.source].issues += 1
    return sorted(groups.values(), key=lambda g: g.source)


# ---------------------------------------------------------------- inventario

def read_manifest(aws: Aws) -> list[dict]:
    """Righe di privacy.privacy_archive_manifests (accesso dal PC aperto per la durata della query)."""
    from app.env import database as D  # noqa: PLC0415 - psycopg solo quando serve

    rds = D.rds_conn(aws)
    with D.db_access(aws), rds.connect() as c, c.cursor() as cur:
        cur.execute("select to_regclass('privacy.privacy_archive_manifests')")
        if cur.fetchone()[0] is None:
            raise OperationError("La tabella privacy.privacy_archive_manifests non esiste in questo DB")
        cur.execute("""select source_schema, source_table, storage_origin, storage_path, object_kind::text,
                              byte_size, row_count, cutoff_at, archived_at, purged_at
                       from privacy.privacy_archive_manifests order by archived_at""")
        cols = [d.name for d in cur.description]
        rows = [dict(zip(cols, r)) for r in cur.fetchall()]
    log.info("Inventario: %d righe (%d archivi di righe, %d file collegati)", len(rows),
             sum(1 for r in rows if r["object_kind"] == "row_archive"),
             sum(1 for r in rows if r["object_kind"] != "row_archive"))
    return rows


def archive_origin(aws: Aws) -> str:
    """Etichetta dello storage di archivio dell'ambiente, come la scrive il backend nell'inventario.

    Si legge dalla task definition del backend (`Retention__Archive__Origin`): e' il valore che il motore
    usa davvero. Se manca, quello di configurazione (`archivi.origine`).
    """
    st = stack_info(aws)
    family = st.outputs.get("TaskDefinitionFamily")
    if family:
        try:
            td = aws.client("ecs").describe_task_definition(taskDefinition=family)["taskDefinition"]
            for c in td["containerDefinitions"]:
                for e in c.get("environment", []):
                    if e["name"] == "Retention__Archive__Origin" and e.get("value"):
                        return e["value"]
        except Exception as exc:  # noqa: BLE001 - si ripiega sulla configurazione
            log.warning("Origine dell'archivio non letta dalla task definition %s: %s", family, exc)
    if not aws.settings.archivi.origine:
        raise OperationError("Origine dello storage di archivio sconosciuta: impostare archivi.origine")
    return aws.settings.archivi.origine


def reconcile(objs: list[ArchiveObject], manifest: list[dict], origin: str) -> Reconciliation:
    """Confronta bucket e inventario, solo per le righe dello storage dell'ambiente (`origin`).

    Le righe scritte su un altro storage (es. `s3_archivio_local`, arrivate con un DB copiato dal PC)
    si contano a parte: il loro oggetto non puo' essere in questo bucket, e segnalarle come mancanti
    sarebbe un falso allarme.
    """
    rec = Reconciliation(origin=origin)
    present = {o.key: o for o in objs}
    listed = set()
    for r in manifest:
        if r["storage_origin"] != origin:
            rec.other_storage[r["storage_origin"]] = rec.other_storage.get(r["storage_origin"], 0) + 1
            continue
        if r["object_kind"] != "row_archive":
            rec.linked_assets += 1
            continue
        path = r["storage_path"]
        listed.add(path)
        src = f"{r['source_schema']}.{r['source_table']}"
        if r["purged_at"] is None:
            rec.rows_by_source[src] = rec.rows_by_source.get(src, 0) + int(r["row_count"] or 0)
            if path in present:
                rec.ok += 1
            else:
                rec.missing.append(r)
        elif path in present:
            rec.purged_but_present.append(r)
    rec.without_manifest = sorted(k for k in present if k not in listed)
    for r in rec.missing[:20]:
        log.error("Archivio inventariato ma assente dal bucket: %s (%s righe)", r["storage_path"], r["row_count"])
    for k in rec.without_manifest[:20]:
        log.warning("Oggetto senza riga di inventario: %s", k)
    log.info("Confronto: %s", rec.text())
    return rec


# ---------------------------------------------------------------- lettura

def iter_rows(aws: Aws, key: str):
    """Righe (dict) di un archivio, leggendo l'oggetto in streaming."""
    body = aws.client("s3").get_object(Bucket=archive_bucket(aws), Key=key)["Body"]
    with gzip.GzipFile(fileobj=body) as gz:
        for line in io.TextIOWrapper(gz, encoding="utf-8"):
            line = line.strip()
            if line:
                yield json.loads(line)


def preview(aws: Aws, key: str, limit: int | None = None) -> tuple[list[str], list[dict], bool]:
    """Prime righe di un archivio: (colonne, righe, troncato)."""
    limit = limit or aws.settings.archivi.righe_anteprima
    rows: list[dict] = []
    more = False
    for row in iter_rows(aws, key):
        if len(rows) >= limit:
            more = True
            break
        rows.append(row)
    return _columns(rows), rows, more


def _columns(rows: list[dict]) -> list[str]:
    cols: list[str] = []
    for r in rows:
        for k in r:
            if k not in cols:
                cols.append(k)
    return cols


def _match(row: dict, text: str, column: str | None) -> bool:
    if column:
        return text.lower() in str(row.get(column, "")).lower()
    return any(text.lower() in str(v).lower() for v in row.values() if v is not None)


def search(aws: Aws, keys: list[str], text: str, column: str | None = None, cancel=None,
           progress: Progress = no_progress, max_results: int = 1000) -> list[dict]:
    """Righe che contengono `text` (in una colonna o in qualunque colonna) negli archivi indicati.

    Ogni risultato porta `_archivio` e `_riga` per ritrovarlo.
    """
    if not text:
        raise OperationError("Indicare il testo da cercare")
    found: list[dict] = []
    for i, key in enumerate(keys, 1):
        check_cancel(cancel)
        progress("ricerca", i, len(keys), f"{key.rsplit('/', 1)[-1]} - {len(found)} trovate")
        for n, row in enumerate(iter_rows(aws, key), 1):
            if _match(row, text, column):
                found.append({"_archivio": key, "_riga": n, **row})
                if len(found) >= max_results:
                    log.warning("Ricerca fermata a %d risultati", max_results)
                    return found
    log.info("Ricerca di %r%s in %d archivi: %d righe", text, f" nella colonna {column}" if column else "",
             len(keys), len(found))
    return found


def export_csv(rows: list[dict], dest: Path) -> Path:
    """Righe in CSV (separatore ;, UTF-8 con BOM: si apre bene in Excel)."""
    dest.parent.mkdir(parents=True, exist_ok=True)
    cols = _columns(rows)
    with dest.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=cols, delimiter=";", extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow({k: json.dumps(v, ensure_ascii=False) if isinstance(v, (dict, list)) else v
                        for k, v in r.items()})
    log.info("Esportate %d righe in %s", len(rows), dest)
    return dest


def export_archive(aws: Aws, key: str, folder: Path, cancel=None) -> tuple[Path, Path]:
    """Scarica l'archivio com'e' (.jsonl.gz) e ne scrive accanto la versione CSV."""
    folder.mkdir(parents=True, exist_ok=True)
    raw = folder / key.rsplit("/", 1)[-1]
    aws.client("s3").download_file(archive_bucket(aws), key, str(raw))
    rows = []
    with gzip.open(raw, "rt", encoding="utf-8") as f:
        for line in f:
            check_cancel(cancel)
            if line.strip():
                rows.append(json.loads(line))
    return raw, export_csv(rows, raw.with_name(raw.name.replace(".jsonl.gz", ".csv")))
