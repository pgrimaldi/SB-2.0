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
  richiesta lo contiene. Si usa `CurrentProperty`, come oggi: un token senza stabilimento risponde 403
  `catalog.property_required`, uno stabilimento che non esiste più 404 `catalog.property_not_found` (senza `errors[]`).
  Il frontend ha già questi due codici, con il loro messaggio in 7 lingue, e li mostra come errore della tab.
- **Una scheda per stabilimento**: il database non lo impedisce (l'indice su `catalog.bathhouses.property_id` non è
  unico), quindi tutte le sei API leggono e scrivono la stessa scheda di `property-info`, cioè la prima per id
  (`PropertyInfoRepository.FindBathhouseAsync`, `OrderBy(b => b.Id)`, tra le schede non cancellate). Senza scheda le
  GET rispondono con i valori di partenza e il primo salvataggio, o il primo caricamento di un file, la crea
  (`PropertyInfoService.BathhouseAsync`, `Bathhouse.Create`). Facoltativo: un indice unico su `property_id` per le
  schede non cancellate, così le schede in più non possono nascere.
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
- Risposta 200. Uno stabilimento che non ha ancora salvato la scheda risponde con tutti i campi `null`, tranne
  `isFreeCancellation` = `false` e `petPolicy` = `OnRequest`, come oggi `GET /property-info` e `Bathhouse.Create`.

