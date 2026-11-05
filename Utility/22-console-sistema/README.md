# 22 — Console del nuovo sistema

Pannello unico per gestire il nuovo sistema su AWS (account 747862473836, `eu-south-1`), per
l'**ambiente scelto** nell'intestazione: `dev` (stack `sb-dev`) o `prod` (stack `sb-prod`, non ancora
creato). Raccoglie e sostituisce le utility 12 (scansione), 20 (ambiente dev) e copre tutto quello
che fa la 23 (produzione), più backup del DB, file S3, dati archiviati, log e costi.

Le linguette e i pulsanti si attivano secondo i **ruoli dell'utente** (gruppi IAM della utility 14):
una linguetta senza funzioni abilitate non si vede, un pulsante senza ruolo resta grigio. La linguetta
«Ruoli» dice, funzione per funzione, quale gruppo serve. Il permesso vero lo decide comunque IAM.

Non scrive mai nel repository; dello stack cambia solo i parametri `ImageTag` e `DatabaseEnabled`
(deploy, DB dev) oppure applica un change set letto e confermato (Infrastruttura). Rifiuta di partire
se un nome di risorsa non contiene il nome del suo ambiente o se l'account non è `account_atteso`.

## Linguette e ruoli

| Linguetta | Cosa fa | Ruoli (oltre ad `admin`) |
|---|---|---|
| Stato e orario | Stato di stack, DB, servizi; in dev accensione/spegnimento programmati (come la 20) | qualunque ruolo dell'ambiente, `lettura`, `infrastruttura`; l'orario: `orario`, `backend`, `database` |
| Infrastruttura | Change set dal template dell'ambiente: anteprima, applica, scarta (come la 23) | `infrastruttura` |
| Deploy | Backend e frontend da un commit del remoto | `backend`, `frontend` |
| Backup DB | Dump del DB in locale, compresso a scelta; verifica, elenco | `database` |
| Database | Crea / aggiorna da PostgreSQL locale / svuota / elimina (solo dev) | `database` |
| Gestione DB | Connessione e porte (mio IP, IP specifico, tutti), istanza e formati di connessione, panoramica, sessioni, query in sola lettura con query pronte, snapshot manuali, log di PostgreSQL | `backend`, `database`; chiudere sessioni, snapshot e log: `database` |
| File S3 | Bucket dello stack: naviga, dimensione, scarica, backup incrementale, link 1 ora, carica, cancella | `file` |
| Archivi | Archivi della retention: per tabella, confronto con l'inventario, anteprima, ricerca, CSV; **esecuzione della retention** (prova ed esecuzione reale) e ultime corse | `file` (+ `backend`/`database` per l'inventario); la retention: `database` |
| Log | Ricerca nei gruppi di log dell'ambiente, esportazione | `log`, `backend`, `infrastruttura` |
| Scansione | Fotografia dell'intero account (come la 12) | `infrastruttura` |
| Costi | Cost Explorer: mese in corso e precedente per servizio | `costi` |
| Ruoli | Funzioni abilitate e gruppi dell'utente | sempre visibile |

I ruoli dell'ambiente sono i gruppi `sb-<ambiente>-ruolo-<nome>`, quelli dell'account `sb-ruolo-<nome>`.
Un utente con `AdministratorAccess` agganciata direttamente (es. `michele`) conta come `admin`. Il ruolo
**`file`** è nuovo: creato nell'account il 2026-10-20 con «Prepara ruoli» della 14, nello stesso giro in cui
la policy `database` ha avuto snapshot manuali `<istanza>-u22-*` e file di log di PostgreSQL. I ruoli prod sono ancora «da definire» nella 14.

## Prima volta

1. Profilo della AWS CLI dell'utente (creato con la utility 14) in `aws.profile`.
2. Sul PC, secondo le funzioni usate: `git` con accesso al repository (deploy); .NET SDK 10 e
   `dotnet-ef` (migration in dev); PostgreSQL 17 locale con `pg_dump`/`pg_restore` (DB dev, verifica dei
   backup `.dump`).
