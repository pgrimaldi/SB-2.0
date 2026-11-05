"""Finestre di dialogo modali delle linguette."""
from __future__ import annotations

import tkinter as tk
from tkinter import ttk


class Dialog(tk.Toplevel):
    def __init__(self, parent, title: str):
        super().__init__(parent)
        self.title(title)
        self.transient(parent)
        self.resizable(False, False)
        self.result = None
        self.body = ttk.Frame(self, padding=12)
        self.body.pack(fill="both", expand=True)

    def _buttons(self) -> None:
        b = ttk.Frame(self.body)
        # Tk non ammette pack e grid nello stesso contenitore: si segue quello gia' usato dal corpo.
        if self.body.grid_slaves():
            b.grid(row=99, column=0, columnspan=4, sticky="e", pady=(12, 0))
        else:
            b.pack(anchor="e", pady=(12, 0))
        ttk.Button(b, text="Annulla", command=self.destroy).pack(side="right")
        ttk.Button(b, text="OK", command=self._ok).pack(side="right", padx=6)
        self.grab_set()
        self.wait_window()

    def _ok(self) -> None:
        raise NotImplementedError


class PowerOffDialog(Dialog):
    def __init__(self, parent, interval: dict, has_db: bool):
        super().__init__(parent, "Spegni")
        ttk.Label(self.body, text=f"Intervallo spento {interval['dal'].replace('T', ' ')} -> "
                                  f"{interval['al'].replace('T', ' ')}").pack(anchor="w")
        self.delete = tk.BooleanVar(value=False)
        self.data_only = tk.BooleanVar(value=False)
        cb = ttk.Checkbutton(self.body, text="Cancella DB", variable=self.delete, command=self._toggle)
        cb.pack(anchor="w", pady=(10, 0))
        self.sub = ttk.Checkbutton(self.body, text="Cancella solo dati (tiene schema e seed)", variable=self.data_only)
        self.sub.pack(anchor="w", padx=(22, 0))
        if not has_db:
            cb.state(["disabled"])
        ttk.Label(self.body, text="Senza caselle: spegnimento normale. «Cancella DB» elimina l'istanza senza snapshot;\n"
                                  "con «Cancella solo dati» svuota le tabelle e tiene l'istanza.",
                  foreground="#666").pack(anchor="w", pady=(8, 0))
        self._toggle()
        self._buttons()

    def _toggle(self) -> None:
        if self.delete.get():
            self.sub.state(["!disabled"])
        else:
            self.data_only.set(False)
            self.sub.state(["disabled"])

    def _ok(self) -> None:
        self.result = (self.delete.get(), self.data_only.get())
        self.destroy()


class CreateDbDialog(Dialog):
    NONE = "(nessuno: solo schema e seed)"

    def __init__(self, parent, names: list[str], selected: str | None):
        super().__init__(parent, "Crea DB")
        ttk.Label(self.body, text="Schema da:").grid(row=0, column=0, sticky="w")
        self.schema = tk.StringVar(value=selected or (names[0] if names else ""))
        ttk.Combobox(self.body, textvariable=self.schema, values=names, state="readonly", width=34).grid(
            row=0, column=1, pady=2)
        ttk.Label(self.body, text="Dati da:").grid(row=1, column=0, sticky="w")
        self.data = tk.StringVar(value=selected or self.NONE)
        ttk.Combobox(self.body, textvariable=self.data, values=[self.NONE, *names], state="readonly",
                     width=34).grid(row=1, column=1, pady=2)
        self._buttons()

    def _ok(self) -> None:
        data = self.data.get()
        self.result = (self.schema.get(), None if data == self.NONE else data)
        self.destroy()


