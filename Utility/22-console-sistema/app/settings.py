"""Caricamento della configurazione (config/settings.local.json).

Stessa impostazione delle altre utility: `settings.default.json` e' versionato e non contiene
segreti, `settings.local.json` e' gitignorato ed e' quello che si legge. Se manca, lo si crea
copiando il default: cosi' F5 funziona anche senza aver lanciato `setup.ps1`.

La configurazione ha una parte comune (profilo AWS, repository, PostgreSQL locale, backup, scansione)
e un blocco per ambiente (`ambienti.dev`, `ambienti.prod`). `EnvSettings` unisce le due parti per
l'ambiente scelto ed espone gli stessi nomi di campo che usano i moduli di `app/env/`.
"""
from __future__ import annotations

import json
import shutil
import sys
from dataclasses import dataclass, field
from pathlib import Path

APP_DIR = (Path(sys.executable).parent if getattr(sys, "frozen", False)
           else Path(__file__).resolve().parent.parent)
# Sorgenti da impacchettare nella Lambda: accanto al codice, o dentro l'eseguibile.
SOURCE_DIR = Path(getattr(sys, "_MEIPASS", APP_DIR))
CONFIG_DIR = APP_DIR / "config"
DEFAULT_SETTINGS_PATH = CONFIG_DIR / "settings.default.json"
LOCAL_SETTINGS_PATH = CONFIG_DIR / "settings.local.json"

ENVIRONMENTS = ("dev", "prod")


@dataclass
class AwsSettings:
    profile: str = "michele"
    region: str = "eu-south-1"


@dataclass
class RepositorySettings:
    percorso: str = ""
    remoto: str = "origin"
    branch: str = "dev"


@dataclass
class BackendSettings:
    build_project: str = ""
    ecr_repository: str = ""
    cartella: str = "backend/summerbooking-backend"
    moduli_migration: list[str] = field(default_factory=list)
    health_path: str = "/health"
    minuti_attesa_health: int = 5
    minuti_attesa_servizio: int = 15
    migration: str = "applica"          # applica = dal PC con conferma; blocca = il rilascio si ferma


@dataclass
class FrontendSettings:
    nome: str = "front office"
    build_project: str = ""
    cartella: str = "frontend/summer-booking-front-office"
    configurazioni: list[str] = field(default_factory=lambda: ["production"])
    output_url: str = "FrontendUrl"
    build_output: str = "FrontendBuildProject"


@dataclass
class LocalDbSettings:
    host: str = "localhost"
    port: int = 5432
    utente: str = "postgres"
    bin: str = "C:/Program Files/PostgreSQL/17/bin"
    escludi: list[str] = field(default_factory=lambda: ["postgres"])


@dataclass
class DatabaseSettings:
    db_secret: str = ""
    locale: LocalDbSettings = field(default_factory=LocalDbSettings)
    schemi: list[str] = field(default_factory=list)
    tabelle_seed: list[str] = field(default_factory=list)
    lavori_paralleli_restore: int = 2
    operazioni_distruttive: bool = False
    utente_lettura: str = "sb_lettura"

    @property
    def secret_lettura(self) -> str:
        return f"{self.db_secret}-lettura"


@dataclass
class S3Settings:
    bucket_output: list[str] = field(default_factory=list)
    sola_lettura: list[str] = field(default_factory=list)
    max_elementi_elenco: int = 5000


@dataclass
class ArchiveSettings:
    bucket_output: str = "ArchiveBucketName"
    prefisso: str = "archivio/"
    righe_anteprima: int = 200
    origine: str = ""


@dataclass
class LogSettings:
    gruppi_output: list[str] = field(default_factory=list)
    gruppi: list[str] = field(default_factory=list)


@dataclass
class InfraSettings:
    template: str = ""
    parametri: dict = field(default_factory=dict)
    protezione_cancellazione: bool = True


@dataclass
class BackupSettings:
    cartella: str = "data/backup"
    formato: str = "custom"
    compressione: int = 6
    tieni_su_s3: bool = False
    prefisso_s3: str = "dump/backup/"


