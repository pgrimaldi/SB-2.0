"""Esecuzione senza GUI della utility 22 (console del nuovo sistema).

Esempi (da questa cartella; --ambiente vale dev se omesso):
  .venv\\Scripts\\python.exe run_headless.py --ruoli
  .venv\\Scripts\\python.exe run_headless.py --stato --ambiente prod
  .venv\\Scripts\\python.exe run_headless.py --infra-elenco --ambiente prod
  .venv\\Scripts\\python.exe run_headless.py --infra-anteprima --ambiente prod
  .venv\\Scripts\\python.exe run_headless.py --infra-applica u22-20261020-153000 --ambiente prod --si
  .venv\\Scripts\\python.exe run_headless.py --infra-scarta u22-20261020-153000 --ambiente prod
  .venv\\Scripts\\python.exe run_headless.py --commit-elenco --branch main
  .venv\\Scripts\\python.exe run_headless.py --backup --formato custom --compressione 9
  .venv\\Scripts\\python.exe run_headless.py --backup --formato sql --compressione 6 --schemi identity,catalog
  .venv\\Scripts\\python.exe run_headless.py --backup-elenco
  .venv\\Scripts\\python.exe run_headless.py --backup-verifica data\\backup\\dev\\dev_summer_booking_20261020_101500.dump
  .venv\\Scripts\\python.exe run_headless.py --accendi --fino "2026-10-20 22:00"
  .venv\\Scripts\\python.exe run_headless.py --spegni
  .venv\\Scripts\\python.exe run_headless.py --applica
  .venv\\Scripts\\python.exe run_headless.py --aggiorna-backend --si
  .venv\\Scripts\\python.exe run_headless.py --aggiorna-backend --ambiente prod --branch main --commit 1a2b3c4 --si
  .venv\\Scripts\\python.exe run_headless.py --aggiorna-frontend "front office" --configurazione production --si
  .venv\\Scripts\\python.exe run_headless.py --db-crea --schema-da summer_booking_migref --dati-da summer_booking_dev --si
  .venv\\Scripts\\python.exe run_headless.py --db-aggiorna --da summer_booking_dev --modo solo-dati --si
  .venv\\Scripts\\python.exe run_headless.py --db-svuota --si
  .venv\\Scripts\\python.exe run_headless.py --db-elimina --si
  .venv\\Scripts\\python.exe run_headless.py --db-credenziali
  .venv\\Scripts\\python.exe run_headless.py --db-accessi
  .venv\\Scripts\\python.exe run_headless.py --db-apri-ip 93.40.1.2 --permanente --nota "ufficio"
  .venv\\Scripts\\python.exe run_headless.py --db-chiudi-ip 93.40.1.2
  .venv\\Scripts\\python.exe run_headless.py --db-apri-tutti --si
  .venv\\Scripts\\python.exe run_headless.py --db-info
  .venv\\Scripts\\python.exe run_headless.py --db-lettura-crea --si
  .venv\\Scripts\\python.exe run_headless.py --db-panoramica
  .venv\\Scripts\\python.exe run_headless.py --db-sessioni
  .venv\\Scripts\\python.exe run_headless.py --db-query "select count(*) from booking.reservations" --csv data\\q.csv
  .venv\\Scripts\\python.exe run_headless.py --db-query-pronta "Retention: ultime corse"
  .venv\\Scripts\\python.exe run_headless.py --db-snapshot-elenco
  .venv\\Scripts\\python.exe run_headless.py --db-snapshot-crea --si
  .venv\\Scripts\\python.exe run_headless.py --db-log-elenco
  .venv\\Scripts\\python.exe run_headless.py --s3-bucket
  .venv\\Scripts\\python.exe run_headless.py --s3-elenco sb-dev-files-747862473836 --percorso catalog/
  .venv\\Scripts\\python.exe run_headless.py --s3-backup sb-dev-files-747862473836
  .venv\\Scripts\\python.exe run_headless.py --archivi
  .venv\\Scripts\\python.exe run_headless.py --archivi --inventario
  .venv\\Scripts\\python.exe run_headless.py --retention-prova
  .venv\\Scripts\\python.exe run_headless.py --retention-esegui --schema-retention booking --si
  .venv\\Scripts\\python.exe run_headless.py --retention-corse
  .venv\\Scripts\\python.exe run_headless.py --archivi-cerca "Rossi" --tabella booking.reservations --csv data\\r.csv
  .venv\\Scripts\\python.exe run_headless.py --log /summerbooking/dev/api --minuti 120 --testo ERROR
  .venv\\Scripts\\python.exe run_headless.py --scan --regions eu-south-1 --skip ecr_images,lambda_code
  .venv\\Scripts\\python.exe run_headless.py --costi

Ogni comando richiede il ruolo che lo abilita nella GUI (gruppi IAM della utility 14): senza, esce con 1.
Le scritture senza --si escono con 1 e dicono cosa avrebbero fatto. In produzione si scrive comunque
--si: la conferma scritta col nome dello stack e' della sola GUI.
Codice di uscita 0 = fatto (anche con avvisi), 1 = errore, rifiuto o interruzione.
"""
from __future__ import annotations

