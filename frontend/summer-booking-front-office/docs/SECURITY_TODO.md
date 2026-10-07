# Security TODO

Attività di sicurezza ancora da fare. Le voci si decidono insieme prima di implementarle; una volta completate si rimuovono da questo file.

Aggiornato al 07/10/2026.

## Content Security Policy severa (difesa contro XSS)

### Perché serve

- Con la gestione dei token prevista un attacco XSS (script iniettato nella pagina) **non può leggere il refresh token**: l'access token è solo in memoria e il refresh token sarà in un cookie `HttpOnly` che JavaScript non legge (oggi il cookie reale non esiste ancora: lo creerà il backend; i mock lo simulano).
- Questo non basta a proteggere la sessione: **finché la pagina è aperta**, uno script iniettato gira con gli stessi poteri dell'app, quindi può usare l'access token in memoria e anche chiedere nuovi token con il refresh (il browser allega il cookie da solo), chiamando le nostre API a nome dell'utente. `HttpOnly` impedisce la lettura del cookie, non le azioni di uno script nella stessa pagina.
- Angular già protegge dall'XSS (escape delle interpolazioni, sanitizzazione di `[innerHTML]`, che usiamo in `app-i18n-text` per le traduzioni). La CSP è la **seconda linea di difesa**: se per un errore nostro o di una dipendenza entrasse uno script, il browser si rifiuterebbe di eseguirlo o di mandare dati altrove.

### Stato (07/10/2026, audit S04)

- **Fatto nel frontend**: la build non inserisce più CSS critico e script inline in `index.html` (`optimization.styles.inlineCritical: false` nelle opzioni di build di `angular.json`; il CSS globale pesa circa 9 KB, il costo sulla prima visualizzazione è minimo). La policy qui sotto, senza le due righe dei Trusted Types, è stata provata in locale sulla build di produzione con l'header attivo (non Report-Only): home IT/EN, popup di accesso, chiamata a `/api`, traduzioni con HTML, service worker, nessuna violazione. L'area gestionale non era raggiungibile in quella prova (la build di produzione non ha i mock): si verifica nella fase Report-Only su DEV.
- **Da fare in CloudFront**: fasi Report-Only e attivazione, prima DEV poi PROD, con la procedura di `README_DEPLOY.md` (sezione "Content Security Policy").
- **Decisioni prese**: niente `autoCsp` né nonce (policy semplice, senza hash da ricalcolare); `style-src 'unsafe-inline'` accettato, perché Angular e Material inseriscono gli stili a runtime; Trusted Types e raccolta dei report rimandati (vedi sotto).

### Cosa fare

1. **Inviare la CSP come header HTTP** da CloudFront, nella stessa *response headers policy* già usata per gli altri header (vedi sotto), non con un tag `<meta>`: alcune direttive (`frame-ancestors`, report) nel `<meta>` non funzionano.
2. **Partire in sola osservazione**: prima `Content-Security-Policy-Report-Only` con un endpoint di report (`Reporting-Endpoints` + `report-to`), poi, dopo qualche giorno senza violazioni inattese, passare a `Content-Security-Policy`.
3. **Policy di partenza** pensata per l'app di oggi. Tutto viene dal nostro dominio: script, stili, font (`@font-face` locale), immagini, traduzioni JSON, API `/api`, service worker e manifest.

   ```text
   default-src 'self';
   script-src 'self';
   style-src 'self' 'unsafe-inline';
   img-src 'self' data:;
   font-src 'self';
   connect-src 'self';
   worker-src 'self';
   manifest-src 'self';
   object-src 'none';
   base-uri 'self';
   form-action 'self';
   frame-ancestors 'none';
   upgrade-insecure-requests;
   require-trusted-types-for 'script';
   trusted-types angular angular#bundler;
   ```

4. **Verificare** ogni pagina in IT/EN (home, login, gestionale, magazzino, datepicker, service worker) con la console del browser aperta, e la policy con CSP Evaluator.

### Ancora aperto

