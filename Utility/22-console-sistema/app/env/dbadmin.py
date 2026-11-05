"""Gestione del DB di un ambiente: istanza, connessione, panoramica, sessioni, query, snapshot, log.

Ogni lettura sul DB passa da `D.db_access`: apre la 5432 all'IP di questo PC per la durata della
lettura (se era gia' aperta la lascia com'e'). Le query libere girano in una transazione READ ONLY con
un tetto di tempo: da qui non si scrive mai nel DB, salvo terminare una sessione (azione esplicita).

Per query, panoramica e sessioni si usa l'utente in sola lettura (`database.utente_lettura`, password
nel segreto `<db_secret>-lettura`) quando esiste: il DB stesso rifiuta le scritture, non solo il
controllo della utility. Senza, si ripiega sull'utente principale con la transazione READ ONLY.

Gli snapshot manuali della utility si chiamano `<istanza>-u22-<data>`: si cancellano solo quelli,
mai gli automatici o quelli fatti da altri.
"""
from __future__ import annotations

import json
import logging
import secrets
import string
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

from botocore.exceptions import ClientError
from psycopg import sql

from app.env import database as D
from app.env.awsutil import Aws, OperationError, Progress, check_cancel, no_progress, stack_info
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)

SNAPSHOT_TAG = "-u22-"
QUERY_TIMEOUT_S = 60
MAX_ROWS = 5000


# ---------------------------------------------------------------- istanza e connessione

def instance_id(aws: Aws) -> str:
    return stack_info(aws).need("DbInstanceId")[0]


def instance_info(aws: Aws) -> list[tuple[str, str]]:
    """Caratteristiche dell'istanza RDS, come coppie (voce, valore) da mostrare."""
    ident = instance_id(aws)
    try:
        db = aws.client("rds").describe_db_instances(DBInstanceIdentifier=ident)["DBInstances"][0]
    except ClientError as exc:
        if exc.response["Error"]["Code"] == "DBInstanceNotFound":
            return [("Istanza", f"{ident}: non esiste")]
        raise
    ep = db.get("Endpoint") or {}
    fmt = lambda d: d.strftime("%Y-%m-%d %H:%M UTC") if d else "-"  # noqa: E731
    return [
        ("Istanza", ident), ("Stato", db["DBInstanceStatus"]),
        ("Motore", f"{db['Engine']} {db['EngineVersion']}"), ("Classe", db["DBInstanceClass"]),
        ("Storage", f"{db['AllocatedStorage']} GB {db.get('StorageType', '')}"
                    + (f", autoscaling fino a {db['MaxAllocatedStorage']} GB" if db.get("MaxAllocatedStorage") else "")),
        ("Endpoint", f"{ep.get('Address', '-')}:{ep.get('Port', '-')}"),
        ("Accessibile da internet", "si'" if db.get("PubliclyAccessible") else "no (solo dentro la VPC)"),
        ("Multi-AZ", "si'" if db.get("MultiAZ") else "no"),
        ("Cifratura", "si'" if db.get("StorageEncrypted") else "no"),
        ("Protezione dalla cancellazione", "si'" if db.get("DeletionProtection") else "no"),
        ("Backup automatici", f"{db.get('BackupRetentionPeriod', 0)} giorni, finestra {db.get('PreferredBackupWindow', '-')}"),
        ("Ultimo istante ripristinabile", fmt(db.get("LatestRestorableTime"))),
        ("Manutenzione", db.get("PreferredMaintenanceWindow", "-")),
        ("Parameter group", ", ".join(p["DBParameterGroupName"] for p in db.get("DBParameterGroups", []))),
        ("Certificato CA", db.get("CACertificateIdentifier", "-")),
        ("Creata il", fmt(db.get("InstanceCreateTime"))),
    ]


