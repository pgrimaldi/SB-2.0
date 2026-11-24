# Informazioni stabilimento - Frontend requests

Tutte le modifiche che il backend deve fare perché la pagina **Informazioni stabilimento** del gestionale funzioni come
la vuole il frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare o
cambiare, con corpo, risposta, tipi, regole, errori e prove.

La pagina è `/settings/bathhouse-configuration` e ha tre tab, ognuna con le sue API:

| Tab | Rotta del frontend | Cosa mostra |
|---|---|---|
| Informazioni Generali | `/settings/bathhouse-configuration/1` | Nome, indirizzo, contatti, orari, cancellazione, animali, descrizione, regolamento |
| Servizi | `/settings/bathhouse-configuration/2` | I servizi del catalogo, da spuntare, divisi in quattro sezioni |
| Media | `/settings/bathhouse-configuration/3` | Posizione sulla mappa, immagini, link del tour virtuale, PDF di listino e menù |

## Riepilogo

API nuove:

| # | Metodo | Rotta | Permesso | Tab | Cosa fa |
|---|---|---|---|---|---|
| 1 | GET | `/api/catalog/property-info/bathhouse-configuration-general` | `CatalogRead` | Informazioni Generali | Legge i campi della tab |
| 2 | POST | `/api/catalog/property-info/save-bathhouse-configuration-general` | `CatalogSettings` | Informazioni Generali | Salva i campi della tab |
| 3 | GET | `/api/catalog/property-info/bathhouse-configuration-services` | `CatalogRead` | Servizi | Legge i servizi della pagina con il loro stato |
| 4 | POST | `/api/catalog/property-info/save-bathhouse-configuration-services` | `CatalogSettings` | Servizi | Salva i servizi offerti |
| 5 | GET | `/api/catalog/property-info/bathhouse-configuration-media` | `CatalogRead` | Media | Legge posizione, foto, tour virtuale e PDF |
| 6 | POST | `/api/catalog/property-info/save-bathhouse-configuration-media` | `CatalogSettings` | Media | Salva tour virtuale e posizione |

Modifiche a quello che c'è già:

| # | Dove | Cosa cambia |
|---|---|---|
| 7 | `catalog.service_amenities` (`Amenity`, `AmenityConfiguration`, nuova migration) | Sezione e ordine di ogni servizio nella tab Servizi, con i valori iniziali |
| 8 | `BathhouseConfiguration`, proprietà `PetsPolicy` | "Ammessi" scelto al primo salvataggio non deve diventare "Non ammessi" |
| 9 | `Storage:PublicBaseUrls` di ogni ambiente e file con origine `external` | Link di foto, PDF e icone sempre completi e https |

Su tutte e sei le API nuove:

| # | Dove | Cosa cambia |
|---|---|---|
| 10 | API 1-6 | Controllo di versione, una versione per tab: `version` nelle letture, `If-Match` ed `ETag` nei salvataggi, 409 `resource.version_conflict` |

Foto e PDF si caricano e si tolgono con le API che il backend ha già, senza modifiche (sezione "API esistenti usate
così come sono").

## Regole comuni

Valgono per le sei API nuove.

- **Modulo e rotte**: Catalog (`Module.Catalog`), nel gruppo `/api/catalog/property-info` di
  `Endpoints/PropertyInfoEndpoints.cs`, con la logica in `PropertyInfoService`, accanto alle API che ci sono già.
  Leggono e scrivono la stessa scheda di `GET /property-info` (`PropertyInfoDto`).
- **Le API esistenti restano**: rotte, corpi e risposte di `/api/catalog/property-info` (`GET` e `PUT /`,
  `PUT /amenities`, `POST` e `DELETE /main-photo`, `POST /photos`, `PUT /photos/order`, `DELETE /photos/{id}`, `POST` e
  `DELETE /documents/{document}`) e di `/api/catalog/lookups/*` non cambiano. Le correzioni 8 e 9 valgono anche per
  loro, perché toccano la stessa entità e gli stessi link.
- **Stabilimento dal token**: nessuna richiesta lo contiene; si usa `CurrentProperty.IdAsync`, come oggi. Token senza
  stabilimento: 403 `catalog.property_required`; stabilimento del token che non c'è in `catalog.properties`: 404
  `catalog.property_not_found`; tutti e due senza `errors[]`. Il frontend ha questi due codici in 7 lingue e li mostra
  nel messaggio della tab.
- **Una scheda per stabilimento**: il database non lo impedisce (l'indice `ix_catalog_bathhouses_property` su
  `property_id` non è unico), quindi le sei API lavorano sulla scheda che usa oggi `property-info`: la prima per id tra
  quelle non cancellate (`PropertyInfoRepository.FindBathhouseAsync`, `OrderBy(b => b.Id)`). Senza scheda le GET
  rispondono con i valori di partenza (sezioni 1, 3 e 5), mai 404; il primo salvataggio (API 2, 4 o 6) o il primo
  caricamento di un file la crea (`PropertyInfoService.BathhouseAsync`, `Bathhouse.Create`), con
  `isFreeCancellation` = `false`, `PetsPolicy.OnRequest` e tutto il resto vuoto.
- **Permessi**: le GET con `Permissions.CatalogRead` (`catalog.read`), come `GET /property-info`; i salvataggi con
  `Permissions.CatalogSettings` (`catalog.settings`), come `PUT /property-info`. Senza token valido: 401
  `auth.unauthenticated`; senza permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Nelle risposte ogni campo c'è **sempre**, anche quando
  vale `null`: il frontend rimanda nei salvataggi i campi che ha letto, e un campo mancante nella lettura mancherebbe
  anche nel salvataggio.
  - `petPolicy` è un testo con il **nome .NET** del membro di `PetsPolicy`: `Allowed`, `NotAllowed`, `OnRequest`. Non i
    codici `allowed`, `not_allowed`, `on_request` che usa oggi `PropertyInfoDto` (`Codes.Of`).
  - `categoryCode` dei servizi (API 3) non è un enum .NET del contratto: è un codice snake_case (es.
    `accessibility_comfort`), da mandare così.
  - Gli orari sono testi `HH:mm` (es. `"08:30"`), senza secondi.
- **Salvataggi**: rispondono 204 senza corpo, con la nuova versione nell'header `ETag` (sezione 10), e salvano **solo
  i campi della loro tab**; il resto della scheda resta com'è. Non vanno fatti richiamando `PUT /property-info`, che
  riscrive tutta la scheda (anche posizione e tour virtuale).
- **Ordine dei controlli**, uguale per tutte: 1) token e permesso (401, 403 `auth.forbidden`); 2) corpo JSON leggibile
  (altrimenti 400: il frontend manda sempre JSON valido, quindi `code` ed `errors[]` di questo 400 non contano); 3)
  validazione del corpo (400 `validation.failed`); 4) stabilimento dal token (403 `catalog.property_required`, 404
  `catalog.property_not_found`); 5) i controlli sui dati della singola API (422 della API 2, 404 della API 4); 6)
  versione in `If-Match` dei salvataggi (409 `resource.version_conflict`, sezione 10); 7) salvataggio. Se un controllo
  fallisce non si salva nulla.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code` e `traceId`, più
  `args` ed `errors[]` quando ci sono. Gli errori di validazione sono 400 `validation.failed` con `errors[]`: un
  elemento per ogni regola non rispettata (`ValidatorExtensions.EnsureValid`), con `field` = nome del campo **nel corpo
  di questa API** (es. `bathhouseName`, non `name`), `code` e `args` (`{}` se non ce ne sono). Il frontend mostra ogni
  errore sotto il suo campo; un errore con un `field` che la tab non ha finisce nel messaggio della tab. Esempio:

```json
{
  "type": "https://errors.summerbooking/validation/failed",
  "title": "validation.failed",
  "status": 400,
  "code": "validation.failed",
  "traceId": "00-4f1c2b7e9a0d4e3f8a6b5c4d3e2f1a0b-7c6d5e4f3a2b1c0d-00",
  "errors": [
    { "field": "cancellationNoticeHours", "code": "validation.out_of_range", "args": { "from": 1, "to": 720 } },
    { "field": "petPolicy", "code": "validation.invalid", "args": {} }
  ]
}
```

- **`args` degli errori di campo**: `ValidatorExtensions.ToFieldError`
  (`BuildingBlocks.Application/Validation/ValidatorExtensions.cs`) copia sempre tutti i segnaposto che FluentValidation
  dà alla regola, quindi `validation.max_length` e `validation.password_too_long` hanno `args.max` e anche
  `args.min` = 0; `validation.min_length` e `validation.password_too_short` `args.min` e anche `args.max` = -1; una
  lunghezza esatta (`Length(2)`) `args.min` e `args.max` uguali; una regola di confronto (`GreaterThan`,
  `GreaterThanOrEqualTo`, anche con un altro `WithErrorCode`) `args.value` con il valore confrontato; `InclusiveBetween`
  `args.from` e `args.to` (provato con FluentValidation 12.1.1, la versione del backend). Il frontend usa solo gli args
  dei suoi testi, quelli scritti nelle tabelle di questo documento.

## 1. GET /api/catalog/property-info/bathhouse-configuration-general

Tab Informazioni Generali: il frontend la chiama la prima volta che si apre la tab e con la risposta riempie i campi.
Restituisce **solo i campi della tab**, con i nomi e i valori del frontend.

- Nessun parametro.
- Risposta 200 con i 20 campi qui sotto, sempre tutti, e `version` (sezione 10). Uno stabilimento senza scheda
  risponde con tutti i campi `null`, tranne `isFreeCancellation` = `false` e `petPolicy` = `OnRequest`: sono i valori di
  `Bathhouse.Create` e quelli che dà oggi `GET /property-info` senza scheda (`PropertyInfoService.ToDtoAsync`, lì
  scritto `on_request`).

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
  "regulation": "<p>Vietato <strong>fumare</strong> in spiaggia.</p>",
  "version": "5f1c2a9e"
}
```

