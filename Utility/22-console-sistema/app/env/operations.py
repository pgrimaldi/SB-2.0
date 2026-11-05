"""Procedure lunghe: aggiornamento di backend e frontend, operazioni sul DB, spegnimento con
cancellazione. Usate identiche dalla GUI e dal runner headless.

Ogni procedura riceve `cancel` (Interrompi), `progress` e `step` (righe della tabella delle fasi) e,
quando serve una seconda conferma a meta' lavoro, `confirm(titolo, testo) -> bool`.
"""
from __future__ import annotations

import logging
import os
import subprocess
import tempfile
import threading
import time
from contextlib import contextmanager
from datetime import datetime
from pathlib import Path
from typing import Callable

from botocore.exceptions import ClientError

from app.env import database as D
from app.env import plan as P
from app.env.awsutil import (Aws, Cancelled, OperationError, Progress, check_cancel, http_get, no_progress, sleep,
                         stack_info, update_stack_parameters)
from app.logging_setup import LOGGER_NAME
from app.env.power import _stack_resources, apply_now, db_status, now_utc, read_plan, services_status, update_plan
from app.env.repo import Repo, commit_from_tag, image_tag

log = logging.getLogger(LOGGER_NAME)

Step = Callable[[str, str, str, str], None]          # chiave, passo, stato, note
Confirm = Callable[[str, str], bool]

RUN, OK, KO, SKIP, WARN = "in corso", "OK", "KO", "saltato", "con avvisi"
_NO_WINDOW = getattr(subprocess, "CREATE_NO_WINDOW", 0)


def no_step(key: str, label: str, status: str, notes: str) -> None:
    pass


def yes(title: str, text: str) -> bool:
    return True


class Ctx:
    """Contesto di una procedura: AWS, interruzione, avanzamento, fasi, conferme."""

    def __init__(self, aws: Aws, cancel: threading.Event | None = None, progress: Progress = no_progress,
                 step: Step = no_step, confirm: Confirm = yes):
        self.aws, self.s = aws, aws.settings
        self.cancel, self.progress, self._step, self.confirm = cancel, progress, step, confirm

    def step(self, key: str, label: str, status: str, notes: str = "") -> None:
        self._step(key, label, status, notes)
        if status in (OK, SKIP, WARN):
            log.info("[%s] %s%s", status, label, f" - {notes}" if notes else "")
        elif status == KO:
            log.error("[%s] %s%s", status, label, f" - {notes}" if notes else "")

    @contextmanager
    def phase(self, key: str, label: str):
        self.step(key, label, RUN)
        try:
            yield
        except Cancelled:
            self.step(key, label, KO, "interrotto")
            raise
        except Exception as exc:
            self.step(key, label, KO, f"{type(exc).__name__}: {str(exc).splitlines()[0][:200]}")
            raise


# ---------------------------------------------------------------- manutenzione e attese

def _kick(ctx: Ctx) -> None:
    """Fa girare subito il riconciliatore, se l'ambiente ha l'orario."""
    if ctx.s.has_schedule:
        apply_now(ctx.aws)


def _resources(ctx: Ctx) -> dict:
    if ctx.s.has_schedule:
        return read_plan(ctx.aws).plan.get("risorse", {})
    return _stack_resources(stack_info(ctx.aws))


@contextmanager
def maintenance(ctx: Ctx, reason: str):
    """DB acceso e servizi a 0 per la durata del blocco; la scadenza protegge da una chiusura improvvisa.

    Senza orario (produzione) non c'e' piano da cambiare: il blocco gira senza fermare niente.
    """
    if not ctx.s.has_schedule:
        log.info("Ambiente senza orario: nessuna manutenzione da aprire (%s)", reason)
        yield
        return
    actor = ctx.aws.actor()
    update_plan(ctx.aws, lambda p: P.with_maintenance(p, now_utc(), ctx.s.ore_massime_manutenzione, reason, actor),
                "manutenzione", f"inizio: {reason}")
    try:
        yield
    finally:
        update_plan(ctx.aws, P.without_maintenance, "manutenzione", f"fine: {reason}")


