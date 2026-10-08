# Deploy Summer Booking su S3 e CloudFront

Questa guida descrive come creare da zero, su AWS, tutto quello che serve per pubblicare il frontend e come funziona il deploy automatico. La stessa procedura vale per DEV, PROD e altri ambienti: cambiano solo nomi, valori e ruoli, che devono essere separati per ogni ambiente.

Credenziali, chiavi AWS e secret non devono mai essere salvati nel repository o nei file di ambiente Angular.

## Architettura

```text
push su GitHub (branch dell'ambiente)
  → CodePipeline: stage Source (CodeConnections)
  → CodePipeline: stage Build (azione AWS CodeBuild)
      → CodeBuild esegue buildspec.yml: npm ci, verifiche (lint, tipi dei test, test unitari),
        build dell'ambiente (dev o production),
        caricamento su S3 con intestazioni di cache, invalidazione CloudFront
  → bucket S3 privato ← CloudFront (Origin Access Control) ← browser
```

Tutte le istruzioni di build e deploy stanno nel file versionato `buildspec.yml`. La pipeline non contiene comandi propri: prende il codice e avvia CodeBuild.

Nomi usati in DEV (per un nuovo ambiente sostituire `dev` con il nome dell'ambiente):

| Risorsa | Nome DEV |
|---|---|
| Pipeline | `summer-booking-frontend-dev` |
| Progetto CodeBuild | `summer-booking-front-office-dev` |
| Ruolo della build | `codebuild-summer-booking-front-office-dev-service-role` |
| Ruolo della pipeline | `AWSCodePipelineServiceRole-eu-north-1-summer-booking-frontend-d` |
| Regione | `eu-north-1` (Europa - Stoccolma) |

Nome del bucket e ID della distribuzione sono nelle variabili d'ambiente del progetto CodeBuild.

## 1. Bucket S3

Console **S3** → **Crea bucket**, nella regione dell'ambiente:

- nome univoco, es. `summer-booking-frontend-<ambiente>-<account>-<regione>`;
- **Blocca tutti gli accessi pubblici**: attivo. Il bucket resta privato e lo legge solo CloudFront;
- controllo versioni: facoltativo;
- crittografia predefinita: **SSE-S3**. Con una chiave KMS dedicata servono permessi KMS aggiuntivi nel ruolo della build;
- **non** attivare l'hosting di siti web statici.

La policy del bucket per CloudFront viene generata al passo 2.

## 2. Distribuzione CloudFront

Console **CloudFront** → **Crea distribuzione**:

- **Origine**: il bucket S3 (endpoint REST, non l'endpoint "website");
- **Accesso all'origine**: **Origin Access Control (OAC)**, creando un nuovo controllo con le impostazioni predefinite. Al termine la console propone la policy del bucket: copiarla in S3 → bucket → **Autorizzazioni** → **Policy del bucket**. Consente a questa sola distribuzione di leggere gli oggetti (`s3:GetObject`);
- **Criterio del protocollo visualizzatore**: **Reindirizza HTTP a HTTPS**;
- **Criterio di cache**: **CachingOptimized** (TTL minimo 1 secondo, predefinito 1 giorno, massimo 1 anno): CloudFront usa il `max-age` impostato dal deploy entro questi limiti; `no-cache` diventa al massimo 1 secondo di cache su CloudFront, mentre il browser lo rispetta. Le future API (`/api`) avranno un comportamento separato, senza cache delle risposte personali;
- **Compressione automatica degli oggetti**: attiva;
- **Oggetto root predefinito**: `index.html`;
- **Firewall (WAF)**: secondo le esigenze dell'ambiente;
- **Risposte di errore personalizzate**: **nessuna** (vedi passo 3).

### Response headers di sicurezza

Creare una policy personalizzata da **CloudFront → Policies → Response headers → Create response headers policy**. Usare un nome specifico per ambiente, ad esempio `summerbooking-<ambiente>-frontoffice-security-headers`; la policy DEV corrente si chiama `summerbooking-dev-frontoffice-security-headers`.

Lasciare **CORS disabilitato**: questa distribuzione serve il frontend statico e non deve aggiungere indiscriminatamente header CORS alle risposte. Nella sezione **Security headers** configurare:

| Impostazione | Valore | Origin override |
|---|---|---|
| `Strict-Transport-Security` | `max-age=31536000` | attivo |
| HSTS `includeSubDomains` | disattivato | — |
| HSTS `preload` | disattivato | — |
| `X-Content-Type-Options` | `nosniff` | attivo |
| `X-Frame-Options` | `DENY` | attivo |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | attivo |
| `X-XSS-Protection` | disattivato | — |
| `Content-Security-Policy` | vedi "Content Security Policy" qui sotto | attivo |

`X-XSS-Protection` è un header legacy e non viene abilitato.

#### Content Security Policy

La build non mette più script né stili inline in `index.html` (`optimization.styles.inlineCritical: false` in `angular.json`), quindi la policy può vietare ogni script che non arrivi dal nostro dominio. Valore della policy, su una riga:

```text
default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; worker-src 'self'; manifest-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; upgrade-insecure-requests
```

Stato: su DEV la `Content-Security-Policy` è attiva dal 07/10/2026; su PROD va fatta con gli stessi passi.

Si attiva in due fasi, prima su DEV e poi su PROD:

1. **Report-Only (solo segnalazioni)**. CloudFront non ha questa variante tra i *Security headers*: nella stessa policy aprire **Custom headers → Add header**, nome `Content-Security-Policy-Report-Only`, valore la policy qui sopra, **Origin override** attivo. Il browser esegue tutto ma scrive in console ogni violazione (`[Report Only] Refused to …`).
2. **Verifica**. Con la console del browser aperta, aprire ogni pagina in IT e in EN, anche dopo il login: home, popup di accesso, gestionale, magazzino con i suoi popup, datepicker, impostazioni, aggiornamento del service worker. Nessun messaggio `Refused` deve comparire. Ripetere nei giorni successivi durante l'uso normale.
3. **Attivazione**. Togliere il custom header `Content-Security-Policy-Report-Only` e, nei **Security headers**, attivare `Content-Security-Policy` con lo stesso valore e **Origin override** attivo. Da qui il browser blocca davvero ciò che la policy non permette.
4. **Controllo**. `curl -I https://<dominio>/` deve mostrare `content-security-policy` con il valore atteso (e non più `content-security-policy-report-only`).

La policy permette solo il nostro dominio: un servizio esterno futuro (API su un altro dominio, pagamenti, mappe, analytics, font esterni) va aggiunto alla direttiva giusta (es. `connect-src`) prima di usarlo, solo con i domini indicati dalla sua documentazione, ripassando dalla fase Report-Only. Il valore della CSP in CloudFront non può superare 1783 caratteri. Decisioni e punti ancora aperti (Trusted Types, raccolta dei report) sono in `SECURITY_TODO.md`.

Non attivare `includeSubDomains` o `preload` e non aumentare la durata HSTS senza aver verificato che tutti i sottodomini interessati funzionino esclusivamente in HTTPS: un'impostazione errata resta memorizzata dai browser e può rendere irraggiungibili servizi ancora HTTP.

Dopo aver creato la policy:

1. aprire **Distribuzioni → distribuzione dell'ambiente → Comportamenti**;
2. selezionare `Default (*)` e scegliere **Modifica**;
3. in **Response headers policy** selezionare la policy dell'ambiente;
4. salvare e attendere che la distribuzione torni nello stato **Deployed**.

Non serve invalidare la cache per questa modifica: CloudFront applica la policy anche alle risposte servite dalla cache. Riferimenti ufficiali: [creazione e associazione della policy](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/creating-response-headers-policies.html) e [funzionamento delle response headers policy](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/modifying-response-headers.html).

Poiché CloudFront non ha il permesso `s3:ListBucket`, un file inesistente restituisce **403** e non 404. È voluto: gli errori reali restano visibili.

## 3. CloudFront Function per le route dell'app

Non configurare un fallback globale che trasformi ogni 403 o 404 in `/index.html`: nasconderebbe errori reali di asset, permessi o route inesistenti, e produrrebbe "soft 404" per i motori di ricerca.

Le pagine dell'app aperte da URL diretto o ricaricate vengono riscritte verso `/index.html` da una funzione: le pagine pubbliche di un elenco esplicito e tutte le pagine dell'area riservata (ogni indirizzo senza estensione fuori da `/it` e `/en`). I file (con estensione) restano serviti così come sono da S3, anche quando mancano:

1. Console **CloudFront** → **Funzioni** → **Crea funzione**, runtime più recente proposto.
2. Scheda **Sviluppo**: incollare il codice seguente e **Salva modifiche**.
3. Scheda **Pubblica**: **Pubblica funzione**. La distribuzione usa solo la versione pubblicata.
4. **Distribuzioni** → distribuzione dell'ambiente → **Comportamenti** → `Default (*)` → **Modifica** → **Associazioni di funzioni** → **Richiesta visualizzatore**: tipo **CloudFront Functions**, selezionare la funzione.

```js
// Pagine pubbliche servite da index.html: solo queste, così un indirizzo pubblico sbagliato resta un 404 vero.
// Aggiornare questo elenco quando si aggiunge una pagina pubblica (/it/..., /en/...).
var PUBLIC_ROUTES = {
    '/': true,
    '/home': true,
    '/it': true,
    '/it/home': true,
    '/en': true,
    '/en/home': true
};

function handler(event) {
    var request = event.request;
    var uri = request.uri;
    // Un file ha un punto nell'ultimo pezzo dell'indirizzo (main.js, logo.svg): lo serve S3 così com'è.
    var isFile = uri.substring(uri.lastIndexOf('/') + 1).indexOf('.') !== -1;
    var isPublic = uri === '/it' || uri === '/en' || uri.indexOf('/it/') === 0 || uri.indexOf('/en/') === 0;

    // Pagine pubbliche in elenco e tutte le pagine dell'area riservata (es. /warehouse, /settings/sharing).
    if (PUBLIC_ROUTES[uri] === true || (!isFile && !isPublic)) {
        request.uri = '/index.html';
    }

    return request;
}
```

Route servite:

- `/` e `/home`, che reindirizzano alla home nella lingua preferita;
- `/it`, `/it/home`, `/en`, `/en/home`, le pagine pubbliche localizzate;
- tutte le pagine dell'area riservata (`/beachmap`, `/warehouse`, `/settings`, `/settings/…` e quelle future), senza elencarle: senza sessione l'app rimanda alla home, con una sessione salvata il refresh resta sulla pagina; un indirizzo riservato inesistente mostra la pagina 404 dell'app (con `noindex`).

Un indirizzo pubblico sbagliato (es. `/it/pagina-sbagliata`) e un file mancante restano errori veri di S3. Le route dell'area riservata non devono iniziare con `/it` o `/en` né avere un punto nell'ultimo pezzo dell'indirizzo.

Una nuova pagina pubblica va aggiunta a `PUBLIC_ROUTES` sia qui sia nella funzione pubblicata; una nuova pagina dell'area riservata non richiede modifiche. Il refresh si verifica sull'ambiente DEV dopo aver pubblicato la funzione (in locale `ng serve` serve sempre `index.html`). La modifica della funzione non richiede invalidazione della cache.

## 4. Connessione a GitHub

Console **Strumenti per sviluppatori** → **Impostazioni** → **Connessioni** → **Crea connessione**:

- provider **GitHub**, installando l'app AWS Connector su GitHub con accesso al solo repository del progetto;
- la connessione deve risultare **Disponibile** prima di usarla nella pipeline.

## 5. Pipeline e progetto CodeBuild

Console **CodePipeline** → **Crea pipeline** → **Crea pipeline personalizzata**:

- tipo **V2**;
- ruolo di servizio: **Nuovo ruolo di servizio**.

### Stage Source

- provider **GitHub (tramite l'app GitHub)**, connessione del passo 4;
- repository del progetto, branch dell'ambiente (es. `dev`);
- formato dell'artefatto di output predefinito (`SourceArtifact`);
- **Trigger**: push sul branch dell'ambiente, con rilevamento delle modifiche attivo. Senza trigger la pipeline non parte al push.

### Stage Build

Aggiungere un **gruppo di azioni** con una sola azione:

- **Nome azione**: `Build`;
- **Provider azione**: **AWS CodeBuild**, non "Comandi";
- **Regione**: quella dell'ambiente;
- **Artefatti di input**: `SourceArtifact`;
- **Nome progetto**: **Crea progetto** (il progetto nasce già collegato alla pipeline, vedi sotto);
- **Variabili d'ambiente** dell'azione: nessuna, stanno nel progetto;
- **Tipo di compilazione**: **Compilazione singola**;
- **Artefatti di output**: nessuno.

Non aggiungere uno stage Deploy: il caricamento su S3 lo fa già la build.

### Progetto CodeBuild

**Configurazione del progetto**

- nome, es. `summer-booking-front-office-<ambiente>`;
- tipo **Progetto predefinito**;
- **Limita il numero di build simultanee**: attivo, valore **1**, così due deploy non si sovrappongono sullo stesso bucket.

**Ambiente**

- provisioning **On demand**, immagine **gestita**, calcolo **EC2**, modalità **Container**;
- sistema operativo **Amazon Linux**, runtime **Standard**, immagine più recente;
- versione immagine: **usa sempre l'immagine più recente per questa versione di runtime**. Il `buildspec.yml` installa poi con `n` la versione esatta di Node indicata in `.nvmrc`;
- ruolo di servizio: **Nuovo ruolo di servizio**;
- **Configurazione aggiuntiva** → **Variabili d'ambiente**, tipo **Testo normale**:
  - `BUILD_CONFIGURATION`: configurazione Angular dell'ambiente, `dev` per DEV (API mock attive) o `production` per PROD (mock spenti). Qualsiasi altro valore ferma la build prima di toccare il bucket;
  - `S3_BUCKET`: nome del bucket;
  - `CLOUDFRONT_DISTRIBUTION_ID`: ID della distribuzione.

  Non sono dati segreti. Eventuali secret futuri vanno in Secrets Manager, mai in chiaro.

**Specifiche di compilazione**

- **Usa un file di specifiche di compilazione**, percorso `frontend/summer-booking-front-office/buildspec.yml`.

**Log**

- CloudWatch Logs attivo, nome gruppo e flusso vuoti (predefiniti).

## 6. Permessi IAM

Ogni ruolo ha solo i permessi che gli servono, limitati alle risorse dell'ambiente.

### Ruolo della build (`codebuild-…-service-role`)

Oltre a **CodeBuildBasePolicy** e **CodeBuildAutoRetryPolicy**, create da AWS insieme al progetto, aggiungere una **policy inline** (es. `summer-booking-front-office-<ambiente>-deploy`):

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "s3:ListBucket",
      "Resource": "arn:aws:s3:::NOME_BUCKET"
    },
    {
      "Effect": "Allow",
      "Action": ["s3:PutObject", "s3:DeleteObject"],
      "Resource": "arn:aws:s3:::NOME_BUCKET/*"
    },
    {
      "Effect": "Allow",
      "Action": "cloudfront:CreateInvalidation",
      "Resource": "arn:aws:cloudfront::ID_ACCOUNT:distribution/ID_DISTRIBUZIONE"
    }
  ]
}
```

### Ruolo della pipeline (`AWSCodePipelineServiceRole-…`)

Oltre alla policy base e a quella di CodeConnections, create da AWS, aggiungere una **policy inline** (es. `summer-booking-front-office-<ambiente>-codebuild`) per avviare e seguire la build:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["codebuild:StartBuild", "codebuild:BatchGetBuilds", "codebuild:StopBuild"],
      "Resource": "arn:aws:codebuild:REGIONE:ID_ACCOUNT:project/NOME_PROGETTO"
    }
  ]
}
```