Tipi e provenienza, rispetto a `PropertyInfoDto` (cioè all'entità `Bathhouse`):

| Campo | Tipo | In `PropertyInfoDto` | Note |
|---|---|---|---|
| `bathhouseName` | string o `null` | `name` | Nome della scheda (`catalog.bathhouses.name`). |
| `address`, `city`, `province`, `region` | string o `null` | stessi nomi | |
| `postalCode` | string o `null` | `cap` | |
| `country` | string o `null` | `countryCode` | Codice ISO 3166-1 alpha-2 in maiuscolo (es. `IT`), da `catalog.countries.code`. |
| `phone`, `whatsapp`, `email`, `website`, `facebook`, `instagram` | string o `null` | stessi nomi | |
| `description`, `regulation` | string o `null` | stessi nomi | HTML dell'editor del frontend, vedi sotto. |
| `openingTime`, `closingTime` | string `HH:mm` o `null` | `openHoursFrom`, `openHoursTo` | `TimeOnly` scritto con `ToString("HH:mm", CultureInfo.InvariantCulture)`; non il formato di `TimeOnly` in JSON (`"08:30:00"`). |
| `isFreeCancellation` | boolean, mai `null` | `isFreeCancellation` | |
| `cancellationNoticeHours` | integer o `null` | `cancellationNoticeHours` | La tendina del frontend sceglie `true` con 48 o 24 ore, oppure `false` con `null`. Una combinazione diversa (es. `true` con 72) lascia la tendina vuota e il salvataggio la rimanda così com'è. |
| `petPolicy` | string, mai `null` | `petsPolicy` | `bathhouse.PetsPolicy.ToString()`: `Allowed`, `NotAllowed`, `OnRequest`. |
| `version` | string, mai `null` | — | La versione dei 20 campi (sezione 10). Il frontend la rimanda nell'header `If-Match` della API 2, non nel corpo. |

HTML di `description` e `regulation`: l'editor (TipTap) produce solo `<p>`, `<br>`, `<strong>`, `<em>`, `<u>`, `<ul>`,
`<ol>`, `<li>` (con il testo dentro un `<p>`) e `<a>`. Attributi: `<a>` ha `href` (un link o vuoto, mai
`javascript:`), `target` e `rel` (ai link creati con il bottone l'editor mette `target="_blank"` e
`rel="noopener noreferrer nofollow"`) e, nei link incollati, `title` e `class`; `<ol>` può avere `start` e `type`
(elenchi incollati).
Oggi il backend non controlla l'HTML e lo salva com'è (toglie solo gli spazi ai lati): deve continuare ad accettarlo
così.

Non ci sono: `slug`, `latitude`, `longitude`, `virtualTourUrl`, `mainPhotoUrl`, `photos`, `menuPdf`, `priceListPdf` e
`amenityCodes` (sono di altre tab o la pagina non li usa).

Errori: solo quelli delle regole comuni (401, 403 `auth.forbidden`, 403 `catalog.property_required`, 404
`catalog.property_not_found`).

## 2. POST /api/catalog/property-info/save-bathhouse-configuration-general

Bottone "Salva" della tab Informazioni Generali.

- Corpo: **tutti** i 20 campi della tab, cambiati o no, con gli stessi nomi, tipi e valori della API 1. Il frontend:
  - manda `null` per un campo di testo vuoto e non toglie gli spazi ai lati;
  - manda `null` per `description` e `regulation` quando l'editor è vuoto;
  - se la API 1 ha dato `country` = `null`, propone la prima nazione di `GET /lookups/countries` (se la lista è
    arrivata) e manda quella;
  - manda `isFreeCancellation` e `petPolicy` sempre (parte da `false` e `OnRequest`, come il backend).

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

- Tipi nel DTO della richiesta, perché i valori sbagliati diventino errori di validazione e non errori di lettura del
  JSON: `petPolicy` string (non l'enum), `openingTime` e `closingTime` string, `isFreeCancellation` `bool?`,
  `cancellationNoticeHours` `short?`, gli altri string.
  - `petPolicy` vale solo se è esattamente uno dei nomi di `Enum.GetNames<PetsPolicy>()` (maiuscole comprese; un numero
    come `"0"` non vale).
  - Un orario vale solo con `TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture,
    DateTimeStyles.None, out _)`.
- Salva **solo questi campi**: posizione, tour virtuale, foto, PDF e servizi restano come sono. Se si riusa
  `Bathhouse.Update`, latitudine, longitudine e tour virtuale vanno passati con i valori attuali della scheda.
- Effetti, gli stessi di `PUT /property-info` di oggi (`PropertyInfoService.SaveAsync`):
  - se lo stabilimento non ha ancora la scheda, la crea (`Bathhouse.Create`) e poi la aggiorna;
  - `bathhouseName` diventa il nome della scheda e anche quello dello stabilimento (`catalog.properties.name`,
    `Property.Rename`), senza spazi ai lati: è il nome che il resto dell'app mostra;
  - slug: questa API non lo manda; se la scheda non ne ha ancora uno (anche se creata prima dalla API 4, dalla API 6 o
    da un caricamento di file) si genera dal nome (`Slugs.From`, con `-2`, `-3`… se è già usato, anche da schede
    cancellate); poi non cambia;
  - `country` diventa anche la nazione dello stabilimento (`catalog.properties.country_id`); con `null` lo stabilimento
    tiene quella di prima e la scheda resta senza nazione;
  - `region` resta testo libero; la regione della piattaforma (`catalog.bathhouses.region_id`) si ricalcola dal testo
    con `AssignRegionAutomatically` (la prima di `ListRegionsAsync` che corrisponde al testo, altrimenti `null`), a meno
    che non sia stata scelta a mano (`is_region_manual`);
  - i testi si salvano senza spazi ai lati e un testo vuoto o di soli spazi diventa `null` (`Clean`);
  - nel log resta l'azione `catalog.property_info_updated`, con `name` e `slug`.
- Risposta: 204 senza corpo.
- Validazione: le stesse regole di `PUT /property-info` (`SavePropertyInfoRequestValidator`) per gli stessi dati, con i
  nomi del corpo qui sopra, più le righe segnate **Nuova**. Le lunghezze si contano sul testo ricevuto, prima di
  togliere gli spazi ai lati. Ogni regola dà al massimo un errore (`CascadeMode.Stop`, `Boot/Program.cs`), ma un campo
  con due regole può averne due (es. `cancellationNoticeHours` = 800 con `isFreeCancellation` = `false`).

| `field` | `code` | `args` | Quando |
|---|---|---|---|
| `bathhouseName` | `validation.required` | | `null`, vuoto o di soli spazi. |
| `bathhouseName`, `address`, `city`, `province`, `region`, `email`, `website`, `facebook`, `instagram` | `validation.max_length` | `max` = 255 | Più di 255 caratteri. |
| `postalCode` | `validation.max_length` | `max` = 16 | Più di 16 caratteri. |
| `phone`, `whatsapp` | `validation.max_length` | `max` = 32 | Più di 32 caratteri. |
| `phone`, `whatsapp` | `validation.phone` | | Fino a 32 caratteri, ma non solo cifre e spazi. |
| `email` | `validation.email` | | Non vuota, fino a 255 caratteri, e senza esattamente una `@` o con la `@` all'inizio o alla fine. |
| `description`, `regulation` | `validation.max_length` | `max` = 20000 | HTML più lungo di 20.000 caratteri. Il frontend limita a 5.000 i caratteri di testo visibile, non l'HTML: con molta formattazione questo errore può arrivare, e va sotto il campo. |
| `country` | `validation.invalid` | `min` = 2, `max` = 2; il frontend non li legge | Non `null` e non lungo 2 caratteri (regola `Length(2)`: FluentValidation dà `ExactLengthValidator`, che `ValidatorExtensions` traduce in `validation.invalid`). |
| `openingTime`, `closingTime` | `validation.invalid` | | **Nuova.** Non `null` e non nel formato `HH:mm` (da `00:00` a `23:59`). |
| `closingTime` | `validation.out_of_range` | `value` = `openingTime` come lo confronta la regola (es. `"08:30:00"` con `TimeOnly`); il frontend non lo legge | Con tutti e due gli orari validi, `closingTime` non è dopo `openingTime` (uguale non vale). |
| `isFreeCancellation` | `validation.required` | | **Nuova.** Mancante o `null`. |
| `cancellationNoticeHours` | `validation.invalid` | | Non `null` con `isFreeCancellation` = `false`. Con `isFreeCancellation` mancante o `null` c'è solo l'errore su `isFreeCancellation` (sotto); la regola qui sotto vale comunque. |
| `cancellationNoticeHours` | `validation.out_of_range` | `from` = 1, `to` = 720 | Non `null` e fuori da 1-720. |
| `petPolicy` | `validation.invalid` | | Mancante, `null` o diverso da `Allowed`, `NotAllowed`, `OnRequest` (la regola di oggi controlla i codici `allowed`…; qui valgono i nomi .NET). |

- Nazione: dopo la validazione e lo stabilimento, un `country` di 2 caratteri senza una nazione non cancellata con quel
  codice (confronto in maiuscolo, `PropertyInfoRepository.FindCountryIdAsync`; `is_active` non conta) risponde 422
  `catalog.country_not_found`, senza `errors[]`, e non si salva nulla. Il frontend lo mostra sotto Nazione.
- Errori: 401 `auth.unauthenticated`, 403 `auth.forbidden`, 400 `validation.failed` (tabella sopra), 403
  `catalog.property_required`, 404 `catalog.property_not_found`, 422 `catalog.country_not_found`, 409
  `resource.version_conflict` (sezione 10), in quest'ordine.

## 3. GET /api/catalog/property-info/bathhouse-configuration-services

Tab Servizi: il frontend la chiama la prima volta che si apre la tab e mostra una casella per servizio. Restituisce
un oggetto con **una sola lista**, `services`, con i servizi della pagina, ognuno con il suo stato per lo stabilimento,
e `version` (sezione 10).

- Nessun parametro. I nomi sono nella lingua dell'header `Accept-Language`, che il frontend manda sempre con la lingua
  scelta nell'app.
- Risposta 200: in `services` i servizi attivi del catalogo che hanno una sezione (sezione 7), **nell'ordine della
  pagina**: per `settings_sort_order`, a parità per `id`. Uno stabilimento senza scheda riceve la stessa lista con
  `isSelected` = `false`.

```json
{
  "services": [
    {
      "idService": 14,
      "name": "Accesso disabili",
      "categoryCode": "accessibility_comfort",
      "iconUrl": "https://media.example.com/icons/accesso-disabili.svg",
      "isSelected": true
    },
    {
      "idService": 38,
      "name": "Wifi",
      "categoryCode": "accessibility_comfort",
      "iconUrl": null,
      "isSelected": false
    }
  ],
  "version": "9b04d7e1"
}
```

`version` (string, mai `null`) è la versione della selezione (sezione 10): non cambia con la lingua della richiesta.
Ogni elemento di `services`:

| Campo | Tipo | Da dove viene |
|---|---|---|
| `idService` | integer | `catalog.service_amenities.id`. Il frontend riconosce i servizi da questo id. |
| `name` | string, mai `null` | Il nome nella lingua della richiesta, come `GET /lookups/amenity-categories`: `Translations.Pick` sulle righe di `catalog.service_amenity_translations` (lingua della richiesta, poi italiano, poi inglese), altrimenti `catalog.service_amenities.name`. |
| `categoryCode` | string, mai `null` | La sezione della pagina: `Codes.Of(amenity.SettingsSection)`, cioè `accessibility_comfort`, `sport_activities`, `food_extra` o `beach` (sezione 7). Non sono le categorie di `GET /lookups/amenity-categories`. |
| `iconUrl` | string o `null` | Link dell'icona (`icon_asset_id`), costruito come i link dei file (`CatalogMedia.UrlsAsync`); completo e https (sezione 9); `null` solo se il servizio non ha icona. Il frontend lo mette così com'è in `<img src>`. |
| `isSelected` | boolean | `true` se il servizio è tra le dotazioni della scheda (`catalog.bathhouse_amenities` non cancellate). |

Il frontend mette ogni servizio nella sezione del suo `categoryCode`, nell'ordine fisso `accessibility_comfort`,
`sport_activities`, `food_extra`, `beach`, e dentro la sezione nell'ordine della lista.

Errori: solo quelli delle regole comuni.

## 4. POST /api/catalog/property-info/save-bathhouse-configuration-services

Bottone "Salva" della tab Servizi.

- Corpo: solo `services` (array, obbligatorio), con **tutti i servizi letti dalla API 3**, ognuno con tutti i campi
  della sua voce e `isSelected` come l'ha lasciato l'utente: `true` se lo stabilimento offre il servizio, `false` se
  non lo offre. Il frontend rimanda sempre tutto il modello letto, mai solo i servizi spuntati o cambiati.

```json
{
  "services": [
    {
      "idService": 14,
      "name": "Accesso disabili",
      "categoryCode": "accessibility_comfort",
      "iconUrl": "https://media.example.com/icons/accesso-disabili.svg",
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
}
```

- Per il backend contano `idService` (integer) e `isSelected` (boolean); gli altri campi si ignorano.
- Ogni elemento decide da solo: con `isSelected` = `true` il servizio diventa (o resta) una dotazione della scheda, con
  `false` non lo è più. Un servizio che non è nella lista resta com'è: il backend non toglie mai una dotazione perché
  manca dal corpo. Così le dotazioni di servizi che la API 3 non restituisce (non attivi o senza sezione), che il
  frontend non ha letto, non cambiano mai.
  Si salva con `Bathhouse.SetAmenities` passando le dotazioni di prima, più gli id con `true`, meno gli id con `false`;
  a differenza di `PUT /property-info/amenities`, che sostituisce tutte le dotazioni con quelle ricevute.
- `services` mancante o `null` è un errore e non si salva nulla.
- `isSelected` mancante o non boolean in un elemento non `null`: 400 `validation.required` su
  `services[i].isSelected` e non si salva nulla. Un elemento `null` non dà errori di validazione: risponde 404 dopo i
  controlli del corpo (tabella sotto).
- Lo stesso `idService` ripetuto nella lista: 400 `validation.invalid` su `services` e non si salva nulla (non si
  saprebbe quale `isSelected` vale).
- Al massimo 200 elementi, come `PUT /property-info/amenities`.
- Effetti: se lo stabilimento non ha la scheda, la crea; nel log resta l'azione `catalog.property_amenities_updated`,
  con `codes` = i `code` di tutte le dotazioni della scheda dopo il salvataggio (quelle della lista più quelle non
  mostrate che restano), cioè l'insieme salvato, come registra `SetAmenitiesAsync`.
- Risposta: 204 senza corpo.
- Errori, in quest'ordine (in ogni caso non si salva nulla): 401 `auth.unauthenticated`, 403 `auth.forbidden`, poi:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` con `errors[]` = `[{ "field": "services", "code": "validation.required", "args": {} }]` | `services` mancante o `null`. |
| 400 | `validation.failed` con `errors[]` = `[{ "field": "services", "code": "validation.invalid", "args": {} }]` | Più di 200 elementi, o lo stesso `idService` ripetuto. |
| 400 | `validation.failed` con `errors[]` = `[{ "field": "services[i].isSelected", "code": "validation.required", "args": {} }]` | `isSelected` mancante o non boolean nell'elemento `i`, non `null`. I tre 400 arrivano insieme in `errors[]`, prima di ogni 404. |
| 403, 404 | `catalog.property_required`, `catalog.property_not_found` | Regole comuni. |
| 404 | `resource.not_found`, senza `errors[]` | Un elemento `null` o senza `idService`, o con un `idService` che la API 3 non restituirebbe (non esiste, è cancellato, non è attivo o non ha sezione). |
| 409 | `resource.version_conflict`, senza `errors[]` | La versione in `If-Match` non è più quella della selezione (sezione 10). Niente cambia. |

Per gli id sbagliati vale 404 `resource.not_found`, non il 422 `catalog.amenity_not_found` di
`PUT /property-info/amenities`, che oggi non guarda `is_active` (`PropertyInfoRepository.FindAmenityIdsAsync`). Il
frontend mostra tutti gli errori di questa API nel messaggio della tab, tranne il 409 `resource.version_conflict`
(sezione 10).

## 5. GET /api/catalog/property-info/bathhouse-configuration-media

Tab Media: il frontend la chiama la prima volta che si apre la tab e mostra la posizione sulla mappa, le foto, il link
del tour virtuale e i due PDF.

- Nessun parametro.
- Risposta 200 con i 9 campi qui sotto, sempre tutti, e `version` (sezione 10). Uno stabilimento senza scheda risponde
  con `photos` = `[]` e gli altri 8 campi `null`.

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
  "menuPdfFileName": "menu.pdf",
  "version": "c27e0b43"
}
```

| Campo | Tipo | In `PropertyInfoDto` | Note |
|---|---|---|---|
| `mainPhotoUrl` | string o `null` | `mainPhotoUrl` | Link dell'immagine principale (sezione 9); `null` se non c'è. |
| `photos` | array, mai `null` | `photos` | Le foto della galleria **già in ordine** di `sortOrder`, ognuna `{ idPhoto, url }`: `idPhoto` string UUID = `id` della foto (`PublicId`), `url` string = link della foto (sezione 9). Niente `sortOrder`: conta l'ordine della lista. `[]` se non ce ne sono. |
| `virtualTourUrl` | string o `null` | `virtualTourUrl` | |
| `latitude`, `longitude` | number o `null` | `latitude`, `longitude` | Colonne `numeric(9,6)`. `null` tutti e due se la posizione non è stata scelta. |
| `priceListPdfUrl`, `menuPdfUrl` | string o `null` | `priceListPdf.url`, `menuPdf.url` | Link del PDF (sezione 9); `null` se non c'è il file. |
| `priceListPdfFileName`, `menuPdfFileName` | string o `null` | `priceListPdf.fileName`, `menuPdf.fileName` | Il nome del file, che la tab mostra; `null` se non c'è il file. Viene dal percorso salvato (`Path.GetFileName`): il nome caricato con i caratteri diversi da lettere, cifre, `.`, `-` e `_` cambiati in `_` (`S3ObjectStorage.SaveAsync`, `BuildingBlocks.Infrastructure/Storage/ObjectStorage.cs`); `file.pdf` se il file non si trova in `catalog.media_assets`. |
| `version` | string, mai `null` | — | La versione di `virtualTourUrl`, `latitude` e `longitude` (sezione 10): foto e PDF non la cambiano. Il frontend la rimanda nell'header `If-Match` della API 6. |

Non ci sono: i campi delle altre tab e `slug`.

Errori: solo quelli delle regole comuni.

## 6. POST /api/catalog/property-info/save-bathhouse-configuration-media

Bottone "Salva" della tab Media: salva il link del tour virtuale e la posizione sulla mappa (in modifica l'utente
clicca sulla mappa per mettere il segnaposto). Foto e PDF non passano da qui.

- Corpo: sempre tutti e tre i campi. `virtualTourUrl` string o `null` (`null` lo toglie; il frontend toglie gli spazi
  ai lati e manda `null` per un link vuoto); `latitude` e `longitude` number o `null` (tutti e due `null` = nessuna
  posizione; il frontend li arrotonda a 6 decimali).

```json
{
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143
}
```

- Salva **solo questi tre campi**: `virtualTourUrl` senza spazi ai lati, con un link vuoto (`""`) che diventa `null`
  (`Clean`, come `PUT /property-info`; un link di soli spazi è un errore di validazione, tabella sotto); latitudine e
  longitudine così come arrivano (il database tiene 6 decimali).
- Non si può riusare `Bathhouse.Update`: riscrive tutti i campi e vuole il nome (`info.Name.Trim()`), che una scheda
  creata da un caricamento di file non ha. Serve un metodo di `Bathhouse` che cambia solo questi tre campi.
- Effetti: se lo stabilimento non ha la scheda, la crea; nel log resta l'azione `catalog.property_info_updated`, con
  `name` e `slug` della scheda (anche `null`), come `PUT /property-info`.
- Risposta: 204 senza corpo.
- Validazione: le regole di `PUT /property-info` per gli stessi dati.

| `field` | `code` | `args` | Quando |
|---|---|---|---|
| `virtualTourUrl` | `validation.max_length` | `max` = 500 | Più di 500 caratteri. |
| `virtualTourUrl` | `validation.invalid` | | Non vuoto, fino a 500 caratteri, e non un link assoluto `http` o `https` (`SavePropertyInfoRequestValidator.BeHttpUrl`). |
| `latitude` | `validation.out_of_range` | `from` = -90, `to` = 90 | Non `null` e fuori da -90/90. |
| `longitude` | `validation.out_of_range` | `from` = -180, `to` = 180 | Non `null` e fuori da -180/180. |
| `latitude` | `validation.required` | | `null` con `longitude` non `null`. |
| `longitude` | `validation.required` | | `null` con `latitude` non `null`. |

- Errori: 401 `auth.unauthenticated`, 403 `auth.forbidden`, 400 `validation.failed` (tabella sopra), 403
  `catalog.property_required`, 404 `catalog.property_not_found`, 409 `resource.version_conflict` (sezione 10), in
  quest'ordine. Il frontend mostra gli errori di `latitude` e `longitude` sotto la mappa e quelli di `virtualTourUrl`
  sotto il campo.

## 7. Sezione e ordine dei servizi

La pagina mostra i servizi in quattro sezioni fisse, in quest'ordine:

| `categoryCode` | Sezione |
|---|---|
| `accessibility_comfort` | Accessibilità e Comfort |
| `sport_activities` | Sport e attività |
| `food_extra` | Ristorazione e extra |
| `beach` | Spiaggia |

**Sezione e ordine di ogni servizio li tiene il backend nel suo database**: il frontend non ha nessuna tabella di id.

### Modifiche al database e a EF

- Nuovo enum PostgreSQL `catalog.amenity_settings_section` con le etichette `accessibility_comfort`,
  `sport_activities`, `food_extra`, `beach`. In C#: `Domain/Enums/AmenitySettingsSection.cs` con `AccessibilityComfort`,
  `SportActivities`, `FoodExtra`, `Beach`, registrato come gli altri enum del modulo: `HasPostgresEnum` in
  `Infrastructure/DbContext.cs` e `MapEnum` nei due metodi `Apply` di `Infrastructure/Infras/CatalogEnumMappings.cs`.
- Due colonne nuove su `catalog.service_amenities`, tutte e due nullable:
  - `settings_section` (`catalog.amenity_settings_section`): sezione della tab Servizi; `NULL` = il servizio non è
    nella pagina;
  - `settings_sort_order` (`smallint`): posizione nella pagina; si legge in ordine crescente.
- Vincolo `ck_catalog_service_amenities_settings_section`: `(settings_section IS NULL) = (settings_sort_order IS NULL)`.
- `Amenity`: proprietà `SettingsSection` (`AmenitySettingsSection?`) e `SettingsSortOrder` (`short?`);
  `AmenityConfiguration`: le due colonne, con `HasComment` come le altre, e il vincolo.
- `category_id` e `sort_order` non si toccano: servono alla ricerca pubblica e a `GET /lookups/amenity-categories`.

### Come le usano le API 3 e 4

- La API 3 legge i servizi con `is_active = true`, non cancellati e con `settings_section` non `NULL`, ordinati per
  `settings_sort_order` e poi per `id`, con le traduzioni; `categoryCode` = `Codes.Of(SettingsSection)`.
- La API 4 accetta solo gli id di quello stesso insieme.
- Un servizio nuovo del catalogo (es. arrivato con l'ETL) resta senza sezione, quindi fuori dalla pagina, finché non gli
  si danno `settings_section` e `settings_sort_order`.

### Valori iniziali

Si caricano nella stessa migration che aggiunge le colonne, con SQL (`migrationBuilder.Sql`). Sono le sezioni e l'ordine
che la pagina mostra oggi con i dati di prova del frontend. La colonna `idService` riporta gli id di
`catalog.service_amenities` del database locale: sono colonne `IDENTITY ALWAYS`, assegnate dal database nell'ordine di
inserimento (i servizi arrivano con l'ETL), quindi in un altro ambiente lo stesso servizio può avere un altro id. Il
`code` non aiuta: oggi è provvisorio (`amenity_<id>`, migration `20261025191523_CatalogLookups`).

Per ogni riga della tabella la migration trova il servizio così:

1. **Controllo dell'id**: il servizio con `id` = `idService` e `deleted_at IS NULL` vale se il suo `name`, o il `name`
   di una sua traduzione italiana (`catalog.service_amenity_translations` con `deleted_at IS NULL` e `culture` = `it` o
   che inizia con `it-`), è uguale al testo della colonna "Servizio". Il confronto è su
   `lower(btrim(replace(testo, '’', '''')))` dei due lati: non contano maiuscole, spazi ai lati e il tipo di apostrofo.
2. **Ricerca per nome**: altrimenti vale l'unico servizio con `deleted_at IS NULL` il cui nome o traduzione italiana è
   uguale nello stesso modo.
3. **Nessuno o più di uno**: la migration si ferma con un errore (`RAISE EXCEPTION`) che dice il nome della riga, e
   non si carica nulla (la migration gira in una transazione).

A quel servizio la migration dà `settings_section` = `categoryCode` e `settings_sort_order` = "Ordine". Se
`catalog.service_amenities` non ha righe con `deleted_at IS NULL` (database appena creato, servizi non ancora caricati
dall'ETL) la migration non carica nulla e non dà errore. Come caricare sezione e ordine quando i servizi arrivano dopo
la migration (es. con l'ETL in un ambiente nuovo) lo decide lo sviluppatore backend: l'agente che implementa questo
punto glielo chiede prima di scrivere codice.

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

## 8. Correzione: "Ammessi" al primo salvataggio

Problema, oggi: la colonna `catalog.bathhouses.pets_policy` ha il default `'not_allowed'` (`BathhouseConfiguration`:
`HasDefaultValueSql("'not_allowed'")`, senza sentinel) e nell'enum `PetsPolicy` `Allowed` vale 0, il default del CLR.
In un `INSERT` EF Core non manda una proprietà con un default del database quando ha il valore di default del CLR, e
lascia scegliere al database. Quando il primo salvataggio crea la scheda (`Bathhouse.Create` e poi `Update`, nello
stesso `SaveChanges`) con `Allowed`, la riga nasce con `not_allowed`. Succede anche oggi con `PUT /property-info`; gli
aggiornamenti (`UPDATE`) e le schede create da un caricamento di file (`OnRequest`) non hanno il problema.

Correzione: in `Infrastructure/Configurations/BathhouseConfiguration.cs`, sulla proprietà `PetsPolicy`, aggiungere
`.HasSentinel((PetsPolicy)(-1))`. Nessun valore dell'enum vale -1, quindi EF manda sempre il valore nell'`INSERT`. Il
default della colonna resta `'not_allowed'` e lo schema non cambia: non serve una migration.

Prova:

1. Uno stabilimento senza scheda: `SELECT count(*) FROM catalog.bathhouses WHERE property_id = <id> AND deleted_at IS
   NULL` dà 0.
2. API 2 con `"petPolicy": "Allowed"` e un nome: 204.
3. API 1: `"petPolicy": "Allowed"`.
4. `SELECT pets_policy FROM catalog.bathhouses WHERE property_id = <id> AND deleted_at IS NULL`: `allowed`.
5. Lo stesso con `PUT /property-info` (un nome e `"petsPolicy": "allowed"`) su un altro stabilimento senza scheda: la
   risposta ha `"petsPolicy": "allowed"`.

## 9. Link di foto, PDF e icone

Valgono per ogni link di un file che la pagina riceve: `iconUrl` (API 3); `mainPhotoUrl`, `photos[].url`,
`priceListPdfUrl` e `menuPdfUrl` (API 5); `mainPhotoUrl`, `photos[].url`, `priceListPdf.url` e `menuPdf.url` nelle
risposte delle API di foto e PDF.

Oggi: il backend li costruisce con `CatalogMedia.UrlsAsync` → `MediaUrls.Resolve` → `PublicFileUrlResolver.Resolve`,
cioè indirizzo dell'origine del file in `Storage:PublicBaseUrls` + `/` + percorso. Se l'origine non è in
`Storage:PublicBaseUrls` il link è `null`; se il suo indirizzo è http il link è http; l'origine `external` ha già il
link intero nel percorso e lo restituisce così com'è. Le chiavi di configurazione corrispondono ai valori di
`catalog.media_assets.origin` così (`MediaOrigins`): `s3us_east2` → `s3_us_east_2`, `s3us_east1` → `s3_us_east_1`,
`cloudfront` → `cloudfront`; `external` non ha chiave. `Boot/appsettings.Development.json` ha
`"s3_us_east_2": "http://localhost:4566/summerbooking-nuovo"` (http); `Boot/appsettings.json` non ha
`Storage:PublicBaseUrls` (lo dà la configurazione di ogni ambiente). I file caricati dalla pagina vanno sull'origine di
`Catalog:Media:Origin` (`s3_us_east_2`).

Cosa fa il frontend: in `src` e `href` usa solo link https o dell'app stessa (`fileLinkOf`), nella tab Media e per le
icone della tab Servizi. Una foto della galleria, l'immagine principale o un PDF con un link diverso restano visibili
senza anteprima (il PDF con il nome del file, senza "Apri il documento"), con la X per cancellarli; una foto conta nei
30 posti. Un link `null` vuol dire file assente. Un'icona con un link diverso non si mostra.

Richiesta, in **ogni ambiente, sviluppo compreso**:

1. `Storage:PublicBaseUrls` ha un indirizzo `https://` per ogni origine che compare in `catalog.media_assets.origin`
   (tranne `external`). In sviluppo LocalStack risponde in https anche su `https://localhost.localstack.cloud:4566`.
2. I file con origine `external` hanno come percorso un link `https://`.
3. Un file che esiste ha sempre il suo link: mai `null`.

## 10. Controllo di versione sui salvataggi delle tre tab

Oggi, se due utenti modificano la stessa tab, vince chi salva per ultimo: ogni Salva rimanda tutto il modello letto
(sezioni 2, 4 e 6) e cancella, senza che nessuno se ne accorga, quello che un altro utente ha cambiato nel frattempo.
Il frontend chiede lo schema di **concorrenza ottimistica** che il backend ha già progettato per le prenotazioni offline
(`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un **codice di errore
unico per tutte le API**: `resource.version_conflict`.

### Cosa ci serve

- **Una versione per tab**: ogni lettura della tabella sotto restituisce, insieme ai dati, il campo `version` (stringa
  opaca, mai `null`). Copre **solo i dati che scrive il salvataggio della sua tab**: cambia ogni volta che cambiano
  quei dati, anche quando li cambia un'altra API (per esempio `PUT /property-info` o `PUT /property-info/amenities`),
  e solo allora. Così il Salva di una tab non fa mai fallire quello di un'altra.
- Per questo non basta una versione della riga di `catalog.bathhouses` (per esempio il token `xmin`): sulla stessa riga
  (`Domain/Entities/Bathhouse.cs`) scrivono le API 2 e 6, i caricamenti dell'immagine principale e dei PDF
  (`PropertyInfoService`, `SetMainPhoto`, `SetMenuPdf`, `SetPriceListPdf`) e i "mi piace"
  (`IdentityEventHandlers`, `Bathhouse.ChangeLikes`); le dotazioni della API 4 stanno invece in
  `catalog.bathhouse_amenities`.
- **`If-Match` nei salvataggi**: il frontend manda la versione letta nell'header `If-Match: "<version>"`, mai nel
  corpo. Se non è più quella attuale, il backend non salva niente e risponde **409 `resource.version_conflict`**, senza
  `errors[]`. Senza `If-Match` risponde allo stesso modo: il frontend lo manda sempre.
- **`ETag` nelle risposte**: ogni salvataggio riuscito risponde 204 senza corpo con la nuova versione nell'header
  `ETag: "<version>"`. Il frontend la tiene per il Salva successivo della stessa tab, senza rileggere.
- Il 409 si controlla dopo i controlli sui dati della singola API (422 `catalog.country_not_found` della API 2, 404
  `resource.not_found` della API 4) e subito prima del salvataggio (regole comuni, "Ordine dei controlli").
- **Stabilimento senza scheda**: le letture restituiscono comunque una `version`, quella dei valori di partenza, e il
  primo salvataggio con quella versione riesce e crea la scheda. Una scheda creata nel frattempo da un'altra API senza
  cambiare i dati della tab (per esempio dal caricamento di una foto) non cambia la versione.
- La API 3 risponde un oggetto `{ "services": [...], "version": "..." }` e non più la sola lista (sezione 3): una
  lista non può portare la versione.
- **Come farlo lo sceglie lo sviluppatore del backend** (un hash dei dati coperti, un contatore per tab...): l'agente
  glielo chiede prima di scrivere codice. Il frontend tratta la versione come una stringa opaca e non la confronta mai.

### API coinvolte

| Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| API 1 `GET /api/catalog/property-info/bathhouse-configuration-general` | API 2 `POST /api/catalog/property-info/save-bathhouse-configuration-general` | I 20 campi della API 1 |
| API 3 `GET /api/catalog/property-info/bathhouse-configuration-services` | API 4 `POST /api/catalog/property-info/save-bathhouse-configuration-services` | Quali servizi della API 3 sono dotazioni della scheda (`isSelected`); non nomi, icone e sezioni |
| API 5 `GET /api/catalog/property-info/bathhouse-configuration-media` | API 6 `POST /api/catalog/property-info/save-bathhouse-configuration-media` | `virtualTourUrl`, `latitude` e `longitude`; non foto e PDF |

Le API di foto e PDF ("API esistenti usate così come sono") restano come sono e non mandano `If-Match`: caricano e
tolgono un file subito, e non cambiano la versione della API 5.

### Cosa fa il frontend

Con 409 `resource.version_conflict` la tab mostra l'avviso "Nel frattempo un altro utente ha modificato questi dati:
li abbiamo ricaricati. Rifai le tue modifiche.", torna in sola lettura e rilegge i suoi dati con la sua lettura (API 1,
3 o 5); il messaggio in fondo alla tab non compare.

## API esistenti usate così come sono

La tab Informazioni Generali legge le voci della tendina Nazione da `GET /api/catalog/lookups/countries`, così com'è
(`LookupService.ListCountriesAsync`, aperta anche senza token): `code` e `name` delle nazioni attive, per `sort_order` e
poi codice, con il nome nella lingua di `Accept-Language` (altrimenti italiano, inglese o il codice). Il valore salvato
in `country` è il `code`. **Non va cambiata.**

La tab Media carica e toglie foto e PDF **subito**, un file per chiamata, con le API che il backend ha già. **Non vanno
cambiate** (salvo i link, sezione 9): sono qui solo perché chi lavora sulla pagina sappia cosa il frontend si aspetta.

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
stabilimento non ha ancora la scheda, la crea la prima chiamata riuscita. Il frontend legge dalla risposta tutti i
media: `mainPhotoUrl`, `photos` (`id`, `url`, `sortOrder`), `virtualTourUrl`, `latitude`, `longitude`,
`priceListPdf` e `menuPdf` (`fileName`, `url`). La tab Media e la sua cache prendono questi campi dalla risposta,
quindi anche link del tour e posizione devono restare nella risposta con i valori salvati, come oggi
(`PropertyInfoService.ToDtoAsync`). Si aspetta questi limiti e codici, che controlla anche lui prima di mandare il
file:

| Codice | Status | Quando |
|---|---|---|
| `catalog.invalid_image` | 422 | Immagine non png, jpeg o webp, vuota o oltre 5 MB (`args.maxBytes`, `args.contentTypes`). |
| `catalog.invalid_pdf` | 422 | File non PDF, vuoto o oltre 10 MB (`args.maxBytes`, `args.contentTypes`). |
| `catalog.gallery_full` | 422 | La galleria ha già 30 foto (`args.max`). |
| `catalog.photo_not_found` | 404 | La foto da togliere non c'è più. |

## Verifica

Con uno stabilimento di prova e un token con `catalog.read` e `catalog.settings`:

1. **Senza scheda**: la API 1 risponde con i 20 campi, tutti `null` tranne `"isFreeCancellation": false` e
   `"petPolicy": "OnRequest"`; la API 3 con la lista e `isSelected` sempre `false`; la API 5 con `"photos": []` e gli
   altri 8 campi `null`. Mai 404.
2. **API 2**: dopo un 204 la API 1 restituisce i valori mandati, con gli orari `"HH:mm"` (non `"08:30:00"`) e
   `petPolicy` col nome .NET; `catalog.properties.name` = `bathhouseName` senza spazi ai lati; al primo salvataggio
   `catalog.bathhouses.slug` non è più `NULL`; `catalog.properties.country_id` è la nazione di `country`; latitudine,
   longitudine, tour virtuale, foto, PDF e servizi non cambiano.
3. **"Ammessi" al primo salvataggio**: la prova della sezione 8.
4. **Validazione della API 2**: il corpo dell'esempio con `postalCode` di 17 caratteri, `petPolicy` = `"allowed"`,
   `openingTime` = `"8:30"`, `isFreeCancellation` = `null` e `cancellationNoticeHours` = `null` risponde 400 con
   quattro elementi in `errors[]`, con `field` `postalCode`, `petPolicy`, `openingTime` e `isFreeCancellation`; `country` = `"ZZ"` risponde 422 `catalog.country_not_found`; in
   tutti e due i casi la API 1 restituisce i valori di prima.
5. **API 3**: nel database locale restituisce i 48 servizi della tabella della sezione 7, in quell'ordine e con quel
   `categoryCode`; con `Accept-Language: en` i nomi sono in inglese dove c'è la traduzione; nessun servizio senza
   sezione o non attivo.
6. **API 4**: mandando tutti i servizi della API 3 con due a `true` e gli altri a `false`, la API 3 dà quei due
   `true` e gli altri `false`; con tutti a `false` la API 3 li dà tutti `false`; un servizio tolto dal corpo resta come
   era; senza `services`, con un `isSelected` mancante o con un id ripetuto 400 `validation.failed`, con un
   `idService` che non esiste 404 `resource.not_found`, e in tutti questi casi la API 3 dà la selezione di prima; una
   dotazione di un servizio non attivo o senza sezione, presente prima, c'è ancora dopo il salvataggio (`SELECT` su
   `catalog.bathhouse_amenities` della scheda).
7. **API 6**: dopo un 204 la API 5 restituisce link e posizione mandati e gli stessi `mainPhotoUrl`, `photos`,
   `priceListPdfUrl`, `priceListPdfFileName`, `menuPdfUrl` e `menuPdfFileName` di prima (foto e PDF non cambiano), e la
   API 1 gli stessi valori di prima; su uno
   stabilimento che ha la scheda solo da un caricamento di foto (nome `NULL`) risponde 204, non 500; con la sola
   `latitude` risponde 400 con `field` `longitude` e `code` `validation.required`. Dopo un 204, una chiamata riuscita a
   una API di foto e PDF (es. `POST /api/catalog/property-info/photos`) risponde con i `virtualTourUrl`, `latitude` e
   `longitude` appena salvati.
8. **Link**: in ogni ambiente, sviluppo compreso, ogni link di icone, foto e PDF della API 3, della API 5 e delle API di
   foto e PDF comincia con `https://`, e nessun file che esiste ha il link `null`.
9. **Accesso**: senza token 401 `auth.unauthenticated`; senza permesso 403 `auth.forbidden`; token senza stabilimento
   403 `catalog.property_required`; stabilimento del token che non esiste 404 `catalog.property_not_found`.
10. **Una scheda**: tutte le API lavorano sulla stessa scheda di `GET /property-info` (la prima per id tra le non
    cancellate).

Controllo di versione (sezione 10), con lo stesso stabilimento e lo stesso token:

11. La API 1 risponde con `version` (per esempio `v1`); la API 2 con `If-Match: "v1"` risponde 204 con l'header `ETag`
    di una versione nuova, diversa da `v1`; una nuova API 1 risponde con quella versione.
12. La stessa API 2 con `If-Match: "v1"` dopo la prima: 409 `resource.version_conflict` senza `errors[]`, e la API 1
    restituisce i valori di prima. La API 2 senza `If-Match`: 409 `resource.version_conflict`.
13. Lette le versioni delle API 1, 3 e 5: dopo un salvataggio riuscito della API 4 le versioni delle API 1 e 5 non
    cambiano; dopo uno della API 6 non cambiano quelle delle API 1 e 3; dopo uno della API 2 non cambiano quelle delle
    API 3 e 5.
14. Letta la versione della API 5, un caricamento e una cancellazione dell'immagine principale, di una foto e di un PDF
    non la cambiano: la API 6 con quella versione risponde 204.
15. Letta la API 3 (versione `v1`), `PUT /api/catalog/property-info/amenities` cambia le dotazioni; la API 4 con
    `If-Match: "v1"` risponde 409 `resource.version_conflict` e le dotazioni restano quelle della `PUT`. Lo stesso per
    la API 2 e la API 6 dopo una `PUT /api/catalog/property-info` che cambia i loro dati.
16. La API 3 con `Accept-Language: it` e con `Accept-Language: en` restituisce la stessa `version`.
17. Su uno stabilimento senza scheda la API 2 con la `version` della API 1 risponde 204 e crea la scheda.
