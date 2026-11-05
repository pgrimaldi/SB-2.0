"""Linguetta «Gestione DB»: connessione e porte, istanza e formati, panoramica, sessioni, query, snapshot e log.

La prima sotto-linguetta e' quella di «Accesso al DB» (credenziali e IP aperti alla 5432); le altre
leggono il DB dal PC (accesso aperto per la durata della lettura) o RDS.
"""
from __future__ import annotations

import tkinter as tk
from datetime import datetime
from tkinter import messagebox, ttk
from tkinter.scrolledtext import ScrolledText

from app.env import dbadmin as M
from app.gui.dialogs import TableWindow
from app.gui.tab_env import AccessTab


def _tree(parent, cols: list[tuple[str, int]], height: int = 8, stretch: str | None = None) -> ttk.Treeview:
    frame = ttk.Frame(parent)
    frame.pack(fill="both", expand=True)
    tree = ttk.Treeview(frame, columns=[c for c, _ in cols], show="headings", height=height)
    for c, w in cols:
        tree.heading(c, text=c)
        tree.column(c, width=w, stretch=c == stretch)
    ys = ttk.Scrollbar(frame, orient="vertical", command=tree.yview)
    tree.configure(yscrollcommand=ys.set)
    tree.pack(side="left", fill="both", expand=True)
    ys.pack(side="left", fill="y")
    return tree


def _fill(tree: ttk.Treeview, rows) -> None:
    tree.delete(*tree.get_children())
    for r in rows:
        tree.insert("", "end", values=["" if v is None else str(v) for v in r])


