"""Artefatti scaricati: codice delle Lambda e dei layer, funzioni CloudFront, export OpenAPI di
API Gateway, user data delle EC2 e dei launch template, documenti SSM, definizioni Step Functions.

Gli URL prefirmati del codice Lambda durano 10 minuti: si chiedono subito prima di scaricare.
"""
from __future__ import annotations

import base64
import json
import logging

from app.scan.awsutil import GLOBAL, OK, safe_name
from app.scan.download import download, human
from app.scan.engine import S_OK, S_RUN, S_WARN, Ctx, call_logged, ensure, parallel
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)


def _qualifier_dir(q: str) -> str:
    return "LATEST" if q == "$LATEST" else q


def run_lambda(ctx: Ctx) -> None:
    cfg = ctx.settings.lambda_
    for region in ctx.regions:
        ctx.check()
        key = f"lambda:{region}"
        label = "Codice Lambda e layer"
        ctx.step(key, region, label, S_RUN, "", "")
        functions = [f for _, r in ensure(ctx, region, "lambda", "list_functions") for f in r.get("Functions", [])]
        jobs: list[tuple[str, str]] = []
        for f in functions:
            name = f["FunctionName"]
            quals = {"$LATEST"}
            if cfg.all_versions:
                src = ctx.result(region, "lambda", "list_versions_by_function")
                quals |= {v["Version"] for k, r in src if k == name for v in r.get("Versions", [])}
            else:
                src = ctx.result(region, "lambda", "list_aliases")
                quals |= {a["FunctionVersion"] for k, r in src if k == name for a in r.get("Aliases", [])}
            jobs += [(name, q) for q in sorted(quals)]
        base = ctx.out / "artifacts" / region / "lambda"

        def one_fn(job: tuple[str, str]) -> int:
            name, q = job
            outcome, res = call_logged(ctx, region, "lambda", "get_function", {"FunctionName": name, "Qualifier": q})
            if outcome != OK:
                raise RuntimeError(f"get_function: {outcome}")
            code, conf = res.get("Code", {}), res.get("Configuration", {})
            folder = base / "functions" / safe_name(name)
            if code.get("ImageUri"):
                folder.mkdir(parents=True, exist_ok=True)
                (folder / f"{_qualifier_dir(q)}.image.txt").write_text(
                    f"Immagine container: {code['ImageUri']}\nResolvedImageUri: {code.get('ResolvedImageUri', '')}\n"
                    "(scaricata nella sezione ECR se il repository e' di questo account)\n", encoding="utf-8")
                return 0
            if not code.get("Location"):
                raise RuntimeError(f"nessun URL del codice (RepositoryType {code.get('RepositoryType')})")
            return download(code["Location"], folder / f"{_qualifier_dir(q)}.zip",
                            sha256_b64=conf.get("CodeSha256"), cancel=ctx.cancel)

        res = parallel(ctx, one_fn, jobs, f"lambda {region}", "lambda")
        bad = sum(isinstance(r, Exception) for r in res)
        size = sum(r for r in res if isinstance(r, int))

        # layer: ultima versione di ognuno + quelle usate dalle funzioni di questo account
        layers: set[tuple[str, int]] = set()
        for _, r in ensure(ctx, region, "lambda", "list_layers"):
            for ly in r.get("Layers", []):
                if cfg.all_layer_versions:
                    for k, lv in ctx.result(region, "lambda", "list_layer_versions"):
                        if k == ly["LayerName"]:
                            layers |= {(ly["LayerName"], v["Version"]) for v in lv.get("LayerVersions", [])}
                latest = (ly.get("LatestMatchingVersion") or {}).get("Version")
                if latest:
                    layers.add((ly["LayerName"], latest))
        foreign = 0
        for f in functions:
            for ly in f.get("Layers", []):
                parts = ly["Arn"].split(":")   # arn:aws:lambda:region:account:layer:name:version
                if len(parts) == 8 and parts[4] == ctx.aws.account_id and parts[3] == region:
                    layers.add((parts[6], int(parts[7])))
                else:
                    foreign += 1

        def one_layer(job: tuple[str, int]) -> int:
            name, ver = job
            outcome, r = call_logged(ctx, region, "lambda", "get_layer_version",
                                     {"LayerName": name, "VersionNumber": ver})
            if outcome != OK:
                raise RuntimeError(f"get_layer_version: {outcome}")
            content = r.get("Content", {})
            return download(content["Location"], base / "layers" / safe_name(name) / f"{ver}.zip",
                            sha256_b64=content.get("CodeSha256"), cancel=ctx.cancel)

        lres = parallel(ctx, one_layer, sorted(layers), f"layer {region}", "lambda")
        lbad = sum(isinstance(r, Exception) for r in lres)
        size += sum(r for r in lres if isinstance(r, int))
        notes = []
        if bad:
            notes.append(f"{bad} funzioni non scaricate")
        if lbad:
            notes.append(f"{lbad} layer non scaricati")
        if foreign:
            notes.append(f"{foreign} riferimenti a layer di altri account/region (non scaricati)")
        status = S_WARN if bad or lbad else S_OK
        ctx.step(key, region, label, status, f"{len(jobs)} codici, {len(layers)} layer, {human(size)}", "; ".join(notes))
        ctx.section(key, status, codici=len(jobs), layer=len(layers), byte=size, note=notes)


