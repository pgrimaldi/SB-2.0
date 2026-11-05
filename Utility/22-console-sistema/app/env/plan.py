"""Piano di accensione/spegnimento: stato voluto, prossimi eventi, comandi rapidi.

Questo file gira identico in due posti: nella utility (anteprima, tabella dei prossimi eventi) e
nella Lambda dev-power (decisione reale). Per questo usa solo la libreria standard e non importa
niente dal resto della utility.

Le ore del piano sono locali di Roma, senza offset ("2026-10-20T06:00"). Internamente si lavora in
UTC: un orario locale inesistente (cambio d'ora di marzo) slitta in avanti dell'ora mancante, uno
ripetuto (ottobre) vale la prima volta che si presenta.
"""
from __future__ import annotations

import copy
import uuid
from datetime import date, datetime, time, timedelta, timezone

from zoneinfo import ZoneInfo

UTC = timezone.utc
DAYS = ["lun", "mar", "mer", "gio", "ven", "sab", "dom"]
ON, OFF, MAINT = "acceso", "spento", "manutenzione"
LOCAL_FMT = "%Y-%m-%dT%H:%M"
MAX_INTERVAL_DAYS = 14
KEEP_ENDED_DAYS = 7


class PlanError(ValueError):
    pass


def default_plan() -> dict:
    return {
        "versione": 1,
        "fuso": "Europe/Rome",
        "default": {"giorni": DAYS[:5], "accensione": "08:00", "spegnimento": "19:00", "anticipo_db_minuti": 15},
        "intervalli": [],
        "manutenzione": None,
        "risorse": {"ecs": [], "rds": []},
        "storico": [],
    }


# ---------------------------------------------------------------- orari

def tz(plan: dict) -> ZoneInfo:
    return ZoneInfo(plan.get("fuso") or "Europe/Rome")


def to_utc(naive: datetime, zone: ZoneInfo) -> datetime:
    """Ora locale senza fuso -> UTC, con le due notti del cambio d'ora gestite."""
    aware = naive.replace(tzinfo=zone, fold=0)
    back = aware.astimezone(UTC).astimezone(zone)
    if back.replace(tzinfo=None) != naive:
        # Ora che non esiste (es. 02:30 dell'ultima domenica di marzo): zoneinfo la legge con
        # l'offset di prima del salto, cioe' la porta all'ora equivalente dopo il salto (03:30).
        return back.astimezone(UTC)
    return aware.astimezone(UTC)


def parse_local(text: str, zone: ZoneInfo) -> datetime:
    text = text.strip().replace(" ", "T")
    try:
        naive = datetime.strptime(text[:16], LOCAL_FMT)
    except ValueError as exc:
        raise PlanError(f"data e ora non valide: {text!r} (atteso AAAA-MM-GG HH:MM)") from exc
    return to_utc(naive, zone)


def format_local(moment: datetime, zone: ZoneInfo) -> str:
    return moment.astimezone(zone).strftime(LOCAL_FMT)


def _hhmm(text: str) -> time:
    try:
        return datetime.strptime(text, "%H:%M").time()
    except ValueError as exc:
        raise PlanError(f"ora non valida: {text!r} (atteso HH:MM)") from exc


def _window(plan: dict, day: date, zone: ZoneInfo) -> tuple[datetime, datetime] | None:
    d = plan["default"]
    if DAYS[day.weekday()] not in d.get("giorni", []):
        return None
    start = to_utc(datetime.combine(day, _hhmm(d["accensione"])), zone)
    end = to_utc(datetime.combine(day, _hhmm(d["spegnimento"])), zone)
    return (start, end) if end > start else None


# ---------------------------------------------------------------- validazione

def validate(plan: dict) -> None:
    """Controlli di forma: un piano non valido non deve mai far accendere o spegnere niente."""
    if not isinstance(plan, dict):
        raise PlanError("il piano non e' un oggetto JSON")
    zone = tz(plan)
    d = plan.get("default")
    if not isinstance(d, dict):
        raise PlanError("manca 'default'")
    for g in d.get("giorni", []):
        if g not in DAYS:
            raise PlanError(f"giorno sconosciuto: {g!r}")
    _hhmm(d.get("accensione", "")), _hhmm(d.get("spegnimento", ""))
    if _hhmm(d["spegnimento"]) <= _hhmm(d["accensione"]):
        raise PlanError("lo spegnimento di default deve venire dopo l'accensione")
    int(d.get("anticipo_db_minuti", 0))
    for it in plan.get("intervalli", []):
        if it.get("stato") not in (ON, OFF):
            raise PlanError(f"intervallo {it.get('id')}: stato {it.get('stato')!r}")
        if parse_local(it["al"], zone) <= parse_local(it["dal"], zone):
            raise PlanError(f"intervallo {it.get('id')}: 'al' deve venire dopo 'dal'")
    m = plan.get("manutenzione")
    if m is not None:
        parse_local(m["fino"], zone)
    r = plan.get("risorse")
    if not isinstance(r, dict):
        raise PlanError("manca 'risorse'")


