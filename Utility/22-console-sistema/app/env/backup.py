"""Backup del DB di un ambiente in locale.

Il dump lo fa il task usa-e-getta `dbloader` dentro la VPC, con lo script sostituito: dal PC una
sessione lunga verso RDS viene interrotta dopo circa 10 minuti, e il task ha gia' pg_dump della stessa
versione del DB e le credenziali del segreto. Il task carica il file nel bucket di build con un URL
firmato (PUT), la utility lo scarica, ne verifica lo SHA-256 calcolato nel task e toglie la copia dal
bucket. Accanto al file si scrive un `.json` con i dati della corsa.

Formati: `custom` (pg_dump -Fc, compresso al livello scelto, si ripristina con pg_restore) oppure `sql`
(testo, `.sql.gz` se la compressione e' maggiore di zero).
"""
from __future__ import annotations

import gzip
import hashlib
import json
import logging
import subprocess
import uuid
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

from app.env import database as D
from app.env.awsutil import Cancelled, OperationError, check_cancel, stack_info
from app.env.operations import OK, WARN, Ctx, maintenance, wait_db_available
from app.env.power import db_status
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)

FORMATS = ("custom", "sql")
_NO_WINDOW = getattr(subprocess, "CREATE_NO_WINDOW", 0)

# Script del container (entrypoint `sh -c`). Scrive righe chiave=valore che la utility legge dal log;
# BACKUP_EXIT e' sempre l'ultima.
_SCRIPT = r'''
echo "$(pg_dump --version)"
apk add --no-cache curl >/dev/null 2>&1 || { echo "BACKUP_EXIT=curl"; exit 0; }
if [ "$FORMAT" = "custom" ]; then
  OUT=/tmp/backup.dump
  pg_dump -Fc -Z "$LEVEL" --no-owner --no-privileges $DUMP_ARGS -f "$OUT" "$PGDATABASE" 2>&1; RC=$?
elif [ "$LEVEL" -gt 0 ]; then
  OUT=/tmp/backup.sql.gz
  { pg_dump -Fp --no-owner --no-privileges $DUMP_ARGS "$PGDATABASE" 2>/tmp/err; echo $? > /tmp/rc; } | gzip -"$LEVEL" > "$OUT"
  cat /tmp/err; RC=$(cat /tmp/rc)
else
  OUT=/tmp/backup.sql
  pg_dump -Fp --no-owner --no-privileges $DUMP_ARGS -f "$OUT" "$PGDATABASE" 2>&1; RC=$?
fi
echo "PG_DUMP_EXIT=$RC"
[ "$RC" = "0" ] || { echo "BACKUP_EXIT=dump"; exit 0; }
echo "BACKUP_SIZE=$(wc -c < "$OUT" | tr -d ' ')"
echo "BACKUP_SHA256=$(sha256sum "$OUT" | cut -d' ' -f1)"
curl -fsS -X PUT -T "$OUT" "$UPLOAD_URL" 2>&1 && echo "BACKUP_EXIT=0" || echo "BACKUP_EXIT=upload"
'''


@dataclass
class BackupOptions:
    formato: str = "custom"
    compressione: int = 6
    schemi: list[str] | None = None        # None = tutto il database
    solo_schema: bool = False
    tieni_su_s3: bool = False

    def extension(self) -> str:
        if self.formato == "custom":
            return ".dump"
        return ".sql.gz" if self.compressione > 0 else ".sql"

    def dump_args(self) -> str:
        args = ["--schema-only"] if self.solo_schema else []
        for s in self.schemi or []:
            args += ["-n", s]
        return " ".join(args)

    def validate(self) -> None:
        if self.formato not in FORMATS:
            raise OperationError(f"Formato {self.formato!r} non valido ({', '.join(FORMATS)})")
        if not 0 <= int(self.compressione) <= 9:
            raise OperationError("Compressione fra 0 (nessuna) e 9 (massima)")
        for s in self.schemi or []:
            if not s.replace("_", "").isalnum():
                raise OperationError(f"Nome di schema non valido: {s!r}")


