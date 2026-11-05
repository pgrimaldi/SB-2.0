"""Database di test: DB locali, DB su AWS, caricamento, svuotamento, accesso a tempo dal PC.

Strumenti: pg_dump/pg_restore del PostgreSQL locale (stessa versione di RDS) e psycopg per le
query. Il DB su AWS si raggiunge solo dopo aver aperto la 5432 per l'IP di questa postazione, con
una regola del security group che si toglie sempre a fine operazione.
"""
from __future__ import annotations

import ipaddress
import json
import logging
import os
import re
import subprocess
import tempfile
import threading
import uuid
from contextlib import contextmanager
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Callable

import keyring
from botocore.config import Config
import psycopg
from botocore.exceptions import ClientError
from psycopg import sql

from app.env.awsutil import Aws, OperationError, Progress, check_cancel, my_public_ip, no_progress, sleep, stack_info
from app.logging_setup import LOGGER_NAME
from app.settings import EnvSettings as Settings

log = logging.getLogger(LOGGER_NAME)

KEYRING_SERVICE = "sb-utility22"
# Password salvata dalla utility 20: si riusa, cosi' chi passa alla 22 non la deve ridigitare.
KEYRING_SERVICE_20 = "sb-dev-utility20"
RULE_PREFIX = "utility22 "
FIXED_PREFIX = "utility22-fisso "
# Regole aperte dalla utility 20 sullo stesso security group: stessa natura, stesso trattamento.
RULE_PREFIXES = (RULE_PREFIX, "utility20 ")
FIXED_PREFIXES = (FIXED_PREFIX, "utility20-fisso ")
HISTORY_TABLE = "__ef_migrations_history"
_NO_WINDOW = getattr(subprocess, "CREATE_NO_WINDOW", 0)
# Errori di pg_restore che su RDS sono attesi e innocui: l'utente master non e' superuser.
_BENIGN = ("must be owner of", "already exists")

AskPassword = Callable[[str], str | None]


# ---------------------------------------------------------------- connessioni

@dataclass
class Conn:
    host: str
    port: int
    user: str
    password: str
    dbname: str
    ssl: bool = False

    def kwargs(self, dbname: str | None = None) -> dict:
        kw = {"host": self.host, "port": self.port, "user": self.user, "password": self.password,
              "dbname": dbname or self.dbname, "connect_timeout": 15, "application_name": "utility22"}
        if self.ssl:
            kw["sslmode"] = "require"
        return kw

    def env(self) -> dict:
        env = dict(os.environ)
        # Messaggi degli strumenti in inglese: il filtro degli errori attesi li riconosce, e non ci
        # sono caratteri accentati in una codifica diversa da UTF-8.
        env.update({"LC_MESSAGES": "C", "LC_ALL": "C", "LANG": "C", "LANGUAGE": "en"})
        env["PGPASSWORD"] = self.password
        env["PGSSLMODE"] = "require" if self.ssl else "prefer"
        return env

    def args(self, dbname: str | None = None) -> list[str]:
        return ["-h", self.host, "-p", str(self.port), "-U", self.user, "-d", dbname or self.dbname]

    def connect(self, dbname: str | None = None, autocommit: bool = False):
        return psycopg.connect(**self.kwargs(dbname), autocommit=autocommit)


def local_conn(settings: Settings, dbname: str = "postgres", ask: AskPassword | None = None) -> Conn:
    loc = settings.database.locale
    account = f"{loc.host}:{loc.port}:{loc.utente}"
    password = keyring.get_password(KEYRING_SERVICE, account) or keyring.get_password(KEYRING_SERVICE_20, account)
    conn = Conn(loc.host, loc.port, loc.utente, password or "", dbname)
    if password:
        try:
            conn.connect().close()
            return conn
        except psycopg.OperationalError:
            log.warning("La password salvata del PostgreSQL locale non funziona piu'")
    if ask is None:
        raise OperationError(f"Password del PostgreSQL locale ({account}) non salvata: avviare una volta la "
                             f"GUI, oppure usare --password-locale.")
    password = ask(f"Password dell'utente {loc.utente} del PostgreSQL locale ({loc.host}:{loc.port})")
    if not password:
        raise OperationError("Password del PostgreSQL locale non indicata")
    conn.password = password
    conn.connect().close()
    keyring.set_password(KEYRING_SERVICE, account, password)
    log.info("Password del PostgreSQL locale salvata nel Gestore credenziali di Windows")
    return conn


