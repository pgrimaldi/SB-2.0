"""Lambda dev-power: riconciliatore dell'ambiente sb-dev.

A ogni giro legge il piano, calcola lo stato voluto con plan.py (lo stesso file della utility) e
porta servizi ECS e DB in quello stato. Se sono gia' li', non fa niente. Un piano illeggibile o non
valido non fa mai accendere o spegnere niente. Scrive una riga JSON per giro.
"""
from __future__ import annotations

import json
import logging
import os
from datetime import datetime, timezone

import boto3
from botocore.exceptions import ClientError

import plan as P

log = logging.getLogger()
log.setLevel(logging.INFO)

ssm = boto3.client("ssm")
ecs = boto3.client("ecs")
rds = boto3.client("rds")

TRANSIENT = {"starting", "stopping", "modifying", "backing-up", "creating", "deleting", "rebooting",
             "configuring-enhanced-monitoring", "upgrading", "maintenance", "renaming",
             "resetting-master-credentials", "storage-optimization"}


def _emit(result: dict) -> dict:
    log.info(json.dumps(result, ensure_ascii=False, default=str))
    return result


def _db_status(ident: str) -> str:
    try:
        return rds.describe_db_instances(DBInstanceIdentifier=ident)["DBInstances"][0]["DBInstanceStatus"]
    except ClientError as exc:
        if exc.response["Error"]["Code"] == "DBInstanceNotFound":
            return "assente"
        raise


def _service(cluster: str, name: str) -> dict | None:
    out = ecs.describe_services(cluster=cluster, services=[name])
    for svc in out.get("services", []):
        if svc.get("status") == "ACTIVE":
            return svc
    return None


def handler(event, context):
    origine = (event or {}).get("origine", "sconosciuta")
    result = {"esito": "ok", "origine": origine, "voluto": None, "motivo": None, "trovato": {}, "azioni": []}
    try:
        raw = ssm.get_parameter(Name=os.environ["PLAN_PARAMETER"])["Parameter"]["Value"]
        piano = json.loads(raw)
        P.validate(piano)
    except Exception as exc:  # noqa: BLE001 - mai agire su un piano che non si e' potuto leggere
        result.update(esito="errore", motivo=f"piano illeggibile o non valido: {type(exc).__name__}: {exc}")
        return _emit(result)

    now = datetime.now(timezone.utc)
    voluto, motivo = P.desired_state(piano, now)
    db_on = P.db_should_run(piano, now)
    result.update(voluto=voluto, motivo=motivo)
    risorse = piano.get("risorse", {})
    try:
        dbs = {ident: _db_status(ident) for ident in risorse.get("rds", [])}
        services = {}
        for item in risorse.get("ecs", []):
            svc = _service(item["cluster"], item["servizio"])
            services[item["servizio"]] = (item, svc)
        result["trovato"] = {
            "db": dbs,
            "servizi": {k: (None if s is None else {"desired": s["desiredCount"], "running": s["runningCount"]})
                        for k, (_, s) in services.items()},
        }
        absent = [k for k, v in dbs.items() if v == "assente"]

        def set_count(count: int) -> None:
            for name, (item, svc) in services.items():
                if svc is None:
                    if count:
                        result["azioni"].append(f"{name}: servizio assente, ignorato")
                    continue
                if svc["desiredCount"] != count:
                    ecs.update_service(cluster=item["cluster"], service=name, desiredCount=count)
                    result["azioni"].append(f"{name}: desiredCount {svc['desiredCount']} -> {count}")

        if absent:
            # Il DB e' stato eliminato di proposito: non si ricrea e non si accende niente.
            set_count(0)
            result.update(esito="nessuna_azione" if not result["azioni"] else "ok",
                          motivo=f"db_assente ({', '.join(absent)}); {motivo}")
            return _emit(result)

        all_available = all(v == "available" for v in dbs.values())
        if voluto == P.ON or voluto == P.MAINT or db_on:
            for ident, status in dbs.items():
                if status == "stopped":
                    rds.start_db_instance(DBInstanceIdentifier=ident)
                    result["azioni"].append(f"{ident}: avvio")
                elif status in TRANSIENT:
                    result["azioni"].append(f"{ident}: {status}, riprovo al giro dopo")
        if voluto == P.ON:
            if all_available:
                for name, (item, svc) in services.items():
                    target = int(item.get("acceso", 1))
                    if svc is None:
                        result["azioni"].append(f"{name}: servizio assente, ignorato")
                    elif svc["desiredCount"] != target:
                        ecs.update_service(cluster=item["cluster"], service=name, desiredCount=target)
                        result["azioni"].append(f"{name}: desiredCount {svc['desiredCount']} -> {target}")
            else:
                result["azioni"].append("servizi in attesa del DB")
        elif voluto == P.MAINT:
            set_count(0)
        else:
            set_count(0)
            if not db_on:
                for ident, status in dbs.items():
                    if status == "available":
                        rds.stop_db_instance(DBInstanceIdentifier=ident)
                        result["azioni"].append(f"{ident}: arresto")
                    elif status in TRANSIENT:
                        result["azioni"].append(f"{ident}: {status}, riprovo al giro dopo")
        if not result["azioni"]:
            result["esito"] = "nessuna_azione"
    except Exception as exc:  # noqa: BLE001 - il giro dopo rimedia
        result.update(esito="errore", motivo=f"{motivo}; {type(exc).__name__}: {exc}")
    return _emit(result)
