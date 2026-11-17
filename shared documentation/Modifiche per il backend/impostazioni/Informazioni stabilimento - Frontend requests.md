# Informazioni stabilimento - Frontend requests

Tutte le modifiche che il backend deve fare perché la pagina **Informazioni stabilimento** del gestionale funzioni come
la vuole il frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare,
con payload, risposta, regole ed errori attesi.

La pagina è `/settings/bathhouse-configuration` e ha tre tab, ognuna con le sue API:

| Tab | Rotta del frontend | Cosa mostra |
|---|---|---|
| Informazioni Generali | `/settings/bathhouse-configuration/1` | Nome, indirizzo, contatti, orari, cancellazione, animali, descrizione, regolamento |
| Servizi | `/settings/bathhouse-configuration/2` | I servizi del catalogo, da spuntare |
| Media | `/settings/bathhouse-configuration/3` | Posizione sulla mappa, immagini, link del tour virtuale, PDF di listino e menù |

## Riepilogo

| # | Metodo | Rotta | Tab | Cosa fa |
|---|---|---|---|---|
| 1 | GET | `/api/catalog/property-info/bathhouse-configuration-general` | Informazioni Generali | Legge i campi della tab |
| 2 | POST | `/api/catalog/property-info/save-bathhouse-configuration-general` | Informazioni Generali | Salva i campi della tab |
| 3 | GET | `/api/catalog/property-info/bathhouse-configuration-services` | Servizi | Legge i servizi del catalogo con il loro stato |
| 4 | POST | `/api/catalog/property-info/save-bathhouse-configuration-services` | Servizi | Salva i servizi offerti |
| 5 | GET | `/api/catalog/property-info/bathhouse-configuration-media` | Media | Legge posizione, foto, tour virtuale e PDF |
| 6 | POST | `/api/catalog/property-info/save-bathhouse-configuration-media` | Media | Salva tour virtuale e posizione |

