"""Log dell'ambiente su CloudWatch: ricerca per testo e intervallo, esportazione.

I gruppi sono quelli degli output dello stack (`log.gruppi_output`) piu' quelli elencati a mano
(`log.gruppi`). La ricerca usa il filtro di CloudWatch Logs (testo semplice o sintassi dei filter
pattern); si ferma a `limite` righe.
"""
from __future__ import annotations

import logging
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path

from botocore.exceptions import ClientError

from app.env.awsutil import Aws, Progress, check_cancel, no_progress, stack_info
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)


@dataclass
class LogLine:
    when: datetime
    stream: str
    message: str


def groups(aws: Aws) -> list[str]:
    s = aws.settings
    out = stack_info(aws).outputs
    names = [out[o] for o in s.log.gruppi_output if out.get(o)]
    return names + [g for g in s.log.gruppi if g not in names]


def search(aws: Aws, group: str, text: str, minutes: int, cancel=None, progress: Progress = no_progress,
           limit: int = 2000) -> list[LogLine]:
    if group not in groups(aws):
        raise ValueError(f"Gruppo {group} non fra quelli dell'ambiente")
    start = int((datetime.now(timezone.utc) - timedelta(minutes=minutes)).timestamp() * 1000)
    kw = {"logGroupName": group, "startTime": start}
    if text:
        # testo libero fra virgolette (frase esatta), a meno che non sia gia' un filter pattern
        kw["filterPattern"] = text if text[:1] in ('"', "{", "[", "?", "%") else f'"{text}"'
    lines: list[LogLine] = []
    cw = aws.client("logs")
    try:
        while True:
            check_cancel(cancel)
            res = cw.filter_log_events(**kw)
            for e in res.get("events", []):
                lines.append(LogLine(datetime.fromtimestamp(e["timestamp"] / 1000, timezone.utc),
                                     e.get("logStreamName", ""), e["message"].rstrip()))
            progress("log", 0, 0, f"{len(lines)} righe")
            if len(lines) >= limit or not res.get("nextToken"):
                break
            kw["nextToken"] = res["nextToken"]
    except ClientError as exc:
        if exc.response["Error"]["Code"] == "ResourceNotFoundException":
            log.warning("Gruppo di log %s inesistente", group)
            return []
        raise
    lines.sort(key=lambda x: x.when)
    log.info("Log %s, ultimi %d minuti%s: %d righe", group, minutes, f", filtro {text!r}" if text else "",
             len(lines))
    return lines[-limit:]


def export(lines: list[LogLine], dest: Path) -> Path:
    dest.parent.mkdir(parents=True, exist_ok=True)
    with dest.open("w", encoding="utf-8") as f:
        for x in lines:
            f.write(f"{x.when:%Y-%m-%d %H:%M:%S} [{x.stream}] {x.message}\n")
    return dest
