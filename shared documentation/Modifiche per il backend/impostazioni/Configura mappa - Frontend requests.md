# Configura mappa - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Configura mappa** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/map-configuration`: due riquadri, "Imposta sfondo" (lo sfondo della mappa, scelto fra quelli
del frontend o caricato dal gestore) e "Imposta reticolo" (file, articoli per fila, inclinazione e le icone di
ombrellone e articoli personalizzati), ognuno con Modifica, Annulla e Salva, e il bottone "Reimposta mappa".

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | PUT | `/api/catalog/map/update-background` | Salva lo sfondo scelto fra quelli del frontend |
| 2 | PUT | `/api/catalog/map/update-configuration` | Salva tutto il reticolo, icone comprese, in una chiamata |
| 3 | GET | `/api/catalog/map/settings` | API esistente: aggiungere il nome di sfondo e icone alla risposta |
| 4 | GET + PUT | `/api/catalog/map/settings`, `/api/catalog/map/update-background` e `/api/catalog/map/update-configuration` | Controllo di versione: due versioni nella lettura, `If-Match` ed `ETag` nei due salvataggi, 409 `resource.version_conflict` |

Caricamento ed eliminazione dello sfondo personalizzato e "Reimposta mappa" usano le API che ci sono già, così come sono
(sotto, "API esistenti usate così come sono"), salvo il campo `name` della sezione 3 e le due versioni della sezione 4
nelle loro risposte.

## Regole comuni

- **Modulo**: le API stanno in Catalog (`Module.Catalog`), accanto alle altre della mappa (`Endpoints/MapEndpoints.cs`,
  `Application/Services/MapSettingsService.cs`), con i dati di `MapLayout` (`catalog.map_layouts`) e
  `MapLayoutSpecialIcon` (`catalog.map_layout_special_icons`).
- **Le API esistenti non cambiano**, salvo il campo `name` (sezione 3) e le due versioni (sezione 4) nelle risposte
  con `MapSettingsDto`:
  `PUT /api/catalog/map/settings`, `PUT /api/catalog/map/icons/umbrella` e
  `PUT /api/catalog/map/icons/custom-articles/{variant}` restano come sono. Il frontend non le usa: le sostituiscono,
  per lui, le API 1 e 2.
- **Permesso dell'API 3**: `CatalogRead`, come oggi.
- **Permesso**: `CatalogSettings`, come `PUT /api/catalog/map/settings`. Senza permesso 403 `auth.forbidden`; senza
  token valido 401 `auth.unauthenticated`.
- **Stabilimento**: lo prende dal token (`CurrentProperty.IdAsync`), come le altre API del Catalog: nessuna richiesta lo
  contiene. Token senza stabilimento: 403 `catalog.property_required`; stabilimento che non esiste: 404
  `catalog.property_not_found`. Se lo stabilimento non ha ancora una mappa, la crea come fa oggi
  `MapSettingsService.LayoutAsync`.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono. Gli errori di validazione sono 400 `validation.failed` con un elemento di `errors[]` per
  ogni regola non rispettata (`ValidatorExtensions.EnsureValid`), con `field` uguale al nome del campo nel corpo.
  `ValidatorExtensions.ToFieldError` copia i segnaposto di FluentValidation: `validation.out_of_range` di
  `InclusiveBetween` ha `args.from` e `args.to`.

### Sfondi e icone sono nomi del frontend

Per il backend sfondi e icone sono un catalogo sul server (`GET /api/catalog/map/icon-catalog`, ognuno con un id e
l'indirizzo dell'immagine). Il frontend invece ha i suoi sfondi e le sue icone, ognuno con un **nome fisso**, e le
immagini stanno nell'app: al backend manda il nome e dal backend vuole indietro lo stesso nome.

| Elenco | Nomi |
|---|---|
| Sfondi | `BackgroundBeach`, `BackgroundBoardwalk` |
| Ombrelloni | `Umbrella1`, `Umbrella2`, … `Umbrella13` |
| Postazioni miste | `MixedSpot1`, `MixedSpot2`, … `MixedSpot36` |
| Ombrellone occupato (solo queste postazioni miste) | `MixedSpot3`, `MixedSpot4`, `MixedSpot9`, `MixedSpot13`, `MixedSpot16`, `MixedSpot18`, `MixedSpot19`, `MixedSpot32`, `MixedSpot36` |

- I nomi si confrontano così come sono scritti (maiuscole comprese), senza spazi ai lati. Un nome fuori da questi elenchi
  è un errore di validazione.
- Il backend salva il nome ricevuto (sezioni 1 e 2) senza spazi ai lati e, quando la pagina rilegge le impostazioni
  (sezione 3), restituisce esattamente il nome salvato.
