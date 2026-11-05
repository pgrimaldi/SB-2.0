"""Costi dell'account da Cost Explorer: mese in corso e mese precedente, per servizio.

Cost Explorer e' dell'intero account (dev, prod e legacy insieme): non si separa per ambiente perche'
le risorse non hanno tutte un tag di costo. Ogni chiamata a Cost Explorer costa 0,01 USD.
"""
from __future__ import annotations

import logging
from dataclasses import dataclass
from datetime import date, timedelta

from app.env.awsutil import Aws
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)


@dataclass
class ServiceCost:
    service: str
    current: float
    previous: float


def _month_start(d: date) -> date:
    return d.replace(day=1)


def monthly_by_service(aws: Aws) -> tuple[list[ServiceCost], str, dict]:
    """(righe, valuta, intervalli) per il mese in corso e quello precedente."""
    today = date.today()
    cur_start = _month_start(today)
    prev_start = _month_start(cur_start - timedelta(days=1))
    end = today + timedelta(days=1)            # Cost Explorer vuole la fine esclusiva
    ce = aws.session.client("ce", region_name="us-east-1")
    res = ce.get_cost_and_usage(TimePeriod={"Start": prev_start.isoformat(), "End": end.isoformat()},
                                Granularity="MONTHLY", Metrics=["UnblendedCost"],
                                GroupBy=[{"Type": "DIMENSION", "Key": "SERVICE"}])
    totals: dict[str, list[float]] = {}
    unit = "USD"
    for i, period in enumerate(res.get("ResultsByTime", [])[:2]):
        for g in period.get("Groups", []):
            amount = g["Metrics"]["UnblendedCost"]
            unit = amount.get("Unit", unit)
            totals.setdefault(g["Keys"][0], [0.0, 0.0])[1 - i] = float(amount["Amount"])
    rows = sorted((ServiceCost(k, v[0], v[1]) for k, v in totals.items() if v[0] or v[1]),
                  key=lambda r: -r.current)
    log.info("Costi: mese in corso %.2f %s, mese precedente %.2f %s", sum(r.current for r in rows), unit,
             sum(r.previous for r in rows), unit)
    return rows, unit, {"corrente": f"{cur_start} - {today}", "precedente": f"{prev_start} - {cur_start - timedelta(days=1)}"}
