"""Linguette dell'ambiente: stato e orario, deploy, database (solo dev), accesso al DB.

Riprendono la utility 20, estesa a piu' ambienti: in produzione non ci sono orario e operazioni
distruttive sul DB, e ogni scrittura si conferma scrivendo il nome dello stack.
"""
from __future__ import annotations

import logging
import tkinter as tk
import webbrowser
from tkinter import messagebox, ttk

from app.env import database as D
from app.env import operations as O
from app.env import plan as P
from app.env.awsutil import stack_info
from app.env.power import apply_now, env_status, install_reconciler, now_utc, save_plan
from app.env.repo import Repo, commit_from_tag
from app.gui.dialogs import CreateDbDialog, OpenCidrDialog, PowerOffDialog, UpdateDbDialog
from app.gui.shell import Tab
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)


def _has_db(app) -> bool:
    st = getattr(app, "env_status", None)
    return bool(st and st.stack.has_db)


# ==================================================================== stato e orario

class StatusTab(Tab):
    title = "Stato e orario"
    features = ("stato", "orario")

    def build(self, tab: ttk.Frame) -> None:
        app = self.app
        app.env_status = None
        self._doc = None

        st = ttk.LabelFrame(tab, text=" Stato attuale ", padding=8)
        st.pack(fill="x")
        self.vars = {k: tk.StringVar(value="-") for k in
                     ("stack", "db", "svc", "voluto", "giro", "frontend", "api", "lambda", "manut", "immagine")}
        rows = (("Stack", "stack"), ("DB", "db"), ("Servizi", "svc"), ("Immagine", "immagine"),
                ("Voluto adesso", "voluto"), ("Manutenzione", "manut"), ("Ultimo giro Lambda", "giro"),
                ("Riconciliatore", "lambda"))
        for i, (label, key) in enumerate(rows):
            ttk.Label(st, text=label + ":").grid(row=i % 4, column=(i // 4) * 2, sticky="w", padx=(0, 4))
            ttk.Label(st, textvariable=self.vars[key], width=54).grid(row=i % 4, column=(i // 4) * 2 + 1, sticky="w")
        for i, (label, key) in enumerate((("Frontend", "frontend"), ("API", "api"))):
            ttk.Label(st, text=label + ":").grid(row=i, column=4, sticky="w", padx=(12, 4))
            lbl = tk.Label(st, textvariable=self.vars[key], fg="#1a56db", cursor="hand2")
            lbl.grid(row=i, column=5, sticky="w")
            lbl.bind("<Button-1>", lambda _e, k=key: self._open_url(self.vars[k].get()))
        btns = ttk.Frame(st)
        btns.grid(row=0, column=6, rowspan=4, sticky="ne", padx=(16, 0))
        is_admin = lambda: bool(app.identity and app.identity.is_admin)  # noqa: E731
        sched = lambda: self.env.has_schedule  # noqa: E731
        self.gate(ttk.Button(btns, text="Aggiorna", command=self.do_refresh), "stato|orario").pack(fill="x", pady=1)
        self.gate(ttk.Button(btns, text="Applica ora", command=self.do_apply), "orario", sched).pack(fill="x", pady=1)
        self.gate(ttk.Button(btns, text="Installa riconciliatore", command=self.do_install), "orario",
                  lambda: sched() and is_admin()).pack(fill="x", pady=1)

        self.sched_box = ttk.Frame(tab)
        self.sched_box.pack(fill="both", expand=True, pady=(6, 0))
        self.no_sched = ttk.Label(tab, text="Questo ambiente non ha accensione e spegnimento programmati: e' sempre "
                                            "acceso.", foreground="#666")
        self._build_schedule(self.sched_box)
        self.on_env_changed()

    def _build_schedule(self, tab: ttk.Frame) -> None:
        top = ttk.Frame(tab)
        top.pack(fill="x")
        g = lambda w: self.gate(w, "orario", lambda: self.env.has_schedule)  # noqa: E731

        fd = ttk.LabelFrame(top, text=" Programmazione di default ", padding=8)
        fd.pack(side="left", fill="y")
        self.day_vars = {d: tk.BooleanVar(value=d in P.DAYS[:5]) for d in P.DAYS}
        days = ttk.Frame(fd)
        days.pack(anchor="w")
        for d in P.DAYS:
            ttk.Checkbutton(days, text=d, variable=self.day_vars[d]).pack(side="left")
        r = ttk.Frame(fd)
        r.pack(anchor="w", pady=(6, 0))
        self.on_var, self.off_var, self.adv_var = tk.StringVar(value="08:00"), tk.StringVar(value="19:00"), tk.StringVar(value="15")
        for label, var, w in (("Accensione", self.on_var, 6), ("Spegnimento", self.off_var, 6),
                              ("Anticipo DB (min)", self.adv_var, 4)):
            ttk.Label(r, text=label).pack(side="left", padx=(0, 3))
            ttk.Entry(r, textvariable=var, width=w).pack(side="left", padx=(0, 10))
        g(ttk.Button(fd, text="Salva", command=self.do_save_default)).pack(anchor="e", pady=(6, 0))

        fc = ttk.LabelFrame(top, text=" Comandi (ore di Roma, AAAA-MM-GG HH:MM) ", padding=8)
        fc.pack(side="left", fill="both", expand=True, padx=8)
        r1 = ttk.Frame(fc)
        r1.pack(anchor="w")
        g(ttk.Button(r1, text="Accendi ora", command=lambda: self.do_power_on(False))).pack(side="left")
        g(ttk.Button(r1, text="Spegni ora…", command=lambda: self.do_power_off(False))).pack(side="left", padx=6)
        ttk.Label(r1, text="  fino a").pack(side="left")
        self.until_var = tk.StringVar()
        ttk.Entry(r1, textvariable=self.until_var, width=17).pack(side="left", padx=4)
        g(ttk.Button(r1, text="Accendi fino a", command=lambda: self.do_power_on(True))).pack(side="left")
        g(ttk.Button(r1, text="Spegni fino a…", command=lambda: self.do_power_off(True))).pack(side="left", padx=6)
        r2 = ttk.Frame(fc)
        r2.pack(anchor="w", pady=(8, 0))
        ttk.Label(r2, text="Intervallo:").pack(side="left")
        self.int_state = tk.StringVar(value=P.ON)
        ttk.Combobox(r2, textvariable=self.int_state, values=[P.ON, P.OFF], width=8, state="readonly").pack(
            side="left", padx=4)
        self.int_from, self.int_to, self.int_note = tk.StringVar(), tk.StringVar(), tk.StringVar()
        for label, var, w in (("dal", self.int_from, 17), ("al", self.int_to, 17), ("nota", self.int_note, 24)):
            ttk.Label(r2, text=label).pack(side="left", padx=(6, 2))
            ttk.Entry(r2, textvariable=var, width=w).pack(side="left")
        g(ttk.Button(r2, text="Aggiungi intervallo", command=self.do_add_interval)).pack(side="left", padx=6)

        tables = ttk.Frame(tab)
        tables.pack(fill="both", expand=True, pady=(6, 0))
        fi = ttk.LabelFrame(tables, text=" Intervalli manuali ", padding=4)
        fi.pack(side="left", fill="both", expand=True)
        cols = (("stato", 70), ("dal", 120), ("al", 120), ("nota", 180), ("creato_da", 150), ("creato_il", 150))
        self.intervals = ttk.Treeview(fi, columns=[c for c, _ in cols], show="headings", height=5)
        for c, w in cols:
            self.intervals.heading(c, text=c.replace("_", " "))
            self.intervals.column(c, width=w, stretch=c == "nota")
        self.intervals.pack(fill="both", expand=True)
        g(ttk.Button(fi, text="Elimina selezionato", command=self.do_delete_interval)).pack(anchor="e", pady=(4, 0))
        fe = ttk.LabelFrame(tables, text=" Prossimi eventi ", padding=4)
        fe.pack(side="left", fill="both", expand=True, padx=(8, 0))
        self.events_tree = ttk.Treeview(fe, columns=("quando", "evento", "motivo"), show="headings", height=6)
        for c, w in (("quando", 150), ("evento", 100), ("motivo", 220)):
            self.events_tree.heading(c, text=c)
            self.events_tree.column(c, width=w, stretch=c == "motivo")
        self.events_tree.pack(fill="both", expand=True)

    # ---------------------------------------------------------------- ambiente e ruoli
    def on_env_changed(self) -> None:
        self.app.env_status = None
        self._doc = None
        for v in self.vars.values():
            v.set("-")
        self.intervals.delete(*self.intervals.get_children())
        self.events_tree.delete(*self.events_tree.get_children())
        if self.env.has_schedule:
            self.no_sched.pack_forget()
            if not self.sched_box.winfo_manager():
                self.sched_box.pack(fill="both", expand=True, pady=(6, 0))
        else:
            self.sched_box.pack_forget()
            self.no_sched.pack(anchor="w", pady=(8, 0))

    def autoload(self):
        if self.app.allowed("stato|orario") and self.app.env_status is None:
            return lambda then: self.do_refresh(then, silent=True)
        return None

    # ---------------------------------------------------------------- stato
    def do_refresh(self, then=None, silent: bool = False) -> None:
        def job(ctx: O.Ctx):
            st = env_status(ctx.aws)
            self.app.render(lambda: self._show(st))

        self.app.run("Stato dell'ambiente", job, then=then, clear_steps=False, quiet=True, silent=silent)

    def _show(self, st) -> None:
        app = self.app
        app.env_status, self._doc = st, st.doc
        info = st.db_info
        self.vars["stack"].set(f"{st.stack.name}: {st.stack.status}")
        self.vars["db"].set(st.db_status + (f"  ({info.get('DBInstanceClass')}, PostgreSQL {info.get('EngineVersion')})"
                                            if info else ""))
        self.vars["svc"].set(", ".join(f"{k}: " + ("assente" if v is None else f"{v['running']}/{v['desired']}")
                                       for k, v in st.services.items()) or "-")
        self.vars["immagine"].set(st.stack.image_tag or "nessuna immagine rilasciata")
        self.vars["voluto"].set(f"{st.desired[0]} - {st.desired[1]}")
        self.vars["frontend"].set(st.stack.outputs.get("FrontendUrl", "-"))
        self.vars["api"].set(st.stack.outputs.get("ApiUrl", "-"))
        if st.doc is None:
            for k in ("manut", "giro", "lambda"):
                self.vars[k].set("-")
            app._refresh_buttons()
            return
        z = P.tz(st.doc.plan)
        m = st.doc.plan.get("manutenzione")
        self.vars["manut"].set(f"{m['motivo']} (fino {m['fino']})" if m else "no")
        lr = st.last_run
        self.vars["giro"].set(f"{P.format_local(lr['_quando'], z).replace('T', ' ')}  {lr.get('esito')}  "
                              f"{'; '.join(lr.get('azioni') or []) or lr.get('motivo') or ''}"[:110]
                              if lr else "nessuno nelle ultime 24 ore")
        self.vars["lambda"].set({True: "CODICE SEGNAPOSTO: premere «Installa riconciliatore»", False: "installato",
                                 None: "non trovato"}[st.lambda_placeholder])
        d = st.doc.plan["default"]
        for day, var in self.day_vars.items():
            var.set(day in d.get("giorni", []))
        self.on_var.set(d["accensione"])
        self.off_var.set(d["spegnimento"])
        self.adv_var.set(str(d.get("anticipo_db_minuti", 0)))
        self.intervals.delete(*self.intervals.get_children())
        for it in st.doc.plan.get("intervalli", []):
            self.intervals.insert("", "end", iid=it["id"], values=(
                it["stato"], it["dal"].replace("T", " "), it["al"].replace("T", " "), it.get("nota", ""),
                it.get("creato_da", "").rsplit("/", 1)[-1], it.get("creato_il", "")[:16].replace("T", " ")))
        self.events_tree.delete(*self.events_tree.get_children())
        for e in st.events:
            self.events_tree.insert("", "end", values=(e["locale"].replace("T", " "), e["evento"], e["motivo"]))
        app._refresh_buttons()

    @staticmethod
    def _open_url(url: str) -> None:
        if url.startswith("http"):
            webbrowser.open(url)

    def do_apply(self) -> None:
        self.app.run("Applica ora", lambda ctx: f"Esito del giro: {apply_now(ctx.aws)}", then=self.do_refresh)

    def do_install(self) -> None:
        if not messagebox.askyesno("Installa riconciliatore",
                                   f"Carico il codice vero (index.py + plan.py + tzdata) nella Lambda "
                                   f"{self.env.lambda_name}. Da qui l'orario diventa effettivo: fuori dalla "
                                   f"finestra l'ambiente verra' spento.\n\nProcedere?", parent=self.app):
            return

        def job(ctx):
            install_reconciler(ctx.aws)
            return f"Riconciliatore installato. Primo giro: {apply_now(ctx.aws)}"

        self.app.run("Installa riconciliatore", job, then=self.do_refresh)

    # ---------------------------------------------------------------- orario
    def _need_doc(self) -> bool:
        if self._doc is None:
            messagebox.showwarning("Piano", "Piano non ancora letto: premere Aggiorna.", parent=self.app)
            return False
        return True

    def _parse(self, text: str):
        return P.parse_local(text, P.tz(self._doc.plan))

    def _preview(self, new_plan: dict) -> str:
        ev = P.next_events(new_plan, now_utc(), self.env.giorni_prossimi_eventi)
        return "\n".join(f"• {e['locale'].replace('T', ' ')} {e['evento']} ({e['motivo']})" for e in ev[:12]) \
            or "nessun evento"

    def _save(self, new_plan: dict, kind: str, text: str) -> None:
        if not messagebox.askyesno("Conferma", f"{text}\n\nProssimi eventi:\n{self._preview(new_plan)}\n\nSalvare?",
                                   parent=self.app):
            return
        doc = self._doc
        self.app.run("Salvataggio del piano", lambda ctx: (save_plan(ctx.aws, doc, new_plan, kind, text), None)[1],
                     then=self.do_refresh)

    def do_save_default(self) -> None:
        if not self._need_doc():
            return
        giorni = [d for d in P.DAYS if self.day_vars[d].get()]
        if not giorni and not messagebox.askyesno("Giorni vuoti", "Nessun giorno selezionato: l'ambiente restera' "
                                                  "sempre spento (salvo intervalli). Procedere?", parent=self.app):
            return
        new = dict(self._doc.plan)
        try:
            new["default"] = {"giorni": giorni, "accensione": self.on_var.get().strip(),
                              "spegnimento": self.off_var.get().strip(), "anticipo_db_minuti": int(self.adv_var.get())}
            P.validate(new)
        except (ValueError, P.PlanError) as exc:
            messagebox.showerror("Programmazione", str(exc), parent=self.app)
            return
        self._save(new, "default", f"Default: {','.join(giorni)} {new['default']['accensione']}-"
                                   f"{new['default']['spegnimento']}")

    def _until(self, use: bool):
        if not use:
            return None
        text = self.until_var.get().strip()
        if not text:
            raise P.PlanError("indicare la data e ora di 'fino a'")
        return self._parse(text)

    def do_power_on(self, use_until: bool) -> None:
        if not self._need_doc():
            return
        if not _has_db(self.app):
            messagebox.showwarning("DB eliminato", "Il DB su AWS e' stato eliminato: ricrearlo dalla linguetta "
                                   "«Database» (Crea DB).", parent=self.app)
            return
        try:
            until = self._until(use_until)
            it = P.cmd_power_on(self._doc.plan, now_utc(), "(io)", until, self.env.ore_accensione_manuale)
        except P.PlanError as exc:
            messagebox.showerror("Accendi", str(exc), parent=self.app)
            return
        if not messagebox.askyesno("Accendi", f"Intervallo acceso {it['dal']} -> {it['al']}.\n\nProssimi eventi:\n"
                                   f"{self._preview(P.with_interval(self._doc.plan, it))}\n\nProcedere?",
                                   parent=self.app):
            return
        self.app.run("Accendi", lambda ctx: (O.power_on(ctx, until), None)[1], then=self.do_refresh)

    def do_power_off(self, use_until: bool) -> None:
        if not self._need_doc():
            return
        try:
            until = self._until(use_until)
            it = P.cmd_power_off(self._doc.plan, now_utc(), "(io)", until)
        except P.PlanError as exc:
            messagebox.showerror("Spegni", str(exc), parent=self.app)
            return
        can_delete = _has_db(self.app) and self.app.allowed("db_gestione") and self.env.database.operazioni_distruttive
        dlg = PowerOffDialog(self.app, it, can_delete)
        if not dlg.result:
            return
        delete, data_only = dlg.result
        if delete:
            what = "SVUOTARE i dati (restano schema e seed)" if data_only else \
                "ELIMINARE l'istanza RDS senza snapshot (dati persi, si ricrea con Crea DB)"
            if not self.app.confirm_typed(f"Spegnimento e {what}."):
                return
        self.app.run("Spegni", lambda ctx: (O.power_off(ctx, until, delete, data_only), None)[1], then=self.do_refresh)

    def do_add_interval(self) -> None:
        if not self._need_doc():
            return
        try:
            it = P.cmd_interval(self._doc.plan, self.int_state.get(), self._parse(self.int_from.get()),
                                self._parse(self.int_to.get()), "(io)", now_utc(), self.int_note.get().strip())
            warn = P.check_interval(self._doc.plan, it)
        except P.PlanError as exc:
            messagebox.showerror("Intervallo", str(exc), parent=self.app)
            return
        if warn and not messagebox.askyesno("Intervallo", "; ".join(warn) + ". Procedere?", parent=self.app):
            return
        doc = self._doc

        def job(ctx):
            it["creato_da"] = ctx.aws.actor()
            save_plan(ctx.aws, doc, P.with_interval(doc.plan, it), "intervallo", f"{it['stato']} {it['dal']} -> {it['al']}")

        if messagebox.askyesno("Conferma", f"Intervallo {it['stato']} {it['dal']} -> {it['al']}\n\nProssimi eventi:\n"
                               f"{self._preview(P.with_interval(doc.plan, it))}\n\nSalvare?", parent=self.app):
            self.app.run("Aggiungi intervallo", job, then=self.do_refresh)

    def do_delete_interval(self) -> None:
        sel = self.intervals.selection()
        if not sel or not self._need_doc():
            return
        self._save(P.without_interval(self._doc.plan, sel[0]), "elimina", f"Elimina intervallo {sel[0]}")


# ==================================================================== deploy

class DeployTab(Tab):
    title = "Deploy"
    features = ("deploy_backend", "deploy_frontend")

    def build(self, tab: ttk.Frame) -> None:
        self._commits = []
        r = ttk.Frame(tab)
        r.pack(anchor="w", fill="x")
        ttk.Label(r, text="Branch:").pack(side="left")
        self.branch_var = tk.StringVar()
        ttk.Entry(r, textvariable=self.branch_var, width=14).pack(side="left", padx=4)
        self.gate(ttk.Button(r, text="Aggiorna elenco (git fetch)", command=self.do_load_commits),
                  "deploy_backend|deploy_frontend").pack(side="left", padx=4)
        ttk.Label(r, text="Commit da pubblicare:").pack(side="left", padx=(12, 4))
        self.commit_var = tk.StringVar()
        self.commit_combo = ttk.Combobox(r, textvariable=self.commit_var, width=90, state="readonly")
        self.commit_combo.pack(side="left", fill="x", expand=True)

        info = ttk.Frame(tab)
        info.pack(anchor="w", fill="x", pady=8)
        self.code_vars = {k: tk.StringVar(value="-") for k in ("head", "backend", "frontend")}
        for i, (label, key) in enumerate((("Ultimo commit remoto", "head"), ("Backend in esercizio", "backend"),
                                          ("Frontend in esercizio", "frontend"))):
            ttk.Label(info, text=label + ":").grid(row=i, column=0, sticky="nw", padx=(0, 6))
            ttk.Label(info, textvariable=self.code_vars[key], wraplength=1150, justify="left").grid(row=i, column=1,
                                                                                                   sticky="w")
        b = ttk.Frame(tab)
        b.pack(anchor="w", pady=(4, 0))
        self.gate(ttk.Button(b, text="Aggiorna backend", command=self.do_update_backend), "deploy_backend").pack(
            side="left")
        ttk.Label(b, text="   Frontend:").pack(side="left")
        self.fe_name = tk.StringVar()
        self.fe_combo = ttk.Combobox(b, textvariable=self.fe_name, width=16, state="readonly")
        self.fe_combo.pack(side="left", padx=4)
        self.fe_combo.bind("<<ComboboxSelected>>", lambda _e: self._fe_confs())
        ttk.Label(b, text="configurazione:").pack(side="left")
        self.fe_conf = tk.StringVar()
        self.conf_combo = ttk.Combobox(b, textvariable=self.fe_conf, width=12, state="readonly")
        self.conf_combo.pack(side="left", padx=4)
        self.gate(ttk.Button(b, text="Aggiorna frontend", command=self.do_update_frontend), "deploy_frontend").pack(
            side="left", padx=4)
        ttk.Label(tab, text="Si pubblica sempre il commit scelto del remoto: modifiche non committate o non "
                            "pushate non finiscono online. Il repository non viene modificato. Le migration pendenti "
                            "si applicano con una seconda conferma.", foreground="#666").pack(anchor="w", pady=(8, 0))
        self.on_env_changed()

    def on_env_changed(self) -> None:
        self._commits = []
        self.commit_combo["values"] = []
        self.commit_var.set("")
        for v in self.code_vars.values():
            v.set("-")
        self.branch_var.set(self.env.repository.branch)
        names = [fe.nome for fe in self.env.frontends]
        self.fe_combo["values"] = names
        self.fe_name.set(names[0] if names else "")
        self._fe_confs()

    def _fe_confs(self) -> None:
        try:
            confs = self.env.frontend(self.fe_name.get()).configurazioni
        except ValueError:
            confs = []
        self.conf_combo["values"] = confs
        self.fe_conf.set(confs[0] if len(confs) == 1 else "")

    def do_load_commits(self) -> None:
        branch = self.branch_var.get().strip() or self.env.repository.branch
        env = self.env

        def job(ctx):
            repo = Repo(env)
            repo.fetch(branch)
            commits = repo.commits(branch)
            head = commits[0] if commits else None
            st = stack_info(ctx.aws)
            bc = commit_from_tag(st.image_tag)
            be = f"{st.image_tag or 'nessuna immagine'}"
            if bc and head:
                n = repo.behind(bc, head.sha)
                be += f"  -  {repo.commit(bc).label()}  ({'?' if n is None else n} commit indietro)"
            fe_lines = []
            for fe in env.frontends:
                last = O.last_build(ctx.aws, fe.build_project)
                text = "nessuna pubblicazione"
                if last and head:
                    n = repo.behind(last["commit"], head.sha)
                    try:
                        label = repo.commit(last["commit"]).label()
                    except Exception:  # noqa: BLE001 - commit non piu' nel repository locale
                        label = "commit non trovato nel repository"
                    text = (f"{last['commit'][:12]} ({last['env'].get('BUILD_CONFIGURATION')})  -  {label}"
                            f"  ({'?' if n is None else n} commit indietro)")
                fe_lines.append(f"{fe.nome}: {text}")

            def show():
                self._commits = commits
                self.commit_combo["values"] = [c.label() for c in commits]
                if commits:
                    self.commit_combo.current(0)
                self.code_vars["head"].set(head.label() if head else "-")
                self.code_vars["backend"].set(be)
                self.code_vars["frontend"].set("\n".join(fe_lines) or "-")

            self.app.render(show)

        self.app.run("Elenco dei commit", job, quiet=True)

    def _chosen_commit(self):
        idx = self.commit_combo.current()
        if idx < 0 or idx >= len(self._commits):
            messagebox.showwarning("Commit", "Premere «Aggiorna elenco» e scegliere un commit.", parent=self.app)
            return None
        return self._commits[idx]

    def do_update_backend(self) -> None:
        c = self._chosen_commit()
        if not c:
            return
        branch = self.branch_var.get().strip()
        if not self.app.confirm_write("Aggiorna backend",
                                      f"Ambiente {self.env.name} - stack {self.env.stack}\nCommit da pubblicare:\n"
                                      f"{c.label()}\n\nIn esercizio: {self.code_vars['backend'].get()}\n\n"
                                      f"Passi: build dell'immagine in CodeBuild, migration pendenti (con seconda "
                                      f"conferma), rilascio, verifica di /health.\n\nProcedere?"):
            return
        self.app.run("Aggiorna backend", lambda ctx: f"Rilasciato {O.update_backend(ctx, branch, c.sha)}")

    def do_update_frontend(self) -> None:
        c = self._chosen_commit()
        if not c:
            return
        name, conf = self.fe_name.get(), self.fe_conf.get()
        if not name or not conf:
            messagebox.showwarning("Aggiorna frontend", "Scegliere frontend e configurazione.", parent=self.app)
            return
        branch = self.branch_var.get().strip()
        if not self.app.confirm_write("Aggiorna frontend", f"Ambiente {self.env.name}\nFrontend: {name}\nCommit:\n"
                                      f"{c.label()}\n\nConfigurazione: {conf}\n\nProcedere?"):
            return
        self.app.run("Aggiorna frontend",
                     lambda ctx: f"Pubblicato {O.update_frontend(ctx, branch, name, conf, c.sha)[:12]} ({name}, {conf})")


# ==================================================================== database (solo dev)

class DatabaseTab(Tab):
    title = "Database"
    features = ("db_gestione",)

    def build(self, tab: ttk.Frame) -> None:
        self._local_dbs: list[D.DbSummary] = []
        ttk.Label(tab, text="Caricamento del DB su AWS da un PostgreSQL locale. Solo per gli ambienti con "
                            "database.operazioni_distruttive (dev). L'ambiente resta in manutenzione durante il lavoro.",
                  foreground="#666").pack(anchor="w")
        body = ttk.Frame(tab)
        body.pack(fill="both", expand=True, pady=(6, 0))
        fl = ttk.LabelFrame(body, text=" DB del PostgreSQL locale ", padding=4)
        fl.pack(side="left", fill="both", expand=True)
        cols = (("nome", 220), ("dimensione", 90), ("tabelle", 70), ("migration", 300))
        self.local_tree = ttk.Treeview(fl, columns=[c for c, _ in cols], show="headings", height=7)
        for c, w in cols:
            self.local_tree.heading(c, text=c)
            self.local_tree.column(c, width=w, stretch=c == "migration")
        self.local_tree.pack(fill="both", expand=True)
        fb = ttk.Frame(body, padding=(8, 0))
        fb.pack(side="left", fill="y")
        ok = lambda: self.env.database.operazioni_distruttive  # noqa: E731
        has = lambda: ok() and _has_db(self.app)  # noqa: E731
        hasnt = lambda: ok() and getattr(self.app, "env_status", None) is not None and not _has_db(self.app)  # noqa: E731
        self.gate(ttk.Button(fb, text="Leggi DB locali", command=self.do_load_local), "db_gestione", ok).pack(fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Crea DB…", command=self.do_create_db), "db_gestione", hasnt).pack(fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Aggiorna DB…", command=self.do_update_db), "db_gestione", has).pack(fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Svuota dati", command=self.do_empty_db), "db_gestione", has).pack(fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Elimina DB", command=self.do_delete_db), "db_gestione", has).pack(fill="x", pady=1)
        ttk.Label(fb, text="Crea / Aggiorna / Svuota / Elimina\nsi attivano dopo «Aggiorna»\nin «Stato e orario».",
                  foreground="#666").pack(anchor="w", pady=(8, 0))

    def on_env_changed(self) -> None:
        self._local_dbs = []
        self.local_tree.delete(*self.local_tree.get_children())

    def _local(self) -> D.Conn:
        return D.local_conn(self.env, ask=self.app.ask_password)

    def do_load_local(self, then=None) -> None:
        env = self.env

        def job(ctx):
            dbs = D.list_local(env, self._local())

            def show():
                self._local_dbs = dbs
                self.local_tree.delete(*self.local_tree.get_children())
                for db in dbs:
                    self.local_tree.insert("", "end", iid=db.name, values=(db.name, db.size, db.tables,
                                                                           db.migrations_text()))

            self.app.render(show)

        self.app.run("Lettura dei DB locali", job, then=then, quiet=True)

    def _names(self) -> list[str]:
        return [d.name for d in self._local_dbs]

    def _selected(self) -> str | None:
        sel = self.local_tree.selection()
        return sel[0] if sel else None

    @staticmethod
    def _real_data_warning(src: str | None) -> str:
        if src and "dev" in src:
            return f"\n\nATTENZIONE: {src} contiene i dati reali migrati dalla produzione."
        return ""

    def do_create_db(self) -> None:
        if not self._local_dbs:
            self.do_load_local(then=self.do_create_db)
            return
        dlg = CreateDbDialog(self.app, self._names(), self._selected())
        if not dlg.result:
            return
        schema_src, data_src = dlg.result
        if not messagebox.askyesno("Crea DB", f"Creo l'istanza RDS (10-15 minuti) e la carico:\n\n• schema da "
                                   f"{schema_src}\n• dati da {data_src or 'nessuno (solo seed)'}\n\n"
                                   f"L'ambiente resta in manutenzione fino alla fine.{self._real_data_warning(data_src)}"
                                   f"\n\nProcedere?", parent=self.app):
            return
        self.app.run("Crea DB", lambda ctx: (O.create_db(ctx, self._local(), schema_src, data_src), "DB creato e caricato.")[1])

    def do_update_db(self) -> None:
        if not self._local_dbs:
            self.do_load_local(then=self.do_update_db)
            return
        dlg = UpdateDbDialog(self.app, self._names(), self._selected())
        if not dlg.result:
            return
        src, mode = dlg.result
        if not self.app.confirm_typed(f"Aggiornare il DB su AWS da {src} ({mode}): i dati attuali su AWS vengono "
                                      f"sostituiti.{self._real_data_warning(src)}"):
            return
        self.app.run("Aggiorna DB", lambda ctx: (O.update_db(ctx, self._local(), src, mode), "DB aggiornato.")[1])

    def do_empty_db(self) -> None:
        if self.app.confirm_typed("Svuotare i dati del DB su AWS (restano schema, storia delle migration e seed)."):
            self.app.run("Svuota dati", lambda ctx: (O.empty_db(ctx), "Dati svuotati.")[1])

    def do_delete_db(self) -> None:
        if self.app.confirm_typed("ELIMINARE l'istanza RDS senza snapshot. Si ricrea con «Crea DB»."):
            self.app.run("Elimina DB", lambda ctx: (O.delete_db(ctx), "DB eliminato.")[1])


# ==================================================================== accesso al DB

class AccessTab(Tab):
    title = "Accesso al DB"
    features = ("db_accesso",)

    def build(self, tab: ttk.Frame) -> None:
        self._build_access(tab)

    def _build_access(self, tab: ttk.Frame) -> None:
        self._rules: dict[str, D.AccessRule] = {}
        self._cleaned: set[str] = set()
        fa = ttk.LabelFrame(tab, text=" Dati di accesso (dal PC serve l'IP aperto, sotto) ", padding=6)
        fa.pack(fill="x")
        self.cred_vars = {k: tk.StringVar(value="-") for k in ("host", "port", "db", "user", "password")}
        self.show_pw = tk.BooleanVar(value=False)
        for i, (label, key, width) in enumerate((("Host", "host", 62), ("Porta", "port", 6), ("Database", "db", 18),
                                                 ("Utente", "user", 14), ("Password", "password", 36))):
            ttk.Label(fa, text=label + ":").grid(row=i // 3, column=(i % 3) * 3, sticky="w", padx=(0, 3))
            entry = ttk.Entry(fa, textvariable=self.cred_vars[key], width=width, state="readonly")
            entry.grid(row=i // 3, column=(i % 3) * 3 + 1, sticky="w")
            ttk.Button(fa, text="Copia", width=6, command=lambda k=key: self.app.copy(self.cred_vars[k].get())).grid(
                row=i // 3, column=(i % 3) * 3 + 2, sticky="w", padx=(2, 12))
            if key == "password":
                self.pw_entry = entry
                entry.configure(show="•")
        ttk.Checkbutton(fa, text="Mostra password", variable=self.show_pw,
                        command=lambda: self.pw_entry.configure(show="" if self.show_pw.get() else "•")).grid(
            row=1, column=6, sticky="w")
        ttk.Button(fa, text="Copia stringa di connessione", command=self._copy_connstring).grid(row=1, column=7,
                                                                                              sticky="w")
        self.gate(ttk.Button(fa, text="Leggi credenziali", command=self.do_credentials), "db_accesso").grid(
            row=0, column=9, sticky="w", padx=(12, 0))
        ttk.Label(fa, text="SSL obbligatorio (sslmode=require). Le credenziali si leggono dal segreto del DB.",
                  foreground="#666").grid(row=2, column=0, columnspan=8, sticky="w", pady=(4, 0))

        body = ttk.LabelFrame(tab, text=" IP che raggiungono il DB (porta 5432) ", padding=6)
        body.pack(fill="both", expand=True, pady=(6, 0))
        cols = (("ip", "IP / CIDR", 170), ("tipo", "Tipo", 90), ("aperto", "Aperto il (UTC)", 130),
                ("descrizione", "Descrizione", 420))
        self.tree = ttk.Treeview(body, columns=[c for c, _, _ in cols], show="headings", height=6,
                                 selectmode="extended")
        for c, title, w in cols:
            self.tree.heading(c, text=title)
            self.tree.column(c, width=w, stretch=c == "descrizione")
        self.tree.tag_configure("servizio", foreground="#888")
        self.tree.tag_configure("mio", background="#e3f5e1")
        self.tree.pack(side="left", fill="both", expand=True)
        fb = ttk.Frame(body, padding=(8, 0))
        fb.pack(side="left", fill="y")
        self.my_ip_var = tk.StringVar(value="IP di questo PC: -")
        ttk.Label(fb, textvariable=self.my_ip_var).pack(anchor="w", pady=(0, 4))
        self.gate(ttk.Button(fb, text="Aggiorna elenco", command=self.do_list), "db_accesso").pack(fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Apri al mio IP (temporaneo)", command=self.do_open_mine), "db_accesso").pack(
            fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Apri un altro IP…", command=self.do_open_cidr), "db_accesso").pack(fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Apri a tutti (temporaneo)…", command=self.do_open_all), "db_accesso").pack(
            fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Chiudi selezionati", command=self.do_close), "db_accesso").pack(fill="x", pady=1)
        self.hint = ttk.Label(fb, foreground="#666", wraplength=220, justify="left")
        self.hint.pack(anchor="w", pady=(8, 0))
        self.on_env_changed()

    def on_env_changed(self) -> None:
        self._rules = {}
        self.tree.delete(*self.tree.get_children())
        for v in self.cred_vars.values():
            v.set("-")
        self.hint.configure(text=f"Temporaneo = si chiude da solo dopo {self.env.ore_massime_manutenzione} ore "
                                 f"(alla prima lettura dell'elenco); permanente = resta finche' non lo si chiude.")

    def _copy_connstring(self) -> None:
        v = {k: var.get() for k, var in self.cred_vars.items()}
        if v["host"] != "-":
            self.app.copy(f"postgresql://{v['user']}:{v['password']}@{v['host']}:{v['port']}/{v['db']}?sslmode=require")

    def do_credentials(self) -> None:
        def job(ctx):
            c = D.rds_conn(ctx.aws)
            values = {"host": c.host, "port": str(c.port), "db": c.dbname, "user": c.user, "password": c.password}
            self.app.render(lambda: [var.set(values[k]) for k, var in self.cred_vars.items()])

        self.app.run("Credenziali del DB", job, quiet=True)

    def _after(self, ctx, text):
        env = self.env.name
        if env not in self._cleaned:
            n = D.cleanup_old_rules(ctx.aws, self.env.ore_massime_manutenzione)
            self._cleaned.add(env)
            if n:
                log.info("Tolte %d regole di accesso al DB rimaste aperte", n)
        rules = D.list_access(ctx.aws)
        try:
            mine = f"{D.my_public_ip()}/32"
        except Exception:  # noqa: BLE001 - l'elenco serve anche senza l'IP
            mine = ""
        self.app.render(lambda: self._show(rules, mine))
        return text

    def _show(self, rules: list, mine: str) -> None:
        self._rules = {r.cidr: r for r in rules}
        self.my_ip_var.set(f"IP di questo PC: {mine or '?'}")
        self.tree.delete(*self.tree.get_children())
        for r in rules:
            tags = ("servizio",) if not r.closable else (("mio",) if r.cidr == mine else ())
            self.tree.insert("", "end", iid=r.cidr, tags=tags, values=(
                r.cidr, r.kind, r.opened_at.strftime("%Y-%m-%d %H:%M") if r.opened_at else "", r.description))

    def do_list(self) -> None:
        self.app.run("Accessi al DB", lambda ctx: self._after(ctx, None), quiet=True)

    def do_open_mine(self) -> None:
        if self.env.is_prod and not self.app.confirm_typed("Aprire il DB di PRODUZIONE all'IP di questo PC "
                                                           "(temporaneo)."):
            return
        self.app.run("Apri al mio IP", lambda ctx: self._after(ctx, O.open_access_for(ctx, 1)))

    def do_open_cidr(self) -> None:
        dlg = OpenCidrDialog(self.app)
        if not dlg.result:
            return
        raw, permanent, note = dlg.result
        try:
            cidr = D.normalize_cidr(raw)
        except Exception as exc:  # noqa: BLE001
            messagebox.showerror("Apri IP", str(exc), parent=self.app)
            return
        everyone = cidr in ("0.0.0.0/0", "::/0")
        if (D.is_wide(cidr) or self.env.is_prod) and not self.app.confirm_typed(
                f"{cidr} apre il DB dell'ambiente {self.env.name} a "
                + ("TUTTA internet." if everyone else "un intervallo ampio di indirizzi." if D.is_wide(cidr)
                   else "questo indirizzo.")):
            return

        def job(ctx):
            added = D.open_cidr(ctx.aws, cidr, permanent, note)
            kind = "permanente" if permanent else "temporaneo"
            return self._after(ctx, f"{cidr}: {'aperto' if added else 'era gia aperto'} ({kind}).")

        self.app.run("Apri IP", job)

    def do_open_all(self) -> None:
        if not self.app.confirm_typed(f"Aprire la porta 5432 del DB dell'ambiente {self.env.name} a TUTTA internet "
                                      f"(0.0.0.0/0), in modo temporaneo: si chiude dopo "
                                      f"{self.env.ore_massime_manutenzione} ore (alla prima lettura dell'elenco) o "
                                      f"con «Chiudi selezionati». Resta la password come unica protezione."):
            return

        def job(ctx):
            added = D.open_cidr(ctx.aws, "0.0.0.0/0", False, "")
            return self._after(ctx, f"0.0.0.0/0: {'aperto' if added else 'era gia aperto'} (temporaneo).")

        self.app.run("Apri a tutti", job)

    def do_close(self) -> None:
        chosen = [self._rules[i] for i in self.tree.selection() if i in self._rules and self._rules[i].closable]
        if not chosen:
            messagebox.showinfo("Chiudi", "Selezionare uno o piu' IP (la regola dei task del backend non si chiude).",
                                parent=self.app)
            return
        lines = "\n".join(f"• {r.cidr} ({r.kind})" for r in chosen)
        if not messagebox.askyesno("Chiudi", f"Chiudere l'accesso al DB per:\n\n{lines}", parent=self.app):
            return

        def job(ctx):
            for r in chosen:
                D.close_cidr(ctx.aws, r.cidr)
            return self._after(ctx, f"Chiusi {len(chosen)} accessi.")

        self.app.run("Chiudi accessi", job)