- Oggi `catalog.map_layouts` e `catalog.map_layout_special_icons` tengono solo riferimenti a immagini
  (`template_asset_id`, `umbrella_free_asset_id`, `umbrella_occupied_asset_id`, `media_asset_id`, quest'ultimo non
  nullo), non nomi. Le strade possibili sono due:
  - **colonne nuove con il nome** (sfondo e due icone dell'ombrellone in `catalog.map_layouts`, le icone degli articoli
    in `catalog.map_layout_special_icons` o in una tabella nuova), con la loro migration; il catalogo non serve alla pagina
    (serve però alla legenda di Prenotazioni online: `Prenotazioni online - Frontend requests.md`, sezione 3);
  - **legame con il catalogo**: per ogni nome del frontend un'icona attiva di `catalog.map_icons` con lo stesso `name`
    (`varchar(255)`, oggi non unico), della categoria giusta; si salva il suo `media_asset_id` come oggi e si restituisce
    il suo `name`. Funziona solo se il catalogo contiene già, una volta sola, tutti i nomi degli elenchi: va controllato
    sui dati, che il frontend non può vedere. **Lo stato non conta**: un'icona ne ha uno solo (`MapIcon.Type`), mentre lo
    stesso nome può stare in Libero e in Occupato. Il controllo di oggi sullo stato (`MapSettingsService.IconAsync`) qui
    non va usato: rifiuterebbe un'icona valida con 422 `map.icon_not_allowed`, che dalle API 1 e 2 non esce mai (il
    frontend non conosce quel codice).

  Quale strada usare lo decide lo sviluppatore backend: l'agente gli presenta le due strade e glielo chiede prima di
  scrivere codice. Il contratto di questo documento resta fisso.
- **Sito pubblico e snapshot**: la stessa `MapSettingsDto` va in `GET /api/catalog/public/bathhouses/{slug}/map`
  (`PublicBathhouseService.MapAsync`, `MapProjection.MapAsync`) e nello snapshot `beach_map` (`ConfigSnapshotBuilder`).
  Le immagini dei nomi stanno solo nell'app del gestionale: con le colonne nuove il sito riceverebbe un'icona scelta con
  `url` `null` e `isDefault` falso, cioè senza immagine e senza nemmeno quella predefinita. Con **qualunque strada** ogni
  sfondo e ogni icona scelti con un nome hanno in `url` un'immagine vera. I file sono in
  `frontend/summer-booking-front-office/src/assets/images/MapConfiguration_<Nome>` (`.png` gli sfondi, `.svg` le icone):
  come caricarli fra i media lo decide lo sviluppatore backend, e l'agente glielo chiede insieme alla strada.

## 1. PUT /api/catalog/map/update-background

Bottone Salva di "Imposta sfondo", quando in Modifica si è scelta un'altra casella di "Sfondo mappa". È un'**API
nuova**: oggi lo sfondo del catalogo si sceglie solo con `PUT /api/catalog/map/settings`, insieme a reticolo, colore e
`resetDeletesBookings`, e con l'id di un'icona del catalogo.

Il frontend la chiama solo se la casella scelta è cambiata (altrimenti Salva chiude la Modifica senza chiamate). Lo
sfondo caricato dal gestore non passa da qui: si carica e si toglie subito con `POST` e `DELETE
/api/catalog/map/background`, che esistono già.

### Richiesta

