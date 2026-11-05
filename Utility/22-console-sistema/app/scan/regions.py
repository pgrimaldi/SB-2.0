"""Scelta delle region da leggere.

`mode = auto`: fra le region abilitate nell'account si tengono quelle in cui una sonda veloce trova
almeno una risorsa creata da qualcuno (non quelle di default: VPC e security group predefiniti,
alias KMS `alias/aws/*`, regole EventBridge gestite da AWS). Le sonde si fermano al primo riscontro.
"""
from __future__ import annotations

import logging
from concurrent.futures import ThreadPoolExecutor

import jmespath

from app.scan.awsutil import Aws, classify
from app.logging_setup import LOGGER_NAME
from app.scan.settings import Settings

log = logging.getLogger(LOGGER_NAME)

_LIVE_STACKS = ["CREATE_COMPLETE", "UPDATE_COMPLETE", "UPDATE_ROLLBACK_COMPLETE", "IMPORT_COMPLETE",
                "CREATE_FAILED", "ROLLBACK_COMPLETE", "UPDATE_ROLLBACK_FAILED"]

# (servizio, operazione, parametri, espressione che e' non vuota se c'e' qualcosa)
PROBES: list[tuple[str, str, dict, str]] = [
    ("ec2", "describe_instances", {"MaxResults": 5}, "Reservations[0]"),
    ("ec2", "describe_vpcs", {"Filters": [{"Name": "is-default", "Values": ["false"]}]}, "Vpcs[0]"),
    ("ec2", "describe_volumes", {"MaxResults": 5}, "Volumes[0]"),
    ("ec2", "describe_security_groups", {}, "SecurityGroups[?GroupName!='default'] | [0]"),
    ("lambda", "list_functions", {"MaxItems": 1}, "Functions[0]"),
    ("rds", "describe_db_instances", {}, "DBInstances[0]"),
    ("rds", "describe_db_clusters", {}, "DBClusters[0]"),
    ("ecs", "list_clusters", {}, "clusterArns[0]"),
    ("elbv2", "describe_load_balancers", {}, "LoadBalancers[0]"),
    ("dynamodb", "list_tables", {"Limit": 1}, "TableNames[0]"),
    ("sqs", "list_queues", {}, "QueueUrls[0]"),
    ("sns", "list_topics", {}, "Topics[0]"),
    ("cloudformation", "list_stacks", {"StackStatusFilter": _LIVE_STACKS}, "StackSummaries[0]"),
    ("ecr", "describe_repositories", {"maxResults": 1}, "repositories[0]"),
    ("secretsmanager", "list_secrets", {"MaxResults": 1}, "SecretList[0]"),
    ("ssm", "describe_parameters", {"MaxResults": 1}, "Parameters[0]"),
    ("apigateway", "get_rest_apis", {"limit": 1}, "items[0]"),
    ("apigatewayv2", "get_apis", {}, "Items[0]"),
    ("kms", "list_aliases", {}, "Aliases[?!starts_with(AliasName, 'alias/aws/')] | [0]"),
    ("acm", "list_certificates", {}, "CertificateSummaryList[0]"),
    ("sesv2", "list_email_identities", {}, "EmailIdentities[0]"),
    ("cognito-idp", "list_user_pools", {"MaxResults": 1}, "UserPools[0]"),
    ("elasticache", "describe_cache_clusters", {}, "CacheClusters[0]"),
    ("events", "list_rules", {}, "Rules[?!ManagedBy] | [0]"),
    ("stepfunctions", "list_state_machines", {"maxResults": 1}, "stateMachines[0]"),
    ("efs", "describe_file_systems", {}, "FileSystems[0]"),
    ("lightsail", "get_instances", {}, "instances[0]"),
    ("elasticbeanstalk", "describe_environments", {}, "Environments[0]"),
    ("resourcegroupstaggingapi", "get_resources", {"ResourcesPerPage": 1}, "ResourceTagMappingList[0]"),
]


def enabled_regions(aws: Aws) -> list[str]:
    res = aws.client("ec2", aws.cfg.home_region).describe_regions(AllRegions=False)
    return sorted(r["RegionName"] for r in res["Regions"])


def _probe(aws: Aws, region: str) -> str | None:
    for service, op, kw, expr in PROBES:
        if not aws.service_in_region(service, region):
            continue
        try:
            res = aws.call(service, region, op, kw, paginate=False)
        except Exception as exc:  # noqa: BLE001
            outcome, text = classify(exc)
            log.debug("sonda %s %s.%s: %s %s", region, service, op, outcome, text)
            continue
        if jmespath.search(expr, res):
            return f"{service}.{op}"
    return None


def bucket_regions(aws: Aws) -> dict[str, list[str]]:
    """Region dei bucket S3 (list_buckets riporta BucketRegion)."""
    out: dict[str, list[str]] = {}
    try:
        res = aws.call("s3", aws.cfg.home_region, "list_buckets", {})
    except Exception as exc:  # noqa: BLE001
        log.warning("Elenco bucket S3 non leggibile per la scelta delle region: %s", classify(exc)[1])
        return out
    for b in res.get("Buckets", []):
        reg = b.get("BucketRegion")
        if not reg:
            try:
                loc = aws.call("s3", aws.cfg.home_region, "get_bucket_location", {"Bucket": b["Name"]}, paginate=False)
                reg = loc.get("LocationConstraint") or "us-east-1"
            except Exception as exc:  # noqa: BLE001
                log.debug("Region del bucket %s non leggibile: %s", b["Name"], classify(exc)[1])
                continue
        out.setdefault(reg, []).append(b["Name"])
    return out


def resolve(aws: Aws, settings: Settings, override: list[str] | None = None,
            progress=None) -> tuple[list[str], dict[str, str]]:
    """(region da leggere con la home per prima, motivo per ciascuna)."""
    cfg = settings.regions
    enabled = enabled_regions(aws)
    reasons: dict[str, str] = {}
    for r in cfg.always:
        reasons[r] = "sempre inclusa (configurazione)"
    if override:
        for r in override:
            reasons[r] = "indicata per questa corsa"
    elif cfg.mode == "list":
        for r in cfg.list:
            reasons.setdefault(r, "elenco in configurazione")
    else:
        for reg, names in bucket_regions(aws).items():
            reasons.setdefault(reg, f"bucket S3 ({len(names)})")
        todo = [r for r in enabled if r not in reasons and r not in cfg.exclude]
        log.info("Sonda su %d region abilitate...", len(todo))
        with ThreadPoolExecutor(max_workers=min(16, max(1, len(todo)))) as ex:
            for i, (reg, hit) in enumerate(zip(todo, ex.map(lambda r: _probe(aws, r), todo)), 1):
                if hit:
                    reasons[reg] = f"trovato {hit}"
                if progress:
                    progress("regions", i, len(todo), reg)
    for r in list(reasons):
        if r in cfg.exclude and r not in cfg.always:
            reasons.pop(r)
        elif r not in enabled:
            log.warning("Region %s non abilitata nell'account: esclusa", r)
            reasons.pop(r)
    home = settings.aws.home_region
    regions = sorted(reasons, key=lambda r: (r != home, r))
    return regions, {r: reasons[r] for r in regions}
