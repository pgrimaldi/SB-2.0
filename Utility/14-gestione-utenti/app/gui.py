"""GUI Tkinter: utenti IAM dell'account, con ruoli componibili e chiavi d'accesso.

Il lavoro gira in un thread a parte; la GUI riceve log, avanzamento e passi tramite una coda letta
con `after()` (Tkinter non e' thread-safe). Le finestre di dialogo stanno nel thread della GUI: il
thread di lavoro restituisce il risultato e la GUI lo mostra.
"""
from __future__ import annotations

import logging
import os
import queue
import threading
import tkinter as tk
from datetime import datetime, timezone
from tkinter import filedialog, messagebox, simpledialog, ttk
from tkinter.scrolledtext import ScrolledText

from app.awsutil import Aws, Cancelled
from app.logging_setup import LOGGER_NAME, setup_logging
from app.roles import Catalog, load_catalog, sync_roles
from app.settings import LOCAL_SETTINGS_PATH, load_settings
from app.users import (NewKey, UserInfo, create_user, credentials_text, default_file_name, delete_key,
                       delete_user, generate_password, list_users, new_key, save_credentials, set_key_status,
                       set_password, set_roles)

log = logging.getLogger(LOGGER_NAME)

USER_COLUMNS = (
    ("console", "Console", 70, "center"),
    ("last", "Ultimo accesso", 130, "w"),
    ("mfa", "MFA", 50, "center"),
    ("keys", "Chiavi / stato", 130, "w"),
    ("created", "Creato", 130, "w"),
    ("notes", "Note", 420, "w"),
)
ROLE_COLUMNS = (
    ("scope", "Ambito", 160, "w"),
    ("item", "Gruppo / policy", 240, "w"),
    ("status", "Stato", 110, "center"),
    ("notes", "Note", 800, "w"),
)
PHASES = {"policy": "Policy", "gruppi": "Gruppi", "utenti": "Lettura utenti", "cancellazione": "Cancellazione"}
TAGS = {"OK": "ok", "creata": "ok", "creato": "ok", "aggiornata": "ok", "aggiornato": "ok",
        "da creare": "warn", "da aggiornare": "warn", "con avvisi": "warn", "da verificare": "skip",
        "KO": "ko", "saltato": "skip", "in corso": "run", "da definire": "skip"}
OLD_KEY_DAYS = 90


def _date(d: datetime | None) -> str:
    return d.astimezone().strftime("%d/%m/%Y %H:%M") if d else "-"


class RoleChecks(ttk.Frame):
    """Una riga di caselle per ambito (dev, prod, account). I preset (fullstack) accendono e riflettono i
    ruoli che contengono; i ruoli da definire sono visibili ma disattivati."""

    def __init__(self, master, cat: Catalog, selected: list[str] | None = None):
        super().__init__(master)
        self.cat = cat
        self.vars: dict[str, tk.BooleanVar] = {}
        self.preset_vars: dict[tuple[str, str], tk.BooleanVar] = {}
        selected = set(selected or [])
        for row, scope in enumerate(cat.scopes):
            head = ttk.Label(self, text=f"{scope.nome}:", width=9, font=("Segoe UI", 9, "bold"))
            head.grid(row=row, column=0, sticky="w")
            _Tooltip(head, scope.descrizione + (" (da definire)" if scope.da_definire else ""))
            col = 1
            for r in scope.ruoli:
                v = tk.BooleanVar(value=r.key in selected)
                self.vars[r.key] = v
                cb = ttk.Checkbutton(self, text=r.nome, variable=v, command=self._sync)
                if r.da_definire:
                    cb.state(["disabled"])
                cb.grid(row=row, column=col, sticky="w", padx=(0, 12))
                _Tooltip(cb, r.descrizione)
                col += 1
            for p, members in scope.preset.items():
                v = tk.BooleanVar()
                self.preset_vars[(scope.nome, p)] = v
                cb = ttk.Checkbutton(self, text=p, variable=v, command=lambda s=scope.nome, n=p: self._preset(s, n))
                if scope.da_definire:
                    cb.state(["disabled"])
                cb.grid(row=row, column=col, sticky="w", padx=(0, 12))
                _Tooltip(cb, " + ".join(members))
                col += 1
            if scope.da_definire:
                ttk.Label(self, text="da definire", foreground="#999").grid(row=row, column=col, sticky="w")
        self._sync()

    def _preset(self, scope: str, name: str) -> None:
        on = self.preset_vars[(scope, name)].get()
        for r in self.cat.scope(scope).preset[name]:
            self.vars[f"{scope}:{r}"].set(on)
        self._sync()

    def _sync(self) -> None:
        for (scope, p), v in self.preset_vars.items():
            v.set(all(self.vars[f"{scope}:{m}"].get() for m in self.cat.scope(scope).preset[p]))

    def get(self) -> list[str]:
        return [n for n, v in self.vars.items() if v.get()]

    def clear(self) -> None:
        for v in [*self.vars.values(), *self.preset_vars.values()]:
            v.set(False)


