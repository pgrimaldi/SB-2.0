"""Accesso ad AWS: sessione dal profilo, client, stack CloudFormation dell'ambiente.

Lo stack si aggiorna sempre con il template gia' pubblicato (UsePreviousTemplate) e cambiando solo
i parametri indicati: la utility non porta mai sull'ambiente modifiche di infrastruttura.
"""
from __future__ import annotations

import logging
import threading
import time
import urllib.error
import urllib.request
from dataclasses import dataclass, field
from typing import Callable

import boto3
from botocore.config import Config
from botocore.exceptions import ClientError

from app.logging_setup import LOGGER_NAME
from app.settings import EnvSettings as Settings

log = logging.getLogger(LOGGER_NAME)

Progress = Callable[[str, float, float, str], None]


class Cancelled(Exception):
    pass


class OperationError(Exception):
    pass


def no_progress(phase: str, done: float, total: float, text: str) -> None:
    pass


def check_cancel(cancel: threading.Event | None) -> None:
    if cancel is not None and cancel.is_set():
        raise Cancelled()


def sleep(seconds: float, cancel: threading.Event | None) -> None:
    end = time.monotonic() + seconds
    while True:
        check_cancel(cancel)
        remaining = end - time.monotonic()
        if remaining <= 0:
            return
        time.sleep(min(1.0, remaining))


class Aws:
    """Client per servizio, creati sotto lock (la sessione boto3 non e' thread-safe)."""

    def __init__(self, settings: Settings):
        self.settings = settings
        self._session = boto3.Session(profile_name=settings.aws.profile or None,
                                      region_name=settings.aws.region)
        self._clients: dict[str, object] = {}
        self._lock = threading.Lock()
        self._config = Config(retries={"mode": "adaptive", "max_attempts": 8},
                              connect_timeout=10, read_timeout=120)

    def client(self, service: str):
        with self._lock:
            if service not in self._clients:
                self._clients[service] = self._session.client(service, config=self._config)
            return self._clients[service]

    @property
    def session(self) -> boto3.Session:
        return self._session

    def identity(self) -> dict:
        return self.client("sts").get_caller_identity()

    def actor(self) -> str:
        return self.identity()["Arn"]

    def check_account(self) -> dict:
        """Identita' del chiamante; rifiuta un account diverso da quello atteso."""
        ident = self.identity()
        expected = self.settings.account_atteso
        if expected and ident["Account"] != expected:
            raise OperationError(f"Il profilo {self.settings.aws.profile!r} e' sull'account {ident['Account']}, "
                                 f"non su {expected} (account_atteso): la utility non parte.")
        return ident


@dataclass
class StackInfo:
    name: str
    status: str
    outputs: dict[str, str] = field(default_factory=dict)
    parameters: dict[str, str] = field(default_factory=dict)

    @property
    def has_db(self) -> bool:
        return self.parameters.get("DatabaseEnabled", "true") == "true"

    @property
    def image_tag(self) -> str:
        return self.parameters.get("ImageTag", "")

    def need(self, *names: str) -> list[str]:
        """Valori degli output richiesti; errore chiaro se lo stack non li espone."""
        missing = [n for n in names if not self.outputs.get(n)]
        if missing:
            raise OperationError(f"Lo stack {self.name} non espone gli output {', '.join(missing)}: la funzione "
                                 f"richiede lo stesso contratto di output di sb-dev (infra/dev/sb-dev.yaml).")
        return [self.outputs[n] for n in names]


def stack_info(aws: Aws) -> StackInfo:
    try:
        st = aws.client("cloudformation").describe_stacks(StackName=aws.settings.stack)["Stacks"][0]
    except ClientError as exc:
        if "does not exist" in str(exc):
            raise OperationError(f"Lo stack {aws.settings.stack} dell'ambiente {aws.settings.name} non esiste "
                                 f"(ancora): va creato prima di usare questa funzione.") from exc
        raise
    return StackInfo(
        name=st["StackName"], status=st["StackStatus"],
        outputs={o["OutputKey"]: o["OutputValue"] for o in st.get("Outputs", [])},
        parameters={p["ParameterKey"]: p.get("ParameterValue", "") for p in st.get("Parameters", [])},
    )


def update_stack_parameters(aws: Aws, changes: dict[str, str], cancel: threading.Event | None,
                            progress: Progress = no_progress) -> StackInfo:
    """Cambia solo i parametri in `changes`, con il template gia' pubblicato, e aspetta la fine."""
    cfn = aws.client("cloudformation")
    info = stack_info(aws)
    if info.status.endswith("_IN_PROGRESS"):
        raise OperationError(f"Lo stack e' in {info.status}: riprovare quando ha finito.")
    params = []
    for key in info.parameters:
        if key in changes:
            params.append({"ParameterKey": key, "ParameterValue": changes[key]})
        else:
            params.append({"ParameterKey": key, "UsePreviousValue": True})
    log.info("Stack %s: aggiorno %s", info.name, ", ".join(f"{k}={v!r}" for k, v in changes.items()))
    started = time.time()
    try:
        cfn.update_stack(StackName=info.name, UsePreviousTemplate=True, Parameters=params,
                         Capabilities=["CAPABILITY_NAMED_IAM", "CAPABILITY_IAM"])
    except ClientError as exc:
        if "No updates are to be performed" in str(exc):
            log.info("Stack gia' con questi valori: niente da fare")
            return info
        raise
    while True:
        sleep(15, cancel)
        st = stack_info(aws)
        progress("stack", 0, 0, f"{st.status} ({int(time.time() - started)} s)")
        if st.status == "UPDATE_COMPLETE":
            log.info("Stack aggiornato in %d s", time.time() - started)
            return st
        if not st.status.endswith("_IN_PROGRESS"):
            reasons = _failure_reasons(aws, started)
            raise OperationError(f"Aggiornamento dello stack fallito ({st.status}).\n" + "\n".join(reasons))


def _failure_reasons(aws: Aws, since: float) -> list[str]:
    events = aws.client("cloudformation").describe_stack_events(StackName=aws.settings.stack)["StackEvents"]
    out = []
    for ev in events:
        if ev["Timestamp"].timestamp() < since:
            break
        if ev.get("ResourceStatus", "").endswith("FAILED") and ev.get("ResourceStatusReason"):
            out.append(f"- {ev['LogicalResourceId']}: {ev['ResourceStatusReason']}")
    return out[:10] or ["(nessun motivo negli eventi dello stack)"]


def http_get(url: str, timeout: int = 15) -> tuple[int, str]:
    req = urllib.request.Request(url, headers={"User-Agent": "utility22"})
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:  # noqa: S310 - URL dello stack
            return resp.status, resp.read(2000).decode("utf-8", "replace")
    except urllib.error.HTTPError as exc:
        return exc.code, ""
    except (urllib.error.URLError, TimeoutError, OSError) as exc:
        return 0, str(exc)


def my_public_ip() -> str:
    status, body = http_get("https://checkip.amazonaws.com", timeout=10)
    ip = body.strip()
    if status != 200 or not ip:
        raise OperationError(f"Impossibile ricavare l'IP pubblico di questa postazione ({status} {ip})")
    return ip