import argparse
import getpass
import sys
import threading
from pathlib import Path

from app import roles as R
from app.env import archives as A
from app.env import backup as B
from app.env import costs as C
from app.env import dbadmin as M
from app.env import infra as I
from app.env import database as D
from app.env import logs as L
from app.env import operations as O
from app.env import plan as P
from app.env import s3files as S
from app.env.awsutil import Aws, Cancelled, OperationError
from app.env.power import apply_now, env_status, read_plan
from app.env.repo import Repo, commit_from_tag
from app.logging_setup import setup_logging
from app.settings import ENVIRONMENTS, load_settings

_last = {"key": None}


def progress(phase, done, total, text):
    pct = int(100 * done / total) if total else 0
    key = (phase, pct // 10) if total else (phase, text[:40])
    if key != _last["key"]:
        _last["key"] = key
        print(f"  {phase}{f' {pct}%' if total else ''}: {text}", flush=True)


def step(key, label, status, notes):
    if status != O.RUN:
        print(f"[{status}] {label}{f' - {notes}' if notes else ''}", flush=True)


def _list(text: str | None) -> list[str]:
    return [x.strip() for x in (text or "").split(",") if x.strip()]


def _print_status(aws: Aws) -> None:
    st = env_status(aws)
    print(f"Account {st.identity['Account']} - {st.identity['Arn']}")
    print(f"Stack {st.stack.name}: {st.stack.status}  ImageTag={st.stack.image_tag or '-'}  "
          f"DatabaseEnabled={st.stack.parameters.get('DatabaseEnabled', '-')}")
    print(f"Frontend: {st.stack.outputs.get('FrontendUrl')}   API: {st.stack.outputs.get('ApiUrl')}")
    print(f"DB: {st.db_status}")
    for name, svc in st.services.items():
        print(f"Servizio {name}: " + ("assente" if svc is None else f"{svc['running']}/{svc['desired']}"))
    print(f"Voluto adesso: {st.desired[0]} ({st.desired[1]})")
    if st.doc:
        z = P.tz(st.doc.plan)
        m = st.doc.plan.get("manutenzione")
        print(f"Manutenzione: {m['motivo'] + ' fino ' + m['fino'] if m else 'no'}")
        for e in st.events[:8]:
            print(f"  {e['locale'].replace('T', ' ')}  {e['evento']}  ({e['motivo']})")
        if st.last_run:
            print(f"Ultimo giro Lambda: {P.format_local(st.last_run['_quando'], z)} {st.last_run.get('esito')}")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--settings", type=Path, help="file di configurazione alternativo")
    ap.add_argument("--ambiente", choices=ENVIRONMENTS, help="ambiente (default: ambiente_iniziale della configurazione)")
    ap.add_argument("--si", action="store_true", help="conferma le scritture")
    g = ap.add_argument_group("stato e ruoli")
    g.add_argument("--ruoli", action="store_true", help="utente, gruppi e funzioni abilitate")
    g.add_argument("--stato", action="store_true")
    g = ap.add_argument_group("infrastruttura (change set dal template dell'ambiente)")
    g.add_argument("--infra-elenco", action="store_true", help="stato dello stack e change set in attesa")
    g.add_argument("--infra-anteprima", action="store_true", help="crea un change set (nessuna risorsa toccata)")
    g.add_argument("--infra-applica", metavar="CHANGE_SET")
    g.add_argument("--infra-scarta", metavar="CHANGE_SET")
    g = ap.add_argument_group("orario (solo ambienti con orario)")
    g.add_argument("--accendi", action="store_true")
    g.add_argument("--spegni", action="store_true")
    g.add_argument("--fino", help="AAAA-MM-GG HH:MM, ora locale del piano")
    g.add_argument("--applica", action="store_true", help="fa girare subito il riconciliatore")
    g = ap.add_argument_group("deploy")
    g.add_argument("--aggiorna-backend", action="store_true")
    g.add_argument("--aggiorna-frontend", metavar="NOME", help="nome del frontend (frontends[].nome)")
    g.add_argument("--configurazione", help="configurazione Angular del frontend")
    g.add_argument("--branch")
    g.add_argument("--commit", help="commit da pubblicare (default: ultimo del branch)")
    g.add_argument("--commit-elenco", action="store_true", help="ultimi commit del branch e quello in esercizio")
    g = ap.add_argument_group("backup del DB")
    g.add_argument("--backup", action="store_true")
    g.add_argument("--formato", choices=B.FORMATS)
    g.add_argument("--compressione", type=int)
    g.add_argument("--schemi", help="schemi separati da virgola (default: tutto il DB)")
    g.add_argument("--solo-schema", action="store_true")
    g.add_argument("--tieni-su-s3", action="store_true")
    g.add_argument("--backup-elenco", action="store_true")
    g.add_argument("--backup-verifica", type=Path, metavar="FILE")
    g = ap.add_argument_group("database (solo dev)")
    g.add_argument("--db-crea", action="store_true")
    g.add_argument("--schema-da")
    g.add_argument("--dati-da")
    g.add_argument("--db-aggiorna", action="store_true")
    g.add_argument("--da")
    g.add_argument("--modo", choices=("schema-dati", "solo-dati"), default="schema-dati")
    g.add_argument("--db-svuota", action="store_true")
    g.add_argument("--db-elimina", action="store_true")
    g.add_argument("--password-locale", action="store_true", help="chiede e salva la password del PostgreSQL locale")
    g = ap.add_argument_group("accesso al DB")
    g.add_argument("--db-credenziali", action="store_true")
    g.add_argument("--db-accessi", action="store_true")
    g.add_argument("--db-apri-ip", metavar="IP")
    g.add_argument("--db-chiudi-ip", metavar="IP")
    g.add_argument("--db-apri-tutti", action="store_true", help="apre la 5432 a 0.0.0.0/0 (temporaneo)")
    g.add_argument("--permanente", action="store_true")
    g.add_argument("--nota", default="")
    g = ap.add_argument_group("gestione DB")
    g.add_argument("--db-info", action="store_true", help="istanza RDS e formati di connessione (con password)")
    g.add_argument("--db-panoramica", action="store_true")
    g.add_argument("--db-lettura-stato", action="store_true", help="utente PostgreSQL in sola lettura: c'e'?")
    g.add_argument("--db-lettura-crea", action="store_true", help="crea o riallinea l'utente in sola lettura")
    g.add_argument("--db-lettura-rimuovi", action="store_true")
    g.add_argument("--db-sessioni", action="store_true")
    g.add_argument("--db-chiudi-sessione", type=int, metavar="PID")
    g.add_argument("--db-query", metavar="SQL", help="una sola istruzione in sola lettura")
    g.add_argument("--db-query-pronta", metavar="NOME", help="una delle query pronte (nome esatto)")
    g.add_argument("--db-snapshot-elenco", action="store_true")
    g.add_argument("--db-snapshot-crea", action="store_true")
    g.add_argument("--db-snapshot-cancella", metavar="NOME")
    g.add_argument("--db-log-elenco", action="store_true")
    g.add_argument("--db-log-scarica", metavar="FILE")
    g = ap.add_argument_group("file S3")
    g.add_argument("--s3-bucket", action="store_true", help="bucket dell'ambiente")
    g.add_argument("--s3-elenco", metavar="BUCKET")
    g.add_argument("--s3-backup", metavar="BUCKET", help="copia incrementale in data/s3/<ambiente>/<bucket>/")
    g.add_argument("--percorso", default="", help="prefisso dentro il bucket")
    g = ap.add_argument_group("archivi della retention")
    g.add_argument("--archivi", action="store_true", help="archivi per tabella di origine")
    g.add_argument("--inventario", action="store_true", help="con --archivi: confronto con l'inventario nel DB")
    g.add_argument("--archivi-cerca", metavar="TESTO")
    g.add_argument("--tabella", help="schema.tabella di origine per --archivi-cerca")
    g.add_argument("--colonna")
    g.add_argument("--csv", type=Path, help="file CSV dei risultati")
    g = ap.add_argument_group("retention (comando del backend lanciato come task)")
    g.add_argument("--retention-prova", action="store_true", help="valuta le policy, nessuna scrittura")
    g.add_argument("--retention-esegui", action="store_true", help="ESECUZIONE REALE (vuole --si)")
    g.add_argument("--schema-retention", help="solo questo schema")
    g.add_argument("--senza-stagionali", action="store_true")
    g.add_argument("--retention-corse", action="store_true", help="ultime corse dal verbale")
    g = ap.add_argument_group("altro")
    g.add_argument("--log", metavar="GRUPPO")
    g.add_argument("--minuti", type=int, default=60)
    g.add_argument("--testo", default="")
    g.add_argument("--scan", action="store_true", help="scansione dell'account (come la utility 12)")
    g.add_argument("--regions")
    g.add_argument("--only")
    g.add_argument("--skip")
    g.add_argument("--costi", action="store_true")
    a = ap.parse_args()

    try:
        settings = load_settings(a.settings)
        env = settings.env(a.ambiente or settings.ambiente_iniziale)
    except (ValueError, OSError) as exc:
        print(f"Configurazione: {exc}")
        return 1
    log_file = setup_logging(settings.log_path)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, ValueError):
        pass
    print(f"Log: {log_file}\nAmbiente: {env.name} (stack {env.stack})")
    cancel = threading.Event()
    try:
        aws = Aws(env)
        me = R.detect(aws)

        def need(feature: str) -> None:
            """`feature` puo' essere "a|b": basta una delle due."""
            names = feature.split("|")
            if not any(R.allowed(me, env, f) for f in names):
                raise OperationError(R.why_not(me, env, names[0]))

        def confirm(what: str) -> bool:
            if not a.si:
                print(f"Non eseguito senza --si: {what}")
            return a.si

        ctx = O.Ctx(aws, cancel, progress, step, lambda t, x: (print(f"{t}:\n{x}\n(--si: confermato)"), True)[1])

        if a.ruoli:
            print(f"Utente {me.user or me.arn} - {me.label(env.gruppi_prefisso)}" + (f" ({me.note})" if me.note else ""))
            for f in R.FEATURES.values():
                print(f"  [{'x' if R.allowed(me, env, f.key) else ' '}] {f.label}")
        if a.stato:
            need("stato|orario")
            _print_status(aws)

        if a.infra_elenco or a.infra_anteprima or a.infra_applica or a.infra_scarta:
            need("infra")
            if a.infra_anteprima:
                cs = I.preview(aws, cancel, progress)
                print(f"Change set {cs.name or '-'} ({cs.kind}): {cs.status}, {len(cs.changes)} modifiche")
                for c in cs.changes:
                    print(f"  {'!!' if c.dangerous else '  '} {c.action:7} {c.logical_id:32} {c.resource_type:40} "
                          f"sostituzione={c.replacement or '-'} {c.details}")
            if a.infra_applica:
                if not confirm(f"applica il change set {a.infra_applica} allo stack {env.stack}"):
                    return 1
                I.apply(aws, a.infra_applica, cancel, progress)
            if a.infra_scarta:
                I.discard(aws, a.infra_scarta)
            if a.infra_elenco:
                st = I.current(aws)
                print(f"Stack {env.stack}: {st.status if st else 'non esiste'}")
                for c in I.list_change_sets(aws):
                    print(f"  {c.name} {c.kind} {c.status} {len(c.changes)} modifiche ({len(c.dangerous)} da guardare)")

        if a.commit_elenco:
            need("deploy_backend|deploy_frontend")
            branch = a.branch or env.repository.branch
            repo = Repo(env)
            repo.fetch(branch)
            st = I.current(aws)
            deployed = commit_from_tag(st.image_tag) if st else ""
            for c in repo.commits(branch, 20):
                print(f"{'*' if deployed and c.sha.startswith(deployed) else ' '} {c.label()}")
            print("(* = backend in esercizio)" if deployed else "(nessun backend in esercizio)")

        if a.accendi or a.spegni:
            need("orario")
            until = P.parse_local(a.fino, P.tz(read_plan(aws).plan)) if a.fino else None
            O.power_on(ctx, until) if a.accendi else O.power_off(ctx, until)
        if a.applica:
            need("orario")
            print(apply_now(aws))

        if a.aggiorna_backend:
            need("deploy_backend")
            if confirm(f"aggiorna backend {env.name} da {a.branch or env.repository.branch}"):
                print("Rilasciato", O.update_backend(ctx, a.branch or env.repository.branch, a.commit))
            else:
                return 1
        if a.aggiorna_frontend:
            need("deploy_frontend")
            conf = a.configurazione or env.frontend(a.aggiorna_frontend).configurazioni[0]
            if confirm(f"aggiorna frontend {a.aggiorna_frontend} ({conf}) su {env.name}"):
                O.update_frontend(ctx, a.branch or env.repository.branch, a.aggiorna_frontend, conf, a.commit)
            else:
                return 1

        if a.backup:
            need("db_backup")
            b = settings.backup
            opt = B.BackupOptions(a.formato or b.formato, b.compressione if a.compressione is None else a.compressione,
                                  _list(a.schemi) or None, a.solo_schema, a.tieni_su_s3 or b.tieni_su_s3)
            print("Backup:", B.run_backup(ctx, opt))
        if a.backup_elenco:
            for bk in B.list_local(env):
                print(f"{bk.path.name}  {bk.size_mb:.1f} MB  {bk.meta.get('fine', '')}")
        if a.backup_verifica:
            print(B.verify_local(env, a.backup_verifica, cancel))

        def local() -> D.Conn:
            ask = (lambda prompt: getpass.getpass(prompt + ": ")) if a.password_locale else None
            return D.local_conn(env, ask=ask)

        if a.db_crea:
            need("db_gestione")
            if not a.schema_da:
                ap.error("--db-crea vuole --schema-da")
            if not confirm(f"crea DB da {a.schema_da} (dati da {a.dati_da or 'nessuno'})"):
                return 1
            O.create_db(ctx, local(), a.schema_da, a.dati_da)
        if a.db_aggiorna:
            need("db_gestione")
            if not a.da:
                ap.error("--db-aggiorna vuole --da")
            if not confirm(f"aggiorna DB da {a.da} ({a.modo})"):
                return 1
            O.update_db(ctx, local(), a.da, a.modo)
        if a.db_svuota:
            need("db_gestione")
            if not confirm("svuota i dati del DB"):
                return 1
            O.empty_db(ctx)
        if a.db_elimina:
            need("db_gestione")
            if not confirm("elimina l'istanza RDS senza snapshot"):
                return 1
            O.delete_db(ctx)

        if a.db_credenziali:
            need("db_accesso")
            c = D.rds_conn(aws)
            print(f"host={c.host} port={c.port} dbname={c.dbname} user={c.user} password={c.password} sslmode=require")
        if a.db_apri_ip:
            need("db_accesso")
            print("aperto" if D.open_cidr(aws, a.db_apri_ip, a.permanente, a.nota) else "era gia' aperto")
        if a.db_chiudi_ip:
            need("db_accesso")
            D.close_cidr(aws, D.normalize_cidr(a.db_chiudi_ip))
        if a.db_accessi:
            need("db_accesso")
            for r in D.list_access(aws):
                print(f"{r.cidr:20} {r.kind:10} {r.opened_at or '':%Y-%m-%d %H:%M}  {r.description}"
                      if r.opened_at else f"{r.cidr:20} {r.kind:10} {'':16}  {r.description}")

        if a.db_apri_tutti:
            need("db_accesso")
            if not confirm(f"apre la 5432 del DB di {env.name} a 0.0.0.0/0"):
                return 1
            print("aperto" if D.open_cidr(aws, "0.0.0.0/0") else "era gia' aperto")
        if a.db_info:
            need("db_accesso")
            for k, v in M.instance_info(aws):
                print(f"{k:32} {v}")
            for k, v in M.connection_strings(D.rds_conn(aws)):
                print(f"{k}:\n  {v}")
        if a.db_lettura_stato:
            need("db_accesso")
            ro = M.read_only_conn(aws)
            print(f"Utente in sola lettura: {ro.user if ro else 'assente'}")
        if a.db_lettura_crea:
            need("db_amministra")
            if not confirm(f"crea o riallinea l'utente {env.database.utente_lettura} sul DB di {env.name}"):
                return 1
            print(M.setup_read_only(aws))
        if a.db_lettura_rimuovi:
            need("db_amministra")
            if not confirm(f"rimuove l'utente {env.database.utente_lettura} dal DB di {env.name}"):
                return 1
            print(M.remove_read_only(aws))
        if a.db_panoramica:
            need("db_accesso")
            o = M.overview(aws)
            print(f"{o['versione']}\nDB {o['dimensione']}, {o['connessioni']} connessioni")
            for title, key in (("Schemi", "schemi"), ("Migration", "migration"), ("Tabelle piu' grandi", "tabelle")):
                cols, rows = o[key]
                print(f"\n{title}: " + " | ".join(cols))
                for r in rows:
                    print("  " + " | ".join("" if v is None else str(v) for v in r))
        if a.db_sessioni:
            need("db_accesso")
            cols, rows = M.sessions(aws)
            print(" | ".join(cols))
            for r in rows:
                print(" | ".join("" if v is None else str(v) for v in r))
        if a.db_chiudi_sessione:
            need("db_amministra")
            if not confirm(f"chiude la sessione {a.db_chiudi_sessione}"):
                return 1
            print("chiusa" if M.stop_session(aws, a.db_chiudi_sessione, True) else "nessun effetto")
        if a.db_query or a.db_query_pronta:
            need("db_accesso")
            text = a.db_query or M.PRESETS.get(a.db_query_pronta or "")
            if text is None:
                ap.error("query pronta sconosciuta: " + "; ".join(M.PRESETS))
            cols, rows, more = M.run_query(aws, text)
            if a.csv:
                M.export_csv(cols, rows, a.csv)
                print(f"{len(rows)} righe in {a.csv}")
            else:
                print(" | ".join(cols))
                for r in rows:
                    print(" | ".join("" if v is None else str(v) for v in r))
            if more:
                print(f"(troncato a {M.MAX_ROWS} righe)")
        if a.db_snapshot_elenco:
            need("db_accesso|db_amministra")
            for s in M.list_snapshots(aws):
                print(f"{s.ident:50} {s.kind:10} {s.status:12} {s.created}  {s.size_gb} GB{'  (utility)' if s.own else ''}")
        if a.db_snapshot_crea:
            need("db_amministra")
            if not confirm(f"crea uno snapshot manuale del DB di {env.name}"):
                return 1
            print("Snapshot:", M.create_snapshot(aws, cancel, progress))
        if a.db_snapshot_cancella:
            need("db_amministra")
            if not confirm(f"cancella lo snapshot {a.db_snapshot_cancella}"):
                return 1
            M.delete_snapshot(aws, a.db_snapshot_cancella)
        if a.db_log_elenco:
            need("db_amministra")
            for name, size, when in M.list_log_files(aws):
                print(f"{name:50} {size:>12}  {when}")
        if a.db_log_scarica:
            need("db_amministra")
            print(M.download_log(aws, a.db_log_scarica, env.work_path / "pglog" / env.name, cancel))

        if a.s3_bucket:
            need("file_lettura")
            for b in S.buckets(aws):
                print(f"{b.name:50} {b.output:22} {'sola lettura' if b.read_only else ''}")
        if a.s3_elenco:
            need("file_lettura")
            entries, truncated = S.list_level(aws, a.s3_elenco, a.percorso)
            for e in entries:
                print(f"{e.key}" if e.is_dir else f"{e.key}  {e.size}  {e.modified:%Y-%m-%d %H:%M}  {e.storage_class}")
            if truncated:
                print("(elenco troncato)")
        if a.s3_backup:
            need("file_lettura")
            path, r = S.backup(aws, a.s3_backup, a.percorso, cancel, progress)
            print(f"{path}: {r}")
            if r["errori"]:
                return 1

        if a.archivi or a.archivi_cerca:
            need("archivi")
            objs = A.list_archives(aws, cancel, progress)
            rec = None
            if a.inventario:
                need("db_accesso")
                rec = A.reconcile(objs, A.read_manifest(aws), A.archive_origin(aws))
                print(rec.text())
            if a.archivi:
                for g in A.by_table(objs, rec):
                    print(f"{g.source:45} {g.objects:5} archivi {g.size / 1e6:9.1f} MB  {g.first} -> {g.last}"
                          + (f"  righe {g.rows}" if g.rows is not None else "") + (f"  PROBLEMI {g.issues}" if g.issues else ""))
            if a.archivi_cerca:
                if not a.tabella:
                    ap.error("--archivi-cerca vuole --tabella schema.tabella")
                keys = [o.key for o in objs if o.source == a.tabella]
                rows = A.search(aws, keys, a.archivi_cerca, a.colonna, cancel, progress)
                print(f"{len(rows)} righe trovate in {len(keys)} archivi")
                if a.csv:
                    A.export_csv(rows, a.csv)
            if rec and rec.missing:
                return 1

        if a.retention_prova or a.retention_esegui:
            from app.env import retention as RT  # noqa: PLC0415

            need("retention")
            if a.retention_esegui and not confirm(f"ESECUZIONE REALE della retention su {env.name}"):
                return 1
            text, report = RT.run(ctx, a.retention_esegui, a.schema_retention, not a.senza_stagionali)
            print("\n".join(report))
            print(f"Retention: {text}")
        if a.retention_corse:
            from app.env import retention as RT  # noqa: PLC0415

            need("db_accesso|retention")
            cols, rows = RT.last_runs(aws, 30)
            print(" | ".join(cols))
            for r in rows:
                print(" | ".join("" if v is None else str(v) for v in r))

        if a.log:
            need("log")
            for x in L.search(aws, a.log, a.testo, a.minuti, cancel, progress):
                print(f"{x.when:%Y-%m-%d %H:%M:%S}  {x.message}")

        if a.scan:
            need("scan")
            from app.scan import awsutil as scan_aws  # noqa: PLC0415
            from app.scan import settings as scan_settings  # noqa: PLC0415
            from app.scan.scanner import run_scan  # noqa: PLC0415

            cfg = scan_settings.from_dict(settings.scan, settings.aws.profile)
            only, skip = _list(a.only), _list(a.skip)
            if only:
                cfg.sections = {k: k in only for k in scan_settings.SECTIONS}
            for k in skip:
                cfg.sections[k] = False
            override = None if not a.regions or a.regions.strip().lower() == "auto" else _list(a.regions)
            try:
                path = run_scan(cfg, cancel, progress,
                                lambda k, sc, lb, stt, it, nt: step(k, f"{sc} {lb}", stt, f"{it} {nt}".strip()), override)
            except scan_aws.Cancelled as exc:
                raise Cancelled() from exc
            print(f"Scansione: {path}")

        if a.costi:
            need("costi")
            rows, unit, periods = C.monthly_by_service(aws)
            for x in rows:
                print(f"{x.service:55} {x.current:10.2f} {x.previous:10.2f} {unit}")
            print(f"Totale {periods['corrente']}: {sum(x.current for x in rows):.2f} {unit}")
    except Cancelled:
        print("ESITO: interrotto")
        return 1
    except Exception as exc:  # noqa: BLE001
        print(f"ESITO: errore - {type(exc).__name__}: {exc}")
        return 1
    print("ESITO: completato")
    return 0


if __name__ == "__main__":
    sys.exit(main())
