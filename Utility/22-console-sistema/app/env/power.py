"""Piano su SSM, stato dell'ambiente, Lambda di accensione (solo per gli ambienti con orario).

Il piano si salva solo se nessun altro l'ha cambiato dopo che lo si e' letto (versione SSM). Ogni
salvataggio toglie gli intervalli vecchi, registra la modifica nello storico e invoca la Lambda,
cosi' l'effetto non aspetta il giro dei 5 minuti.

Un ambiente senza orario (produzione) non ha piano: lo stato si legge dagli output dello stack.
"""
from __future__ import annotations

import io
import json
import logging
import time
import zipfile
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Callable

from botocore.exceptions import ClientError

from app.env import plan as P
from app.env.awsutil import Aws, OperationError, StackInfo, stack_info
from app.logging_setup import LOGGER_NAME
from app.settings import SOURCE_DIR

log = logging.getLogger(LOGGER_NAME)

PLACEHOLDER_MAX_BYTES = 5000   # il codice segnaposto del template pesa meno di 1 KB


class PlanConflict(OperationError):
    pass


@dataclass
class PlanDoc:
    plan: dict
    version: int


def now_utc() -> datetime:
    return datetime.now(timezone.utc)


def read_plan(aws: Aws) -> PlanDoc:
    s = aws.settings
    if not s.has_schedule:
        raise OperationError(f"L'ambiente {s.name} non ha un piano di accensione")
    try:
        par = aws.client("ssm").get_parameter(Name=s.plan_parameter)["Parameter"]
    except ClientError as exc:
        if exc.response["Error"]["Code"] == "ParameterNotFound":
            raise OperationError(f"Il parametro {s.plan_parameter} non esiste: lo stack non e' stato creato?") from exc
        raise
    plan = json.loads(par["Value"])
    plan.setdefault("manutenzione", None)
    P.validate(plan)
    return PlanDoc(plan, int(par["Version"]))


MAX_PLAN_CHARS = 8192   # limite di un parametro SSM Advanced


def _fit(plan: dict) -> str:
    """JSON compatto; se supera il limite di SSM si tolgono le voci piu' vecchie dello storico."""
    while True:
        body = json.dumps(plan, ensure_ascii=False, separators=(",", ":"))
        if len(body) <= MAX_PLAN_CHARS or not plan.get("storico"):
            return body
        plan["storico"] = plan["storico"][1:]


def save_plan(aws: Aws, doc: PlanDoc, new_plan: dict, kind: str, text: str, invoke: bool = True) -> PlanDoc:
    """Salva `new_plan` se la versione su SSM e' ancora quella letta; poi applica subito."""
    s = aws.settings
    now = now_utc()
    new_plan = P.prune(new_plan, now, s.storico_max)
    new_plan = P.add_history(new_plan, now, aws.actor(), kind, text, s.storico_max)
    P.validate(new_plan)
    current = aws.client("ssm").get_parameter(Name=s.plan_parameter)["Parameter"]
    if int(current["Version"]) != doc.version:
        raise PlanConflict("Il piano e' stato modificato da un'altra parte dopo che l'avevo letto: "
                           "l'ho riletto, rivedere e riprovare.")
    body = _fit(new_plan)
    out = aws.client("ssm").put_parameter(Name=s.plan_parameter, Value=body, Type="String",
                                          Overwrite=True, Tier=s.plan_parameter_tier)
    log.info("Piano salvato (versione %s): %s", out["Version"], text)
    if invoke:
        apply_now(aws)
    return PlanDoc(new_plan, int(out["Version"]))


def update_plan(aws: Aws, change: Callable[[dict], dict], kind: str, text: str, invoke: bool = True,
                attempts: int = 3) -> PlanDoc:
    """Rilegge, applica `change`, salva; ritenta se nel frattempo qualcun altro ha salvato.

    Per le modifiche fatte dalle operazioni (manutenzione, storico): la modifica e' piccola e non
    dipende da quello che l'utente stava guardando, quindi ritentare e' sicuro.
    """
    for i in range(attempts):
        doc = read_plan(aws)
        try:
            return save_plan(aws, doc, change(doc.plan), kind, text, invoke)
        except PlanConflict:
            if i == attempts - 1:
                raise
            time.sleep(1)
    raise AssertionError("non raggiungibile")


def apply_now(aws: Aws) -> dict:
    """Invoca la Lambda e ne restituisce la risposta (la riga del giro)."""
    resp = aws.client("lambda").invoke(FunctionName=aws.settings.lambda_name,
                                       Payload=json.dumps({"origine": "utility"}).encode())
    payload = json.loads(resp["Payload"].read() or b"{}")
    if resp.get("FunctionError"):
        log.error("Lambda %s: errore %s", aws.settings.lambda_name, payload)
    else:
        log.info("Lambda: %s - voluto %s, azioni: %s", payload.get("esito"), payload.get("voluto"),
                 "; ".join(payload.get("azioni") or []) or "nessuna")
    return payload


# ---------------------------------------------------------------- stato

@dataclass
class EnvStatus:
    identity: dict
    stack: StackInfo
    doc: PlanDoc | None
    db_status: str                       # stato RDS oppure "assente"
    db_info: dict = field(default_factory=dict)
    services: dict[str, dict | None] = field(default_factory=dict)
    desired: tuple[str, str] = ("", "")
    events: list[dict] = field(default_factory=list)
    last_run: dict | None = None
    lambda_placeholder: bool | None = None


