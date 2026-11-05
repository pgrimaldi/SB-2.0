"""Template CloudFormation: quelli degli stack esistenti e quelli prodotti dall'IaC generator.

IaC generator (una scansione per region):
1. `start` - lancia le scansioni all'inizio della corsa, cosi' lavorano mentre si leggono le altre
   sezioni. Se AWS rifiuta (limite di scansioni, una gia' in corso) si usa quella in corso o
   l'ultima completata.
2. `finish` - attende le scansioni, elenca le risorse trovate, toglie quelle gia' gestite da uno
   stack e i doppioni fra region (le risorse globali, come IAM, compaiono in ogni scansione: si
   tengono nella prima region, la home), crea i template a blocchi di <= 500 risorse, li scarica
   in YAML e (di default) li cancella dall'account.
"""
from __future__ import annotations

import json
import logging
import time
from datetime import datetime, timezone

from app.scan.awsutil import OK, Cancelled, classify, safe_name, write_json
from app.scan.engine import S_KO, S_OK, S_RUN, S_WARN, Ctx, call_logged, ensure, parallel
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)


# ------------------------------------------------------------------------------------ stack esistenti

def run_stack_templates(ctx: Ctx) -> None:
    for region in ctx.regions:
        ctx.check()
        key = f"stacks:{region}"
        ctx.step(key, region, "Template degli stack CloudFormation", S_RUN, "", "")
        stacks = [s for _, r in ensure(ctx, region, "cloudformation", "describe_stacks") for s in r.get("Stacks", [])]

        def one(s: dict) -> str:
            name = s["StackName"]
            outcome, res = call_logged(ctx, region, "cloudformation", "get_template",
                                       {"StackName": name, "TemplateStage": "Original"})
            if outcome != OK:
                raise RuntimeError(f"get_template: {outcome}")
            body = res.get("TemplateBody")
            folder = ctx.out / "templates" / region / "stacks"
            folder.mkdir(parents=True, exist_ok=True)
            if isinstance(body, (dict, list)):
                path = folder / f"{safe_name(name)}.json"
                path.write_text(json.dumps(body, indent=2, ensure_ascii=False, default=str), encoding="utf-8")
            else:
                text = body or ""
                ext = "json" if text.lstrip().startswith("{") else "yaml"
                path = folder / f"{safe_name(name)}.{ext}"
                path.write_text(text, encoding="utf-8")
            write_json(folder / f"{safe_name(name)}.stack.json",
                       {k: s.get(k) for k in ("StackName", "StackId", "Description", "Parameters", "Capabilities",
                                              "Outputs", "Tags", "RoleARN", "StackStatus", "TerminationProtection",
                                              "ParentId", "RootId")})
            return name

        res = parallel(ctx, one, stacks, f"stack {region}", "stacks")
        bad = sum(isinstance(r, Exception) for r in res)
        status = S_WARN if bad else S_OK
        ctx.step(key, region, "Template degli stack CloudFormation", status, f"{len(stacks) - bad} template",
                 f"{bad} non letti" if bad else "")
        ctx.section(key, status, stack=len(stacks), errori=bad)


# Tipi globali: compaiono in ogni scansione regionale, si tengono una volta sola.
_GLOBAL_PREFIXES = ("AWS::IAM::", "AWS::CloudFront::", "AWS::Route53::", "AWS::Organizations::",
                    "AWS::Billing::", "AWS::S3::Bucket", "AWS::S3::StorageLens")


def _is_global(r: dict) -> bool:
    t = r["ResourceType"]
    if t.startswith("AWS::WAFv2::"):
        return r["ResourceIdentifier"].get("Scope") == "CLOUDFRONT"
    return t.startswith(_GLOBAL_PREFIXES)


