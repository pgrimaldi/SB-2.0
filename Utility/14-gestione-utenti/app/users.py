"""Utenti IAM: elenco, creazione, ruoli, password, chiavi, cancellazione.

Si modificano e cancellano solo gli utenti con il tag di gestione (sb:gestito-da=utility-14): quelli
creati a mano compaiono nell'elenco ma restano intoccabili. Il secret di una chiave esiste solo nella
risposta di CreateAccessKey: non si scrive mai nel log, si mostra e si salva su richiesta.
"""
from __future__ import annotations

import logging
import re
import secrets
import string
import threading
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path

from botocore.exceptions import ClientError

from app.awsutil import Aws, OperationError, Progress, check_cancel, no_progress
from app.logging_setup import LOGGER_NAME
from app.roles import Catalog

log = logging.getLogger(LOGGER_NAME)

USER_NAME = re.compile(r"^[A-Za-z0-9+=,.@_-]{1,64}$")
SYMBOLS = "!@#%^*-_=+?"
MIN_PASSWORD = 12
MAX_KEYS = 2
ADMIN = "account:admin"


@dataclass
class KeyInfo:
    key_id: str
    status: str
    created: datetime
    last_used: datetime | None = None
    last_service: str = ""


@dataclass
class UserInfo:
    name: str
    arn: str
    created: datetime
    managed: bool
    console: bool = False
    password_last_used: datetime | None = None
    roles: list[str] = field(default_factory=list)
    other_groups: list[str] = field(default_factory=list)
    has_base: bool = False
    keys: list[KeyInfo] = field(default_factory=list)
    mfa: int = 0


@dataclass
class NewKey:
    user: str
    key_id: str
    secret: str
    created: datetime
    password: str = ""
    roles: list[str] = field(default_factory=list)


# ---------------------------------------------------------------- password

def generate_password(length: int = 20) -> str:
    """Password casuale con almeno una maiuscola, una minuscola, una cifra e un simbolo."""
    length = max(length, MIN_PASSWORD)
    alphabet = string.ascii_letters + string.digits + SYMBOLS
    while True:
        p = "".join(secrets.choice(alphabet) for _ in range(length))
        if (any(c.islower() for c in p) and any(c.isupper() for c in p)
                and any(c.isdigit() for c in p) and any(c in SYMBOLS for c in p)):
            return p


def check_password(p: str) -> None:
    kinds = sum([any(c.islower() for c in p), any(c.isupper() for c in p), any(c.isdigit() for c in p),
                 any(not c.isalnum() for c in p)])
    if len(p) < MIN_PASSWORD or kinds < 3:
        raise OperationError(f"Password troppo debole: almeno {MIN_PASSWORD} caratteri e tre tipi fra "
                             "maiuscole, minuscole, cifre e simboli.")


def check_name(name: str) -> None:
    if not USER_NAME.match(name):
        raise OperationError("Nome utente non valido: 1-64 caratteri fra lettere, cifre e + = , . @ _ -")


# ---------------------------------------------------------------- lettura

def _managed(aws: Aws, tags: list[dict]) -> bool:
    t = aws.settings.tag_gestione
    return any(x["Key"] == t.chiave and x["Value"] == t.valore for x in tags)


def read_user(aws: Aws, cat: Catalog, name: str) -> UserInfo:
    iam = aws.iam
    u = iam.get_user(UserName=name)["User"]
    tags = iam.list_user_tags(UserName=name)["Tags"]
    info = UserInfo(name=u["UserName"], arn=u["Arn"], created=u["CreateDate"], managed=_managed(aws, tags),
                    password_last_used=u.get("PasswordLastUsed"))
    try:
        iam.get_login_profile(UserName=name)
        info.console = True
    except iam.exceptions.NoSuchEntityException:
        pass
    for page in iam.get_paginator("list_groups_for_user").paginate(UserName=name):
        for g in page["Groups"]:
            role = cat.role_of_group(g["GroupName"])
            if role:
                info.roles.append(role)
            elif g["GroupName"] == cat.base.gruppo:
                info.has_base = True
            else:
                info.other_groups.append(g["GroupName"])
    info.roles = [k for k in cat.keys if k in info.roles]
    for k in iam.list_access_keys(UserName=name)["AccessKeyMetadata"]:
        used = iam.get_access_key_last_used(AccessKeyId=k["AccessKeyId"])["AccessKeyLastUsed"]
        info.keys.append(KeyInfo(k["AccessKeyId"], k["Status"], k["CreateDate"], used.get("LastUsedDate"),
                                 used.get("ServiceName", "") if used.get("ServiceName") != "N/A" else ""))
    info.mfa = len(iam.list_mfa_devices(UserName=name)["MFADevices"])
    return info


