"""Linguette di supporto: log, scansione dell'infrastruttura, costi, ruoli."""
from __future__ import annotations

import logging
import tkinter as tk
from datetime import datetime
from tkinter import messagebox, ttk
from tkinter.scrolledtext import ScrolledText

from app import roles as R
from app.env import costs as C
from app.env import logs as L
from app.env.awsutil import Cancelled
from app.gui.shell import Tab
from app.logging_setup import LOGGER_NAME
from app.scan import awsutil as scan_aws
from app.scan import settings as scan_settings
from app.scan.scanner import SECTION_LABELS, check_access, run_scan

log = logging.getLogger(LOGGER_NAME)


# ==================================================================== log

class LogsTab(Tab):
    title = "Log"
    features = ("log",)

    def build(self, tab: ttk.Frame) -> None:
        self._lines: list[L.LogLine] = []
        r = ttk.Frame(tab)
        r.pack(fill="x")
        ttk.Label(r, text="Gruppo:").pack(side="left")
        self.group = tk.StringVar()
        self.group_combo = ttk.Combobox(r, textvariable=self.group, width=42, state="readonly")
        self.group_combo.pack(side="left", padx=4)
        self.gate(ttk.Button(r, text="Leggi gruppi", command=self.do_groups), "log").pack(side="left", padx=4)
        ttk.Label(r, text="Ultimi minuti:").pack(side="left", padx=(12, 2))
        self.minutes = tk.IntVar(value=60)
        ttk.Spinbox(r, from_=5, to=10080, increment=15, textvariable=self.minutes, width=6).pack(side="left")
        ttk.Label(r, text="Testo o filter pattern:").pack(side="left", padx=(12, 2))
        self.text = tk.StringVar()
        ttk.Entry(r, textvariable=self.text, width=40).pack(side="left", padx=2)
        self.gate(ttk.Button(r, text="Cerca", command=self.do_search), "log", lambda: bool(self.group.get())).pack(
            side="left", padx=4)
        self.gate(ttk.Button(r, text="Esporta…", command=self.do_export), "log", lambda: bool(self._lines)).pack(
            side="left", padx=4)
        self.out = ScrolledText(tab, height=14, font=("Consolas", 9), state="disabled", wrap="none")
        self.out.pack(fill="both", expand=True, pady=(6, 0))
        ttk.Label(tab, text="Esempi: ERROR · \"Unhandled exception\" · { $.level = \"Error\" } (log JSON). "
                            "Al massimo 2000 righe, le piu' recenti.", foreground="#666").pack(anchor="w")

    def on_env_changed(self) -> None:
        self._lines = []
        self.group_combo["values"] = []
        self.group.set("")
        self._write("")

    def autoload(self):
        if self.app.allowed("log") and not self.group_combo["values"]:
            return lambda then: self.do_groups(then, silent=True)
        return None

    def _write(self, text: str) -> None:
        self.out.configure(state="normal")
        self.out.delete("1.0", "end")
        self.out.insert("end", text)
        self.out.configure(state="disabled")

    def do_groups(self, then=None, silent: bool = False) -> None:
        def job(ctx):
            names = L.groups(ctx.aws)

            def show():
                self.group_combo["values"] = names
                if names and not self.group.get():
                    self.group.set(names[0])
                self.app._refresh_buttons()

            self.app.render(show)

        self.app.run("Gruppi di log", job, then=then, quiet=True, clear_steps=False, silent=silent)

    def do_search(self) -> None:
        group, text, minutes = self.group.get(), self.text.get().strip(), int(self.minutes.get())

        def job(ctx):
            lines = L.search(ctx.aws, group, text, minutes, ctx.cancel, ctx.progress)

            def show():
                self._lines = lines
                self._write("\n".join(f"{x.when:%Y-%m-%d %H:%M:%S}  {x.message}" for x in lines)
                            or "(nessuna riga)")
                self.out.see("end")
                self.app._refresh_buttons()

            self.app.render(show)

        self.app.run("Ricerca nei log", job, quiet=True, clear_steps=False)

    def do_export(self) -> None:
        dest = self.env.work_path / "log" / self.env.name / f"log_{datetime.now():%Y%m%d_%H%M%S}.txt"
        L.export(self._lines, dest)
        self.app.open_folder(dest.parent)


# ==================================================================== scansione

