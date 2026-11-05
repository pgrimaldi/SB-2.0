"""Linguette dei dati: backup del DB, file S3, dati archiviati dalla retention."""
from __future__ import annotations

import logging
import tkinter as tk
from datetime import datetime
from pathlib import Path
from tkinter import filedialog, messagebox, ttk

from app.env import archives as A
from app.env import backup as B
from app.env import retention as RT
from app.env import s3files as S
from app.gui.dialogs import TableWindow, TextWindow
from app.gui.shell import Tab
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)


def _mb(n: int) -> str:
    return f"{n / 1e6:,.1f} MB".replace(",", ".") if n >= 100_000 else f"{n / 1024:,.1f} KB".replace(",", ".")


# ==================================================================== backup del DB

class BackupTab(Tab):
    title = "Backup DB"
    features = ("db_backup",)

    def build(self, tab: ttk.Frame) -> None:
        b = self.app.settings.backup
        self._backups: list[B.LocalBackup] = []
        ttk.Label(tab, text="Il dump lo fa il task Fargate «dbloader» dentro la VPC (stessa versione di pg_dump del DB); "
                            "il file passa dal bucket di build e si scarica qui, con verifica dello SHA-256.",
                  foreground="#666").pack(anchor="w")
        f = ttk.LabelFrame(tab, text=" Nuovo backup ", padding=8)
        f.pack(fill="x", pady=(6, 0))
        ttk.Label(f, text="Formato:").grid(row=0, column=0, sticky="w")
        self.fmt = tk.StringVar(value=b.formato)
        ttk.Radiobutton(f, text="custom (pg_restore)", value="custom", variable=self.fmt).grid(row=0, column=1, sticky="w")
        ttk.Radiobutton(f, text="SQL", value="sql", variable=self.fmt).grid(row=0, column=2, sticky="w", padx=(6, 0))
        ttk.Label(f, text="Compressione (0 = nessuna, 9 = massima):").grid(row=0, column=3, sticky="w", padx=(18, 4))
        self.level = tk.IntVar(value=b.compressione)
        ttk.Spinbox(f, from_=0, to=9, textvariable=self.level, width=4, state="readonly").grid(row=0, column=4, sticky="w")
        ttk.Label(f, text="Schemi (vuoto = tutto il DB, separati da virgola):").grid(row=1, column=0, columnspan=3,
                                                                                    sticky="w", pady=(6, 0))
        self.schemi = tk.StringVar()
        ttk.Entry(f, textvariable=self.schemi, width=60).grid(row=1, column=3, columnspan=3, sticky="w", pady=(6, 0))
        self.solo_schema = tk.BooleanVar(value=False)
        ttk.Checkbutton(f, text="Solo schema (niente dati)", variable=self.solo_schema).grid(row=2, column=1,
                                                                                            columnspan=2, sticky="w")
        self.keep = tk.BooleanVar(value=b.tieni_su_s3)
        ttk.Checkbutton(f, text="Tieni anche la copia nel bucket di build", variable=self.keep).grid(
            row=2, column=3, columnspan=2, sticky="w")
        self.gate(ttk.Button(f, text="Avvia backup", command=self.do_backup), "db_backup").grid(row=0, column=6,
                                                                                              rowspan=2, padx=(18, 0))
        self.dest_var = tk.StringVar()
        ttk.Label(f, textvariable=self.dest_var, foreground="#666").grid(row=3, column=0, columnspan=7, sticky="w",
                                                                         pady=(6, 0))

        fl = ttk.LabelFrame(tab, text=" Backup in locale per questo ambiente ", padding=4)
        fl.pack(fill="both", expand=True, pady=(6, 0))
        cols = (("file", 380), ("dimensione", 90), ("formato", 110), ("schemi", 160), ("fine", 150), ("da", 160))
        self.tree = ttk.Treeview(fl, columns=[c for c, _ in cols], show="headings", height=6)
        for c, w in cols:
            self.tree.heading(c, text=c)
            self.tree.column(c, width=w, stretch=c == "file")
        self.tree.pack(side="left", fill="both", expand=True)
        fb = ttk.Frame(fl, padding=(8, 0))
        fb.pack(side="left", fill="y")
        ttk.Button(fb, text="Aggiorna elenco", command=self.refresh_list).pack(fill="x", pady=1)
        ttk.Button(fb, text="Verifica selezionato", command=self.do_verify).pack(fill="x", pady=1)
        ttk.Button(fb, text="Apri cartella", command=lambda: self.app.open_folder(self.env.backup_path)).pack(fill="x", pady=1)
        ttk.Button(fb, text="Cancella selezionato…", command=self.do_delete).pack(fill="x", pady=1)
        self.on_env_changed()

    def on_env_changed(self) -> None:
        self.dest_var.set(f"Destinazione: {self.env.backup_path}")
        self.refresh_list()

    def refresh_list(self) -> None:
        self._backups = B.list_local(self.env)
        self.tree.delete(*self.tree.get_children())
        for i, bk in enumerate(self._backups):
            m = bk.meta
            fmt = f"{m.get('formato', '?')} z{m.get('compressione', '?')}" + (" schema" if m.get("solo_schema") else "")
            schemi = m.get("schemi", "")
            self.tree.insert("", "end", iid=str(i), values=(
                bk.path.name, f"{bk.size_mb:.1f} MB", fmt, ", ".join(schemi) if isinstance(schemi, list) else schemi,
                (m.get("fine") or "")[:19].replace("T", " "), (m.get("eseguito_da") or "").rsplit("/", 1)[-1]))

    def _selected(self) -> B.LocalBackup | None:
        sel = self.tree.selection()
        return self._backups[int(sel[0])] if sel else None

    def do_backup(self) -> None:
        schemi = [x.strip() for x in self.schemi.get().split(",") if x.strip()] or None
        opt = B.BackupOptions(self.fmt.get(), int(self.level.get()), schemi, self.solo_schema.get(), self.keep.get())
        try:
            opt.validate()
        except Exception as exc:  # noqa: BLE001
            messagebox.showerror("Backup", str(exc), parent=self.app)
            return
        text = (f"Backup del DB dell'ambiente {self.env.name} (stack {self.env.stack})\n\n"
                f"Formato: {opt.formato}, compressione {opt.compressione}\nSchemi: {', '.join(schemi) if schemi else 'tutti'}"
                f"{' (solo schema)' if opt.solo_schema else ''}\nDestinazione: {self.env.backup_path}\n\n"
                f"Il DB non viene modificato. Se e' spento dall'orario, resta acceso in manutenzione per la durata "
                f"del dump.\n\nProcedere?")
        if not messagebox.askyesno("Backup DB", text, parent=self.app):
            return
        self.app.run("Backup DB", lambda ctx: f"Backup salvato in\n{B.run_backup(ctx, opt)}", then=self.refresh_list)

    def do_verify(self) -> None:
        bk = self._selected()
        if not bk:
            return
        env = self.env
        self.app.run("Verifica backup", lambda ctx: f"{bk.path.name}: {B.verify_local(env, bk.path, ctx.cancel)}")

    def do_delete(self) -> None:
        bk = self._selected()
        if bk and messagebox.askyesno("Cancella backup", f"Cancellare il file locale\n{bk.path}\n(e il suo .json)?",
                                      parent=self.app):
            B.delete_local(bk.path)
            self.refresh_list()


