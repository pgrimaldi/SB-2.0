"""Ruoli: definizioni (app/ruoli/), policy e gruppi IAM.

Un ruolo e' un gruppo IAM con le sue policy; un utente puo' stare in piu' gruppi e i permessi si
sommano. I ruoli stanno in tre ambiti: un ambiente (dev, prod), con le policy legate al suo stack, oppure
l'intero account. Un ruolo si indica con la chiave `ambito:nome` (dev:frontend, prod:log, account:admin).

Le policy proprie sono file JSON con segnaposto ({{Account}}, {{Region}}, {{Stack}}, {{Output:<nome>}})
risolti con l'identita' del chiamante e gli output dello stack dell'ambiente che le usa.

"Prepara ruoli" porta policy e gruppi esattamente allo stato dei file: crea quello che manca,
aggiorna le policy cambiate (nuova versione predefinita) e stacca dai gruppi le policy in piu'.
Ambienti e ruoli marcati da_definire si mostrano ma non si preparano ne' si assegnano.
"""
from __future__ import annotations

import json
import logging
import re
import threading
from dataclasses import dataclass, field
from typing import Callable

from botocore.exceptions import ClientError

from app.awsutil import Aws, OperationError, Progress, check_cancel, no_progress
from app.logging_setup import LOGGER_NAME
from app.settings import RUOLI_DIR, Settings

log = logging.getLogger(LOGGER_NAME)

Step = Callable[[str, str, str, str, str], None]   # chiave, ambito, elemento, stato, note
PLACEHOLDER = re.compile(r"\{\{([A-Za-z]+(?::[A-Za-z0-9]+)?)\}\}")
MAX_POLICY_VERSIONS = 5
ACCOUNT = "account"


def no_step(key: str, scope: str, item: str, status: str, notes: str) -> None:
    pass


@dataclass
class Role:
    nome: str
    gruppo: str
    ambito: str = ACCOUNT
    descrizione: str = ""
    da_definire: bool = False
    policy: list[str] = field(default_factory=list)
    policy_aws: list[str] = field(default_factory=list)
    policy_esistenti: list[str] = field(default_factory=list)

    @property
    def key(self) -> str:
        return f"{self.ambito}:{self.nome}"


@dataclass
class Scope:
    """Un ambiente (dev, prod) oppure l'intero account."""
    nome: str
    descrizione: str = ""
    stack: str = ""
    da_definire: bool = False
    ruoli: list[Role] = field(default_factory=list)
    preset: dict[str, list[str]] = field(default_factory=dict)