def _db_id(ctx: Ctx) -> str:
    ids = _resources(ctx).get("rds", [])
    if not ids:
        raise OperationError("Nessuna istanza RDS fra le risorse dell'ambiente")
    return ids[0]


def wait_db_available(ctx: Ctx, minutes: int = 25) -> None:
    ident = _db_id(ctx)
    end = time.monotonic() + minutes * 60
    kicks = 0
    while True:
        status, _ = db_status(ctx.aws, ident)
        ctx.progress("db", 0, 0, f"DB {ident}: {status}")
        if status == "available":
            return
        if status == "assente":
            raise OperationError("Il DB su AWS non esiste: usare Crea DB")
        if status == "stopped" and kicks < 3 and ctx.s.has_schedule:
            apply_now(ctx.aws)          # la manutenzione chiede il DB acceso: lo avvia la Lambda
            kicks += 1
        if time.monotonic() > end:
            raise OperationError(f"Il DB non e' diventato available in {minutes} minuti (stato {status})")
        sleep(20, ctx.cancel)


def wait_backend(ctx: Ctx) -> str:
    """Se l'orario vuole il backend acceso: servizio stabile e /health sano. Restituisce l'esito."""
    st = stack_info(ctx.aws)
    if ctx.s.has_schedule:
        plan = read_plan(ctx.aws).plan
        state, _ = P.desired_state(plan, now_utc())
        if state != P.ON:
            return f"l'orario lo vuole {state}: partira' alla prossima accensione"
    else:
        plan = {"risorse": _stack_resources(st)}
    minutes = ctx.s.backend.minuti_attesa_servizio
    end = time.monotonic() + minutes * 60
    while True:
        svc = next(iter(services_status(ctx.aws, plan).values()), None)
        if svc and svc["desired"] > 0 and svc["running"] == svc["desired"] and svc["deployments"] == 1:
            break
        ctx.progress("backend", 0, 0, f"servizio: {svc}")
        if time.monotonic() > end:
            raise OperationError(f"Il servizio non si e' stabilizzato in {minutes} minuti: {svc}")
        sleep(15, ctx.cancel)
    url = st.need("ApiUrl")[0].rstrip("/") + ctx.s.backend.health_path
    end = time.monotonic() + ctx.s.backend.minuti_attesa_health * 60
    while True:
        code, body = http_get(url)
        if code == 200 and "Healthy" in body:
            return f"{url} -> Healthy"
        if time.monotonic() > end:
            raise OperationError(f"{url} non risponde Healthy ({code} {body[:120]})")
        sleep(10, ctx.cancel)


# ---------------------------------------------------------------- CodeBuild

def _upload(ctx: Ctx, path: Path, key: str) -> str:
    bucket = stack_info(ctx.aws).need("BuildBucketName")[0]
    ctx.aws.client("s3").upload_file(str(path), bucket, key)
    log.info("Caricato s3://%s/%s (%.1f MB)", bucket, key, path.stat().st_size / 1e6)
    return f"{bucket}/{key}"


def run_build(ctx: Ctx, project: str, location: str, env: dict[str, str]) -> dict:
    cb = ctx.aws.client("codebuild")
    build = cb.start_build(projectName=project, sourceTypeOverride="S3", sourceLocationOverride=location,
                           environmentVariablesOverride=[{"name": k, "value": v, "type": "PLAINTEXT"}
                                                         for k, v in env.items()])["build"]
    ident, started = build["id"], time.monotonic()
    log.info("Build %s avviata", ident)
    try:
        while True:
            sleep(10, ctx.cancel)
            build = cb.batch_get_builds(ids=[ident])["builds"][0]
            ctx.progress("build", 0, 0, f"{project}: {build.get('currentPhase')} ({int(time.monotonic() - started)} s)")
            if build["buildStatus"] != "IN_PROGRESS":
                break
    except Cancelled:
        cb.stop_build(id=ident)
        raise
    if build["buildStatus"] != "SUCCEEDED":
        link = build.get("logs", {}).get("deepLink", "")
        raise OperationError(f"Build {project} {build['buildStatus']}. Log: {link}")
    log.info("Build %s riuscita in %d s", project, time.monotonic() - started)
    return build


