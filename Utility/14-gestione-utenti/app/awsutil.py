"""Accesso ad AWS: sessione dal profilo, client, controllo dell'account."""
from __future__ import annotations

import logging
import threading
from typing import Callable

import boto3
from botocore.config import Config

from app.logging_setup import LOGGER_NAME
from app.settings import Settings

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


class Aws:
    """Client per servizio, creati sotto lock (la sessione boto3 non e' thread-safe)."""

    def __init__(self, settings: Settings):
        self.settings = settings
        self._session = boto3.Session(profile_name=settings.aws.profile or None,
                                      region_name=settings.aws.region)
        self._clients: dict[str, object] = {}
        self._lock = threading.Lock()
        self._config = Config(retries={"mode": "adaptive", "max_attempts": 8},
                              connect_timeout=10, read_timeout=60)
        self._identity: dict | None = None

    def client(self, service: str):
        with self._lock:
            if service not in self._clients:
                self._clients[service] = self._session.client(service, config=self._config)
            return self._clients[service]

    @property
    def iam(self):
        return self.client("iam")

    def identity(self) -> dict:
        if self._identity is None:
            self._identity = self.client("sts").get_caller_identity()
        return self._identity

    @property
    def account(self) -> str:
        return self.identity()["Account"]

    def actor(self) -> str:
        """Nome dell'utente IAM che usa la utility (vuoto se non e' un utente IAM)."""
        arn = self.identity()["Arn"]
        return arn.split(":user/", 1)[1].rsplit("/", 1)[-1] if ":user/" in arn else ""

    def check_account(self) -> dict:
        """Rifiuta di lavorare su un account diverso da quello atteso."""
        ident = self.identity()
        expected = self.settings.account_atteso.strip()
        if expected and ident["Account"] != expected:
            raise OperationError(f"Il profilo '{self.settings.aws.profile}' appartiene all'account "
                                 f"{ident['Account']}, non a {expected} (account_atteso).")
        if ident["Arn"].endswith(":root"):
            raise OperationError("Il profilo usa l'utente root: usare un utente amministratore.")
        return ident