class ScanTab(Tab):
    title = "Scansione"
    features = ("scan",)

    def build(self, tab: ttk.Frame) -> None:
        cfg = scan_settings.from_dict(self.app.settings.scan, self.app.settings.aws.profile)
        ttk.Label(tab, text="Fotografia dell'intero account AWS (tutti gli ambienti e il legacy): configurazioni, IAM, "
                            "template, codice Lambda, immagini ECR, segreti IN CHIARO. Niente righe di DB ne' oggetti S3. "
                            "Stessa logica della utility 12.", foreground="#666", wraplength=1300,
                  justify="left").pack(anchor="w")
        f = ttk.LabelFrame(tab, text=" Sezioni ", padding=6)
        f.pack(fill="x", pady=(6, 0))
        self.sections = {k: tk.BooleanVar(value=cfg.on(k)) for k in SECTION_LABELS}
        for i, (k, label) in enumerate(SECTION_LABELS.items()):
            ttk.Checkbutton(f, text=label, variable=self.sections[k]).grid(row=i // 4, column=i % 4, sticky="w",
                                                                          padx=(0, 16))
        o = ttk.Frame(tab)
        o.pack(fill="x", pady=(6, 0))
        ttk.Label(o, text="Region (auto oppure elenco separato da virgole):").pack(side="left")
        self.regions = tk.StringVar(value="auto")
        ttk.Entry(o, textvariable=self.regions, width=40).pack(side="left", padx=4)
        ttk.Label(o, text="Immagini ECR per repository (0 = tutte):").pack(side="left", padx=(12, 2))
        self.ecr_max = tk.IntVar(value=cfg.ecr.max_images_per_repo)
        ttk.Spinbox(o, from_=0, to=100, textvariable=self.ecr_max, width=5).pack(side="left")
        ttk.Label(o, text="Riusa scansione IaC piu' recente di (min):").pack(side="left", padx=(12, 2))
        self.iac_reuse = tk.IntVar(value=cfg.iac.reuse_recent_scan_minutes)
        ttk.Spinbox(o, from_=0, to=1440, increment=30, textvariable=self.iac_reuse, width=6).pack(side="left")
        b = ttk.Frame(tab)
        b.pack(fill="x", pady=(8, 0))
        self.gate(ttk.Button(b, text="Verifica accesso e region", command=self.do_check), "scan").pack(side="left")
        self.gate(ttk.Button(b, text="Avvia scansione", command=self.do_scan), "scan").pack(side="left", padx=6)
        ttk.Button(b, text="Apri cartella delle scansioni", command=lambda: self.app.open_folder(cfg.output_path)).pack(
            side="left", padx=6)
        ttk.Label(tab, text=f"Output: {cfg.output_path}  (una cartella per corsa, contiene segreti: non va condivisa)",
                  foreground="#666").pack(anchor="w", pady=(6, 0))

    def _settings(self):
        cfg = scan_settings.from_dict(self.app.settings.scan, self.app.settings.aws.profile)
        cfg.sections = {k: v.get() for k, v in self.sections.items()}
        cfg.ecr.max_images_per_repo = int(self.ecr_max.get())
        cfg.iac.reuse_recent_scan_minutes = int(self.iac_reuse.get())
        text = self.regions.get().strip()
        override = None if text.lower() in ("", "auto") else [x.strip() for x in text.split(",") if x.strip()]
        return cfg, override

    def _step(self, key, scope, label, status, items, notes) -> None:
        self.app.step_cb(key, f"{scope} {label}".strip(" -"), status, f"{items} {notes}".strip())

    def do_check(self) -> None:
        cfg, override = self._settings()

        def job(ctx):
            _, ident, regions, reasons = check_access(cfg, self.app.progress_cb, override)
            return f"Account {ident['Account']}\n{ident['Arn']}\n\n" + "\n".join(f"{r}: {reasons[r]}" for r in regions)

        self.app.run("Verifica accesso", job)

    def do_scan(self) -> None:
        cfg, override = self._settings()
        chosen = [SECTION_LABELS[k] for k, v in cfg.sections.items() if v]
        if not chosen:
            messagebox.showwarning("Scansione", "Nessuna sezione scelta.", parent=self.app)
            return
        if not messagebox.askyesno("Scansione", "Sezioni:\n" + "\n".join(f"• {c}" for c in chosen)
                                   + f"\n\nRegion: {self.regions.get()}\nDestinazione: {cfg.output_path}\n\n"
                                   "Sola lettura sull'account (l'IaC generator crea e poi cancella i template "
                                   "generati). Procedere?", parent=self.app):
            return

        def job(ctx):
            try:
                path = run_scan(cfg, ctx.cancel, self.app.progress_cb, self._step, override)
            except scan_aws.Cancelled as exc:
                raise Cancelled() from exc
            self.app.render(lambda: self.app.open_folder(path))
            return f"Scansione completata:\n{path}\n\nVedi riepilogo.md nella cartella."

        self.app.run("Scansione dell'infrastruttura", job)