def _sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def _value(lines: list[str], key: str) -> str | None:
    for line in reversed(lines):
        if line.startswith(key + "="):
            return line.split("=", 1)[1].strip()
    return None


def run_backup(ctx: Ctx, opt: BackupOptions) -> Path:
    """Dump nel task, scarico in locale, verifica. Restituisce il percorso del file."""
    s, aws = ctx.s, ctx.aws
    opt.validate()
    st = stack_info(aws)
    if not st.has_db:
        raise OperationError(f"Il DB dell'ambiente {s.name} non esiste")
    out = dict(st.outputs)
    bucket, dbname, *_ = st.need("BuildBucketName", "DbName", "DbEndpoint", "ClusterName", "DbLoaderTaskFamily",
                                 "DbLoaderLogGroup", "SubnetIds", "AppSecurityGroupId")
    started = datetime.now(timezone.utc)
    stamp = f"{datetime.now():%Y%m%d_%H%M%S}"          # ora locale: e' il nome che legge una persona
    name = f"{s.name}_{dbname}_{stamp}{opt.extension()}"
    key = f"{s.backup.prefisso_s3.rstrip('/')}/{uuid.uuid4().hex}/{name}"
    dest_dir = s.backup_path
    dest_dir.mkdir(parents=True, exist_ok=True)
    dest = dest_dir / name

    with ctx.phase("db", "DB acceso"):
        ident = aws.identity()
        status, _ = db_status(aws, out.get("DbInstanceId", ""))
        ctx.step("db", "DB acceso", OK if status == "available" else WARN, f"stato {status}")

    def dump() -> list[str]:
        if status != "available":
            wait_db_available(ctx)
        signer = D.presigner(aws)
        url = signer.generate_presigned_url("put_object", Params={"Bucket": bucket, "Key": key}, ExpiresIn=6 * 3600)
        env = {"FORMAT": opt.formato, "LEVEL": str(int(opt.compressione)), "DUMP_ARGS": opt.dump_args(),
               "UPLOAD_URL": url, "PGHOST": out["DbEndpoint"], "PGPORT": "5432", "PGDATABASE": dbname}
        lines, task_status, _ = D.run_db_task(aws, out, env, ctx.cancel, ctx.progress, "BACKUP_EXIT=", "dump",
                                           command=_SCRIPT)
        code = _value(lines, "BACKUP_EXIT")
        if code != "0":
            tail = "\n".join(lines[-15:])
            reason = {"curl": "il task non ha potuto installare curl (serve accesso a internet dal task)",
                      "dump": "pg_dump fallito", "upload": "caricamento nel bucket fallito",
                      None: f"il task si e' fermato senza esito ({task_status})"}.get(code, code)
            raise OperationError(f"Backup fallito: {reason}.\n{tail}")
        return lines

    with ctx.phase("dump", "Dump nel task dbloader"):
        if status != "available" and s.has_schedule:
            # DB spento dall'orario: lo si accende in manutenzione per la durata del dump.
            with maintenance(ctx, f"backup {name}"):
                lines = dump()
        else:
            lines = dump()
        size = int(_value(lines, "BACKUP_SIZE") or 0)
        sha = _value(lines, "BACKUP_SHA256") or ""
        ctx.step("dump", "Dump nel task dbloader", OK, f"{size / 1e6:.1f} MB")

    s3 = aws.client("s3")
    try:
        with ctx.phase("download", "Scarico in locale"):
            part = dest.with_name(dest.name + ".partial")
            done = [0]

            def cb(n: int) -> None:
                done[0] += n
                ctx.progress("download", done[0], size, f"{done[0] / 1e6:.1f} / {size / 1e6:.1f} MB")
                check_cancel(ctx.cancel)

            s3.download_file(bucket, key, str(part), Callback=cb)
            got = _sha256(part)
            if sha and got != sha:
                raise OperationError(f"SHA-256 diverso: nel task {sha}, scaricato {got}. File lasciato in {part}")
            part.replace(dest)
            ctx.step("download", "Scarico in locale", OK, f"{dest} (SHA-256 verificato)")
    finally:
        if opt.tieni_su_s3:
            log.info("Copia tenuta in s3://%s/%s", bucket, key)
        else:
            try:
                s3.delete_object(Bucket=bucket, Key=key)
            except Exception as exc:  # noqa: BLE001 - la pulizia non deve nascondere l'esito
                log.warning("Copia nel bucket non cancellata (s3://%s/%s): %s", bucket, key, exc)

    meta = {
        "ambiente": s.name, "stack": st.name, "account": ident["Account"], "eseguito_da": ident["Arn"],
        "database": dbname, "endpoint": out["DbEndpoint"], "file": dest.name, "byte": dest.stat().st_size,
        "sha256": sha or _sha256(dest), "formato": opt.formato, "compressione": opt.compressione,
        "schemi": opt.schemi or "tutti", "solo_schema": opt.solo_schema,
        "pg_dump": next((x for x in lines if x.startswith("pg_dump")), ""),
        "copia_s3": f"s3://{bucket}/{key}" if opt.tieni_su_s3 else None,
        "inizio": started.isoformat(timespec="seconds"),
        "fine": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }
    dest.with_name(dest.name + ".json").write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")
    log.info("Backup completato: %s (%.1f MB)", dest, dest.stat().st_size / 1e6)
    return dest


