# Deploy Summer Booking su S3 e CloudFront

Questa guida descrive come creare da zero, su AWS, tutto quello che serve per pubblicare il frontend e come funziona il deploy automatico. La stessa procedura vale per DEV, PROD e altri ambienti: cambiano solo nomi, valori e ruoli, che devono essere separati per ogni ambiente.

Credenziali, chiavi AWS e secret non devono mai essere salvati nel repository o nei file di ambiente Angular.

## Architettura

```text
push su GitHub (branch dell'ambiente)
  → CodePipeline: stage Source (CodeConnections)
  → CodePipeline: stage Build (azione AWS CodeBuild)
      → CodeBuild esegue buildspec.yml: npm ci, build dell'ambiente (dev o production),
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
- **Criterio di cache**: **CachingOptimized**, che rispetta le intestazioni `Cache-Control` impostate dal deploy;
- **Compressione automatica degli oggetti**: attiva;
- **Oggetto root predefinito**: `index.html`;
- **Firewall (WAF)**: secondo le esigenze dell'ambiente;
- **Risposte di errore personalizzate**: **nessuna** (vedi passo 3).

Poiché CloudFront non ha il permesso `s3:ListBucket`, un file inesistente restituisce **403** e non 404. È voluto: gli errori reali restano visibili.

## 3. CloudFront Function per le route dell'app

Non configurare un fallback globale che trasformi ogni 403 o 404 in `/index.html`: nasconderebbe errori reali di asset, permessi o route inesistenti, e produrrebbe "soft 404" per i motori di ricerca.

Le sole route raggiungibili da URL diretto vengono riscritte verso `/index.html` da una funzione:

1. Console **CloudFront** → **Funzioni** → **Crea funzione**, runtime più recente proposto.
2. Scheda **Sviluppo**: incollare il codice seguente e **Salva modifiche**.
3. Scheda **Pubblica**: **Pubblica funzione**. La distribuzione usa solo la versione pubblicata.
4. **Distribuzioni** → distribuzione dell'ambiente → **Comportamenti** → `Default (*)` → **Modifica** → **Associazioni di funzioni** → **Richiesta visualizzatore**: tipo **CloudFront Functions**, selezionare la funzione.

```js
// Route SPA servite da index.html. Aggiornare questo elenco quando si aggiunge una route raggiungibile da URL diretto.
var SPA_ROUTES = {
    '/': true,
    '/home': true,
    '/it': true,
    '/it/home': true,
    '/en': true,
    '/en/home': true,
    '/beachmap': true
};

function handler(event) {
    var request = event.request;

    if (SPA_ROUTES[request.uri] === true) {
        request.uri = '/index.html';
    }

    return request;
}
```

Route servite:

- `/` e `/home`, che reindirizzano alla home nella lingua preferita;
- `/it`, `/it/home`, `/en`, `/en/home`, le pagine pubbliche localizzate;
- `/beachmap`, area privata: senza sessione l'app rimanda alla home, con una sessione salvata il refresh resta sulla pagina.

Ogni nuova route pubblica dell'app va aggiunta sia qui sia nella funzione pubblicata. La modifica della funzione non richiede invalidazione della cache.

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
3. **build**: `ng build --configuration $BUILD_CONFIGURATION`. Entrambe le configurazioni sono ottimizzate; `dev` usa `environment.dev.ts` (API mock attive, es. login di test), `production` usa `environment.prod.ts` (mock spenti).
4. **post_build**: se la build è fallita si ferma senza toccare il bucket e il sito online resta quello precedente. Altrimenti carica i file con intestazioni `Cache-Control` diverse, che CloudFront inoltra al browser:
   - bundle con hash nel nome (`main-*.js`, `chunk-*.js`, `polyfills-*.js`, `styles-*.css`): `public, max-age=31536000, immutable`;
   - immagini e font in `assets/`: `public, max-age=86400`, perché mantengono lo stesso nome quando vengono sostituiti;
   - `index.html`, traduzioni in `assets/i18n/`, service worker, manifest e favicon: `no-cache`, caricati per ultimi in modo che la nuova `index.html` vada online solo quando i file che richiama sono già nel bucket;
   - un `aws s3 sync --delete` finale rimuove i file delle build precedenti;
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

Risultati attesi:

- `/it/home` e `it.json`: `200` con `Cache-Control: no-cache`;
- `main-*.js`: `Cache-Control: public, max-age=31536000, immutable`;
- immagini in `assets/images/`: `Cache-Control: public, max-age=86400`;
- una route non prevista (es. `/login`): `403`.

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

## Deploy manuale (solo emergenze)

Dalla cartella `frontend/summer-booking-front-office`, con credenziali AWS autorizzate:

```bash
npm ci
```

```bash
npm run build:prod
```

Per DEV usare `npm run build:dev-env` (configurazione `dev`, API mock attive). Poi eseguire gli stessi comandi di caricamento e invalidazione del `post_build` in `buildspec.yml`, sostituendo `$DIST`, `$S3_BUCKET` e `$CLOUDFRONT_DISTRIBUTION_ID`. Un semplice `aws s3 sync` non imposta le intestazioni di cache. Verificare sempre il nome del bucket prima di eseguire comandi con `--delete`.