def rds_conn(aws: Aws) -> Conn:
    st = stack_info(aws)
    host = st.outputs.get("DbEndpoint")
    if not host:
        raise OperationError("Il DB su AWS non esiste (DatabaseEnabled=false)")
    secret = json.loads(aws.client("secretsmanager").get_secret_value(
        SecretId=aws.settings.database.db_secret)["SecretString"])
    return Conn(host, int(secret.get("port", 5432)), secret["username"], secret["password"],
                st.outputs.get("DbName", "summer_booking"), ssl=True)


# ---------------------------------------------------------------- accesso a tempo

def _rule_desc(user: str) -> str:
    return f"{RULE_PREFIX}{user} {datetime.now(timezone.utc):%Y-%m-%dT%H:%MZ}"


def _sg_id(aws: Aws) -> str:
    sg = stack_info(aws).outputs.get("DbSecurityGroupId")
    if not sg:
        raise OperationError("Output DbSecurityGroupId mancante nello stack")
    return sg


@dataclass
class AccessRule:
    cidr: str
    description: str
    kind: str              # temporaneo | permanente | altro | servizio
    opened_at: datetime | None = None

    @property
    def closable(self) -> bool:
        return self.kind != "servizio"


def _perm(cidr: str, description: str | None = None) -> dict:
    net = ipaddress.ip_network(cidr, strict=False)
    rng = {"CidrIpv6" if net.version == 6 else "CidrIp": str(net)}
    if description:
        rng["Description"] = description
    return {"IpProtocol": "tcp", "FromPort": 5432, "ToPort": 5432,
            ("Ipv6Ranges" if net.version == 6 else "IpRanges"): [rng]}


def normalize_cidr(text: str) -> str:
    """'1.2.3.4' -> '1.2.3.4/32'; rifiuta valori non validi."""
    text = text.strip()
    try:
        net = ipaddress.ip_network(text if "/" in text else f"{text}/{128 if ':' in text else 32}", strict=False)
    except ValueError as exc:
        raise OperationError(f"IP o CIDR non valido: {text!r}") from exc
    return str(net)


def is_wide(cidr: str) -> bool:
    """Apertura a un intervallo ampio (meno di /24 in IPv4, di /64 in IPv6): va confermata."""
    net = ipaddress.ip_network(cidr, strict=False)
    return net.prefixlen < (24 if net.version == 4 else 64)


def list_access(aws: Aws) -> list[AccessRule]:
    group = aws.client("ec2").describe_security_groups(GroupIds=[_sg_id(aws)])["SecurityGroups"][0]
    out = []
    for perm in group.get("IpPermissions", []):
        for pair in perm.get("UserIdGroupPairs", []):
            out.append(AccessRule(pair.get("GroupId", ""), "task del backend e caricamenti (fissa)", "servizio"))
        for rng in perm.get("IpRanges", []) + perm.get("Ipv6Ranges", []):
            cidr = rng.get("CidrIp") or rng.get("CidrIpv6")
            desc = rng.get("Description", "")
            m = re.search(r"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})Z$", desc)
            when = datetime.strptime(m.group(1), "%Y-%m-%dT%H:%M").replace(tzinfo=timezone.utc) if m else None
            kind = ("temporaneo" if desc.startswith(RULE_PREFIXES)
                    else "permanente" if desc.startswith(FIXED_PREFIXES) else "altro")
            out.append(AccessRule(cidr, desc, kind, when))
    return out


