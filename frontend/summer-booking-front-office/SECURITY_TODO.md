# Security TODO

Attività di sicurezza ancora da fare. Le voci si decidono insieme prima di implementarle; una volta completate si rimuovono da questo file.

Aggiornato al 29/09/2026.

## Content Security Policy severa (difesa contro XSS)

### Perché serve

- Con la gestione attuale dei token un attacco XSS (script iniettato nella pagina) **non può rubare la sessione in modo duraturo**: l'access token è solo in memoria e il refresh token è in un cookie `HttpOnly` che JavaScript non legge.
- Però, **finché la pagina è aperta**, uno script iniettato potrebbe comunque usare l'access token in memoria per chiamare le nostre API a nome dell'utente.
- Angular già protegge dall'XSS (escape delle interpolazioni, sanitizzazione di `[innerHTML]`, che usiamo in `app-i18n-text` per le traduzioni). La CSP è la **seconda linea di difesa**: se per un errore nostro o di una dipendenza entrasse uno script, il browser si rifiuterebbe di eseguirlo o di mandare dati altrove.

### Cosa fare

1. **Inviare la CSP come header HTTP** da CloudFront (*Response headers policy*), non con un tag `<meta>`: alcune direttive (`frame-ancestors`, report) nel `<meta>` non funzionano.
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

### Da decidere prima di attivarla

- **CSS critico inline in `index.html`**: la build di produzione oggi inserisce uno `<style>` e un `onload="this.media='all'"` inline, che `script-src 'self'` bloccherebbe. Opzioni:
  - disattivare l'inlining del CSS critico (`optimization.styles.inlineCritical: false`): policy semplice (`script-src 'self'`), ma primo rendering un po' più lento;
  - attivare `security.autoCsp` di Angular: calcola gli hash degli script inline in fase di build. In quel caso l'header CloudFront **non** deve contenere `default-src` né `script-src`, e non è compatibile con il rendering lato server;
  - usare un nonce diverso per ogni risposta (`ngCspNonce`), che però richiede una funzione sull'edge (Lambda@Edge) per scriverlo nell'HTML.
- **`style-src 'unsafe-inline'`**: Angular inserisce a runtime gli stili dei componenti come `<style>`. Senza un nonce per risposta servono stili inline permessi. Il rischio è molto minore di quello degli script, ma va accettato consapevolmente, oppure si adotta il nonce.
- **Trusted Types** (`require-trusted-types-for 'script'`): Angular li supporta. Vanno provati in Report-Only perché bloccano anche le librerie che scrivono HTML nel DOM. Nei browser che non li supportano l'app funziona comunque.
- **Endpoint dei report**: serve un indirizzo che raccolga le violazioni (backend nostro o servizio esterno).
- **Servizi esterni futuri** (pagamenti Stripe, mappe, analytics, font esterni…): ognuno va aggiunto esplicitamente alla policy, solo con i domini che la sua documentazione indica.

### Altri header di sicurezza da impostare nella stessa policy di CloudFront

- `Strict-Transport-Security: max-age=63072000; includeSubDomains; preload`, dopo aver verificato che tutti i sottodomini siano in HTTPS.
- `X-Content-Type-Options: nosniff`.
- `Referrer-Policy: strict-origin-when-cross-origin`.
- `Permissions-Policy`: disattivare ciò che l'app non usa (es. `camera=(), microphone=(), geolocation=()`).
- `Cross-Origin-Opener-Policy: same-origin`.

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