def connection_strings(c: D.Conn) -> list[tuple[str, str]]:
    """La stessa connessione nei formati che servono in questo progetto."""
    return [
        ("URI (psql, DBeaver, pgAdmin)", f"postgresql://{c.user}:{c.password}@{c.host}:{c.port}/{c.dbname}?sslmode=require"),
        ("Npgsql (.NET, ConnectionStrings__SummerBooking)",
         f"Host={c.host};Port={c.port};Database={c.dbname};Username={c.user};Password={c.password};SSL Mode=Require"),
        ("JDBC", f"jdbc:postgresql://{c.host}:{c.port}/{c.dbname}?sslmode=require&user={c.user}&password={c.password}"),
        ("Comando psql", f'psql "host={c.host} port={c.port} dbname={c.dbname} user={c.user} sslmode=require"'),
        ("Variabili d'ambiente (PowerShell)",
         f"$env:PGHOST='{c.host}'; $env:PGPORT='{c.port}'; $env:PGDATABASE='{c.dbname}'; $env:PGUSER='{c.user}'; "
         f"$env:PGPASSWORD='{c.password}'; $env:PGSSLMODE='require'"),
    ]


# ---------------------------------------------------------------- letture sul DB

def _rows(cur) -> tuple[list[str], list[tuple]]:
    return [d.name for d in cur.description], cur.fetchall()


def overview(aws: Aws) -> dict:
    """Versione, dimensioni per schema, tabelle piu' grandi, ultima migration per schema, connessioni."""
    s = aws.settings
    rds = reader_conn(aws)
    with D.db_access(aws), rds.connect() as c, c.cursor() as cur:
        cur.execute("select version(), pg_size_pretty(pg_database_size(current_database())), "
                    "(select count(*) from pg_stat_activity where datname = current_database())")
        version, size, conns = cur.fetchone()
        cur.execute("""
            select n.nspname as schema, count(*) filter (where c.relkind in ('r','p') and not c.relispartition) as tabelle,
                   pg_size_pretty(sum(pg_total_relation_size(c.oid)) filter (where c.relkind in ('r','m'))) as dimensione,
                   coalesce(sum(greatest(c.reltuples,0)) filter (where c.relkind = 'r'), 0)::bigint as righe_stimate
            from pg_class c join pg_namespace n on n.oid = c.relnamespace
            where n.nspname = any(%s) group by 1 order by sum(pg_total_relation_size(c.oid)) filter (where c.relkind in ('r','m')) desc nulls last""",
                    (s.database.schemi,))
        schemas = _rows(cur)
        cur.execute("""
            select n.nspname || '.' || c.relname as tabella, pg_size_pretty(pg_total_relation_size(c.oid)) as totale,
                   pg_size_pretty(pg_relation_size(c.oid)) as dati, greatest(c.reltuples,0)::bigint as righe_stimate
            from pg_class c join pg_namespace n on n.oid = c.relnamespace
            where c.relkind = 'r' and n.nspname = any(%s)
            order by pg_total_relation_size(c.oid) desc limit 25""", (s.database.schemi,))
        tables = _rows(cur)
        migrations = []
        for schema in s.database.schemi:
            cur.execute("select to_regclass(%s)", (f"{schema}.{D.HISTORY_TABLE}",))
            if cur.fetchone()[0] is None:
                migrations.append((schema, "-", 0))
                continue
            cur.execute(sql.SQL("select max(migration_id), count(*) from {}.{}").format(
                sql.Identifier(schema), sql.Identifier(D.HISTORY_TABLE)))
            last, n = cur.fetchone()
            migrations.append((schema, last or "-", n))
    return {"versione": version, "dimensione": size, "connessioni": conns, "schemi": schemas, "tabelle": tables,
            "migration": (["schema", "ultima migration", "applicate"], migrations)}


def sessions(aws: Aws) -> tuple[list[str], list[tuple]]:
    """Sessioni sul DB (esclusa questa), con chi le blocca."""
    rds = reader_conn(aws)
    with D.db_access(aws), rds.connect() as c, c.cursor() as cur:
        cur.execute("""
            select pid, usename as utente, application_name as applicazione, client_addr::text as indirizzo,
                   state as stato, wait_event_type || coalesce(':' || wait_event, '') as attesa,
                   to_char(now() - coalesce(query_start, backend_start), 'HH24:MI:SS') as durata,
                   array_to_string(pg_blocking_pids(pid), ',') as bloccato_da,
                   left(regexp_replace(coalesce(query, ''), '\\s+', ' ', 'g'), 300) as query
            from pg_stat_activity
            where datname = current_database() and pid <> pg_backend_pid() and backend_type = 'client backend'
            order by query_start nulls last""")
        return _rows(cur)


