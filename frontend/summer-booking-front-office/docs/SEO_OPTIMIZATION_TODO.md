# SEO optimization TODO

Attività SEO ancora da fare, divise per pagina. Ogni pagina di riferimento analizzata aggiunge una sezione. Le voci si decidono insieme prima di implementarle; una volta completate si rimuovono da questo file.

Riferimento analizzato: https://devhub.summerbooking.it/ (analisi del 25/09/2026).

## Tutto il sito

Voci che valgono per ogni pagina pubblica.

### Cosa manca da fare

- **Dominio definitivo**: serve per canonical, Open Graph, sitemap e dati strutturati. Il riferimento usa `https://hub.summerbooking.it/`.
- **robots.txt**: permettere la scansione delle pagine pubbliche e indicare la sitemap. Il riferimento ha `User-agent: *`, `Disallow:` vuoto e `Sitemap: https://hub.summerbooking.it/sitemap.xml`. Aggiungerlo anche alle route servite dalla CloudFront Function.
- **sitemap.xml**: elenco delle pagine pubbliche in entrambe le lingue, con le alternative `hreflang`. La sitemap del riferimento contiene anche `/hub/advantages`, `/hub/functionality`, `/hub/booking`, `/hub/prizes`, `/hub/about-us`… da allineare man mano che creiamo le pagine.
- **hreflang**: per ogni pagina indicare le versioni `it`, `en` e `x-default` (tag `<link rel="alternate" hreflang="…">`). Il riferimento è solo in italiano e non li ha.
- **Prerendering**: oggi il contenuto esiste solo dopo l'esecuzione del JavaScript. Va generato nell'HTML iniziale di ogni pagina pubblica (le nostre regole SEO lo richiedono). Migliora anche FCP e LCP.
- **Dati strutturati di sito** (JSON-LD), comuni a tutte le pagine:
  - `Organization`: nome, URL, logo e profili social (`sameAs`: Facebook, Instagram);
  - `WebSite`: URL, nome, publisher.
- **Meta secondari**: `theme-color` e `author` (il riferimento usa `author` = "Summer Booking").

### Cosa non va bene sul riferimento (da non ripetere)

- **Codifica dichiarata due volte in modo contraddittorio**: `<meta charset="utf-8">` e `<meta http-equiv="Content-Type" content="text/html; charset=iso-8859-1">`. Dichiararla una sola volta, in UTF-8.
- **Due favicon da domini diversi** (`summerbooking.it` e `hub.summerbooking.it`): una sola favicon, servita dal nostro dominio.
- **Contenuto non presente nell'HTML iniziale**: senza JavaScript i motori di ricerca vedono solo il loader. Da noi va risolto con il prerendering.
- **Il `theme-color` è `#000000`**: incoerente con i colori del sito; scegliere il colore di brand.

## Home (`/it/home`, `/en/home`)

### Cosa manca da fare

- **Canonical** verso la home nella lingua della pagina, sul dominio definitivo.
- **Open Graph**: `og:title`, `og:description`, `og:url`, `og:type` (`website`), più `og:image` (manca anche nel riferimento) e `og:locale` con le alternative di lingua.
- **Twitter Card**: `twitter:card` (`summary_large_image`), `twitter:title`, `twitter:description`, `twitter:image`. Il riferimento usa `https://summerbooking.it/assets/summerBooking_hub.svg`: serve un'immagine nostra in formato PNG o JPG, perché le piattaforme social non mostrano gli SVG.
- **Titolo per la condivisione**: il riferimento usa "Summer Booking Hub" come `og:title`, diverso dal `<title>`. Decidere quale usare.
- **Dati strutturati della pagina** (JSON-LD):
  - `SoftwareApplication`: nome, categoria (`BusinessApplication`), sottocategoria ("Gestionale Stabilimento Balneare, Gestionale Spiagge"), `operatingSystem` "Web", offerta (`AggregateOffer` da 499 EUR);
  - `aggregateRating`: da decidere, vedi sotto;
  - `BreadcrumbList` con la sola voce "Home".
- **Da verificare con chi ha curato il SEO**:
  - la valutazione "4.9 su 100 recensioni" deve corrispondere a recensioni reali e verificabili. La pagina ne mostra solo 5, e Google può penalizzare valutazioni non giustificate;
  - il prezzo "da 499 EUR" ha `priceValidUntil` 2026-12-31: dopo quella data va aggiornato.

### Cosa non va bene sul riferimento (da non ripetere)

- **Due `<title>` diversi** nella stessa pagina ("Gestionale per Stabilimenti Balneari: Prova Summer Booking Hub" e "Summer Booking Hub"): una pagina deve avere un solo titolo. Da noi è già così.
- **Gerarchia dei titoli disordinata**: il primo titolo è un H2 ("Visione, visibilità, controllo."), l'H1 è "Beach Management System" e arriva dopo. I titoli delle tre card (H3) compaiono due volte nel codice della pagina. Da noi l'H1 è il titolo della hero, unico, e le sezioni sono H2.