def last_build(aws: Aws, project: str) -> dict | None:
    """Ultima build riuscita: commit (dallo zip) e variabili."""
    cb = aws.client("codebuild")
    try:
        ids = cb.list_builds_for_project(projectName=project, sortOrder="DESCENDING").get("ids", [])[:20]
    except ClientError:
        return None
    if not ids:
        return None
    for b in cb.batch_get_builds(ids=ids)["builds"]:
        if b["buildStatus"] == "SUCCEEDED":
            loc = b.get("source", {}).get("location", "")
            env = {v["name"]: v["value"] for v in b.get("environment", {}).get("environmentVariables", [])}
            return {"commit": Path(loc).stem, "env": env, "fine": b.get("endTime")}
    return None


# ---------------------------------------------------------------- backend

MIGRATIONS = "*/Infrastructure/Migrations/*"


def backend_build_project(s, st) -> str:
    """Progetto CodeBuild del backend: dall'output dello stack, altrimenti dalla configurazione."""
    name = st.outputs.get("BackendBuildProject") or s.backend.build_project
    if not name:
        raise OperationError("Progetto CodeBuild del backend sconosciuto (output BackendBuildProject assente e "
                             "backend.build_project vuoto)")
    return name


def ecr_repository(s, st) -> str:
    out = st.outputs
    if out.get("EcrRepositoryName"):
        return out["EcrRepositoryName"]
    if out.get("EcrRepositoryUri"):
        return out["EcrRepositoryUri"].split("/", 1)[1]
    if not s.backend.ecr_repository:
        raise OperationError("Repository ECR sconosciuto (output EcrRepositoryName/EcrRepositoryUri assenti e "
                             "backend.ecr_repository vuoto)")
    return s.backend.ecr_repository


def frontend_build_project(fe, st) -> str:
    name = st.outputs.get(fe.build_output) or fe.build_project
    if not name:
        raise OperationError(f"Progetto CodeBuild del frontend {fe.nome} sconosciuto")
    return name


def _check_migrations_unchanged(ctx: Ctx, repo: Repo, st, sha: str, commit) -> None:
    """Modalita' `blocca`: le migration non si applicano da qui; se il commit le cambia ci si ferma."""
    deployed = commit_from_tag(st.image_tag)
    if not deployed:
        if not ctx.confirm("Primo rilascio", f"Sull'ambiente {ctx.s.name} non c'e' ancora un backend.\n\nLo schema "
                           f"del DB deve essere gia' allineato alle migration del commit {commit.short} (fase "
                           f"«Schema» della migrazione finale). Procedere?"):
            raise Cancelled()
        ctx.step("migration", "Migration", WARN, "primo rilascio: schema dichiarato allineato")
        return
    changed = repo.changed(deployed, sha, f"{ctx.s.backend.cartella}/{MIGRATIONS}")
    if changed:
        raise OperationError(f"Il commit cambia {len(changed)} file di migration rispetto a quello in esercizio "
                             f"({deployed[:12]}): su {ctx.s.name} vanno applicate prima, fuori da questa utility "
                             f"(backend.migration = blocca).\n" + "\n".join(changed[:15]))
    ctx.step("migration", "Migration", OK, f"nessuna migration cambiata da {deployed[:12]}")

def _history(ctx: Ctx, kind: str, text: str) -> None:
    """Storico nel piano (solo con l'orario); in ogni caso nel log della corsa."""
    log.info("Storico: %s - %s", kind, text)
    if ctx.s.has_schedule:
        update_plan(ctx.aws, lambda p: p, kind, text, invoke=False)


def _pending_migrations(ctx: Ctx, repo: Repo, sha: str, rds: D.Conn) -> dict[str, list[str]]:
    pending = {}
    with rds.connect() as c, c.cursor() as cur:
        for module in ctx.s.backend.moduli_migration:
            schema = module.lower()
            wanted = repo.migration_ids(sha, ctx.s.backend.cartella, module)
            cur.execute("select to_regclass(%s)", (f"{schema}.{D.HISTORY_TABLE}",))
            applied = set()
            if cur.fetchone()[0] is not None:
                cur.execute(f'select migration_id from "{schema}"."{D.HISTORY_TABLE}"')
                applied = {r[0] for r in cur.fetchall()}
            missing = [m for m in wanted if m not in applied]
            if missing:
                pending[module] = missing
    return pending