def open_cidr(aws: Aws, cidr: str, permanent: bool = False, note: str = "") -> bool:
    """Apre la 5432 a un IP/CIDR. Restituisce False se era gia' aperto."""
    cidr = normalize_cidr(cidr)
    user = aws.actor().rsplit("/", 1)[-1]
    clean = re.sub(r"[^A-Za-z0-9 ._:/()#,@\[\]+=&;{}!$*-]", "", note)[:80]
    desc = (f"{FIXED_PREFIX}{clean + ' ' if clean else ''}{user} {datetime.now(timezone.utc):%Y-%m-%dT%H:%MZ}"
            if permanent else _rule_desc(user))
    try:
        aws.client("ec2").authorize_security_group_ingress(GroupId=_sg_id(aws), IpPermissions=[_perm(cidr, desc)])
        log.info("Accesso al DB aperto per %s (%s)", cidr, "permanente" if permanent else "temporaneo")
        return True
    except ClientError as exc:
        if exc.response["Error"]["Code"] != "InvalidPermission.Duplicate":
            raise
        log.info("Accesso al DB gia' aperto per %s", cidr)
        return False


def close_cidr(aws: Aws, cidr: str) -> None:
    try:
        aws.client("ec2").revoke_security_group_ingress(GroupId=_sg_id(aws), IpPermissions=[_perm(cidr)])
        log.info("Accesso al DB chiuso per %s", cidr)
    except ClientError as exc:
        log.warning("Chiusura dell'accesso %s: %s", cidr, exc)


def open_access(aws: Aws) -> tuple[str, bool]:
    """Apre la 5432 al proprio IP (temporaneo); restituisce (cidr, aperta adesso)."""
    cidr = f"{my_public_ip()}/32"
    return cidr, open_cidr(aws, cidr)


@contextmanager
def db_access(aws: Aws):
    """Accesso dal PC per la durata del blocco. Se l'IP era gia' aperto (es. permanente) lo lascia."""
    cidr, added = open_access(aws)
    try:
        yield
    finally:
        if added:
            close_cidr(aws, cidr)


def cleanup_old_rules(aws: Aws, hours: int) -> int:
    """Toglie le regole temporanee della utility piu' vecchie di `hours` (le permanenti restano)."""
    try:
        rules = list_access(aws)
    except OperationError:
        return 0
    limit = datetime.now(timezone.utc) - timedelta(hours=hours)
    old = [r for r in rules if r.kind == "temporaneo" and r.opened_at and r.opened_at < limit]
    for r in old:
        close_cidr(aws, r.cidr)
    return len(old)


# ---------------------------------------------------------------- descrizione dei DB

@dataclass
class DbSummary:
    name: str
    size: str = ""
    size_bytes: int = 0
    migrations: dict[str, str | None] = field(default_factory=dict)   # schema -> ultima migration
    tables: int = 0
    rows_hint: int = 0                                                # stima dalle statistiche

    def migrations_text(self) -> str:
        present = {k: v for k, v in self.migrations.items() if v}
        if not present:
            return "nessuno schema"
        return f"{len(present)} schemi, ultima {max(present.values())[:14]}"


def _tables(cur, schemas: list[str], skip: set[str] | None = None) -> list[str]:
    """Tabelle "vere" degli schemi (niente partizioni figlie), nella forma schema.tabella."""
    cur.execute("""
        select n.nspname || '.' || c.relname
        from pg_class c join pg_namespace n on n.oid = c.relnamespace
        where c.relkind in ('r','p') and not c.relispartition and n.nspname = any(%s)
        order by 1""", (schemas,))
    names = [r[0] for r in cur.fetchall()]
    skip = skip or set()
    return [t for t in names if t not in skip and not t.endswith("." + HISTORY_TABLE)]


