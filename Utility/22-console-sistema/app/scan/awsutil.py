"""Accesso ad AWS: sessione dal profilo, client per (servizio, region), chiamate con paginazione,
classificazione degli errori e scrittura JSON.

I client boto3 sono thread-safe, la sessione no: i client si creano sotto lock e si riusano.
"""
from __future__ import annotations

import base64
import datetime as dt
import json
import logging
import threading
from dataclasses import dataclass, field
from decimal import Decimal
from pathlib import Path
from typing import Any

import boto3
from botocore.config import Config
from botocore.exceptions import (BotoCoreError, ClientError, EndpointConnectionError,
                                 NoCredentialsError, ParamValidationError, ProfileNotFound)
from botocore.response import StreamingBody

from app.logging_setup import LOGGER_NAME
from app.scan.settings import AwsSettings

log = logging.getLogger(LOGGER_NAME)

GLOBAL = "global"
GLOBAL_REGION = "us-east-1"   # endpoint dei servizi globali (IAM, CloudFront, Route 53, Organizations)


class Cancelled(Exception):
    pass


# Esiti di una chiamata
OK, EMPTY, ABSENT, DENIED, UNAVAILABLE, ERROR = "ok", "vuoto", "assente", "negato", "non disponibile", "errore"

_UNAVAILABLE_CODES = {
    "OptInRequired", "SubscriptionRequiredException", "UnsupportedOperation", "InvalidAction",
    "AWSOrganizationsNotInUseException", "UnknownOperationException", "NotSubscribed",
    "UnsupportedOperationException", "InvalidClientTokenId", "BadRequestException",
    "NoLongerSupportedException", "OperationNotSupportedException", "ServiceUnavailableException",
    "BadRequest", "InvalidRequestException", "NotAuthorizedException", "OrganizationAccessDeniedException",
}


def classify(exc: Exception) -> tuple[str, str]:
    """(esito, testo breve) per un'eccezione di una chiamata API."""
    if isinstance(exc, ClientError):
        err = exc.response.get("Error", {})
        code, msg = err.get("Code", ""), err.get("Message", "") or ""
        text = f"{code}: {msg}"[:300]
        low = msg.lower()
        if code.startswith("NoSuch") or "NotFound" in code or code in (
                "ResourceNotFoundException", "NoSuchEntity", "DBParameterGroupNotFound"):
            return ABSENT, text
        if "is not authorized to perform" in low:
            return DENIED, text   # rifiuto IAM vero: prima delle frasi «funzione non attiva»
        if "organization" in low or "delegated" in low or "isn't enabled" in low or "is disabled" in low \
                or "currently disabled" in low or "not supported" in low or "only available in" in low \
                or code.endswith(".Disabled") or code == "OperationNotPermitted":
            return UNAVAILABLE, text   # account fuori da Organizations, funzione non attivata, region sbagliata
        if "AccessDenied" in code or "Unauthorized" in code or code in ("AuthorizationError", "Forbidden"):
            return DENIED, text
        if code in _UNAVAILABLE_CODES or "not supported" in low or "not available" in low \
                or "not enabled" in low or "not subscribed" in low \
                or "opt-in" in low or "not opted in" in low:
            return UNAVAILABLE, text
        return ERROR, text
    if isinstance(exc, EndpointConnectionError):
        return UNAVAILABLE, "endpoint non raggiungibile nella region"
    if isinstance(exc, ParamValidationError):
        return ERROR, f"parametri: {exc}"[:300]
    return ERROR, f"{type(exc).__name__}: {exc}"[:300]


@dataclass
class CallStats:
    """Conteggio degli esiti per (region, servizio): finisce nel manifesto e nel riepilogo."""
    counts: dict[tuple[str, str, str], int] = field(default_factory=dict)
    problems: list[dict] = field(default_factory=list)
    lock: threading.Lock = field(default_factory=threading.Lock)

    def add(self, region: str, service: str, op: str, outcome: str, text: str = "") -> None:
        with self.lock:
            k = (region, service, outcome)
            self.counts[k] = self.counts.get(k, 0) + 1
            if outcome in (DENIED, ERROR):
                self.problems.append({"region": region, "service": service, "op": op,
                                      "esito": outcome, "messaggio": text})

    def total(self, outcome: str) -> int:
        with self.lock:
            return sum(n for (_, _, o), n in self.counts.items() if o == outcome)