def db_status(aws: Aws, ident: str) -> tuple[str, dict]:
    try:
        db = aws.client("rds").describe_db_instances(DBInstanceIdentifier=ident)["DBInstances"][0]
        return db["DBInstanceStatus"], db
    except ClientError as exc:
        if exc.response["Error"]["Code"] == "DBInstanceNotFound":
            return "assente", {}
        raise


def services_status(aws: Aws, plan: dict) -> dict[str, dict | None]:
    out: dict[str, dict | None] = {}
    for item in plan.get("risorse", {}).get("ecs", []):
        res = aws.client("ecs").describe_services(cluster=item["cluster"], services=[item["servizio"]])
        svc = next((x for x in res.get("services", []) if x.get("status") == "ACTIVE"), None)
        out[item["servizio"]] = None if svc is None else {
            "desired": svc["desiredCount"], "running": svc["runningCount"], "pending": svc["pendingCount"],
            "deployments": len(svc.get("deployments", [])),
            "rollout": (svc.get("deployments") or [{}])[0].get("rolloutState", ""),
        }
    return out


def last_lambda_run(aws: Aws, hours: int = 24) -> dict | None:
    start = int((now_utc() - timedelta(hours=hours)).timestamp() * 1000)
    try:
        res = aws.client("logs").filter_log_events(logGroupName=aws.settings.log_group, startTime=start,
                                                   filterPattern='"esito"')
    except ClientError as exc:
        if exc.response["Error"]["Code"] == "ResourceNotFoundException":
            return None
        raise
    events = res.get("events", [])
    while res.get("nextToken"):
        res = aws.client("logs").filter_log_events(logGroupName=aws.settings.log_group, startTime=start,
                                                   filterPattern='"esito"', nextToken=res["nextToken"])
        events += res.get("events", [])
    if not events:
        return None
    last = max(events, key=lambda e: e["timestamp"])
    msg = last["message"]
    try:
        data = json.loads(msg[msg.index("{"):])
    except ValueError:
        data = {"esito": "?", "motivo": msg.strip()[:200]}
    data["_quando"] = datetime.fromtimestamp(last["timestamp"] / 1000, timezone.utc)
    return data


def lambda_is_placeholder(aws: Aws) -> bool | None:
    try:
        cfg = aws.client("lambda").get_function_configuration(FunctionName=aws.settings.lambda_name)
    except ClientError:
        return None
    return cfg.get("CodeSize", 0) < PLACEHOLDER_MAX_BYTES


def _stack_resources(st: StackInfo) -> dict:
    """Risorse da guardare, ricavate dagli output, per gli ambienti senza piano."""
    out = st.outputs
    ecs = ([{"cluster": out["ClusterName"], "servizio": out["WebServiceName"]}]
           if out.get("ClusterName") and out.get("WebServiceName") else [])
    rds = [out["DbInstanceId"]] if out.get("DbInstanceId") else []
    return {"ecs": ecs, "rds": rds}


def env_status(aws: Aws) -> EnvStatus:
    ident = aws.check_account()
    st = stack_info(aws)
    if not aws.settings.has_schedule:
        res = _stack_resources(st)
        status, info = db_status(aws, res["rds"][0]) if res["rds"] else ("assente", {})
        return EnvStatus(identity=ident, stack=st, doc=None, db_status=status, db_info=info,
                         services=services_status(aws, {"risorse": res}),
                         desired=(P.ON, "ambiente senza orario: sempre acceso"))
    doc = read_plan(aws)
    rds_ids = doc.plan.get("risorse", {}).get("rds", [])
    status, info = db_status(aws, rds_ids[0]) if rds_ids else ("assente", {})
    now = now_utc()
    return EnvStatus(
        identity=ident, stack=st, doc=doc, db_status=status, db_info=info,
        services=services_status(aws, doc.plan),
        desired=P.desired_state(doc.plan, now),
        events=P.next_events(doc.plan, now, aws.settings.giorni_prossimi_eventi),
        last_run=last_lambda_run(aws),
        lambda_placeholder=lambda_is_placeholder(aws),
    )


# ---------------------------------------------------------------- riconciliatore

def build_lambda_zip() -> bytes:
    """index.py + plan.py + il database dei fusi (tzdata): la Lambda non ha i fusi di sistema."""
    import tzdata  # noqa: PLC0415 - serve solo qui

    app_dir = SOURCE_DIR / "app" / "env"
    buf = io.BytesIO()
    with zipfile.ZipFile(buf, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.write(app_dir / "lambda_src" / "index.py", "index.py")
        zf.write(app_dir / "plan.py", "plan.py")
        root = Path(tzdata.__file__).resolve().parent
        for f in root.rglob("*"):
            if f.is_file() and "__pycache__" not in f.parts:
                zf.write(f, str(Path("tzdata") / f.relative_to(root)))
    return buf.getvalue()


def install_reconciler(aws: Aws) -> str:
    data = build_lambda_zip()
    lam = aws.client("lambda")
    name = aws.settings.lambda_name
    out = lam.update_function_code(FunctionName=name, ZipFile=data)
    lam.get_waiter("function_updated").wait(FunctionName=name)
    log.info("Riconciliatore installato su %s: %d KB, sha256 %s", name, len(data) // 1024, out["CodeSha256"])
    return out["CodeSha256"]