def summarize(conn: Conn, schemas: list[str], dbname: str | None = None) -> DbSummary:
    name = dbname or conn.dbname
    with conn.connect(name) as c, c.cursor() as cur:
        cur.execute("select pg_size_pretty(pg_database_size(current_database())), pg_database_size(current_database())")
        size, size_bytes = cur.fetchone()
        summary = DbSummary(name, size, size_bytes)
        for schema in schemas:
            cur.execute("select to_regclass(%s)", (f"{schema}.{HISTORY_TABLE}",))
            if cur.fetchone()[0] is None:
                summary.migrations[schema] = None
                continue
            cur.execute(sql.SQL("select max(migration_id) from {}.{}").format(
                sql.Identifier(schema), sql.Identifier(HISTORY_TABLE)))
            summary.migrations[schema] = cur.fetchone()[0]
        tables = _tables(cur, schemas)
        summary.tables = len(tables)
        cur.execute("""select coalesce(sum(greatest(c.reltuples, 0)), 0)::bigint from pg_class c
                       join pg_namespace n on n.oid = c.relnamespace
                       where c.relkind = 'r' and n.nspname = any(%s)""", (schemas,))
        summary.rows_hint = cur.fetchone()[0]
    return summary


def list_local(settings: Settings, conn: Conn) -> list[DbSummary]:
    with conn.connect("postgres") as c, c.cursor() as cur:
        cur.execute("select datname from pg_database where not datistemplate and datallowconn order by 1")
        names = [r[0] for r in cur.fetchall() if r[0] not in settings.database.locale.escludi]
    out = []
    for name in names:
        try:
            out.append(summarize(conn, settings.database.schemi, name))
        except psycopg.Error as exc:
            log.warning("DB locale %s: %s", name, exc)
    return out


def migration_diff(a: DbSummary, b: DbSummary) -> list[str]:
    """Schemi in cui l'ultima migration non coincide."""
    keys = sorted(set(a.migrations) | set(b.migrations))
    return [f"{k}: {a.name}={a.migrations.get(k) or '-'}  {b.name}={b.migrations.get(k) or '-'}"
            for k in keys if a.migrations.get(k) != b.migrations.get(k)]


# ---------------------------------------------------------------- strumenti pg