class _AwsOwned:
    """Riconosce le risorse che AWS crea da se' in ogni account (documenti SSM pubblici, policy
    gestite di CloudFront, parameter group `default.*`, ...): nel template sarebbero solo rumore e
    lo gonfierebbero oltre il limite di 1 MB. Le liste delle risorse proprie si leggono una volta."""

    def __init__(self, ctx: Ctx):
        self.ctx = ctx
        self._cache: dict[tuple, set | None] = {}

    def _own(self, key: tuple, loader) -> set | None:
        if key not in self._cache:
            try:
                self._cache[key] = set(loader())
            except Exception as exc:  # noqa: BLE001
                log.warning("IaC generator: elenco %s non leggibile (%s), nessun filtro", key, exc)
                self._cache[key] = None
        return self._cache[key]

    def _ssm_docs(self, region: str) -> set:
        res = self.ctx.aws.call("ssm", region, "list_documents", {"Filters": [{"Key": "Owner", "Values": ["Self"]}]})
        return {d["Name"] for d in res.get("DocumentIdentifiers", [])}

    def _prefix_lists(self, region: str) -> set:
        res = self.ctx.aws.call("ec2", region, "describe_managed_prefix_lists", {})
        return {p["PrefixListId"] for p in res.get("PrefixLists", []) if p.get("OwnerId") != "AWS"}

    def _aws_keys(self, region: str) -> set:
        res = self.ctx.aws.call("kms", region, "list_aliases", {})
        return {a["TargetKeyId"] for a in res.get("Aliases", [])
                if a["AliasName"].startswith("alias/aws/") and a.get("TargetKeyId")}

    def _cf_policies(self, op: str, path: str) -> set:
        res = self.ctx.aws.call("cloudfront", "global", op, {"Type": "custom"}, paginate=False)
        items = ((res.get(path) or {}).get("Items")) or []
        return {i[next(k for k in i if k.endswith("Policy"))]["Id"] for i in items}

    def reason(self, r: dict, region: str) -> str | None:
        t, ident = r["ResourceType"], r["ResourceIdentifier"]
        v = str(next(iter(ident.values()), "")) if ident else ""
        if t == "AWS::SSM::Document":
            own = self._own(("ssm", region), lambda: self._ssm_docs(region))
            return "documento SSM di AWS" if own is not None and v not in own else None
        if t == "AWS::EC2::PrefixList":
            own = self._own(("pl", region), lambda: self._prefix_lists(region))
            return "prefix list gestita da AWS" if own is not None and v not in own else None
        if t == "AWS::KMS::Key":
            aws_keys = self._own(("kms", region), lambda: self._aws_keys(region))
            return "chiave KMS gestita da AWS" if aws_keys and v in aws_keys else None
        cf = {"AWS::CloudFront::CachePolicy": ("list_cache_policies", "CachePolicyList"),
              "AWS::CloudFront::OriginRequestPolicy": ("list_origin_request_policies", "OriginRequestPolicyList"),
              "AWS::CloudFront::ResponseHeadersPolicy": ("list_response_headers_policies", "ResponseHeadersPolicyList")}
        if t in cf:
            own = self._own(("cf", t), lambda: self._cf_policies(*cf[t]))
            return "policy CloudFront gestita da AWS" if own is not None and v not in own else None
        rules = [
            (t == "AWS::RAM::Permission" and ":aws:permission/" in v, "permesso RAM gestito da AWS"),
            (t == "AWS::CodeDeploy::DeploymentConfig" and v.startswith("CodeDeployDefault."), "default di CodeDeploy"),
            (t == "AWS::AppConfig::DeploymentStrategy" and v.startswith("AppConfig."), "predefinito di AppConfig"),
            (t == "AWS::AppConfig::Extension" and v.startswith("AWS."), "predefinito di AppConfig"),
            (t.startswith("AWS::AppRunner::") and "/DefaultConfiguration/" in v, "default di App Runner"),
            (t == "AWS::Athena::WorkGroup" and v == "primary", "default di Athena"),
            (t == "AWS::Cassandra::Keyspace" and v.startswith("system"), "keyspace di sistema"),
            (t == "AWS::CloudTrail::Channel" and "aws-service-channel" in v, "canale di servizio AWS"),
            (t == "AWS::ECS::CapacityProvider" and v in ("FARGATE", "FARGATE_SPOT"), "default di ECS"),
            (t.endswith("ParameterGroup") and v.startswith("default."), "parameter group di default"),
            (t == "AWS::RDS::OptionGroup" and v.startswith("default:"), "option group di default"),
            (t in ("AWS::ElastiCache::User", "AWS::ElastiCache::UserGroup", "AWS::MemoryDB::User")
             and v.startswith("default"), "utente di default"),
            (t == "AWS::MemoryDB::ACL" and v == "open-access", "default di MemoryDB"),
            (t == "AWS::Events::EventBus" and v == "default", "event bus di default"),
            (t == "AWS::GameLift::Location", "location di AWS"),
            (t == "AWS::IoT::DomainConfiguration" and v.startswith("iot:"), "default di IoT"),
            (t == "AWS::IoT::EncryptionConfiguration", "default di IoT"),
            (t == "AWS::Logs::LogStream", "stream di log (dati)"),
            (t.startswith("AWS::MediaLive::CloudWatchAlarmTemplate") and v.startswith("aws-"), "predefinito di MediaLive"),
            (t == "AWS::S3::StorageLens" and v == "default-account-dashboard", "default di S3"),
            (t.startswith("AWS::SMSVOICE::") and (v == "Default" or v.endswith("/Default")), "default di SMS"),
            (t == "AWS::Scheduler::ScheduleGroup" and v == "default", "default di EventBridge Scheduler"),
            (t.startswith("AWS::XRay::") and v.endswith("/Default"), "default di X-Ray"),
            (t == "AWS::Route53Resolver::ResolverRuleAssociation" and "rslvr-autodefined" in v,
             "default di Route 53 Resolver"),
            (t == "AWS::IAM::Role" and v.startswith("AWSServiceRoleFor"), "ruolo collegato a un servizio (lo crea AWS)"),
            (t == "AWS::Billing::BillingView" and v.endswith("/primary"), "default di Billing"),
        ]
        return next((why for cond, why in rules if cond), None)


