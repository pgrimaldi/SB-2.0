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

Caricamento ed eliminazione dello sfondo personalizzato e "Reimposta mappa" usano le API che ci sono già, così come sono
(sotto, "API esistenti usate così come sono"), salvo il campo `name` della sezione 3 nelle loro risposte.

## Regole comuni

- **Modulo**: le API stanno in Catalog (`Module.Catalog`), accanto alle altre della mappa (`Endpoints/MapEndpoints.cs`,
  `Application/Services/MapSettingsService.cs`), con i dati di `MapLayout` (`catalog.map_layouts`) e
  `MapLayoutSpecialIcon` (`catalog.map_layout_special_icons`).
- **Le API esistenti non cambiano**, salvo il campo `name` nelle risposte con `MapSettingsDto` (sezione 3):
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
    in `catalog.map_layout_special_icons` o in una tabella nuova), con la loro migration; il catalogo non serve;
  - **legame con il catalogo**: per ogni nome del frontend un'icona attiva di `catalog.map_icons` con lo stesso `name`
    (`varchar(255)`, oggi non unico), della categoria e dello stato giusti; si salva il suo `media_asset_id` come oggi e
    si restituisce il suo `name`. Funziona solo se il catalogo contiene già, una volta sola, tutti i nomi degli elenchi:
    va controllato sui dati, che il frontend non può vedere.

  Quale strada usare lo decide lo sviluppatore backend: l'agente gli presenta le due strade e glielo chiede prima di
  scrivere codice. Il contratto di questo documento resta fisso.

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

### Cosa fa

1. Lo sfondo della mappa diventa quello con quel nome, **non** caricato dal gestore (`isCustom` falso): lo sfondo
   caricato, se c'era, non si usa più, come oggi quando `PUT /api/catalog/map/settings` riceve `BackgroundIconId`.
   Se cancellare l'immagine caricata lo decide lo sviluppatore backend.
2. Reticolo, inclinazione, colore di sfondo, icone e `resetDeletesBookings` non cambiano.
3. Nel registro una voce `catalog.map_background_updated`, come `UploadBackgroundAsync` e `RemoveBackgroundAsync`, con
   il nome dello sfondo nei metadati.

### Risposta

204 senza corpo. Il frontend tiene la casella scelta e chiude la Modifica, senza rileggere.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 400 | `validation.failed` | `backgroundName` assente, vuoto o non nell'elenco, con `errors[]` (sopra, "Richiesta"). |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |

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
3. Come oggi `SaveSettingsAsync`: se una postazione già sulla mappa resterebbe fuori dal nuovo reticolo, 409
   `map.spots_outside_grid` con `args.rows` e `args.columns`, le misure minime che servono. Ridurre il reticolo non
   elimina postazioni.
4. Reticolo: file = `rowQuantity`, colonne = `maxRowItemQuantity`, inclinazione = `tiltDegrees`.
5. Icone: Libero e Occupato dell'ombrellone e, per ogni `variant`, Libero e Occupato dell'articolo personalizzato,
   salvati come nomi (vedi "Sfondi e icone sono nomi del frontend"); `null` toglie l'icona scelta.
6. Sfondo, colore di sfondo e `resetDeletesBookings` non cambiano.
7. Nel registro, come le tre API che sostituisce: `catalog.map_settings_updated` con `rows`, `columns` e
   `tiltDegrees`, e `catalog.map_icons_updated`.

### Risposta

204 senza corpo. Il frontend tiene i valori salvati e chiude la Modifica, senza rileggere.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 400 | `validation.failed` | Corpo non valido (sopra, "Richiesta"), con `errors[]`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
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
{{columns}} articoli per fila."), sopra i bottoni del riquadro, che resta in Modifica. Il frontend controlla da sé
numeri e nomi prima di mandarli, quindi i 400 arrivano solo da richieste fatte a mano.

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
  `backgroundColor`, `resetDeletesBookings`, e `customArticles` sempre con 8 voci, una per `variant` da 1 a 8. Dello
  sfondo caricato il frontend usa `url` per l'anteprima, come oggi.
- Vale per ogni risposta che contiene `MapSettingsDto`: `GET /api/catalog/map/settings`, `POST` e
  `DELETE /api/catalog/map/background`, `settings` di `POST /api/catalog/map/reset` e di `GET /api/catalog/map`. Il
  frontend usa le prime quattro.

```json
{
  "rows": 6,
  "columns": 16,
  "tiltDegrees": 8,
  "backgroundColor": null,
  "background": { "iconId": null, "name": "BackgroundBeach", "url": null, "isCustom": false },
  "icons": {
    "umbrella": {
      "free": { "iconId": null, "name": "Umbrella3", "url": null, "isDefault": false },
      "occupied": { "iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false }
    },
    "customArticles": [
      {
        "variant": 1,
        "icons": {
          "free": { "iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false },
          "occupied": { "iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false }
        }
      },
      {
        "variant": 2,
        "icons": {
          "free": { "iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false },
          "occupied": { "iconId": "0b7d0f7e-6f3a-4c1e-9d2b-5a8e4c3f2a10", "name": null, "url": "https://cdn.summerbooking.it/icons/ombrellone-occupato.svg", "isDefault": true }
        }
      }
    ]
  },
  "resetDeletesBookings": false
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

## API esistenti usate così come sono

Il frontend le chiama senza modifiche; qui solo come le usa e dove mostra le risposte. Le loro risposte con
`MapSettingsDto` hanno il campo `name` della sezione 3.

| Azione | API | Corpo | Risposta | Errori e dove li mostra il frontend |
|---|---|---|---|---|
| Immagine scelta o trascinata in "Sfondo personalizzato", in Modifica, subito, senza Salva | `POST /api/catalog/map/background` | multipart con il campo `file` (png, jpeg o webp, al massimo 5 MB: il frontend non manda altro) | 200 con `MapSettingsDto`: il frontend non la legge, tiene l'anteprima dell'immagine già nel browser e chiude la Modifica | Nel riquadro "Imposta sfondo", con il testo del `code` (es. 422 `map.invalid_image`, con `args.maxBytes` e `args.contentTypes`); il riquadro resta in Modifica con l'anteprima di prima. |
| Cestino sopra l'anteprima dello sfondo caricato, subito, senza conferma | `DELETE /api/catalog/map/background` | Nessuno | 200 con `MapSettingsDto`: il frontend non la legge, toglie l'anteprima e chiude la Modifica | Nel riquadro "Imposta sfondo", con il testo del `code`. |
| "Procedi ed elimina la mappa configurata" del popup "Reimposta mappa" | `POST /api/catalog/map/reset` | Nessuno | 200 con `{ settings, spots, sectors, beachElements }`: il frontend legge solo `settings` e ricompone i due riquadri senza un'altra `GET`. Il reticolo torna a 0 × 0 (`MapLayout.ClearGrid`), che il frontend mostra come campi vuoti da riempire; sfondo, icone e inclinazione restano, come dice il testo del popup | Nel popup, con il testo del `code`; il popup resta aperto. |

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

1. Su una mappa mai configurata: `background` `null` e tutti i `name` delle icone `null`.
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
