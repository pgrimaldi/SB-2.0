"""Logging: file per corsa in `logs/`, console, e console della GUI.

Righe identiche consecutive compresse in una con suffisso ` x N` (stesso criterio di 01/02, qui
in versione ridotta).
Copia consapevole e non import da 01/02/05: le utility devono poter essere spostate separatamente.
"""
from __future__ import annotations

import logging
import sys
import threading
from datetime import datetime
from pathlib import Path
from typing import Callable, Optional

LOGGER_NAME = "console-sistema"


class DedupeWrapperHandler(logging.Handler):
    """Comprime righe consecutive con stesso livello e testo; emette al cambio o dopo `delay` s."""

    def __init__(self, target: logging.Handler, delay: float = 2.0):
        super().__init__()
        self._target = target
        self._delay = delay
        self._pending: logging.LogRecord | None = None
        self._body = ""
        self._count = 0
        self._timer: threading.Timer | None = None
        self._guard = threading.RLock()

    def emit(self, record: logging.LogRecord) -> None:
        body = record.getMessage()
        with self._guard:
            if self._pending is not None and body == self._body and record.levelno == self._pending.levelno:
                self._count += 1
                return
            self._flush()
            self._pending, self._body, self._count = record, body, 1
            self._timer = threading.Timer(self._delay, self.flush)
            self._timer.daemon = True
            self._timer.start()

    def _flush(self) -> None:
        if self._timer is not None:
            self._timer.cancel()
            self._timer = None
        if self._pending is None:
            return
        record = self._pending
        if self._count > 1:
            record.msg, record.args = f"{self._body} x {self._count}", None
        self._pending = None
        try:
            self._target.emit(record)
        except Exception:  # noqa: BLE001
            self.handleError(record)

    def flush(self) -> None:
        with self._guard:
            self._flush()
        self._target.flush()

    def close(self) -> None:
        self.flush()
        self._target.close()
        super().close()

    def setFormatter(self, fmt) -> None:  # noqa: N802
        self._target.setFormatter(fmt)


class CallbackHandler(logging.Handler):
    def __init__(self, callback: Callable[[str, str], None]):
        super().__init__()
        self._callback = callback

    def emit(self, record: logging.LogRecord) -> None:
        try:
            self._callback(record.levelname, self.format(record))
        except Exception:  # noqa: BLE001
            self.handleError(record)


def setup_logging(log_dir: Path, gui_callback: Optional[Callable[[str, str], None]] = None) -> Path:
    logger = logging.getLogger(LOGGER_NAME)
    logger.setLevel(logging.INFO)
    for h in list(logger.handlers):
        logger.removeHandler(h)
        h.close()

    fmt = logging.Formatter("%(asctime)s %(levelname)-7s %(message)s", "%H:%M:%S")
    log_dir.mkdir(parents=True, exist_ok=True)
    log_file = log_dir / f"run_{datetime.now():%Y%m%d_%H%M%S}.log"

    handlers: list[logging.Handler] = [
        logging.FileHandler(log_file, encoding="utf-8"),
        logging.StreamHandler(sys.stdout),
    ]
    if gui_callback is not None:
        handlers.append(CallbackHandler(gui_callback))
    for h in handlers:
        wrapped = DedupeWrapperHandler(h)
        wrapped.setFormatter(fmt)
        logger.addHandler(wrapped)
    return log_file
