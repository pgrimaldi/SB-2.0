# Sicurezza

Regole di sicurezza del frontend: OWASP, secret e chiavi, password, token e sessione, XSS, link esterni, dipendenze,
header di sicurezza e CSP.

## Principi

- La sicurezza è un requisito prioritario, soprattutto per credenziali, token e sessione.
- Riferimenti: OWASP Top 10 e OWASP ASVS.

## Secret e valori pubblici

- Mai credenziali, secret, chiavi (anche AWS) o informazioni sensibili nel codice, nei mock, nei file `environment` o
  in qualunque altro file del repository.
- Nei file `environment` stanno solo valori pubblici per natura: `stripeClientId` e `stripeConnectReturnUrl` (Stripe
  Connect; mai la chiave segreta `sk_…`) e `googleMapsMapId`.
- In produzione (`environment.prod.ts`) questi valori restano vuoti finché non ci sono quelli di produzione.
- Dati dei mock (solo inventati, password solo in hash): vedi `api-e-mock.md`.

## Chiave di Google Maps

- Non sta in nessun file versionato. CodeBuild la legge da Secrets Manager e la passa al compilatore con
  `--define GOOGLE_MAPS_API_KEY_DEV=…` tramite `npm run build:codebuild` (`scripts/build-codebuild.mjs`, che fallisce
  se la chiave manca o è malformata).
- Il codice la legge da `environments/google-maps-api-key.ts`. Senza chiave (`npm start`, `build:dev-env`,
  `build:prod` lanciati da soli) `GoogleMapsBehaviour` non contatta Google e la tab Media mostra il messaggio di mappa
  non disponibile.
- La chiave resta visibile nel browser: la proteggono le restrizioni in Google Cloud (solo i nostri domini, solo Maps
  JavaScript API), le quote e gli avvisi di budget.
- Procedura: `docs/README_DEPLOY.md`, sezione "Chiave Google Maps tramite Secrets Manager".

## Password e login

- Le password non si salvano mai nel browser, non finiscono mai nei log e viaggiano solo nel body di una `POST` HTTPS.
- Campi con `autocomplete` corretto: `username` o `email` per l'utente, `current-password`, `new-password`.
- Errori di login generici ("Credenziali non valide"): non rivelano se l'utente esiste.
- Login: `POST /api/identity/auth/login` con `LoginRequest` (`username`, `password`, `remember`) più `app`, fisso a
  `"hub"` in `environment.authApp` (in futuro verrà da un file di configurazione).
- Risposta: `AuthSession` = `{ accessToken, expiresIn, user: { id, email, role, propertyId } }`. Le pagine leggono
  `user.propertyId` solo per sapere che l'utente è pronto: lo stabilimento non si manda mai alle API, il backend lo
  prende dal token.
- La verifica in due passaggi del backend (`twoFactorRequired`, `mfaToken`, `login/verify-otp`) non è ancora gestita:
  vedi `docs/TODO.md`.

## Token

- L'access token (breve durata) vive solo in memoria in `AuthBehaviour`: mai in `localStorage`/`sessionStorage`,
  nell'URL o nei log. Il frontend lo tratta come opaco (non lo decodifica).
- Il refresh token sta in un cookie `HttpOnly` + `Secure` + `SameSite=Strict` gestito dal server, con rotazione e
  revoca: `sb_rt` in sviluppo, `__Secure-sb_rt` in produzione, Path `/api/identity`.
- Nello storage restano solo segni non segreti:
  - `sb.signed-in` dice all'avvio se provare a rinnovare la sessione; sta in `sessionStorage` (segno della scheda) o,
    con "Ricordami", in `localStorage` (segno condiviso);
  - `sb.session-name` è il nome della sessione (vedi "Sessione su più schede");
  - `sb.stripe-connect-state`, in `sessionStorage`, è il valore casuale mandato a Stripe Connect, da ritrovare al ritorno; si
    toglie quando la sessione finisce nella scheda.
- L'access token si allega alle chiamate solo tramite l'interceptor (`services/api/auth.interceptor.ts`) e solo verso
  l'API dell'applicazione, mai verso domini terzi.