def _run_dotnet(ctx: Ctx, args: list[str], cwd: Path, env: dict) -> None:
    log.info("dotnet %s", " ".join(args))
    proc = subprocess.Popen(["dotnet", *args], cwd=cwd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                            text=True, encoding="utf-8", errors="replace", creationflags=_NO_WINDOW)
    tail: list[str] = []
    assert proc.stdout is not None
    try:
        for line in proc.stdout:
            line = line.rstrip()
            if line:
                tail = (tail + [line])[-20:]
                ctx.progress("migration", 0, 0, line[:120])
            check_cancel(ctx.cancel)
    except BaseException:
        proc.kill()
        proc.wait()
        raise
    if proc.wait() != 0:
        raise OperationError(f"dotnet {' '.join(args[:2])} fallito:\n" + "\n".join(tail))


def _apply_migrations(ctx: Ctx, repo: Repo, sha: str, rds: D.Conn, modules: list[str]) -> None:
    with repo.worktree(sha) as wt:
        root = wt / ctx.s.backend.cartella
        env = dict(os.environ)
        env["ConnectionStrings__SummerBooking"] = (
            f"Host={rds.host};Port={rds.port};Database={rds.dbname};Username={rds.user};"
            f"Password={rds.password};SSL Mode=Require")
        env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        _run_dotnet(ctx, ["build", "Boot/Boot.csproj", "-c", "Debug", "--nologo", "-v", "q"], root, env)
        for module in modules:
            check_cancel(ctx.cancel)
            _run_dotnet(ctx, ["ef", "database", "update", "--no-build",
                              "--project", f"Module.{module}/Module.{module}.csproj"], root, env)


def update_backend(ctx: Ctx, branch: str, rev: str | None = None) -> str:
    s, aws = ctx.s, ctx.aws
    repo = Repo(s)
    st = stack_info(aws)
    if not st.has_db:
        raise OperationError("Il DB su AWS non esiste: prima Crea DB")

    with ctx.phase("commit", "Commit"):
        repo.fetch(branch)
        sha = repo.resolve(rev or repo.ref(branch))
        commit = repo.commit(sha)
        tag = image_tag(branch, sha)
        ctx.step("commit", "Commit", OK, commit.label())

    block = s.backend.migration == "blocca"
    if block:
        with ctx.phase("migration", "Migration"):
            _check_migrations_unchanged(ctx, repo, st, sha, commit)

    with ctx.phase("build", "Immagine"):
        ecr = aws.client("ecr")
        try:
            ecr.describe_images(repositoryName=ecr_repository(s, st), imageIds=[{"imageTag": tag}])
            ctx.step("build", "Immagine", SKIP, f"{tag} gia' presente in ECR")
        except ClientError as exc:
            if exc.response["Error"]["Code"] != "ImageNotFoundException":
                raise
            with tempfile.TemporaryDirectory(prefix="sb22-src-") as tmp:
                zipf = repo.archive(sha, "backend", Path(tmp) / f"{sha}.zip")
                loc = _upload(ctx, zipf, f"backend/{sha}.zip")
            run_build(ctx, backend_build_project(s, st), loc, {"IMAGE_TAG": tag})
            ctx.step("build", "Immagine", OK, tag)

    if not block:
        with ctx.phase("migration", "Migration"):
            _apply_pending(ctx, repo, st, sha, commit)

    with ctx.phase("release", "Rilascio"):
        if st.image_tag == tag:
            ctx.step("release", "Rilascio", SKIP, f"{tag} gia' in esercizio")
        else:
            if s.is_prod and not ctx.confirm("Rilascio in produzione", f"ImageTag {st.image_tag or '-'} -> {tag}\n\n"
                                             f"{commit.label()}\n\nRilasciare?"):
                raise Cancelled()
            update_stack_parameters(aws, {"ImageTag": tag}, ctx.cancel, ctx.progress)
            ctx.step("release", "Rilascio", OK, f"ImageTag {st.image_tag or '-'} -> {tag}")
        _kick(ctx)

    with ctx.phase("verify", "Verifica"):
        result = wait_backend(ctx)
        ctx.step("verify", "Verifica", OK, result)

    _history(ctx, "aggiornamento-backend", f"{tag} ({commit.subject[:60]})")
    return tag


