"""Ruoli dell'utente (gruppi IAM della utility 14) e funzioni che abilitano.

I ruoli si leggono dai gruppi IAM dell'utente del profilo: `sb-<ambiente>-ruolo-<nome>` per i ruoli
di un ambiente, `sb-ruolo-<nome>` per quelli dell'intero account. Il permesso vero lo decide IAM: qui
si decide solo cosa mostrare attivo, perche' un pulsante che finirebbe in AccessDenied non serve.

L'utente amministratore creato a mano (es. `michele`) non ha gruppi ma `AdministratorAccess`
agganciata direttamente: conta come `admin`.
"""
from __future__ import annotations

import logging
from dataclasses import dataclass, field

from botocore.exceptions import ClientError

from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)

ACCOUNT_PREFIX = "sb-ruolo-"
ADMIN_POLICIES = ("arn:aws:iam::aws:policy/AdministratorAccess",)


@dataclass(frozen=True)
class Feature:
    key: str
    label: str
    env_roles: tuple[str, ...] = ()        # ruoli dell'ambiente scelto
    account_roles: tuple[str, ...] = ()    # ruoli sull'intero account (admin vale sempre)
    only_env: tuple[str, ...] = ()         # ambienti in cui esiste (vuoto = tutti)


FEATURES: dict[str, Feature] = {f.key: f for f in (
    Feature("stato", "Stato dell'ambiente", ("frontend", "backend", "database", "orario", "log", "file"),
            ("lettura", "infrastruttura")),
    Feature("infra", "Infrastruttura: change set dello stack", (), ("infrastruttura",)),
    Feature("orario", "Accensione e spegnimento", ("orario", "backend", "database"), only_env=("dev",)),
    Feature("deploy_backend", "Deploy del backend", ("backend",)),
    Feature("deploy_frontend", "Deploy dei frontend", ("frontend",)),
    Feature("db_backup", "Backup del DB", ("database",)),
    Feature("db_gestione", "Crea / carica / svuota / elimina il DB", ("database",), only_env=("dev",)),
    Feature("db_accesso", "Gestione DB: connessione, porte, panoramica, sessioni, query in lettura", ("backend", "database")),
    Feature("db_amministra", "Gestione DB: chiusura sessioni, snapshot manuali, log di PostgreSQL", ("database",)),
    Feature("retention", "Retention: prova ed esecuzione delle policy", ("database",)),
    Feature("file_lettura", "File S3: elenco e scarico", ("file",)),
    Feature("file_scrittura", "File S3: caricamento e cancellazione", ("file",)),
    Feature("archivi", "Dati archiviati dalla retention", ("file",)),
    Feature("log", "Log del backend", ("log", "backend"), ("infrastruttura",)),
    Feature("scan", "Scansione dell'infrastruttura", (), ("infrastruttura",)),
    Feature("costi", "Costi dell'account", (), ("costi",)),
)}


@dataclass
class Identity:
    account: str
    arn: str
    user: str | None
    groups: set[str] = field(default_factory=set)
    admin_direct: bool = False
    note: str = ""

    @property
    def is_admin(self) -> bool:
        return self.admin_direct or f"{ACCOUNT_PREFIX}admin" in self.groups

    def env_roles(self, prefix: str) -> set[str]:
        return {g[len(prefix):] for g in self.groups if prefix and g.startswith(prefix)}

    def account_roles(self) -> set[str]:
        roles = {g[len(ACCOUNT_PREFIX):] for g in self.groups if g.startswith(ACCOUNT_PREFIX)}
        if self.admin_direct:
            roles.add("admin")
        return roles - {"base"}

    def label(self, prefix: str) -> str:
        env = sorted(self.env_roles(prefix))
        acc = sorted(self.account_roles())
        parts = []
        if acc:
            parts.append("account: " + ", ".join(acc))
        if env:
            parts.append("ambiente: " + ", ".join(env))
        return " - ".join(parts) or "nessun ruolo"


def detect(aws) -> Identity:
    """Utente e gruppi del profilo. Non fallisce: senza permessi IAM si resta senza ruoli."""
    ident = aws.check_account()
    arn = ident["Arn"]
    if arn.endswith(":root"):
        return Identity(ident["Account"], arn, None, admin_direct=True,
                        note="utente root: tutto abilitato, ma la root non andrebbe usata")
    if ":user/" not in arn:
        return Identity(ident["Account"], arn, None,
                        note="non e' un utente IAM (ruolo o SSO): ruoli non determinabili, funzioni disattivate")
    user = arn.rsplit("/", 1)[-1]
    iam = aws.client("iam")
    me = Identity(ident["Account"], arn, user)
    try:
        for page in iam.get_paginator("list_groups_for_user").paginate(UserName=user):
            me.groups.update(g["GroupName"] for g in page["Groups"])
    except ClientError as exc:
        me.note = f"gruppi non leggibili ({exc.response['Error']['Code']})"
        log.warning("Ruoli: %s", me.note)
    try:
        for page in iam.get_paginator("list_attached_user_policies").paginate(UserName=user):
            if any(p["PolicyArn"] in ADMIN_POLICIES for p in page["AttachedPolicies"]):
                me.admin_direct = True
    except ClientError:
        pass            # un utente non amministratore di solito non puo' leggerle: va bene cosi'
    log.info("Utente %s - gruppi: %s%s", user, ", ".join(sorted(me.groups)) or "nessuno",
             " - AdministratorAccess diretta" if me.admin_direct else "")
    return me


def allowed(me: Identity | None, env, key: str) -> bool:
    """La funzione `key` e' utilizzabile da `me` sull'ambiente `env` (EnvSettings)?"""
    f = FEATURES[key]
    if f.only_env and env.name not in f.only_env:
        return False
    if me is None:
        return False
    if me.is_admin:
        return True
    if set(f.account_roles) & me.account_roles():
        return True
    return bool(set(f.env_roles) & me.env_roles(env.gruppi_prefisso))


def why_not(me: Identity | None, env, key: str) -> str:
    f = FEATURES[key]
    if f.only_env and env.name not in f.only_env:
        return f"{f.label}: non esiste sull'ambiente {env.name}"
    need = [f"{env.gruppi_prefisso}{r}" for r in f.env_roles] + [f"{ACCOUNT_PREFIX}{r}" for r in
                                                                 (*f.account_roles, "admin")]
    return f"{f.label}: serve uno di questi ruoli (utility 14): {', '.join(need)}"