- Login, rinnovo e logout (`POST /api/identity/auth/login`, `/refresh`, `/logout`) usano solo il cookie del refresh
  token.
- Su una risposta 401 il token si rinnova una volta e la chiamata si ripete una sola volta.

## Sessione su più schede

### Generazione della sessione

- `AuthBehaviour.sessionGeneration()` è un numero che sale quando la sessione cambia mano (login, logout, scadenza,
  logout da un'altra scheda), non a ogni rinnovo normale.
- Una risposta di rinnovo arrivata per una generazione vecchia non cambia nulla: non riapre una sessione chiusa, non
  sostituisce né chiude quella nuova. Un rinnovo in corso non viene condiviso con la sessione successiva.
- L'interceptor ricorda la generazione di ogni chiamata: se al 401 è cambiata, restituisce l'errore senza rinnovare né
  ripetere (un'operazione partita con un utente non si esegue mai sotto un altro) e non fa uscire nessuno.
- Se il rinnovo è rifiutato, è `refresh()` a chiudere la sessione e a tornare alla home; all'avvio, senza una sessione
  in uso, chiude e basta.

### Solo il rifiuto del server chiude una sessione

- Rinnovo rifiutato con 401: la sessione si chiude in questa scheda e, con un messaggio, nelle altre schede della
  stessa sessione.
- Rinnovo che non raggiunge il server (rete assente, server fermo, limite di 15 secondi): fallisce con il suo errore e
  non cambia nulla. Sessione e "Ricordami" restano, la chiamata che lo aveva chiesto riceve l'errore di rete; all'avvio
  l'app si apre senza sessione e riprova alla visita successiva.
- All'avvio, se mentre la scheda si apriva qualcuno è entrato in un'altra scheda (è cambiato il nome del proprietario
  del cookie), la scheda non adotta quella sessione.

### Una chiamata del cookie alla volta

- Login, rinnovo e logout riscrivono il cookie del refresh, che il browser applica da solo e JavaScript non può leggere
  né annullare: per questo passano da un lucchetto, `AUTH_LOCK` (`behaviours/auth/auth-lock.ts`).
- Il lucchetto è la Web Locks API del browser (nome `sb-auth-cookie`), condivisa da tutte le schede del sito e liberata
  dal browser se una scheda si chiude; dove manca (test) è una coda della sola scheda.
- Ogni chiamata ha al massimo 15 secondi (`COOKIE_CALL_TIMEOUT`), poi fallisce e la successiva può partire.
- Un rinnovo che ha aspettato un login o un logout non chiede nulla al server se nel frattempo la sessione ha cambiato
  mano.

### Mai un'identità non scelta

- Se in un'altra scheda qualcuno fa il login (messaggio sul `BroadcastChannel`), o un rinnovo restituisce un utente o
  uno stabilimento diversi da quelli in memoria, la scheda lascia la sua sessione e torna alla home, dove serve un
  nuovo login.
- Lo fa senza logout sul server e senza togliere il segno condiviso `sb.signed-in`, che appartengono alla sessione
  dell'altra scheda (`leaveHere()` in `AuthBehaviour`).

### Identità della sessione

- Al login la scheda dà alla sessione un nome casuale non segreto (`crypto.randomUUID()`), scritto in `localStorage`
  (`sb.session-name`) qualunque sia "Ricordami", perché il cookie è condiviso da tutte le schede. Una scheda che
  recupera la sessione dal cookie adotta quel nome.
- I messaggi tra schede portano il nome: `{ isLogout, session }` (`TabMessage`; `isLogout: false` = login).
- Un messaggio di logout chiude solo la sessione con lo stesso nome: un messaggio vecchio non tocca una sessione nuova.
- Un login con un altro nome fa uscire la scheda solo se quel nome è ancora il proprietario registrato del cookie: un
  messaggio di login più vecchio, arrivato in ritardo, non fa uscire una sessione più nuova.
- Prima di rinnovare o di fare logout, dentro il lucchetto, la scheda controlla che il nome in `localStorage` sia
  ancora il suo: se no il cookie è di un'altra sessione, non manda nulla ed esce (`leaveHere()`).

### Il rinnovo non cambia nulla per le pagine

- `AuthBehaviour.user` resta lo stesso oggetto finché id, email, ruolo e stabilimento sono gli stessi (funzione
  `equal` del computed): un rinnovo del token non fa rileggere i dati (es. la bozza della Configurazione email resta)
  e le tabelle non tornano a pagina 1.

### Chiusura della sessione in una scheda

- Una scheda che chiude la sessione (logout, scadenza, logout arrivato da un'altra scheda) cancella sempre il segno
  della scheda; il segno condiviso e il nome della sessione solo se il cookie è ancora della sua sessione. Se nel
  frattempo altrove è entrato qualcuno con "Ricordami", il suo accesso ricordato resta.
- Una scheda fatta uscire perché altrove è entrato qualcuno cancella solo il segno della scheda: ricaricandola entra
  solo se il browser ricorda la nuova sessione con "Ricordami", come una scheda nuova.
- Limite noto: la specifica HTML non garantisce che una scrittura in `localStorage` sia subito visibile alle altre
  schede; la garanzia assoluta può darla solo un server che revoca la sessione indicata dalla richiesta.

## Logout

- La sessione si chiude nel browser solo dopo la risposta del server, che è sempre 204, anche se la sessione era già
  chiusa (un 401, per esempio da un proxy, vale come sessione già chiusa), con un nuovo tentativo automatico. I due
  tentativi e la pausa fra loro stanno in un solo turno del lucchetto e valgono solo per la sessione che ha chiesto il
  logout.
- Se non riesce, l'utente resta connesso e vede il popup "Disconnessione Fallita" (`app-alert-popup`).
- Il logout vale per tutte le schede aperte della sessione e cancella il segno condiviso, quello della scheda e il
  nome della sessione: dopo un logout serve sempre un nuovo login, anche con "Ricordami".

## XSS e link

- Niente `bypassSecurityTrust*`, niente HTML costruito a mano.
- `[innerHTML]` solo con testi sanificati da Angular (toglie script, attributi di evento e link `javascript:`): le
  traduzioni (`appI18nText`) e l'HTML dei termini e condizioni di Fiscalizzazione (`termsAndConditions`).
- I link dei file che arrivano dal backend (foto, PDF, icone) finiscono in `src` e `href` solo se sono https o del
  nostro dominio (`fileLinkOf`, `behaviours/files/file-link.ts`). Un file con un link non sicuro resta visibile senza
  anteprima, e si può cancellare.
- I link esterni si aprono in una nuova scheda con `rel="noopener noreferrer"`.

## Dipendenze

- Si aggiungono solo se necessarie, da fonti ufficiali, e si controllano con `npm audit`.
- Quali librerie sono ammesse: vedi `architettura.md`.

## Header di sicurezza e CSP

- Gli header di sicurezza li aggiunge CloudFront con una response headers policy per ambiente: configurazione in
  `docs/README_DEPLOY.md`.
- Su DEV sono attivi HSTS, `nosniff`, `X-Frame-Options`, `Referrer-Policy` e la CSP.
- La CSP DEV include Google Maps: script solo da `maps.googleapis.com` e `maps.gstatic.com`, font Google, origini
  immagini Google e worker `blob:`. `connect-src` include anche i due host dei font, perché il service worker li
  scarica con `fetch`.
- Immagini, script e servizi su altri domini (es. Google Maps) vanno aggiunti alla CSP prima di usarli; le immagini in
  `img-src` e in `connect-src`, sempre per il service worker. `blob:` in `img-src` serve alle anteprime dei file scelti.
- Non copiare l'allowlist generica di Google e non lasciare attivo il bypass del service worker come soluzione.
- Valore completo della CSP e verifiche su pagina e `ngsw-worker.js`: `docs/README_DEPLOY.md`. Policy di PROD e
  scelte ancora aperte: `docs/SECURITY_TODO.md`.