def _apply_pending(ctx: Ctx, repo: Repo, st, sha: str, commit) -> None:
    """Modalita' `applica`: migration pendenti applicate dal PC, con DB in manutenzione e seconda conferma."""
    s, aws = ctx.s, ctx.aws
    status, _ = db_status(aws, _db_id(ctx))
    deployed = commit_from_tag(st.image_tag)
    need_db = True
    if status != "available" and deployed:
        changed = repo.changed(deployed, sha, f"{s.backend.cartella}/{MIGRATIONS}")
        need_db = bool(changed)
    if not need_db:
        ctx.step("migration", "Migration", SKIP, "nessuna migration cambiata dal commit in esercizio, DB spento")
    else:
        with maintenance(ctx, f"migration {commit.short}"):
            wait_db_available(ctx)
            rds = D.rds_conn(aws)
            with D.db_access(aws):
                pending = _pending_migrations(ctx, repo, sha, rds)
                if not pending:
                    ctx.step("migration", "Migration", SKIP, "nessuna migration pendente")
                else:
                    text = "\n".join(f"• {m}: {', '.join(v)}" for m, v in pending.items())
                    if not ctx.confirm("Migration pendenti", f"Da applicare sul DB di AWS:\n\n{text}\n\nApplicare?"):
                        raise Cancelled()
                    _apply_migrations(ctx, repo, sha, rds, list(pending))
                    ctx.step("migration", "Migration", OK, f"{sum(len(v) for v in pending.values())} applicate")


# ---------------------------------------------------------------- frontend

def update_frontend(ctx: Ctx, branch: str, name: str, configuration: str, rev: str | None = None) -> str:
    s, aws = ctx.s, ctx.aws
    fe = s.frontend(name)
    if configuration not in fe.configurazioni:
        raise OperationError(f"Configurazione {configuration!r} non ammessa per {name} "
                             f"({', '.join(fe.configurazioni)})")
    repo = Repo(s)
    with ctx.phase("commit", "Commit"):
        repo.fetch(branch)
        sha = repo.resolve(rev or repo.ref(branch))
        commit = repo.commit(sha)
        ctx.step("commit", "Commit", OK, commit.label())
    with ctx.phase("build", f"Build e pubblicazione ({configuration})"):
        if s.is_prod and not ctx.confirm("Pubblicazione in produzione", f"{name} dal commit:\n{commit.label()}\n\n"
                                         f"Configurazione: {configuration}. Pubblicare?"):
            raise Cancelled()
        with tempfile.TemporaryDirectory(prefix="sb22-src-") as tmp:
            zipf = repo.archive(sha, fe.cartella, Path(tmp) / f"{sha}.zip")
            loc = _upload(ctx, zipf, f"frontend/{sha}.zip")
        run_build(ctx, frontend_build_project(fe, stack_info(aws)), loc, {"BUILD_CONFIGURATION": configuration})
        ctx.step("build", f"Build e pubblicazione ({configuration})", OK, commit.short)
    with ctx.phase("verify", "Verifica"):
        url = stack_info(aws).need(fe.output_url)[0].rstrip("/") + "/"
        code, _ = http_get(url)
        if code != 200:
            raise OperationError(f"{url} risponde {code}")
        ctx.step("verify", "Verifica", OK, f"{url} -> 200")
    _history(ctx, "aggiornamento-frontend", f"{name}: {commit.short} {configuration} ({commit.subject[:60]})")
    return sha


# ---------------------------------------------------------------- database

