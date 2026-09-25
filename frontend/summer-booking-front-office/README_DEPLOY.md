# Deploy Summer Booking su S3 e CloudFront

Questa procedura pubblica la build statica del frontend in un ambiente AWS. Lo stesso flusso si applica a DEV, PROD e ad altri ambienti usando risorse e variabili diverse.

## Prerequisiti

- Node.js 22.22.3 o una versione compatibile indicata da Angular 22.
- Accesso al bucket S3 dell'ambiente.
- Accesso alla distribuzione CloudFront dell'ambiente.
- AWS CLI disponibile nell'ambiente di build tramite ruolo IAM o credenziali protette della CI/CD.
- Bucket S3 e distribuzione CloudFront già creati.

Credenziali, chiavi AWS e secret non devono essere salvati nel repository o nei file di ambiente Angular.

## Build Angular

Dalla cartella `frontend/summer-booking-front-office` eseguire:

```bash
npm ci
npm run build:prod
```

La cartella da pubblicare è:

```text
dist/summer-booking-front-office/browser/
```

Prima del caricamento verificare che contenga almeno `index.html`, i bundle JavaScript, i fogli di stile, `ngsw.json` e `ngsw-worker.js`.

## Pubblicazione in un altro bucket

Impostare il nome del bucket dell'ambiente e sincronizzare esclusivamente la cartella `browser`:

```bash
aws s3 sync dist/summer-booking-front-office/browser/ s3://NOME_BUCKET --delete
```

Il flag `--delete` rimuove dal bucket i file di una build precedente che non esistono più. Verificare sempre il nome del bucket prima di eseguire il comando.

La pipeline (`buildspec.yml`) carica i file con intestazioni `Cache-Control` diverse, perché CloudFront le inoltra al browser:

- bundle con hash nel nome (`main-*.js`, `chunk-*.js`, `polyfills-*.js`, `styles-*.css`): `public, max-age=31536000, immutable`;
- immagini e font in `assets/`: `public, max-age=86400`, perché mantengono lo stesso nome quando vengono sostituiti;
- `index.html`, traduzioni in `assets/i18n/`, file del service worker, manifest e favicon: `no-cache`.

Il caricamento usa `aws s3 cp --recursive`, che riscrive sempre i metadati; un `aws s3 sync --delete` finale rimuove solo i file delle build precedenti. Per un caricamento manuale ripetere gli stessi comandi del `buildspec.yml`.

## Invalidazione CloudFront

Dopo il caricamento creare un'invalidazione per la distribuzione dell'ambiente:

```bash
aws cloudfront create-invalidation \
  --distribution-id ID_DISTRIBUZIONE \
  --paths "/*"
```

Per il routing SPA non configurare un fallback globale che trasformi ogni risposta 403 o 404 in `/index.html`: nasconderebbe errori reali di asset, permessi o route inesistenti.

Per le sole route previste come ingressi diretti, associare al comportamento predefinito una CloudFront Function sull'evento **Viewer request** che riscriva esclusivamente queste URL verso `/index.html`:

- `/` e `/home`, che reindirizzano alla home nella lingua preferita;
- `/it`, `/it/home`, `/en`, `/en/home`, le pagine pubbliche localizzate.

Qualsiasi altra risposta 403 o 404 deve rimanere visibile e mantenere il proprio status. Codice della funzione:

```js
// Route SPA servite da index.html. Aggiornare questo elenco quando si aggiunge una route raggiungibile da URL diretto.
var SPA_ROUTES = {
    '/': true,
    '/home': true,
    '/it': true,
    '/it/home': true,
    '/en': true,
    '/en/home': true
};

function handler(event) {
    var request = event.request;

    if (SPA_ROUTES[request.uri] === true) {
        request.uri = '/index.html';
    }

    return request;
}
```

Dopo aver modificato la funzione, pubblicarla dalla scheda **Publish** della console CloudFront: la versione in sviluppo non viene usata dalla distribuzione. La modifica della funzione non richiede invalidazione della cache.

## Valori diversi tra DEV e PROD

Ogni ambiente usa valori propri per:

- bucket S3;
- distribuzione CloudFront;
- dominio e certificato TLS;
- URL del backend;
- flag funzionali;
- ruolo IAM della pipeline.

La build production sostituisce `src/environments/environment.ts` con `src/environments/environment.prod.ts`. Il file production disattiva i mock. Prima di collegare il backend reale, aggiornare `apiBaseUrl` senza inserire secret.

## CI CD con CodePipeline e CodeBuild

Tutte le istruzioni di build e deploy stanno nel file versionato `buildspec.yml`: installazione delle dipendenze, build production, caricamento su S3 con le intestazioni di cache e invalidazione CloudFront. La pipeline AWS non deve contenere comandi propri: si limita a prendere il codice e ad avviare CodeBuild.

La pipeline ha due stage:

1. **Source**: GitHub tramite CodeConnections, branch dell'ambiente (es. `dev`), con trigger sul push.
2. **Build**: azione di tipo **AWS CodeBuild** (non "Comandi") collegata al progetto CodeBuild dell'ambiente, con artefatto di input `SourceArtifact`.

Configurare il progetto CodeBuild con:

- origine: AWS CodePipeline;
- ambiente: immagine gestita Amazon Linux standard più recente, compatibile con `nodejs: 22` (il `buildspec.yml` installa poi con `n` la versione esatta indicata in `.nvmrc`);
- buildspec: "Usa un file buildspec", percorso `frontend/summer-booking-front-office/buildspec.yml`;
- variabile `S3_BUCKET` con il bucket dell'ambiente;
- variabile `CLOUDFRONT_DISTRIBUTION_ID` con la distribuzione dell'ambiente;
- ruolo IAM autorizzato solo su bucket e distribuzione dell'ambiente: `s3:ListBucket` sul bucket, `s3:PutObject` e `s3:DeleteObject` sui suoi oggetti, `cloudfront:CreateInvalidation` sulla distribuzione, oltre ai permessi di log di CodeBuild.

Se la build fallisce, il `buildspec.yml` interrompe la pubblicazione prima di toccare il bucket.

Per PROD creare valori e ruoli separati da DEV. Le variabili sensibili devono essere archiviate nei servizi AWS dedicati o nelle impostazioni protette della pipeline, mai nel repository.