def list_users(aws: Aws, cat: Catalog, cancel: threading.Event | None = None,
               progress: Progress = no_progress) -> list[UserInfo]:
    aws.check_account()
    names = [u["UserName"] for page in aws.iam.get_paginator("list_users").paginate() for u in page["Users"]]
    out = []
    for i, name in enumerate(names):
        check_cancel(cancel)
        progress("utenti", i, len(names), name)
        out.append(read_user(aws, cat, name))
    progress("utenti", len(names), len(names), "fatto")
    log.info("Utenti: %d, gestiti dalla utility: %d", len(out), sum(u.managed for u in out))
    return out


def _require_managed(aws: Aws, cat: Catalog, name: str) -> UserInfo:
    aws.check_account()
    try:
        info = read_user(aws, cat, name)
    except aws.iam.exceptions.NoSuchEntityException:
        raise OperationError(f"L'utente {name} non esiste.") from None
    if not info.managed:
        t = aws.settings.tag_gestione
        raise OperationError(f"L'utente {name} non e' gestito dalla utility (manca il tag {t.chiave}={t.valore}): "
                             "non si modifica da qui.")
    return info


def _no_self_lockout(aws: Aws, name: str, roles: list[str] | None = None, deleting: bool = False) -> None:
    if name != aws.actor():
        return
    if deleting:
        raise OperationError("Non si puo' cancellare l'utente con cui si sta usando la utility.")
    if roles is not None and ADMIN not in roles:
        raise OperationError("Non si puo' togliere il ruolo admin all'utente con cui si sta usando la utility.")


# ---------------------------------------------------------------- scrittura

def _set_groups(aws: Aws, cat: Catalog, name: str, roles: list[str]) -> tuple[list[str], list[str]]:
    iam = aws.iam
    wanted = {cat.base.gruppo, *(cat.role(r).gruppo for r in roles)}
    current = {g["GroupName"] for page in iam.get_paginator("list_groups_for_user").paginate(UserName=name)
               for g in page["Groups"]}
    managed_groups = cat.group_names()
    add = sorted(wanted - current)
    drop = sorted((current & managed_groups) - wanted)
    for g in add:
        iam.add_user_to_group(GroupName=g, UserName=name)
    for g in drop:
        iam.remove_user_from_group(GroupName=g, UserName=name)
    return add, drop


def _check_groups_exist(aws: Aws, cat: Catalog, roles: list[str]) -> None:
    missing = []
    for g in [cat.base.gruppo, *(cat.role(r).gruppo for r in roles)]:
        try:
            aws.iam.get_group(GroupName=g, MaxItems=1)
        except aws.iam.exceptions.NoSuchEntityException:
            missing.append(g)
    if missing:
        raise OperationError(f"Gruppi assenti: {', '.join(missing)}. Premere prima 'Prepara ruoli'.")


def _create_key(aws: Aws, name: str) -> NewKey:
    k = aws.iam.create_access_key(UserName=name)["AccessKey"]
    log.info("Chiave %s creata per %s", k["AccessKeyId"], name)
    return NewKey(user=name, key_id=k["AccessKeyId"], secret=k["SecretAccessKey"], created=k["CreateDate"])