def stop_session(aws: Aws, pid: int, terminate: bool) -> bool:
    """Annulla la query in corso (`terminate=False`) o chiude la sessione."""
    rds = D.rds_conn(aws)
    fn = "pg_terminate_backend" if terminate else "pg_cancel_backend"
    with D.db_access(aws), rds.connect(autocommit=True) as c, c.cursor() as cur:
        cur.execute(f"select {fn}(%s)", (int(pid),))
        ok = bool(cur.fetchone()[0])
    log.warning("%s(%d) sul DB dell'ambiente %s: %s", fn, pid, aws.settings.name, "fatto" if ok else "nessun effetto")
    return ok


_ALLOWED_START = ("select", "with", "explain", "show", "table", "values")


def check_read_only(text: str) -> str:
    """Una sola istruzione, che inizi come una lettura.

    Senza parametri psycopg manda il testo cosi' com'e', e `commit; delete ...` uscirebbe dalla
    transazione READ ONLY: per questo niente `;` in mezzo (anche dentro una stringa: si rifiuta per
    prudenza). Un `with` che modifica dati o un `explain analyze` di una scrittura li ferma comunque
    la transazione READ ONLY.
    """
    body = text.strip().rstrip(";").strip()
    if not body:
        raise OperationError("Query vuota")
    if ";" in body:
        raise OperationError("Una sola istruzione per volta (niente ';' in mezzo)")
    first = body.split(None, 1)[0].lower().lstrip("(")
    if first not in _ALLOWED_START:
        raise OperationError(f"Solo letture: la query deve iniziare con {', '.join(_ALLOWED_START)}")
    return body


def run_query(aws: Aws, text: str, limit: int = MAX_ROWS) -> tuple[list[str], list[tuple], bool]:
    """Query in sola lettura (transazione READ ONLY, tetto di tempo). (colonne, righe, troncato)."""
    text = check_read_only(text)
    rds = reader_conn(aws)
    with D.db_access(aws), rds.connect() as c:
        with c.cursor() as cur:
            cur.execute("set transaction read only")
            cur.execute(f"set local statement_timeout = '{QUERY_TIMEOUT_S}s'")
            cur.execute(text)
            if cur.description is None:
                c.rollback()
                return ["esito"], [(cur.statusmessage,)], False
            cols = [d.name for d in cur.description]
            rows = cur.fetchmany(limit + 1)
        c.rollback()
    log.info("Query in sola lettura sull'ambiente %s: %d righe", aws.settings.name, min(len(rows), limit))
    return cols, rows[:limit], len(rows) > limit


# Query pronte, scritte sullo schema di questo progetto.
PRESETS: dict[str, str] = {
    "Migration applicate (tutti gli schemi)": """
select 'identity' as schema, migration_id, product_version from identity."__ef_migrations_history"
union all select 'booking', migration_id, product_version from booking."__ef_migrations_history"
union all select 'catalog', migration_id, product_version from catalog."__ef_migrations_history"
union all select 'privacy', migration_id, product_version from privacy."__ef_migrations_history"
order by 2 desc""",
    "Righe per tabella (esatte, lento sulle grandi)": """
select table_schema || '.' || table_name as tabella,
       (xpath('/row/n/text()', query_to_xml(format('select count(*) as n from %I.%I', table_schema, table_name), false, true, '')))[1]::text::bigint as righe
from information_schema.tables
where table_type = 'BASE TABLE' and table_schema in ('identity','catalog','booking','payment','fiskaly','fiscalprint','notification','switchboard','chatbot','privacy','infra','audit')
order by righe desc""",
    "Retention: ultime corse": """
select * from privacy.privacy_retention_runs order by 1 desc limit 200""",
    "Retention: policy attive e in prova": """
select * from privacy.privacy_retention_policies order by 1""",
    "Retention: inventario degli archivi": """
select source_schema, source_table, object_kind, count(*) as oggetti, sum(row_count) as righe,
       min(archived_at) as primo, max(archived_at) as ultimo, count(*) filter (where purged_at is not null) as rimossi
from privacy.privacy_archive_manifests group by 1, 2, 3 order by 1, 2""",
    "Retention: cancellazioni di file in coda": """
select * from privacy.privacy_file_deletions where executed_at is null order by 1 limit 500""",
    "Audit: ultimi eventi": """
select * from audit.trail_entries order by 1 desc limit 200""",
    "Indici non usati (dalle statistiche)": """
select schemaname || '.' || relname as tabella, indexrelname as indice, idx_scan as letture,
       pg_size_pretty(pg_relation_size(indexrelid)) as dimensione
from pg_stat_user_indexes where idx_scan = 0 order by pg_relation_size(indexrelid) desc limit 100""",
    "Tabelle da pulire (righe morte, ultimo vacuum)": """
select schemaname || '.' || relname as tabella, n_live_tup as vive, n_dead_tup as morte,
       last_autovacuum, last_autoanalyze
from pg_stat_user_tables order by n_dead_tup desc limit 50""",
    "Blocchi in attesa": """
select a.pid, a.usename, pg_blocking_pids(a.pid) as bloccato_da, a.wait_event_type, a.wait_event,
       now() - a.query_start as da, left(a.query, 200) as query
from pg_stat_activity a where cardinality(pg_blocking_pids(a.pid)) > 0""",
    "Estensioni installate": """
select extname, extversion from pg_extension order by 1""",
}


