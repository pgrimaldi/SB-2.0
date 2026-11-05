"""Infrastruttura: lo stack dell'ambiente dal suo template, sempre attraverso un change set.

Mai un aggiornamento diretto: prima si crea un change set (anteprima, nessuna risorsa toccata), lo si
legge, e solo dopo lo si applica o lo si scarta. Cosi' una sostituzione del DB o una cancellazione si
vedono prima di succedere. Se lo stack non esiste il change set e' di creazione.

Un template oltre i 51.200 byte CloudFormation non lo accetta nel corpo della richiesta: si carica nel
bucket di build dello stack (che quindi deve gia' esistere) e si passa l'URL.
"""
from __future__ import annotations

import logging
import threading
import time
from dataclasses import dataclass, field
from datetime import datetime

from botocore.exceptions import ClientError

from app.env.awsutil import (Aws, OperationError, Progress, StackInfo, check_cancel, no_progress, sleep, stack_info)
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)

PREFIX = "u22-"
MAX_TEMPLATE_BODY = 51_200
CAPABILITIES = ["CAPABILITY_NAMED_IAM", "CAPABILITY_IAM", "CAPABILITY_AUTO_EXPAND"]
DONE = ("CREATE_COMPLETE", "UPDATE_COMPLETE", "IMPORT_COMPLETE")


@dataclass
class Change:
    action: str                     # Add | Modify | Remove | Import | Dynamic
    logical_id: str
    resource_type: str
    replacement: str                # True | False | Conditional | ""
    details: str = ""

    @property
    def dangerous(self) -> bool:
        return self.action == "Remove" or self.replacement in ("True", "Conditional")


@dataclass
class ChangeSet:
    name: str
    kind: str                       # CREATE | UPDATE
    status: str
    reason: str = ""
    created: str = ""
    changes: list[Change] = field(default_factory=list)

    @property
    def dangerous(self) -> list[Change]:
        return [c for c in self.changes if c.dangerous]


def current(aws: Aws) -> StackInfo | None:
    """Lo stack, o None se non esiste."""
    try:
        return stack_info(aws)
    except OperationError as exc:
        if "non esiste" in str(exc):
            return None
        raise


def _template(aws: Aws, st: StackInfo | None) -> dict:
    """Argomento del template per CloudFormation: corpo, oppure URL se troppo grande."""
    path = aws.settings.template_path
    if not aws.settings.infra.template or not path.exists():
        raise OperationError(f"Template non trovato: {path} (ambienti.{aws.settings.name}.infra.template)")
    body = path.read_text(encoding="utf-8")
    if len(body.encode("utf-8")) <= MAX_TEMPLATE_BODY:
        return {"TemplateBody": body}
    if st is None or not st.outputs.get("BuildBucketName"):
        raise OperationError(f"Il template supera {MAX_TEMPLATE_BODY} byte e va caricato su S3, ma lo stack non ha "
                             f"ancora un bucket di build (output BuildBucketName): la prima creazione va fatta a mano")
    bucket = st.outputs["BuildBucketName"]
    key = f"template/{datetime.now():%Y%m%d-%H%M%S}-{path.name}"
    aws.client("s3").put_object(Bucket=bucket, Key=key, Body=body.encode("utf-8"), ContentType="text/yaml")
    region = aws.settings.aws.region
    log.info("Template di %d byte caricato in s3://%s/%s", len(body.encode("utf-8")), bucket, key)
    return {"TemplateURL": f"https://{bucket}.s3.{region}.amazonaws.com/{key}"}


def _parameters(aws: Aws, template: dict, existing: dict[str, str] | None) -> list[dict]:
    """Valori della configurazione; gli altri restano quelli in esercizio o il default del template."""
    declared = [p["ParameterKey"] for p in
                aws.client("cloudformation").validate_template(**template).get("Parameters", [])]
    wanted = aws.settings.infra.parametri
    unknown = [k for k in wanted if k not in declared]
    if unknown:
        raise OperationError(f"Parametri in configurazione che il template non dichiara: {', '.join(unknown)}")
    out = []
    for key in declared:
        if key in wanted:
            out.append({"ParameterKey": key, "ParameterValue": str(wanted[key])})
        elif existing is not None and key in existing:
            out.append({"ParameterKey": key, "UsePreviousValue": True})
    return out


def _describe(aws: Aws, name: str) -> ChangeSet:
    # DescribeChangeSet non dice il tipo: uno stack in REVIEW_IN_PROGRESS esiste solo come change set di creazione
    st = current(aws)
    kind = "CREATE" if st is None or st.status == "REVIEW_IN_PROGRESS" else "UPDATE"
    cfn = aws.client("cloudformation")
    cs: ChangeSet | None = None
    for page in cfn.get_paginator("describe_change_set").paginate(ChangeSetName=name, StackName=aws.settings.stack):
        if cs is None:
            created = page.get("CreationTime")
            cs = ChangeSet(page["ChangeSetName"], kind, page["Status"], page.get("StatusReason", ""),
                           created.strftime("%Y-%m-%d %H:%M") if created else "")
        for ch in page.get("Changes", []):
            rc = ch.get("ResourceChange", {})
            props = sorted({d.get("Target", {}).get("Name") or d.get("Target", {}).get("Attribute", "")
                            for d in rc.get("Details", [])} - {""})
            cs.changes.append(Change(rc.get("Action", ""), rc.get("LogicalResourceId", ""),
                                     rc.get("ResourceType", ""), rc.get("Replacement", ""), ", ".join(props)))
    if cs is None:
        raise OperationError(f"Change set {name} non trovato")
    return cs