@dataclass
class EnvSettings:
    """Configurazione di UN ambiente, con la parte comune gia' unita."""
    name: str = "dev"
    descrizione: str = ""
    aws: AwsSettings = field(default_factory=AwsSettings)
    account_atteso: str = ""
    stack: str = ""
    gruppi_prefisso: str = ""
    # orario: vuoto = l'ambiente non ha accensione/spegnimento (produzione)
    plan_parameter: str = ""
    plan_parameter_tier: str = "Intelligent-Tiering"
    lambda_name: str = ""
    log_group: str = ""
    schedule_name: str = ""
    ore_accensione_manuale: int = 3
    giorni_prossimi_eventi: int = 7
    storico_max: int = 50
    ore_massime_manutenzione: int = 3
    repository: RepositorySettings = field(default_factory=RepositorySettings)
    backend: BackendSettings = field(default_factory=BackendSettings)
    frontends: list[FrontendSettings] = field(default_factory=list)
    database: DatabaseSettings = field(default_factory=DatabaseSettings)
    s3: S3Settings = field(default_factory=S3Settings)
    archivi: ArchiveSettings = field(default_factory=ArchiveSettings)
    log: LogSettings = field(default_factory=LogSettings)
    backup: BackupSettings = field(default_factory=BackupSettings)
    infra: InfraSettings = field(default_factory=InfraSettings)
    log_dir: str = "logs"
    work_dir: str = "data"

    @property
    def is_prod(self) -> bool:
        return self.name == "prod"

    @property
    def has_schedule(self) -> bool:
        return bool(self.plan_parameter and self.lambda_name)

    @property
    def log_path(self) -> Path:
        return _abs(self.log_dir)

    @property
    def work_path(self) -> Path:
        return _abs(self.work_dir)

    @property
    def backup_path(self) -> Path:
        return _abs(self.backup.cartella) / self.name

    @property
    def template_path(self) -> Path:
        return _abs(self.infra.template).resolve()

    def frontend(self, nome: str) -> FrontendSettings:
        for fe in self.frontends:
            if fe.nome == nome:
                return fe
        raise ValueError(f"Frontend {nome!r} non configurato per l'ambiente {self.name}")

    def check_names(self) -> None:
        """Ogni nome di risorsa deve contenere il nome dell'ambiente e non quello dell'altro.

        Protegge da una configurazione copiata male: un blocco `dev` che punta a risorse di
        produzione (o il contrario) fa rifiutare la partenza.
        """
        names = {
            "stack": self.stack, "plan_parameter": self.plan_parameter, "lambda_name": self.lambda_name,
            "log_group": self.log_group, "schedule_name": self.schedule_name,
            "backend.build_project": self.backend.build_project,
            "backend.ecr_repository": self.backend.ecr_repository,
            "database.db_secret": self.database.db_secret,
            "gruppi_prefisso": self.gruppi_prefisso,
            **{f"frontends[{fe.nome}].build_project": fe.build_project for fe in self.frontends},
        }
        others = [e for e in ENVIRONMENTS if e != self.name]
        bad = [f"{k} = {v!r}" for k, v in names.items()
               if v and (self.name not in v.lower() or any(o in v.lower() for o in others))]
        if bad:
            raise ValueError(f"Configurazione dell'ambiente {self.name} rifiutata: questi nomi non contengono "
                             f"'{self.name}' o contengono il nome di un altro ambiente:\n" + "\n".join(bad))


@dataclass
class Settings:
    """Configurazione completa: la parte comune piu' i blocchi grezzi degli ambienti."""
    aws: AwsSettings = field(default_factory=AwsSettings)
    account_atteso: str = ""
    ambiente_iniziale: str = "dev"
    repository: RepositorySettings = field(default_factory=RepositorySettings)
    locale_pg: LocalDbSettings = field(default_factory=LocalDbSettings)
    backup: BackupSettings = field(default_factory=BackupSettings)
    ambienti: dict[str, dict] = field(default_factory=dict)
    scan: dict = field(default_factory=dict)
    log_dir: str = "logs"
    work_dir: str = "data"
    path: Path = LOCAL_SETTINGS_PATH

    @property
    def log_path(self) -> Path:
        return _abs(self.log_dir)

    @property
    def work_path(self) -> Path:
        return _abs(self.work_dir)

    def env(self, name: str) -> EnvSettings:
        if name not in self.ambienti:
            raise ValueError(f"Ambiente {name!r} non configurato (ambienti: {', '.join(self.ambienti)})")
        data = self.ambienti[name]
        db = data.get("database") or {}
        database = _pick(DatabaseSettings, {k: v for k, v in db.items() if k != "locale"})
        database.locale = self.locale_pg
        top = {k: v for k, v in data.items()
               if k not in ("aws", "repository", "backend", "frontends", "database", "s3", "archivi", "log",
                            "orario", "backup", "infra")}
        orario = data.get("orario") or {}
        e = _pick(EnvSettings, {**top, **{k: v for k, v in orario.items() if k in EnvSettings.__dataclass_fields__}})
        e.name = name
        e.aws = self.aws
        e.account_atteso = self.account_atteso
        e.repository = RepositorySettings(self.repository.percorso, self.repository.remoto,
                                          data.get("branch") or self.repository.branch)
        e.backend = _pick(BackendSettings, data.get("backend"))
        e.frontends = [_pick(FrontendSettings, f) for f in data.get("frontends") or []]
        e.database = database
        e.s3 = _pick(S3Settings, data.get("s3"))
        e.archivi = _pick(ArchiveSettings, data.get("archivi"))
        e.log = _pick(LogSettings, data.get("log"))
        e.backup = self.backup
        e.infra = _pick(InfraSettings, data.get("infra"))
        if e.backend.migration not in ("applica", "blocca"):
            raise ValueError(f"ambienti.{name}.backend.migration: 'applica' oppure 'blocca'")
        e.log_dir, e.work_dir = self.log_dir, self.work_dir
        e.check_names()
        return e


def _abs(p: str) -> Path:
    path = Path(p)
    return path if path.is_absolute() else APP_DIR / path


def _pick(cls, data: dict | None):
    """Costruisce la dataclass ignorando le chiavi di commento (`_...`) e quelle sconosciute."""
    known = cls.__dataclass_fields__.keys()
    return cls(**{k: v for k, v in (data or {}).items() if k in known})


def load_settings(path: Path | None = None) -> Settings:
    if path is None:
        path = LOCAL_SETTINGS_PATH
        if not path.exists():
            shutil.copyfile(DEFAULT_SETTINGS_PATH, path)
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    s = _pick(Settings, {k: v for k, v in data.items()
                         if k not in ("aws", "repository", "locale_pg", "backup", "ambienti", "scan")})
    s.aws = _pick(AwsSettings, data.get("aws"))
    s.repository = _pick(RepositorySettings, data.get("repository"))
    s.locale_pg = _pick(LocalDbSettings, data.get("locale_pg"))
    s.backup = _pick(BackupSettings, data.get("backup"))
    s.ambienti = {k: v for k, v in (data.get("ambienti") or {}).items() if not k.startswith("_")}
    s.scan = data.get("scan") or {}
    s.path = Path(path)
    for name in s.ambienti:
        s.env(name)          # valida subito tutti gli ambienti: un errore di configurazione si vede all'avvio
    return s