# ---------------------------------------------------------------- backup locali

@dataclass
class LocalBackup:
    path: Path
    meta: dict

    @property
    def size_mb(self) -> float:
        return self.path.stat().st_size / 1e6 if self.path.exists() else 0.0


def list_local(s) -> list[LocalBackup]:
    """Backup in cartella per l'ambiente, dal piu' recente; i `.partial` non sono backup."""
    folder = s.backup_path
    if not folder.exists():
        return []
    out = []
    for f in folder.iterdir():
        if f.is_file() and not f.name.endswith((".json", ".partial")):
            meta_file = f.with_name(f.name + ".json")
            meta = json.loads(meta_file.read_text(encoding="utf-8")) if meta_file.exists() else {}
            out.append(LocalBackup(f, meta))
    return sorted(out, key=lambda b: b.path.stat().st_mtime, reverse=True)


def verify_local(s, path: Path, cancel=None) -> str:
    """Rilegge il file: SHA-256 contro il .json; per `custom` anche l'indice con pg_restore --list."""
    meta_file = path.with_name(path.name + ".json")
    meta = json.loads(meta_file.read_text(encoding="utf-8")) if meta_file.exists() else {}
    got = _sha256(path)
    if meta.get("sha256") and meta["sha256"] != got:
        raise OperationError(f"SHA-256 diverso da quello registrato: il file e' cambiato o rovinato ({path.name})")
    notes = [f"SHA-256 {'coincide' if meta.get('sha256') else 'calcolato (nessun .json)'}"]
    if path.suffix == ".dump":
        exe = Path(s.database.locale.bin) / "pg_restore"
        res = subprocess.run([str(exe), "--list", str(path)], capture_output=True, text=True, encoding="utf-8",
                             errors="replace", creationflags=_NO_WINDOW)
        if res.returncode != 0:
            raise OperationError(f"pg_restore --list fallito: {res.stderr.strip()[:300]}")
        entries = [x for x in res.stdout.splitlines() if x and not x.startswith(";")]
        data = sum(1 for x in entries if " TABLE DATA " in x)
        notes.append(f"indice leggibile: {len(entries)} voci, {data} tabelle con dati")
    elif path.name.endswith(".gz"):
        n = 0
        with gzip.open(path, "rb") as f:
            for chunk in iter(lambda: f.read(1 << 20), b""):
                n += len(chunk)
                if cancel is not None and cancel.is_set():
                    raise Cancelled()
        notes.append(f"gzip integro, {n / 1e6:.1f} MB di SQL")
    return "; ".join(notes)


def delete_local(path: Path) -> None:
    for f in (path, path.with_name(path.name + ".json")):
        if f.exists():
            f.unlink()
    log.info("Backup locale cancellato: %s", path)