@dataclass
class Catalog:
    base: Role
    scopes: list[Scope]
    policy_senza_gruppo: list[str]
    descrizioni_policy: dict[str, str]

    @property
    def ruoli(self) -> list[Role]:
        return [r for s in self.scopes for r in s.ruoli]

    @property
    def keys(self) -> list[str]:
        return [r.key for r in self.ruoli]

    def scope(self, name: str) -> Scope:
        for s in self.scopes:
            if s.nome == name:
                return s
        raise OperationError(f"Ambito sconosciuto: {name} (validi: {', '.join(s.nome for s in self.scopes)})")

    def role(self, key: str) -> Role:
        for r in self.ruoli:
            if r.key == key:
                return r
        raise OperationError(f"Ruolo sconosciuto: {key} (validi: {', '.join(self.keys)})")

    @property
    def groups(self) -> list[Role]:
        """Gruppi da preparare: il base e i ruoli definiti."""
        return [self.base, *(r for r in self.ruoli if not r.da_definire)]

    def group_names(self) -> set[str]:
        return {self.base.gruppo, *(r.gruppo for r in self.ruoli)}

    def role_of_group(self, group: str) -> str | None:
        return next((r.key for r in self.ruoli if r.gruppo == group), None)

    def own_policies(self) -> dict[str, str]:
        """Policy proprie -> ambito i cui valori le risolvono (account = solo Account e Region)."""
        out: dict[str, str] = {p: ACCOUNT for p in self.policy_senza_gruppo}
        for r in self.groups:
            for p in r.policy:
                prev = out.setdefault(p, r.ambito)
                if prev != r.ambito and ACCOUNT not in (prev, r.ambito):
                    raise OperationError(f"La policy {p} e' usata da due ambienti ({prev}, {r.ambito})")
                if prev == ACCOUNT:
                    out[p] = r.ambito
        return out

    def parse(self, text: str) -> str:
        """'dev:frontend' -> chiave; senza prefisso vale per dev e account se il nome e' unico fra i due."""
        t = text.strip().lower()
        if ":" in t:
            return t
        hits = [r.key for r in self.ruoli if r.nome == t and r.ambito in ("dev", ACCOUNT)]
        hits += [f"dev:{t}"] if t in self.scope("dev").preset else []
        if len(hits) != 1:
            raise OperationError(f"Ruolo '{text}': indicare l'ambito, per esempio dev:{t} o prod:{t}")
        return hits[0]

    def expand(self, items: list[str]) -> list[str]:
        """Ruoli richiesti -> chiavi vere, con i preset (dev:fullstack) sciolti e senza doppioni."""
        out: list[str] = []
        for item in items:
            if not item.strip():
                continue
            key = self.parse(item)
            scope_name, name = key.split(":", 1)
            scope = self.scope(scope_name)
            for real in scope.preset.get(name, [name]):
                r = self.role(f"{scope_name}:{real}")
                if r.da_definire or scope.da_definire:
                    raise OperationError(f"Il ruolo {r.key} e' ancora da definire: non si puo' assegnare.")
                if r.key not in out:
                    out.append(r.key)
        return [k for k in self.keys if k in out]

    def label_scope(self, scope: Scope, keys: list[str]) -> str:
        names = [r.nome for r in scope.ruoli if r.key in keys]
        presets = [p for p, m in scope.preset.items() if m and set(m) <= set(names)]
        text = ", ".join(names) or "-"
        return f"{text} ({', '.join(presets)})" if presets else text

    def label(self, keys: list[str]) -> str:
        parts = [f"{s.nome}: {self.label_scope(s, keys)}" for s in self.scopes
                 if any(k.startswith(s.nome + ":") for k in keys)]
        return " · ".join(parts) or "nessuno"


def load_catalog() -> Catalog:
    data = json.loads((RUOLI_DIR / "ruoli.json").read_text(encoding="utf-8"))

    def mk(d: dict, ambito: str, todo: bool = False) -> Role:
        r = Role(**{k: v for k, v in d.items() if k in Role.__dataclass_fields__}, ambito=ambito)
        r.da_definire = r.da_definire or todo
        return r

    scopes = []
    for env in data.get("ambienti", []):
        todo = bool(env.get("da_definire"))
        scopes.append(Scope(nome=env["nome"], descrizione=env.get("descrizione", ""), stack=env.get("stack", ""),
                            da_definire=todo, ruoli=[mk(r, env["nome"], todo) for r in env.get("ruoli", [])],
                            preset=dict(env.get("preset") or {})))
    acc = data.get("account") or {}
    scopes.append(Scope(nome=ACCOUNT, descrizione=acc.get("descrizione", ""),
                        ruoli=[mk(r, ACCOUNT) for r in acc.get("ruoli", [])], preset=dict(acc.get("preset") or {})))
    cat = Catalog(base=mk({"nome": "base", **data["base"]}, ACCOUNT), scopes=scopes,
                  policy_senza_gruppo=list(data.get("policy_senza_gruppo") or []),
                  descrizioni_policy=dict(data.get("descrizioni_policy") or {}))
    for name in cat.own_policies():
        if not (RUOLI_DIR / "policy" / f"{name}.json").exists():
            raise OperationError(f"Policy {name}: manca il file app/ruoli/policy/{name}.json")
    return cat


# ---------------------------------------------------------------- segnaposto

