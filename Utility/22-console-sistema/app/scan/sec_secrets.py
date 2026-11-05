"""Sezione segreti: VALORI di Secrets Manager e di Systems Manager Parameter Store, in chiaro.

Tutto finisce in `secrets/<region>/`, separato dal resto, cosi' la cartella si puo' cifrare o
cancellare a parte. Per Secrets Manager si leggono tutte le versioni elencate (con le loro
etichette AWSCURRENT/AWSPREVIOUS/...); per Parameter Store tutti i parametri, SecureString
decifrati.
"""
from __future__ import annotations

import base64
import logging

from app.scan.awsutil import OK, write_json
from app.scan.engine import S_OK, S_RUN, S_WARN, Ctx, call_logged, ensure, parallel
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)

README = """ATTENZIONE - questa cartella contiene SEGRETI IN CHIARO.

secretsmanager.json : per ogni segreto metadati, resource policy e valore di ogni versione.
ssm-parameters.json : tutti i parametri di Parameter Store, SecureString decifrati.

Non versionare, non condividere, non lasciare su dischi non cifrati.
Per ricrearli in un altro account: aws secretsmanager create-secret / aws ssm put-parameter.
"""


def _secret(ctx: Ctx, region: str, s: dict) -> dict:
    arn = s["ARN"]
    out: dict = {"Name": s.get("Name"), "ARN": arn}
    for op in ("describe_secret", "get_resource_policy"):
        outcome, res = call_logged(ctx, region, "secretsmanager", op, {"SecretId": arn})
        if outcome == OK:
            out[op] = res
    outcome, vers = call_logged(ctx, region, "secretsmanager", "list_secret_version_ids",
                                {"SecretId": arn, "IncludeDeprecated": True})
    versions = (vers or {}).get("Versions") or [{"VersionId": None, "VersionStages": ["AWSCURRENT"]}]
    out["versions"] = []
    for v in versions:
        kw = {"SecretId": arn, **({"VersionId": v["VersionId"]} if v.get("VersionId") else {})}
        outcome, val = call_logged(ctx, region, "secretsmanager", "get_secret_value", kw)
        entry = {"VersionId": v.get("VersionId"), "VersionStages": v.get("VersionStages"),
                 "CreatedDate": v.get("CreatedDate"), "letto": outcome == OK}
        if outcome == OK:
            entry["SecretString"] = val.get("SecretString")
            if val.get("SecretBinary") is not None:
                entry["SecretBinaryBase64"] = base64.b64encode(val["SecretBinary"]).decode()
        out["versions"].append(entry)
    return out


def run_secrets(ctx: Ctx) -> None:
    for region in ctx.regions:
        ctx.check()
        key = f"secrets:{region}"
        ctx.step(key, region, "Segreti (valori in chiaro)", S_RUN, "", "")
        base = ctx.out / "secrets" / region
        listing = ensure(ctx, region, "secretsmanager", "list_secrets", {"IncludePlannedDeletion": True})
        secrets = [s for _, r in listing for s in r.get("SecretList", [])]
        res = parallel(ctx, lambda s: _secret(ctx, region, s), secrets, f"segreto {region}", "secrets")
        good = [r for r in res if isinstance(r, dict)]
        unread = sum(1 for r in good for v in r["versions"] if not v["letto"])

        params_meta = [p for _, r in ensure(ctx, region, "ssm", "describe_parameters") for p in r.get("Parameters", [])]
        names = [p["Name"] for p in params_meta]
        values: dict[str, dict] = {}
        for i in range(0, len(names), 10):
            ctx.check()
            outcome, r = call_logged(ctx, region, "ssm", "get_parameters",
                                     {"Names": names[i:i + 10], "WithDecryption": True})
            for p in (r or {}).get("Parameters", []):
                values[p["Name"]] = p
        params = [{"meta": m, "valore": values.get(m["Name"])} for m in params_meta]
        missing = sum(1 for p in params if p["valore"] is None)

        if good or params:
            base.mkdir(parents=True, exist_ok=True)
            (base.parent / "LEGGIMI.txt").write_text(README, encoding="utf-8")
        if good:
            write_json(base / "secretsmanager.json", good)
        if params:
            write_json(base / "ssm-parameters.json", params)
        notes = []
        if unread:
            notes.append(f"{unread} versioni non lette")
        if missing:
            notes.append(f"{missing} parametri senza valore")
        if len(good) < len(secrets):
            notes.append(f"{len(secrets) - len(good)} segreti in errore")
        status = S_WARN if notes else S_OK
        ctx.step(key, region, "Segreti (valori in chiaro)", status,
                 f"{len(good)} segreti, {len(params)} parametri", "; ".join(notes))
        ctx.section(f"secrets:{region}", status, segreti=len(good), parametri=len(params), note=notes)