def export_csv(cols: list[str], rows: list[tuple], dest: Path) -> Path:
    import csv  # noqa: PLC0415

    dest.parent.mkdir(parents=True, exist_ok=True)
    with dest.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f, delimiter=";")
        w.writerow(cols)
        w.writerows(rows)
    return dest


# ---------------------------------------------------------------- utente in sola lettura

def read_only_conn(aws: Aws) -> D.Conn | None:
    """Connessione con l'utente in sola lettura, o None se il suo segreto non esiste."""
    s = aws.settings
    try:
        data = json.loads(aws.client("secretsmanager").get_secret_value(
            SecretId=s.database.secret_lettura)["SecretString"])
    except ClientError as exc:
        if exc.response["Error"]["Code"] in ("ResourceNotFoundException", "AccessDeniedException"):
            return None
        raise
    st = stack_info(aws)
    host = st.outputs.get("DbEndpoint")
    if not host:
        raise OperationError("Il DB su AWS non esiste")
    return D.Conn(host, int(data.get("port", 5432)), data["username"], data["password"],
                  st.outputs.get("DbName", "summer_booking"), ssl=True)


def reader_conn(aws: Aws) -> D.Conn:
    ro = read_only_conn(aws)
    if ro is not None:
        return ro
    log.warning("Utente in sola lettura assente: uso l'utente principale (transazione READ ONLY)")
    return D.rds_conn(aws)


def _password(n: int = 32) -> str:
    alphabet = string.ascii_letters + string.digits
    return "".join(secrets.choice(alphabet) for _ in range(n))


def grant_read_only(cur, s, master_user: str) -> list[str]:
    """Permessi dell'utente in sola lettura sugli schemi esistenti; restituisce gli schemi toccati.

    Le DEFAULT PRIVILEGES valgono per le tabelle che l'utente principale creera' (migration, partizioni
    nuove): non serve rilanciare dopo ogni migration, solo dopo un DB ricreato o uno schema nuovo.
    """
    user = sql.Identifier(s.database.utente_lettura)
    cur.execute("select current_database()")
    cur.execute(sql.SQL("grant connect on database {} to {}").format(sql.Identifier(cur.fetchone()[0]), user))
    cur.execute("select nspname from pg_namespace where nspname = any(%s)", (s.database.schemi,))
    done = []
    for (schema,) in cur.fetchall():
        sch = sql.Identifier(schema)
        cur.execute(sql.SQL("grant usage on schema {} to {}").format(sch, user))
        cur.execute(sql.SQL("grant select on all tables in schema {} to {}").format(sch, user))
        cur.execute(sql.SQL("grant select on all sequences in schema {} to {}").format(sch, user))
        cur.execute(sql.SQL("alter default privileges for role {} in schema {} grant select on tables to {}").format(
            sql.Identifier(master_user), sch, user))
        done.append(schema)
    return done


