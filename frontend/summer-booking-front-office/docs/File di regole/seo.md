# SEO

Strategia SEO delle pagine pubbliche, dispositivi e browser, review SEO obbligatoria e stato della home pubblica
dell'hub.

## Quali pagine

- La SEO si applica con rigore alle poche pagine pubbliche indicate dall'utente. Una pagina non dichiarata SEO è non
  indicizzabile per impostazione predefinita.
- La home è una pagina pubblica e deve avere SEO.
- Ordine di priorità:
  1. replica corretta di layout e comportamento;
  2. architettura che non ostacoli la SEO futura;
  3. SEO tecnica della pagina;
  4. ottimizzazioni avanzate soltanto dopo una baseline funzionante e misurabile.
- Punti SEO ancora da fare: `docs/SEO_OPTIMIZATION_TODO.md`.

## Rendering

- Pagine pubbliche che cambiano raramente: preferire prerendering/SSG, mantenendo il deploy statico S3/CloudFront.
- SSR solo per contenuti pubblici realmente dinamici a ogni richiesta; mai SSR globale senza necessità.
- Dashboard e area privata: il CSR è appropriato; non si indicizzano.
- Il contenuto SEO fondamentale sta nell'HTML iniziale/prerenderizzato e non dipende da click, canvas o immagini.

## URL e multilingua

- Ogni pagina pubblica ha un URL stabile, leggibile e specifico (prefissi delle lingue: vedi
  `routing-e-autenticazione.md`).
- Contenuti indicizzabili multilingua: URL distinti (es. `/it/...` e `/en/...`). Non affidarsi soltanto alla lingua
  salvata nel browser mantenendo lo stesso URL per tutte le versioni indicizzabili.
- Non duplicare componenti per lingua: un solo componente, con contenuti e metadata localizzati.
- `html lang`, `hreflang`, eventuale `x-default`, canonical, title e description coerenti con la lingua.

## Metadata e HTML

- Ogni pagina pubblica ha:
  - un solo `h1` appropriato e una gerarchia di titoli logica;
  - title unico e tradotto tramite il servizio Angular `Title`;
  - meta description specifica e tradotta tramite `Meta`;
  - canonical coerente, mai puntato genericamente alla home o alla lingua sbagliata;
  - Open Graph e Twitter/X card quando utili alla condivisione;
  - HTML semantico e link interni crawlable;
  - contenuto principale realmente tradotto.
- `index.html` contiene favicon e description di default.
- Schema.org/JSON-LD solo quando corrisponde a contenuto reale: mai markup fittizio per ottenere rich result.

## Indicizzazione e status

- Sitemap XML con sole URL pubbliche, canoniche e indicizzabili.
- `robots.txt` coerente, mai usato come protezione dell'area privata.
- Login, dashboard, amministrazione e route tecniche non compaiono nella sitemap e, se necessario, hanno `noindex`.
- Status corretti: 200 per pagine valide, 301/308 per redirect permanenti, 404 per pagine inesistenti, 401/403 per
  risorse protette.
- Niente soft 404 e fallback SPA indiscriminati: vedi "Regola SPA di CloudFront" in `git-e-deploy.md`.
- Se cambiano URL già indicizzati, redirect permanenti verso l'equivalente corretto; mai reindirizzare in massa tutto
  alla home.

## Performance e immagini

- Proteggere LCP, INP e CLS: niente JavaScript iniziale superfluo, richieste duplicate o librerie pesanti per funzioni
  minime.
- Code splitting e lazy loading delle feature pesanti, senza rendere irraggiungibili route, testo o link pubblici.
- Immagini ottimizzate, con dimensioni dichiarate e caching/fingerprinting.
- Immagini raster sempre in WebP; quelle mostrate molto più piccole del file originale hanno varianti `srcset` con
  `sizes`.
- Verifica con Lighthouse in una finestra in incognito (le estensioni del browser falsano i risultati).

## Dispositivi e browser (accessibilità)

- Accessibilità significa funzionare bene su tutti i dispositivi e browser: telefono, tablet e desktop; Safari,
  Chrome, Firefox, Opera, Edge; touch e mouse.
- Non si aggiunge supporto a lettori vocali, comandi vocali o altri strumenti assistivi (etichette nascoste, `aria-*`,
  ruoli, testi solo per screen reader), a meno che non serva alla SEO o alla sicurezza: l'app la usa chi lavora in
  spiaggia o al bar. Le segnalazioni di revisione su questi strumenti si ignorano.
- Restano gli attributi che Angular Material mette da sé e `CalendarTextsBehaviour`, i cui testi sono anche i tooltip
  visibili delle frecce del calendario. Prima di togliere un testo "per screen reader" controllare che Material non lo
  usi anche come tooltip o testo visibile.
- Restano gli `alt` delle immagini e i nomi dei link, che servono alla SEO.
- Testo con contrasto minimo 4.5:1 sul proprio sfondo (WCAG AA): i colori di brand troppo chiari per il testo si
  scuriscono mantenendo la tinta (es. l'azzurrino `#0e99c6` diventa `#0c80a6` sui testi; grigio `#767676`).
- Ogni elemento cliccabile ha un'area di tocco di almeno 1.5rem × 1.5rem, anche se la parte visibile è più piccola.
- Dentro `<ul>`/`<ol>` solo `<li>` (HTML valido).

## Review SEO obbligatoria

- Una pagina pubblica non è finita finché non ha superato prima la verifica funzionale e poi una review SEO dedicata.
- La review controlla almeno:
  - route e URL diretti, stabili e leggibili;
  - rendering/prerendering del contenuto principale;
  - title, description, canonical, `lang` e `hreflang`;
  - heading, `h1`, HTML semantico e link crawlable;
  - alt, peso delle immagini e comportamento mobile;
  - sitemap, robots/noindex e status HTTP;
  - redirect e dati strutturati, se presenti;
  - assenza di contenuti importanti vincolati a interazioni inutili;
  - Lighthouse/PageSpeed e Core Web Vitals ragionevoli;
  - verifica con Search Console, URL Inspection e Rich Results Test quando applicabile.

## Home pubblica dell'hub

- Riferimento visuale: `https://devhub.summerbooking.it/`.
- Route `/<lingua>/home` (`it`, `en`, `fr`, `es`, `de`, `zh`, `ar`): è la landing di default e ha la SEO in tutte le
  lingue alla pari dell'italiano: title e description tradotti con la parola chiave all'inizio, `lang`/`dir`, un solo
  H1, gerarchia dei titoli, alt, nomi di link e bottoni, testi di almeno 12px.
- Hero desktop e mobile sono asset locali distinti.
- La hero desktop usa l'originale del riferimento (`hero-beach-desktop.webp`, 1966×1031): mantenere queste
  proporzioni, senza convertirla in 16:9, perché con `object-fit: cover` il faro verrebbe ingrandito e tagliato
  diversamente. Il preload in `index.html` usa lo stesso asset; le varianti mobile restano separate.
- Header e logo replicano ridimensionamento e transizione durante lo scroll.
- Sul desktop il logo è allineato alla colonna testuale della hero; sotto il breakpoint tablet è centrato.
- Font: vedi `asset-e-icone.md`.