class _Tooltip:
    def __init__(self, widget, text: str):
        self.widget, self.text, self.tip = widget, text, None
        widget.bind("<Enter>", self._show)
        widget.bind("<Leave>", self._hide)

    def _show(self, _e=None):
        if self.tip or not self.text:
            return
        x, y = self.widget.winfo_rootx() + 10, self.widget.winfo_rooty() + self.widget.winfo_height() + 4
        self.tip = tk.Toplevel(self.widget)
        self.tip.wm_overrideredirect(True)
        self.tip.wm_geometry(f"+{x}+{y}")
        tk.Label(self.tip, text=self.text, background="#ffffe0", relief="solid", borderwidth=1,
                 wraplength=420, justify="left").pack()

    def _hide(self, _e=None):
        if self.tip:
            self.tip.destroy()
            self.tip = None


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.settings = load_settings()
        self.catalog = load_catalog()
        self.title("14 - Gestione degli utenti AWS")
        self.geometry("1480x900")
        self.minsize(1150, 700)

        self._events: queue.Queue = queue.Queue()
        self._busy = False
        self._cancel = threading.Event()
        self._users: dict[str, UserInfo] = {}
        self._aws: Aws | None = None
        self._aws_profile = None

        self._build()
        self.log_file = setup_logging(self.settings.log_path, self._on_log)
        log.info("Configurazione: %s", LOCAL_SETTINGS_PATH)
        log.info("Log della corsa: %s", self.log_file)
        self.after(100, self._drain)
        self.protocol("WM_DELETE_WINDOW", self._on_close)
        self.after(200, self.do_list)

    # ------------------------------------------------------------------ layout
    def _build(self) -> None:
        s = self.settings
        head = ttk.Frame(self, padding=(10, 8, 10, 4))
        head.pack(fill="x")
        self.account_var = tk.StringVar(value=f"Account atteso {s.account_atteso} - non ancora verificato")
        tk.Label(head, textvariable=self.account_var, fg="#b00020",
                 font=("Segoe UI", 10, "bold")).pack(anchor="w")
        envs = ", ".join(f"{sc.nome} = stack {sc.stack}" + (" (da definire)" if sc.da_definire else "")
                         for sc in self.catalog.scopes if sc.stack)
        ttk.Label(head, text=f"Utenti IAM dell'account (prod legacy compresa: IAM e' unico). Ambienti: {envs}. "
                             f"Schede delle chiavi: {s.schede_path}   -   Configurazione: {LOCAL_SETTINGS_PATH}",
                  foreground="#666").pack(anchor="w")

        top = ttk.Frame(self, padding=(10, 4))
        top.pack(fill="x")

        f1 = ttk.LabelFrame(top, text=" 1. Ruoli ", padding=8)
        f1.pack(side="left", fill="y")
        r = ttk.Frame(f1)
        r.pack(anchor="w")
        ttk.Label(r, text="Profilo AWS:").pack(side="left")
        self.profile_var = tk.StringVar(value=s.aws.profile)
        ttk.Entry(r, textvariable=self.profile_var, width=14).pack(side="left", padx=4)
        self.btn_verify = ttk.Button(f1, text="Verifica ruoli", command=lambda: self.do_roles(False))
        self.btn_verify.pack(fill="x", pady=(6, 0))
        self.btn_prepare = ttk.Button(f1, text="Prepara ruoli…", command=lambda: self.do_roles(True))
        self.btn_prepare.pack(fill="x", pady=(4, 0))
        self.btn_list = ttk.Button(f1, text="Aggiorna elenco utenti", command=self.do_list)
        self.btn_list.pack(fill="x", pady=(4, 0))

        f2 = ttk.LabelFrame(top, text=" 2. Nuovo utente ", padding=8)
        f2.pack(side="left", fill="both", expand=True, padx=8)
        g = ttk.Frame(f2)
        g.pack(anchor="w", fill="x")
        ttk.Label(g, text="Nome:").grid(row=0, column=0, sticky="w")
        self.name_var = tk.StringVar()
        ttk.Entry(g, textvariable=self.name_var, width=28).grid(row=0, column=1, sticky="w", padx=4)
        ttk.Label(g, text="Password:").grid(row=0, column=2, sticky="w", padx=(12, 0))
        self.pwd_var = tk.StringVar()
        self.pwd_entry = ttk.Entry(g, textvariable=self.pwd_var, width=28, show="•")
        self.pwd_entry.grid(row=0, column=3, sticky="w", padx=4)
        self.pwd_show = tk.BooleanVar(value=False)
        ttk.Checkbutton(g, text="Mostra", variable=self.pwd_show,
                        command=lambda: self.pwd_entry.configure(show="" if self.pwd_show.get() else "•")
                        ).grid(row=0, column=4, sticky="w")
        ttk.Button(g, text="Genera", command=self._gen_password).grid(row=0, column=5, sticky="w", padx=4)
        ttk.Label(f2, text="Ruoli per ambiente (si sommano; passare col mouse per la descrizione):",
                  foreground="#555").pack(anchor="w", pady=(6, 2))
        self.new_roles = RoleChecks(f2, self.catalog)
        self.new_roles.pack(anchor="w")
        self.btn_create = ttk.Button(f2, text="Crea utente e chiave…", command=self.do_create)
        self.btn_create.pack(anchor="e", pady=(6, 0))

        f3 = ttk.LabelFrame(top, text=" 3. Utente selezionato ", padding=8)
        f3.pack(side="left", fill="y")
        self.btn_roles = ttk.Button(f3, text="Ruoli…", command=self.do_set_roles)
        self.btn_pwd = ttk.Button(f3, text="Password…", command=self.do_set_password)
        self.btn_key = ttk.Button(f3, text="Nuova chiave…", command=self.do_new_key)
        self.btn_key_toggle = ttk.Button(f3, text="Disattiva / riattiva chiave", command=self.do_toggle_key)
        self.btn_key_del = ttk.Button(f3, text="Cancella chiave…", command=self.do_delete_key)
        self.btn_user_del = ttk.Button(f3, text="Cancella utente…", command=self.do_delete_user)
        for i, b in enumerate((self.btn_roles, self.btn_pwd, self.btn_key, self.btn_key_toggle,
                               self.btn_key_del, self.btn_user_del)):
            b.grid(row=i % 3, column=i // 3, sticky="ew", padx=2, pady=2)

        bar = ttk.Frame(self, padding=(10, 2, 10, 4))
        bar.pack(fill="x")
        self.progress = ttk.Progressbar(bar, maximum=1000)
        self.progress.pack(side="left", fill="x", expand=True)
        self.btn_cancel = ttk.Button(bar, text="Interrompi", command=self._request_cancel)
        self.btn_cancel.pack(side="left", padx=(8, 0))
        self.status = tk.StringVar(value="")
        ttk.Label(self, textvariable=self.status, padding=(10, 0, 10, 4)).pack(fill="x")

        panes = ttk.PanedWindow(self, orient="vertical")
        panes.pack(fill="both", expand=True, padx=10, pady=(0, 8))
        self.tabs = ttk.Notebook(panes)
        scope_columns = tuple((f"scope_{sc.nome}", f"Ruoli {sc.nome}", 170, "w") for sc in self.catalog.scopes)
        self.users_tree = self._tree(self.tabs, scope_columns + USER_COLUMNS, show="tree headings")
        self.users_tree.heading("#0", text="Utente / chiave")
        self.users_tree.column("#0", width=220, stretch=False)
        self.users_tree.bind("<<TreeviewSelect>>", lambda _e: self._refresh_buttons())
        self.tabs.add(self.users_tree.master, text=" Utenti ")
        self.roles_tree = self._tree(self.tabs, ROLE_COLUMNS)
        self.tabs.add(self.roles_tree.master, text=" Ruoli ")
        self._fill_roles()
        panes.add(self.tabs, weight=3)

        self.console = ScrolledText(panes, height=10, font=("Consolas", 9), state="disabled")
        for level, color in (("WARNING", "#9a6700"), ("ERROR", "#b00020")):
            self.console.tag_configure(level, foreground=color)
        panes.add(self.console, weight=2)
        self._refresh_buttons()

    def _tree(self, parent, columns, show: str = "headings") -> ttk.Treeview:
        frame = ttk.Frame(parent)
        tree = ttk.Treeview(frame, columns=[c[0] for c in columns], show=show)
        for key, title, width, anchor in columns:
            tree.heading(key, text=title)
            tree.column(key, width=width, anchor=anchor, stretch=key == "notes")
        for tag, color in (("ok", "#e3f5e1"), ("warn", "#fff4d6"), ("ko", "#fde2e2"), ("skip", "#eeeeee"),
                           ("run", "#e6f0fb")):
            tree.tag_configure(tag, background=color)
        ysb = ttk.Scrollbar(frame, orient="vertical", command=tree.yview)
        tree.configure(yscrollcommand=ysb.set)
        tree.grid(row=0, column=0, sticky="nsew")
        ysb.grid(row=0, column=1, sticky="ns")
        frame.rowconfigure(0, weight=1)
        frame.columnconfigure(0, weight=1)
        return tree

    def _fill_roles(self) -> None:
        t = self.roles_tree
        for item in t.get_children():
            t.delete(item)
        for r in [self.catalog.base, *self.catalog.ruoli]:
            policies = [*r.policy, *(a.rsplit("/", 1)[-1] for a in r.policy_aws), *r.policy_esistenti]
            t.insert("", "end", iid=f"group:{r.gruppo}", tags=("skip",),
                     values=(f"{r.ambito} · {r.nome}", r.gruppo, "da definire" if r.da_definire else "da verificare",
                             f"{r.descrizione}  [{', '.join(policies)}]" if policies else r.descrizione))

    # ------------------------------------------------------------------ thread/coda
    def _on_log(self, level: str, text: str) -> None:
        self._events.put(("log", level, text))

    def _progress(self, phase: str, done: float, total: float, text: str) -> None:
        self._events.put(("progress", phase, done, total, text))

    def _step(self, key, scope, item, status, notes) -> None:
        self._events.put(("step", key, scope, item, status, notes))

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
                    _, phase, done, total, text = ev
                    self.progress["value"] = 1000 * done / total if total else 0
                    pct = 100 * done / total if total else 0
                    self.status.set(f"{PHASES.get(phase, phase)}: {pct:.0f}% - {text}")
                elif kind == "step":
                    _, key, scope, item, status, notes = ev
                    values, tags = (scope, item, status, notes), (TAGS.get(status, ""),)
                    if self.roles_tree.exists(key):
                        self.roles_tree.item(key, values=values, tags=tags)
                    else:
                        self.roles_tree.insert("", "end", iid=key, values=values, tags=tags)
                    self.roles_tree.see(key)
                elif kind == "account":
                    self.account_var.set(ev[1])
                elif kind == "done":
                    _, msg, after = ev
                    self._busy = False
                    self._refresh_buttons()
                    if after:
                        after()
                    if msg:
                        title, text, *level = msg
                        self.status.set(f"{title}: {text.splitlines()[0]}")
                        show = {"error": messagebox.showerror, "warning": messagebox.showwarning}.get(
                            level[0] if level else "", messagebox.showinfo)
                        show(title, text, parent=self)
        except queue.Empty:
            pass
        self.after(100, self._drain)

    def _refresh_buttons(self) -> None:
        idle = ["!disabled"] if not self._busy else ["disabled"]
        for b in (self.btn_verify, self.btn_prepare, self.btn_list, self.btn_create):
            b.state(idle)
        user, key = self._selection()
        managed = bool(user and user.managed) and not self._busy
        for b in (self.btn_roles, self.btn_pwd, self.btn_key, self.btn_user_del):
            b.state(["!disabled"] if managed else ["disabled"])
        for b in (self.btn_key_toggle, self.btn_key_del):
            b.state(["!disabled"] if managed and key else ["disabled"])
        self.btn_cancel.state(["!disabled"] if self._busy else ["disabled"])

    def _aws_client(self) -> Aws:
        profile = self.profile_var.get().strip()
        if self._aws is None or profile != self._aws_profile:
            self.settings.aws.profile = profile
            self._aws, self._aws_profile = Aws(self.settings), profile
        return self._aws

    def _run(self, title: str, fn) -> None:
        """fn() -> (messaggio | None, callback da eseguire nel thread della GUI | None)."""
        if self._busy:
            return
        self._busy = True
        self._cancel.clear()
        self.progress["value"] = 0
        self._refresh_buttons()
        self.status.set(f"{title}…")
        aws = self._aws_client()

        def worker():
            after = None
            try:
                ident = aws.check_account()
                self._events.put(("account", f"Account {ident['Account']}  -  {ident['Arn']}"))
                msg, after = fn(aws)
            except Cancelled:
                log.warning("%s: interrotto dall'utente", title)
                msg = (title, "Operazione interrotta.", "warning")
            except Exception as exc:  # noqa: BLE001 - qualunque errore va mostrato, non perso
                log.exception("%s: errore", title)
                msg = (title, f"{type(exc).__name__}: {exc}\n\nLog: {self.log_file}", "error")
            self._events.put(("done", msg, after))

        threading.Thread(target=worker, daemon=True).start()

    def _request_cancel(self) -> None:
        if self._busy:
            self._cancel.set()
            self.status.set("Interruzione richiesta…")

    # ------------------------------------------------------------------ elenco utenti
    def _selection(self) -> tuple[UserInfo | None, str | None]:
        sel = self.users_tree.selection()
        if not sel:
            return None, None
        iid = sel[0]
        if iid.startswith("key:"):
            _, user, key = iid.split(":", 2)
            return self._users.get(user), key
        return self._users.get(iid.removeprefix("user:")), None

    def _show_users(self, users: list[UserInfo]) -> None:
        t = self.users_tree
        sel = t.selection()
        for item in t.get_children():
            t.delete(item)
        self._users = {u.name: u for u in users}
        now = datetime.now(timezone.utc)
        for u in sorted(users, key=lambda x: (not x.managed, x.name.lower())):
            active = sum(k.status == "Active" for k in u.keys)
            notes = []
            if not u.managed:
                notes.append("non gestito dalla utility: solo lettura")
            if u.other_groups:
                notes.append("altri gruppi: " + ", ".join(u.other_groups))
            if u.managed and not u.has_base:
                notes.append(f"manca il gruppo {self.catalog.base.gruppo}")
            if {"account:admin", "account:infrastruttura"} & set(u.roles) and u.console and not u.mfa:
                notes.append("MFA non ancora configurata (in console vedra' solo la configurazione MFA)")
            iid = f"user:{u.name}"
            per_scope = tuple(self.catalog.label_scope(sc, u.roles) for sc in self.catalog.scopes)
            t.insert("", "end", iid=iid, text=u.name, open=True, tags=("" if u.managed else "skip",),
                     values=(*per_scope, "si" if u.console else "no", _date(u.password_last_used),
                             u.mfa or "-", f"{active} attive / {len(u.keys)}", _date(u.created), "; ".join(notes)))
            for k in u.keys:
                age = (now - k.created).days
                knotes = [f"ultimo uso {_date(k.last_used)}" + (f" ({k.last_service})" if k.last_service else "")
                          if k.last_used else "mai usata"]
                if age > OLD_KEY_DAYS:
                    knotes.append(f"creata {age} giorni fa: da ruotare")
                t.insert(iid, "end", iid=f"key:{u.name}:{k.key_id}", text=f"  {k.key_id}",
                         tags=("warn" if k.status != "Active" or age > OLD_KEY_DAYS else "",),
                         values=(*("",) * len(self.catalog.scopes), "", "", "",
                                 "attiva" if k.status == "Active" else "disattivata",
                                 _date(k.created), "; ".join(knotes)))
        keep = [i for i in sel if t.exists(i)]
        if keep:
            t.selection_set(keep)
        self._refresh_buttons()

    def _job_then_list(self, action):
        """Esegue l'azione e rilegge gli utenti; il risultato dell'azione passa al callback."""
        def job(aws: Aws):
            result = action(aws)
            users = list_users(aws, self.catalog, self._cancel, self._progress)
            return result, users
        return job

    def do_list(self) -> None:
        def job(aws):
            users = list_users(aws, self.catalog, self._cancel, self._progress)
            return None, lambda: self._show_users(users)
        self._run("Elenco utenti", job)

    # ------------------------------------------------------------------ ruoli
    def do_roles(self, apply: bool) -> None:
        if apply and not messagebox.askyesno(
                "Prepara ruoli",
                "Crea o aggiorna nell'account le policy sb-ruolo-* / sb-dev-ruolo-* / sb-dev-confine e i "
                "gruppi dei ruoli, come descritti in app/ruoli/.\n\nAi gruppi dei ruoli vengono staccate le "
                "policy non previste. Utenti, policy e gruppi creati a mano non si toccano.\n\nProcedere?",
                parent=self):
            return
        self.tabs.select(1)
        self._fill_roles()

        def job(aws):
            counts = sync_roles(aws, self.settings, self.catalog, apply, self._cancel, self._progress, self._step)
            text = ", ".join(f"{k}: {v}" for k, v in sorted(counts.items()))
            bad = counts.get("KO", 0) + counts.get("saltato", 0)
            title = "Ruoli preparati" if apply else "Verifica ruoli"
            return (title, text, "warning" if bad else "info"), None
        self._run("Prepara ruoli" if apply else "Verifica ruoli", job)

    # ------------------------------------------------------------------ nuovo utente
    def _gen_password(self) -> None:
        self.pwd_var.set(generate_password(self.settings.lunghezza_password))
        self.pwd_show.set(True)
        self.pwd_entry.configure(show="")

    def do_create(self) -> None:
        name, password, roles = self.name_var.get().strip(), self.pwd_var.get(), self.new_roles.get()
        if not name or not password:
            messagebox.showwarning("Nuovo utente", "Nome e password sono obbligatori.", parent=self)
            return
        if not roles and not messagebox.askyesno(
                "Nuovo utente", "Nessun ruolo scelto: l'utente potra' solo gestire password e chiavi proprie. "
                                "Continuare?", parent=self):
            return
        if not messagebox.askyesno(
                "Nuovo utente",
                f"Account: {self.settings.account_atteso}\nUtente: {name}\nRuoli: {self.catalog.label(roles)}\n\n"
                "Verranno creati l'utente, l'accesso alla console con la password indicata (nessun cambio "
                "obbligatorio al primo accesso) e una chiave d'accesso.\n\nProcedere?", parent=self):
            return

        def done(result):
            key, users = result
            self._show_users(users)
            self.name_var.set("")
            self.pwd_var.set("")
            self.new_roles.clear()
            KeyDialog(self, key)

        job = self._job_then_list(lambda aws: create_user(aws, self.catalog, name, password, roles))

        def run(aws):
            result = job(aws)
            return None, lambda: done(result)
        self._run("Nuovo utente", run)

    # ------------------------------------------------------------------ utente selezionato
    def _after_action(self, title: str, action, show_key: bool = False) -> None:
        job = self._job_then_list(action)

        def run(aws):
            result, users = job(aws)

            def after():
                self._show_users(users)
                if show_key:
                    KeyDialog(self, result)
            return (None if show_key else (title, "Fatto.")), after
        self._run(title, run)

    def do_set_roles(self) -> None:
        user, _ = self._selection()
        if not user:
            return
        dlg = RolesDialog(self, self.catalog, user)
        if dlg.result is None:
            return
        roles = dlg.result
        if not messagebox.askyesno("Ruoli", f"{user.name}\n\nPrima: {self.catalog.label(user.roles)}\n"
                                            f"Dopo:  {self.catalog.label(roles)}\n\nProcedere?", parent=self):
            return
        self._after_action("Ruoli", lambda aws: set_roles(aws, self.catalog, user.name, roles))

    def do_set_password(self) -> None:
        user, _ = self._selection()
        if not user:
            return
        dlg = PasswordDialog(self, user.name, self.settings.lunghezza_password)
        if not dlg.result:
            return
        self._after_action("Password", lambda aws: set_password(aws, self.catalog, user.name, dlg.result))

    def do_new_key(self) -> None:
        user, _ = self._selection()
        if not user:
            return
        if not messagebox.askyesno("Nuova chiave", f"Creare una nuova chiave d'accesso per {user.name}?\n\n"
                                                   f"Chiavi attuali: {len(user.keys)} (massimo 2).", parent=self):
            return
        self._after_action("Nuova chiave", lambda aws: new_key(aws, self.catalog, user.name), show_key=True)

    def do_toggle_key(self) -> None:
        user, key_id = self._selection()
        if not user or not key_id:
            return
        key = next(k for k in user.keys if k.key_id == key_id)
        activate = key.status != "Active"
        verb = "Riattivare" if activate else "Disattivare"
        if not messagebox.askyesno("Chiave", f"{verb} la chiave {key_id} di {user.name}?", parent=self):
            return
        self._after_action("Chiave", lambda aws: set_key_status(aws, self.catalog, user.name, key_id, activate))

    def do_delete_key(self) -> None:
        user, key_id = self._selection()
        if not user or not key_id:
            return
        if not messagebox.askyesno("Cancella chiave", f"Cancellare definitivamente la chiave {key_id} di "
                                                      f"{user.name}?\nChi la usa smette subito di funzionare.",
                                   parent=self, icon="warning"):
            return
        self._after_action("Cancella chiave", lambda aws: delete_key(aws, self.catalog, user.name, key_id))

    def do_delete_user(self) -> None:
        user, _ = self._selection()
        if not user:
            return
        typed = simpledialog.askstring(
            "Cancella utente",
            f"Si cancellano l'utente {user.name}, la sua password, le {len(user.keys)} chiavi, l'MFA e "
            f"l'appartenenza ai gruppi. Non si puo' annullare.\n\nScrivere il nome dell'utente per confermare:",
            parent=self)
        if typed is None:
            return
        if typed.strip() != user.name:
            messagebox.showwarning("Cancella utente", "Il nome non coincide: nessuna modifica.", parent=self)
            return
        self._after_action("Cancella utente",
                           lambda aws: delete_user(aws, self.catalog, user.name, self._cancel, self._progress))

    # ------------------------------------------------------------------ chiusura
    def _on_close(self) -> None:
        if self._busy and not messagebox.askyesno(
                "Operazione in corso", "C'e' un'operazione in corso. Chiudere comunque?", parent=self):
            return
        self._cancel.set()
        for h in log.handlers:
            h.flush()
        self.destroy()


# ---------------------------------------------------------------------- finestre di dialogo

class _Modal(tk.Toplevel):
    def __init__(self, master, title: str):
        super().__init__(master)
        self.title(title)
        self.transient(master)
        self.resizable(False, False)
        self.result = None
        self.body = ttk.Frame(self, padding=12)
        self.body.pack(fill="both", expand=True)

    def run(self) -> None:
        self.grab_set()
        self.update_idletasks()
        m = self.master
        self.geometry(f"+{m.winfo_rootx() + (m.winfo_width() - self.winfo_width()) // 2}"
                      f"+{m.winfo_rooty() + 120}")
        self.wait_window(self)


class RolesDialog(_Modal):
    def __init__(self, master, cat: Catalog, user: UserInfo):
        super().__init__(master, f"Ruoli di {user.name}")
        ttk.Label(self.body, text="I ruoli si sommano. Il gruppo base resta sempre.").pack(anchor="w")
        self.checks = RoleChecks(self.body, cat, user.roles)
        self.checks.pack(anchor="w", pady=8)
        b = ttk.Frame(self.body)
        b.pack(anchor="e")
        ttk.Button(b, text="Annulla", command=self.destroy).pack(side="right")
        ttk.Button(b, text="OK", command=self._ok).pack(side="right", padx=6)
        self.run()

    def _ok(self) -> None:
        self.result = self.checks.get()
        self.destroy()


class PasswordDialog(_Modal):
    def __init__(self, master, user: str, length: int):
        super().__init__(master, f"Password di {user}")
        self.length = length
        ttk.Label(self.body, text="Nuova password della console (nessun cambio obbligatorio al primo accesso):"
                  ).pack(anchor="w")
        r = ttk.Frame(self.body)
        r.pack(anchor="w", pady=8)
        self.var = tk.StringVar()
        ttk.Entry(r, textvariable=self.var, width=36).pack(side="left")
        ttk.Button(r, text="Genera", command=lambda: self.var.set(generate_password(self.length))
                   ).pack(side="left", padx=4)
        b = ttk.Frame(self.body)
        b.pack(anchor="e")
        ttk.Button(b, text="Annulla", command=self.destroy).pack(side="right")
        ttk.Button(b, text="OK", command=self._ok).pack(side="right", padx=6)
        self.run()

    def _ok(self) -> None:
        self.result = self.var.get()
        self.destroy()


class KeyDialog(_Modal):
    """Mostra chiave e secret una volta sola, con copia e salvataggio della scheda."""

    def __init__(self, master: App, key: NewKey):
        super().__init__(master, f"Chiave d'accesso di {key.user}")
        self.app, self.key = master, key
        account = master._aws.account if master._aws else master.settings.account_atteso
        self.text = credentials_text(account, master.settings.aws.region, key, master.catalog)
        self.kept = False
        tk.Label(self.body, text="Il secret non si potra' piu' rivedere: salvarlo o copiarlo adesso.",
                 fg="#b00020", font=("Segoe UI", 10, "bold")).grid(row=0, column=0, columnspan=3, sticky="w")
        rows = [("Utente", key.user), ("Access key ID", key.key_id), ("Secret access key", key.secret)]
        if key.password:
            rows.append(("Password console", key.password))
        rows.append(("Console", f"https://{account}.signin.aws.amazon.com/console"))
        for i, (label, value) in enumerate(rows, start=1):
            ttk.Label(self.body, text=label + ":").grid(row=i, column=0, sticky="w", pady=2)
            var = tk.StringVar(value=value)
            ttk.Entry(self.body, textvariable=var, width=58, state="readonly", font=("Consolas", 10)
                      ).grid(row=i, column=1, sticky="w", padx=6)
            ttk.Button(self.body, text="Copia", command=lambda v=value: self._copy(v, False)
                       ).grid(row=i, column=2, sticky="w")
        b = ttk.Frame(self.body)
        b.grid(row=len(rows) + 1, column=0, columnspan=3, sticky="e", pady=(10, 0))
        ttk.Button(b, text="Chiudi", command=self._close).pack(side="right")
        ttk.Button(b, text="Copia scheda", command=lambda: self._copy(self.text, True)).pack(side="right", padx=6)
        ttk.Button(b, text="Salva in file…", command=self._save).pack(side="right")
        self.protocol("WM_DELETE_WINDOW", self._close)
        self.run()

    def _copy(self, value: str, whole: bool) -> None:
        self.clipboard_clear()
        self.clipboard_append(value)
        if whole or value == self.key.secret:
            self.kept = True

    def _save(self) -> None:
        folder = self.app.settings.schede_path
        folder.mkdir(parents=True, exist_ok=True)
        path = filedialog.asksaveasfilename(parent=self, title="Salva la scheda delle credenziali",
                                            initialdir=str(folder), initialfile=default_file_name(self.key),
                                            defaultextension=".txt", filetypes=[("Testo", "*.txt")])
        if not path:
            return
        save_credentials(self.text, path)
        self.kept = True
        if messagebox.askyesno("Scheda salvata", f"{path}\n\nTesto in chiaro: consegnarlo per un canale sicuro "
                                                 "e cancellarlo dopo l'uso.\n\nAprire la cartella?", parent=self):
            os.startfile(os.path.dirname(path))  # noqa: S606 - Windows: apre Esplora risorse

    def _close(self) -> None:
        if not self.kept and not messagebox.askyesno(
                "Chiudere?", "Il secret non e' stato ne' salvato ne' copiato e non si potra' piu' rivedere "
                             "(servira' una nuova chiave). Chiudere comunque?", parent=self, icon="warning"):
            return
        self.destroy()


def main() -> None:
    App().mainloop()
