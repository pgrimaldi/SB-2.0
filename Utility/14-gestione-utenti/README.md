# 14 — Gestione degli utenti AWS

Crea, mostra e cancella gli **utenti IAM** dell'account AWS 747862473836 (quello di sb-dev **e della
produzione legacy**: IAM è unico per l'account). Per ogni utente imposta nome, password della console e
**ruoli**, e alla creazione genera una **chiave d'accesso** (access key ID + secret) da vedere, copiare o
salvare in una scheda di testo nel formato di `~/.aws/credentials`.

I ruoli sono gruppi IAM e **si sommano**. Ogni utente può avere ruoli per l'ambiente **dev**, per
l'ambiente **prod** e per l'intero **account**, scelti in modo indipendente (una riga di caselle per
ambito). Un ruolo si indica come `ambito:nome` (`dev:frontend`, `prod:log`, `account:admin`). I ruoli prod
`frontend` e `backend` hanno già le policy (per la utility 23) ma l'ambiente prod resta **da definire**
finché lo stack `sb-prod` non esiste: si vedono, non si possono ancora assegnare.

Non tocca mai gli utenti creati a mano (oggi `michele` e `summerbooking2023`): li mostra, ma modifica e
cancella solo quelli con il tag `sb:gestito-da=utility-14`. Non usa l'utente root e rifiuta di partire
su un account diverso da `account_atteso`.

## Ruoli

| Ruolo | Gruppo IAM | Cosa può fare |
|---|---|---|
| *(base, sempre)* | `sb-ruolo-base` | Cambiare la propria password, gestire le proprie chiavi e il proprio MFA |
| `dev:frontend` | `sb-dev-ruolo-frontend` | Build e pubblicazione del front office su sb-dev (utility 20 «Aggiorna frontend»), log delle build |
| `dev:backend` | `sb-dev-ruolo-backend` | Build dell'immagine, migration, rilascio (utility 20 «Aggiorna backend»), log del backend, ECS Exec nel container, credenziali del DB, orario |
| `dev:fullstack` | *(preset)* | `dev:frontend` + `dev:backend` |
| `dev:database` | `sb-dev-ruolo-database` | Crea, carica, svuota ed elimina il DB di sb-dev, accesso dal PC, credenziali, orario |
| `dev:orario` | `sb-dev-ruolo-orario` | Solo accensione e spegnimento di sb-dev (utility 20, parte orario) |
| `dev:log` | `sb-dev-ruolo-log` | Log e metriche di sb-dev in sola lettura |
| `dev:file` | `sb-dev-ruolo-file` | File S3 di sb-dev (utility 22, linguette File S3 e Archivi): file applicativi in lettura e scrittura; archivio della retention, frontend e build in sola lettura |
| `prod:frontend` | `sb-prod-ruolo-frontend` | Build e pubblicazione del front office di produzione (utility 23, linguetta Frontend), log delle build. Policy pronte, attive quando esiste `sb-prod` |
| `prod:backend` | `sb-prod-ruolo-backend` | Build dell'immagine e rilascio del backend di produzione (utility 23, linguetta Backend), log del backend. Niente migration, niente credenziali del DB. Policy pronte, attive quando esiste `sb-prod` |
| `prod:fullstack` | *(preset)* | `prod:frontend` + `prod:backend` |
| `prod:database` | `sb-prod-ruolo-database` | **Da definire** |
| `prod:log` | `sb-prod-ruolo-log` | **Da definire** |
| `account:lettura` | `sb-ruolo-lettura` | Configurazione delle risorse dell'account senza dati (`ViewOnlyAccess`: niente file, segreti, log, righe) |
| `account:costi` | `sb-ruolo-costi` | Fatturazione e Cost Explorer in sola lettura |
| `account:infrastruttura` | `sb-ruolo-infrastruttura` | `PowerUserAccess` + IAM limitato: i ruoli IAM che crea devono avere il confine `sb-dev-confine`. MFA obbligatoria in console |
| `account:admin` | `sb-ruolo-admin` | `AdministratorAccess`, compresa questa utility. MFA obbligatoria in console |

Le policy sono in [app/ruoli/policy/](app/ruoli/policy/), ambienti, ruoli e gruppi in
[app/ruoli/ruoli.json](app/ruoli/ruoli.json). Ogni ambiente ha il suo stack (`dev` → `sb-dev`, `prod` →
`sb-prod`): i segnaposto `{{Output:...}}` di una policy si risolvono con gli output dello stack
dell'ambiente che la usa. Se lo stack cambia, va ripremuto **Prepara ruoli**.