- **CSS critico inline**: deciso il 07/10/2026, inlining disattivato (vedi "Stato"). Le alternative scartate erano `security.autoCsp` (hash degli script inline calcolati in build; l'header CloudFront non dovrebbe contenere `default-src` né `script-src`) e un nonce per risposta (`ngCspNonce`, richiede Lambda@Edge). Se in futuro si riattiva l'inlining, la build torna a mettere uno script inline e la policy va rivista.
- **`style-src 'unsafe-inline'`**: accettato consapevolmente il 07/10/2026; l'alternativa sarebbe il nonce per risposta.
- **Trusted Types** (`require-trusted-types-for 'script'`): Angular li supporta. Vanno provati in Report-Only perché bloccano anche le librerie che scrivono HTML nel DOM. Nei browser che non li supportano l'app funziona comunque.
- **Endpoint dei report**: serve un indirizzo che raccolga le violazioni (backend nostro o servizio esterno).
- **Servizi esterni futuri** (pagamenti Stripe, mappe, analytics, font esterni…): ognuno va aggiunto esplicitamente alla policy, solo con i domini che la sua documentazione indica.

### Altri header di sicurezza (response headers policy di CloudFront)

Già configurati sull'ambiente DEV con la policy `summerbooking-dev-frontoffice-security-headers` (procedura e verifiche in `README_DEPLOY.md`, sezione "Response headers di sicurezza"):

- `Strict-Transport-Security: max-age=31536000` (senza `includeSubDomains` né `preload`);
- `X-Content-Type-Options: nosniff`;
- `X-Frame-Options: DENY` (in futuro affiancato da `frame-ancestors 'none'` nella CSP);
- `Referrer-Policy: strict-origin-when-cross-origin`;
- `X-XSS-Protection` volutamente non attivo (header superato).

Ancora da fare:

- **PROD**: creare la policy equivalente per l'ambiente di produzione e associarla alla sua distribuzione.
- **CSP**: aggiungerla alla stessa policy, prima in Report-Only (vedi sopra).
- **HSTS più stretto**: `includeSubDomains`, `preload` e durata di 2 anni solo dopo aver verificato che tutti i sottodomini del dominio definitivo funzionino esclusivamente in HTTPS: un'impostazione errata resta memorizzata dai browser.
- **`Permissions-Policy`**: disattivare ciò che l'app non usa (es. `camera=(), microphone=(), geolocation=()`); va aggiunto come header personalizzato della policy.
- **`Cross-Origin-Opener-Policy: same-origin`**: anche questo come header personalizzato.

### Fonti

- **OWASP – Content Security Policy Cheat Sheet**: guida pratica alle direttive e alle policy consigliate.
  https://cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html
- **OWASP – Cross Site Scripting Prevention Cheat Sheet**: cos'è l'XSS e come si previene; la CSP come difesa aggiuntiva.
  https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html
- **MDN – Content Security Policy (CSP)**: spiegazione chiara di ogni direttiva, con esempi.
  https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CSP
- **MDN – Trusted Types API**: cosa sono i Trusted Types e come proteggono dal DOM XSS.
  https://developer.mozilla.org/en-US/docs/Web/API/Trusted_Types_API
- **web.dev (Google) – Mitigate cross-site scripting (XSS) with a strict Content Security Policy**: perché le policy "strict" (nonce/hash) sono più efficaci delle liste di domini.
  https://web.dev/articles/strict-csp
- **Angular – Security**: CSP con Angular (`ngCspNonce`, `CSP_NONCE`, `autoCsp`, stili inline) e Trusted Types.
  https://angular.dev/best-practices/security
- **Angular – Workspace configuration**: le opzioni di build citate (`security.autoCsp`, `optimization.styles.inlineCritical`).
  https://angular.dev/reference/configs/workspace-config
- **W3C – Content Security Policy Level 3**: la specifica ufficiale.
  https://www.w3.org/TR/CSP3/
- **Google – CSP Evaluator**: strumento per controllare una policy prima di pubblicarla.
  https://csp-evaluator.withgoogle.com/
- **AWS – CloudFront response headers policies**: come aggiungere CSP e gli altri header di sicurezza su CloudFront.
  https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/understanding-response-headers-policies.html
- **OWASP Secure Headers Project**: elenco e valori consigliati degli header di sicurezza HTTP.
  https://owasp.org/www-project-secure-headers/
- **IETF – OAuth 2.0 for Browser-Based Applications**: il documento di riferimento per la gestione dei token nelle app nel browser (contesto della scelta memoria + cookie HttpOnly).
  https://datatracker.ietf.org/doc/draft-ietf-oauth-browser-based-apps/

## Mock: credenziali e sessione (da fare quando avremo il backend)

Rischi accettati finché l'ambiente DEV usa i mock (dati finti, ambiente da dismettere). La build di produzione non contiene nulla dei mock.

- **Hash della password di test nel codice**: `services/mocks/auth/auth.mock.ts` contiene l'hash SHA-256 (senza sale) della password dell'account di test, che finisce anche nella build DEV. È nella cronologia Git dal commit `6cb15e5` (già su GitHub).
  - Già deciso: la password reale va cambiata ovunque fosse usata (sito di riferimento, repository, casella email); dopo, l'hash non apre più nulla di reale.
  - Da fare: togliere l'hash dai file facendo accettare al mock solo l'username di test con qualunque password (il 401 si prova con un username diverso). Non riscrivere la cronologia Git: non serve dopo il cambio password.
- **Refresh del mock aggirabile**: il "cookie" simulato del mock è in `sessionStorage`/`localStorage` e il mock non verifica il `refreshToken`; chi apre DEV può crearlo a mano e ottenere una sessione. Non è un problema del prodotto: con il backend il refresh token è in un cookie `HttpOnly` validato dal server (hash in `identity.access_refresh_tokens`). Se DEV deve restare pubblico a lungo, l'unica protezione reale è limitarne l'accesso (login davanti al sito su CloudFront o IP ammessi).