class PgTools:
    def __init__(self, settings: Settings, cancel: threading.Event | None, progress: Progress = no_progress,
                 aws: Aws | None = None):
        self.aws = aws
        self.bin = Path(settings.database.locale.bin)
        self.jobs = max(1, settings.database.lavori_paralleli_restore)
        self.cancel = cancel
        self.progress = progress
        for tool in ("pg_dump",):
            if not (self.bin / f"{tool}.exe").exists() and not (self.bin / tool).exists():
                raise OperationError(f"{tool} non trovato in {self.bin} (database.locale.bin)")

    def _run(self, tool: str, args: list[str], env: dict, phase: str) -> tuple[int, list[str]]:
        exe = str(self.bin / tool)
        log.info("%s %s", tool, " ".join(a for a in args if not a.startswith("password")))
        proc = subprocess.Popen([exe, *args], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, env=env,
                                text=True, encoding="utf-8", errors="replace", creationflags=_NO_WINDOW)
        lines: list[str] = []
        assert proc.stdout is not None
        try:
            for line in proc.stdout:
                line = line.rstrip()
                if line:
                    lines.append(line)
                    self.progress(phase, 0, 0, line[:120])
                check_cancel(self.cancel)
        except BaseException:
            # Mai lasciare un pg_restore orfano che continua a scrivere sul DB.
            proc.kill()
            proc.wait()
            raise
        return proc.wait(), lines

    def dump(self, conn: Conn, dbname: str, out: Path, extra: list[str]) -> Path:
        code, lines = self._run("pg_dump", [*conn.args(dbname), "-Fc", "--no-owner", "--no-privileges",
                                            "-f", str(out), *extra], conn.env(), "dump")
        if code != 0:
            raise OperationError("pg_dump fallito:\n" + "\n".join(lines[-15:]))
        log.info("Dump %s: %.1f MB", out.name, out.stat().st_size / 1e6)
        return out

    def restore(self, conn: Conn, dump: Path, extra: list[str], env_extra: dict | None = None,
                jobs: int | None = None) -> list[str]:
        """pg_restore dentro la VPC, con il task usa-e-getta dbloader dello stack.

        Dal PC una sessione lunga verso RDS viene interrotta dopo circa 10 minuti e la latenza rende il
        restore lentissimo: il dump si carica nel bucket di build e il task lo scarica con un URL firmato.
        """
        if self.aws is None:
            raise OperationError("restore su AWS senza sessione AWS")
        aws, out = self.aws, stack_info(self.aws).outputs
        stack_info(aws).need("ClusterName", "DbLoaderTaskFamily", "DbLoaderLogGroup", "SubnetIds",
                             "AppSecurityGroupId", "BuildBucketName")
        bucket, key = out["BuildBucketName"], f"dump/{uuid.uuid4().hex}/{dump.name}"
        s3 = aws.client("s3")
        signer = presigner(aws)
        log.info("Carico il dump %s (%.1f MB) in s3://%s/%s", dump.name, dump.stat().st_size / 1e6, bucket, key)
        s3.upload_file(str(dump), bucket, key)
        try:
            url = signer.generate_presigned_url("get_object", Params={"Bucket": bucket, "Key": key}, ExpiresIn=7200)
            env = {"DUMP_URL": url, "RESTORE_ARGS": " ".join(extra), "JOBS": str(jobs or self.jobs),
                   "PGHOST": conn.host, "PGPORT": str(conn.port), "PGDATABASE": conn.dbname}
            env.update(env_extra or {})
            lines, status = self._run_loader(out, env)
        finally:
            s3.delete_object(Bucket=bucket, Key=key)
        exit_line = next((x for x in reversed(lines) if x.startswith("PG_RESTORE_EXIT=")), None)
        if exit_line is None:
            raise OperationError(f"Il task di caricamento si e' fermato senza esito ({status}).\n"
                                 + "\n".join(lines[-15:]))
        code = exit_line.split("=", 1)[1]
        if code == "download":
            raise OperationError("Il task di caricamento non ha potuto scaricare il dump dal bucket")
        errors = [x for x in lines if "error:" in x.lower()]
        real = [x for x in errors if not any(b in x for b in _BENIGN)]
        if code != "0" and (real or not errors):
            raise OperationError("pg_restore fallito:\n" + "\n".join((real or lines)[-15:]))
        if errors:
            log.warning("pg_restore: %d errori attesi su RDS ignorati (es. %s)", len(errors) - len(real),
                        errors[0][:160])
        return lines

    def _run_loader(self, out: dict, env: dict) -> tuple[list[str], str]:
        lines, status, _ = run_db_task(self.aws, out, env, self.cancel, self.progress, "PG_RESTORE_EXIT=", "restore")
        return lines, status


