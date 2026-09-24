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
- `/it`, `/it/home`, `/en`, `/en/home`, le pagine pubbliche localizzate;
- `/login`.

Qualsiasi altra risposta 403 o 404 deve rimanere visibile e mantenere il proprio status. Codice della funzione:

```js
// Route SPA servite da index.html. Aggiornare questo elenco quando si aggiunge una route raggiungibile da URL diretto.
var SPA_ROUTES = {
    '/': true,
    '/login': true,
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

Il file `buildspec.yml` esegue `npm ci`, la build production, il sync S3 e l'invalidazione CloudFront. Configurare il progetto CodeBuild con:

- percorso buildspec `frontend/summer-booking-front-office/buildspec.yml`;
- variabile `S3_BUCKET` con il bucket dell'ambiente;
- variabile `CLOUDFRONT_DISTRIBUTION_ID` con la distribuzione dell'ambiente;
- ruolo IAM autorizzato a scrivere solo nel bucket e a invalidare solo la distribuzione previsti.

Per PROD creare valori e ruoli separati da DEV. Le variabili sensibili devono essere archiviate nei servizi AWS dedicati o nelle impostazioni protette della pipeline, mai nel repository.
