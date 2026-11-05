"""Gestione degli utenti IAM senza GUI.

Esempi (da questa cartella):
  .venv\\Scripts\\python.exe run_headless.py --elenco
  .venv\\Scripts\\python.exe run_headless.py --verifica-ruoli
  .venv\\Scripts\\python.exe run_headless.py --prepara-ruoli --si
  .venv\\Scripts\\python.exe run_headless.py --crea mario.rossi --ruoli dev:fullstack,dev:log --scheda data\\mario.txt --si
  .venv\\Scripts\\python.exe run_headless.py --ruoli-utente mario.rossi --ruoli dev:frontend,account:costi --si
  .venv\\Scripts\\python.exe run_headless.py --password mario.rossi --password-env NUOVA_PWD --si
  .venv\\Scripts\\python.exe run_headless.py --nuova-chiave mario.rossi --scheda data\\mario-2.txt --si
  .venv\\Scripts\\python.exe run_headless.py --disattiva-chiave mario.rossi AKIA... --si
  .venv\\Scripts\\python.exe run_headless.py --cancella mario.rossi --si

Ruoli come ambito:nome. dev: frontend, backend, database, orario, log, fullstack (= frontend + backend);
prod: frontend, backend, database, log, fullstack (ancora da definire: non assegnabili); account: lettura,
costi, infrastruttura, admin. Senza prefisso un nome vale per dev o account se non e' ambiguo
(es. 'admin', 'fullstack'). Il gruppo base si assegna sempre. La password di --crea si legge dalla variabile indicata con --password-env, altrimenti
si genera; in entrambi i casi finisce nella scheda insieme alla chiave. Il secret non si stampa mai:
--crea e --nuova-chiave richiedono --scheda.
Le scritture senza --si escono con 1 e stampano cosa avrebbero fatto. Uscita 0 = fatto, 1 = errore,
rifiuto o interruzione.
"""
from __future__ import annotations

import argparse
import os
import sys
import threading
from pathlib import Path

from app.awsutil import Aws, Cancelled
from app.logging_setup import setup_logging
from app.roles import load_catalog, sync_roles
from app.users import (create_user, credentials_text, delete_key, delete_user, generate_password, list_users,
                       new_key, save_credentials, set_key_status, set_password, set_roles)
from app.settings import load_settings