def list_change_sets(aws: Aws) -> list[ChangeSet]:
    """Change set della utility in attesa sullo stack (quelli creati da altri non si toccano)."""
    if current(aws) is None:
        return []
    cfn = aws.client("cloudformation")
    names = [s["ChangeSetName"] for p in cfn.get_paginator("list_change_sets").paginate(StackName=aws.settings.stack)
             for s in p.get("Summaries", []) if s["ChangeSetName"].startswith(PREFIX)]
    return [_describe(aws, n) for n in names]


def preview(aws: Aws, cancel: threading.Event | None = None, progress: Progress = no_progress) -> ChangeSet:
    """Crea il change set e lo descrive. Non tocca nessuna risorsa."""
    st = current(aws)
    if st is not None and st.status.endswith("_IN_PROGRESS") and st.status != "REVIEW_IN_PROGRESS":
        raise OperationError(f"Lo stack e' in {st.status}: riprovare quando ha finito")
    if st is not None and st.status == "ROLLBACK_COMPLETE":
        raise OperationError("Lo stack e' in ROLLBACK_COMPLETE (creazione fallita): va eliminato a mano "
                             "dalla console prima di ricrearlo")
    kind = "UPDATE" if st is not None and st.status != "REVIEW_IN_PROGRESS" else "CREATE"
    template = _template(aws, st)
    params = _parameters(aws, template, st.parameters if kind == "UPDATE" else None)
    name = f"{PREFIX}{datetime.now():%Y%m%d-%H%M%S}"
    log.info("Change set %s (%s) dello stack %s dal template %s", name, kind, aws.settings.stack,
             aws.settings.template_path)
    cfn = aws.client("cloudformation")
    try:
        cfn.create_change_set(StackName=aws.settings.stack, ChangeSetName=name, ChangeSetType=kind,
                              Parameters=params, Capabilities=CAPABILITIES, Description="utility 22", **template)
        return _wait_change_set(aws, name, kind, cancel, progress)
    finally:
        if "TemplateURL" in template:
            # CloudFormation legge il template alla creazione del change set: la copia non serve piu'
            bucket, key = st.outputs["BuildBucketName"], template["TemplateURL"].split(".amazonaws.com/", 1)[1]
            aws.client("s3").delete_object(Bucket=bucket, Key=key)


def _wait_change_set(aws: Aws, name: str, kind: str, cancel, progress: Progress) -> ChangeSet:
    cfn = aws.client("cloudformation")
    started = time.monotonic()
    while True:
        sleep(5, cancel)
        cs = _describe(aws, name)
        progress("anteprima", 0, 0, f"{cs.status} ({int(time.monotonic() - started)} s)")
        if cs.status == "CREATE_COMPLETE":
            break
        if cs.status == "FAILED":
            if "didn't contain changes" in cs.reason or "No updates" in cs.reason:
                cfn.delete_change_set(ChangeSetName=name, StackName=aws.settings.stack)
                log.info("Nessuna modifica fra template e stack in esercizio")
                return ChangeSet(name="", kind=kind, status="NESSUNA_MODIFICA")
            raise OperationError(f"Change set fallito: {cs.reason}")
    log.info("Change set %s: %d modifiche, %d da guardare (sostituzioni o rimozioni)",
             name, len(cs.changes), len(cs.dangerous))
    for c in cs.dangerous:
        log.warning("  %s %s (%s) sostituzione=%s", c.action, c.logical_id, c.resource_type, c.replacement)
    return cs


def _wait(aws: Aws, started: float, cancel, progress: Progress) -> StackInfo:
    while True:
        sleep(15, cancel)
        st = stack_info(aws)
        progress("stack", 0, 0, f"{st.status} ({int(time.time() - started)} s)")
        if st.status in DONE:
            log.info("Stack %s: %s in %d s", st.name, st.status, time.time() - started)
            return st
        if not st.status.endswith("_IN_PROGRESS"):
            events = aws.client("cloudformation").describe_stack_events(StackName=st.name)["StackEvents"]
            reasons = [f"- {e['LogicalResourceId']}: {e['ResourceStatusReason']}" for e in events
                       if e["Timestamp"].timestamp() >= started and e.get("ResourceStatus", "").endswith("FAILED")
                       and e.get("ResourceStatusReason")][:10]
            raise OperationError(f"Stack {st.name} in {st.status}.\n" + "\n".join(reasons or ["(nessun motivo)"]))


def apply(aws: Aws, name: str, cancel: threading.Event | None = None, progress: Progress = no_progress) -> None:
    cs = _describe(aws, name)
    if cs.status != "CREATE_COMPLETE":
        raise OperationError(f"Il change set {name} e' in {cs.status}: non si puo' applicare")
    check_cancel(cancel)
    started = time.time()
    aws.client("cloudformation").execute_change_set(ChangeSetName=name, StackName=aws.settings.stack)
    log.info("Change set %s in esecuzione", name)
    # da qui Interrompi smette di aspettare ma non ferma CloudFormation: lo stack va avanti da solo
    _wait(aws, started, cancel, progress)
    if cs.kind == "CREATE" and aws.settings.infra.protezione_cancellazione:
        aws.client("cloudformation").update_termination_protection(
            StackName=aws.settings.stack, EnableTerminationProtection=True)
        log.info("Protezione dalla cancellazione attivata sullo stack %s", aws.settings.stack)


def discard(aws: Aws, name: str) -> None:
    try:
        aws.client("cloudformation").delete_change_set(ChangeSetName=name, StackName=aws.settings.stack)
    except ClientError as exc:
        raise OperationError(f"Change set {name}: {exc}") from exc
    log.info("Change set %s scartato", name)
