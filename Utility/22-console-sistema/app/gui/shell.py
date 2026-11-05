"""Finestra principale: ambiente, identita' e ruoli, linguette, avanzamento, passi e console.

Il lavoro gira in un thread a parte; la GUI riceve log, avanzamento, passi e richieste di conferma
tramite una coda letta con `after()` (Tkinter non e' thread-safe). Un'operazione alla volta.

Le linguette si mostrano solo se l'utente ha almeno un ruolo che ne abilita una funzione; dentro una
linguetta ogni pulsante e' legato a una funzione (`roles.FEATURES`) e resta disattivato senza il ruolo.
La linguetta «Ruoli» e' sempre visibile e dice cosa serve per ciascuna funzione.
"""
from __future__ import annotations

import logging
import os
import queue
import threading
import tkinter as tk
from tkinter import messagebox, simpledialog, ttk
from tkinter.scrolledtext import ScrolledText

from app import roles as R
from app.env import operations as O
from app.env.awsutil import Aws, Cancelled
from app.env.power import PlanConflict
from app.logging_setup import LOGGER_NAME, setup_logging
from app.settings import load_settings

log = logging.getLogger(LOGGER_NAME)

TAGS = {O.OK: "ok", O.WARN: "warn", O.KO: "ko", O.SKIP: "skip", O.RUN: "run"}
ROW_COLORS = (("ok", "#e3f5e1"), ("warn", "#fff4d6"), ("ko", "#fde2e2"), ("skip", "#eeeeee"), ("run", "#e6f0fb"))