def _list(text: str | None) -> list[str]:
    return [x.strip() for x in (text or "").split(",") if x.strip()]


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--settings", type=Path)
    ap.add_argument("--profile", help="profilo della AWS CLI (sovrascrive aws.profile)")
    op = ap.add_mutually_exclusive_group(required=True)
    op.add_argument("--elenco", action="store_true", help="utenti, ruoli e chiavi")
    op.add_argument("--verifica-ruoli", action="store_true", help="confronta policy e gruppi con app/ruoli/")
    op.add_argument("--prepara-ruoli", action="store_true", help="crea/aggiorna policy e gruppi dei ruoli")
    op.add_argument("--crea", metavar="NOME", help="nuovo utente con password, ruoli e una chiave")
    op.add_argument("--ruoli-utente", metavar="NOME", help="sostituisce i ruoli dell'utente con --ruoli")
    op.add_argument("--password", metavar="NOME", help="nuova password della console (--password-env)")
    op.add_argument("--nuova-chiave", metavar="NOME", help="nuova chiave d'accesso (--scheda)")
    op.add_argument("--disattiva-chiave", nargs=2, metavar=("NOME", "CHIAVE"))
    op.add_argument("--riattiva-chiave", nargs=2, metavar=("NOME", "CHIAVE"))
    op.add_argument("--cancella-chiave", nargs=2, metavar=("NOME", "CHIAVE"))
    op.add_argument("--cancella", metavar="NOME", help="cancella l'utente e tutto quello che ha")
    ap.add_argument("--ruoli", help="ruoli separati da virgola")
    ap.add_argument("--password-env", metavar="VARIABILE", help="variabile d'ambiente con la password")
    ap.add_argument("--scheda", type=Path, help="file in cui salvare la scheda con chiave e secret")
    ap.add_argument("--si", action="store_true", help="conferma le scritture")
    a = ap.parse_args()

    s = load_settings(a.settings)
    if a.profile:
        s.aws.profile = a.profile
    cat = load_catalog()
    log_file = setup_logging(s.log_path)
    print(f"Log: {log_file}")
    last = {"key": None}

    def progress(phase, done, total, text):
        pct = int(100 * done / total) if total else 0
        k = (phase, pct // 10)
        if k != last["key"]:
            last["key"] = k
            print(f"  {phase}: {pct}% - {text}", flush=True)

    def step(key, scope, item, status, notes):
        if status != "in corso":
            print(f"[{status:>13}] {scope:<24} {item}: {notes}", flush=True)

    def password_from_env() -> str | None:
        if not a.password_env:
            return None
        value = os.environ.get(a.password_env)
        if not value:
            ap.error(f"variabile d'ambiente {a.password_env} vuota o assente")
        return value

    def refuse(what: str) -> int:
        print(f"Senza --si non si scrive. Si farebbe: {what}")
        print("ESITO: rifiutato (manca --si)")
        return 1

    def write_card(key) -> None:
        save_credentials(credentials_text(aws.account, s.aws.region, key, cat), a.scheda)
        print(f"Chiave {key.key_id} creata. Scheda (secret in chiaro): {a.scheda}")

    aws = Aws(s)
    cancel = threading.Event()
    try:
        ident = aws.check_account()
        print(f"Account {ident['Account']} - {ident['Arn']}")

        if a.elenco:
            for u in list_users(aws, cat, cancel, progress):
                flag = "" if u.managed else "  [non gestito]"
                keys = ", ".join(f"{k.key_id} {k.status}" for k in u.keys) or "nessuna"
                print(f"{u.name:<28} ruoli: {cat.label(u.roles):<40} console: {'si' if u.console else 'no':<3} "
                      f"MFA: {u.mfa}  chiavi: {keys}{flag}")
            print("ESITO: completato")
            return 0

        if a.verifica_ruoli or a.prepara_ruoli:
            apply = bool(a.prepara_ruoli)
            if apply and not a.si:
                return refuse("crea/aggiorna le policy e i gruppi dei ruoli (vedere --verifica-ruoli)")
            counts = sync_roles(aws, s, cat, apply, cancel, progress, step)
            bad = counts.get("KO", 0) + counts.get("saltato", 0)
            print("ESITO: " + ("con errori" if bad else "completato") + " - " +
                  ", ".join(f"{k}: {v}" for k, v in sorted(counts.items())))
            return 1 if bad else 0

        if a.crea:
            if not a.scheda:
                ap.error("--crea richiede --scheda")
            roles = cat.expand(_list(a.ruoli))
            if not a.si:
                return refuse(f"crea {a.crea} con ruoli {cat.label(roles)}, password della console e una chiave")
            pwd = password_from_env() or generate_password(s.lunghezza_password)
            write_card(create_user(aws, cat, a.crea, pwd, roles))
        elif a.ruoli_utente:
            roles = cat.expand(_list(a.ruoli))
            if not a.si:
                return refuse(f"ruoli di {a.ruoli_utente} = {cat.label(roles)}")
            add, drop = set_roles(aws, cat, a.ruoli_utente, roles)
            print(f"Gruppi aggiunti: {', '.join(add) or '-'}; tolti: {', '.join(drop) or '-'}")
        elif a.password:
            pwd = password_from_env()
            if not pwd:
                ap.error("--password richiede --password-env")
            if not a.si:
                return refuse(f"nuova password della console per {a.password}")
            set_password(aws, cat, a.password, pwd)
        elif a.nuova_chiave:
            if not a.scheda:
                ap.error("--nuova-chiave richiede --scheda")
            if not a.si:
                return refuse(f"nuova chiave per {a.nuova_chiave}")
            write_card(new_key(aws, cat, a.nuova_chiave))
        elif a.disattiva_chiave or a.riattiva_chiave:
            name, key_id = a.disattiva_chiave or a.riattiva_chiave
            active = bool(a.riattiva_chiave)
            if not a.si:
                return refuse(f"{'riattiva' if active else 'disattiva'} la chiave {key_id} di {name}")
            set_key_status(aws, cat, name, key_id, active)
        elif a.cancella_chiave:
            name, key_id = a.cancella_chiave
            if not a.si:
                return refuse(f"cancella la chiave {key_id} di {name}")
            delete_key(aws, cat, name, key_id)
        elif a.cancella:
            if not a.si:
                return refuse(f"cancella l'utente {a.cancella} con password, chiavi, MFA e gruppi")
            delete_user(aws, cat, a.cancella, cancel, progress)
        print("ESITO: completato")
        return 0
    except Cancelled:
        print("ESITO: interrotto", file=sys.stderr)
        return 1
    except Exception as exc:  # noqa: BLE001
        print(f"ESITO: errore - {type(exc).__name__}: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
