"""Sezioni IAM (credential report) e S3 (configurazione di ogni bucket, mai gli oggetti).

Il dump completo di IAM (utenti, gruppi, ruoli con trust policy, policy gestite con tutte le
versioni, policy inline) e' l'operazione generica `get_account_authorization_details`, gia' in
`json/global/iam/`; qui si aggiunge il credential report, che va prima generato.
"""
from __future__ import annotations

import logging
import time

from app.scan.awsutil import ABSENT, GLOBAL, OK, safe_name, write_json
from app.scan.engine import S_OK, S_RUN, S_WARN, Ctx, call_logged, ensure, parallel
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)


def run_iam(ctx: Ctx) -> None:
    ctx.step("iam", GLOBAL, "IAM: dump completo e credential report", S_RUN, "", "")
    auth = ensure(ctx, GLOBAL, "iam", "get_account_authorization_details")
    if auth and not (ctx.out / "json/global/iam/get_account_authorization_details.json").exists():
        write_json(ctx.out / "json/global/iam/get_account_authorization_details.json", auth[0][1])
    a = auth[0][1] if auth else {}
    counts = (f"{len(a.get('UserDetailList', []))} utenti, {len(a.get('GroupDetailList', []))} gruppi, "
              f"{len(a.get('RoleDetailList', []))} ruoli, {len(a.get('Policies', []))} policy")

    report = None
    for _ in range(30):
        ctx.check()
        outcome, res = call_logged(ctx, GLOBAL, "iam", "generate_credential_report", None)
        if outcome != OK:
            break
        if res.get("State") == "COMPLETE":
            outcome, report = call_logged(ctx, GLOBAL, "iam", "get_credential_report", None)
            break
        time.sleep(2)
    if report and report.get("Content"):
        path = ctx.out / "iam" / "credential-report.csv"
        path.parent.mkdir(parents=True, exist_ok=True)
        content = report["Content"]
        path.write_bytes(content if isinstance(content, bytes) else content.encode("utf-8"))
        ctx.step("iam", GLOBAL, "IAM: dump completo e credential report", S_OK, counts, ctx.rel(path))
        ctx.section("iam", S_OK, riepilogo=counts, credential_report=ctx.rel(path))
    else:
        note = "credential report non disponibile (permessi iam:GenerateCredentialReport/GetCredentialReport?)"
        log.warning(note)
        ctx.step("iam", GLOBAL, "IAM: dump completo e credential report", S_WARN, counts, note)
        ctx.section("iam", S_WARN, riepilogo=counts, nota=note)


# Configurazioni lette per ogni bucket. Gli errori «non configurato» (NoSuch…, …NotFound…) sono normali.
BUCKET_OPS = [
    "get_bucket_policy", "get_bucket_policy_status", "get_bucket_acl", "get_bucket_cors",
    "get_bucket_lifecycle_configuration", "get_bucket_versioning", "get_bucket_encryption",
    "get_bucket_replication", "get_bucket_notification_configuration", "get_bucket_website",
    "get_bucket_logging", "get_bucket_tagging", "get_public_access_block", "get_bucket_ownership_controls",
    "get_bucket_accelerate_configuration", "get_bucket_request_payment", "get_object_lock_configuration",
    "list_bucket_intelligent_tiering_configurations", "list_bucket_analytics_configurations",
    "list_bucket_inventory_configurations", "list_bucket_metrics_configurations",
    "get_bucket_abac", "get_bucket_metadata_configuration",
]


def run_s3(ctx: Ctx) -> None:
    ctx.step("s3", GLOBAL, "S3: configurazione dei bucket (senza oggetti)", S_RUN, "", "")
    listing = ensure(ctx, GLOBAL, "s3", "list_buckets")
    buckets = [b for _, r in listing for b in r.get("Buckets", [])]
    client = ctx.aws.client("s3", GLOBAL)
    ops = [op for op in BUCKET_OPS if op in client.meta.method_to_api_mapping]

    def one(b: dict) -> str:
        name = b["Name"]
        region = b.get("BucketRegion")
        if not region:
            outcome, loc = call_logged(ctx, GLOBAL, "s3", "get_bucket_location", {"Bucket": name})
            region = (loc or {}).get("LocationConstraint") or "us-east-1"
        cfg: dict = {"Bucket": b, "Region": region}
        for op in ops:
            outcome, res = call_logged(ctx, region, "s3", op, {"Bucket": name})
            if outcome == OK:
                cfg[op] = res
            elif outcome == ABSENT:
                cfg[op] = None
        write_json(ctx.out / "json" / GLOBAL / "s3" / "buckets" / f"{safe_name(name)}.json", cfg)
        return region

    res = parallel(ctx, one, buckets, "bucket", "s3")
    bad = sum(isinstance(r, Exception) for r in res)
    status = S_WARN if bad else S_OK
    ctx.step("s3", GLOBAL, "S3: configurazione dei bucket (senza oggetti)", status, f"{len(buckets)} bucket",
             f"{bad} in errore" if bad else "json/global/s3/buckets/")
    ctx.section("s3", status, bucket=len(buckets), errori=bad)