# ------------------------------------------------------------------------------------ IaC generator

_TERMINAL_SCAN = {"COMPLETE", "FAILED", "EXPIRED"}


def _latest_complete(ctx: Ctx, region: str) -> dict | None:
    outcome, res = call_logged(ctx, region, "cloudformation", "list_resource_scans", None)
    scans = [s for s in (res or {}).get("ResourceScanSummaries", []) if s.get("Status") == "COMPLETE"
             and s.get("ScanType", "FULL") == "FULL"]
    return max(scans, key=lambda s: s.get("StartTime")) if scans else None


def start_scans(ctx: Ctx) -> dict[str, dict]:
    """Lancia (o sceglie) la scansione di ogni region: {region: {"id":..., "origine":...}}."""
    out: dict[str, dict] = {}
    reuse = ctx.settings.iac.reuse_recent_scan_minutes
    for region in ctx.regions:
        ctx.check()
        ctx.step(f"iac:{region}", region, "IaC generator: template dalle risorse", S_RUN, "", "scansione…")
        if reuse > 0:
            last = _latest_complete(ctx, region)
            if last and (datetime.now(timezone.utc) - last["StartTime"]).total_seconds() < reuse * 60:
                out[region] = {"id": last["ResourceScanId"], "origine": f"riusata ({last['StartTime']:%Y-%m-%d %H:%M} UTC)"}
                continue
        try:
            res = ctx.aws.call("cloudformation", region, "start_resource_scan", {}, paginate=False)
            out[region] = {"id": res["ResourceScanId"], "origine": "nuova"}
            log.info("IaC generator %s: scansione avviata %s", region, res["ResourceScanId"])
        except Exception as exc:  # noqa: BLE001
            outcome, text = classify(exc)
            log.warning("IaC generator %s: nuova scansione rifiutata (%s)", region, text)
            outcome, res = call_logged(ctx, region, "cloudformation", "list_resource_scans", None)
            running = [s for s in (res or {}).get("ResourceScanSummaries", []) if s.get("Status") == "IN_PROGRESS"]
            last = running[0] if running else _latest_complete(ctx, region)
            if last:
                out[region] = {"id": last["ResourceScanId"],
                               "origine": f"{'in corso' if running else 'ultima completata'} "
                                          f"({last['StartTime']:%Y-%m-%d %H:%M} UTC) - {text}"}
            else:
                out[region] = {"id": None, "origine": text}
    return out