def setup_read_only(aws: Aws) -> str:
    """Crea (o riallinea) utente e segreto. Idempotente: rilanciato non cambia la password."""
    s = aws.settings
    name = s.database.utente_lettura
    sm = aws.client("secretsmanager")
    master = D.rds_conn(aws)
    try:
        data = json.loads(sm.get_secret_value(SecretId=s.database.secret_lettura)["SecretString"])
        password, created = data["password"], False
    except ClientError as exc:
        if exc.response["Error"]["Code"] != "ResourceNotFoundException":
            raise
        password, created = _password(), True
    with D.db_access(aws):
        with master.connect(autocommit=True) as c, c.cursor() as cur:
            cur.execute("select 1 from pg_roles where rolname = %s", (name,))
            verb = "alter" if cur.fetchone() else "create"
            cur.execute(sql.SQL("{} role {} with login password {} connection limit 5").format(
                sql.SQL(verb), sql.Identifier(name), sql.Literal(password)))
            for setting, value in (("default_transaction_read_only", "on"), ("statement_timeout", f"{QUERY_TIMEOUT_S}s"),
                                   ("idle_in_transaction_session_timeout", "60s")):
                cur.execute(sql.SQL("alter role {} set {} = {}").format(sql.Identifier(name), sql.Identifier(setting),
                                                                       sql.Literal(value)))
            try:
                cur.execute(sql.SQL("grant pg_read_all_stats to {}").format(sql.Identifier(name)))
            except Exception as exc:  # noqa: BLE001 - serve solo a vedere le query delle altre sessioni
                log.warning("pg_read_all_stats non concesso (%s): nelle sessioni il testo delle query altrui "
                            "restera' nascosto", str(exc).splitlines()[0])
            schemas = grant_read_only(cur, s, master.user)
        secret = json.dumps({"engine": "postgres", "username": name, "password": password, "host": master.host,
                             "port": master.port, "dbname": master.dbname})
        if created:
            sm.create_secret(Name=s.database.secret_lettura, SecretString=secret,
                             Description=f"Utente PostgreSQL in sola lettura del DB {s.name} (utility 22)")
        else:
            sm.put_secret_value(SecretId=s.database.secret_lettura, SecretString=secret)
        ro = D.Conn(master.host, master.port, name, password, master.dbname, ssl=True)
        with ro.connect() as c, c.cursor() as cur:
            cur.execute("show default_transaction_read_only")
            ro_flag = cur.fetchone()[0]
    if ro_flag != "on":
        raise OperationError(f"L'utente {name} non risulta in sola lettura (default_transaction_read_only={ro_flag})")
    log.info("Utente %s %s: %d schemi, segreto %s %s", name, "creato" if verb == "create" else "riallineato",
             len(schemas), s.database.secret_lettura, "creato" if created else "aggiornato")
    return (f"Utente {name} {'creato' if verb == 'create' else 'riallineato'} in sola lettura su {len(schemas)} schemi; "
            f"password nel segreto {s.database.secret_lettura}.")


def regrant_if_present(aws: Aws, master: D.Conn) -> None:
    """Dopo un DB ricreato o caricato: rimette i permessi se l'utente in sola lettura esiste."""
    s = aws.settings
    try:
        with master.connect(autocommit=True) as c, c.cursor() as cur:
            cur.execute("select 1 from pg_roles where rolname = %s", (s.database.utente_lettura,))
            if not cur.fetchone():
                return
            n = len(grant_read_only(cur, s, master.user))
        log.info("Permessi dell'utente %s riallineati (%d schemi)", s.database.utente_lettura, n)
    except Exception as exc:  # noqa: BLE001 - non deve far fallire il caricamento appena riuscito
        log.warning("Permessi dell'utente in sola lettura non riallineati: %s (rifarli da Gestione DB)", exc)


def remove_read_only(aws: Aws) -> str:
    s = aws.settings
    name = s.database.utente_lettura
    master = D.rds_conn(aws)
    with D.db_access(aws), master.connect(autocommit=True) as c, c.cursor() as cur:
        cur.execute("select 1 from pg_roles where rolname = %s", (name,))
        if cur.fetchone():
            cur.execute("select pg_terminate_backend(pid) from pg_stat_activity where usename = %s", (name,))
            for schema in s.database.schemi:
                cur.execute("select 1 from pg_namespace where nspname = %s", (schema,))
                if cur.fetchone():
                    cur.execute(sql.SQL("alter default privileges for role {} in schema {} revoke select on tables "
                                        "from {}").format(sql.Identifier(master.user), sql.Identifier(schema),
                                                          sql.Identifier(name)))
            cur.execute(sql.SQL("drop owned by {}").format(sql.Identifier(name)))
            cur.execute(sql.SQL("drop role {}").format(sql.Identifier(name)))
    try:
        aws.client("secretsmanager").delete_secret(SecretId=s.database.secret_lettura, RecoveryWindowInDays=7)
    except ClientError as exc:
        if exc.response["Error"]["Code"] != "ResourceNotFoundException":
            raise
    log.info("Utente %s rimosso; segreto %s in cancellazione (recuperabile per 7 giorni)", name,
             s.database.secret_lettura)
    return f"Utente {name} rimosso; il segreto si cancella fra 7 giorni (fino ad allora si recupera)."


