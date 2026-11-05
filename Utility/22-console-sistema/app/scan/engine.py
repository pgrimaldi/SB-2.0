"""Contesto di una scansione e motore delle chiamate generiche e delle regole di dettaglio.

Il contesto tiene in memoria tutte le risposte (configurazioni: pochi MB) perche' le regole e le
sezioni successive ne leggono gli id. Ogni risposta non vuota finisce anche su disco, in
`json/<region>/<servizio>/<operazione>.json`.
"""
from __future__ import annotations

import json
import logging
import threading
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Callable, Iterable

import jmespath

from app.scan import catalog
from app.scan.awsutil import (ABSENT, DENIED, EMPTY, ERROR, GLOBAL, OK, UNAVAILABLE, Aws, CallStats,
                         Cancelled, classify, count_items, is_empty, write_json)
from app.logging_setup import LOGGER_NAME
from app.scan.settings import Settings

log = logging.getLogger(LOGGER_NAME)

# Callback verso GUI / headless
ProgressFn = Callable[[str, float, float, str], None]
StepFn = Callable[[str, str, str, str, str, str], None]   # key, ambito, passo, stato, elementi, note

# Stati di un passo
S_RUN, S_OK, S_WARN, S_KO, S_SKIP = "in corso", "OK", "con avvisi", "KO", "saltato"

# Codici che, su una chiamata generica senza parametri, vogliono dire «servono parametri»:
# l'operazione non si applica, non e' un errore della scansione.
_NEEDS_PARAMS = {"ValidationException", "ValidationError", "InvalidParameterException",
                 "InvalidParameterValue", "InvalidParameterValueException", "MissingParameter",
                 "InvalidParameterCombination", "InvalidInput", "InvalidInputException",
                 "InvalidRequestException", "BadRequestException", "InvalidParameter"}


@dataclass
class Ctx:
    settings: Settings
    aws: Aws
    out: Path
    cancel: threading.Event
    progress: ProgressFn
    step: StepFn
    regions: list[str] = field(default_factory=list)       # region vere, la home per prima
    stats: CallStats = field(default_factory=CallStats)
    generic: dict[tuple[str, str, str], Any] = field(default_factory=dict)
    detail: dict[tuple[str, str, str], dict[str, Any]] = field(default_factory=dict)
    sections: dict[str, dict] = field(default_factory=dict)  # esito per sezione, per il manifesto
    lock: threading.Lock = field(default_factory=threading.Lock)

    @property
    def labels(self) -> list[str]:
        """Ambiti di lettura: `global` + le region."""
        return [GLOBAL, *self.regions]

    def check(self) -> None:
        if self.cancel.is_set():
            raise Cancelled()

    def executor(self) -> ThreadPoolExecutor:
        return ThreadPoolExecutor(max_workers=max(1, self.settings.aws.max_workers),
                                  thread_name_prefix="aws")

    def result(self, region: str, service: str, op: str) -> list[tuple[str | None, Any]]:
        """Risposte salvate per un'operazione: [(chiave, risposta)]; il dettaglio prevale."""
        with self.lock:
            d = self.detail.get((region, service, op))
            if d is not None:
                return list(d.items())
            g = self.generic.get((region, service, op))
        if g is None:
            return []
        if isinstance(g, dict) and g.get("__varianti__"):
            return [(k, v) for k, v in g.items() if k != "__varianti__"]
        return [(None, g)]

    def items(self, region: str, service: str, op: str, path: str) -> list[tuple[str | None, Any]]:
        """Elementi estratti con JMESPath da tutte le risposte di un'operazione: [(chiave, elemento)]."""
        out = []
        for key, resp in self.result(region, service, op):
            found = jmespath.search(path, resp)
            if found is None:
                continue
            for it in (found if isinstance(found, list) else [found]):
                if it is not None and it != "":
                    out.append((key, it))
        return out

    def scopes_for(self, service: str) -> list[str]:
        if service in catalog.GLOBAL_SERVICES:
            return [GLOBAL]
        regs = list(self.regions)
        if service in catalog.GLOBAL_ALSO and "us-east-1" not in regs:
            regs.append(GLOBAL)
        return regs

    def section(self, name: str, status: str, **info) -> None:
        with self.lock:
            self.sections[name] = {"stato": status, **info}

    def rel(self, path: Path) -> str:
        return path.relative_to(self.out).as_posix()