3. `setup.ps1`. Al primo uso del PostgreSQL locale la password si chiede e si salva nel Gestore
   credenziali di Windows (si riusa quella già salvata dalla utility 20).

## Avvio

```powershell
powershell -ExecutionPolicy Bypass -File setup.ps1     # una volta
.venv\Scripts\python.exe main.py                       # oppure F5 da VS Code
```

All'avvio legge utente e ruoli, poi da sola stato, bucket e gruppi di log dell'ambiente. Cambiando
ambiente i dati si svuotano e si rileggono; il passaggio a `prod` chiede conferma e l'intestazione
diventa rossa. In prod ogni scrittura si conferma scrivendo il nome dello stack.

## Uso

### Backup DB

Il dump lo fa il task Fargate `dbloader` dello stack, dentro la VPC, con lo script sostituito: dal PC
una sessione lunga verso RDS cade dopo circa 10 minuti. Il task carica il file nel bucket di build
(`dump/backup/…`) con un URL firmato, la utility lo scarica in `data/backup/<ambiente>/`, verifica lo
SHA-256 calcolato nel task e toglie la copia dal bucket (salvo «Tieni anche la copia»). Accanto al
file un `.json` con ambiente, endpoint, formato, chi e quando.

| Opzione | Effetto |
|---|---|
| Formato `custom` | `pg_dump -Fc`, compressione interna 0–9; si ripristina con `pg_restore` |
| Formato `SQL` | testo; con compressione > 0 diventa `.sql.gz` |
| Schemi | vuoto = tutto il DB, altrimenti `-n` per schema |
| Solo schema | `--schema-only` |

**Verifica selezionato** rilegge lo SHA-256 e, per i `.dump`, l'indice con `pg_restore --list`; per i
`.gz` decomprime tutto. Se il DB è spento dall'orario lo si accende in manutenzione per il dump.

### Infrastruttura

Mai un aggiornamento diretto: **Anteprima modifiche** crea un change set (`u22-…`) dal template di
`ambienti.<nome>.infra.template` con i parametri di `infra.parametri` (gli altri restano quelli in
esercizio); sostituzioni e rimozioni in rosso. **Applica…** chiede il nome dello stack; alla creazione
attiva la protezione dalla cancellazione se `protezione_cancellazione`. Un template oltre 51.200 byte
(quello di sb-dev) si carica nel bucket di build e si toglie subito dopo; per questo la *prima*
creazione di uno stack con template grande va fatta a mano.

### Deploy

Come la 20 (zip del commit del remoto → CodeBuild → rilascio con `ImageTag` → verifica servizio e
`/health`), con le regole della 23 dove servono:

- `backend.migration = applica` (dev): le migration pendenti si applicano dal PC, DB in manutenzione,
  con seconda conferma;
- `backend.migration = blocca` (prod): se il commit cambia file di migration rispetto a quello in
  esercizio il rilascio si ferma **prima della build**; al primo rilascio si conferma che lo schema è
  già allineato; prima del rilascio e della pubblicazione c'è una conferma in più.

Progetti CodeBuild e repository ECR si leggono dagli output dello stack (`BackendBuildProject`,
`FrontendBuildProject`, `EcrRepositoryName`/`EcrRepositoryUri`) e, se mancano, dalla configurazione.

### File S3

