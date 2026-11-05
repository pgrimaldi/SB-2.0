"""Caricamento della configurazione (config/settings.local.json).

Stessa impostazione delle altre utility: `settings.default.json` e' versionato e non contiene
segreti, `settings.local.json` e' gitignorato ed e' quello che si legge. Se manca, lo si crea
copiando il default: cosi' F5 funziona anche senza aver lanciato `setup.ps1`.
"""
from __future__ import annotations

import json
import shutil
import sys
from dataclasses import dataclass, field
from pathlib import Path

APP_DIR = (Path(sys.executable).parent if getattr(sys, "frozen", False)
           else Path(__file__).resolve().parent.parent)
# Da eseguibile i file aggiunti con --add-data stanno nella cartella interna di PyInstaller.
RUOLI_DIR = Path(getattr(sys, "_MEIPASS", APP_DIR)) / "app" / "ruoli"
CONFIG_DIR = APP_DIR / "config"
DEFAULT_SETTINGS_PATH = CONFIG_DIR / "settings.default.json"
LOCAL_SETTINGS_PATH = CONFIG_DIR / "settings.local.json"


@dataclass
class AwsSettings:
    profile: str = ""
    region: str = "eu-south-1"


@dataclass
class TagSettings:
    chiave: str = "sb:gestito-da"
    valore: str = "utility-14"


@dataclass
class Settings:
    aws: AwsSettings = field(default_factory=AwsSettings)
    account_atteso: str = ""
    tag_gestione: TagSettings = field(default_factory=TagSettings)
    lunghezza_password: int = 20
    cartella_schede: str = "data"
    log_dir: str = "logs"

    @property
    def log_path(self) -> Path:
        return APP_DIR / self.log_dir

    @property
    def schede_path(self) -> Path:
        p = Path(self.cartella_schede)
        return p if p.is_absolute() else APP_DIR / p


def _pick(cls, data: dict):
    """Costruisce la dataclass ignorando le chiavi di commento (`_...`) e quelle sconosciute."""
    known = cls.__dataclass_fields__.keys()
    return cls(**{k: v for k, v in (data or {}).items() if k in known})


def load_settings(path: Path | None = None) -> Settings:
    if path is None:
        path = LOCAL_SETTINGS_PATH
        if not path.exists():
            shutil.copyfile(DEFAULT_SETTINGS_PATH, path)
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    s = _pick(Settings, {k: v for k, v in data.items() if k not in ("aws", "tag_gestione")})
    s.aws = _pick(AwsSettings, data.get("aws"))
    s.tag_gestione = _pick(TagSettings, data.get("tag_gestione"))
    return s
