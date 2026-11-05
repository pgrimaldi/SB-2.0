"""Configurazione della scansione (blocco `scan` di config/settings.local.json).

Stessi campi della utility 12; il profilo AWS e' quello comune della utility (`aws.profile`).
"""
from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path

from app.settings import APP_DIR

SECTIONS = ("json", "details", "iam", "s3", "secrets", "stack_templates", "iac_generator",
            "lambda_code", "ecr_images", "other_artifacts")


@dataclass
class AwsSettings:
    profile: str = ""
    home_region: str = "eu-south-1"
    max_workers: int = 12
    max_attempts: int = 10
    connect_timeout: int = 10
    read_timeout: int = 120


@dataclass
class RegionSettings:
    mode: str = "auto"
    always: list[str] = field(default_factory=lambda: ["eu-south-1"])
    list: list[str] = field(default_factory=list)
    exclude: list[str] = field(default_factory=list)


@dataclass
class IacSettings:
    reuse_recent_scan_minutes: int = 0
    poll_seconds: int = 10
    timeout_minutes: int = 60
    max_resources_per_template: int = 500
    delete_generated_templates: bool = True
    include_managed_by_stack: bool = False


@dataclass
class LambdaSettings:
    all_versions: bool = False
    all_layer_versions: bool = False


@dataclass
class EcrSettings:
    max_images_per_repo: int = 0
    include_untagged: bool = False
    repositories: list[str] = field(default_factory=list)


@dataclass
class Settings:
    aws: AwsSettings = field(default_factory=AwsSettings)
    regions: RegionSettings = field(default_factory=RegionSettings)
    sections: dict[str, bool] = field(default_factory=lambda: {s: True for s in SECTIONS})
    services: list[str] = field(default_factory=list)
    iac: IacSettings = field(default_factory=IacSettings)
    lambda_: LambdaSettings = field(default_factory=LambdaSettings)
    ecr: EcrSettings = field(default_factory=EcrSettings)
    output_dir: str = "data"
    log_dir: str = "logs"

    @property
    def log_path(self) -> Path:
        return APP_DIR / self.log_dir

    @property
    def output_path(self) -> Path:
        p = Path(self.output_dir)
        return p if p.is_absolute() else APP_DIR / p

    def on(self, section: str) -> bool:
        return bool(self.sections.get(section, False))


def _pick(cls, data: dict):
    """Costruisce la dataclass ignorando le chiavi di commento (`_...`) e quelle sconosciute."""
    known = cls.__dataclass_fields__.keys()
    return cls(**{k: v for k, v in (data or {}).items() if k in known})


def from_dict(data: dict, profile: str) -> Settings:
    """Settings della scansione dal blocco `scan`, con il profilo comune della utility."""
    sections = {s: True for s in SECTIONS}
    sections.update({k: bool(v) for k, v in (data.get("sections") or {}).items() if k in SECTIONS})
    aws = _pick(AwsSettings, data.get("aws"))
    aws.profile = profile
    return Settings(
        aws=aws,
        regions=_pick(RegionSettings, data.get("regions")),
        sections=sections,
        services=list(data.get("services") or []),
        iac=_pick(IacSettings, data.get("iac")),
        lambda_=_pick(LambdaSettings, data.get("lambda")),
        ecr=_pick(EcrSettings, data.get("ecr")),
        output_dir=data.get("output_dir", "data/scan"),
        log_dir="logs",
    )