def _load(ctx: Ctx, local: D.Conn, rds: D.Conn, schema_src: str, data_src: str | None) -> None:
    s = ctx.s
    if data_src == schema_src:
        with ctx.phase("load", f"Schema + dati da {schema_src}"):
            D.load_full(ctx.aws, local, schema_src, rds, ctx.cancel, ctx.progress)
            ctx.step("load", f"Schema + dati da {schema_src}", OK)
    else:
        with ctx.phase("schema", f"Schema da {schema_src}"):
            D.load_schema(ctx.aws, local, schema_src, rds, ctx.cancel, ctx.progress)
            ctx.step("schema", f"Schema da {schema_src}", OK, "con storia delle migration e tabelle di seed")
        if data_src:
            with ctx.phase("data", f"Dati da {data_src}"):
                D.load_data(ctx.aws, local, data_src, rds, ctx.cancel, ctx.progress)
                ctx.step("data", f"Dati da {data_src}", OK)
    from app.env.dbadmin import regrant_if_present  # noqa: PLC0415 - evita l'import circolare

    regrant_if_present(ctx.aws, rds)
    if data_src:
        with ctx.phase("verify-db", "Verifica conteggi"):
            diff = D.verify(s, local, data_src, rds, ctx.cancel)
            ctx.step("verify-db", "Verifica conteggi", WARN if diff else OK,
                     f"{len(diff)} tabelle diverse (vedi log)" if diff else "tutte le tabelle coincidono")


def _destructive_allowed(ctx: Ctx) -> None:
    if not ctx.s.database.operazioni_distruttive:
        raise OperationError(f"Operazione non ammessa sull'ambiente {ctx.s.name} "
                             f"(database.operazioni_distruttive = false)")


def create_db(ctx: Ctx, local: D.Conn, schema_src: str, data_src: str | None) -> None:
    s, aws = ctx.s, ctx.aws
    _destructive_allowed(ctx)
    if stack_info(aws).has_db:
        raise OperationError("Il DB su AWS esiste gia': usare Aggiorna DB")
    if data_src and data_src != schema_src:
        a = D.summarize(local, s.database.schemi, schema_src)
        b = D.summarize(local, s.database.schemi, data_src)
        diff = D.migration_diff(a, b)
        if diff:
            raise OperationError("Schema e dati vengono da DB con migration diverse:\n" + "\n".join(diff))
    with maintenance(ctx, f"crea DB da {schema_src}"):
        with ctx.phase("stack", "Creazione dell'istanza RDS"):
            update_stack_parameters(aws, {"DatabaseEnabled": "true"}, ctx.cancel, ctx.progress)
            ctx.step("stack", "Creazione dell'istanza RDS", OK)
        wait_db_available(ctx)
        rds = D.rds_conn(aws)
        with D.db_access(aws):
            _load(ctx, local, rds, schema_src, data_src)
    _kick(ctx)
    with ctx.phase("verify", "Verifica backend"):
        ctx.step("verify", "Verifica backend", OK, wait_backend(ctx) if stack_info(aws).image_tag else "nessuna immagine")
    _history(ctx, "crea-db", f"schema da {schema_src}, dati da {data_src or 'nessuno'}")


def update_db(ctx: Ctx, local: D.Conn, src: str, mode: str) -> None:
    """mode: 'schema-dati' oppure 'solo-dati'."""
    s, aws = ctx.s, ctx.aws
    _destructive_allowed(ctx)
    if not stack_info(aws).has_db:
        raise OperationError("Il DB su AWS non esiste: usare Crea DB")
    if mode not in ("schema-dati", "solo-dati"):
        raise OperationError(f"modo {mode!r}")
    with maintenance(ctx, f"aggiorna DB da {src} ({mode})"):
        wait_db_available(ctx)
        rds = D.rds_conn(aws)
        with D.db_access(aws):
            local_sum = D.summarize(local, s.database.schemi, src)
            aws_sum = D.summarize(rds, s.database.schemi)
            diff = D.migration_diff(local_sum, aws_sum)
            if mode == "solo-dati" and diff:
                raise OperationError("Solo dati non ammesso: le migration differiscono fra locale e AWS.\n"
                                     + "\n".join(diff) + "\n\nUsare «Schema + dati», oppure allineare lo schema.")
            text = (f"DB su AWS: {aws_sum.size}, {aws_sum.migrations_text()}\n"
                    f"DB locale {src}: {local_sum.size}, {local_sum.migrations_text()}\n"
                    + (("\nMigration diverse:\n" + "\n".join(diff)) if diff else "\nMigration uguali.")
                    + f"\n\nModo: {mode}. I dati attuali su AWS vengono sostituiti. Procedere?")
            if not ctx.confirm("Aggiorna DB", text):
                raise Cancelled()
            if mode == "schema-dati":
                _load(ctx, local, rds, src, src)
            else:
                with ctx.phase("data", f"Dati da {src}"):
                    D.load_data(ctx.aws, local, src, rds, ctx.cancel, ctx.progress)
                    ctx.step("data", f"Dati da {src}", OK)
                with ctx.phase("verify-db", "Verifica conteggi"):
                    d = D.verify(s, local, src, rds, ctx.cancel)
                    ctx.step("verify-db", "Verifica conteggi", WARN if d else OK,
                             f"{len(d)} tabelle diverse (vedi log)" if d else "tutte le tabelle coincidono")
    _kick(ctx)
    _history(ctx, "aggiorna-db", f"da {src} ({mode})")