def create_user(aws: Aws, cat: Catalog, name: str, password: str, roles: list[str]) -> NewKey:
    aws.check_account()
    check_name(name)
    check_password(password)
    roles = cat.expand(roles)
    _check_groups_exist(aws, cat, roles)
    iam = aws.iam
    try:
        iam.get_user(UserName=name)
        raise OperationError(f"L'utente {name} esiste gia'. Per cambiarne ruoli, password o chiavi usare "
                             "i pulsanti dell'utente selezionato.")
    except iam.exceptions.NoSuchEntityException:
        pass
    t = aws.settings.tag_gestione
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    iam.create_user(UserName=name, Tags=[{"Key": t.chiave, "Value": t.valore},
                                         {"Key": "sb:creato-da", "Value": aws.actor() or "sconosciuto"},
                                         {"Key": "sb:creato-il", "Value": now}])
    log.info("Utente %s creato (ruoli: %s)", name, ", ".join(roles) or "nessuno")
    try:
        iam.create_login_profile(UserName=name, Password=password, PasswordResetRequired=False)
        log.info("Accesso alla console attivato per %s", name)
        _set_groups(aws, cat, name, roles)
        key = _create_key(aws, name)
    except ClientError as exc:
        raise OperationError(f"Utente {name} creato ma non completato: {exc.response['Error']['Message']}. "
                             "Completarlo con i pulsanti dell'utente selezionato oppure cancellarlo.") from exc
    key.password, key.roles = password, roles
    return key


def set_roles(aws: Aws, cat: Catalog, name: str, roles: list[str]) -> tuple[list[str], list[str]]:
    roles = cat.expand(roles)
    _require_managed(aws, cat, name)
    _no_self_lockout(aws, name, roles=roles)
    _check_groups_exist(aws, cat, roles)
    add, drop = _set_groups(aws, cat, name, roles)
    log.info("Ruoli di %s: %s (gruppi + %s, - %s)", name, ", ".join(roles) or "nessuno",
             ", ".join(add) or "nessuno", ", ".join(drop) or "nessuno")
    return add, drop


def set_password(aws: Aws, cat: Catalog, name: str, password: str) -> None:
    check_password(password)
    info = _require_managed(aws, cat, name)
    if info.console:
        aws.iam.update_login_profile(UserName=name, Password=password, PasswordResetRequired=False)
    else:
        aws.iam.create_login_profile(UserName=name, Password=password, PasswordResetRequired=False)
    log.info("Password della console di %s %s", name, "cambiata" if info.console else "impostata")


def new_key(aws: Aws, cat: Catalog, name: str) -> NewKey:
    info = _require_managed(aws, cat, name)
    if len(info.keys) >= MAX_KEYS:
        raise OperationError(f"{name} ha gia' {MAX_KEYS} chiavi (il massimo di AWS): disattivare e "
                             "cancellare la piu' vecchia prima di crearne un'altra.")
    key = _create_key(aws, name)
    key.roles = info.roles
    return key


def set_key_status(aws: Aws, cat: Catalog, name: str, key_id: str, active: bool) -> None:
    _require_managed(aws, cat, name)
    aws.iam.update_access_key(UserName=name, AccessKeyId=key_id, Status="Active" if active else "Inactive")
    log.info("Chiave %s di %s %s", key_id, name, "riattivata" if active else "disattivata")


def delete_key(aws: Aws, cat: Catalog, name: str, key_id: str) -> None:
    _require_managed(aws, cat, name)
    aws.iam.delete_access_key(UserName=name, AccessKeyId=key_id)
    log.info("Chiave %s di %s cancellata", key_id, name)