def placeholders(aws: Aws, settings: Settings, scope: Scope | None) -> tuple[dict[str, str], str]:
    """Valori dei segnaposto per un ambito. Il secondo elemento e' il motivo per cui mancano gli output."""
    values = {"Account": aws.account, "Region": settings.aws.region}
    if scope is None or not scope.stack:
        return values, ""
    values["Stack"] = scope.stack
    try:
        stack = aws.client("cloudformation").describe_stacks(StackName=scope.stack)["Stacks"][0]
    except ClientError as exc:
        return values, f"stack {scope.stack} non leggibile: {exc.response['Error']['Message']}"
    for o in stack.get("Outputs", []):
        values[f"Output:{o['OutputKey']}"] = o["OutputValue"]
    return values, ""


def render(name: str, values: dict[str, str]) -> dict:
    text = (RUOLI_DIR / "policy" / f"{name}.json").read_text(encoding="utf-8")
    missing = sorted({m for m in PLACEHOLDER.findall(text) if m not in values})
    if missing:
        raise OperationError(f"valori mancanti: {', '.join(missing)}")
    return json.loads(PLACEHOLDER.sub(lambda m: values[m.group(1)], text))


def _canonical(doc: dict) -> str:
    return json.dumps(doc, sort_keys=True, separators=(",", ":"))


def policy_arn(account: str, name: str) -> str:
    return f"arn:aws:iam::{account}:policy/{name}"


# ---------------------------------------------------------------- sincronizzazione

def _sync_policy(aws: Aws, cat: Catalog, name: str, doc: dict, apply: bool) -> str:
    """Ritorna lo stato: 'OK', 'da creare', 'da aggiornare', 'creata', 'aggiornata'."""
    iam = aws.iam
    arn = policy_arn(aws.account, name)
    body = json.dumps(doc, indent=1)
    try:
        pol = iam.get_policy(PolicyArn=arn)["Policy"]
    except iam.exceptions.NoSuchEntityException:
        if not apply:
            return "da creare"
        iam.create_policy(PolicyName=name, PolicyDocument=body,
                          Description=cat.descrizioni_policy.get(name, "Utility 14"),
                          Tags=[{"Key": aws.settings.tag_gestione.chiave, "Value": aws.settings.tag_gestione.valore}])
        log.info("Policy %s creata", name)
        return "creata"
    current = iam.get_policy_version(PolicyArn=arn, VersionId=pol["DefaultVersionId"])["PolicyVersion"]["Document"]
    if isinstance(current, str):
        current = json.loads(current)
    if _canonical(current) == _canonical(doc):
        return "OK"
    if not apply:
        return "da aggiornare"
    versions = iam.list_policy_versions(PolicyArn=arn)["Versions"]
    if len(versions) >= MAX_POLICY_VERSIONS:
        oldest = min((v for v in versions if not v["IsDefaultVersion"]), key=lambda v: v["CreateDate"])
        iam.delete_policy_version(PolicyArn=arn, VersionId=oldest["VersionId"])
    iam.create_policy_version(PolicyArn=arn, PolicyDocument=body, SetAsDefault=True)
    log.info("Policy %s aggiornata", name)
    return "aggiornata"


def _sync_group(aws: Aws, role: Role, wanted: list[str], apply: bool) -> tuple[str, str]:
    iam = aws.iam
    try:
        iam.get_group(GroupName=role.gruppo, MaxItems=1)
        exists = True
    except iam.exceptions.NoSuchEntityException:
        exists = False
    if not exists:
        if not apply:
            return "da creare", f"{len(wanted)} policy"
        iam.create_group(GroupName=role.gruppo)
        log.info("Gruppo %s creato", role.gruppo)
    attached = set()
    if exists:
        for page in iam.get_paginator("list_attached_group_policies").paginate(GroupName=role.gruppo):
            attached |= {p["PolicyArn"] for p in page["AttachedPolicies"]}
    add = [a for a in wanted if a not in attached]
    drop = sorted(attached - set(wanted))
    inline = iam.list_group_policies(GroupName=role.gruppo)["PolicyNames"] if exists else []
    notes = []
    if add:
        notes.append("+ " + ", ".join(a.rsplit("/", 1)[-1] for a in add))
    if drop:
        notes.append("- " + ", ".join(a.rsplit("/", 1)[-1] for a in drop))
    if inline:
        notes.append("policy inline non gestite: " + ", ".join(inline))
    if not add and not drop:
        return ("con avvisi" if inline else "OK"), "; ".join(notes) or f"{len(wanted)} policy"
    if not apply:
        return "da aggiornare", "; ".join(notes)
    for a in add:
        iam.attach_group_policy(GroupName=role.gruppo, PolicyArn=a)
    for a in drop:
        iam.detach_group_policy(GroupName=role.gruppo, PolicyArn=a)
    log.info("Gruppo %s: %s", role.gruppo, "; ".join(notes))
    return ("creato" if not exists else "aggiornato"), "; ".join(notes)