Il ruolo della pipeline **non** deve avere permessi su S3 del sito o su CloudFront: il deploy lo fa il ruolo della build.

### Se l'editor JSON di IAM restituisce un errore di Access Analyzer

Le regole dell'organizzazione (SCP) bloccano `access-analyzer:ValidatePolicy` in `us-east-1`, cioè il controllo automatico dell'editor. La policy si può comunque creare: ignorare l'avviso e proseguire, oppure usare l'editor visivo, oppure la AWS CLI:

```bash
aws iam put-role-policy --role-name NOME_RUOLO --policy-name NOME_POLICY --policy-document file://policy.json
```

## 7. Cosa fa il deploy (`buildspec.yml`)

1. **install**: installa la versione di Node indicata in `.nvmrc` ed esegue `npm ci`.
2. **pre_build**: verifica che `BUILD_CONFIGURATION` sia `dev` o `production` e che `S3_BUCKET` e `CLOUDFRONT_DISTRIBUTION_ID` siano impostate.
3. **build**: prima `npm run verify` (lint, controllo dei tipi dei test con `tsc -p tsconfig.spec.json --noEmit`, test unitari con `ng test --no-watch`): se una verifica fallisce la build non parte e nel passo successivo non si pubblica nulla, il sito online resta quello precedente (dal 07/10/2026, Q03 dell'audit esterno; prima si pubblicava anche con test rotti). Poi `ng build --configuration $BUILD_CONFIGURATION`, che controlla anche i tipi dell'app e dei template. Entrambe le configurazioni sono ottimizzate; `dev` usa `environment.dev.ts` e include le API mock (es. login di test), `production` usa `environment.prod.ts` e sostituisce `src/app/services/mocks/mock-interceptors.ts` con `mock-interceptors.none.ts` (`fileReplacements` in `angular.json`): il codice dei mock, account di test compreso, non entra nel bundle di produzione.
4. **post_build**: se la build è fallita si ferma senza toccare il bucket e il sito online resta quello precedente. Altrimenti carica i file con intestazioni `Cache-Control` diverse, che CloudFront inoltra al browser:
   - bundle con hash nel nome (`main-*.js`, `chunk-*.js`, `polyfills-*.js`, `styles-*.css`): `public, max-age=31536000, immutable`;
   - immagini e font in `assets/`: `public, max-age=86400`, perché mantengono lo stesso nome quando vengono sostituiti;
   - `index.html`, traduzioni in `assets/i18n/`, service worker, manifest e favicon: `no-cache`, caricati per ultimi in modo che la nuova `index.html` vada online solo quando i file che richiama sono già nel bucket;
   - un `aws s3 sync --delete` rimuove i file delle build precedenti, tranne i bundle con hash: una scheda ancora aperta sulla versione vecchia li chiede quando apre una pagina che non aveva ancora caricato;
   - un bundle che la nuova build non usa più riceve un file vuoto `retired/<nome del bundle>`: la sua data (`LastModified`) è il momento in cui il bundle è stato sostituito. La data del bundle stesso non basta, perché dice solo quando è stato caricato, magari giorni prima;
   - 24 ore dopo la sostituzione, al primo deploy successivo, il bundle viene cancellato insieme al suo `retired/…`; se una build torna a usare un bundle (es. un ritorno alla versione precedente), il suo `retired/…` viene tolto. I file in `retired/` sono vuoti e non contengono nulla dell'app; il `sync --delete` li esclude (dal 07/10/2026, R01 dell'audit esterno, durata scelta dall'utente; la prima versione contava le 24 ore dal caricamento ed è stata corretta su segnalazione del revisore);
   - infine invalida la cache di CloudFront (`/*`).

Il caricamento usa `aws s3 cp --recursive`, che riscrive sempre i metadati. `aws s3 sync` salterebbe i file invariati e lascerebbe le vecchie intestazioni.

Protezione limitata: se il caricamento si interrompe a metà (es. errore di rete), il bucket può restare con file misti fino al deploy successivo.

## 8. Verifica dopo il deploy

```bash
curl -sI https://DOMINIO/it/home
```

```bash
curl -sI https://DOMINIO/assets/i18n/it.json
```

```bash
curl -sI http://DOMINIO/
```

Risultati attesi:

- `/it/home` e `it.json`: `200` con `Cache-Control: no-cache`;
- `main-*.js`: `Cache-Control: public, max-age=31536000, immutable`;
- immagini in `assets/images/`: `Cache-Control: public, max-age=86400`;
- una pagina dell'area riservata (es. `/settings`): `200` con l'HTML dell'app (`index.html`), anche ricaricandola;
- un indirizzo pubblico inesistente (es. `/it/login`): `403`.
- la richiesta HTTP: `301` con `Location: https://DOMINIO/`;
- sulle risposte HTTPS:
  - `Strict-Transport-Security: max-age=31536000`;
  - `X-Content-Type-Options: nosniff`;
  - `X-Frame-Options: DENY`;
  - `Referrer-Policy: strict-origin-when-cross-origin`;
- `Content-Security-Policy` con il valore della sezione "Content Security Policy" (su DEV dal 07/10/2026); `X-XSS-Protection` assente.

Verificare le prestazioni con Lighthouse in una finestra in incognito: le estensioni del browser falsano i risultati.

## 9. Errori già incontrati

| Errore | Causa | Soluzione |
|---|---|---|
| La pipeline non parte al push | Nessun trigger sullo stage Source | Aggiungere il trigger sul push del branch |
| `not authorized to perform: codebuild:StartBuild` | Il ruolo della pipeline non può avviare la build | Policy del ruolo della pipeline (passo 6) |
| `test "$BUILD_CONFIGURATION" = …` fallisce | Variabile `BUILD_CONFIGURATION` mancante o diversa da `dev`/`production` | Aggiungerla nel progetto CodeBuild |
| `test -n "$S3_BUCKET"` fallisce | Variabili d'ambiente mancanti nel progetto CodeBuild | Progetto → Modifica → Ambiente → Configurazione aggiuntiva |
| `AccessDenied` su `s3:PutObject` | Policy di deploy mancante sul ruolo della build | Policy del ruolo della build (passo 6) |
| `access-analyzer:ValidatePolicy` … `service control policy` | SCP dell'organizzazione sull'editor IAM | Proseguire comunque, editor visivo o AWS CLI |
| "Avvia compilazione" da CodeBuild non funziona | Il progetto riceve il codice solo dalla pipeline | Usare **Rilascia modifica** in CodePipeline |
| Intestazioni di cache assenti | Upload fatto fuori dal `buildspec.yml` | Tutto il deploy deve passare dal progetto CodeBuild |

## 10. Nuovo ambiente (es. PROD)

Ripetere i passi da 1 a 6 con risorse separate: bucket, distribuzione, funzione CloudFront (stesso codice), pipeline sul branch dell'ambiente, progetto CodeBuild, ruoli e policy propri. Il `buildspec.yml` è lo stesso: cambiano solo le variabili `BUILD_CONFIGURATION` (`production`), `S3_BUCKET` e `CLOUDFRONT_DISTRIBUTION_ID` del progetto.

Per un dominio personalizzato il certificato ACM usato da CloudFront deve stare in `us-east-1`. Le regole dell'organizzazione limitano alcune azioni in quella regione: verificare con l'amministratore AWS prima di configurarlo.

## 11. Copia dei documenti su Nuclino

Il workflow GitHub Actions `.github/workflows/sync-readme-deploy-nuclino.yml`, chiamato **Sync docs to Nuclino**, legge le associazioni da `.github/nuclino-docs.json`. La fonte ufficiale resta ciascun file sul branch `dev`: le modifiche fatte direttamente negli item vengono sovrascritte alla sincronizzazione successiva, mentre i titoli degli item non cambiano.

L'elenco ha due liste abbinate **per posizione**: il primo file aggiorna il primo ID, il secondo file il secondo ID e così via. I percorsi partono dalla root del repository, non dalla cartella `.github`. La configurazione iniziale è:

```json
{
  "_comment": "Mantenere le liste con lo stesso numero gli elementi. Il riferimento tra file e item su nuclino è posizionale!! (quindi il primo file corrisponde al primo id, il secondo al secondo...)",
  "files": ["frontend/summer-booking-front-office/docs/README_DEPLOY.md"],
  "idsNuclino": ["aec3346f-8065-47f6-a699-a1f591067b43"]
}
```

Il campo `_comment` è un avviso per chi modifica l'elenco: JSON non ammette commenti e il workflow ignora questo campo.

Per aggiungere un documento, creare il suo item su Nuclino, aggiungere il percorso del Markdown in fondo a `files` e il relativo ID in fondo a `idsNuclino`, poi fare commit e push su `dev`. Non servono un altro secret o modifiche al workflow. Rimuovere un'associazione interrompe gli aggiornamenti di quell'item ma non lo cancella da Nuclino. Se si riordinano gli elenchi, spostare sempre insieme file e ID: un ID valido abbinato al file sbagliato non è riconoscibile automaticamente.

Configurazione e primo test:

1. In GitHub, nel repository, aprire **Settings → Secrets and variables → Actions** e creare il repository secret `NUCLINO_API_KEY` con la chiave API Nuclino. Non inserire mai la chiave nei file o nei log. La chiave ha i diritti dell'account Nuclino che l'ha creata, non quelli del token GitHub.
2. Pubblicare il workflow sul branch `dev` con commit e push: il primo caricamento del workflow avvia anche la prima sincronizzazione.
3. Aprire **Actions → Sync docs to Nuclino**, verificare che l'esecuzione sia riuscita e controllare il contenuto degli item Nuclino.

I push su `dev` avviano la sincronizzazione se cambia un file `.md`, l'elenco JSON o il workflow. Si aggiornano tutti e soltanto i documenti elencati; un Markdown non elencato non viene pubblicato. Le esecuzioni non si sovrappongono e leggono la versione più recente di `dev`.

Prima di inviare qualsiasi documento si controllano tutte le associazioni: liste di lunghezze diverse, ID non validi, duplicati, file mancanti, vuoti, non Markdown o esterni al repository bloccano l'intera sincronizzazione. Anche un secret mancante blocca l'invio. Gli aggiornamenti API sono sequenziali: se una chiamata fallisce, il workflow si ferma, ma gli item già aggiornati restano aggiornati; il successivo push che attiva il workflow risincronizza l'elenco. La chiave e il contenuto delle risposte non vengono stampati nei log.

Il workflow non installa dipendenze dell'applicazione, non esegue build e non accede ad AWS. Non modifica `buildspec.yml` né CodePipeline; gli eventuali deploy avviati dallo stesso push dipendono dai trigger già configurati in AWS.

Riferimenti: [aggiornamento di un item Nuclino](https://help.nuclino.com/fa38d15f-items-and-collections), [autenticazione e diritti della chiave](https://help.nuclino.com/8090bb76-authentication), [repository secrets GitHub](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets).

## Deploy manuale (solo emergenze)

Dalla cartella `frontend/summer-booking-front-office`, con credenziali AWS autorizzate:

```bash
npm ci
```

```bash
npm run build:prod
```

Per DEV usare `npm run build:dev-env` (configurazione `dev`, API mock attive). Poi eseguire gli stessi comandi di caricamento e invalidazione del `post_build` in `buildspec.yml`, sostituendo `$DIST`, `$S3_BUCKET` e `$CLOUDFRONT_DISTRIBUTION_ID`. Un semplice `aws s3 sync` non imposta le intestazioni di cache. Verificare sempre il nome del bucket prima di eseguire comandi con `--delete`.