### Come attivare i ruoli prod

Quando la produzione nuova avrà il suo stack:

1. le policy di `frontend` e `backend` ci sono già (`sb-prod-ruolo-comune`, `-frontend`, `-backend`,
   `-rilascio`); per `database` e `log` vanno ancora scritte. I segnaposto chiedono allo stack gli
   output `BackendBuildProject`, `FrontendBuildProject`, `BuildBucketName`, `FrontendBucketName`,
   `FrontendDistributionId`, `ClusterName`, `EcrRepositoryName`, `AppLogGroup`, `TaskRoleArn`,
   `TaskExecutionRoleArn` (lo stesso contratto della utility 23);
2. in `ruoli.json`, nell'ambiente `prod`: togliere `da_definire` dall'ambiente (e dai ruoli che si
   vogliono attivare dopo aver scritto le loro policy);
3. **Verifica ruoli**, poi **Prepara ruoli…**: crea policy e gruppi `sb-prod-ruolo-*`;
4. da quel momento le caselle prod si attivano e i ruoli si assegnano come quelli dev; la utility 23
   mostra a ciascun utente le linguette dei suoi ruoli.

Si può attivare un ruolo alla volta: un ruolo con `da_definire` resta visibile e non assegnabile anche
se l'ambiente è attivo.

Note sui ruoli:

- `backend` vede la password del DB di sb-dev (serve alle migration), e sb-dev può contenere i dati migrati
  dalla produzione.
- **`infrastruttura` non è limitato a sb-dev**: `PowerUserAccess` vale su tutto l'account, produzione
  legacy compresa (RDS `summerbooking`, bucket, …). Il confine impedisce di darsi permessi IAM in più, non
  di toccare le risorse di prod. Per un vero «solo dev» servirebbe un account AWS separato per sb-dev.
- **MFA in console**: per `infrastruttura` e `admin`, senza MFA la console mostra solo la configurazione
  dell'MFA. La CLI con le chiavi non è toccata.
- Lo stack `sb-dev` è nato senza confine sui suoi ruoli IAM: chi ha solo `infrastruttura` non può
  modificarne le policy finché il template non aggiunge `PermissionsBoundary: sb-dev-confine` ai ruoli.

## Prima volta

1. Profilo della AWS CLI di un **amministratore** (default `michele`) in `config/settings.local.json`.
2. `setup.ps1`.
3. Avviare la GUI e premere **Verifica ruoli**, poi **Prepara ruoli…**: crea le 11 policy e i 10 gruppi.
   Senza questo passo non si possono creare utenti.

## Avvio

```powershell
powershell -ExecutionPolicy Bypass -File setup.ps1     # una volta
.venv\Scripts\python.exe main.py                       # oppure F5 da VS Code
```

## Uso

All'avvio la GUI legge gli utenti (sola lettura).