class Tab:
    """Base di una linguetta: frame, pulsanti legati alle funzioni, reazione al cambio di ambiente."""
    title = ""
    features: tuple[str, ...] = ()        # la linguetta si vede se almeno una e' abilitata

    def __init__(self, app: "App", nb: ttk.Notebook):
        self.app = app
        self.frame = ttk.Frame(nb, padding=8)
        self._gated: list[tuple[ttk.Widget, str, object]] = []
        self.build(self.frame)

    def build(self, parent: ttk.Frame) -> None:
        raise NotImplementedError

    def gate(self, widget, feature: str, extra=None):
        """Lega `widget` alla funzione: attivo solo col ruolo, a GUI libera e se `extra()` e' vero."""
        self._gated.append((widget, feature, extra))
        return widget

    def refresh_state(self, busy: bool) -> None:
        for widget, feature, extra in self._gated:
            ok = not busy and self.app.allowed(feature) and (extra is None or extra())
            widget.state(["!disabled"] if ok else ["disabled"])

    def visible(self) -> bool:
        return any(self.app.allowed(f) for f in self.features)

    def on_env_changed(self) -> None:
        """Il cambio di ambiente invalida i dati mostrati: ogni linguetta li svuota."""

    def on_identity(self) -> None:
        """Chiamata quando i ruoli sono stati letti (anche al cambio di ambiente)."""

    def autoload(self):
        """Lettura da fare da sola dopo i ruoli o il cambio di ambiente: una funzione `f(then)` o None."""
        return None

    @property
    def env(self):
        return self.app.env


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.settings = load_settings()
        self.env = self.settings.env(self.settings.ambiente_iniziale)
        self.identity: R.Identity | None = None
        self.title("22 - Console del nuovo sistema")
        self.geometry("1500x960")
        self.minsize(1180, 720)

        self._events: queue.Queue = queue.Queue()
        self._busy = False
        self._cancel = threading.Event()
        self.tabs: list[Tab] = []

        self._build()
        self.log_file = setup_logging(self.settings.log_path, self._on_log)
        log.info("Configurazione: %s", self.settings.path)
        log.info("Log della corsa: %s", self.log_file)
        self.after(100, self._drain)
        self.protocol("WM_DELETE_WINDOW", self._on_close)
        self.after(300, self.do_identity)

    # ================================================================ layout
    def _build(self) -> None:
        from app.gui import tab_data, tab_dbadmin, tab_env, tab_infra, tab_tools  # noqa: PLC0415 - evita l'import circolare

        head = ttk.Frame(self, padding=(10, 8, 10, 2))
        head.pack(fill="x")
        row = ttk.Frame(head)
        row.pack(fill="x")
        ttk.Label(row, text="Ambiente:").pack(side="left")
        self.env_var = tk.StringVar(value=self.env.name)
        self.env_combo = ttk.Combobox(row, textvariable=self.env_var, values=list(self.settings.ambienti),
                                      width=8, state="readonly")
        self.env_combo.pack(side="left", padx=(4, 10))
        self.env_combo.bind("<<ComboboxSelected>>", lambda _e: self._change_env())
        self.banner_var = tk.StringVar()
        self.banner = tk.Label(row, textvariable=self.banner_var, font=("Segoe UI", 11, "bold"))
        self.banner.pack(side="left")
        self.btn_identity = ttk.Button(row, text="Rileggi utente e ruoli", command=self.do_identity)
        self.btn_identity.pack(side="right")
        self.identity_var = tk.StringVar(value="Utente e ruoli: non ancora letti")
        ttk.Label(head, textvariable=self.identity_var).pack(anchor="w")
        ttk.Label(head, text=f"Profilo AWS: {self.settings.aws.profile} ({self.settings.aws.region})   -   "
                             f"Repository: {self.settings.repository.percorso}   -   Configurazione: "
                             f"{self.settings.path}", foreground="#666").pack(anchor="w")

        self.nb = ttk.Notebook(self)
        self.nb.pack(fill="both", expand=False, padx=10, pady=4)
        for cls in (tab_env.StatusTab, tab_infra.InfraTab, tab_env.DeployTab, tab_data.BackupTab, tab_env.DatabaseTab,
                    tab_dbadmin.DbAdminTab, tab_data.FilesTab, tab_data.ArchivesTab, tab_tools.LogsTab,
                    tab_tools.ScanTab, tab_tools.CostsTab, tab_tools.RolesTab):
            tab = cls(self, self.nb)
            self.tabs.append(tab)
            self.nb.add(tab.frame, text=f" {tab.title} ")

        bar = ttk.Frame(self, padding=(10, 2, 10, 2))
        bar.pack(fill="x")
        self.progress = ttk.Progressbar(bar, mode="indeterminate")
        self.progress.pack(side="left", fill="x", expand=True)
        self.btn_cancel = ttk.Button(bar, text="Interrompi", command=self._request_cancel)
        self.btn_cancel.pack(side="left", padx=(8, 0))
        self.status = tk.StringVar(value="")
        ttk.Label(self, textvariable=self.status, padding=(10, 0, 10, 2)).pack(fill="x")

        panes = ttk.PanedWindow(self, orient="horizontal")
        panes.pack(fill="both", expand=True, padx=10, pady=(0, 8))
        steps = ttk.Frame(panes)
        self.steps = ttk.Treeview(steps, columns=("passo", "stato", "note"), show="headings", height=6)
        for key, title, width in (("passo", "Passo", 240), ("stato", "Stato", 80), ("note", "Note", 380)):
            self.steps.heading(key, text=title)
            self.steps.column(key, width=width, stretch=key == "note")
        for tag, color in ROW_COLORS:
            self.steps.tag_configure(tag, background=color)
        self.steps.pack(fill="both", expand=True)
        panes.add(steps, weight=2)
        self.console = ScrolledText(panes, height=10, font=("Consolas", 9), state="disabled")
        for level, color in (("WARNING", "#9a6700"), ("ERROR", "#b00020")):
            self.console.tag_configure(level, foreground=color)
        panes.add(self.console, weight=3)
        self._show_env()
        self._refresh_buttons()

    def _show_env(self) -> None:
        e = self.env
        if e.is_prod:
            self.banner_var.set(f"PRODUZIONE - stack {e.stack} - {e.descrizione}")
            self.banner.configure(fg="#b00020")
        else:
            self.banner_var.set(f"{e.name.upper()} - stack {e.stack} - {e.descrizione}")
            self.banner.configure(fg="#1a56db")

    def _change_env(self) -> None:
        name = self.env_var.get()
        if self._busy:
            self.env_var.set(self.env.name)
            return
        if name == self.env.name:
            return
        if self.settings.ambienti.get(name) and name == "prod" and not messagebox.askyesno(
                "Produzione", "Passare all'ambiente di PRODUZIONE?\n\nOgni scrittura chiede conferma scrivendo il "
                              "nome dello stack.", parent=self):
            self.env_var.set(self.env.name)
            return
        self.env = self.settings.env(name)
        log.info("Ambiente: %s (stack %s)", self.env.name, self.env.stack)
        self._show_env()
        for tab in self.tabs:
            tab.on_env_changed()
        self._apply_identity()
        self._autoload()

    # ================================================================ ruoli
    def allowed(self, feature: str) -> bool:
        """`feature` puo' essere "a|b": basta una delle due."""
        return any(R.allowed(self.identity, self.env, f) for f in feature.split("|"))

    def do_identity(self) -> None:
        def job(ctx):
            me = R.detect(ctx.aws)
            self.render(lambda: self._set_identity(me))

        self.run("Utente e ruoli", job, then=self._autoload, clear_steps=False, quiet=True)

    def _autoload(self, pending: list | None = None) -> None:
        """Letture automatiche delle linguette, una dopo l'altra (un'operazione alla volta)."""
        if pending is None:
            if self.identity is None:
                return
            pending = [f for f in (t.autoload() for t in self.tabs) if f is not None]
        if not pending or self._busy:
            return
        first, rest = pending[0], pending[1:]
        first(lambda: self._autoload(rest) if rest else None)

    def _set_identity(self, me: R.Identity) -> None:
        self.identity = me
        self._apply_identity()

    def _apply_identity(self) -> None:
        me = self.identity
        if me is None:
            return
        who = me.user or me.arn
        self.identity_var.set(f"Account {me.account} - utente {who} - ruoli: {me.label(self.env.gruppi_prefisso)}"
                              + (f"   ({me.note})" if me.note else ""))
        current = self.nb.select()
        for tab in self.tabs:
            if tab.visible():
                self.nb.add(tab.frame)          # rimostra una linguetta nascosta
            else:
                self.nb.hide(tab.frame)
        if current and self.nb.tab(current, "state") == "hidden":
            self.nb.select(self.tabs[-1].frame)
        for tab in self.tabs:
            tab.on_identity()
        self._refresh_buttons()

    # ================================================================ thread / coda
    def _on_log(self, level: str, text: str) -> None:
        self._events.put(("log", level, text))

    def progress_cb(self, phase: str, done: float, total: float, text: str) -> None:
        pct = f" {int(100 * done / total)}%" if total else ""
        self._events.put(("progress", phase + pct, text))

    def step_cb(self, key: str, label: str, status: str, notes: str) -> None:
        self._events.put(("step", key, label, status, notes))

    def call_main(self, fn):
        """Esegue `fn` nel thread della GUI e ne restituisce il risultato (per conferme e password)."""
        holder: dict = {}
        done = threading.Event()
        self._events.put(("call", fn, holder, done))
        done.wait()
        if "error" in holder:
            raise holder["error"]
        return holder.get("value")

    def confirm_cb(self, title: str, text: str) -> bool:
        return bool(self.call_main(lambda: messagebox.askyesno(title, text, parent=self)))

    def ask_password(self, prompt: str) -> str | None:
        return self.call_main(lambda: simpledialog.askstring("PostgreSQL locale", prompt, show="•", parent=self))

    def render(self, fn) -> None:
        self._events.put(("render", fn))

    def _drain(self) -> None:
        try:
            while True:
                ev = self._events.get_nowait()
                kind = ev[0]
                if kind == "log":
                    self.console.configure(state="normal")
                    self.console.insert("end", ev[2] + "\n", ev[1])
                    self.console.see("end")
                    self.console.configure(state="disabled")
                elif kind == "progress":
                    self.status.set(f"{ev[1]}: {ev[2]}")
                elif kind == "step":
                    _, key, label, status, notes = ev
                    values, tags = (label, status, notes), (TAGS.get(status, ""),)
                    if self.steps.exists(key):
                        self.steps.item(key, values=values, tags=tags)
                    else:
                        self.steps.insert("", "end", iid=key, values=values, tags=tags)
                    self.steps.see(key)
                elif kind == "call":
                    _, fn, holder, done = ev
                    try:
                        holder["value"] = fn()
                    except Exception as exc:  # noqa: BLE001
                        holder["error"] = exc
                    done.set()
                elif kind == "render":
                    ev[1]()
                elif kind == "done":
                    self._busy = False
                    self.progress.stop()
                    self._refresh_buttons()
                    if ev[1]:
                        title, text, level = ev[1]
                        self.status.set(f"{title}: {text.splitlines()[0]}")
                        {"error": messagebox.showerror, "warning": messagebox.showwarning}.get(
                            level, messagebox.showinfo)(title, text, parent=self)
                    if ev[2]:
                        self.after(50, ev[2])
        except queue.Empty:
            pass
        self.after(100, self._drain)

    def _refresh_buttons(self) -> None:
        self.btn_cancel.state(["!disabled"] if self._busy else ["disabled"])
        self.btn_identity.state(["disabled"] if self._busy else ["!disabled"])
        self.env_combo.state(["disabled"] if self._busy else ["readonly", "!disabled"])
        for tab in self.tabs:
            tab.refresh_state(self._busy)

    @property
    def busy(self) -> bool:
        return self._busy

    def run(self, title: str, fn, then=None, clear_steps: bool = True, quiet: bool = False,
            silent: bool = False) -> None:
        """Esegue `fn(ctx)` in un thread; `fn` restituisce il testo dell'esito (o None).

        `then` e' chiamata nel thread della GUI a lavoro finito (anche dopo un errore). `silent`: un
        errore va nel log e nella riga di stato, senza finestra (letture automatiche).
        """
        if self._busy:
            return
        self._busy = True
        self._cancel.clear()
        self.progress.start(12)
        self._refresh_buttons()
        self.status.set(f"{title}…")
        if clear_steps:
            self.steps.delete(*self.steps.get_children())
        env = self.env

        def worker():
            msg = None
            try:
                aws = Aws(env)
                ctx = O.Ctx(aws, self._cancel, self.progress_cb, self.step_cb, self.confirm_cb)
                text = fn(ctx)
                if text and not quiet:
                    msg = (title, text, "info")
            except Cancelled:
                log.warning("%s: interrotto o non confermato", title)
                msg = (title, "Operazione interrotta o non confermata.", "warning")
            except PlanConflict as exc:
                msg = (title, str(exc), "warning")
            except Exception as exc:  # noqa: BLE001 - qualunque errore va mostrato, non perso
                if silent:
                    log.warning("%s: %s", title, exc)
                    self._events.put(("progress", title, f"non riuscito: {str(exc).splitlines()[0][:160]}"))
                else:
                    log.exception("%s: errore", title)
                    msg = (title, f"{type(exc).__name__}: {exc}\n\nLog: {self.log_file}", "error")
            self._events.put(("done", msg, then))

        threading.Thread(target=worker, daemon=True).start()

    def _request_cancel(self) -> None:
        if self._busy:
            self._cancel.set()
            self.status.set("Interruzione richiesta…")

    # ================================================================ utilita' per le linguette
    def confirm_typed(self, text: str, token: str | None = None) -> bool:
        """Conferma scrivendo un testo: il nome dello stack (o `token`)."""
        token = token or self.env.stack
        got = simpledialog.askstring("Conferma", f"{text}\n\nPer confermare scrivere: {token}", parent=self)
        if got != token:
            if got is not None:
                messagebox.showwarning("Conferma", "Testo diverso: operazione annullata.", parent=self)
            return False
        return True

    def confirm_write(self, title: str, text: str) -> bool:
        """Conferma di una scrittura: in produzione si scrive il nome dello stack, altrimenti si/no."""
        if self.env.is_prod:
            return self.confirm_typed(f"PRODUZIONE - {title}\n\n{text}")
        return messagebox.askyesno(title, text, parent=self)

    def copy(self, text: str) -> None:
        if text and text != "-":
            self.clipboard_clear()
            self.clipboard_append(text)
            self.status.set("Copiato negli appunti")

    @staticmethod
    def open_folder(path) -> None:
        path.mkdir(parents=True, exist_ok=True)
        os.startfile(path)  # noqa: S606 - cartella locale della utility

    # ================================================================ chiusura
    def _on_close(self) -> None:
        if self._busy and not messagebox.askyesno(
                "Operazione in corso", "C'e' un'operazione in corso. Chiudere comunque?\n\nUna manutenzione "
                "aperta scade da sola; le regole di accesso al DB si tolgono al prossimo avvio.", parent=self):
            return
        self._cancel.set()
        for h in log.handlers:
            h.flush()
        self.destroy()


def main() -> None:
    App().mainloop()