def run_db_task(aws: Aws, out: dict, env: dict, cancel, progress: Progress, marker: str, phase: str,
                command: str | list[str] | None = None, family: str | None = None, container: str = "dbloader",
                log_group: str | None = None) -> tuple[list[str], str, int | None]:
    """Esegue il task usa-e-getta dbloader dentro la VPC e ne restituisce le righe di log.

    `command` sostituisce lo script del container (l'entrypoint e' `sh -c`): cosi' lo stesso task fa
    sia il restore sia il dump, senza cambiare il template. `marker` e' l'inizio della riga d'esito
    che lo script scrive per ultima.

    Con `family`, `container` e `log_group` lancia un'altra task definition dello stack (es. quella del
    backend per `retention`); `command` puo' essere una lista di argomenti. Restituisce anche il codice
    di uscita del container.
    """
    ecs, logs = aws.client("ecs"), aws.client("logs")
    override = {"name": container, "environment": [{"name": k, "value": v} for k, v in env.items()]}
    if command:
        override["command"] = command if isinstance(command, list) else [command]
    log_group = log_group or out["DbLoaderLogGroup"]
    resp = ecs.run_task(
        cluster=out["ClusterName"], taskDefinition=family or out["DbLoaderTaskFamily"], launchType="FARGATE", count=1,
        networkConfiguration={"awsvpcConfiguration": {
            "subnets": out["SubnetIds"].split(","), "securityGroups": [out["AppSecurityGroupId"]],
            "assignPublicIp": "ENABLED"}},
        overrides={"containerOverrides": [override]})
    if resp.get("failures") or not resp.get("tasks"):
        raise OperationError(f"Avvio del task {container} fallito: {resp.get('failures')}")
    arn = resp["tasks"][0]["taskArn"]
    stream = f"{container}/{container}/{arn.rsplit('/', 1)[-1]}"
    log.info("Task %s avviato: %s", container, arn.rsplit("/", 1)[-1])
    lines: list[str] = []

    def pull() -> None:
        # Sempre dall'inizio: un cursore preso quando lo stream era vuoto puo' saltare righe
        # arrivate dopo, e il log del task e' piccolo.
        got: list[str] = []
        token = None
        while True:
            kw = {"logGroupName": log_group, "logStreamName": stream, "startFromHead": True}
            if token:
                kw["nextToken"] = token
            try:
                res = logs.get_log_events(**kw)
            except ClientError as exc:
                if exc.response["Error"]["Code"] == "ResourceNotFoundException":
                    return
                raise
            got += [e["message"].rstrip() for e in res.get("events", []) if e["message"].strip()]
            if not res.get("events") or res.get("nextForwardToken") == token:
                break
            token = res.get("nextForwardToken")
        for line in got[len(lines):]:
            progress(phase, 0, 0, line[:120])
        lines[:] = got

    status, task = "", {}
    try:
        while True:
            sleep(10, cancel)
            task = ecs.describe_tasks(cluster=out["ClusterName"], tasks=[arn])["tasks"][0]
            status = task.get("lastStatus", "")
            pull()
            if status == "STOPPED":
                break
            progress(phase, 0, 0, f"task {status.lower()}")
    except BaseException:
        ecs.stop_task(cluster=out["ClusterName"], task=arn, reason="interrotto dalla utility 22")
        raise
    for _ in range(6):        # i log possono arrivare qualche secondo dopo l'arresto
        if any(x.startswith(marker) for x in lines):
            break
        sleep(5, None)
        pull()
    code = next((c.get("exitCode") for c in task.get("containers", []) if c.get("name") == container), None)
    return lines, f"{status}: {task.get('stoppedReason', '')}", code


def presigner(aws: Aws):
    """Client S3 per gli URL firmati: firma v4 sull'endpoint regionale (Milano e' una regione opt-in
    e rifiuta gli URL globali)."""
    region = aws.settings.aws.region
    return aws.session.client("s3", region_name=region, endpoint_url=f"https://s3.{region}.amazonaws.com",
                              config=Config(signature_version="s3v4"))


# ---------------------------------------------------------------- operazioni sul DB di AWS

def _recreate_database(rds: Conn) -> None:
    with rds.connect("postgres", autocommit=True) as c:
        c.execute(sql.SQL("drop database if exists {} with (force)").format(sql.Identifier(rds.dbname)))
        c.execute(sql.SQL("create database {}").format(sql.Identifier(rds.dbname)))
    log.info("Database %s ricreato vuoto", rds.dbname)


def _truncate(rds: Conn, schemas: list[str], keep: set[str]) -> int:
    with rds.connect() as c, c.cursor() as cur:
        tables = _tables(cur, schemas, keep)
        if tables:
            idents = sql.SQL(", ").join(sql.Identifier(*t.split(".", 1)) for t in tables)
            cur.execute(sql.SQL("truncate table {} restart identity").format(idents))
        c.commit()
    log.info("Svuotate %d tabelle (tenute: storia delle migration%s)", len(tables),
             f" e {len(keep)} tabelle di seed" if keep else "")
    return len(tables)


def load_full(aws: Aws, local: Conn, src: str, rds: Conn, cancel, progress: Progress) -> None:
    """Schema + dati: il DB su AWS viene sostituito per intero dal DB locale."""
    settings = aws.settings
    tools = PgTools(settings, cancel, progress, aws)
    with tempfile.TemporaryDirectory(prefix="sb22-dump-") as tmp:
        dump = tools.dump(local, src, Path(tmp) / f"{src}.dump", [])
        check_cancel(cancel)
        _recreate_database(rds)
        tools.restore(rds, dump, [])