class Aws:
    """Sessione + cache dei client."""

    def __init__(self, cfg: AwsSettings):
        self.cfg = cfg
        try:
            self.session = boto3.session.Session(profile_name=cfg.profile or None,
                                                 region_name=cfg.home_region)
        except ProfileNotFound as exc:
            raise RuntimeError(f"Profilo AWS «{cfg.profile}» non trovato: crearlo con "
                               f"`aws configure --profile {cfg.profile}`") from exc
        self._config = Config(retries={"max_attempts": cfg.max_attempts, "mode": "adaptive"},
                              connect_timeout=cfg.connect_timeout, read_timeout=cfg.read_timeout,
                              max_pool_connections=max(10, cfg.max_workers * 2))
        self._clients: dict[tuple[str, str], Any] = {}
        self._lock = threading.Lock()
        self.account_id = ""
        self.caller_arn = ""
        self._available: dict[str, set[str]] = {}

    def client(self, service: str, region: str):
        real = GLOBAL_REGION if region == GLOBAL else region
        key = (service, real)
        with self._lock:
            c = self._clients.get(key)
            if c is None:
                c = self.session.client(service, region_name=real, config=self._config)
                self._clients[key] = c
            return c

    def identify(self) -> dict:
        try:
            ident = self.client("sts", self.cfg.home_region).get_caller_identity()
        except NoCredentialsError as exc:
            raise RuntimeError("Nessuna credenziale AWS: configurare il profilo indicato in "
                               "config/settings.local.json (aws.profile)") from exc
        self.account_id, self.caller_arn = ident["Account"], ident["Arn"]
        return ident

    def service_in_region(self, service: str, region: str) -> bool:
        if region == GLOBAL:
            return True
        with self._lock:
            av = self._available.get(service)
            if av is None:
                try:
                    av = set(self.session.get_available_regions(service))
                except Exception:  # noqa: BLE001
                    av = set()
                self._available[service] = av
        # elenco vuoto = botocore non lo sa (servizio nuovo): si prova comunque
        return not av or region in av

    def call(self, service: str, region: str, op: str, kwargs: dict | None = None,
             paginate: bool = True) -> dict:
        """Chiama `op` (nome snake_case) e, se possibile, unisce tutte le pagine."""
        c = self.client(service, region)
        kwargs = kwargs or {}
        if paginate and c.can_paginate(op):
            res = c.get_paginator(op).paginate(**kwargs).build_full_result()
        else:
            res = getattr(c, op)(**kwargs)
        res.pop("ResponseMetadata", None)
        return res


def is_empty(value: Any) -> bool:
    """Vero se la risposta non contiene niente (tolti contatori e token)."""
    if value is None:
        return True
    if isinstance(value, dict):
        rest = {k: v for k, v in value.items()
                if k not in ("ResponseMetadata", "NextToken", "nextToken", "Marker", "IsTruncated",
                             "MaxItems", "MaxResults", "Quantity", "Count", "ScannedCount")}
        return all(is_empty(v) for v in rest.values())
    if isinstance(value, (list, tuple, set)):
        return len(value) == 0
    if isinstance(value, str):
        return value == ""
    return False


def count_items(value: Any) -> int:
    """Numero di elementi delle liste di primo e secondo livello: misura grezza per il riepilogo."""
    if isinstance(value, list):
        return len(value)
    if isinstance(value, dict):
        n = sum(len(v) for v in value.values() if isinstance(v, list))
        if n:
            return n
        return sum(len(v[k]) for v in value.values() if isinstance(v, dict)
                   for k in v if isinstance(v[k], list))
    return 0


def _default(o: Any):
    if isinstance(o, (dt.datetime, dt.date)):
        return o.isoformat()
    if isinstance(o, Decimal):
        return int(o) if o == o.to_integral_value() else float(o)
    if isinstance(o, (bytes, bytearray)):
        try:
            return o.decode("utf-8")
        except UnicodeDecodeError:
            return {"__base64__": base64.b64encode(o).decode("ascii")}
    if isinstance(o, StreamingBody):
        return _default(o.read())
    if isinstance(o, set):
        return sorted(o)
    return str(o)


def write_json(path: Path, data: Any) -> int:
    path.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(data, default=_default, ensure_ascii=False, indent=2)
    path.write_text(text, encoding="utf-8")
    return len(text)


def to_jsonable(data: Any) -> Any:
    return json.loads(json.dumps(data, default=_default, ensure_ascii=False))


def safe_name(text: str, limit: int = 120) -> str:
    """Nome di file Windows-safe da un id/ARN."""
    out = "".join(ch if ch.isalnum() or ch in "-_.=+@" else "_" for ch in str(text))
    out = out.strip("._") or "_"
    return out[:limit]


__all__ = ["Aws", "BotoCoreError", "Cancelled", "CallStats", "ClientError", "GLOBAL", "OK", "EMPTY",
           "ABSENT", "DENIED", "UNAVAILABLE", "ERROR", "classify", "count_items", "is_empty",
           "safe_name", "to_jsonable", "write_json"]
