"""Linguetta Infrastruttura: change set dello stack dell'ambiente dal suo template."""
from __future__ import annotations

import tkinter as tk
from tkinter import messagebox, ttk

from app.env import infra as I
from app.gui.shell import Tab


class InfraTab(Tab):
    title = "Infrastruttura"
    features = ("infra",)

    def build(self, tab: ttk.Frame) -> None:
        self._sets: dict[str, I.ChangeSet] = {}
        self.head_var = tk.StringVar()
        ttk.Label(tab, textvariable=self.head_var, foreground="#666", wraplength=1350, justify="left").pack(anchor="w")
        top = ttk.Frame(tab)
        top.pack(fill="x", pady=(4, 0))
        chosen = lambda: bool(self.sets.selection())  # noqa: E731
        self.gate(ttk.Button(top, text="Aggiorna stato", command=self.do_refresh), "infra").pack(side="left")
        self.gate(ttk.Button(top, text="Anteprima modifiche", command=self.do_preview), "infra").pack(side="left", padx=6)
        self.gate(ttk.Button(top, text="Applica…", command=self.do_apply), "infra", chosen).pack(side="left")
        self.gate(ttk.Button(top, text="Scarta", command=self.do_discard), "infra", chosen).pack(side="left", padx=6)
        self.stack_var = tk.StringVar(value="Stack: -")
        ttk.Label(top, textvariable=self.stack_var).pack(side="left", padx=12)

        body = ttk.Frame(tab)
        body.pack(fill="both", expand=True, pady=(6, 0))
        fl = ttk.LabelFrame(body, text=" Change set in attesa ", padding=4)
        fl.pack(side="left", fill="y")
        self.sets = ttk.Treeview(fl, columns=("nome", "tipo", "stato", "modifiche"), show="headings", height=8)
        for c, w in (("nome", 170), ("tipo", 70), ("stato", 120), ("modifiche", 80)):
            self.sets.heading(c, text=c)
            self.sets.column(c, width=w)
        self.sets.pack(fill="both", expand=True)
        self.sets.bind("<<TreeviewSelect>>", lambda _e: (self._show_changes(), self.app._refresh_buttons()))
        fc = ttk.LabelFrame(body, text=" Modifiche (in rosso sostituzioni e rimozioni) ", padding=4)
        fc.pack(side="left", fill="both", expand=True, padx=(8, 0))
        cols = (("azione", 70), ("risorsa", 230), ("tipo", 230), ("sostituzione", 90), ("proprieta", 380))
        self.changes = ttk.Treeview(fc, columns=[c for c, _ in cols], show="headings", height=8)
        for c, w in cols:
            self.changes.heading(c, text=c)
            self.changes.column(c, width=w, stretch=c == "proprieta")
        self.changes.tag_configure("ko", background="#fde2e2")
        self.changes.pack(fill="both", expand=True)
        fo = ttk.LabelFrame(tab, text=" Parametri e output dello stack ", padding=4)
        fo.pack(fill="both", expand=True, pady=(6, 0))
        self.outputs = ttk.Treeview(fo, columns=("tipo", "nome", "valore"), show="headings", height=6)
        for c, w in (("tipo", 80), ("nome", 240), ("valore", 700)):
            self.outputs.heading(c, text=c)
            self.outputs.column(c, width=w, stretch=c == "valore")
        self.outputs.pack(fill="both", expand=True)
        self.on_env_changed()

    def on_env_changed(self) -> None:
        e = self.env
        self.head_var.set(f"Stack {e.stack} dal template {e.template_path}. Parametri imposti: "
                          f"{e.infra.parametri or 'nessuno'} (gli altri restano quelli in esercizio). Mai un "
                          f"aggiornamento diretto: anteprima con change set, poi Applica o Scarta."
                          + (" Alla creazione si attiva la protezione dalla cancellazione."
                             if e.infra.protezione_cancellazione else ""))
        self._sets = {}
        self.stack_var.set("Stack: -")
        for tree in (self.sets, self.changes, self.outputs):
            tree.delete(*tree.get_children())

    def _render(self, st, sets: list[I.ChangeSet], select: str | None = None) -> None:
        self.stack_var.set(f"Stack: {st.status if st else 'NON ESISTE (l anteprima sara una creazione)'}")
        self.outputs.delete(*self.outputs.get_children())
        if st:
            for k, v in sorted(st.parameters.items()):
                self.outputs.insert("", "end", values=("parametro", k, v))
            for k, v in sorted(st.outputs.items()):
                self.outputs.insert("", "end", values=("output", k, v))
        self._sets = {c.name: c for c in sets}
        self.sets.delete(*self.sets.get_children())
        for c in sets:
            self.sets.insert("", "end", iid=c.name, values=(c.name, c.kind, c.status, len(c.changes)))
        if select and select in self._sets:
            self.sets.selection_set(select)
        self._show_changes()
        self.app._refresh_buttons()

    def _show_changes(self) -> None:
        self.changes.delete(*self.changes.get_children())
        sel = self.sets.selection()
        cs = self._sets.get(sel[0]) if sel else None
        for c in cs.changes if cs else []:
            self.changes.insert("", "end", tags=("ko",) if c.dangerous else (),
                                values=(c.action, c.logical_id, c.resource_type, c.replacement, c.details))

    def do_refresh(self, select: str | None = None) -> None:
        def job(ctx):
            st = I.current(ctx.aws)
            sets = I.list_change_sets(ctx.aws)
            self.app.render(lambda: self._render(st, sets, select))

        self.app.run("Stato dello stack", job, quiet=True, clear_steps=False)

    def do_preview(self) -> None:
        def job(ctx):
            cs = I.preview(ctx.aws, ctx.cancel, ctx.progress)
            st = I.current(ctx.aws)
            sets = I.list_change_sets(ctx.aws)
            self.app.render(lambda: self._render(st, sets, cs.name or None))
            if not cs.name:
                return "Nessuna modifica fra il template e lo stack in esercizio."
            danger = cs.dangerous
            return (f"Change set {cs.name} ({cs.kind}): {len(cs.changes)} modifiche"
                    + (f", ATTENZIONE {len(danger)} sostituzioni o rimozioni:\n"
                       + "\n".join(f"• {c.action} {c.logical_id} ({c.resource_type})" for c in danger[:15])
                       if danger else ", nessuna sostituzione o rimozione.")
                    + "\n\nNiente e' stato ancora toccato: leggere le modifiche e poi Applica o Scarta.")

        self.app.run("Anteprima modifiche", job)

    def do_apply(self) -> None:
        sel = self.sets.selection()
        cs = self._sets.get(sel[0]) if sel else None
        if not cs:
            return
        danger = cs.dangerous
        text = (f"Applicare il change set {cs.name} ({cs.kind}) allo stack {self.env.stack}?\n\n"
                f"{len(cs.changes)} modifiche"
                + (f", di cui {len(danger)} SOSTITUZIONI O RIMOZIONI:\n"
                   + "\n".join(f"• {c.action} {c.logical_id} ({c.resource_type})" for c in danger[:12])
                   if danger else ", nessuna sostituzione o rimozione.")
                + "\n\nUna volta partito, CloudFormation non si ferma con Interrompi.")
        if not self.app.confirm_typed(text):
            return
        name = cs.name

        def job(ctx):
            I.apply(ctx.aws, name, ctx.cancel, ctx.progress)
            return f"Change set {name} applicato."

        self.app.run("Applica change set", job, then=self.do_refresh)

    def do_discard(self) -> None:
        sel = self.sets.selection()
        if not sel or not messagebox.askyesno("Scarta", f"Eliminare il change set {sel[0]}? Lo stack non cambia.",
                                              parent=self.app):
            return
        name = sel[0]
        self.app.run("Scarta change set", lambda ctx: (I.discard(ctx.aws, name), None)[1], then=self.do_refresh)