def delete_user(aws: Aws, cat: Catalog, name: str, cancel: threading.Event | None = None,
                progress: Progress = no_progress) -> None:
    """IAM vuole l'utente spoglio: si tolgono una cosa alla volta tutte le sue dipendenze."""
    _require_managed(aws, cat, name)
    _no_self_lockout(aws, name, deleting=True)
    iam = aws.iam
    steps = [
        ("password della console", lambda: _try(iam.delete_login_profile, UserName=name)),
        ("chiavi", lambda: [iam.delete_access_key(UserName=name, AccessKeyId=k["AccessKeyId"])
                            for k in iam.list_access_keys(UserName=name)["AccessKeyMetadata"]]),
        ("MFA", lambda: _delete_mfa(iam, name)),
        ("chiavi SSH", lambda: [iam.delete_ssh_public_key(UserName=name, SSHPublicKeyId=k["SSHPublicKeyId"])
                                for k in iam.list_ssh_public_keys(UserName=name)["SSHPublicKeys"]]),
        ("certificati", lambda: [iam.delete_signing_certificate(UserName=name, CertificateId=c["CertificateId"])
                                 for c in iam.list_signing_certificates(UserName=name)["Certificates"]]),
        ("credenziali di servizio", lambda: [
            iam.delete_service_specific_credential(UserName=name,
                                                   ServiceSpecificCredentialId=c["ServiceSpecificCredentialId"])
            for c in iam.list_service_specific_credentials(UserName=name)["ServiceSpecificCredentials"]]),
        ("policy inline", lambda: [iam.delete_user_policy(UserName=name, PolicyName=p)
                                   for p in iam.list_user_policies(UserName=name)["PolicyNames"]]),
        ("policy agganciate", lambda: [iam.detach_user_policy(UserName=name, PolicyArn=p["PolicyArn"])
                                       for p in iam.list_attached_user_policies(UserName=name)["AttachedPolicies"]]),
        ("gruppi", lambda: [iam.remove_user_from_group(GroupName=g["GroupName"], UserName=name)
                            for g in iam.list_groups_for_user(UserName=name)["Groups"]]),
        ("utente", lambda: iam.delete_user(UserName=name)),
    ]
    for i, (label, fn) in enumerate(steps):
        check_cancel(cancel)
        progress("cancellazione", i, len(steps), label)
        fn()
    progress("cancellazione", len(steps), len(steps), "fatto")
    log.info("Utente %s cancellato", name)


def _try(fn, **kw) -> None:
    try:
        fn(**kw)
    except ClientError as exc:
        if exc.response["Error"]["Code"] != "NoSuchEntity":
            raise


def _delete_mfa(iam, name: str) -> None:
    for d in iam.list_mfa_devices(UserName=name)["MFADevices"]:
        iam.deactivate_mfa_device(UserName=name, SerialNumber=d["SerialNumber"])
        if ":mfa/" in d["SerialNumber"]:
            _try(iam.delete_virtual_mfa_device, SerialNumber=d["SerialNumber"])


# ---------------------------------------------------------------- scheda delle credenziali

def credentials_text(account: str, region: str, key: NewKey, cat: Catalog) -> str:
    """Testo da consegnare all'utente: nel formato di ~/.aws/credentials, piu' l'accesso alla console."""
    lines = [
        f"# Utente IAM {key.user} - account AWS {account}",
        f"# Generata il {datetime.now():%d/%m/%Y %H:%M} dalla utility 14. Contiene segreti IN CHIARO:",
        "# consegnarla per un canale sicuro e cancellarla dopo l'uso.",
        f"# Ruoli: {cat.label(key.roles)}",
        "#",
        f"# Console:  https://{account}.signin.aws.amazon.com/console",
        f"# Utente:   {key.user}",
    ]
    if key.password:
        lines.append(f"# Password: {key.password}")
    lines += [
        "#",
        "# Uso con la AWS CLI: copiare il blocco qui sotto in %USERPROFILE%\\.aws\\credentials",
        f"# (oppure: aws configure --profile {key.user}) e usare --profile {key.user}",
        "",
        f"[{key.user}]",
        f"aws_access_key_id = {key.key_id}",
        f"aws_secret_access_key = {key.secret}",
        f"region = {region}",
        "",
    ]
    return "\n".join(lines)


def default_file_name(key: NewKey) -> str:
    return f"{key.user}_{key.key_id[-4:]}_{datetime.now():%Y%m%d_%H%M%S}.txt"


def save_credentials(text: str, path: Path | str) -> None:
    p = Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")
    log.info("Scheda delle credenziali salvata in %s", p)