def _fill(value: Any, account: str) -> Any:
    if isinstance(value, str):
        return value.replace("{account}", account)
    if isinstance(value, list):
        return [_fill(v, account) for v in value]
    if isinstance(value, dict):
        return {k: _fill(v, account) for k, v in value.items()}
    return value


def _label(kwargs: dict) -> str:
    return ",".join(f"{k}={v}" for k, v in kwargs.items()) or "-"


def call_logged(ctx: Ctx, region: str, service: str, op: str, kwargs: dict | None,
                generic: bool = False) -> tuple[str, Any]:
    """Chiama e registra l'esito; non solleva per gli errori AWS (solo per l'interruzione)."""
    ctx.check()
    try:
        res = ctx.aws.call(service, region, op, kwargs)
    except Exception as exc:  # noqa: BLE001 - ogni errore si classifica e si conta
        outcome, text = classify(exc)
        code = getattr(exc, "response", {}).get("Error", {}).get("Code", "") if hasattr(exc, "response") else ""
        if generic and outcome == ERROR and code in _NEEDS_PARAMS:
            outcome = UNAVAILABLE
        ctx.stats.add(region, service, op, outcome, text)
        if outcome == ERROR:
            log.warning("%s %s.%s: %s", region, service, op, text)
        elif outcome == DENIED:
            log.debug("%s %s.%s negato: %s", region, service, op, text)
        return outcome, None
    outcome = EMPTY if is_empty(res) else OK
    ctx.stats.add(region, service, op, outcome)
    return outcome, res


# ------------------------------------------------------------------------------------ generiche

def run_generic(ctx: Ctx) -> None:
    services = ctx.settings.services
    tasks: list[tuple[str, str, str, Any]] = []
    for service in services:
        for region in ctx.scopes_for(service):
            if not ctx.aws.service_in_region(service, region):
                continue
            try:
                client = ctx.aws.client(service, region)
            except Exception as exc:  # noqa: BLE001
                log.warning("Client %s in %s non disponibile: %s", service, region, exc)
                continue
            for op, ov in catalog.generic_ops(client, service, region):
                tasks.append((region, service, op, ov))
    log.info("Chiamate generiche: %d (%d servizi, ambiti %s)", len(tasks), len(services), ", ".join(ctx.labels))
    for label in ctx.labels:
        ctx.step(f"json:{label}", label, "Configurazioni (List/Describe/Get)", S_RUN, "", "")

    def one(region, service, op, ov):
        if not isinstance(ov, list):  # nessun override o parametri fissi: una chiamata
            kw = _fill(ov, ctx.aws.account_id) if ov else None
            return region, service, op, *call_logged(ctx, region, service, op, kw, generic=True)
        merged, best = {"__varianti__": True}, EMPTY   # piu' varianti: risposte per parametri
        for kw in ov:
            kw = _fill(kw, ctx.aws.account_id)
            outcome, res = call_logged(ctx, region, service, op, kw, generic=True)
            if outcome == OK:
                best = OK
                merged[_label(kw)] = res
        return region, service, op, best, merged if best == OK else None

    done, files = 0, {label: 0 for label in ctx.labels}
    with ctx.executor() as ex:
        futs = [ex.submit(one, *t) for t in tasks]
        try:
            for f in as_completed(futs):
                region, service, op, outcome, res = f.result()
                done += 1
                if outcome == OK and res is not None:
                    with ctx.lock:
                        ctx.generic[(region, service, op)] = res
                    write_json(ctx.out / "json" / region / service / f"{op}.json", res)
                    files[region] += 1
                if done % 25 == 0 or done == len(tasks):
                    ctx.progress("json", done, len(tasks), f"{region} {service}.{op}")
        except Cancelled:
            for f in futs:
                f.cancel()
            raise
    for label in ctx.labels:
        ctx.step(f"json:{label}", label, "Configurazioni (List/Describe/Get)", S_OK, f"{files[label]} file", "")
    ctx.section("json", S_OK, chiamate=len(tasks), file=sum(files.values()))


# ------------------------------------------------------------------------------------ regole