class UpdateDbDialog(Dialog):
    def __init__(self, parent, names: list[str], selected: str | None):
        super().__init__(parent, "Aggiorna DB")
        ttk.Label(self.body, text="DB locale:").grid(row=0, column=0, sticky="w")
        self.src = tk.StringVar(value=selected or (names[0] if names else ""))
        ttk.Combobox(self.body, textvariable=self.src, values=names, state="readonly", width=34).grid(
            row=0, column=1, pady=2)
        self.mode = tk.StringVar(value="schema-dati")
        ttk.Radiobutton(self.body, text="Schema + dati (sostituisce tutto)", value="schema-dati",
                        variable=self.mode).grid(row=1, column=1, sticky="w")
        ttk.Radiobutton(self.body, text="Solo dati (stesse migration su AWS e in locale)", value="solo-dati",
                        variable=self.mode).grid(row=2, column=1, sticky="w")
        self._buttons()

    def _ok(self) -> None:
        self.result = (self.src.get(), self.mode.get())
        self.destroy()


class OpenCidrDialog(Dialog):
    def __init__(self, parent):
        super().__init__(parent, "Apri un IP al DB")
        ttk.Label(self.body, text="IP o CIDR (es. 93.40.1.2 oppure 93.40.1.0/24):").grid(row=0, column=0, sticky="w")
        self.cidr = tk.StringVar()
        ttk.Entry(self.body, textvariable=self.cidr, width=30).grid(row=0, column=1, pady=2, sticky="w")
        ttk.Label(self.body, text="Nota (chi o cosa):").grid(row=1, column=0, sticky="w")
        self.note = tk.StringVar()
        ttk.Entry(self.body, textvariable=self.note, width=30).grid(row=1, column=1, pady=2, sticky="w")
        self.permanent = tk.BooleanVar(value=False)
        ttk.Checkbutton(self.body, text="Permanente (non si chiude da solo)", variable=self.permanent).grid(
            row=2, column=1, sticky="w")
        self._buttons()

    def _ok(self) -> None:
        self.result = (self.cidr.get().strip(), self.permanent.get(), self.note.get().strip())
        self.destroy()


class TableWindow(tk.Toplevel):
    """Finestra non modale con una tabella di righe (anteprima di un archivio, risultati di ricerca)."""

    def __init__(self, parent, title: str, columns: list[str], rows: list[dict], note: str = "",
                 on_export=None):
        super().__init__(parent)
        self.title(title)
        self.geometry("1200x600")
        if note:
            ttk.Label(self, text=note, padding=(8, 6)).pack(anchor="w")
        frame = ttk.Frame(self)
        frame.pack(fill="both", expand=True, padx=8, pady=4)
        tree = ttk.Treeview(frame, columns=columns, show="headings")
        for c in columns:
            tree.heading(c, text=c)
            tree.column(c, width=max(80, min(260, 9 * len(c) + 30)), stretch=False)
        ys = ttk.Scrollbar(frame, orient="vertical", command=tree.yview)
        xs = ttk.Scrollbar(frame, orient="horizontal", command=tree.xview)
        tree.configure(yscrollcommand=ys.set, xscrollcommand=xs.set)
        tree.grid(row=0, column=0, sticky="nsew")
        ys.grid(row=0, column=1, sticky="ns")
        xs.grid(row=1, column=0, sticky="ew")
        frame.rowconfigure(0, weight=1)
        frame.columnconfigure(0, weight=1)
        for r in rows:
            tree.insert("", "end", values=[_cell(r.get(c)) for c in columns])
        if on_export:
            ttk.Button(self, text="Esporta in CSV…", command=on_export).pack(anchor="e", padx=8, pady=6)


class TextWindow(tk.Toplevel):
    """Finestra non modale con un testo a larghezza fissa (resoconti)."""

    def __init__(self, parent, title: str, text: str):
        super().__init__(parent)
        self.title(title)
        self.geometry("1150x650")
        from tkinter.scrolledtext import ScrolledText  # noqa: PLC0415

        box = ScrolledText(self, font=("Consolas", 9), wrap="none")
        box.pack(fill="both", expand=True, padx=8, pady=8)
        box.insert("1.0", text)
        box.configure(state="disabled")


def _cell(v) -> str:
    if v is None:
        return ""
    text = str(v)
    return text if len(text) <= 300 else text[:300] + "…"