```json
{
  "backgroundName": "BackgroundBoardwalk"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `backgroundName` | string | Obbligatorio: assente, `null` o vuoto dà `validation.required`. Uno dei nomi degli sfondi (`BackgroundBeach`, `BackgroundBoardwalk`); un altro valore dà `validation.invalid`. |

Header `If-Match: "<backgroundVersion>"`, con la `backgroundVersion` letta o restituita dall'ultimo salvataggio dello
sfondo (sezione 4).

### Cosa fa

1. Lo sfondo della mappa diventa quello con quel nome, **non** caricato dal gestore (`isCustom` falso): lo sfondo
   caricato, se c'era, non si usa più, come oggi quando `PUT /api/catalog/map/settings` riceve `BackgroundIconId`.
   Se cancellare l'immagine caricata lo decide lo sviluppatore backend. Con le colonne nuove si azzera anche il
   riferimento di prima (`template_asset_id`).
2. Reticolo, inclinazione, colore di sfondo, icone e `resetDeletesBookings` non cambiano.
3. Nel registro una voce `catalog.map_background_updated`, come `UploadBackgroundAsync` e `RemoveBackgroundAsync`, con
   il nome dello sfondo nei metadati.

### Risposta

204 senza corpo, con la nuova `backgroundVersion` nell'header `ETag` (sezione 4). Il frontend tiene la casella scelta,
la versione per il Salva successivo e chiude la Modifica, senza rileggere.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 400 | `validation.failed` | `backgroundName` assente, vuoto o non nell'elenco, con `errors[]` (sopra, "Richiesta"). |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 409 | `resource.version_conflict` | `If-Match` manca o non è più la `backgroundVersion` della mappa (sezione 4), senza `errors[]`. Niente cambia. |

```json
{
  "type": "https://errors.summerbooking/validation/failed",
  "title": "validation.failed",
  "status": 400,
  "code": "validation.failed",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    { "field": "backgroundName", "code": "validation.invalid", "args": {} }
  ]
}
```

Il frontend manda solo i due nomi, quindi il 400 arriva solo da richieste fatte a mano. Ogni errore di questa API
compare sopra i bottoni del riquadro "Imposta sfondo", con il testo del suo `code`; il riquadro resta in Modifica.
Il 409 `resource.version_conflict` no: lo gestisce come dice la sezione 4.

## 2. PUT /api/catalog/map/update-configuration

Bottone Salva di "Imposta reticolo": file, articoli per fila, inclinazione e le icone Libero e Occupato di Ombrellone e
degli 8 articoli personalizzati, **tutto in una chiamata**. È un'**API nuova**: oggi servono tre API
(`PUT /api/catalog/map/settings` per il reticolo, `PUT /api/catalog/map/icons/umbrella` e
`PUT /api/catalog/map/icons/custom-articles/{variant}` per le icone), che lavorano con gli id del catalogo.

Il frontend la chiama solo se nel riquadro è cambiato qualcosa, e manda sempre **tutto** il riquadro, anche le parti non
cambiate: le icone di tutti gli 8 articoli, comprese quelle vuote (`null`).

### Richiesta

```json
{
  "rowQuantity": 6,
  "maxRowItemQuantity": 16,
  "tiltDegrees": 8,
  "umbrellaIcons": { "freeIcon": "Umbrella3", "occupiedIcon": "MixedSpot4" },
  "customArticleIcons": [
    { "variant": 1, "freeIcon": "MixedSpot1", "occupiedIcon": "MixedSpot13" },
    { "variant": 2, "freeIcon": "MixedSpot8", "occupiedIcon": null },
    { "variant": 3, "freeIcon": null, "occupiedIcon": null },
    { "variant": 4, "freeIcon": null, "occupiedIcon": null },
    { "variant": 5, "freeIcon": null, "occupiedIcon": null },
    { "variant": 6, "freeIcon": null, "occupiedIcon": null },
    { "variant": 7, "freeIcon": null, "occupiedIcon": null },
    { "variant": 8, "freeIcon": null, "occupiedIcon": null }
  ]
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `rowQuantity` | number (intero) | "Numero di file". Obbligatorio (`validation.required`), da 1 a 50 (`validation.out_of_range`, `args.from` 1, `args.to` 50). Oggi `PUT /map/settings` accetta da 0 (`MapValidators.cs:14`). |
| `maxRowItemQuantity` | number (intero) | "Numero massimo articoli per fila". Obbligatorio, da 1 a 80 (`args.from` 1, `args.to` 80). Oggi da 0 (`MapValidators.cs:15`). |
| `tiltDegrees` | number (intero) | "Inclinazione mappa". Obbligatorio, da -45 a 45 (`args.from` -45, `args.to` 45), come `MapLayout.MaxTiltDegrees`. |
| `umbrellaIcons` | oggetto | Riga Ombrellone, obbligatoria. `freeIcon`: un ombrellone (`Umbrella1` … `Umbrella13`) o `null`; `occupiedIcon`: una delle 9 postazioni miste dell'ombrellone occupato o `null`. |
| `customArticleIcons` | array di oggetti | Righe "Articolo personalizzato 1" … "8": **esattamente 8** voci, una per `variant` da 1 a 8 (`MapLayoutSpecialIcon.MaxVariant`), ognuna una volta sola. `freeIcon` e `occupiedIcon`: una postazione mista (`MixedSpot1` … `MixedSpot36`) o `null`. |

`null` vuol dire "nessuna icona scelta": il frontend mostra il bottone vuoto.

Header `If-Match: "<configurationVersion>"`, con la `configurationVersion` letta o restituita dall'ultimo salvataggio
del reticolo (sezione 4).

Errori di validazione, ognuno sul suo campo: i tre numeri con `validation.required` o `validation.out_of_range`;
`umbrellaIcons` con `validation.invalid` (oggetto assente o un nome non ammesso per quella colonna);
`customArticleIcons` con `validation.invalid` (non 8 voci, un `variant` fuori da 1-8 o ripetuto, un nome non
ammesso). Gli errori dei vari campi tornano tutti insieme in `errors[]`.

`field` deve essere **esattamente** `rowQuantity`, `maxRowItemQuantity`, `tiltDegrees`, `umbrellaIcons` o
`customArticleIcons`, con un solo errore per ciascuno: il frontend mette sotto la riga solo gli errori con questi nomi.
`ValidatorExtensions.ToFieldError` usa il percorso intero della regola (`BuildingBlocks.Application/Validation/
ValidatorExtensions.cs:44`), quindi una regola scritta su un sotto-campo darebbe `umbrellaIcons.freeIcon` o
`customArticleIcons[2].freeIcon`, che il frontend non sa dove mettere e mostra come errore generico: le regole sulle
icone vanno scritte sull'oggetto `umbrellaIcons` e sulla lista `customArticleIcons` (per esempio con `Must`), con
`WithErrorCode(ValidationErrorCodes.Invalid)`.

### Cosa fa

In una sola transazione, tutto o niente:

1. Validazione: se non va, 400 e nient'altro.
2. Stabilimento dal token: 403 `catalog.property_required` / 404 `catalog.property_not_found`.
3. Controllo di versione (sezione 4): se `If-Match` manca o non è più la `configurationVersion` della mappa, 409
   `resource.version_conflict` e nient'altro.
4. Come oggi `SaveSettingsAsync`: se una postazione già sulla mappa resterebbe fuori dal nuovo reticolo, 409
   `map.spots_outside_grid` con `args.rows` e `args.columns`, le misure minime che servono. Ridurre il reticolo non
   elimina postazioni.
5. Reticolo: file = `rowQuantity`, colonne = `maxRowItemQuantity`, inclinazione = `tiltDegrees`.
6. Icone: Libero e Occupato dell'ombrellone e, per ogni `variant`, Libero e Occupato dell'articolo personalizzato,
   salvati come nomi (vedi "Sfondi e icone sono nomi del frontend"); `null` toglie l'icona scelta. Con le colonne nuove
   si azzerano anche i riferimenti di prima: `umbrella_free_asset_id`, `umbrella_occupied_asset_id` e le righe di
   `catalog.map_layout_special_icons`. Se restano, `MapProjection.SettingsAsync` (funzione `Slot`) ridà un'icona scelta
   senza nome (`isDefault` falso) e la pagina la mostra come "Icona non esistente", con Salva spento, a ogni rilettura.
7. Sfondo, colore di sfondo e `resetDeletesBookings` non cambiano.
8. Nel registro, come le tre API che sostituisce: `catalog.map_settings_updated` con `rows`, `columns` e
   `tiltDegrees`, e `catalog.map_icons_updated`.

### Risposta

204 senza corpo, con la nuova `configurationVersion` nell'header `ETag` (sezione 4). Il frontend tiene i valori
salvati, la versione per il Salva successivo e chiude la Modifica, senza rileggere.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 400 | `validation.failed` | Corpo non valido (sopra, "Richiesta"), con `errors[]`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 409 | `resource.version_conflict` | `If-Match` manca o non è più la `configurationVersion` della mappa (sezione 4), senza `errors[]`. Niente cambia. |
| 409 | `map.spots_outside_grid` | Postazioni fuori dal nuovo reticolo, con `args.rows` e `args.columns`. Niente cambia. |

```json
{
  "type": "https://errors.summerbooking/map/spots_outside_grid",
  "title": "map.spots_outside_grid",
  "status": 409,
  "code": "map.spots_outside_grid",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "args": { "rows": 4, "columns": 12 }
}
```

Dove li mostra il frontend: un errore su `rowQuantity`, `maxRowItemQuantity` o `tiltDegrees` sotto quel campo; su
`umbrellaIcons` sotto la riga Ombrellone; su `customArticleIcons` sotto le righe degli articoli personalizzati; gli
altri, `map.spots_outside_grid` compreso ("Ci sono postazioni fuori da queste misure: servono almeno {{rows}} file e
{{columns}} articoli per fila."), sopra i bottoni del riquadro, che resta in Modifica; il 409
`resource.version_conflict` come dice la sezione 4. Il frontend controlla da sé numeri e nomi prima di mandarli, quindi
i 400 arrivano solo da richieste fatte a mano.

## 3. GET /api/catalog/map/settings: il nome di sfondo e icone

All'apertura la pagina fa solo questa chiamata e con la risposta riempie i due riquadri: la casella scelta di "Sfondo
mappa", l'anteprima dello sfondo caricato, i tre campi del reticolo e l'icona di ogni bottone. È un'**API esistente**
(`MapSettingsService.GetSettingsAsync`, risposta `MapSettingsDto` costruita da `MapProjection.SettingsAsync`): rotta,
permesso ed errori restano come sono, cambia solo la risposta, con un campo in più.

### Cosa serve

Il frontend riconosce sfondi e icone **solo dal nome** (vedi "Sfondi e icone sono nomi del frontend"). Oggi la risposta
ha `iconId` e `url` del catalogo ma nessun nome, quindi la pagina non può mostrare né lo sfondo scelto né le icone
scelte. Serve un campo **`name`** nello sfondo e in ogni icona:

| Dove | Valore di `name` |
|---|---|
| `background.name` | Il nome salvato da `update-background` (sezione 1). `null` se lo sfondo è stato caricato dal gestore (`isCustom` true). Senza sfondo `background` resta `null`, come oggi. |
| `icons.umbrella.free.name` e `icons.umbrella.occupied.name` | Il nome salvato da `update-configuration` (sezione 2) per Libero e Occupato dell'Ombrellone; `null` se non c'è. |
| `icons.customArticles[].icons.free.name` e `.occupied.name` | Il nome salvato per l'articolo personalizzato con quel `variant`; `null` se non c'è. |

- `name` c'è sempre, con un nome o `null`, mai assente.
- Solo nomi degli elenchi di "Sfondi e icone sono nomi del frontend", scritti esattamente come sono stati salvati. Un
  nome fuori elenco la pagina lo mostra come "Icona non esistente" (X rossa) e non lascia salvare finché l'utente non
  lo cambia.
- `name` è `null` anche quando, per un'icona non scelta, il backend mette in `iconId`, `url` e `isDefault` la sua icona
  predefinita (funzione `Slot` di `MapProjection.SettingsAsync`, riga 59), con `isDefault` vero.
- **`isDefault` distingue i due casi di `name` `null`**, e il frontend lo usa solo per questo:
  - `isDefault` vero: nessuna icona scelta; la pagina mostra il bottone vuoto e al salvataggio manda `null`;
  - `isDefault` falso: un'icona scelta che non ha un nome del frontend (per esempio scelta con le API che usano gli id
    del catalogo); la pagina la mostra come "Icona non esistente" e non lascia salvare finché l'utente non sceglie
    un'icona o la svuota. Così `update-configuration` (sezione 2) non riceve mai `null` al posto di un'icona che
    l'utente non ha visto.
- Il resto della risposta non cambia: `iconId`, `url`, `isDefault`, `isCustom`, `rows`, `columns`, `tiltDegrees`,
  `backgroundColor`, `resetDeletesBookings`, e `customArticles` sempre con 8 voci, una per `variant` da 1 a 8. Degli
  sfondi e delle icone scelti con un nome `url` è l'immagine per sito e snapshot ("Sfondi e icone sono nomi del
  frontend"): il gestionale non la usa. Dello sfondo caricato il frontend usa `url` per l'anteprima, come oggi, e solo se
  è un link https o dell'app (`Informazioni stabilimento - Frontend requests.md`, sezione 9). In più ci sono
  `backgroundVersion` e `configurationVersion` della sezione 4 (nell'esempio sotto, in fondo).
- Vale per ogni risposta che contiene `MapSettingsDto`: `GET /api/catalog/map/settings`, `POST` e
  `DELETE /api/catalog/map/background`, `settings` di `POST /api/catalog/map/reset` e di `GET /api/catalog/map`. Il
  frontend usa le prime quattro.

```json
{
  "rows": 6,
  "columns": 16,
  "tiltDegrees": 8,
  "backgroundColor": null,
  "background": { "iconId": null, "name": "BackgroundBeach", "url": "https://cdn.summerbooking.it/backgrounds/BackgroundBeach.png", "isCustom": false },
  "icons": {
    "umbrella": {
      "free": { "iconId": null, "name": "Umbrella3", "url": "https://cdn.summerbooking.it/icons/Umbrella3.svg", "isDefault": false },
      "occupied": { "iconId": null, "name": "MixedSpot4", "url": "https://cdn.summerbooking.it/icons/MixedSpot4.svg", "isDefault": false }
    },
    "customArticles": [
      {
        "variant": 1,
        "icons": {
          "free": { "iconId": null, "name": "MixedSpot1", "url": "https://cdn.summerbooking.it/icons/MixedSpot1.svg", "isDefault": false },
          "occupied": { "iconId": null, "name": "MixedSpot13", "url": "https://cdn.summerbooking.it/icons/MixedSpot13.svg", "isDefault": false }
        }
      },
      {
        "variant": 2,
        "icons": {
          "free": { "iconId": null, "name": "MixedSpot8", "url": "https://cdn.summerbooking.it/icons/MixedSpot8.svg", "isDefault": false },
          "occupied": { "iconId": "0b7d0f7e-6f3a-4c1e-9d2b-5a8e4c3f2a10", "name": null, "url": "https://cdn.summerbooking.it/icons/ombrellone-occupato.svg", "isDefault": true }
        }
      }
    ]
  },
  "resetDeletesBookings": false,
  "backgroundVersion": "5b7e21c0",
  "configurationVersion": "e08d4a93"
}
```

Nell'esempio `customArticles` è accorciato a 2 voci (nella risposta sono sempre 8); l'Occupato dell'articolo 2 non è
scelto, quindi `name` è `null` anche se `iconId` e `url` indicano l'icona predefinita. Con lo sfondo caricato dal
gestore:

```json
"background": { "iconId": null, "name": null, "url": "https://cdn.summerbooking.it/backgrounds/sfondo-123.png", "isCustom": true }
```

### Come farlo

1. **Dove prendere i nomi**: da dove li salvano le sezioni 1 e 2, secondo la scelta fatta per "Sfondi e icone sono
   nomi del frontend" (Regole comuni). Si fa una volta per tutte e tre le sezioni.
2. **DTO** in `Application/Dtos/MapDtos.cs`, con `Name` subito dopo `IconId`:
   - `MapBackgroundDto(Guid? IconId, string? Name, string? Url, bool IsCustom)`;
   - `MapIconRefDto(Guid? IconId, string? Name, string? Url, bool IsDefault)`.
3. **`MapProjection.SettingsAsync`** (`Application/Services/MapProjection.cs`), l'unico punto che costruisce questi
   DTO:
   - riga 56, icona scelta: `Name` = il nome salvato per quella casella;
   - riga 59, nessuna icona scelta (icona predefinita del backend): `Name` = `null`;
   - riga 72, sfondo: `Name` = il nome salvato, `null` con `IsCustomTemplate`.
   Tutte le API dell'elenco sopra passano da qui, quindi non serve altro.
4. **Mappe salvate prima di questa modifica**, o con `PUT /api/catalog/map/settings` e le API `PUT /api/catalog/map/icons/...`,
   che lavorano con gli id del catalogo: `name` è il nome del frontend se la scelta del punto 1 lega le icone del
   catalogo ai nomi, altrimenti `null` con `isDefault` falso (vedi sopra). Uno sfondo del catalogo senza nome non
   sceglie nessuna casella; un'icona senza nome compare come "Icona non esistente" e l'utente ne sceglie un'altra o la
   svuota prima di salvare.

## 4. Controllo di versione sui salvataggi

Oggi, se due utenti cambiano la stessa mappa, vince chi salva per ultimo: i due riquadri rimandano tutto quello che
hanno letto all'apertura della pagina (sezioni 1 e 2) e cancellano, senza che nessuno se ne accorga, quello che un
altro utente ha cambiato nel frattempo (per esempio file e colonne aggiunte nell'editor della mappa, che "Imposta
reticolo" riporterebbe alle misure lette). Il backend non ha un controllo di concorrenza sugli aggiornamenti della
mappa: nessun token di concorrenza in `MapLayoutConfiguration` e `MapLayoutSpecialIconConfiguration`, nessun
`If-Match`. Il frontend chiede la **concorrenza ottimistica** degli altri documenti, con un **codice di errore unico per
tutte le API**: `resource.version_conflict`. È lo stesso di `Impostazioni magazzino - Frontend requests.md` (sezione 7):
se lì è già fatto, qui si aggiungono solo le API della tabella sotto.

**Non è quello che Booking ha già per le prenotazioni** (`Endpoints.cs:253-255`, `ReservationService.cs:114-115`): lì
l'`If-Match` è un numero, se manca o non si legge non si controlla niente, e il conflitto è 412 o 409
`booking.version_conflict`. Qui valgono le regole sotto: con un altro codice il frontend non rileggerebbe i dati.

### Cosa ci serve

- **Due versioni nella lettura**: una sola lettura (`GET /api/catalog/map/settings`, sezione 3) riempie due riquadri
  che si salvano ognuno per conto suo e scrivono dati diversi. Per questo la risposta (`MapSettingsDto`) ha **una
  versione per salvataggio**, due campi stringa opachi:
  - `backgroundVersion`: lo sfondo della mappa, cioè quello che scrive `update-background` (sezione 1). Cambia anche
    con `POST` e `DELETE /api/catalog/map/background` e con `PUT /api/catalog/map/settings` quando cambia lo sfondo
    (`SetBackground` in `MapSettingsService`, righe 70, 97 e 106).
  - `configurationVersion`: file, colonne, inclinazione e le icone di ombrellone e articoli personalizzati, cioè
    quello che scrive `update-configuration` (sezione 2). Cambia anche con `POST /api/catalog/map/reset`
    (`MapLayout.ClearGrid`), con l'aggiunta e l'eliminazione di file e colonne dell'editor (`/api/catalog/map/rows` e
    `/api/catalog/map/columns`, `MapEditorService.InsertLineAsync` e `DeleteLineAsync`), con
    `PUT /api/catalog/map/settings` e con `PUT /api/catalog/map/icons/umbrella` e
    `PUT /api/catalog/map/icons/custom-articles/{variant}`.
  - Ognuna cambia ogni volta che cambiano i suoi dati, e solo allora: salvare lo sfondo non cambia
    `configurationVersion` e salvare il reticolo non cambia `backgroundVersion`, altrimenti un riquadro farebbe
    fallire il Salva dell'altro.
  - **Mai `null`, anche senza una mappa salvata** (`MapSettingsService.GetSettingsAsync` lavora anche senza riga): senza
    versione il frontend non manda `If-Match` e ogni Salva darebbe 409. Il primo salvataggio con la versione letta riesce
    e crea la mappa.
  - **Riga eliminata** (`deleted_at` valorizzato, per esempio da un'importazione: nessun codice la elimina oggi): vale
    come mappa mai salvata, e il salvataggio riesce. Oggi darebbe un 500: il filtro globale la nasconde (`MapLayout` è
    `ISoftDeletable`) ma l'indice unico `uq_catalog_map_layouts_property` non ha il filtro.
- **In ogni risposta con `MapSettingsDto`**: `GET /api/catalog/map/settings`, `POST` e
  `DELETE /api/catalog/map/background`, `settings` di `POST /api/catalog/map/reset` e di `GET /api/catalog/map`.
  Dopo il caricamento o l'eliminazione dello sfondo personalizzato il frontend prende da lì la nuova
  `backgroundVersion`; dopo "Reimposta mappa" prende tutte e due le versioni da `settings`.
- **`If-Match` nei salvataggi**: `update-background` riceve `If-Match: "<backgroundVersion>"`,
  `update-configuration` riceve `If-Match: "<configurationVersion>"`. Se non è più la versione attuale di quei dati, il
  backend non salva niente e risponde **409 `resource.version_conflict`**, senza `errors[]`. Senza `If-Match` risponde
  allo stesso modo: il frontend lo manda sempre.
- **`ETag` nelle risposte**: ogni salvataggio riuscito restituisce la nuova versione dei suoi dati nell'header
  `ETag: "<version>"`, anche se risponde 204 senza corpo.
- Il 409 `resource.version_conflict` si controlla dopo validazione e stabilimento e prima di
  `map.spots_outside_grid`.
- **Controllo e salvataggio sono una cosa sola**: di due salvataggi della stessa API nello stesso momento con lo stesso
  `If-Match` uno risponde 204 e l'altro 409 `resource.version_conflict`; mai i dati di tutti e due mescolati, mai un 500.
  Vale anche per il primo in assoluto dello stabilimento, e per i due riquadri salvati insieme: riescono tutti e due.
- **Come farlo lo sceglie lo sviluppatore del backend** (un hash dei dati coperti da ogni versione, due contatori...):
  l'agente glielo chiede prima di scrivere codice. Un solo token di riga su `catalog.map_layouts` (come `xmin`) non
  basta: cambierebbe con tutti e due i salvataggi, e le icone degli articoli personalizzati
  stanno in un'altra tabella (`catalog.map_layout_special_icons`). Il frontend tratta le versioni come stringhe opache
  e non le confronta mai.

### API coinvolte

| Lettura (restituisce la versione) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| `GET /api/catalog/map/settings`, campo `backgroundVersion` | `PUT /api/catalog/map/update-background` (sezione 1) | Lo sfondo: scelto fra quelli del frontend o caricato dal gestore |
| `GET /api/catalog/map/settings`, campo `configurationVersion` | `PUT /api/catalog/map/update-configuration` (sezione 2) | File, colonne, inclinazione e icone di ombrellone e articoli personalizzati |

`POST` e `DELETE /api/catalog/map/background` e `POST /api/catalog/map/reset` non ricevono `If-Match`: non
rimandano dati letti.

### Cosa fa il frontend

Ogni riquadro tiene la sua versione e la rimanda con il suo Salva. Con 409 `resource.version_conflict` il riquadro
mostra l'avviso "Nel frattempo un altro utente ha modificato questi dati: li abbiamo ricaricati. Rifai le tue
modifiche." e rilegge con `GET /api/catalog/map/settings`, prendendo solo la sua parte: l'altro riquadro resta com'è,
con le sue modifiche non salvate.

## API esistenti usate così come sono

Il frontend le chiama senza modifiche; qui solo come le usa e dove mostra le risposte. Le loro risposte con
`MapSettingsDto` hanno il campo `name` della sezione 3 e le due versioni della sezione 4.

| Azione | API | Corpo | Risposta | Errori e dove li mostra il frontend |
|---|---|---|---|---|
| Immagine scelta o trascinata in "Sfondo personalizzato", in Modifica, subito, senza Salva | `POST /api/catalog/map/background` | multipart con il campo `file` (png, jpeg o webp, al massimo 5 MB: il frontend non manda altro) | 200 con `MapSettingsDto`: il frontend ne legge solo `backgroundVersion` (sezione 4), tiene l'anteprima dell'immagine già nel browser e chiude la Modifica | Nel riquadro "Imposta sfondo", con il testo del `code` (es. 422 `map.invalid_image`, con `args.maxBytes` e `args.contentTypes`); il riquadro resta in Modifica con l'anteprima di prima. |
| Cestino sopra l'anteprima dello sfondo caricato, in Modifica, dopo la conferma, senza Salva | `DELETE /api/catalog/map/background` | Nessuno | 200 con `MapSettingsDto`: il frontend ne legge solo `backgroundVersion` (sezione 4), toglie l'anteprima e chiude la Modifica | Nel riquadro "Imposta sfondo", con il testo del `code`. |
| "Procedi ed elimina la mappa configurata" del popup "Reimposta mappa" | `POST /api/catalog/map/reset` | Nessuno | 200 con `{ settings, spots, sectors, beachElements }`: il frontend legge solo `settings` (con le due versioni della sezione 4) e ricompone i due riquadri senza un'altra `GET`. Il reticolo torna a 0 × 0 (`MapLayout.ClearGrid`), che il frontend mostra come campi vuoti da riempire; sfondo, icone e inclinazione restano, come dice il testo del popup | Nel popup, con il testo del `code`; il popup resta aperto. |

## Verifica

Con uno stabilimento di prova e un token con `catalog.settings`:

1. `update-background` con `BackgroundBoardwalk`: 204; lo sfondo è quello, non caricato dal gestore; reticolo, icone e
   colore non cambiano; nel registro una voce `catalog.map_background_updated`.
2. Con uno sfondo caricato dal gestore, `update-background` con `BackgroundBeach`: lo sfondo diventa `BackgroundBeach` e
   quello caricato non si usa più.
3. `update-background` senza `backgroundName` o con `""`: 400 con `validation.required` su `backgroundName`; con
   `"Altro"` o `"backgroundbeach"`: 400 con `validation.invalid`.
4. `update-configuration` con l'esempio sopra: 204; reticolo 6 × 16, inclinazione 8; le icone salvate con i loro nomi,
   le voci `null` senza icona; sfondo e colore non cambiano; nel registro `catalog.map_settings_updated` e
   `catalog.map_icons_updated`.
5. Lo stesso corpo con `rowQuantity` 0, `maxRowItemQuantity` 81 e `tiltDegrees` 46: 400 con tre errori
   `validation.out_of_range`, ognuno con i suoi `args.from` e `args.to`; nulla cambia.
6. `umbrellaIcons.occupiedIcon` `MixedSpot1` (non fra le 9) o `freeIcon` `MixedSpot3`: 400 con `validation.invalid` su
   `umbrellaIcons`. `customArticleIcons` con 7 voci, con due `variant` 2 o con `variant` 9: 400 con `validation.invalid`
   su `customArticleIcons`.
7. Con una postazione in fila 5, colonna 14, `rowQuantity` 4: 409 `map.spots_outside_grid` con `args.rows` 5 e
   `args.columns` 14 (o più, se ci sono postazioni più lontane); nulla cambia, icone comprese.
8. Senza `catalog.settings` 403 `auth.forbidden` per tutte e due le API; senza token 401; token senza stabilimento 403
   `catalog.property_required`.

API 3, con un token con `catalog.read` e `catalog.settings`:

1. Su una mappa mai configurata: `background` `null`, tutti i `name` delle icone `null` e le due versioni valorizzate.
2. Dopo `update-background` con `BackgroundBoardwalk`: `background.name` `"BackgroundBoardwalk"`, `isCustom` false.
3. Dopo `POST /api/catalog/map/background` con un'immagine: la sua risposta e la `GET` hanno `background.name` `null`,
   `url` valorizzato e `isCustom` true. Dopo `DELETE /api/catalog/map/background`: `background` `null`.
4. Dopo `update-configuration` con l'esempio della sezione 2: `icons.umbrella.free.name` `"Umbrella3"`,
   `icons.umbrella.occupied.name` `"MixedSpot4"`; nella voce con `variant` 1 `"MixedSpot1"` e `"MixedSpot13"`, nella
   voce 2 `"MixedSpot8"` e `null`, nelle voci 3-8 `null` e `null`; sempre 8 voci.
5. Un'icona non scelta ha `name` `null` e `isDefault` vero, anche se `iconId` e `url` indicano l'icona predefinita.
   Un'icona scelta con `PUT /api/catalog/map/icons/umbrella` (id del catalogo) e senza nome del frontend ha `name`
   `null` e `isDefault` falso.
6. `POST /api/catalog/map/reset`: in `settings` gli stessi nomi di prima (il reset non tocca sfondo e icone).
7. Ogni `name` restituito è uno dei nomi degli elenchi, identico a quello salvato.
8. Dopo `update-background` e `update-configuration` con l'esempio: `GET /api/catalog/public/bathhouses/{slug}/map` ha lo
   sfondo e le icone con un `url` che si apre. Una mappa con icone scelte prima con gli id del catalogo, salvata con
   `update-configuration`: la rilettura non ha nessuna icona con `name` `null` e `isDefault` falso.

API 4, con un token con `catalog.read` e `catalog.settings`:

1. `GET /api/catalog/map/settings` risponde con `backgroundVersion` (per esempio `b1`) e `configurationVersion`
   (`c1`). `update-background` con `If-Match: "b1"`: 204 con l'header `ETag` di una versione nuova; una nuova `GET` ha
   quella `backgroundVersion` e ancora `configurationVersion` `c1`.
2. `update-configuration` con `If-Match: "c1"`: 204 con l'header `ETag` di una versione nuova; una nuova `GET` ha
   quella `configurationVersion` e la `backgroundVersion` di prima.
3. Lo stesso `update-background` con la versione di prima: 409 `resource.version_conflict` senza `errors[]`; lo
   sfondo non cambia. Lo stesso per `update-configuration`: reticolo e icone non cambiano.
4. `GET` (versioni `b1` e `c1`), poi `POST /api/catalog/map/rows` (una fila in più), poi `update-configuration` con
   `If-Match: "c1"`: 409 `resource.version_conflict`; la fila aggiunta resta. Poi `update-background` con
   `If-Match: "b1"`: 204.
5. `POST /api/catalog/map/background` con un'immagine: la risposta ha una `backgroundVersion` nuova e la
   `configurationVersion` di prima; `update-background` con la `backgroundVersion` della risposta: 204; con quella di
   prima del caricamento: 409.
6. `POST /api/catalog/map/reset`: `settings` ha una `configurationVersion` nuova e la `backgroundVersion` di prima.
7. `update-background` o `update-configuration` senza `If-Match`: 409 `resource.version_conflict`.
8. Mappa mai configurata, e mappa con la riga eliminata: `update-configuration` e poi `update-background` con le versioni
   lette: 204 tutti e due.
9. Due `update-configuration` nello stesso momento con lo stesso `If-Match` e valori diversi, anche su una mappa mai
   configurata: uno 204 e uno 409, mai 500.
