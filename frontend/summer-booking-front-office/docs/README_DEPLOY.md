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

La build non mette più script né stili inline in `index.html` (`optimization.styles.inlineCritical: false` in `angular.json`). La policy consente gli script del nostro dominio e dei soli due host necessari al caricamento di Google Maps, senza `'unsafe-inline'` o `'unsafe-eval'` negli script. Valore completo dell'header `Content-Security-Policy`, su una riga (aggiornato il 22/10/2026):

```text
default-src 'self'; script-src 'self' https://maps.googleapis.com https://maps.gstatic.com; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; img-src 'self' data: https://*.googleapis.com https://*.gstatic.com https://*.google.com https://*.googleusercontent.com; font-src 'self' https://fonts.gstatic.com; connect-src 'self' https://maps.googleapis.com https://mapsresources-pa.googleapis.com https://maps.gstatic.com https://fonts.googleapis.com https://fonts.gstatic.com; worker-src 'self' blob:; manifest-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; upgrade-insecure-requests
```

I domini di Google servono alla mappa della tab Media di Informazioni stabilimento (Maps JavaScript API, dal 21/10/2026). La configurazione è basata sulla [guida CSP di Google Maps](https://developers.google.com/maps/documentation/javascript/content-security-policy) e sull'[elenco dei domini Maps](https://developers.google.com/maps/domains), limitando gli script a `maps.googleapis.com` e `maps.gstatic.com` e le connessioni ai cinque host espliciti sopra. I caratteri jolly restano solo nelle origini delle immagini; `blob:` è consentito solo ai worker, non agli script. Questa allowlist non equivale a una CSP strict con nonce: eventuali nuovi domini o richieste di `eval` vanno valutati prima di aggiungere eccezioni.

`fonts.googleapis.com` deve essere in `style-src` e `connect-src`, `fonts.gstatic.com` in `font-src` e `connect-src`: il service worker Angular inoltra le richieste tramite `fetch()`, che è controllato da `connect-src` anche quando la risorsa richiesta è un foglio CSS o un font. Autorizzare solo stili e font non basta quando interviene il worker. Riferimento: [MDN, direttiva connect-src](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/connect-src).

Stato: CSP attiva su DEV dal 07/10/2026, estesa a Maps il 22/10/2026 nella policy `summerbooking-dev-frontoffice-security-headers`. L'utente ha confermato il funzionamento dopo gli adeguamenti; richieste `HEAD` del 22/10/2026 hanno verificato il valore finale sia sulla pagina `/it/home` sia su `/ngsw-worker.js` del dominio CloudFront DEV. Su PROD resta da configurare e verificare.

Si attiva in due fasi, prima su DEV e poi su PROD:

1. **Report-Only (solo segnalazioni)**. CloudFront non ha questa variante tra i *Security headers*: nella stessa policy aprire **Custom headers → Add header**, nome `Content-Security-Policy-Report-Only`, valore la policy qui sopra, **Origin override** attivo. Questo header segnala le violazioni (`[Report Only] Refused to …`) senza bloccare, ma **non allenta una CSP attiva**: se esiste già una `Content-Security-Policy` più restrittiva, questa continua a bloccare le risorse. L'assenza di nuove segnalazioni non dimostra quindi che un'integrazione già bloccata funzioni; la prova completa delle eccezioni va fatta su DEV prima del passaggio a PROD, senza disabilitare indiscriminatamente la protezione.
2. **Verifica**. Con la console del browser aperta, aprire ogni pagina in IT e in EN, anche dopo il login: home, popup di accesso, gestionale, magazzino con i suoi popup, datepicker, impostazioni (anche la tab Media di Informazioni stabilimento, con la mappa di Google), aggiornamento del service worker. Nessun messaggio `Refused` deve comparire. Ripetere nei giorni successivi durante l'uso normale.
3. **Attivazione**. Togliere il custom header `Content-Security-Policy-Report-Only` e, nei **Security headers**, attivare `Content-Security-Policy` con lo stesso valore e **Origin override** attivo. Da qui il browser blocca davvero ciò che la policy non permette.
4. **Controllo**. `curl -I https://<dominio>/` deve mostrare `content-security-policy` con il valore atteso (e non più `content-security-policy-report-only`).

La policy permette il nostro dominio e le origini Google elencate: un servizio esterno futuro (API su un altro dominio, pagamenti, analytics, altri font) va aggiunto alla direttiva giusta (es. `connect-src`) prima di usarlo, solo con i domini indicati dalla sua documentazione, ripassando dalla fase Report-Only e verificando anche il service worker. Non aggiungere `*`, `https:` o `'unsafe-eval'` come soluzione generica a un blocco. Il valore della CSP in CloudFront non può superare 1783 caratteri. Decisioni e punti ancora aperti (Trusted Types, raccolta dei report) sono in `SECURITY_TODO.md`.

Per modificare una policy esistente: **CloudFront → Policies → Response headers → summerbooking-dev-frontoffice-security-headers → Edit → Security headers → Content-Security-Policy**. Sostituire il valore completo, mantenere **Origin override** attivo e salvare. È una modifica CloudFront: non richiede commit o nuova build; se la policy è già associata al comportamento corretto, non richiede invalidazione della cache.

#### Verifica della CSP con il service worker

Controllare gli header sia di `/it/home` sia di `/ngsw-worker.js` con `curl -I https://DOMINIO/PERCORSO`: CloudFront applica la response headers policy anche alle risposte dalla cache. Una scheda o un service worker già attivi possono però mantenere la vecchia policy. Se la console cita ancora il vecchio valore, confrontarlo con gli header restituiti dalla rete e provare in una sessione incognito nuova, chiudendo prima quella precedente.

Per isolare il problema in Chrome: **F12 → Application → Service Workers → Bypass for network**, poi ricaricare con gli strumenti aperti. È solo una prova diagnostica: togliere la spunta e verificare il normale funzionamento con il worker prima di considerare concluso il test. Non richiede modifiche al codice né alle protezioni CloudFront. Riferimenti: [Chrome, debug dei service worker](https://developer.chrome.com/docs/devtools/progressive-web-apps) e [Angular, versioni e cache del service worker](https://angular.dev/ecosystem/service-workers/devops).

Se un errore CSP è seguito da `504 Gateway Timeout` in `ngsw-worker.js`, non concludere che Google sia in timeout: il metodo `safeFetch` del worker Angular restituisce 504 anche quando `fetch` fallisce per il blocco CSP. Correggere la direttiva indicata nel primo errore. Un eventuale 403 va invece ricondotto al suo URL effettivo prima di attribuirgli la stessa causa. Nei log condivisi oscurare sempre i valori delle chiavi negli URL.

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

Le pagine dell'app aperte da URL diretto o ricaricate vengono riscritte verso `/index.html` da una funzione: le pagine pubbliche di un elenco esplicito e tutte le pagine dell'area riservata (ogni indirizzo senza estensione fuori dalle lingue `/it`, `/en`, `/fr`, `/es`, `/de`, `/zh`, `/ar`). I file (con estensione) restano serviti così come sono da S3, anche quando mancano:

1. Console **CloudFront** → **Funzioni** → **Crea funzione**, runtime più recente proposto.
2. Scheda **Sviluppo**: incollare il codice seguente e **Salva modifiche**.
3. Scheda **Pubblica**: **Pubblica funzione**. La distribuzione usa solo la versione pubblicata.
4. **Distribuzioni** → distribuzione dell'ambiente → **Comportamenti** → `Default (*)` → **Modifica** → **Associazioni di funzioni** → **Richiesta visualizzatore**: tipo **CloudFront Functions**, selezionare la funzione.

```js
// Pagine pubbliche servite da index.html: solo queste, così un indirizzo pubblico sbagliato resta un 404 vero.
// Aggiornare questo elenco quando si aggiunge una pagina pubblica (/it/..., /en/...) o una lingua.
var PUBLIC_ROUTES = {
    '/': true,
    '/home': true,
    '/it': true,
    '/it/home': true,
    '/en': true,
    '/en/home': true,
    '/fr': true,
    '/fr/home': true,
    '/es': true,
    '/es/home': true,
    '/de': true,
    '/de/home': true,
    '/zh': true,
    '/zh/home': true,
    '/ar': true,
    '/ar/home': true
};
// Le lingue dell'app (enum Language): i loro indirizzi sono pagine pubbliche.
var LANGUAGES = ['it', 'en', 'fr', 'es', 'de', 'zh', 'ar'];

function handler(event) {
    var request = event.request;
    var uri = request.uri;
    // Un file ha un punto nell'ultimo pezzo dell'indirizzo (main.js, logo.svg): lo serve S3 così com'è.
    var isFile = uri.substring(uri.lastIndexOf('/') + 1).indexOf('.') !== -1;
    var isPublic = LANGUAGES.some(function (language) {
        return uri === '/' + language || uri.indexOf('/' + language + '/') === 0;
    });

    // Pagine pubbliche in elenco e tutte le pagine dell'area riservata (es. /warehouse, /settings/sharing).
    if (PUBLIC_ROUTES[uri] === true || (!isFile && !isPublic)) {
        request.uri = '/index.html';
    }

    return request;
}
```

Route servite:

- `/` e `/home`, che reindirizzano alla home nella lingua preferita;
- `/it`, `/it/home`, `/en`, `/en/home`, `/fr`, `/fr/home`, `/es`, `/es/home`, `/de`, `/de/home`, `/zh`, `/zh/home`, `/ar`, `/ar/home`, le pagine pubbliche localizzate;
- tutte le pagine dell'area riservata (`/beachmap`, `/warehouse`, `/settings`, `/settings/…` e quelle future), senza elencarle: senza sessione l'app rimanda alla home, con una sessione salvata il refresh resta sulla pagina; un indirizzo riservato inesistente mostra la pagina 404 dell'app (con `noindex`).

Un indirizzo pubblico sbagliato (es. `/it/pagina-sbagliata`) e un file mancante restano errori veri di S3. Le route dell'area riservata non devono iniziare con il codice di una lingua (`/it`, `/en`, `/fr`, `/es`, `/de`, `/zh`, `/ar`) né avere un punto nell'ultimo pezzo dell'indirizzo.

Una nuova pagina pubblica va aggiunta a `PUBLIC_ROUTES` sia qui sia nella funzione pubblicata; una nuova lingua va aggiunta a `LANGUAGES` e a `PUBLIC_ROUTES` (francese, spagnolo e tedesco aggiunti il 08/10/2026, cinese semplificato e arabo il 09/10/2026: la funzione pubblicata va aggiornata con questo codice); una nuova pagina dell'area riservata non richiede modifiche. Il refresh si verifica sull'ambiente DEV dopo aver pubblicato la funzione (in locale `ng serve` serve sempre `index.html`). La modifica della funzione non richiede invalidazione della cache.

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

  Queste tre variabili non sono dati segreti. La chiave Maps va invece configurata come descritto sotto.

### Chiave Google Maps tramite Secrets Manager

Dal 22/10/2026 la chiave non è più scritta nei file `environment`: CodeBuild la legge da Secrets Manager e la passa al compilatore Angular attraverso `--define`, senza generare file sorgenti con il valore. Non viene stampata dallo script di build. **Rimane visibile nel bundle e nelle richieste del browser**: Secrets Manager evita di versionarla, non la rende segreta sul client. In Google Cloud sono obbligatorie entrambe le restrizioni: siti web autorizzati e sola **Maps JavaScript API**; usare chiavi separate per ambiente, quote e avvisi di budget.

1. Nella regione del progetto CodeBuild creare un segreto di tipo **Other type of secret**, con proprietà JSON `GOOGLE_MAPS_API_KEY_DEV` e valore della nuova chiave; cifratura predefinita `aws/secretsmanager`. Nome DEV: `summerbooking/dev/frontend/google-maps`.
2. Nel progetto CodeBuild aggiungere una variabile: nome **`GOOGLE_MAPS_API_KEY_DEV`**, tipo **Secrets Manager**, valore **`summerbooking/dev/frontend/google-maps:GOOGLE_MAPS_API_KEY_DEV`**. Per un altro ambiente cambiare il nome del segreto. Non usare tipo Testo normale e non incollare il valore nel buildspec.
3. Al ruolo di servizio **della build**, senza rimuovere i permessi di deploy, aggiungere una policy inline (DEV: `summerbooking-dev-read-google-maps`):

   ```json
   {
     "Version": "2012-10-17",
     "Statement": [
       {
         "Effect": "Allow",
         "Action": "secretsmanager:GetSecretValue",
         "Resource": "ARN_COMPLETO_DEL_SOLO_SEGRETO_DELL_AMBIENTE"
       }
     ]
   }
   ```

   L'ARN completo include il suffisso generato da AWS. Con una chiave KMS gestita dal cliente servono anche i relativi permessi di decrittazione; non sono necessari con la chiave predefinita `aws/secretsmanager`.
4. Pubblicare le modifiche al codice tramite il flusso Git abituale. La build fallisce senza pubblicare se la chiave è assente o malformata. Per PROD configurare un segreto e una chiave propri; impostare anche il Map ID di produzione in `environment.prod.ts`.
5. Dopo il deploy verificare la mappa nella tab Media di Informazioni stabilimento, gli errori nella console Google/browser e la CSP. Solo dopo aver verificato che nessun altro servizio la usi, eliminare la vecchia chiave in Google Cloud. Togliere la stringa dal codice non la revoca: rimane nella cronologia Git e nei bundle precedenti.

In DEV l'unico sito autorizzato per la nuova chiave è `https://d3nn1cfmv1ald2.cloudfront.net/*`. Le normali build senza lo script CodeBuild non ricevono una chiave: l'app funziona ma mostra il messaggio di mappa non disponibile, senza contattare Google. Non aggiungere la chiave a file versionati per aggirare questa scelta.

Riferimenti: [CodeBuild e valori da Secrets Manager](https://docs.aws.amazon.com/codebuild/latest/userguide/build-spec-ref.html#build-spec.env.secrets-manager), [permessi sul singolo segreto](https://docs.aws.amazon.com/secretsmanager/latest/userguide/auth-and-access_iam-policies.html), [Angular: sostituzioni di valori in build](https://angular.dev/tools/cli/build-system-migration#build-time-value-replacement-with-define), [Google Maps: protezione delle chiavi](https://developers.google.com/maps/api-security-best-practices).

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
2. **pre_build**: verifica che `BUILD_CONFIGURATION` sia `dev` o `production` e che `S3_BUCKET`, `CLOUDFRONT_DISTRIBUTION_ID` e `GOOGLE_MAPS_API_KEY_DEV` siano impostate.
3. **build**: prima `npm run verify` (lint, controllo dei tipi dei test con `tsc -p tsconfig.spec.json --noEmit`, test unitari con `ng test --no-watch`): se una verifica fallisce la build non parte e nel passo successivo non si pubblica nulla, il sito online resta quello precedente (dal 07/10/2026, Q03 dell'audit esterno; prima si pubblicava anche con test rotti). Poi `npm run build:codebuild` controlla la chiave e avvia Angular con la configurazione scelta e il valore Maps definito in build, controllando anche i tipi dell'app e dei template. Entrambe le configurazioni sono ottimizzate; `dev` usa `environment.dev.ts` e include le API mock (es. login di test), `production` usa `environment.prod.ts` e sostituisce `src/app/services/mocks/mock-interceptors.ts` con `mock-interceptors.none.ts` (`fileReplacements` in `angular.json`): il codice dei mock, account di test compreso, non entra nel bundle di produzione.
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
- `Content-Security-Policy` con il valore della sezione "Content Security Policy" (su DEV dal 07/10/2026, estesa a Maps e ai font Google il 22/10/2026), verificata anche su `/ngsw-worker.js`; `X-XSS-Protection` assente.

Aprire la tab Media di Informazioni stabilimento con il service worker attivo (nessun bypass): verificare mappa, immagini, font e assenza di violazioni CSP. Ripetere in una sessione nuova per distinguere gli header correnti dalle vecchie risposte conservate nel browser.

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

I push su `dev` avviano la sincronizzazione se cambia un file `.md`, l'elenco JSON o il workflow. Si aggiornano tutti e soltanto i documenti elencati e quelli delle cartelle rispecchiate (vedi sotto); un Markdown fuori da entrambi non viene pubblicato. Le esecuzioni non si sovrappongono e leggono la versione più recente di `dev`.

Prima di inviare qualsiasi documento si controllano tutte le associazioni: liste di lunghezze diverse, ID non validi, duplicati, file mancanti, vuoti, non Markdown o esterni al repository bloccano l'intera sincronizzazione. Anche un secret mancante blocca l'invio. Gli aggiornamenti API sono sequenziali: se una chiamata fallisce, il workflow si ferma, ma gli item già aggiornati restano aggiornati; il successivo push che attiva il workflow risincronizza l'elenco. La chiave e il contenuto delle risposte non vengono stampati nei log.

Il workflow non installa dipendenze dell'applicazione, non esegue build e non accede ad AWS. Non modifica `buildspec.yml` né CodePipeline; gli eventuali deploy avviati dallo stesso push dipendono dai trigger già configurati in AWS.

### Cartelle rispecchiate

Per le cartelle con molti documenti, come `shared documentation/backend`, non si abbinano i file uno per uno: la lista `mirrors` di `.github/nuclino-docs.json` indica la cartella, la collection Nuclino che la ospita e le sottocartelle da escludere (percorsi relativi alla cartella):

```json
"mirrors": [
  {
    "path": "shared documentation/backend",
    "collectionId": "<ID della collection Nuclino>",
    "exclude": ["analisi-pre/db/old", "analisi-pre/db-no-copied"]
  }
]
```

Dopo il passo dei file elencati, lo script `.github/scripts/nuclino-mirror.js` ricrea sotto la collection l'albero della cartella: ogni sottocartella diventa una collection e ogni `.md` un item con il nome del file senza estensione. Ciò che manca viene creato, quindi un documento nuovo compare su Nuclino al primo push senza toccare il JSON. Nulla viene cancellato: un file rimosso o rinominato lascia il vecchio item, da archiviare a mano. Gli item si riconoscono dal titolo dentro la collection: un item rinominato su Nuclino viene ricreato con il nome originale.

I link relativi vengono riscritti: verso un documento rispecchiato diventano link all'item Nuclino, verso un altro file del repository diventano link a GitHub sul branch `dev`, verso cartelle che non sono nel repository (sorgenti legacy, utility locali) restano solo come testo. In testa a ogni item c'è il rimando al file d'origine.

Per non riscrivere centinaia di item a ogni push, lo script aggiorna solo i documenti cambiati dall'ultima esecuzione riuscita del workflow, più quelli appena creati. Riscrive tutto quando cambia qualcosa in `.github/`, quando non c'è un'esecuzione riuscita precedente o quando si avvia a mano **Actions → Sync docs to Nuclino → Run workflow** con l'opzione *full*. L'API concede 150 richieste al minuto: la prima sincronizzazione della cartella backend (circa 380 creazioni e 350 aggiornamenti) dura qualche minuto. Se un documento non viene accettato, gli altri proseguono e il workflow termina con l'elenco degli errori.

Per attivare uno specchio: creare la collection su Nuclino, copiarne l'ID dall'URL in `collectionId` e fare push su `dev`. Con `collectionId` vuoto lo specchio viene saltato con un avviso. In locale lo script si prova senza chiave con `NUCLINO_DRY_RUN=1 node .github/scripts/nuclino-mirror.js` (serve un `collectionId` qualsiasi purché valido): stampa cosa creerebbe, senza chiamare Nuclino.

Riferimenti: [aggiornamento di un item Nuclino](https://help.nuclino.com/fa38d15f-items-and-collections), [autenticazione e diritti della chiave](https://help.nuclino.com/8090bb76-authentication), [repository secrets GitHub](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets).

## Deploy manuale (solo emergenze)

Dalla cartella `frontend/summer-booking-front-office`, con credenziali AWS autorizzate:

```bash
npm ci
```

```bash
npm run verify
npm run build:codebuild
```

Prima della build impostare nell'ambiente del processo `BUILD_CONFIGURATION` (`dev` o `production`) e `GOOGLE_MAPS_API_KEY_DEV`, recuperata dal segreto con strumenti autorizzati, senza stamparla né salvarla in file versionati o nella cronologia dei comandi. I comandi `build:prod` e `build:dev-env` da soli non iniettano la chiave Maps. Poi eseguire gli stessi comandi di caricamento e invalidazione del `post_build` in `buildspec.yml`, sostituendo `$DIST`, `$S3_BUCKET` e `$CLOUDFRONT_DISTRIBUTION_ID`. Un semplice `aws s3 sync` non imposta le intestazioni di cache. Verificare sempre il nome del bucket prima di eseguire comandi con `--delete`.