def _wait_scan(ctx: Ctx, region: str, scan_id: str) -> dict:
    deadline = time.monotonic() + ctx.settings.iac.timeout_minutes * 60
    while True:
        ctx.check()
        res = ctx.aws.call("cloudformation", region, "describe_resource_scan", {"ResourceScanId": scan_id},
                           paginate=False)
        pct = res.get("PercentageCompleted") or 0
        ctx.progress("iac", pct, 100, f"{region}: scansione {res.get('Status')} {pct:.0f}%")
        if res.get("Status") in _TERMINAL_SCAN:
            return res
        if time.monotonic() > deadline:
            raise TimeoutError(f"scansione {scan_id} non conclusa in {ctx.settings.iac.timeout_minutes} minuti")
        _sleep(ctx, ctx.settings.iac.poll_seconds)


def _sleep(ctx: Ctx, seconds: float) -> None:
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        ctx.check()
        time.sleep(0.5)


def _wait_template(ctx: Ctx, region: str, name: str) -> dict:
    deadline = time.monotonic() + ctx.settings.iac.timeout_minutes * 60
    while True:
        ctx.check()
        res = ctx.aws.call("cloudformation", region, "describe_generated_template",
                           {"GeneratedTemplateName": name}, paginate=False)
        st = res.get("Status", "")
        prog = res.get("Progress") or {}
        done = (prog.get("ResourcesSucceeded") or 0) + (prog.get("ResourcesFailed") or 0)
        tot = done + (prog.get("ResourcesPending") or 0) + (prog.get("ResourcesProcessing") or 0)
        ctx.progress("iac", done, tot or 1, f"{region}: template {name} {st}")
        if st in ("COMPLETE", "FAILED"):
            return res
        if time.monotonic() > deadline:
            raise TimeoutError(f"template {name} non concluso in {ctx.settings.iac.timeout_minutes} minuti")
        _sleep(ctx, ctx.settings.iac.poll_seconds)