```json
{
  "bathhouseName": "Lido Demo",
  "address": "Lungomare dei Pini 12",
  "postalCode": "00100",
  "city": "Roma",
  "province": "Roma",
  "region": "Lazio",
  "country": "IT",
  "phone": "06 1234567",
  "whatsapp": "333 1234567",
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
| `isFreeCancellation`, `cancellationNoticeHours` | stessi nomi | Il frontend usa solo `true` con 48 o 24 ore, oppure `false` con `null`. `isFreeCancellation` mai `null`. |
| `petPolicy` | `petsPolicy` | `Allowed`, `NotAllowed`, `OnRequest` (al posto di `allowed`, `not_allowed`, `on_request`); mai `null`. |

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
  "phone": "06 1234567",
  "whatsapp": "333 1234567",
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

- Salva **solo questi campi**: posizione, tour virtuale, foto, PDF e servizi restano come sono; lo slug anche, tranne
  il primo salvataggio (vedi sotto).
- Effetti del salvataggio, gli stessi di `PUT /property-info` di oggi (`PropertyInfoService.SaveAsync`):
  - se lo stabilimento non ha ancora la scheda, la crea (`Bathhouse.Create`) e poi la aggiorna;
  - `bathhouseName` rinomina anche lo stabilimento (`catalog.properties.name`, senza spazi ai lati): è il nome che il
    resto dell'app mostra;
  - slug: se la scheda non ne ha uno, si genera dal nome (`Slugs.From`, con `-2`, `-3`… se è già usato); dopo non
    cambia più, perché questa API non lo manda;
  - `country` diventa anche la nazione dello stabilimento (`catalog.properties.country_id`); con `null` lo stabilimento
    tiene quella di prima e la scheda resta senza nazione;
  - `region` resta testo libero; la regione della piattaforma si assegna da sola dal testo
    (`AssignRegionAutomatically`), a meno che non sia stata scelta a mano;
  - i testi si salvano senza spazi ai lati e un testo vuoto o di soli spazi diventa `null`;
  - nel log resta l'azione `catalog.property_info_updated`, con nome e slug.
- Stesse regole di validazione di `PUT /property-info` per gli stessi dati. `bathhouseName` è obbligatorio (come
  `name`); `isFreeCancellation` e `petPolicy` arrivano sempre (il frontend parte da `false` e `OnRequest`, come il
  backend); gli altri campi possono arrivare `null`.
- Risposta: 204 senza corpo.
- Errori: 401 `auth.unauthenticated`, 403 `auth.forbidden` e 400 `validation.failed` con `errors[]`, un elemento per
  campo, con i nomi del corpo qui sopra (es. `postalCode`, non `cap`; `closingTime`, non `openHoursTo`):

| `field` | `code` | Quando |
|---|---|---|
| `bathhouseName` | `validation.required` | Vuoto o solo spazi. |
| `bathhouseName`, `address`, `city`, `province`, `region`, `website`, `facebook`, `instagram` | `validation.max_length` (`args.max` = 255) | Più di 255 caratteri. |
| `postalCode` | `validation.max_length` (`args.max` = 16) | Più di 16 caratteri. |
| `email` | `validation.max_length` (`args.max` = 255) | Più di 255 caratteri. |
| `email` | `validation.email` | Non vuota e senza esattamente una `@`, o con la `@` all'inizio o alla fine. |
| `description`, `regulation` | `validation.max_length` (`args.max` = 20000) | HTML più lungo di 20.000 caratteri. |
| `phone`, `whatsapp` | `validation.max_length` (`args.max` = 32) | Più di 32 caratteri. |
| `phone`, `whatsapp` | `validation.phone` | Caratteri diversi da cifre e spazi. |
| `country` | `validation.length` (`args.min` = `args.max` = 2) | Non è un codice di 2 lettere. |
| `closingTime` | `validation.out_of_range` | Con tutti e due gli orari, `closingTime` non è dopo `openingTime`. |
| `cancellationNoticeHours` | `validation.invalid` | Non `null` con `isFreeCancellation` = `false`. |
| `cancellationNoticeHours` | `validation.out_of_range` (`args.from` = 1, `args.to` = 720) | Fuori da 1-720. |
| `petPolicy` | `validation.invalid` | Mancante o diverso da `Allowed`, `NotAllowed`, `OnRequest`. |

- Una nazione di 2 lettere che non è nel catalogo (nessuna riga di `catalog.countries` con quel codice, confrontato in
  maiuscolo: `PropertyInfoRepository.FindCountryIdAsync`): 422 `catalog.country_not_found`, senza `errors[]`, e non si
  salva nulla. Il frontend lo mostra sotto Nazione.
- **Da verificare e correggere: "Ammessi" al primo salvataggio.** La colonna `catalog.bathhouses.pets_policy` ha il
  default `'not_allowed'` (`BathhouseConfiguration`: `HasDefaultValueSql("'not_allowed'")`, senza `HasSentinel`) e
  nell'enum `PetsPolicy` `Allowed` vale 0, il default del CLR. In un `INSERT` EF Core non manda una proprietà con un
  default del database quando ha il valore di default del CLR, e lascia scegliere al database. Quando il primo
  salvataggio crea la scheda (`Bathhouse.Create` e `Update` nello stesso `SaveChanges`) con `petPolicy` = `Allowed`, la
  riga nasce quindi con `not_allowed`. Gli aggiornamenti successivi (`UPDATE`) non hanno il problema; succede anche con
  `PUT /property-info`.
  - Correzione: EF deve mandare sempre il valore, es. `.HasSentinel((PetsPolicy)(-1))` sulla proprietà, oppure senza
    `HasDefaultValueSql` (con la sua migration). Facoltativo: default della colonna `'on_request'`, come
    `Bathhouse.Create` e la GET di uno stabilimento senza scheda.
  - Prova: stabilimento senza scheda, API 2 con `"petPolicy": "Allowed"`, poi la API 1 risponde
    `"petPolicy": "Allowed"`.

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
| `idService` | `catalog.service_amenities.id` (es. 14 per "Accesso disabili" nel database locale). Il frontend riconosce i servizi da questo id. |
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

Valori iniziali, da caricare nel database (sono quelli che la pagina mostra oggi). La colonna `idService` riporta gli id
di `catalog.service_amenities` del database locale: sono colonne `IDENTITY ALWAYS`, assegnate dal database nell'ordine
di inserimento (i servizi arrivano con l'ETL), quindi in un altro ambiente lo stesso servizio può avere un altro id. Il
`code` non aiuta: oggi è provvisorio (`amenity_<id>`, migration `20261025191523_CatalogLookups`: "codice provvisorio,
quello vero arriva dall'ETL").

- Prima di caricare la tabella, in ogni ambiente il backend controlla che ogni id corrisponda al servizio della colonna
  "Servizio" (`catalog.service_amenities.name` o la sua traduzione `it`, a meno di maiuscole o apostrofi); se non
  corrisponde, il servizio si cerca per nome e vale l'id che ha in quell'ambiente.
- Quando dall'ETL arriveranno i codici veri, sezione e ordine si caricano per `code` invece che per id.
- Le API 3 e 4 restano con `idService`.

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
- `services` mancante o `null` **non** vale come `[]`: è un errore e non si salva nulla, così una richiesta incompleta
  non toglie mai tutti i servizi.
- Lo stesso `idService` ripetuto vale una volta sola (come i codici ripetuti di `PUT /property-info/amenities`).
- Al massimo 200 servizi, come `PUT /property-info/amenities`.
- Risposta: 204 senza corpo.
- Errori (in ogni caso non si salva nulla): 401 `auth.unauthenticated`, 403 `auth.forbidden` e:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` con `errors[]` = `{ "field": "services", "code": "validation.required" }` | `services` mancante o `null`. |
| 400 | `validation.failed` con `errors[]` = `{ "field": "services", "code": "validation.invalid" }` | Più di 200 servizi. |
| 404 | `resource.not_found` | Un elemento senza `idService`, o con un `idService` che non è un servizio attivo del catalogo. |

Per gli id sbagliati vale 404 `resource.not_found` senza `errors[]`, non il 422 `catalog.amenity_not_found` di
`PUT /property-info/amenities`, che oggi non guarda nemmeno `is_active` (`PropertyInfoRepository.FindAmenityIdsAsync`).

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
| `priceListPdfFileName`, `menuPdfFileName` | `priceListPdf.fileName`, `menuPdf.fileName` | Il nome del file caricato, che la tab mostra; `null` se non c'è il file. Oggi viene dal percorso salvato (`Path.GetFileName`): il nome caricato con i caratteri diversi da lettere, cifre, `.`, `-` e `_` cambiati in `_` (`ObjectStorage.SaveAsync`), `file.pdf` se il file non si trova. |

Non ci sono: i campi delle altre tab e `slug`.