def load_schema(aws: Aws, local: Conn, src: str, rds: Conn, cancel, progress: Progress) -> None:
    """Solo schema, piu' la storia delle migration e le tabelle di seed: il backend parte."""
    settings = aws.settings
    tools = PgTools(settings, cancel, progress, aws)
    with tempfile.TemporaryDirectory(prefix="sb22-dump-") as tmp:
        schema = tools.dump(local, src, Path(tmp) / f"{src}-schema.dump", ["--schema-only"])
        with local.connect(src) as c, c.cursor() as cur:
            cur.execute("""select n.nspname || '.' || c.relname from pg_class c
                           join pg_namespace n on n.oid = c.relnamespace
                           where c.relkind in ('r','p') and n.nspname = any(%s)""", (settings.database.schemi,))
            existing = {r[0] for r in cur.fetchall()}
        wanted = [t for t in settings.database.tabelle_seed if t in existing]
        wanted += [f"{s}.{HISTORY_TABLE}" for s in settings.database.schemi if f"{s}.{HISTORY_TABLE}" in existing]
        args = ["--data-only"]
        for t in wanted:
            args += ["-t", t]
        seed = tools.dump(local, src, Path(tmp) / f"{src}-seed.dump", args) if wanted else None
        check_cancel(cancel)
        _recreate_database(rds)
        tools.restore(rds, schema, [])
        if seed:
            _restore_data(tools, rds, seed)


def _restore_data(tools: PgTools, rds: Conn, dump: Path) -> None:
    """Dati con vincoli e trigger sospesi (session_replication_role), se RDS lo consente."""
    try:
        tools.restore(rds, dump, ["--data-only"], {"PGOPTIONS": "-c session_replication_role=replica"})
    except OperationError as exc:
        if "session_replication_role" not in str(exc):
            raise
        log.warning("RDS non consente session_replication_role: carico nell'ordine delle dipendenze")
        tools.restore(rds, dump, ["--data-only"], jobs=1)


def load_data(aws: Aws, local: Conn, src: str, rds: Conn, cancel, progress: Progress) -> None:
    """Solo dati: lo schema su AWS resta, i dati si sostituiscono (storia delle migration esclusa)."""
    settings = aws.settings
    s = settings.database
    tools = PgTools(settings, cancel, progress, aws)
    with tempfile.TemporaryDirectory(prefix="sb22-dump-") as tmp:
        args = ["--data-only", f"--exclude-table-data=*.{HISTORY_TABLE}"]
        for schema in s.schemi:
            args += ["-n", schema]
        dump = tools.dump(local, src, Path(tmp) / f"{src}-data.dump", args)
        check_cancel(cancel)
        _truncate(rds, s.schemi, set())
        _restore_data(tools, rds, dump)


def empty_data(settings: Settings, rds: Conn) -> int:
    """Svuota tutto tranne la storia delle migration e le tabelle di seed."""
    return _truncate(rds, settings.database.schemi, set(settings.database.tabelle_seed))


def count_rows(conn: Conn, schemas: list[str], dbname: str | None = None, cancel=None) -> dict[str, int]:
    out = {}
    with conn.connect(dbname) as c, c.cursor() as cur:
        for t in _tables(cur, schemas):
            check_cancel(cancel)
            cur.execute(sql.SQL("select count(*) from {}").format(sql.Identifier(*t.split(".", 1))))
            out[t] = cur.fetchone()[0]
    return out


def verify(settings: Settings, local: Conn, src: str, rds: Conn, cancel) -> list[str]:
    """Conteggio righe per tabella, locale contro AWS. Restituisce le differenze."""
    a = count_rows(local, settings.database.schemi, src, cancel)
    b = count_rows(rds, settings.database.schemi, None, cancel)
    diff = [f"{t}: locale {a.get(t, '-')}, AWS {b.get(t, '-')}" for t in sorted(set(a) | set(b))
            if a.get(t) != b.get(t)]
    log.info("Verifica: %d tabelle, %d righe in locale, %d su AWS, %d differenze",
             len(a), sum(a.values()), sum(b.values()), len(diff))
    for line in diff[:30]:
        log.warning("  %s", line)
    return diff
