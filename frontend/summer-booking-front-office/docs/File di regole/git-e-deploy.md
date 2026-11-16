# Git e deploy

Git e `.gitignore`, documentazione di deploy, PWA e service worker, deploy su S3 e CloudFront, regola SPA.

## Git e `.gitignore`

- Si versionano sorgenti, configurazioni e asset necessari alla build e alla CI/CD.
- `.editorconfig`, `.nvmrc`, `.prettierrc`, `buildspec.yml`, `ngsw-config.json`, `tsconfig*.json` e
  `manifest.webmanifest` sono configurazioni necessarie: non si ignorano.
- Si ignorano `node_modules`, `dist`, cache Angular/Sass, coverage, report, log, file IDE, file temporanei,
  credenziali, chiavi e file locali sensibili.
- Non ignorare per errore traduzioni, font, immagini dell'applicazione o configurazioni richieste dal deploy.
- `.gitignore` si aggiorna quando arrivano nuovi strumenti o artefatti locali.
- Prima di ogni commit: solo modifiche pertinenti, nessun file generato, locale o sensibile.

## Documentazione di deploy

- `docs/README_DEPLOY.md` contiene soltanto istruzioni di pubblicazione chiare, senza riferimenti locali.
- I riferimenti a `localhost` non compaiono nella documentazione di deploy né in quella destinata al repository
  pubblico.
- I file di regole fanno eccezione: servono al passaggio di contesto tra agenti e possono citare indirizzi locali.

## PWA e service worker

- `@angular/service-worker` è abilitato nelle build destinate al deploy e disabilitato nel normale sviluppo locale.
- `ngsw-config.json` è un file sorgente necessario e va versionato.
- La configurazione resta estendibile per la shell applicativa e per futuri casi offline, senza caching API complesso
  finché non viene richiesto.

## Deploy su S3 e CloudFront

- La build di produzione produce asset statici pubblicabili da `dist/summer-booking-front-office/browser/`.
- Con CloudFront e Origin Access Control (OAC) il bucket S3 resta privato. La bucket policy concede a CloudFront
  soltanto l'accesso necessario ed è limitata alla distribuzione corretta.
- Mai rendere pubblico il bucket per risolvere problemi di accesso.
- Un accesso diretto al dominio REST di S3 può restituire `AccessDenied`: con OAC è normale, il test pubblico si fa
  tramite CloudFront.
- Per diagnosticare un `AccessDenied` controllare origin e OAC, bucket policy, behavior, restrizioni geografiche e
  AWS WAF.
- Dopo ogni deploy: invalidazione CloudFront appropriata (normalmente `/*`, quando il flusso di rilascio la richiede) e
  attenzione al possibile aggiornamento controllato dal service worker.

## Regola SPA di CloudFront

- Mai un fallback globale che trasformi tutte le risposte 403 o 404 in `/index.html`: maschererebbe errori reali,
  asset mancanti, permessi errati e route inesistenti.
- Una CloudFront Function associata a **Viewer request** riscrive verso `/index.html`:
  - le pagine pubbliche di un elenco esplicito: `/`, `/home` e, per ogni lingua (`it`, `en`, `fr`, `es`, `de`, `zh`,
    `ar`), `/<lingua>` e `/<lingua>/home`;
  - tutte le pagine dell'area riservata, senza elencarle: ogni indirizzo senza estensione che non inizia con il codice
    di una lingua (es. `/warehouse`, `/settings`, `/settings/sharing`). Così il refresh di qualunque pagina riservata
    riapre quella pagina.
- Il codice aggiornato della funzione è in `docs/README_DEPLOY.md`.
- Un indirizzo pubblico sbagliato e un file mancante restano 404/403 veri (SEO: niente soft 404 sulle pagine
  pubbliche).
- Le route dell'area riservata non iniziano con il codice di una lingua (`/it`, `/en`, `/fr`, `/es`, `/de`, `/zh`,
  `/ar`) e non hanno un punto nell'ultimo pezzo dell'indirizzo.
- Una nuova pagina pubblica va aggiunta all'elenco della funzione, dopo conferma dell'utente; una nuova pagina
  riservata non richiede modifiche.