class DbAdminTab(AccessTab):
    title = "Gestione DB"
    features = ("db_accesso", "db_amministra")

    def build(self, tab: ttk.Frame) -> None:
        self._formats: dict[str, str] = {}
        self._last: tuple[list[str], list[tuple]] | None = None
        nb = ttk.Notebook(tab)
        nb.pack(fill="both", expand=True)
        pages = {}
        for key, title in (("access", "Connessione e porte"), ("info", "Istanza e formati"),
                           ("overview", "Panoramica"), ("sessions", "Sessioni"), ("query", "Query"),
                           ("snap", "Snapshot e log")):
            pages[key] = ttk.Frame(nb, padding=6)
            nb.add(pages[key], text=f" {title} ")
        self._build_access(pages["access"])
        self._build_info(pages["info"])
        self._build_overview(pages["overview"])
        self._build_sessions(pages["sessions"])
        self._build_query(pages["query"])
        self._build_snap(pages["snap"])

    # ---------------------------------------------------------------- istanza e formati
    def _build_info(self, page) -> None:
        top = ttk.Frame(page)
        top.pack(fill="x")
        self.gate(ttk.Button(top, text="Leggi istanza e formati", command=self.do_info), "db_accesso").pack(side="left")
        ttk.Button(top, text="Copia formato selezionato", command=self._copy_format).pack(side="left", padx=6)
        ttk.Label(top, text="I formati contengono la password: non incollarli in chat o in file versionati.",
                  foreground="#9a6700").pack(side="left", padx=8)
        body = ttk.Frame(page)
        body.pack(fill="both", expand=True, pady=(6, 0))
        left = ttk.LabelFrame(body, text=" Istanza RDS ", padding=4)
        left.pack(side="left", fill="both", expand=True)
        self.info_tree = _tree(left, [("voce", 200), ("valore", 380)], 12, "valore")
        right = ttk.LabelFrame(body, text=" Formati di connessione ", padding=4)
        right.pack(side="left", fill="both", expand=True, padx=(8, 0))
        self.fmt_tree = _tree(right, [("formato", 260), ("valore", 520)], 12, "valore")

    def do_info(self) -> None:
        def job(ctx):
            from app.env import database as D  # noqa: PLC0415

            info = M.instance_info(ctx.aws)
            conn = D.rds_conn(ctx.aws)
            formats = M.connection_strings(conn)
            ro = M.read_only_conn(ctx.aws)
            secrets_ = [conn.password]
            if ro is not None:
                formats += [(f"{k} - SOLA LETTURA ({ro.user})", v) for k, v in M.connection_strings(ro)]
                secrets_.append(ro.password)

            def show():
                _fill(self.info_tree, info)
                self._formats = dict(formats)
                self.fmt_tree.delete(*self.fmt_tree.get_children())
                for name, value in formats:
                    shown = value
                    for pw in secrets_:
                        shown = shown.replace(pw, "••••••")  # a video mascherata, intera nella copia
                    self.fmt_tree.insert("", "end", iid=name, values=(name, shown))

            self.app.render(show)

        self.app.run("Istanza e formati", job, quiet=True, clear_steps=False)

    def _copy_format(self) -> None:
        sel = self.fmt_tree.selection()
        if sel:
            self.app.copy(self._formats.get(sel[0], ""))

    # ---------------------------------------------------------------- panoramica
    def _build_overview(self, page) -> None:
        top = ttk.Frame(page)
        top.pack(fill="x")
        self.gate(ttk.Button(top, text="Leggi panoramica", command=self.do_overview), "db_accesso").pack(side="left")
        self.ov_var = tk.StringVar(value="Versione, dimensioni, connessioni, migration per schema, tabelle piu' grandi.")
        ttk.Label(top, textvariable=self.ov_var, wraplength=1100).pack(side="left", padx=8)
        body = ttk.Frame(page)
        body.pack(fill="both", expand=True, pady=(6, 0))
        f1 = ttk.LabelFrame(body, text=" Schemi ", padding=4)
        f1.pack(side="left", fill="both", expand=True)
        self.ov_schemas = _tree(f1, [("schema", 110), ("tabelle", 60), ("dimensione", 90), ("righe_stimate", 100)], 9)
        f2 = ttk.LabelFrame(body, text=" Migration ", padding=4)
        f2.pack(side="left", fill="both", expand=True, padx=6)
        self.ov_migr = _tree(f2, [("schema", 100), ("ultima migration", 260), ("applicate", 70)], 9, "ultima migration")
        f3 = ttk.LabelFrame(body, text=" Tabelle piu' grandi ", padding=4)
        f3.pack(side="left", fill="both", expand=True)
        self.ov_tables = _tree(f3, [("tabella", 260), ("totale", 80), ("dati", 80), ("righe_stimate", 100)], 9, "tabella")

    def do_overview(self) -> None:
        def job(ctx):
            o = M.overview(ctx.aws)

            def show():
                self.ov_var.set(f"{o['versione'].split(',')[0]} - DB {o['dimensione']} - {o['connessioni']} connessioni")
                _fill(self.ov_schemas, o["schemi"][1])
                _fill(self.ov_migr, o["migration"][1])
                _fill(self.ov_tables, o["tabelle"][1])

            self.app.render(show)

        self.app.run("Panoramica del DB", job, quiet=True, clear_steps=False)

    # ---------------------------------------------------------------- sessioni
    def _build_sessions(self, page) -> None:
        top = ttk.Frame(page)
        top.pack(fill="x")
        sel = lambda: bool(self.sess.selection())  # noqa: E731
        self.gate(ttk.Button(top, text="Leggi sessioni", command=self.do_sessions), "db_accesso").pack(side="left")
        self.gate(ttk.Button(top, text="Annulla la query…", command=lambda: self.do_stop(False)), "db_amministra",
                  sel).pack(side="left", padx=6)
        self.gate(ttk.Button(top, text="Chiudi la sessione…", command=lambda: self.do_stop(True)), "db_amministra",
                  sel).pack(side="left")
        ttk.Label(top, text="Le sessioni del backend (Npgsql) si riaprono da sole; chiudere quelle bloccate o "
                            "dimenticate (DBeaver, psql).", foreground="#666").pack(side="left", padx=8)
        cols = [("pid", 60), ("utente", 90), ("applicazione", 140), ("indirizzo", 110), ("stato", 90),
                ("attesa", 110), ("durata", 70), ("bloccato_da", 80), ("query", 500)]
        self.sess = _tree(page, cols, 12, "query")
        self.sess.tag_configure("ko", background="#fde2e2")
        self.sess.bind("<<TreeviewSelect>>", lambda _e: self.app._refresh_buttons())

    def do_sessions(self) -> None:
        def job(ctx):
            cols, rows = M.sessions(ctx.aws)

            def show():
                self.sess.delete(*self.sess.get_children())
                for r in rows:
                    blocked = bool(r[cols.index("bloccato_da")])
                    self.sess.insert("", "end", iid=str(r[0]), tags=("ko",) if blocked else (),
                                     values=["" if v is None else str(v) for v in r])
                self.app._refresh_buttons()

            self.app.render(show)

        self.app.run("Sessioni del DB", job, quiet=True, clear_steps=False)

    def do_stop(self, terminate: bool) -> None:
        sel = self.sess.selection()
        if not sel:
            return
        pid = int(sel[0])
        values = self.sess.item(sel[0], "values")
        what = "CHIUDERE la sessione" if terminate else "annullare la query in corso della sessione"
        text = f"{what} {pid} ({values[1]}, {values[2]}, {values[3]})?\n\n{values[8][:200]}"
        if not (self.app.confirm_typed(text) if self.env.is_prod else messagebox.askyesno("Sessione", text,
                                                                                         parent=self.app)):
            return
        self.app.run("Sessione", lambda ctx: (f"{'Chiusa' if terminate else 'Annullata'}: "
                                              f"{'si' if M.stop_session(ctx.aws, pid, terminate) else 'nessun effetto'}"),
                     then=self.do_sessions)

    # ---------------------------------------------------------------- query
    def _build_query(self, page) -> None:
        top = ttk.Frame(page)
        top.pack(fill="x")
        ttk.Label(top, text="Query pronta:").pack(side="left")
        self.preset = tk.StringVar()
        cb = ttk.Combobox(top, textvariable=self.preset, values=list(M.PRESETS), width=50, state="readonly")
        cb.pack(side="left", padx=4)
        cb.bind("<<ComboboxSelected>>", lambda _e: self._load_preset())
        self.gate(ttk.Button(top, text="Esegui (sola lettura)", command=self.do_query), "db_accesso").pack(side="left",
                                                                                                         padx=6)
        self.gate(ttk.Button(top, text="Esporta risultato in CSV", command=self.do_export), "db_accesso",
                  lambda: self._last is not None).pack(side="left")
        ttk.Label(page, text=f"Una sola istruzione che inizi con select / with / explain / show / table / values, in una "
                             f"transazione READ ONLY, al massimo {M.QUERY_TIMEOUT_S} s e {M.MAX_ROWS} righe. "
                             f"Il DB contiene dati personali: i risultati esportati non vanno condivisi.",
                  foreground="#666").pack(anchor="w", pady=(4, 0))
        ro = ttk.Frame(page)
        ro.pack(fill="x", pady=(4, 0))
        self.ro_var = tk.StringVar(value=f"Utente in sola lettura ({self.env.database.utente_lettura}): premere "
                                         f"«Stato utente»")
        ttk.Label(ro, textvariable=self.ro_var).pack(side="left")
        self.gate(ttk.Button(ro, text="Stato utente", command=self.do_ro_status), "db_accesso").pack(side="left", padx=6)
        self.gate(ttk.Button(ro, text="Crea / riallinea…", command=self.do_ro_setup), "db_amministra").pack(side="left")
        self.gate(ttk.Button(ro, text="Rimuovi…", command=self.do_ro_remove), "db_amministra").pack(side="left", padx=6)
        self.sql = ScrolledText(page, height=9, font=("Consolas", 10), wrap="none")
        self.sql.pack(fill="both", expand=True, pady=(4, 0))

    def do_ro_status(self) -> None:
        def job(ctx):
            ro = M.read_only_conn(ctx.aws)
            text = (f"Utente in sola lettura: {ro.user} - query, panoramica e sessioni passano da qui"
                    if ro else f"Utente in sola lettura ASSENTE ({self.env.database.secret_lettura} non esiste o non "
                               f"leggibile): le query usano l'utente principale in transazione READ ONLY")
            self.app.render(lambda: self.ro_var.set(text))

        self.app.run("Utente in sola lettura", job, quiet=True, clear_steps=False)

    def do_ro_setup(self) -> None:
        d = self.env.database
        if not self.app.confirm_write("Utente in sola lettura",
                                      f"Creo o riallineo l'utente PostgreSQL {d.utente_lettura} sul DB di "
                                      f"{self.env.name}: login, sola lettura di default, SELECT sugli schemi del "
                                      f"progetto (anche sulle tabelle future), al massimo 5 connessioni. La password "
                                      f"sta nel segreto {d.secret_lettura} (se esiste gia' resta la stessa).\n\n"
                                      f"Procedere?"):
            return
        self.app.run("Utente in sola lettura", lambda ctx: M.setup_read_only(ctx.aws), then=self.do_ro_status)

    def do_ro_remove(self) -> None:
        d = self.env.database
        if not self.app.confirm_typed(f"Rimuovere l'utente {d.utente_lettura} dal DB di {self.env.name} (le sue "
                                      f"sessioni vengono chiuse) e mettere in cancellazione il segreto "
                                      f"{d.secret_lettura}?", d.utente_lettura):
            return
        self.app.run("Rimuovi utente in sola lettura", lambda ctx: M.remove_read_only(ctx.aws), then=self.do_ro_status)

    def _load_preset(self) -> None:
        self.sql.delete("1.0", "end")
        self.sql.insert("1.0", M.PRESETS[self.preset.get()].strip())

    def do_query(self) -> None:
        text = self.sql.get("1.0", "end")
        try:
            M.check_read_only(text)
        except Exception as exc:  # noqa: BLE001
            messagebox.showwarning("Query", str(exc), parent=self.app)
            return
        if self.env.is_prod and not messagebox.askyesno("Query in produzione", "Eseguire la query (sola lettura) sul DB "
                                                        "di PRODUZIONE?", parent=self.app):
            return

        def job(ctx):
            cols, rows, more = M.run_query(ctx.aws, text)
            self._last = (cols, rows)
            note = f"{len(rows)} righe" + (f" (troncate a {M.MAX_ROWS})" if more else "")
            dicts = [dict(zip(cols, r)) for r in rows]
            self.app.render(lambda: (TableWindow(self.app, f"Query - {self.env.name}", cols, dicts, note,
                                                 self.do_export), self.app._refresh_buttons()))

        self.app.run("Query", job, quiet=True, clear_steps=False)

    def do_export(self) -> None:
        if not self._last:
            return
        dest = self.env.work_path / "query" / self.env.name / f"query_{datetime.now():%Y%m%d_%H%M%S}.csv"
        M.export_csv(*self._last, dest)
        self.app.open_folder(dest.parent)

    # ---------------------------------------------------------------- snapshot e log
    def _build_snap(self, page) -> None:
        fs = ttk.LabelFrame(page, text=" Snapshot dell'istanza (manuali della utility: <istanza>-u22-<data>) ", padding=4)
        fs.pack(fill="both", expand=True)
        top = ttk.Frame(fs)
        top.pack(fill="x")
        self.gate(ttk.Button(top, text="Leggi snapshot", command=self.do_snapshots), "db_accesso|db_amministra").pack(
            side="left")
        self.gate(ttk.Button(top, text="Crea snapshot manuale…", command=self.do_create_snapshot), "db_amministra").pack(
            side="left", padx=6)
        self.gate(ttk.Button(top, text="Cancella snapshot selezionato…", command=self.do_delete_snapshot),
                  "db_amministra", lambda: bool(self.snaps.selection())).pack(side="left")
        ttk.Label(top, text="Utile prima di un deploy con migration o di una retention. Gli snapshot automatici si "
                            "leggono ma non si cancellano.", foreground="#666").pack(side="left", padx=8)
        self.snaps = _tree(fs, [("snapshot", 340), ("tipo", 90), ("stato", 90), ("creato", 130), ("GB", 50)], 6, "snapshot")
        self.snaps.bind("<<TreeviewSelect>>", lambda _e: self.app._refresh_buttons())
        fl = ttk.LabelFrame(page, text=" File di log di PostgreSQL (RDS) ", padding=4)
        fl.pack(fill="both", expand=True, pady=(6, 0))
        top = ttk.Frame(fl)
        top.pack(fill="x")
        self.gate(ttk.Button(top, text="Elenca file di log", command=self.do_logs), "db_amministra").pack(side="left")
        self.gate(ttk.Button(top, text="Scarica selezionati", command=self.do_download_logs), "db_amministra",
                  lambda: bool(self.logs.selection())).pack(side="left", padx=6)
        self.logs = _tree(fl, [("file", 360), ("byte", 100), ("ultima scrittura", 140)], 6, "file")
        self.logs.configure(selectmode="extended")
        self.logs.bind("<<TreeviewSelect>>", lambda _e: self.app._refresh_buttons())

    def do_snapshots(self) -> None:
        def job(ctx):
            snaps = M.list_snapshots(ctx.aws)

            def show():
                self.snaps.delete(*self.snaps.get_children())
                for s in snaps:
                    self.snaps.insert("", "end", iid=s.ident, values=(s.ident, s.kind + (" (utility)" if s.own else ""),
                                                                      s.status, s.created, s.size_gb))
                self.app._refresh_buttons()

            self.app.render(show)

        self.app.run("Snapshot", job, quiet=True, clear_steps=False)

    def do_create_snapshot(self) -> None:
        if not self.app.confirm_write("Snapshot", f"Creare uno snapshot manuale del DB dell'ambiente {self.env.name}? "
                                      f"Dura qualche minuto, il DB resta in servizio. Uno snapshot manuale resta (e si "
                                      f"paga) finche' non lo si cancella."):
            return
        self.app.run("Crea snapshot", lambda ctx: f"Snapshot {M.create_snapshot(ctx.aws, ctx.cancel, ctx.progress)} "
                                                  f"pronto.", then=self.do_snapshots)

    def do_delete_snapshot(self) -> None:
        sel = self.snaps.selection()
        if not sel:
            return
        name = sel[0]
        if not self.app.confirm_typed(f"Cancellare lo snapshot {name}? Non si recupera.", name):
            return
        self.app.run("Cancella snapshot", lambda ctx: (M.delete_snapshot(ctx.aws, name), f"{name} in cancellazione.")[1],
                     then=self.do_snapshots)

    def do_logs(self) -> None:
        def job(ctx):
            files = M.list_log_files(ctx.aws)
            self.app.render(lambda: (_fill(self.logs, files), self._iid_logs(files)))

        self.app.run("File di log", job, quiet=True, clear_steps=False)

    def _iid_logs(self, files) -> None:
        self.logs.delete(*self.logs.get_children())
        for f in files:
            self.logs.insert("", "end", iid=f[0], values=f)
        self.app._refresh_buttons()

    def do_download_logs(self) -> None:
        names = list(self.logs.selection())
        dest = self.env.work_path / "pglog" / self.env.name

        def job(ctx):
            for n in names:
                M.download_log(ctx.aws, n, dest, ctx.cancel)
            self.app.render(lambda: self.app.open_folder(dest))
            return f"{len(names)} file scaricati in {dest}"

        self.app.run("Scarico dei log", job)

    def on_env_changed(self) -> None:
        super().on_env_changed()
        if not hasattr(self, "snaps"):
            return
        self._last = None
        self._formats = {}
        for tree in (self.info_tree, self.fmt_tree, self.ov_schemas, self.ov_migr, self.ov_tables, self.sess,
                     self.snaps, self.logs):
            tree.delete(*tree.get_children())