def _b64_text(value: str) -> str:
    raw = base64.b64decode(value)
    try:
        return raw.decode("utf-8")
    except UnicodeDecodeError:
        return "(binario, base64)\n" + value


def run_other(ctx: Ctx) -> None:
    for scope in ctx.labels:
        ctx.check()
        key = f"other:{scope}"
        label = "Altri artefatti (CloudFront, API, user data, SSM)"
        ctx.step(key, scope, label, S_RUN, "", "")
        base = ctx.out / "artifacts" / scope
        written, bad = 0, 0

        def save(rel: str, text: str) -> None:
            nonlocal written
            p = base / rel
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(text, encoding="utf-8")
            written += 1

        if scope == GLOBAL:
            # funzioni CloudFront (codice JavaScript)
            for _, r in ctx.result(GLOBAL, "cloudfront", "list_functions"):
                for fn in (r.get("FunctionList") or {}).get("Items", []) or []:
                    for stage in ("LIVE", "DEVELOPMENT"):
                        outcome, res = call_logged(ctx, GLOBAL, "cloudfront", "get_function",
                                                   {"Name": fn["Name"], "Stage": stage})
                        if outcome == OK:
                            body = res.get("FunctionCode")
                            body = body.read() if hasattr(body, "read") else body
                            save(f"cloudfront-functions/{safe_name(fn['Name'])}.{stage.lower()}.js",
                                 body.decode("utf-8") if isinstance(body, bytes) else str(body))
        else:
            region = scope
            # API Gateway REST: export OpenAPI 3 con le estensioni API Gateway, per ogni stage
            for api_id, r in ctx.result(region, "apigateway", "get_stages"):
                for st in r.get("item", []):
                    outcome, res = call_logged(ctx, region, "apigateway", "get_export", {
                        "restApiId": api_id, "stageName": st["stageName"], "exportType": "oas30",
                        "parameters": {"extensions": "apigateway"}, "accepts": "application/json"})
                    if outcome == OK:
                        body = res["body"].read() if hasattr(res.get("body"), "read") else res.get("body", b"")
                        save(f"apigateway/{safe_name(api_id)}-{safe_name(st['stageName'])}.oas30.json",
                             body.decode("utf-8") if isinstance(body, bytes) else str(body))
                    else:
                        bad += 1
            # API Gateway HTTP (v2): export OpenAPI 3
            for _, r in ctx.result(region, "apigatewayv2", "get_apis"):
                for api in r.get("Items", []):
                    if api.get("ProtocolType") != "HTTP":
                        continue
                    outcome, res = call_logged(ctx, region, "apigatewayv2", "export_api", {
                        "ApiId": api["ApiId"], "OutputType": "JSON", "Specification": "OAS30",
                        "IncludeExtensions": True})
                    if outcome == OK:
                        body = res["body"].read() if hasattr(res.get("body"), "read") else res.get("body", b"")
                        save(f"apigatewayv2/{safe_name(api['ApiId'])}.oas30.json",
                             body.decode("utf-8") if isinstance(body, bytes) else str(body))
                    else:
                        bad += 1
            # user data delle istanze EC2 e dei launch template
            for inst, r in ctx.result(region, "ec2", "describe_instance_attribute"):
                v = (r.get("UserData") or {}).get("Value")
                if v:
                    save(f"ec2-userdata/{safe_name(inst)}.txt", _b64_text(v))
            for lt, r in ctx.result(region, "ec2", "describe_launch_template_versions"):
                for v in r.get("LaunchTemplateVersions", []):
                    ud = (v.get("LaunchTemplateData") or {}).get("UserData")
                    if ud:
                        save(f"ec2-userdata/{safe_name(lt)}-v{v['VersionNumber']}.txt", _b64_text(ud))
            # documenti SSM propri
            for name, r in ctx.result(region, "ssm", "get_document"):
                fmt = (r.get("DocumentFormat") or "JSON").lower()
                save(f"ssm-documents/{safe_name(name)}.{fmt}", r.get("Content") or "")
            # Step Functions
            for arn, r in ctx.result(region, "stepfunctions", "describe_state_machine"):
                try:
                    text = json.dumps(json.loads(r.get("definition") or "{}"), indent=2, ensure_ascii=False)
                except ValueError:
                    text = r.get("definition") or ""
                save(f"stepfunctions/{safe_name(r.get('name') or arn)}.asl.json", text)
        status = S_WARN if bad else S_OK
        ctx.step(key, scope, label, status, f"{written} file", f"{bad} export non riusciti" if bad else "")
        ctx.section(key, status, file=written, errori=bad)


__all__ = ["run_lambda", "run_other", "parallel"]
