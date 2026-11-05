"""Repository SB-2.0 in sola lettura: fetch, commit, archivio di un commit, worktree temporaneo.

Si pubblica sempre un commit del remoto, mai la cartella di lavoro: modifiche non committate o non
pushate non finiscono online. Il worktree per le migration sta in una cartella temporanea fuori dal
repository e si rimuove sempre.
"""
from __future__ import annotations

import logging
import re
import shutil
import subprocess
import tempfile
from contextlib import contextmanager
from dataclasses import dataclass
from pathlib import Path

from app.env.awsutil import OperationError
from app.logging_setup import LOGGER_NAME
from app.settings import EnvSettings as Settings

log = logging.getLogger(LOGGER_NAME)

_NO_WINDOW = getattr(subprocess, "CREATE_NO_WINDOW", 0)


@dataclass
class Commit:
    sha: str
    short: str
    date: str
    author: str
    subject: str

    def label(self) -> str:
        return f"{self.short}  {self.date}  {self.author}: {self.subject}"


class Repo:
    def __init__(self, settings: Settings):
        self.s = settings.repository
        if not self.s.percorso:
            raise OperationError("Percorso del repository non impostato (repository.percorso in "
                                 "config/settings.local.json): serve per il deploy")
        self.path = Path(self.s.percorso)
        if not (self.path / ".git").exists():
            raise OperationError(f"Repository non trovato in {self.path} (repository.percorso)")

    def git(self, *args: str, check: bool = True) -> str:
        res = subprocess.run(["git", "-C", str(self.path), *args], capture_output=True, text=True,
                             encoding="utf-8", errors="replace", creationflags=_NO_WINDOW)
        if check and res.returncode != 0:
            raise OperationError(f"git {' '.join(args)}: {res.stderr.strip() or res.stdout.strip()}")
        return res.stdout

    def ref(self, branch: str) -> str:
        return f"{self.s.remoto}/{branch}"

    def fetch(self, branch: str) -> None:
        self.git("fetch", "--quiet", self.s.remoto, branch)

    def commits(self, branch: str, limit: int = 40) -> list[Commit]:
        out = self.git("log", self.ref(branch), f"-{limit}", "--date=format:%Y-%m-%d %H:%M",
                       "--format=%H%x1f%h%x1f%ad%x1f%an%x1f%s")
        result = []
        for line in out.splitlines():
            sha, short, date, author, subject = line.split("\x1f", 4)
            result.append(Commit(sha, short, date, author, subject))
        return result

    def resolve(self, rev: str) -> str:
        return self.git("rev-parse", "--verify", f"{rev}^{{commit}}").strip()

    def commit(self, rev: str) -> Commit:
        out = self.git("log", "-1", rev, "--date=format:%Y-%m-%d %H:%M", "--format=%H%x1f%h%x1f%ad%x1f%an%x1f%s")
        sha, short, date, author, subject = out.strip().split("\x1f", 4)
        return Commit(sha, short, date, author, subject)

    def behind(self, old: str, new: str) -> int | None:
        """Commit che `old` ha in meno di `new`; None se `old` non e' nella storia."""
        if not old:
            return None
        if subprocess.run(["git", "-C", str(self.path), "cat-file", "-e", f"{old}^{{commit}}"],
                          capture_output=True, creationflags=_NO_WINDOW).returncode != 0:
            return None
        return int(self.git("rev-list", "--count", f"{old}..{new}").strip() or 0)

    def changed(self, old: str, new: str, pattern: str) -> list[str]:
        out = self.git("diff", "--name-only", f"{old}..{new}", "--", pattern)
        return [x for x in out.splitlines() if x]

    def archive(self, sha: str, subdir: str, dest: Path) -> Path:
        dest.parent.mkdir(parents=True, exist_ok=True)
        # I file come sono nel commit: con core.autocrlf attivo git archive li convertirebbe in CRLF,
        # e la build su Linux leggerebbe "22.22.3\r" da .nvmrc.
        self.git("-c", "core.autocrlf=false", "archive", "--format=zip", "-o", str(dest), sha,
                 subdir.rstrip("/") + "/")
        return dest

    def migration_ids(self, sha: str, backend_dir: str, module: str) -> list[str]:
        """Id delle migration di un modulo in un commit (nomi dei file, senza i .Designer)."""
        base = f"{backend_dir}/Module.{module}/Infrastructure/Migrations/"
        out = self.git("ls-tree", "--name-only", f"{sha}", base, check=False)
        names = [Path(x).name for x in out.splitlines()]
        return sorted(n[:-3] for n in names
                      if re.match(r"^\d{14}_.+\.cs$", n) and not n.endswith(".Designer.cs"))

    @contextmanager
    def worktree(self, sha: str):
        tmp = Path(tempfile.mkdtemp(prefix="sbdev-wt-"))
        target = tmp / "repo"
        self.git("worktree", "add", "--detach", str(target), sha)
        try:
            yield target
        finally:
            self.git("worktree", "remove", "--force", str(target), check=False)
            self.git("worktree", "prune", check=False)
            shutil.rmtree(tmp, ignore_errors=True)


def image_tag(branch: str, sha: str) -> str:
    return f"{re.sub(r'[^A-Za-z0-9_.-]', '-', branch)}-{sha[:12]}"


def commit_from_tag(tag: str) -> str:
    m = re.search(r"-([0-9a-f]{7,40})$", tag or "")
    return m.group(1) if m else ""