Solo i bucket esposti dagli output dello stack (`s3.bucket_output`). I bucket in `s3.sola_lettura`
(l'archivio della retention; in prod anche i file) non accettano caricamenti né cancellazioni. Il
**backup del percorso** scarica in `data/s3/<ambiente>/<bucket>/` con un indice degli ETag: rilanciato
scarica solo il nuovo o il cambiato, e segnala (senza cancellare) i file locali spariti dal bucket.
La cancellazione si conferma scrivendo il nome del bucket.

### Archivi

Gli oggetti `archivio/<schema>/<tabella>/<anno>/<mese>/<istante>-<policy>-<lotto>.jsonl.gz` del bucket
di archivio, raggruppati per tabella di origine. **Confronta con l'inventario** legge
`privacy.privacy_archive_manifests` (accesso dal PC aperto per la durata della query) e segnala righe di
inventario senza oggetto (gravi), oggetti senza inventario, oggetti segnati come rimossi ma presenti.
Anteprima, ricerca per testo (in tutte le colonne o in una) ed esportazione CSV (`;`, UTF-8 con BOM).
**Sola lettura**: un archivio non si modifica, non si cancella e non si reinserisce nel DB.

Il confronto riguarda solo le righe dell'inventario scritte sullo **storage dell'ambiente** (etichetta
`Retention__Archive__Origin` letta dalla task definition del backend, es. `s3_archivio_dev`; ripiego:
`archivi.origine`). Le righe di altri storage, per esempio `s3_archivio_local` arrivate con un DB
copiato dal PC, sono contate a parte, perché il loro oggetto non può essere in questo bucket.

**Retention** (riquadro in fondo, ruolo `database`): il backend non ha uno scheduler, quindi le policy
girano solo quando qualcuno lancia `Boot -- retention`. La 22 lo lancia come task Fargate, con la task
definition del backend (stessa immagine, stesse connessioni e bucket dell'ambiente) e il comando
cambiato. **Prova** = senza `--apply`: valuta le policy e scrive il verbale, non tocca né dati né file.
**Esegui…** = `--apply`, con conferma scrivendo il nome dello stack: conviene fare prima la prova e uno
snapshot. Si può limitare a uno schema e togliere le policy stagionali. Il resoconto stampato dal
comando si apre in una finestra; **Ultime corse** mostra `privacy.privacy_retention_runs`.

### Gestione DB

| Sotto-linguetta | Cosa fa |
|---|---|
| Connessione e porte | Credenziali dal segreto del DB; IP che raggiungono la 5432. **Apri al mio IP** (temporaneo), **Apri un altro IP…** (IP o CIDR, temporaneo o permanente, con nota), **Apri a tutti (temporaneo)…** (`0.0.0.0/0`, conferma scrivendo il nome dello stack), **Chiudi selezionati**. Le aperture temporanee si chiudono dopo `ore_massime_manutenzione` ore, alla prima lettura dell'elenco |
| Istanza e formati | Caratteristiche dell'istanza RDS (stato, motore, classe, storage, accessibilità da internet, Multi-AZ, backup, ultimo istante ripristinabile, protezione…) e la connessione nei formati del progetto: URI, Npgsql (`ConnectionStrings__SummerBooking`), JDBC, comando `psql`, variabili d'ambiente. A video la password è mascherata, «Copia» la porta intera |
| Panoramica | Versione, dimensione, connessioni; per schema tabelle, dimensione e righe stimate; ultima migration e quante applicate; le 25 tabelle più grandi |
| Sessioni | `pg_stat_activity` con chi blocca chi (in rosso le bloccate); **Annulla la query** / **Chiudi la sessione** (ruolo `database`) |
| Query | Una sola istruzione che inizia con `select`/`with`/`explain`/`show`/`table`/`values`, in transazione READ ONLY, al massimo 60 s e 5.000 righe; **query pronte** del progetto (migration, righe per tabella, retention: corse, policy, inventario archivi, cancellazioni in coda; audit; indici non usati; righe morte; blocchi; estensioni); esportazione CSV in `data/query/<ambiente>/` |
| Snapshot e log | Snapshot dell'istanza; **Crea snapshot manuale** (`<istanza>-u22-<data>`, utile prima di un deploy con migration o di una retention) e cancellazione dei soli snapshot della utility; elenco e scarico dei file di log di PostgreSQL in `data/pglog/<ambiente>/` |

Ogni lettura sul DB apre l'accesso all'IP del PC per la sua durata (se era già aperto lo lascia).

**Utente in sola lettura** (`database.utente_lettura`, default `sb_lettura`; riquadro in «Query»):
**Crea / riallinea…** (ruolo `database`) crea l'utente PostgreSQL con login, al massimo 5 connessioni,
`default_transaction_read_only`, tetto di 60 s, `pg_read_all_stats` (per vedere le query delle altre
sessioni) e **solo `SELECT`** sugli schemi del progetto, comprese le tabelle che l'utente principale
creerà dopo (default privileges). La password, generata, sta nel segreto `<db_secret>-lettura`
(`sb-dev/db-lettura`); rilanciato non la cambia. Da quel momento query, panoramica e sessioni passano da
lui (chiudere le sessioni resta all'utente principale) e «Istanza e formati» mostra anche i suoi formati
di connessione: sono quelli da dare a chi deve solo consultare il DB. Dopo «Crea DB» / «Aggiorna DB»
(schema + dati) i permessi si riallineano da soli; dopo uno schema nuovo va ripremuto «Crea / riallinea».
**Rimuovi…** chiude le sue sessioni, toglie i permessi e l'utente, e mette il segreto in cancellazione
(recuperabile per 7 giorni).

### Altre linguette

Stato e orario, Database e la parte «Connessione e porte» di Gestione DB funzionano come nella utility 20 (vedi il suo README e
`ISTRUZIONI.md`); la Scansione come la 12 (configurazione nel blocco `scan`, output in `data/scan/`).

## Headless

Esempi in testa a `run_headless.py` (`--help`). Stessi controlli di ruolo della GUI; `--ambiente`
sceglie l'ambiente. Le scritture senza `--si` escono con 1 e dicono cosa avrebbero fatto; con `--si` le
conferme a metà lavoro sono date per accettate. Uscita 0 = fatto (anche con avvisi), 1 = errore,
rifiuto, interruzione o archivi inventariati mancanti.

```text
run_headless.py --ruoli
run_headless.py --backup --formato custom --compressione 9
run_headless.py --infra-anteprima --ambiente prod
run_headless.py --aggiorna-backend --ambiente prod --branch main --si
run_headless.py --s3-backup sb-dev-files-747862473836
run_headless.py --archivi --inventario
run_headless.py --scan --skip ecr_images
run_headless.py --db-info
run_headless.py --retention-prova
run_headless.py --retention-esegui --schema-retention booking --si
run_headless.py --db-lettura-crea --si
run_headless.py --db-query-pronta "Retention: ultime corse" --csv data\r.csv
run_headless.py --db-apri-tutti --si
run_headless.py --db-snapshot-crea --si
```

Corrispondenze con la 23: `--chi` → `--ruoli`, `--stato` → `--stato`/`--infra-elenco`, `--commit` →
`--commit-elenco`, `--rev` → `--commit`, `--infra-anteprima`/`--infra-applica` uguali.

## Eseguibile

Pacchetto, manuale e procedura di aggiornamento sono comuni alle utility di questa cartella: vedi
[../README.md](../README.md). In breve: `build-all.ps1` (nella cartella superiore) crea
`../dist/22-console-sistema.zip` con eseguibili, configurazione di partenza, README e il manuale
`../manuali/Manuale-utility-22.docx` (Word, da aggiornare a mano quando cambiano le funzioni).
Per una build singola: `setup.ps1` (una volta), poi `build-exe.ps1` (output in `dist\`).

Al primo avvio si crea `config\settings.local.json`: impostare profilo AWS e, per le funzioni
che li usano, percorso del repository, PostgreSQL e template dell'infrastruttura (i valori di partenza
sono vuoti). Dopo una modifica a `app/env/plan.py` o `app/env/lambda_src/index.py` va ripremuto
«Installa riconciliatore».

## Note

- **Dati reali e segreti in locale**: `data/backup`, `data/s3`, `data/archivi` e `data/scan` contengono
  dati personali o segreti in chiaro. `data/` è gitignorata; non va mai inclusa nello zip.
- **Contratto con lo stack**: ogni funzione legge gli output di cui ha bisogno e, se mancano, lo dice e
  si ferma. Il template di produzione (`infra/summerbooking-aws.yaml`) non è ancora allineato al
  contratto di sb-dev (niente `dbloader`, CodeBuild, frontend, `ImageTag`): in prod oggi funziona solo
  la linguetta Infrastruttura. L'elenco completo degli output è nel README della 23.
- **Backup**: il task scarica `curl` all'avvio, quindi ha bisogno di uscire su internet (in sb-dev ha
  l'IP pubblico). Un singolo PUT su S3 regge fino a 5 GB.
- **Interrompi** ferma attese, build e task, ma un aggiornamento dello stack già partito prosegue.
- Le regole di accesso al DB aperte dalla utility 20 (`utility20 …`) sono riconosciute come temporanee
  o permanenti e trattate come quelle della 22.
- Costi: ogni lettura di Cost Explorer costa 0,01 USD.

## Collaudo

### Prove eseguite (2026-10-20, account 747862473836, profilo `michele`)

| Prova | Esito |
|---|---|
| `--help`, `--ruoli`: `michele` riconosciuto admin (AdministratorAccess diretta) | ✅ |
| Abilitazioni con identità simulate (`frontend`, `orario`, `database`, `costi`, `lettura`, `infrastruttura`, admin per gruppo) su dev e prod | ✅ come da tabella |
| `--stato` dev; prod con stack assente → messaggio chiaro, uscita 1 | ✅ |
| **Backup** custom z6 di sb-dev | ✅ 140,9 MB in ~3 min, SHA-256 verificato, copia nel bucket tolta |
| `--backup-verifica` del `.dump` | ✅ 4.903 voci, 225 tabelle con dati |
| Backup SQL `.sql.gz` del solo schema `identity` | ✅ 11,7 MB |
| File S3: bucket dell'ambiente, elenco, backup incrementale del bucket del frontend | ✅ 244 file; rilancio: solo i 3 cambiati |
| Archivi: elenco (il bucket di sb-dev è vuoto) | ✅ 0 archivi |
| Log: ricerca su `/summerbooking/dev/api` | ✅ 262 righe |
| Accesso al DB: elenco (regola permanente della 20 riconosciuta) | ✅ |
| Infrastruttura dev: `--infra-elenco`, anteprima con template > 51.200 byte via S3, scarta | ✅ «nessuna modifica» fra repository e stack |
| `--commit-elenco` su `dev`, rifiuto di `--infra-applica` senza `--si` | ✅ |
| GUI da script: utente e ruoli, letture automatiche, linguette e pulsanti secondo ruolo e bucket in sola lettura, cambio a prod (linguetta Database nascosta, errori delle letture automatiche solo nel log) | ✅ (i pulsanti non sono stati premuti a mano) |

Difetti trovati e corretti durante il collaudo:

1. 3 file su 244 non si scaricavano: `Path.resolve()` di Windows, con più thread che creano la stessa
   cartella, dava risultati diversi e faceva scattare il controllo dei percorsi. Ora il controllo è solo
   lessicale e i caratteri non ammessi da Windows diventano `_`.
2. Le letture automatiche dopo il riconoscimento dell'utente non partivano (chieste mentre il lavoro
   «Utente e ruoli» era ancora in corso): ora girano in catena a lavoro finito.
3. Ereditato dalla 23: il tag `sb:gestito-da` sul change set diventa un tag dello **stack** e
   CloudFormation lo propaga a ogni risorsa (61 modifiche su sb-dev, alcune con sostituzione
   «Conditional»). Tolto. **La 23 ha ancora lo stesso difetto.**

### Prove eseguite (2026-10-20, pomeriggio) — Gestione DB

| Prova | Esito |
|---|---|
| Ruolo `file` e policy `database` aggiornata: «Prepara ruoli» della 14, poi «Verifica ruoli» | ✅ 23 OK |
| `--db-info`: istanza `sb-dev-postgres` e 5 formati | ✅ |
| `--db-panoramica`: 12 schemi, 1.666 MB, migration per schema | ✅ |
| `--db-sessioni` | ✅ (nessuna sessione oltre alla utility) |
| Query pronta «Retention: inventario degli archivi» | ✅ 2 archivi nell'inventario (il bucket di sb-dev è vuoto: il confronto li segnalerebbe come mancanti) |
| Rifiuti: due istruzioni, `delete`, `with … delete … where false` | ✅ i primi due dal controllo, il terzo dalla transazione READ ONLY |
| Snapshot manuale creato (~5 min) e cancellato; cancellazione di uno automatico rifiutata | ✅ |
| Elenco e scarico di un file di log di PostgreSQL | ✅ |
| GUI da script: tutte le sotto-linguette con i dati veri | ✅ |

| Utente in sola lettura: policy della 14 aggiornate («Prepara ruoli»), `--db-lettura-crea` | ✅ `sb_lettura` su 12 schemi, segreto `sb-dev/db-lettura`; rilanciato: «riallineato», stessa password |
| Scritture fatte **direttamente** con `sb_lettura`, senza i controlli della utility | ✅ `delete`/`create` rifiutati dalla sola lettura; `set transaction read write` è accettato (il default si può aggirare), ma poi `delete`, `update`, `truncate`, `create` in `public` e negli schemi vengono rifiutati per **permessi**: la protezione vera sono i permessi |
| Query pronte, panoramica, sessioni con `sb_lettura`; GUI: 10 formati (5 in sola lettura), password mai a video | ✅ |

### Prove eseguite (2026-10-21) — Retention

| Prova | Esito |
|---|---|
| Diagnosi: perché su sb-dev la linguetta Archivi era vuota | le policy sono attive (59 su 64) ma la retention su sb-dev non era **mai stata eseguita**; le 181 corse e i 3 archivi del verbale vengono dal DB locale copiato (archivi su `s3_archivio_local`, cioè LocalStack) |
| Confronto con l'inventario dopo la correzione | ✅ «0 mancanti; su altri storage: s3_archivio_local 3» (prima sarebbero risultati 3 archivi mancanti) |
| Policy della 14 per la retention («Prepara ruoli»): `RunTask` sulla task definition del backend, `PassRole` su `<stack>-task`, log dell'API; `DescribeTaskDefinition` nella policy comune | ✅ 23 OK |
| `--retention-prova` su sb-dev (task Fargate con `retention`) | ✅ 62 policy: 53 ok, 9 saltate (8 su prefisso di storage, 1 delegata al job delle foto), 0 fallite; 64.895 righe trovate, 0 toccate |
| GUI: «Ultime corse», confronto con l'inventario | ✅ |

**Esecuzione reale (`--apply`) non ancora lanciata**: la decide il titolare.

Non provati: chiusura di una sessione, apertura a tutti, «Rimuovi» dell'utente in sola lettura, il
riallineamento automatico dopo «Aggiorna DB», utenti con soli ruoli `backend`/`database`.

Incidente durante le prove: ripulendo `template/` nel bucket di build è stato cancellato anche
`template/0209b98….template` (caricato alla creazione di sb-dev). Il bucket non ha versioning; è stato
ricaricato con lo stesso nome dal template dello stack in esercizio, identico byte per byte a
`infra/dev/sb-dev.yaml`.

Non ancora provati sul reale: orario dai pulsanti, deploy (lanciati dall'utente), applicazione di un
change set, operazioni sul DB dev, caricamento/cancellazione/link S3, archivi con contenuto e confronto
con l'inventario, scansione, costi, utenti non amministratori veri (ruolo `file` non ancora creato),
tutto ciò che è prod (stack assente), `build-exe.ps1`.
