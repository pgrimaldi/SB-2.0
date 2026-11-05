"""Scaricamento di file da URL prefirmati (codice Lambda e layer, blob ECR) con verifica dell'hash."""
from __future__ import annotations

import base64
import hashlib
import os
import time
import urllib.request
from pathlib import Path

CHUNK = 1024 * 1024


def download(url: str, dest: Path, sha256_hex: str | None = None, sha256_b64: str | None = None,
             headers: dict | None = None, attempts: int = 3, cancel=None) -> int:
    """Scarica in `dest` (via file .part + rename) e verifica lo sha256 se dato. Ritorna i byte."""
    dest.parent.mkdir(parents=True, exist_ok=True)
    tmp = dest.with_name(dest.name + ".part")
    last: Exception | None = None
    for attempt in range(1, attempts + 1):
        try:
            h = hashlib.sha256()
            size = 0
            req = urllib.request.Request(url, headers=headers or {})
            with urllib.request.urlopen(req, timeout=120) as resp, open(tmp, "wb") as fh:  # noqa: S310
                while True:
                    if cancel is not None and cancel.is_set():
                        raise InterruptedError("interrotto")
                    buf = resp.read(CHUNK)
                    if not buf:
                        break
                    fh.write(buf)
                    h.update(buf)
                    size += len(buf)
            if sha256_hex and h.hexdigest() != sha256_hex.lower():
                raise ValueError(f"sha256 diverso: atteso {sha256_hex}, scaricato {h.hexdigest()}")
            if sha256_b64 and base64.b64encode(h.digest()).decode() != sha256_b64:
                raise ValueError("CodeSha256 diverso da quello dichiarato da AWS")
            os.replace(tmp, dest)
            return size
        except InterruptedError:
            tmp.unlink(missing_ok=True)
            raise
        except Exception as exc:  # noqa: BLE001
            last = exc
            tmp.unlink(missing_ok=True)
            if attempt < attempts:
                time.sleep(2 * attempt)
    raise RuntimeError(f"download fallito dopo {attempts} tentativi: {last}") from last


def human(n: float) -> str:
    for unit in ("B", "KB", "MB", "GB", "TB"):
        if n < 1024 or unit == "TB":
            return f"{n:,.0f} {unit}" if unit == "B" else f"{n:,.1f} {unit}"
        n /= 1024
    return f"{n} B"