# ==================================================================== costi

class CostsTab(Tab):
    title = "Costi"
    features = ("costi",)

    def build(self, tab: ttk.Frame) -> None:
        r = ttk.Frame(tab)
        r.pack(fill="x")
        self.gate(ttk.Button(r, text="Leggi costi", command=self.do_costs), "costi").pack(side="left")
        self.total_var = tk.StringVar(value="Costi dell'intero account (tutti gli ambienti e il legacy), da Cost "
                                            "Explorer. Ogni lettura costa 0,01 USD.")
        ttk.Label(r, textvariable=self.total_var).pack(side="left", padx=10)
        cols = (("servizio", 420), ("mese in corso", 130), ("mese precedente", 130))
        self.tree = ttk.Treeview(tab, columns=[c for c, _ in cols], show="headings", height=12)
        for c, w in cols:
            self.tree.heading(c, text=c)
            self.tree.column(c, width=w, stretch=c == "servizio", anchor="w" if c == "servizio" else "e")
        self.tree.pack(fill="both", expand=True, pady=(6, 0))

    def do_costs(self) -> None:
        def job(ctx):
            rows, unit, periods = C.monthly_by_service(ctx.aws)

            def show():
                self.tree.delete(*self.tree.get_children())
                for x in rows:
                    self.tree.insert("", "end", values=(x.service, f"{x.current:,.2f} {unit}", f"{x.previous:,.2f} {unit}"))
                self.total_var.set(f"Totale mese in corso ({periods['corrente']}): {sum(x.current for x in rows):,.2f} "
                                   f"{unit} - mese precedente ({periods['precedente']}): "
                                   f"{sum(x.previous for x in rows):,.2f} {unit}")

            self.app.render(show)

        self.app.run("Costi", job, quiet=True, clear_steps=False)


# ==================================================================== ruoli

class RolesTab(Tab):
    title = "Ruoli"
    features = ()

    def build(self, tab: ttk.Frame) -> None:
        ttk.Label(tab, text="Funzioni della utility per l'ambiente scelto e ruoli (gruppi IAM della utility 14) che le "
                            "abilitano. Il permesso vero lo decide IAM: qui si decide solo cosa mostrare attivo.",
                  foreground="#666").pack(anchor="w")
        cols = (("funzione", 300), ("stato", 90), ("ruoli", 700))
        self.tree = ttk.Treeview(tab, columns=[c for c, _ in cols], show="headings", height=14)
        for c, w in cols:
            self.tree.heading(c, text=c)
            self.tree.column(c, width=w, stretch=c == "ruoli")
        self.tree.tag_configure("ok", background="#e3f5e1")
        self.tree.tag_configure("no", foreground="#888")
        self.tree.pack(fill="both", expand=True, pady=(6, 0))
        self.groups_var = tk.StringVar(value="Gruppi dell'utente: -")
        ttk.Label(tab, textvariable=self.groups_var, wraplength=1300, justify="left").pack(anchor="w", pady=(6, 0))

    def visible(self) -> bool:
        return True

    def on_identity(self) -> None:
        me, env = self.app.identity, self.app.env
        self.tree.delete(*self.tree.get_children())
        for f in R.FEATURES.values():
            ok = R.allowed(me, env, f.key)
            roles = R.why_not(me, env, f.key).split(": ", 1)[-1]
            self.tree.insert("", "end", tags=("ok" if ok else "no",),
                             values=(f.label, "abilitata" if ok else "no", roles))
        if me:
            self.groups_var.set(f"Gruppi dell'utente {me.user or me.arn}: {', '.join(sorted(me.groups)) or 'nessuno'}"
                                + (" - AdministratorAccess agganciata direttamente" if me.admin_direct else ""))

    def on_env_changed(self) -> None:
        if self.app.identity:
            self.on_identity()