# ---------------------------------------------------------------- snapshot

@dataclass
class Snapshot:
    ident: str
    kind: str
    status: str
    created: str
    size_gb: int
    own: bool


def list_snapshots(aws: Aws) -> list[Snapshot]:
    ident = instance_id(aws)
    rds = aws.client("rds")
    out = []
    for page in rds.get_paginator("describe_db_snapshots").paginate(DBInstanceIdentifier=ident):
        for s in page["DBSnapshots"]:
            t = s.get("SnapshotCreateTime")
            out.append(Snapshot(s["DBSnapshotIdentifier"], s.get("SnapshotType", ""), s["Status"],
                                t.strftime("%Y-%m-%d %H:%M") if t else "", s.get("AllocatedStorage", 0),
                                s["DBSnapshotIdentifier"].startswith(ident + SNAPSHOT_TAG)))
    return sorted(out, key=lambda x: x.created, reverse=True)


def create_snapshot(aws: Aws, cancel=None, progress: Progress = no_progress, wait: bool = True) -> str:
    ident = instance_id(aws)
    name = f"{ident}{SNAPSHOT_TAG}{datetime.now():%Y%m%d-%H%M%S}"
    rds = aws.client("rds")
    rds.create_db_snapshot(DBInstanceIdentifier=ident, DBSnapshotIdentifier=name)
    log.info("Snapshot %s avviato", name)
    if wait:
        from app.env.awsutil import sleep  # noqa: PLC0415

        while True:
            sleep(20, cancel)
            s = rds.describe_db_snapshots(DBSnapshotIdentifier=name)["DBSnapshots"][0]
            progress("snapshot", s.get("PercentProgress", 0), 100, f"{name}: {s['Status']}")
            if s["Status"] == "available":
                break
            if s["Status"] in ("failed", "deleted"):
                raise OperationError(f"Snapshot {name}: {s['Status']}")
        log.info("Snapshot %s pronto", name)
    return name


def delete_snapshot(aws: Aws, name: str) -> None:
    ident = instance_id(aws)
    if not name.startswith(ident + SNAPSHOT_TAG):
        raise OperationError(f"{name} non e' uno snapshot manuale di questa utility: non si cancella da qui")
    aws.client("rds").delete_db_snapshot(DBSnapshotIdentifier=name)
    log.info("Snapshot %s cancellato", name)


# ---------------------------------------------------------------- log di PostgreSQL

def list_log_files(aws: Aws) -> list[tuple[str, int, str]]:
    ident = instance_id(aws)
    files = []
    for page in aws.client("rds").get_paginator("describe_db_log_files").paginate(DBInstanceIdentifier=ident):
        for f in page["DescribeDBLogFiles"]:
            files.append((f["LogFileName"], f["Size"],
                          datetime.fromtimestamp(f["LastWritten"] / 1000).strftime("%Y-%m-%d %H:%M")))
    return sorted(files, key=lambda x: x[2], reverse=True)


def download_log(aws: Aws, name: str, dest_dir: Path, cancel=None) -> Path:
    ident = instance_id(aws)
    rds = aws.client("rds")
    dest = dest_dir / name.replace("/", "_")
    dest_dir.mkdir(parents=True, exist_ok=True)
    marker = "0"
    with dest.open("w", encoding="utf-8") as f:
        while True:
            check_cancel(cancel)
            part = rds.download_db_log_file_portion(DBInstanceIdentifier=ident, LogFileName=name, Marker=marker,
                                                    NumberOfLines=10000)
            f.write(part.get("LogFileData") or "")
            if not part.get("AdditionalDataPending"):
                break
            marker = part["Marker"]
    log.info("Log %s scaricato in %s", name, dest)
    return dest