Sono tutte **API nuove**. Foto e PDF invece usano le API che il backend ha già (sezione "API esistenti usate così come
sono").

## Regole comuni

Valgono per tutte e sei le API.

- **Modulo e rotte**: Catalog (`Module.Catalog`). Le sei API stanno nel gruppo `/api/catalog/property-info`
  (`Endpoints/PropertyInfoEndpoints.cs`, `PropertyInfoService`), accanto a quelle che ci sono già. I dati sono gli
  stessi della scheda dello stabilimento (`PropertyInfoDto`): le nuove API leggono e scrivono le stesse tabelle.
- **Le API esistenti non cambiano**: le rotte di oggi di `/api/catalog/property-info` (`GET` e `PUT /`, `/amenities`,
  `/main-photo`, `/photos`, `/photos/order`, `/photos/{id}`, `/documents/{document}`) e `/api/catalog/lookups/*`
  restano come sono; le sei nuove si aggiungono.
- **Stabilimento dal token**: lo stabilimento è quello dell'utente autenticato, come per `property-info`; nessuna
  richiesta lo contiene.
- **Permessi**: le GET come `GET /property-info` (`CatalogRead`), i salvataggi come `PUT /property-info`
  (`CatalogSettings`). Senza permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Gli enum viaggiano con il nome .NET (es. `OnRequest`).
- **Salvataggi**: rispondono 204 senza corpo e salvano **solo i campi della loro tab**; il resto della scheda resta
  com'è. Non vanno fatti richiamando `PUT /property-info`, che sostituisce tutta la scheda.
- **Errori** nel formato Problem Details del backend, con `code`. Gli errori di validazione sono 400
  `validation.failed` con `errors[]`, un elemento per campo: `field` è il nome del campo **nel corpo di questa API**
  (es. `bathhouseName`, non `name`), `code` il codice (es. `validation.required`), `args` i valori del messaggio. Il
  frontend mostra ogni errore sotto il suo campo. Senza token valido: 401 `auth.unauthenticated`.

## 1. GET /api/catalog/property-info/bathhouse-configuration-general

Tab Informazioni Generali: il frontend la chiama la prima volta che si apre la tab e con la risposta riempie i campi.
Restituisce **solo i campi della tab**, con i nomi e i valori del frontend.

- Nessun parametro.
- Risposta 200. Uno stabilimento che non ha ancora salvato la scheda risponde con tutti i campi `null`.

```json
{
  "bathhouseName": "Lido Demo",
  "address": "Lungomare dei Pini 12",
  "postalCode": "00100",
  "city": "Roma",
  "province": "Roma",
  "region": "Lazio",
  "country": "IT",
  "phone": "+39 06 1234567",
  "whatsapp": "+39 333 1234567",
  "email": "info@lidodemo.example",
  "website": "https://www.lidodemo.example",
  "facebook": "https://www.facebook.com/lidodemo.example",
  "instagram": "https://www.instagram.com/lidodemo.example",
  "description": "<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p>",
  "openingTime": "08:30",
  "closingTime": "19:00",
  "isFreeCancellation": true,
  "cancellationNoticeHours": 48,
  "petPolicy": "Allowed",
  "regulation": "<p>Vietato <strong>fumare</strong> in spiaggia.</p>"
}
```

Da dove vengono i campi, rispetto a `PropertyInfoDto`:

| Campo | In `PropertyInfoDto` | Note |
|---|---|---|
| `bathhouseName` | `name` | |
| `address`, `city`, `province`, `region` | stessi nomi | |
| `postalCode` | `cap` | |
| `country` | `countryCode` | Codice ISO 3166-1 alpha-2 (es. `IT`). |
| `phone`, `whatsapp`, `email`, `website`, `facebook`, `instagram` | stessi nomi | |
| `description`, `regulation` | stessi nomi | HTML con solo `<p>`, `<br>`, `<strong>`, `<em>`, `<u>`, `<ul>`, `<ol>`, `<li>` e `<a href>`. |
| `openingTime`, `closingTime` | `openHoursFrom`, `openHoursTo` | Formato `HH:mm`, senza secondi (es. `"08:30"`). |
| `isFreeCancellation`, `cancellationNoticeHours` | stessi nomi | Il frontend usa solo `true` con 48 o 24 ore, oppure `false` con `null`. |
| `petPolicy` | `petsPolicy` | `Allowed`, `NotAllowed`, `OnRequest` (al posto di `allowed`, `not_allowed`, `on_request`); `null` se non scelta. |

Non ci sono: `slug`, `latitude`, `longitude`, `virtualTourUrl`, `mainPhotoUrl`, `photos`, `menuPdf`, `priceListPdf` e
`amenityCodes` (sono di altre tab o la pagina non li usa).

Errori: 401 `auth.unauthenticated`, 403 `auth.forbidden`.

## 2. POST /api/catalog/property-info/save-bathhouse-configuration-general

Bottone "Salva" della tab Informazioni Generali.

- Corpo: **tutti** i campi della tab, cambiati o no, con gli stessi nomi e valori della API 1.

```json
{
  "bathhouseName": "Lido Demo",
  "address": "Lungomare dei Pini 12",
  "postalCode": "00100",
  "city": "Roma",
  "province": "Roma",
  "region": "Lazio",
  "country": "IT",
  "phone": "+39 06 1234567",
  "whatsapp": "+39 333 1234567",
  "email": "info@lidodemo.example",
  "website": "https://www.lidodemo.example",
  "facebook": "https://www.facebook.com/lidodemo.example",
  "instagram": "https://www.instagram.com/lidodemo.example",
  "description": "<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p>",
  "openingTime": "08:30",
  "closingTime": "19:00",
  "isFreeCancellation": true,
  "cancellationNoticeHours": 48,
  "petPolicy": "Allowed",
  "regulation": "<p>Vietato <strong>fumare</strong> in spiaggia.</p>"
}
```

- Salva **solo questi campi**: posizione, tour virtuale, slug, foto, PDF e servizi restano come sono.
- Stesse regole di validazione di `PUT /property-info` per gli stessi dati. `bathhouseName` è obbligatorio (come
  `name`); gli altri campi possono arrivare `null`.
- Risposta: 204 senza corpo.
- Errori: 400 `validation.failed` con `errors[]` sui nomi del corpo qui sopra (es. `bathhouseName` vuoto con
  `validation.required`, `email` non valida con `validation.email`); 401 `auth.unauthenticated`; 403
  `auth.forbidden`.

## 3. GET /api/catalog/property-info/bathhouse-configuration-services

Tab Servizi: il frontend la chiama la prima volta che si apre la tab e mostra una casella per servizio. Restituisce
**una sola lista** con tutti i servizi attivi del catalogo, ognuno con il suo stato per lo stabilimento.

- Nessun parametro. La lingua dei nomi è quella dell'header `Accept-Language`.
- Risposta 200: i servizi **nell'ordine in cui la pagina li mostra** (vedi "Sezione e ordine dei servizi").

```json
[
  {
    "idService": 14,
    "name": "Accesso disabili",
    "categoryCode": "accessibility_comfort",
    "iconUrl": "/media/icons/accesso-disabili.svg",
    "isSelected": true
  },
  {
    "idService": 38,
    "name": "Wifi",
    "categoryCode": "accessibility_comfort",
    "iconUrl": null,
    "isSelected": false
  }
]
```

| Campo | Da dove viene |
|---|---|
| `idService` | `catalog.service_amenities.id` (es. 14 per "Accesso disabili"). Il frontend riconosce i servizi da questo id. |
| `name` | Il nome del servizio nella lingua della richiesta (traduzioni della dotazione). |
| `categoryCode` | La **sezione della pagina** in cui va il servizio: `accessibility_comfort`, `sport_activities`, `food_extra` o `beach`. Non sono le categorie di `GET /lookups/amenity-categories`: vedi "Sezione e ordine dei servizi". Mai `null`. |
| `iconUrl` | Link dell'icona del servizio; `null` se non c'è. |
| `isSelected` | `true` se lo stabilimento offre il servizio (è tra le `amenityCodes` della scheda). |

### Sezione e ordine dei servizi

La pagina mostra i servizi in quattro sezioni fisse, in quest'ordine:

| `categoryCode` | Sezione |
|---|---|
| `accessibility_comfort` | Accessibilità e Comfort |
| `sport_activities` | Sport e attività |
| `food_extra` | Ristorazione e extra |
| `beach` | Spiaggia |

**Sezione e ordine di ogni servizio li decide il backend e li tiene nel suo database** (es. due colonne in più su
`catalog.service_amenities`, o una tabella apposita): il frontend non ha nessuna tabella di id. Mette ogni servizio
nella sezione del suo `categoryCode` e, dentro la sezione, nell'ordine della lista. Un servizio nuovo del catalogo deve
avere sezione e ordine, altrimenti la pagina non lo mostra.

Valori iniziali, da caricare nel database (sono quelli che la pagina mostra oggi):

| `idService` | Servizio | `categoryCode` | Ordine |
|---|---|---|---|
| 14 | Accesso disabili | `accessibility_comfort` | 1 |
| 1 | Doccia per disabili | `accessibility_comfort` | 2 |
| 31 | Doccia | `accessibility_comfort` | 3 |
| 36 | Doccia calda | `accessibility_comfort` | 4 |
| 34 | Tabacchi/edicola | `accessibility_comfort` | 5 |
| 38 | Wifi | `accessibility_comfort` | 6 |
| 35 | Toilette | `accessibility_comfort` | 7 |
| 23 | Parcheggio | `accessibility_comfort` | 8 |
| 30 | Parcheggio custodito | `accessibility_comfort` | 9 |
| 7 | Parcheggio bici | `accessibility_comfort` | 10 |
| 22 | Parcheggio incluso | `accessibility_comfort` | 11 |
| 29 | Cassette di sicurezza | `accessibility_comfort` | 12 |
| 8 | Cabine | `accessibility_comfort` | 13 |
| 21 | Personale multilingua | `accessibility_comfort` | 14 |
| 18 | Infermeria | `accessibility_comfort` | 15 |
| 4 | Beach soccer | `sport_activities` | 16 |
| 5 | Beach volley | `sport_activities` | 17 |
| 15 | Beach tennis | `sport_activities` | 18 |
| 17 | Calcio balilla | `sport_activities` | 19 |
| 10 | Escursioni | `sport_activities` | 20 |
| 2 | Animazione | `sport_activities` | 21 |
| 19 | Kitesurf | `sport_activities` | 22 |
| 33 | Surf | `sport_activities` | 23 |
| 9 | Canoa | `sport_activities` | 24 |
| 37 | Moto d’acqua | `sport_activities` | 25 |
| 20 | Motoscafo | `sport_activities` | 26 |
| 32 | Sub/snorkeling | `sport_activities` | 27 |
| 24 | Pedalò | `sport_activities` | 28 |
| 27 | Piscina | `sport_activities` | 29 |
| 13 | Idromassaggio | `sport_activities` | 30 |
| 16 | Area giochi | `sport_activities` | 31 |
| 6 | Posto barca | `sport_activities` | 32 |
| 11 | Palestra | `sport_activities` | 33 |
| 3 | Bar | `food_extra` | 34 |
| 28 | Ristorante | `food_extra` | 35 |
| 26 | Pizzeria | `food_extra` | 36 |
| 12 | Happy hour | `food_extra` | 37 |
| 57 | Cibo vegano - Cibo senza glutine | `food_extra` | 38 |
| 25 | Animali ammessi | `beach` | 39 |
| 40 | Spiaggia per single | `beach` | 40 |
| 48 | Servizio spiaggia | `beach` | 41 |
| 53 | Spiaggia LGBT | `beach` | 42 |
| 44 | Spiaggia del silenzio | `beach` | 43 |
| 49 | Spiaggia per famiglie | `beach` | 44 |
| 56 | Spiaggia naturista | `beach` | 45 |
| 39 | Pacchetti all inclusive | `beach` | 46 |
| 45 | Spiaggia per anziani | `beach` | 47 |
| 52 | Ventilatore sotto l’ombrellone | `beach` | 48 |

Errori: 401 `auth.unauthenticated`, 403 `auth.forbidden`.

## 4. POST /api/catalog/property-info/save-bathhouse-configuration-services

Bottone "Salva" della tab Servizi.

- Corpo: solo `services`, cioè i **soli servizi spuntati**, ognuno con tutti i campi della sua voce della API 3
  (`isSelected` sempre `true`).

```json
{
  "services": [
    {
      "idService": 14,
      "name": "Accesso disabili",
      "categoryCode": "accessibility_comfort",
      "iconUrl": "/media/icons/accesso-disabili.svg",
      "isSelected": true
    }
  ]
}
```

- Per il backend conta solo `idService`: i servizi della lista sono quelli offerti dallo stabilimento, gli altri non lo
  sono più. `[]` li toglie tutti. È lo stesso effetto di `PUT /property-info/amenities`, ma con gli id al posto dei
  codici.
- Risposta: 204 senza corpo.
- Errori: 404 `resource.not_found` se un `idService` non è un servizio attivo del catalogo (e non si salva nulla); 401
  `auth.unauthenticated`; 403 `auth.forbidden`.

## 5. GET /api/catalog/property-info/bathhouse-configuration-media

Tab Media: il frontend la chiama la prima volta che si apre la tab e mostra la posizione sulla mappa, le foto, il link
del tour virtuale e i due PDF.

- Nessun parametro.
- Risposta 200.

```json
{
  "mainPhotoUrl": "https://media.example.com/lido-demo/spiaggia.webp",
  "photos": [
    { "idPhoto": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01", "url": "https://media.example.com/lido-demo/bar.webp" },
    { "idPhoto": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02", "url": "https://media.example.com/lido-demo/ombrelloni.webp" }
  ],
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143,
  "priceListPdfUrl": "https://media.example.com/lido-demo/listino-prezzi.pdf",
  "priceListPdfFileName": "listino-prezzi.pdf",
  "menuPdfUrl": "https://media.example.com/lido-demo/menu.pdf",
  "menuPdfFileName": "menu.pdf"
}
```

| Campo | In `PropertyInfoDto` | Note |
|---|---|---|
| `mainPhotoUrl` | `mainPhotoUrl` | Link dell'immagine principale; `null` se non c'è. |
| `photos` | `photos` | Le foto della galleria **già in ordine** (`sortOrder`), ognuna con `idPhoto` (l'`id` della foto, UUID pubblico) e `url` (link completo). Niente `sortOrder`: conta l'ordine della lista. `[]` se non ce ne sono. |
| `virtualTourUrl` | `virtualTourUrl` | |
| `latitude`, `longitude` | `latitude`, `longitude` | Colonne `catalog.bathhouses.latitude` / `longitude` (6 decimali). `null` tutti e due se la posizione non è stata scelta. |
| `priceListPdfUrl`, `menuPdfUrl` | `priceListPdf.url`, `menuPdf.url` | Solo il link; `null` se non c'è il file. |
| `priceListPdfFileName`, `menuPdfFileName` | `priceListPdf.fileName`, `menuPdf.fileName` | Il nome del file caricato, che la tab mostra; `null` se non c'è il file. |

Non ci sono: i campi delle altre tab e `slug`.

Errori: 401 `auth.unauthenticated`, 403 `auth.forbidden`.

## 6. POST /api/catalog/property-info/save-bathhouse-configuration-media

Bottone "Salva" della tab Media: salva il link del tour virtuale e la posizione sulla mappa (in modifica l'utente
clicca sulla mappa per mettere il segnaposto). Foto e PDF non passano da qui.

- Corpo: `virtualTourUrl` (`null` lo toglie) e `latitude` / `longitude` (6 decimali; `null` tutti e due = nessuna
  posizione).

```json
{
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143
}
```

- Salva **solo questi tre campi**.
- Stesse regole di `PUT /property-info`: `virtualTourUrl` link completo http o https, al massimo 500 caratteri;
  `latitude` da -90 a 90, `longitude` da -180 a 180, tutti e due o nessuno.
- Risposta: 204 senza corpo.
- Errori: 401 `auth.unauthenticated`, 403 `auth.forbidden` e 400 `validation.failed` con `errors[]`, un elemento per
  campo:

| `field` | `code` | Quando |
|---|---|---|
| `virtualTourUrl` | `validation.invalid` | Non è un link http o https. |
| `virtualTourUrl` | `validation.max_length` (`args.max` = 500) | Più di 500 caratteri. |
| `latitude` / `longitude` | `validation.out_of_range` | Fuori intervallo. |
| `latitude` / `longitude` | `validation.required` | C'è una sola delle due. |

## API esistenti usate così come sono

La tab Informazioni Generali legge le voci della tendina Nazione da `GET /api/catalog/lookups/countries`, così com'è
(`code` e `name`, nell'ordine del backend): il valore salvato in `country` è il `code`. **Non va cambiata.**

La tab Media carica e toglie foto e PDF **subito**, un file per chiamata, con le API che il backend ha già. **Non vanno
cambiate**: sono qui solo perché chi lavora sulla pagina sappia cosa il frontend si aspetta.

| Metodo | Rotta | Uso nella tab Media |
|---|---|---|
| POST | `/api/catalog/property-info/main-photo` | Carica l'immagine principale (multipart, campo `file`). |
| DELETE | `/api/catalog/property-info/main-photo` | Toglie l'immagine principale (X sull'immagine). |
| POST | `/api/catalog/property-info/photos` | Aggiunge una foto in fondo alla galleria (multipart, campo `file`). |
| DELETE | `/api/catalog/property-info/photos/{id}` | Toglie una foto (X sulla foto); `id` = `idPhoto`. |
| POST | `/api/catalog/property-info/documents/{price-list\|menu}` | Carica o sostituisce il PDF (multipart, campo `file`). |
| DELETE | `/api/catalog/property-info/documents/{price-list\|menu}` | Toglie il PDF (X accanto al documento). |

Il frontend legge dalla risposta (`PropertyInfoDto`) solo `mainPhotoUrl`, `photos` (`id`, `url`, `sortOrder`),
`priceListPdf` e `menuPdf` (`fileName`, `url`). Si aspetta questi limiti e codici, che controlla anche lui prima di
mandare il file:

| Codice | Status | Quando |
|---|---|---|
| `catalog.invalid_image` | 422 | Immagine non png, jpeg o webp, o oltre 5 MB (`args.maxBytes`, `args.contentTypes`). |
| `catalog.invalid_pdf` | 422 | File non PDF, o oltre 10 MB (`args.maxBytes`, `args.contentTypes`). |
| `catalog.gallery_full` | 422 | La galleria ha già 30 foto (`args.max`). |
| `catalog.photo_not_found` | 404 | La foto da togliere non c'è più. |

## Verifica

- Le sei API rispondono con i nomi, i tipi e gli esempi di questo documento.
- La API 3 dà a ogni servizio uno dei quattro `categoryCode` e restituisce la lista nell'ordine della tabella dei valori
  iniziali.
- Dopo ogni salvataggio, la GET della stessa tab restituisce i valori salvati; i campi delle altre tab non cambiano
  (es. salvare la tab Media non tocca nome, servizi o foto).
- Uno stabilimento senza dati risponde alle GET con `null` (o `[]` per le liste), mai con 404.
- Errori di validazione con `field` uguale al nome del campo del corpo.
- Senza token 401, senza permesso 403; lo stabilimento viene solo dal token.
