"""Orchestrazione di una scansione completa.

Ordine: account e region -> avvio delle scansioni IaC generator (lavorano in background) ->
configurazioni generiche -> dettaglio per risorsa -> IAM -> S3 -> segreti -> template degli stack
-> codice Lambda -> immagini ECR -> altri artefatti -> attesa e download dei template IaC ->
manifesto e riepilogo.

Si scrive in `<account>_<data>.partial/`, rinominata a fine corsa: una scansione interrotta resta
riconoscibile. Una sezione che fallisce non ferma le altre: l'errore va nel manifesto.
"""
from __future__ import annotations

import logging
import threading
import time
from dataclasses import asdict
from datetime import datetime, timezone
from pathlib import Path

from app.scan import regions as regmod
from app.scan.awsutil import (ABSENT, DENIED, EMPTY, ERROR, GLOBAL, OK, UNAVAILABLE, Aws, Cancelled,
                         classify, write_json)
from app.scan.engine import S_KO, S_OK, S_RUN, S_SKIP, S_WARN, Ctx, ProgressFn, StepFn, run_generic, run_rules
from app.logging_setup import LOGGER_NAME
from app.scan.sec_artifacts import run_lambda, run_other
from app.scan.sec_ecr import run_ecr
from app.scan.sec_iam_s3 import run_iam, run_s3
from app.scan.sec_secrets import run_secrets
from app.scan.sec_templates import finish_scans, run_stack_templates, start_scans
from app.scan.settings import Settings

log = logging.getLogger(LOGGER_NAME)

SECTION_LABELS = {
    "json": "Configurazioni (List/Describe/Get)",
    "details": "Dettaglio per risorsa",
    "iam": "IAM: dump completo e credential report",
    "s3": "S3: configurazione dei bucket",
    "secrets": "Segreti (valori in chiaro)",
    "stack_templates": "Template degli stack CloudFormation",
    "iac_generator": "IaC generator: template dalle risorse",
    "lambda_code": "Codice Lambda e layer",
    "ecr_images": "Immagini ECR (OCI)",
    "other_artifacts": "Altri artefatti (CloudFront, API, user data, SSM)",
}


def check_access(settings: Settings, progress: ProgressFn | None = None,
                 regions_override: list[str] | None = None) -> tuple[Aws, dict, list[str], dict[str, str]]:
    """Credenziali + region: (aws, identita', region, motivi)."""
    aws = Aws(settings.aws)
    ident = aws.identify()
    log.info("Account %s - %s", ident["Account"], ident["Arn"])
    regions, reasons = regmod.resolve(aws, settings, regions_override, progress)
    for r in regions:
        log.info("Region %s: %s", r, reasons[r])
    return aws, ident, regions, reasons


def run_scan(settings: Settings, cancel: threading.Event, progress: ProgressFn, step: StepFn,
             regions_override: list[str] | None = None) -> Path:
    started = datetime.now(timezone.utc)
    t0 = time.monotonic()
    step("account", "-", "Account e region", S_RUN, "", "")
    aws, ident, regions, reasons = check_access(settings, progress, regions_override)
    if not regions:
        raise RuntimeError("Nessuna region da leggere")
    step("account", "-", "Account e region", S_OK, f"{len(regions)} region",
         f"{ident['Account']} - {', '.join(regions)}")

    settings.output_path.mkdir(parents=True, exist_ok=True)
    final = settings.output_path / f"{ident['Account']}_{datetime.now():%Y%m%d_%H%M%S}"
    out = final.with_name(final.name + ".partial")
    out.mkdir(parents=True)
    log.info("Cartella della scansione: %s", out)
    ctx = Ctx(settings=settings, aws=aws, out=out, cancel=cancel, progress=progress, step=step, regions=regions)

    on = settings.on
    for name, label in SECTION_LABELS.items():
        if not on(name):
            step(f"skip:{name}", "-", label, S_SKIP, "", "disattivata")
    if on("other_artifacts") and not on("details"):
        log.warning("«Altri artefatti» usa i risultati del dettaglio per risorsa, disattivato: usciranno "
                    "solo le funzioni CloudFront")

    scans: dict = {}
    plan = [
        ("iac_generator", lambda: scans.update(start_scans(ctx))),
        ("json", lambda: run_generic(ctx)),
        ("details", lambda: run_rules(ctx)),
        ("iam", lambda: run_iam(ctx)),
        ("s3", lambda: run_s3(ctx)),
        ("secrets", lambda: run_secrets(ctx)),
        ("stack_templates", lambda: run_stack_templates(ctx)),
        ("lambda_code", lambda: run_lambda(ctx)),
        ("ecr_images", lambda: run_ecr(ctx)),
        ("other_artifacts", lambda: run_other(ctx)),
        ("iac_generator", lambda: finish_scans(ctx, scans)),
    ]
    cancelled = False
    try:
        for name, fn in plan:
            if not on(name):
                continue
            ctx.check()
            t = time.monotonic()
            try:
                fn()
            except Cancelled:
                raise
            except Exception as exc:  # noqa: BLE001 - una sezione rotta non ferma le altre
                text = classify(exc)[1] if hasattr(exc, "response") else f"{type(exc).__name__}: {exc}"
                log.exception("Sezione %s: %s", name, text)
                step(f"err:{name}", "-", SECTION_LABELS[name], S_KO, "", text)
                ctx.section(name, S_KO, errore=text)
            log.info("Sezione %s: %.0f s", name, time.monotonic() - t)
    except Cancelled:
        cancelled = True
        log.warning("Scansione interrotta: la cartella resta %s", out)

    manifest = _manifest(ctx, ident, reasons, started, time.monotonic() - t0, cancelled)
    write_json(out / "manifest.json", manifest)
    (out / "riepilogo.md").write_text(_summary(ctx, manifest), encoding="utf-8")
    if cancelled:
        raise Cancelled()
    out.rename(final)
    log.info("Scansione completata in %.0f s: %s", time.monotonic() - t0, final)
    return final