# ---------------------------------------------------------------- stato voluto

def _merged_on_intervals(plan: dict, zone: ZoneInfo) -> list[tuple[datetime, datetime, str]]:
    """Intervalli 'acceso' gia' fusi con la finestra del loro giorno (regola di unione)."""
    out = []
    for it in plan.get("intervalli", []):
        if it.get("stato") != ON:
            continue
        start, end = parse_local(it["dal"], zone), parse_local(it["al"], zone)
        win = _window(plan, start.astimezone(zone).date(), zone)
        # Tocca, si sovrappone o finisce prima della finestra dello stesso giorno: si fonde.
        if win and start <= win[1]:
            start, end = min(start, win[0]), max(end, win[1])
        out.append((start, end, it.get("id", "?")))
    return out


def _maintenance(plan: dict, now: datetime, zone: ZoneInfo) -> dict | None:
    m = plan.get("manutenzione")
    if m and parse_local(m["fino"], zone) > now:
        return m
    return None


def state_at(plan: dict, moment: datetime) -> tuple[str, str]:
    """(acceso|spento, motivo) in un istante, senza la manutenzione."""
    zone = tz(plan)
    for it in plan.get("intervalli", []):
        if it.get("stato") == OFF and parse_local(it["dal"], zone) <= moment < parse_local(it["al"], zone):
            return OFF, f"intervallo spento {it.get('id')}"
    for start, end, ident in _merged_on_intervals(plan, zone):
        if start <= moment < end:
            return ON, f"intervallo acceso {ident}"
    day = moment.astimezone(zone).date()
    for dd in (day - timedelta(days=1), day):
        win = _window(plan, dd, zone)
        if win and win[0] <= moment < win[1]:
            return ON, "finestra di default"
    return OFF, "fuori orario"


def desired_state(plan: dict, now: datetime) -> tuple[str, str]:
    """(acceso|spento|manutenzione, motivo) adesso. La manutenzione vince su tutto."""
    m = _maintenance(plan, now, tz(plan))
    if m:
        return MAINT, f"manutenzione: {m.get('motivo', '')} (fino {m['fino']})"
    return state_at(plan, now)


def _boundaries(plan: dict, start: datetime, end: datetime) -> list[datetime]:
    zone = tz(plan)
    points = set()
    day = start.astimezone(zone).date() - timedelta(days=1)
    while day <= end.astimezone(zone).date() + timedelta(days=1):
        win = _window(plan, day, zone)
        if win:
            points.update(win)
        day += timedelta(days=1)
    for it in plan.get("intervalli", []):
        points.add(parse_local(it["dal"], zone))
        points.add(parse_local(it["al"], zone))
    for s, e, _ in _merged_on_intervals(plan, zone):
        points.update((s, e))
    return sorted(p for p in points if start < p <= end)


def next_events(plan: dict, now: datetime, days: int = 7) -> list[dict]:
    """Accensioni e spegnimenti fra adesso e `days` giorni, calcolati con state_at."""
    zone = tz(plan)
    horizon = now + timedelta(days=days)
    events = []
    current, _ = state_at(plan, now)
    for point in _boundaries(plan, now, horizon):
        new, why = state_at(plan, point)
        if new != current:
            events.append({"quando": point, "locale": format_local(point, zone),
                           "evento": "accensione" if new == ON else "spegnimento", "motivo": why})
            current = new
    return events


def db_should_run(plan: dict, now: datetime) -> bool:
    """DB acceso adesso, oppure fra poco (anticipo_db_minuti) inizia un periodo acceso."""
    state, _ = desired_state(plan, now)
    if state in (ON, MAINT):
        return True
    ahead = timedelta(minutes=int(plan["default"].get("anticipo_db_minuti", 0)))
    if not ahead:
        return False
    return any(e["evento"] == "accensione" and e["quando"] <= now + ahead
               for e in next_events(plan, now, days=1))