| Dove | Pulsante | Cosa fa |
|---|---|---|
| 1. Ruoli | **Verifica ruoli** | Confronta policy e gruppi nell'account con `app/ruoli/` (scheda «Ruoli»): `OK`, `da creare`, `da aggiornare` |
| 1. Ruoli | **Prepara ruoli…** | Crea o aggiorna policy (nuova versione predefinita, si tengono le ultime 5) e gruppi; stacca dai gruppi le policy non previste |
| 1. Ruoli | **Aggiorna elenco utenti** | Utenti, ruoli, console, ultimo accesso, MFA, chiavi con stato e ultimo uso |
| 2. Nuovo utente | **Genera** | Password casuale (20 caratteri, maiuscole, minuscole, cifre, simboli) |
| 2. Nuovo utente | **Crea utente e chiave…** | Utente con tag, password della console (nessun cambio obbligatorio al primo accesso), gruppi, una chiave. Poi la finestra della chiave |
| 3. Utente selezionato | **Ruoli…** | Sostituisce i ruoli dev, prod e account (il gruppo base resta) |
| 3. Utente selezionato | **Password…** | Nuova password della console (o la crea se l'utente non l'aveva) |
| 3. Utente selezionato | **Nuova chiave…** | Seconda chiave, per ruotare: AWS ne ammette al massimo 2 |
| 3. Utente selezionato | **Disattiva / riattiva chiave** | Sulla chiave selezionata (riga sotto l'utente) |
| 3. Utente selezionato | **Cancella chiave…** | Definitiva |
| 3. Utente selezionato | **Cancella utente…** | Toglie password, chiavi, MFA, chiavi SSH, certificati, policy e gruppi, poi l'utente. Conferma scrivendo il nome |

**Finestra della chiave**: mostra utente, access key ID, secret, password (alla creazione) e indirizzo
della console, ognuno con «Copia». **Salva in file…** propone `data\<utente>_<ultime 4>_<data>.txt`;
**Copia scheda** copia lo stesso testo. Il secret esiste solo in questo momento: chiudere senza averlo
salvato o copiato chiede conferma. Nel log non finiscono mai secret né password.

Esempio di scheda:

```text
# Utente IAM mario.rossi - account AWS 747862473836
# ...
# Console:  https://747862473836.signin.aws.amazon.com/console
# Utente:   mario.rossi
# Password: ...

[mario.rossi]
aws_access_key_id = AKIA...
aws_secret_access_key = ...
region = eu-south-1
```

Protezioni: non si cancella né si toglie `admin` all'utente con cui si usa la utility; gli utenti non
gestiti hanno i pulsanti disattivati; un utente già esistente non si ricrea.

## Headless

Esempi in testa a `run_headless.py` (`--help`):

```text
run_headless.py --elenco
run_headless.py --verifica-ruoli
run_headless.py --prepara-ruoli --si
run_headless.py --crea mario.rossi --ruoli dev:fullstack,dev:log --scheda data\mario.txt --si
run_headless.py --ruoli-utente mario.rossi --ruoli dev:frontend,account:costi --si
run_headless.py --password mario.rossi --password-env NUOVA_PWD --si
run_headless.py --nuova-chiave mario.rossi --scheda data\mario-2.txt --si
run_headless.py --cancella mario.rossi --si
```

`--ruoli` vuole `ambito:nome`; senza prefisso un nome vale per dev o account quando non è ambiguo
(`fullstack`, `admin`). Un ruolo prod da definire viene rifiutato. Il secret non si stampa: `--crea` e `--nuova-chiave` vogliono `--scheda`. La password di `--crea` si
genera, oppure si legge dalla variabile d'ambiente indicata con `--password-env`, e finisce nella scheda.
Le scritture senza `--si` escono con 1 e dicono cosa avrebbero fatto. Uscita 0 = fatto, 1 = errore,
rifiuto o interruzione (anche policy in KO o gruppi saltati in `--verifica-ruoli` / `--prepara-ruoli`).

## Eseguibile

Pacchetto, manuale e procedura di aggiornamento sono comuni alle utility di questa cartella: vedi
[../README.md](../README.md). In breve: `build-all.ps1` (nella cartella superiore) crea
`../dist/14-gestione-utenti.zip` con eseguibili, configurazione di partenza, README e il manuale
`../manuali/Manuale-utility-14.docx` (Word, da aggiornare a mano quando cambiano le funzioni).
Per una build singola: `setup.ps1` (una volta), poi `build-exe.ps1` (output in `dist\`).

Al primo avvio si crea `config\settings.local.json`: impostare il profilo AWS dell'amministratore
(il valore di partenza è vuoto). Dopo una modifica delle policy in `app/ruoli/`, chi usa la utility
deve premere **Prepara ruoli…**.

## Note

- **Le schede sono segreti in chiaro**: `data/` è gitignorata; consegnarle per un canale sicuro e
  cancellarle dopo l'uso. Mai includere `data\` nello zip dell'eseguibile.
- L'account non ha una password policy: valgono le regole di base di AWS. La utility chiede comunque
  almeno 12 caratteri e tre tipi fra maiuscole, minuscole, cifre e simboli.
- `costi` funziona solo se sull'account è attivo «IAM user and role access to Billing information»
  (impostazione dell'utente root, in Account).
- Le policy `sb-dev-*` dipendono dai nomi dello stack: `sb-dev-power-utility` (orario) è creata dallo
  stack ed è solo agganciata; se manca, i gruppi che la usano risultano «saltato».
- L'elenco segnala le chiavi più vecchie di 90 giorni («da ruotare»): oggi sono le due di
  `summerbooking2023`, una delle quali è in uso (S3).
- Access Analyzer dà un solo avviso sulle policy: `iam:CreateServiceLinkedRole` su `*` nel confine,
  identico a quello di `PowerUserAccess`.

## Collaudo

### Prove eseguite (2026-10-19, account 747862473836, profilo `michele`)

| Prova | Esito |
|---|---|
| `setup.ps1` su venv nuovo, compilazione dei sorgenti | ✅ |
| `--help`, `--elenco` | ✅ 2 utenti, entrambi «non gestito», chiavi con ultimo uso |
| `--verifica-ruoli` con lo stack `sb-dev` reale | ✅ 32 segnaposto risolti, 11 policy e 10 gruppi «da creare» |
| `--prepara-ruoli` senza `--si` | ✅ rifiutato, uscita 1 |
| Le 11 policy con i valori veri passate ad Access Analyzer (`ValidatePolicy`) | ✅ nessun errore, 1 avviso (sopra) |
| GUI: avvio, elenco utenti, verifica ruoli, pulsanti disattivati su un utente non gestito | ✅ (da script; i pulsanti non sono stati premuti a mano) |

### Prove eseguite (2026-10-20)

| Prova | Esito |
|---|---|
| `--prepara-ruoli --si` | ✅ 11 policy e 10 gruppi creati; rilanciando `--verifica-ruoli` tutto `OK` (21) |
| `--crea prova-utility14 --ruoli fullstack --scheda …` | ✅ utente, console, gruppi base + frontend + backend, chiave; scheda scritta |
| Utility 20 `--stato` con le chiavi della scheda (`AWS_SHARED_CREDENTIALS_FILE` = scheda) | ✅ stack, DB, servizio, piano e Lambda letti come `prova-utility14` |
| IAM Policy Simulator sull'utente `fullstack`: 29 chiamate di stato / Aggiorna frontend / Aggiorna backend | ✅ tutte permesse (compresi `UpdateStack`, task definition, servizio, `PassRole` dei task) |
| Stesso simulatore: 7 azioni che `fullstack` non deve fare (cancellare DB sb-dev e prod, task di caricamento, file, IAM, `PassRole` del ruolo di build, altri stack) | ✅ tutte negate |
| `--cancella prova-utility14 --si` | ✅ 10 passi, utente sparito dall'elenco |
| Utility 20 **Aggiorna frontend** `production` come utente `fullstack` | ✅ zip su S3, build CodeBuild 162 s, pubblicazione, CloudFront 200 |
| Utility 20 **Aggiorna backend** (commit già in esercizio) come utente `fullstack` | ✅ manutenzione (piano SSM + Lambda), accesso al DB aperto e chiuso, controllo migration sul DB, `/health` Healthy; build e rilascio saltati perché già fatti |
| Utility 20 **Aggiorna backend** `--commit a4df259` (backend identico, tag nuovo) come `fullstack` | ✅ build CodeBuild 131 s e push su ECR; ❌ rilascio: l'update dello stack è tornato indietro due volte, mancavano `iam:GetRole` (attributo `Arn` dei ruoli dei task) e poi `apigateway:GET` (output `ApiUrl`). Il simulatore non li vedeva: li chiede CloudFormation per i `GetAtt` |
| Ruoli per ambito (dev / prod / account): `--verifica-ruoli` sulla struttura nuova, lettura dei ruoli degli utenti esistenti, `dev:fullstack`, nomi senza prefisso, ruoli prod rifiutati, caselle della GUI | ✅ i gruppi esistenti non cambiano nome |

| Stack `sb-dev` passato al ruolo di servizio `sb-dev-cfn-deploy` (CloudFormation aggiorna con i suoi permessi): `sb-dev-ruolo-rilascio` ridotta a `UpdateStack` + `iam:PassRole` su quel ruolo; `PassRole` del ruolo d'esecuzione spostato in `database`; oggetti di tutto il bucket di build; `iam:SimulatePrincipalPolicy` su sé stessi nel ruolo base (la utility 20 verifica i permessi prima di attivare i pulsanti) | ✅ pubblicate con Prepara ruoli |
| Utility 20 **Aggiorna backend** `--commit a4df259` come `fullstack`, con le policy nuove | ✅ migration controllate, **stack aggiornato in 242 s**, `/health` Healthy |
| Utility 20 **Aggiorna backend** (ultimo commit di `dev`, `560063b`) come `fullstack` | ✅ build 141 s, migration, **stack aggiornato in 200 s** tramite `sb-dev-cfn-deploy`, Healthy |
| Cancellazione dell'utente di prova e della sua scheda | ✅ |

Nota: la verifica preventiva della utility 20 simula `iam:PassRole` senza dire a quale servizio: una
condizione `iam:PassedToService` nella policy la fa fallire. Per questo `PassRole` è limitato solo per
risorsa (il ruolo `sb-dev-cfn-deploy` può essere assunto solo da CloudFormation).

Non ancora provati sul reale: i ruoli `database`, `log`, `infrastruttura`, `costi`; la finestra della
chiave nella GUI; `build-exe.ps1`.