def _manifest(ctx: Ctx, ident: dict, reasons: dict, started: datetime, seconds: float, cancelled: bool) -> dict:
    by_service: dict[str, dict[str, dict[str, int]]] = {}
    for (region, service, outcome), n in ctx.stats.counts.items():
        by_service.setdefault(region, {}).setdefault(service, {})[outcome] = n
    s = ctx.settings
    return {
        "utility": "12-scan-infrastruttura",
        "account": ident["Account"],
        "chiamante": ident["Arn"],
        "profilo": s.aws.profile,
        "iniziata_utc": started.isoformat(timespec="seconds"),
        "durata_secondi": round(seconds),
        "interrotta": cancelled,
        "region": reasons,
        "sezioni_attive": [k for k, v in s.sections.items() if v],
        "sezioni": ctx.sections,
        "chiamate": {o: ctx.stats.total(o) for o in (OK, EMPTY, ABSENT, UNAVAILABLE, DENIED, ERROR)},
        "chiamate_per_servizio": by_service,
        "problemi": ctx.stats.problems,
        "impostazioni": {"iac": asdict(s.iac), "lambda": asdict(s.lambda_), "ecr": asdict(s.ecr),
                         "servizi": s.services},
    }


def _summary(ctx: Ctx, m: dict) -> str:
    lines = [f"# Scansione infrastruttura AWS - account {m['account']}", "",
             f"- Iniziata: {m['iniziata_utc']} UTC, durata {m['durata_secondi'] // 60} min {m['durata_secondi'] % 60} s"
             + (" - **INTERROTTA**" if m["interrotta"] else ""),
             f"- Chiamante: `{m['chiamante']}` (profilo `{m['profilo']}`)",
             "- Region: " + ", ".join(f"**{r}** ({why})" for r, why in m["region"].items()), "",
             "## Sezioni", "", "| Sezione | Stato | Dettagli |", "|---|---|---|"]
    for name, info in sorted(m["sezioni"].items()):
        extra = ", ".join(f"{k}: {v}" for k, v in info.items() if k != "stato" and v not in (None, [], ""))
        lines.append(f"| {name} | {info['stato']} | {extra.replace('|', '/')[:400]} |")
    c = m["chiamate"]
    lines += ["", "## Chiamate API", "",
              f"{c[OK]} con dati, {c[EMPTY]} vuote, {c[ABSENT]} «non configurato», {c[UNAVAILABLE]} non disponibili "
              f"nella region o non applicabili, **{c[DENIED]} negate**, **{c[ERROR]} in errore**.", ""]
    if m["problemi"]:
        lines += ["### Negate e in errore", "", "| Ambito | Servizio | Operazione | Esito | Messaggio |",
                  "|---|---|---|---|---|"]
        for p in sorted(m["problemi"], key=lambda p: (p["esito"], p["region"], p["service"], p["op"]))[:400]:
            lines.append(f"| {p['region']} | {p['service']} | {p['op']} | {p['esito']} | "
                         f"{p['messaggio'].replace('|', '/')[:200]} |")
        if len(m["problemi"]) > 400:
            lines.append(f"| … | | | | altri {len(m['problemi']) - 400} nel manifest.json |")
        lines.append("")
    lines += ["## Contenuto della cartella", "",
              "| Percorso | Cosa contiene |", "|---|---|",
              "| `json/<ambito>/<servizio>/<operazione>.json` | risposte delle API (`global` = servizi globali) |",
              "| `json/global/iam/get_account_authorization_details.json` | dump completo di IAM |",
              "| `json/global/s3/buckets/` | configurazione di ogni bucket |",
              "| `iam/credential-report.csv` | credential report |",
              "| `secrets/` | **segreti in chiaro** |",
              "| `templates/<region>/stacks/` | template degli stack esistenti |",
              "| `templates/<region>/iac-generator/` | template generati dalle risorse non in uno stack |",
              "| `artifacts/<region>/lambda/` | codice delle funzioni e dei layer |",
              "| `artifacts/<region>/ecr/` | immagini container (OCI image layout) |",
              "| `artifacts/...` | funzioni CloudFront, export OpenAPI, user data, documenti SSM, Step Functions |",
              "", "Esclusi per scelta: righe dei database, oggetti S3, contenuto dei log, messaggi delle code.", ""]
    return "\n".join(lines)


__all__ = ["SECTION_LABELS", "check_access", "run_scan", "GLOBAL"]