# ---------------------------------------------------------------- comandi

def _now_minute(now: datetime) -> datetime:
    return now.replace(second=0, microsecond=0)


def _interval(plan: dict, state: str, start: datetime, end: datetime, note: str, actor: str,
              now: datetime) -> dict:
    zone = tz(plan)
    return {"id": uuid.uuid4().hex[:6], "stato": state, "dal": format_local(start, zone),
            "al": format_local(end, zone), "nota": note, "creato_da": actor,
            "creato_il": now.astimezone(zone).isoformat(timespec="seconds")}


def next_window_start(plan: dict, now: datetime) -> datetime | None:
    zone = tz(plan)
    day = now.astimezone(zone).date()
    for i in range(15):
        win = _window(plan, day + timedelta(days=i), zone)
        if win and win[0] > now:
            return win[0]
    return None


def cmd_power_on(plan: dict, now: datetime, actor: str, until: datetime | None = None,
                 default_hours: int = 3, note: str = "accendi ora") -> dict:
    now = _now_minute(now)
    if until is None:
        zone = tz(plan)
        win = _window(plan, now.astimezone(zone).date(), zone)
        until = win[1] if win and now < win[1] else now + timedelta(hours=default_hours)
    return _interval(plan, ON, now, until, note, actor, now)


def cmd_power_off(plan: dict, now: datetime, actor: str, until: datetime | None = None,
                  note: str = "spegni ora") -> dict:
    now = _now_minute(now)
    if until is None:
        until = next_window_start(plan, now) or now + timedelta(days=7)
    return _interval(plan, OFF, now, until, note, actor, now)


def cmd_interval(plan: dict, state: str, start: datetime, end: datetime, actor: str, now: datetime,
                 note: str = "") -> dict:
    if state not in (ON, OFF):
        raise PlanError(f"stato {state!r}")
    return _interval(plan, state, start, end, note, actor, now)


def check_interval(plan: dict, it: dict) -> list[str]:
    """Problemi da far confermare (non bloccanti): intervalli lunghi."""
    zone = tz(plan)
    start, end = parse_local(it["dal"], zone), parse_local(it["al"], zone)
    if end <= start:
        raise PlanError("'al' deve venire dopo 'dal'")
    if end - start > timedelta(days=MAX_INTERVAL_DAYS):
        return [f"l'intervallo dura piu' di {MAX_INTERVAL_DAYS} giorni"]
    return []


def with_interval(plan: dict, it: dict) -> dict:
    new = copy.deepcopy(plan)
    new.setdefault("intervalli", []).append(it)
    return new


def without_interval(plan: dict, ident: str) -> dict:
    new = copy.deepcopy(plan)
    new["intervalli"] = [i for i in new.get("intervalli", []) if i.get("id") != ident]
    return new


def with_maintenance(plan: dict, now: datetime, hours: int, reason: str, actor: str) -> dict:
    new = copy.deepcopy(plan)
    new["manutenzione"] = {"motivo": reason, "fino": format_local(now + timedelta(hours=hours), tz(plan)),
                           "creato_da": actor}
    return new


def without_maintenance(plan: dict) -> dict:
    new = copy.deepcopy(plan)
    new["manutenzione"] = None
    return new


def prune(plan: dict, now: datetime, keep_history: int = 50) -> dict:
    """Toglie gli intervalli finiti da piu' di 7 giorni, la manutenzione scaduta, lo storico in eccesso."""
    new = copy.deepcopy(plan)
    zone = tz(new)
    limit = now - timedelta(days=KEEP_ENDED_DAYS)
    new["intervalli"] = [i for i in new.get("intervalli", []) if parse_local(i["al"], zone) > limit]
    if new.get("manutenzione") and parse_local(new["manutenzione"]["fino"], zone) <= now:
        new["manutenzione"] = None
    new["storico"] = (new.get("storico") or [])[-keep_history:]
    return new


def add_history(plan: dict, now: datetime, actor: str, kind: str, text: str, keep: int = 50) -> dict:
    new = copy.deepcopy(plan)
    hist = new.setdefault("storico", [])
    hist.append({"quando": now.astimezone(tz(new)).isoformat(timespec="seconds"), "chi": actor,
                 "tipo": kind, "cosa": text})
    new["storico"] = hist[-keep:]
    return new