# ==================================================================== file S3

class FilesTab(Tab):
    title = "File S3"
    features = ("file_lettura",)

    def build(self, tab: ttk.Frame) -> None:
        self._buckets: dict[str, S.Bucket] = {}
        self._entries: dict[str, S.Entry] = {}
        top = ttk.Frame(tab)
        top.pack(fill="x")
        ttk.Label(top, text="Bucket:").pack(side="left")
        self.bucket = tk.StringVar()
        self.bucket_combo = ttk.Combobox(top, textvariable=self.bucket, width=42, state="readonly")
        self.bucket_combo.pack(side="left", padx=4)
        self.bucket_combo.bind("<<ComboboxSelected>>", lambda _e: self.go(""))
        self.gate(ttk.Button(top, text="Leggi bucket", command=self.do_buckets), "file_lettura").pack(side="left", padx=4)
        ttk.Label(top, text="Percorso:").pack(side="left", padx=(12, 2))
        self.prefix = tk.StringVar()
        ttk.Entry(top, textvariable=self.prefix, width=60).pack(side="left", fill="x", expand=True)
        has_bucket = lambda: bool(self.bucket.get())  # noqa: E731
        self.gate(ttk.Button(top, text="Vai", command=lambda: self.go(self.prefix.get())), "file_lettura",
                  has_bucket).pack(side="left", padx=2)
        self.gate(ttk.Button(top, text="Su", command=self.up), "file_lettura", has_bucket).pack(side="left", padx=2)
        self.ro_var = tk.StringVar()
        ttk.Label(tab, textvariable=self.ro_var, foreground="#9a6700").pack(anchor="w")

        body = ttk.Frame(tab)
        body.pack(fill="both", expand=True, pady=(4, 0))
        cols = (("nome", 420), ("dimensione", 100), ("modificato", 150), ("classe", 110))
        self.tree = ttk.Treeview(body, columns=[c for c, _ in cols], show="headings", height=9, selectmode="extended")
        for c, w in cols:
            self.tree.heading(c, text=c)
            self.tree.column(c, width=w, stretch=c == "nome")
        self.tree.tag_configure("dir", foreground="#1a56db")
        ys = ttk.Scrollbar(body, orient="vertical", command=self.tree.yview)
        self.tree.configure(yscrollcommand=ys.set)
        self.tree.pack(side="left", fill="both", expand=True)
        ys.pack(side="left", fill="y")
        self.tree.bind("<Double-1>", self._open)
        fb = ttk.Frame(body, padding=(8, 0))
        fb.pack(side="left", fill="y")
        writable = lambda: has_bucket() and not self._read_only()  # noqa: E731
        self.gate(ttk.Button(fb, text="Dimensione del percorso", command=self.do_summary), "file_lettura",
                  has_bucket).pack(fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Scarica selezionati…", command=self.do_download), "file_lettura",
                  has_bucket).pack(fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Backup del percorso", command=self.do_backup), "file_lettura",
                  has_bucket).pack(fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Copia link (1 ora)", command=self.do_link), "file_lettura", has_bucket).pack(
            fill="x", pady=1)
        ttk.Separator(fb).pack(fill="x", pady=6)
        self.gate(ttk.Button(fb, text="Carica file qui…", command=self.do_upload), "file_scrittura", writable).pack(
            fill="x", pady=1)
        self.gate(ttk.Button(fb, text="Cancella selezionati…", command=self.do_delete), "file_scrittura",
                  writable).pack(fill="x", pady=1)
        ttk.Separator(fb).pack(fill="x", pady=6)
        ttk.Button(fb, text="Apri cartella dei backup",
                   command=lambda: self.app.open_folder(self.env.work_path / "s3" / self.env.name)).pack(fill="x", pady=1)
        self.info = ttk.Label(fb, foreground="#666", wraplength=230, justify="left",
                              text="Doppio clic su una cartella per entrarci. Il backup e' incrementale: rilanciato "
                                   "scarica solo i file nuovi o cambiati.")
        self.info.pack(anchor="w", pady=(8, 0))

    def on_env_changed(self) -> None:
        self._buckets = {}
        self._entries = {}
        self.bucket_combo["values"] = []
        self.bucket.set("")
        self.prefix.set("")
        self.ro_var.set("")
        self.tree.delete(*self.tree.get_children())

    def autoload(self):
        if self.app.allowed("file_lettura") and not self._buckets:
            return lambda then: self.do_buckets(then, silent=True)
        return None

    def _read_only(self) -> bool:
        b = self._buckets.get(self.bucket.get())
        return b is None or b.read_only

    def do_buckets(self, then=None, silent: bool = False) -> None:
        def job(ctx):
            bs = S.buckets(ctx.aws)

            def show():
                self._buckets = {b.name: b for b in bs}
                self.bucket_combo["values"] = [b.name for b in bs]
                self.app._refresh_buttons()

            self.app.render(show)

        self.app.run("Bucket dell'ambiente", job, then=then, quiet=True, silent=silent)

    def go(self, prefix: str) -> None:
        bucket = self.bucket.get()
        if not bucket:
            return
        if prefix and not prefix.endswith("/"):
            prefix += "/"
        b = self._buckets.get(bucket)
        self.ro_var.set(f"{b.output}: sola lettura per questa utility (s3.sola_lettura)" if b and b.read_only else "")

        def job(ctx):
            entries, truncated = S.list_level(ctx.aws, bucket, prefix)

            def show():
                self.prefix.set(prefix)
                self._entries = {e.key: e for e in entries}
                self.tree.delete(*self.tree.get_children())
                for e in sorted(entries, key=lambda x: (not x.is_dir, x.key)):
                    self.tree.insert("", "end", iid=e.key, tags=("dir",) if e.is_dir else (), values=(
                        e.name, "" if e.is_dir else _mb(e.size),
                        e.modified.strftime("%Y-%m-%d %H:%M") if e.modified else "", e.storage_class))
                self.app._refresh_buttons()

            self.app.render(show)
            if truncated:
                return (f"Elenco troncato a {self.env.s3.max_elementi_elenco} elementi: restringere il percorso.")
            return None

        self.app.run(f"s3://{bucket}/{prefix}", job, clear_steps=False)

    def up(self) -> None:
        p = self.prefix.get().rstrip("/")
        self.go(p.rsplit("/", 1)[0] + "/" if "/" in p else "")

    def _open(self, _event) -> None:
        sel = self.tree.selection()
        if sel and sel[0].endswith("/"):
            self.go(sel[0])

    def _keys(self) -> list[str]:
        return list(self.tree.selection())

    def do_summary(self) -> None:
        bucket, prefix = self.bucket.get(), self.prefix.get()

        def job(ctx):
            r = S.summary(ctx.aws, bucket, prefix, ctx.cancel, ctx.progress)
            classes = ", ".join(f"{k} {_mb(v)}" for k, v in r["per_classe"].items())
            return f"s3://{bucket}/{prefix}\n{r['oggetti']} oggetti, {_mb(r['byte'])}\n{classes}"

        self.app.run("Dimensione", job)

    def do_download(self) -> None:
        keys = self._keys()
        if not keys:
            messagebox.showinfo("Scarica", "Selezionare file o cartelle.", parent=self.app)
            return
        dest = filedialog.askdirectory(title="Cartella di destinazione", parent=self.app,
                                       initialdir=str(self.env.work_path))
        if not dest:
            return
        bucket = self.bucket.get()

        def job(ctx):
            r = S.download(ctx.aws, bucket, keys, Path(dest), ctx.cancel, ctx.progress)
            return f"{r['scaricati']} file scaricati in {dest}, {r['errori']} errori"

        self.app.run("Scarico", job)

    def do_backup(self) -> None:
        bucket, prefix = self.bucket.get(), self.prefix.get()
        root = self.env.work_path / "s3" / self.env.name / bucket
        if not messagebox.askyesno("Backup", f"Copia incrementale di s3://{bucket}/{prefix} in\n{root}\n\nProcedere?",
                                   parent=self.app):
            return

        def job(ctx):
            path, r = S.backup(ctx.aws, bucket, prefix, ctx.cancel, ctx.progress)
            return (f"{path}\n{r['scaricati']} scaricati, {r['gia_presenti']} gia' presenti, {r['errori']} errori"
                    + (f", {r['non_piu_nel_bucket']} in locale non piu' nel bucket" if r["non_piu_nel_bucket"] else ""))

        self.app.run("Backup dei file", job)

    def do_link(self) -> None:
        keys = [k for k in self._keys() if not k.endswith("/")]
        if len(keys) != 1:
            messagebox.showinfo("Link", "Selezionare un solo file.", parent=self.app)
            return
        bucket = self.bucket.get()

        def job(ctx):
            url = S.presigned_url(ctx.aws, bucket, keys[0], 60)
            self.app.render(lambda: self.app.copy(url))
            log.info("Link temporaneo (1 ora) copiato per s3://%s/%s", bucket, keys[0])

        self.app.run("Link temporaneo", job, quiet=True, clear_steps=False)

    def do_upload(self) -> None:
        files = filedialog.askopenfilenames(title="File da caricare", parent=self.app)
        if not files:
            return
        bucket, prefix = self.bucket.get(), self.prefix.get()
        paths = [Path(f) for f in files]

        def job(ctx):
            clash = S.existing(ctx.aws, bucket, [prefix + p.name for p in paths])
            names = "\n".join(f"• {p.name}" for p in paths[:15])
            text = (f"Caricare {len(paths)} file in s3://{bucket}/{prefix}?\n\n{names}"
                    + (f"\n\nATTENZIONE: {len(clash)} esistono gia' e verranno sovrascritti:\n"
                       + "\n".join(clash[:10]) if clash else ""))
            ok = self.app.call_main(lambda: self.app.confirm_write("Carica file", text))
            if not ok:
                return "Caricamento annullato."
            n = S.upload(ctx.aws, bucket, prefix, paths, ctx.cancel, ctx.progress)
            return f"{n} file caricati."

        self.app.run("Caricamento", job, then=lambda: self.go(prefix))

    def do_delete(self) -> None:
        keys = self._keys()
        if not keys:
            return
        bucket, prefix = self.bucket.get(), self.prefix.get()

        def job(ctx):
            objs = S.expand(ctx.aws, bucket, keys, ctx.cancel)
            if not objs:
                return "Niente da cancellare."
            total = sum(o.size for o in objs)
            sample = "\n".join(f"• {o.key}" for o in objs[:12]) + ("\n…" if len(objs) > 12 else "")
            text = (f"CANCELLARE {len(objs)} oggetti ({_mb(total)}) da s3://{bucket}?\n\n{sample}\n\n"
                    f"La cancellazione non si annulla (salvo versioning del bucket). I file referenziati dal DB "
                    f"resteranno con il link rotto.")
            ok = self.app.call_main(lambda: self.app.confirm_typed(text, bucket))
            if not ok:
                return "Cancellazione annullata."
            r = S.delete(ctx.aws, objs, bucket, ctx.cancel, ctx.progress)
            return f"{r['cancellati']} oggetti cancellati, {r['errori']} errori."

        self.app.run("Cancellazione", job, then=lambda: self.go(prefix))