def _rule_calls(ctx: Ctx, rule: catalog.Rule, region: str) -> list[tuple[str, dict]]:
    """[(chiave, kwargs)] per una regola in una region."""
    found = ctx.items(region, rule.service, rule.source, rule.path)
    calls: list[tuple[str, dict]] = []
    if rule.chunk:
        by_src: dict[str | None, list] = {}
        for k, it in found:
            by_src.setdefault(k, []).append(it)
        for src, ids in by_src.items():
            ids = list(dict.fromkeys(ids))
            for i in range(0, len(ids), rule.chunk):
                part = ids[i:i + rule.chunk]
                kw = rule.param(part, src, region) if callable(rule.param) else {rule.param: part}
                calls.append((f"{src or ''}#{i // rule.chunk}", {**rule.extra, **kw}))
        return calls
    seen = set()
    for src, it in found:
        if callable(rule.param):
            try:
                kw = rule.param(it, src, region)
            except (KeyError, TypeError):
                continue
            key = it if isinstance(it, str) else json.dumps(kw, sort_keys=True, default=str)
        else:
            kw, key = {rule.param: it}, str(it)
        if key in seen:
            continue
        seen.add(key)
        calls.append((key, {**rule.extra, **kw}))
    return calls


def run_rules(ctx: Ctx) -> None:
    active = [r for r in catalog.RULES if r.service in ctx.settings.services]
    for label in ctx.labels:
        ctx.step(f"details:{label}", label, "Dettaglio per risorsa", S_RUN, "", "")
    total_calls = 0
    with ctx.executor() as ex:
        for n, rule in enumerate(active, 1):
            ctx.check()
            for region in ctx.scopes_for(rule.service):
                calls = _rule_calls(ctx, rule, region)
                if not calls:
                    continue
                futs = {ex.submit(call_logged, ctx, region, rule.service, rule.op, kw): key for key, kw in calls}
                got: dict[str, Any] = {}
                for f in as_completed(futs):
                    outcome, res = f.result()
                    if outcome == OK:
                        got[futs[f]] = res
                total_calls += len(calls)
                if got:
                    with ctx.lock:
                        ctx.detail.setdefault((region, rule.service, rule.op), {}).update(got)
            ctx.progress("details", n, len(active), f"{rule.service}.{rule.op}")

    files = {label: 0 for label in ctx.labels}
    for (region, service, op), data in ctx.detail.items():
        name = f"{op}.dettaglio.json" if (region, service, op) in ctx.generic else f"{op}.json"
        write_json(ctx.out / "json" / region / service / name, data)
        files[region] += 1
    for label in ctx.labels:
        ctx.step(f"details:{label}", label, "Dettaglio per risorsa", S_OK, f"{files[label]} file", "")
    log.info("Regole di dettaglio: %d regole, %d chiamate", len(active), total_calls)
    ctx.section("details", S_OK, regole=len(active), chiamate=total_calls, file=sum(files.values()))


def ensure(ctx: Ctx, region: str, service: str, op: str, kwargs: dict | None = None) -> list[tuple[str | None, Any]]:
    """Risposte di un'operazione: dal contesto se gia' lette, altrimenti le legge ora (sezioni
    attivate senza la sezione JSON)."""
    res = ctx.result(region, service, op)
    if res:
        return res
    outcome, data = call_logged(ctx, region, service, op, kwargs)
    if outcome == OK:
        with ctx.lock:
            ctx.generic[(region, service, op)] = data
        return [(None, data)]
    return []


def count_of(ctx: Ctx, region: str, service: str, op: str) -> int:
    return sum(count_items(r) for _, r in ctx.result(region, service, op))


def parallel(ctx: Ctx, fn: Callable, items: Iterable, label: str, phase: str) -> list:
    """Esegue `fn(item)` in parallelo con avanzamento; gli errori di un elemento non fermano gli altri."""
    items = list(items)
    out = []
    with ctx.executor() as ex:
        futs = {ex.submit(fn, it): it for it in items}
        try:
            for i, f in enumerate(as_completed(futs), 1):
                try:
                    out.append(f.result())
                except Cancelled:
                    raise
                except Exception as exc:  # noqa: BLE001
                    log.error("%s %s: %s: %s", label, futs[f], type(exc).__name__, exc)
                    out.append(exc)
                ctx.progress(phase, i, len(items), f"{label} {i}/{len(items)}")
        except Cancelled:
            for f in futs:
                f.cancel()
            raise
    return out


__all__ = ["Ctx", "S_RUN", "S_OK", "S_WARN", "S_KO", "S_SKIP", "ABSENT", "DENIED", "ERROR", "OK",
           "call_logged", "count_of", "ensure", "parallel", "run_generic", "run_rules"]