def finish_scans(ctx: Ctx, scans: dict[str, dict]) -> None:
    seen: set[tuple[str, str]] = set()
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    cfg = ctx.settings.iac
    owned = _AwsOwned(ctx)
    for region in ctx.regions:
        ctx.check()
        key = f"iac:{region}"
        label = "IaC generator: template dalle risorse"
        info = scans.get(region) or {}
        folder = ctx.out / "templates" / region / "iac-generator"
        if not info.get("id"):
            ctx.step(key, region, label, S_KO, "", f"nessuna scansione: {info.get('origine', '?')}")
            ctx.section(key, S_KO, nota=info.get("origine"))
            continue
        try:
            scan = _wait_scan(ctx, region, info["id"])
            if scan.get("Status") != "COMPLETE":
                raise RuntimeError(f"scansione {scan.get('Status')}: {scan.get('StatusReason', '')}")
            res = ctx.aws.call("cloudformation", region, "list_resource_scan_resources",
                               {"ResourceScanId": info["id"]})
            found = res.get("Resources", [])
            write_json(folder / "scan-resources.json", {"scan": scan, "origine": info["origine"], "risorse": found})
            chosen, skipped = [], []
            managed = dup = 0
            for r in found:
                if r.get("ManagedByStack") and not cfg.include_managed_by_stack:
                    managed += 1
                    continue
                why = owned.reason(r, region)
                if why:
                    skipped.append({**r, "motivo": why})
                    continue
                if _is_global(r):
                    k = (r["ResourceType"], json.dumps(r["ResourceIdentifier"], sort_keys=True))
                    if k in seen:
                        dup += 1
                        continue
                    seen.add(k)
                chosen.append({"ResourceType": r["ResourceType"], "ResourceIdentifier": r["ResourceIdentifier"]})
            write_json(folder / "escluse.json", skipped)
            chosen.sort(key=lambda r: r["ResourceType"])
            size = max(1, min(500, cfg.max_resources_per_template))
            queue = [chosen[i:i + size] for i in range(0, len(chosen), size)]
            failed_res, written, lost, n = 0, 0, 0, 0
            while queue:
                ctx.check()
                chunk = queue.pop(0)
                n += 1
                name = f"scan-infra-{stamp}-{region}-{n:02d}"
                ctx.aws.call("cloudformation", region, "create_generated_template", {
                    "GeneratedTemplateName": name, "Resources": chunk,
                    "TemplateConfiguration": {"DeletionPolicy": "RETAIN", "UpdateReplacePolicy": "RETAIN"}},
                    paginate=False)
                try:
                    desc = _wait_template(ctx, region, name)
                    body = ctx.aws.call("cloudformation", region, "get_generated_template",
                                        {"GeneratedTemplateName": name, "Format": "YAML"}, paginate=False)
                    write_json(folder / f"{name}.describe.json", desc)
                    failed_res += (desc.get("Progress") or {}).get("ResourcesFailed") or 0
                    (folder / f"{name}.yaml").write_text(body.get("TemplateBody", ""), encoding="utf-8")
                    written += 1
                except Cancelled:
                    raise
                except Exception as exc:  # noqa: BLE001
                    text = classify(exc)[1]
                    if "size limit" in text.lower() and len(chunk) > 1:
                        half = len(chunk) // 2   # oltre 1 MB: si rifa' in due meta'
                        log.info("IaC generator %s: %s oltre 1 MB con %d risorse, divido in due",
                                 region, name, len(chunk))
                        queue[:0] = [chunk[:half], chunk[half:]]
                    else:
                        lost += len(chunk)
                        log.error("IaC generator %s: template %s non scaricato: %s", region, name, text)
                        write_json(folder / f"{name}.errore.json", {"errore": text, "risorse": chunk})
                finally:
                    if cfg.delete_generated_templates:
                        call_logged(ctx, region, "cloudformation", "delete_generated_template",
                                    {"GeneratedTemplateName": name})
            by_reason: dict[str, int] = {}
            for r in skipped:
                by_reason[r["motivo"]] = by_reason.get(r["motivo"], 0) + 1
            notes = [f"scansione {info['origine']}", f"{len(found)} trovate"]
            if managed:
                notes.append(f"{managed} gia' in uno stack")
            if skipped:
                notes.append(f"{len(skipped)} di AWS/di default escluse (escluse.json)")
            if dup:
                notes.append(f"{dup} globali gia' in un'altra region")
            if failed_res:
                notes.append(f"{failed_res} non generate (vedi .describe.json)")
            if lost:
                notes.append(f"{lost} risorse in template non scaricati")
            status = S_WARN if failed_res or lost else S_OK
            ctx.step(key, region, label, status, f"{len(chosen)} risorse in {written} template", "; ".join(notes))
            ctx.section(key, status, risorse=len(chosen), template=written, escluse_per_motivo=by_reason, note=notes)
        except Cancelled:
            raise
        except Exception as exc:  # noqa: BLE001
            text = classify(exc)[1] if hasattr(exc, "response") else f"{type(exc).__name__}: {exc}"
            log.error("IaC generator %s: %s", region, text)
            ctx.step(key, region, label, S_KO, "", text)
            ctx.section(key, S_KO, errore=text)