# ==================================================================== archivi della retention

class ArchivesTab(Tab):
    title = "Archivi"
    features = ("archivi",)

    def build(self, tab: ttk.Frame) -> None:
        self._objs: list[A.ArchiveObject] = []
        self._rec: A.Reconciliation | None = None
        self._last_rows: list[dict] = []
        ttk.Label(tab, text="Righe uscite dal DB per le policy di retention «archive_cold»: JSON Lines compressi nel "
                            "bucket di archivio. Sola lettura: non si modificano, non si cancellano e non si reinseriscono.",
                  foreground="#666").pack(anchor="w")
        top = ttk.Frame(tab)
        top.pack(fill="x", pady=(4, 0))
        self.gate(ttk.Button(top, text="Elenca archivi", command=self.do_list), "archivi").pack(side="left")
        self.gate(ttk.Button(top, text="Confronta con l'inventario (DB)", command=self.do_reconcile),
                  "archivi", lambda: bool(self._objs) and self.app.allowed("db_accesso")).pack(side="left", padx=6)
        self.rec_var = tk.StringVar()
        ttk.Label(top, textvariable=self.rec_var, wraplength=900).pack(side="left", padx=8)

        body = ttk.Frame(tab)
        body.pack(fill="both", expand=True, pady=(6, 0))
        ft = ttk.LabelFrame(body, text=" Per tabella di origine ", padding=4)
        ft.pack(side="left", fill="both", expand=True)
        cols = (("tabella", 230), ("archivi", 60), ("dimensione", 90), ("dal", 130), ("al", 130), ("righe", 80),
                ("problemi", 70))
        self.tables = ttk.Treeview(ft, columns=[c for c, _ in cols], show="headings", height=8)
        for c, w in cols:
            self.tables.heading(c, text=c)
            self.tables.column(c, width=w, stretch=c == "tabella")
        self.tables.tag_configure("ko", background="#fde2e2")
        self.tables.pack(fill="both", expand=True)
        self.tables.bind("<<TreeviewSelect>>", lambda _e: self._show_objects())

        fo = ttk.LabelFrame(body, text=" Archivi della tabella ", padding=4)
        fo.pack(side="left", fill="both", expand=True, padx=(8, 0))
        cols = (("archivio", 330), ("dimensione", 80), ("scritto", 130), ("classe", 90))
        self.objs = ttk.Treeview(fo, columns=[c for c, _ in cols], show="headings", height=8, selectmode="extended")
        for c, w in cols:
            self.objs.heading(c, text=c)
            self.objs.column(c, width=w, stretch=c == "archivio")
        self.objs.pack(fill="both", expand=True)

        fb = ttk.Frame(tab)
        fb.pack(fill="x", pady=(6, 0))
        has = lambda: bool(self._objs)  # noqa: E731
        self.gate(ttk.Button(fb, text="Anteprima", command=self.do_preview), "archivi", has).pack(side="left")
        self.gate(ttk.Button(fb, text="Scarica + CSV…", command=self.do_export), "archivi", has).pack(side="left", padx=6)
        ttk.Label(fb, text="   Cerca:").pack(side="left")
        self.text = tk.StringVar()
        ttk.Entry(fb, textvariable=self.text, width=28).pack(side="left", padx=4)
        ttk.Label(fb, text="nella colonna (vuoto = tutte):").pack(side="left")
        self.column = tk.StringVar()
        ttk.Entry(fb, textvariable=self.column, width=20).pack(side="left", padx=4)
        self.gate(ttk.Button(fb, text="Cerca negli archivi selezionati", command=self.do_search), "archivi",
                  has).pack(side="left", padx=4)
        ttk.Label(tab, text="Senza archivi selezionati la ricerca guarda tutti quelli della tabella scelta.",
                  foreground="#666").pack(anchor="w")

        fr = ttk.LabelFrame(tab, text=" Retention (comando «retention» del backend, lanciato come task) ", padding=6)
        fr.pack(fill="x", pady=(6, 0))
        ttk.Label(fr, text="Schema (vuoto = tutti):").pack(side="left")
        self.ret_schema = tk.StringVar()
        ttk.Entry(fr, textvariable=self.ret_schema, width=14).pack(side="left", padx=4)
        self.ret_seasonal = tk.BooleanVar(value=True)
        ttk.Checkbutton(fr, text="con le policy stagionali", variable=self.ret_seasonal).pack(side="left", padx=4)
        self.gate(ttk.Button(fr, text="Prova (nessuna scrittura)", command=lambda: self.do_retention(False)),
                  "retention").pack(side="left", padx=6)
        self.gate(ttk.Button(fr, text="Esegui…", command=lambda: self.do_retention(True)), "retention").pack(side="left")
        self.gate(ttk.Button(fr, text="Ultime corse", command=self.do_runs), "db_accesso|retention").pack(side="left",
                                                                                                        padx=6)
        ttk.Label(fr, text="Il backend non ha uno scheduler: la retention gira solo quando la si lancia. Prima la "
                           "prova, poi Esegui.", foreground="#666").pack(side="left", padx=8)

    def on_env_changed(self) -> None:
        self._objs, self._rec = [], None
        self.rec_var.set("")
        self.tables.delete(*self.tables.get_children())
        self.objs.delete(*self.objs.get_children())

    def do_list(self) -> None:
        def job(ctx):
            objs = A.list_archives(ctx.aws, ctx.cancel, ctx.progress)
            self.app.render(lambda: self._set(objs, None))
            return None

        self.app.run("Elenco degli archivi", job, quiet=True)

    def do_reconcile(self) -> None:
        if not messagebox.askyesno("Inventario", "Leggo privacy.privacy_archive_manifests dal DB dell'ambiente "
                                   f"{self.env.name}: l'accesso dal PC si apre per la durata della lettura.\n\nProcedere?",
                                   parent=self.app):
            return
        objs = self._objs

        def job(ctx):
            rec = A.reconcile(objs, A.read_manifest(ctx.aws), A.archive_origin(ctx.aws))
            self.app.render(lambda: self._set(objs, rec))
            return rec.text()

        self.app.run("Confronto con l'inventario", job)

    def _set(self, objs: list[A.ArchiveObject], rec: A.Reconciliation | None) -> None:
        self._objs, self._rec = objs, rec
        self.rec_var.set(rec.text() if rec else f"{len(objs)} archivi, {_mb(sum(o.size for o in objs))}")
        self.tables.delete(*self.tables.get_children())
        for g in A.by_table(objs, rec):
            fmt = lambda w: f"{w[:4]}-{w[4:6]}-{w[6:8]} {w[9:11]}:{w[11:13]}" if w else ""  # noqa: E731
            self.tables.insert("", "end", iid=g.source, tags=("ko",) if g.issues else (), values=(
                g.source, g.objects, _mb(g.size), fmt(g.first), fmt(g.last), "" if g.rows is None else g.rows,
                g.issues or ""))
        self.objs.delete(*self.objs.get_children())
        self.app._refresh_buttons()

    def _show_objects(self) -> None:
        sel = self.tables.selection()
        self.objs.delete(*self.objs.get_children())
        if not sel:
            return
        for o in sorted((o for o in self._objs if o.source == sel[0]), key=lambda x: x.key, reverse=True):
            self.objs.insert("", "end", iid=o.key, values=(o.key.rsplit("/", 1)[-1], _mb(o.size),
                                                           o.modified.strftime("%Y-%m-%d %H:%M") if o.modified else "",
                                                           o.storage_class))

    def _chosen(self, all_if_none: bool) -> list[str]:
        sel = list(self.objs.selection())
        if sel or not all_if_none:
            return sel
        return list(self.objs.get_children())

    def do_preview(self) -> None:
        keys = self._chosen(False)
        if len(keys) != 1:
            messagebox.showinfo("Anteprima", "Selezionare un archivio.", parent=self.app)
            return

        def job(ctx):
            cols, rows, more = A.preview(ctx.aws, keys[0])
            note = f"{keys[0]} - {len(rows)} righe" + (" (prime righe: l'archivio ne contiene altre)" if more else "")
            self.app.render(lambda: TableWindow(self.app, "Anteprima archivio", cols, rows, note))

        self.app.run("Anteprima", job, quiet=True, clear_steps=False)

    def do_export(self) -> None:
        keys = self._chosen(False)
        if not keys:
            messagebox.showinfo("Esporta", "Selezionare uno o piu' archivi.", parent=self.app)
            return
        folder = self.env.work_path / "archivi" / self.env.name

        def job(ctx):
            for k in keys:
                A.export_archive(ctx.aws, k, folder, ctx.cancel)
            self.app.render(lambda: self.app.open_folder(folder))
            return f"{len(keys)} archivi scaricati e convertiti in CSV in\n{folder}\n\nContengono dati personali: " \
                   f"non condividerli e cancellarli dopo l'uso."

        self.app.run("Scarico archivi", job)

    def do_search(self) -> None:
        text = self.text.get().strip()
        keys = self._chosen(True)
        if not text or not keys:
            messagebox.showinfo("Cerca", "Indicare il testo e scegliere una tabella (o degli archivi).", parent=self.app)
            return
        column = self.column.get().strip() or None

        def job(ctx):
            rows = A.search(ctx.aws, keys, text, column, ctx.cancel, ctx.progress)
            cols = A._columns(rows) if rows else ["_archivio"]
            self._last_rows = rows
            self.app.render(lambda: TableWindow(self.app, f"Ricerca di «{text}»", cols, rows,
                                                f"{len(rows)} righe in {len(keys)} archivi", self._export_results))
            return None

        self.app.run("Ricerca negli archivi", job, quiet=True)

    def do_retention(self, apply: bool) -> None:
        schema = self.ret_schema.get().strip() or None
        seasonal = self.ret_seasonal.get()
        what = f"schema {schema}" if schema else "tutti gli schemi"
        if apply:
            text = (f"ESECUZIONE REALE della retention sull'ambiente {self.env.name} ({what}"
                    f"{', policy stagionali comprese' if seasonal else ''}).\n\nLe policy attive cancellano, "
                    f"anonimizzano e archiviano righe e file in modo definitivo. Conviene aver fatto prima la prova "
                    f"(ed eventualmente uno snapshot da Gestione DB).")
            if not self.app.confirm_typed(text):
                return
        elif not messagebox.askyesno("Retention", f"Prova della retention sull'ambiente {self.env.name} ({what}): "
                                     f"valuta le policy e scrive il verbale, nessun dato toccato.\n\nProcedere?",
                                     parent=self.app):
            return

        def job(ctx):
            text, report = RT.run(ctx, apply, schema, seasonal)
            title = f"Retention {'eseguita' if apply else 'prova'} - {self.env.name}"
            self.app.render(lambda: TextWindow(self.app, title, "\n".join(report)))
            return text

        self.app.run("Retention", job)

    def do_runs(self) -> None:
        def job(ctx):
            cols, rows = RT.last_runs(ctx.aws)
            dicts = [dict(zip(cols, r)) for r in rows]
            self.app.render(lambda: TableWindow(self.app, f"Ultime corse di retention - {self.env.name}", cols, dicts,
                                                f"{len(rows)} righe di privacy.privacy_retention_runs"))

        self.app.run("Ultime corse", job, quiet=True, clear_steps=False)

    def _export_results(self) -> None:
        if not self._last_rows:
            return
        default = f"ricerca_{datetime.now():%Y%m%d_%H%M%S}.csv"
        folder = self.env.work_path / "archivi" / self.env.name
        folder.mkdir(parents=True, exist_ok=True)
        path = filedialog.asksaveasfilename(parent=self.app, initialdir=str(folder), initialfile=default,
                                            defaultextension=".csv", filetypes=[("CSV", "*.csv")])
        if path:
            A.export_csv(self._last_rows, Path(path))
            messagebox.showinfo("Esporta", f"Salvato {path}", parent=self.app)

