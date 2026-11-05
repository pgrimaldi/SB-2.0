"""Retention sull'ambiente: il comando `retention` del backend, lanciato come task Fargate.

Il backend non ha uno scheduler: la retention si esegue a mano con `Boot -- retention [--apply]`. Qui
lo si lancia con la task definition del backend (stessa immagine e stessa configurazione del servizio:
connessioni, bucket di archivio e destinazioni delle cancellazioni dell'ambiente) cambiando solo il
comando. Senza `--apply` e' una prova: valuta le policy e scrive il verbale, ma non tocca dati ne' file.

Il resoconto e' quello stampato dal comando, letto dal log del task. Il verbale completo resta in
`privacy.privacy_retention_runs`.
"""
from __future__ import annotations

import logging
import re

from app.env import database as D
from app.env.awsutil import OperationError, stack_info
from app.env.operations import OK, WARN, Ctx
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)

_SCHEMA = re.compile(r"^[a-z_]{2,30}$")
# I log dell'host .NET hanno prefissi tipo "info: Categoria[0]": per il resoconto si tengono le righe utili.
_NOISE = ("info: Microsoft.", "warn: Microsoft.", "      Executed DbCommand", "      Entity Framework")


def run(ctx: Ctx, apply: bool, schema: str | None = None, seasonal: bool = True) -> tuple[str, list[str]]:
    """Esegue la retention e restituisce (riassunto, righe del resoconto)."""
    aws, s = ctx.aws, ctx.s
    if schema and not _SCHEMA.match(schema):
        raise OperationError(f"Schema non valido: {schema!r}")
    st = stack_info(aws)
    if not st.has_db:
        raise OperationError(f"Il DB dell'ambiente {s.name} non esiste")
    out = dict(st.outputs)
    family, log_group, *_ = st.need("TaskDefinitionFamily", "AppLogGroup", "ClusterName", "SubnetIds",
                                    "AppSecurityGroupId")
    args = ["retention"] + (["--apply"] if apply else []) + (["--schema", schema] if schema else []) \
        + ([] if seasonal else ["--no-seasonal"])
    label = f"Retention {'ESECUZIONE REALE' if apply else 'prova'}{f' ({schema})' if schema else ''}"
    with ctx.phase("retention", label):
        lines, status, code = D.run_db_task(aws, out, {}, ctx.cancel, ctx.progress, "Verbale completo:", "retention",
                                            command=args, family=family, container="api", log_group=log_group)
        report = [x for x in lines if not x.startswith(_NOISE)]
        summary = next((x for x in report if x.startswith("policy:")), "")
        rows = next((x for x in report if x.startswith("righe trovate:")), "")
        errors = [x for x in report if x.startswith("  - ")]
        if code is None:
            raise OperationError(f"Il task si e' fermato senza codice d'uscita ({status}).\n" + "\n".join(report[-15:]))
        if code == 1:
            raise OperationError("La retention non e' partita.\n" + "\n".join(report[-20:]))
        text = f"{summary}; {rows}".strip("; ") or f"uscita {code}"
        ctx.step("retention", label, OK if code == 0 else WARN,
                 text + (f"; {len(errors)} policy in errore" if errors else ""))
    log.info("%s: %s (uscita %s)", label, text, code)
    return text, report


def last_runs(aws, limit: int = 100) -> tuple[list[str], list[tuple]]:
    """Ultime corse dal verbale (con l'utente in sola lettura se c'e')."""
    from app.env import dbadmin as M  # noqa: PLC0415

    cols, rows, _ = M.run_query(aws, f"select * from privacy.privacy_retention_runs order by 1 desc limit {int(limit)}")
    return cols, rows