Link dei file (`mainPhotoUrl`, `photos[].url`, `priceListPdfUrl`, `menuPdfUrl`; vale anche per le risposte delle API
esistenti di foto e PDF):

- Oggi il backend li costruisce con `PublicFileUrlResolver` (indirizzo pubblico dell'origine del file + percorso): se
  l'origine non è in `Storage:PublicBaseUrls` il link è `null`, e se il suo indirizzo è http il link è http (l'origine
  `external` ha già il link intero nel percorso, così com'è).
- Il frontend mette nelle anteprime e nei link solo indirizzi https (o dell'app stessa). Una foto della galleria con
  `url` `null` o non https resta al suo posto senza anteprima, con la X per cancellarla, e conta nei 30 posti.
  L'immagine principale o un PDF con un link così si vedono come assenti, senza X; caricandone un altro si
  sostituiscono.
- Richiesta: in ogni ambiente ogni link è completo e https (`Storage:PublicBaseUrls` con indirizzi https per tutte le
  origini usate), e non è mai `null` per un file che esiste.

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

- Salva **solo questi tre campi**. Come `PUT /property-info`, `virtualTourUrl` si salva senza spazi ai lati e un link
  vuoto diventa `null`.
- Stesse regole di `PUT /property-info`: `virtualTourUrl` link completo http o https, al massimo 500 caratteri;
  `latitude` da -90 a 90, `longitude` da -180 a 180, tutti e due o nessuno.
- Risposta: 204 senza corpo.
- Errori: 401 `auth.unauthenticated`, 403 `auth.forbidden` e 400 `validation.failed` con `errors[]`, un elemento per
  campo:

| `field` | `code` | Quando |
|---|---|---|
| `virtualTourUrl` | `validation.invalid` | Non è un link http o https. |
| `virtualTourUrl` | `validation.max_length` (`args.max` = 500) | Più di 500 caratteri. |
| `latitude` / `longitude` | `validation.out_of_range` (`args.from` e `args.to`: -90 e 90, -180 e 180) | Fuori intervallo. |
| `latitude` / `longitude` | `validation.required` | C'è una sola delle due. |

## API esistenti usate così come sono

La tab Informazioni Generali legge le voci della tendina Nazione da `GET /api/catalog/lookups/countries`, così com'è
(`LookupService.ListCountriesAsync`, aperta anche senza accesso): `code` e `name` delle nazioni attive, per `sort_order`
e poi codice, con il nome nella lingua di `Accept-Language` (altrimenti italiano, inglese o il codice). Il valore
salvato in `country` è il `code`. **Non va cambiata.**

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

Rispondono 200 con la scheda (`PropertyInfoDto`) e chiedono `CatalogSettings` (senza: 403 `auth.forbidden`) e lo
stabilimento dal token, come le API nuove (403 `catalog.property_required`, 404 `catalog.property_not_found`); se lo
stabilimento non ha ancora la scheda, la crea la prima chiamata riuscita. Il frontend legge dalla risposta solo
`mainPhotoUrl`, `photos` (`id`, `url`, `sortOrder`), `priceListPdf` e `menuPdf` (`fileName`, `url`). Si aspetta questi
limiti e codici, che controlla anche lui prima di mandare il file:

| Codice | Status | Quando |
|---|---|---|
| `catalog.invalid_image` | 422 | Immagine non png, jpeg o webp, vuota o oltre 5 MB (`args.maxBytes`, `args.contentTypes`). |
| `catalog.invalid_pdf` | 422 | File non PDF, vuoto o oltre 10 MB (`args.maxBytes`, `args.contentTypes`). |
| `catalog.gallery_full` | 422 | La galleria ha già 30 foto (`args.max`). |
| `catalog.photo_not_found` | 404 | La foto da togliere non c'è più. |

## Verifica

- Le sei API rispondono con i nomi, i tipi e gli esempi di questo documento.
- La API 3 dà a ogni servizio uno dei quattro `categoryCode` e restituisce la lista nell'ordine della tabella dei valori
  iniziali, caricata con gli id di ogni ambiente controllati sul nome del servizio.
- Dopo ogni salvataggio, la GET della stessa tab restituisce i valori salvati; i campi delle altre tab non cambiano
  (es. salvare la tab Media non tocca nome, servizi o foto).
- Uno stabilimento senza dati risponde alle GET con `null` (o `[]` per le liste), mai con 404.
- Errori di validazione con `field` uguale al nome del campo del corpo.
- La API 4 senza `services` risponde 400 e lascia i servizi come sono.
- Il primo salvataggio della API 2 crea lo slug; nome e nazione dello stabilimento seguono `bathhouseName` e `country`.
- Il primo salvataggio della API 2 con `petPolicy` = `Allowed` salva `Allowed`: la API 1 lo restituisce.
- Senza token 401, senza permesso 403; lo stabilimento viene solo dal token: senza stabilimento 403
  `catalog.property_required`, stabilimento inesistente 404 `catalog.property_not_found`.
- Tutte le API lavorano sulla stessa scheda di `property-info` (la prima per id).
- I link di foto e PDF sono sempre completi e https.