def sync_roles(aws: Aws, settings: Settings, cat: Catalog, apply: bool, cancel: threading.Event | None = None,
               progress: Progress = no_progress, step: Step = no_step) -> dict[str, int]:
    """apply=False = solo confronto. Ritorna i conteggi per stato."""
    aws.check_account()
    values: dict[str, tuple[dict[str, str], str]] = {}
    for s in cat.scopes:
        if s.da_definire or not any(not r.da_definire for r in s.ruoli):
            continue
        values[s.nome] = placeholders(aws, settings, s if s.nome != ACCOUNT else None)
        if values[s.nome][1]:
            log.warning("%s: i ruoli di %s non si possono preparare", values[s.nome][1], s.nome)
    values.setdefault(ACCOUNT, placeholders(aws, settings, None))
    counts: dict[str, int] = {}

    def count(status: str) -> None:
        counts[status] = counts.get(status, 0) + 1

    policies = cat.own_policies()
    total = len(policies) + len(cat.groups)
    broken: set[str] = set()
    for i, (name, scope_name) in enumerate(policies.items()):
        check_cancel(cancel)
        progress("policy", i, total, name)
        key = f"policy:{name}"
        label = f"policy {scope_name}"
        step(key, label, name, "in corso", "")
        vals, problem = values[scope_name]
        try:
            status = _sync_policy(aws, cat, name, render(name, vals), apply)
            step(key, label, name, status, cat.descrizioni_policy.get(name, ""))
        except (OperationError, ClientError) as exc:
            broken.add(name)
            status = "KO"
            note = str(exc) + (f" ({problem})" if problem else "")
            log.error("Policy %s: %s", name, note)
            step(key, label, name, status, note)
        count(status)

    existing_cache: dict[str, bool] = {}
    for j, role in enumerate(cat.groups):
        check_cancel(cancel)
        progress("gruppi", len(policies) + j, total, role.gruppo)
        key = f"group:{role.gruppo}"
        scope = f"{role.ambito} · {role.nome}"
        step(key, scope, role.gruppo, "in corso", "")
        bad = [p for p in role.policy if p in broken]
        missing_existing = []
        for p in role.policy_esistenti:
            if p not in existing_cache:
                try:
                    aws.iam.get_policy(PolicyArn=policy_arn(aws.account, p))
                    existing_cache[p] = True
                except aws.iam.exceptions.NoSuchEntityException:
                    existing_cache[p] = False
            if not existing_cache[p]:
                missing_existing.append(p)
        if bad or missing_existing:
            note = "; ".join(filter(None, [
                f"policy in errore: {', '.join(bad)}" if bad else "",
                f"policy dello stack assenti: {', '.join(missing_existing)}" if missing_existing else ""]))
            step(key, scope, role.gruppo, "saltato", note)
            log.warning("Gruppo %s saltato: %s", role.gruppo, note)
            count("saltato")
            continue
        wanted = ([policy_arn(aws.account, p) for p in role.policy] + list(role.policy_aws)
                  + [policy_arn(aws.account, p) for p in role.policy_esistenti])
        try:
            status, note = _sync_group(aws, role, wanted, apply)
            step(key, scope, role.gruppo, status, note)
        except ClientError as exc:
            status = "KO"
            log.error("Gruppo %s: %s", role.gruppo, exc)
            step(key, scope, role.gruppo, status, str(exc))
        count(status)

    for role in cat.ruoli:
        if role.da_definire:
            step(f"group:{role.gruppo}", f"{role.ambito} · {role.nome}", role.gruppo, "da definire", role.descrizione)
    progress("gruppi", total, total, "fatto")
    return counts