def empty_db(ctx: Ctx) -> None:
    aws = ctx.aws
    _destructive_allowed(ctx)
    if not stack_info(aws).has_db:
        raise OperationError("Il DB su AWS non esiste")
    with maintenance(ctx, "svuotamento dati"):
        wait_db_available(ctx)
        rds = D.rds_conn(aws)
        with D.db_access(aws), ctx.phase("empty", "Svuotamento dati"):
            n = D.empty_data(ctx.s, rds)
            ctx.step("empty", "Svuotamento dati", OK, f"{n} tabelle svuotate; tenute storia delle migration e seed")
    _kick(ctx)
    _history(ctx, "svuota-db", "dati cancellati, schema e seed tenuti")


def delete_db(ctx: Ctx) -> None:
    aws = ctx.aws
    _destructive_allowed(ctx)
    if not stack_info(aws).has_db:
        ctx.step("delete", "Eliminazione del DB", SKIP, "gia' assente")
        return
    with ctx.phase("delete", "Eliminazione del DB"):
        update_stack_parameters(aws, {"DatabaseEnabled": "false"}, ctx.cancel, ctx.progress)
        ctx.step("delete", "Eliminazione del DB", OK, "istanza eliminata senza snapshot, servizio rimosso")
    if ctx.s.has_schedule:
        update_plan(aws, P.without_maintenance, "elimina-db", "DB eliminato")


def open_access_for(ctx: Ctx, hours: float) -> str:
    """Apre l'accesso temporaneo al proprio IP; la pulizia all'avvio della utility lo richiude."""
    cidr, added = D.open_access(ctx.aws)
    return (f"Accesso {'aperto' if added else 'gia aperto'} per {cidr}. Le aperture temporanee si chiudono "
            f"da sole dopo {ctx.s.ore_massime_manutenzione} ore (all'avvio successivo della utility) o dalla "
            f"scheda «Accessi al DB».")


# ---------------------------------------------------------------- comandi dell'orario

def _need_schedule(ctx: Ctx) -> None:
    if not ctx.s.has_schedule:
        raise OperationError(f"L'ambiente {ctx.s.name} non ha accensione e spegnimento programmati")


def power_off(ctx: Ctx, until: datetime | None, delete: bool = False, data_only: bool = False) -> None:
    aws = ctx.aws
    _need_schedule(ctx)
    if delete and data_only:
        empty_db(ctx)
    actor = aws.actor()
    doc = read_plan(aws)
    it = P.cmd_power_off(doc.plan, now_utc(), actor, until)
    update_plan(aws, lambda p: P.with_interval(p, it), "spegni", f"spento {it['dal']} -> {it['al']}")
    ctx.step("off", "Spegnimento", OK, f"intervallo spento {it['dal']} -> {it['al']}")
    if delete and not data_only:
        delete_db(ctx)


def power_on(ctx: Ctx, until: datetime | None) -> None:
    aws = ctx.aws
    _need_schedule(ctx)
    if not stack_info(aws).has_db:
        raise OperationError("Il DB su AWS e' stato eliminato: usare Crea DB per ricrearlo.")
    doc = read_plan(aws)
    it = P.cmd_power_on(doc.plan, now_utc(), aws.actor(), until, ctx.s.ore_accensione_manuale)
    update_plan(aws, lambda p: P.with_interval(p, it), "accendi", f"acceso {it['dal']} -> {it['al']}")
    ctx.step("on", "Accensione", OK, f"intervallo acceso {it['dal']} -> {it['al']}")
