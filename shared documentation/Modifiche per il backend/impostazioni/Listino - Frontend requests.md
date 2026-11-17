# Listino - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Listino** del gestionale funzioni come la vuole il frontend.
Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload, risposta,
regole ed errori attesi.

La pagina è `/settings/catalog`, con due tab: "Pacchetti" (`/settings/catalog/1`) e "Articoli/Servizi aggiuntivi"
(`/settings/catalog/2`). Ognuna ha una tabella (cestino, duplica e matita per riga, cancella e duplica selezionati) e
un pannello laterale per aggiungere e uno per modificare. I pannelli dei pacchetti hanno le tab "Info Generali" e
"Listino"; quelli degli articoli aggiuntivi hanno i loro campi e la stessa tab "Listino" (prezzi stagionali, prezzi per
periodo, festivi, prezzi intermedi).

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/catalog/packages-list` | Tabella Pacchetti, con il nome nella lingua dell'utente |
| 2 | POST | `/api/catalog/delete-package-item` | Cancella uno o più pacchetti, tutti o nessuno |
| 3 | POST | `/api/catalog/duplicate-package-item` | Duplica uno o più pacchetti, tutti o nessuno |
| 4 | POST | `/api/catalog/map/sector-area-combobox` | Tendina "Aree abilitate" dei pannelli dei pacchetti |
| 5 | POST | `/api/catalog/packages/create-package` | Crea un pacchetto con le due tab del pannello |
| 6 | GET | `/api/catalog/packages/{idPackage}` | Legge un pacchetto per il pannello di modifica |
| 7 | POST | `/api/catalog/packages/update-package` | Salva un pacchetto modificato |
| 8 | POST | `/api/catalog/additional-services-articles-list` | Tabella Articoli/Servizi aggiuntivi |
| 9 | POST | `/api/catalog/delete-additional-services-article-item` | Cancella uno o più articoli aggiuntivi, tutti o nessuno |
| 10 | POST | `/api/catalog/duplicate-additional-services-article-item` | Duplica uno o più articoli aggiuntivi, tutti o nessuno |
| 11 | POST | `/api/catalog/package-combobox` | Tendina "Pacchetti" dei pannelli degli articoli aggiuntivi |
| 12 | POST | `/api/warehouse/article-combobox` | Tendina "Seleziona articolo" (anche Impostazioni magazzino) |
| 13 | POST | `/api/catalog/create-additional-services-article-item` | Crea un articolo aggiuntivo |
| 14 | GET | `/api/catalog/get-additional-services-article-item` | Legge un articolo aggiuntivo per il pannello di modifica |
| 15 | POST | `/api/catalog/update-additional-services-article-item` | Salva un articolo aggiuntivo modificato |

Sono tutte **nuove**. Per il frontend sostituiscono le API di `/api/catalog/price-lists`, che lavorano con
`SavePriceListRequest` e `PriceListDto`: ogni sezione dice campo per campo cosa cambia rispetto a quelle.

## Regole comuni

- **Le API esistenti non cambiano**: `/api/catalog/price-lists` (con le sue sotto-rotte) e
  `GET /api/catalog/inventory-items/lookup` restano come sono; le nuove si aggiungono e possono riusarne la logica
  (`PriceListsService`).
- **Stabilimento dal token**: nessuna richiesta lo contiene.
- **Permessi**: come le API dei listini di oggi, salvo dove la sezione dice altro. Senza permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Date: giorni in UTC ISO 8601
  (`"2026-06-01T00:00:00.000Z"`). Enum: i nomi .NET (es. `Daily`, `SaturdaySunday`).
- **Lingua**: il frontend manda sempre `Accept-Language` con la lingua dell'utente (`it`, `en`, `fr`, `es`, `de`,
  `zh`, `ar`).
- **Errori** nel formato Problem Details del backend, con `code`; gli errori di un campo in `errors[]` con i nomi del
  corpo di questo documento (es. `translations[0].packageName`). Senza token valido: 401 `auth.unauthenticated`.

## 1. POST /api/catalog/packages-list

Tabella della tab "Pacchetti" della pagina Listino (`/settings/catalog/1`), allineata alle altre tabelle. Per il
frontend sostituisce `GET /price-lists?kind=package`.

- Corpo: la richiesta standard delle tabelle; lo stabilimento lo prende il backend dal token.
  - `filters` sempre `[]`.
  - `search` sul nome, senza distinguere maiuscole e minuscole.
  - `sortField` `packageName`, oppure `''` per l'ordine del backend.

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "ombrellone",
  "filters": [],
  "sortField": "packageName",
  "sortDirection": "Ascending"
}
```

- Risposta 200 nello stile delle altre tabelle: `total` (tutti i pacchetti che rispettano la ricerca) e `rows` (la
  pagina richiesta), con `idPackage` (oggi `id`) e `packageName` (oggi `name`).
- `packageName` è il nome nella lingua di `Accept-Language`; se in quella lingua non c'è, quello della colonna `name`
  (vedi `create-package`). `search` e l'ordinamento per `packageName` lavorano su questo nome. Esempio: pacchetto con
  solo "Umbrella" (en) → un utente italiano vede "Umbrella"; dopo che qualcuno scrive anche "Ombrellone" (it), vede
  "Ombrellone".

```json
{
  "total": 2,
  "rows": [
    { "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "packageName": "Ombrellone e 2 lettini" },
    { "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02", "packageName": "Ombrellone, sdraio e lettino" }
  ]
}
```

- Errori come le altre tabelle: 401 `auth.unauthenticated`; 403 `auth.forbidden` per un utente senza il permesso
  per questa operazione.

## 2. POST /api/catalog/delete-package-item

Cestino di una riga e "elimina selezionati" della tabella della tab "Pacchetti" (pagina Listino), come le altre tabelle
(per esempio `catalog/delete-sector-item`). Per il frontend sostituisce `DELETE /price-lists/{id}` e
`POST /price-lists/bulk-delete`, che dà un esito parziale: qui arrivano **uno o più id insieme**, tutto o niente.

- Corpo: `idPackages`, gli `idPackage` delle righe di `packages-list`; uno solo con il cestino della
  riga, più con "elimina selezionati".

```json
{
  "idPackages": ["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02"]
}
```

- **Tutto o niente**: se un id non va bene non si elimina nessuno.
- Risposta: 204 senza corpo.
- Errori:
  - 404 `resource.not_found` per un id che non è un pacchetto dello stabilimento (anche un articolo aggiuntivo) o per
    `idPackages` vuoto;
  - 403 `auth.forbidden` e 401 `auth.unauthenticated` come le altre tabelle.

```json
{
  "type": "https://errors.summerbooking/resource/not-found",
  "status": 404,
  "code": "resource.not_found",
  "title": "Resource not found"
}
```

## 3. POST /api/catalog/duplicate-package-item

Bottone duplica di una riga e "duplica selezionati" della tabella della tab "Pacchetti" (pagina Listino), come le altre
tabelle (per esempio `catalog/duplicate-sector-item`). Per il frontend sostituisce `POST /price-lists/{id}/duplicate` e
`POST /price-lists/bulk-duplicate`, che dà un esito parziale: qui arrivano **uno o più id insieme**, tutto o niente.

- Corpo: `idPackages`, gli `idPackage` delle righe di `packages-list`; uno solo con il bottone della
  riga, più con "duplica selezionati".

```json
{
  "idPackages": ["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"]
}
```

- **Tutto o niente**: se un id non va bene non si duplica nessuno.
- Per ogni pacchetto una copia con gli stessi dati, mai predefinita, con un nome scelto dal backend che non si ripete
  fra i pacchetti dello stabilimento, senza distinguere maiuscole e minuscole, come le altre tabelle: per esempio
  "Ombrellone e 2 lettini (copia)", poi "Ombrellone e 2 lettini (copia 2)" (oggi «Nome copia», «Nome copia 2»).
- Risposta: 204 senza corpo.
- Errori: 404 `resource.not_found` per un id che non è un pacchetto dello stabilimento (anche un articolo aggiuntivo) o
  per `idPackages` vuoto; 403 `auth.forbidden` e 401 `auth.unauthenticated` come le altre tabelle.

## 4. POST /api/catalog/map/sector-area-combobox

Tendina a scelta multipla "Aree abilitate" della tab Info Generali dei pannelli "Aggiungi nuovo pacchetto" e
"Modifica pacchetto" (pagina Listino), riletta a ogni apertura della tab. Combobox standard come le altre del frontend
(`catalog/sector-combobox`, `warehouse/article-combobox`): tutte le aree della mappa dello stabilimento, senza pagine,
nel loro ordine.

- POST senza corpo: lo stabilimento lo prende il backend dal token.
- Risposta 200: un array di `{ id, value }`, con `id` = id dell'area e `value` = nome; array vuoto se non ce ne sono.

```json
[
  { "id": "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01", "value": "Area lettini" },
  { "id": "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a02", "value": "Area ombrelloni" }
]
```

- Errori: 403 `auth.forbidden` per un utente senza il permesso per questa operazione; 401 `auth.unauthenticated`.

## 5. POST /api/catalog/packages/create-package

Bottone "Crea pacchetto" del pannello "Aggiungi nuovo pacchetto" (pagina Listino): il frontend manda in una sola
chiamata tutto il modulo delle due tab (Info Generali e Listino). Per il frontend sostituisce
`POST /api/catalog/price-lists`, che accetta solo `SavePriceListRequest`. Lo stabilimento è quello del token. Il corpo è
quello qui sotto.

```json
{
  "translations": [
    { "language": "it", "packageName": "Ombrellone e 2 lettini" },
    { "language": "en", "packageName": "Umbrella and 2 sunbeds" },
    { "language": "fr", "packageName": null },
    { "language": "de", "packageName": null },
    { "language": "es", "packageName": null },
    { "language": "ar", "packageName": null },
    { "language": "zh", "packageName": null }
  ],
  "maxAdditionalItemQuantity": 2,
  "maxPeopleQuantity": 4,
  "umbrellaQuantity": 1,
  "sunbedQuantity": 2,
  "deckchairQuantity": null,
  "directorChairQuantity": null,
  "cabinQuantity": null,
  "tentQuantity": null,
  "includedServices": "Cassetta di sicurezza e doccia calda",
  "idSectorAreas": ["9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01"],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isDefault": false,
  "isDefaultOnline": false,
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "reducedSeasonalPrices": [
    {
      "startsOn": "2026-06-01T00:00:00.000Z",
      "endsOn": "2026-06-30T00:00:00.000Z",
      "reducedSeasonalPriceName": "Giugno",
      "sectorPrices": [
        { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800 }
      ]
    },
    {
      "startsOn": "2026-09-01T00:00:00.000Z",
      "endsOn": "2026-09-15T00:00:00.000Z",
      "reducedSeasonalPriceName": "Inizio settembre",
      "sectorPrices": []
    }
  ],
  "sectorPrices": [
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 600, "seasonalPrice": 1100 }
  ],
  "periodPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "idPeriod": 0,
      "halfDayPrice": 15,
      "dailyPrice": 25,
      "weekendPrice": 30,
      "halfDayWeekendPrice": 18,
      "halfDaySevenDaysPrice": 90,
      "sevenDaysPrice": 150,
      "halfDayFourteenDaysPrice": 170,
      "fourteenDaysPrice": 280,
      "halfDayMonthlyPrice": 320,
      "monthlyPrice": 520
    }
  ],
  "calculationMethod": "DailyWithWeekend",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": true,
  "publicHolidayDates": ["2026-06-02T00:00:00.000Z", "2026-08-15T00:00:00.000Z"],
  "isIntermediatePricesIncluded": true,
  "intermediatePrices": [
    { "idPeriod": 0, "isFirstRow": true, "dayQuantity": 2, "price": 35 },
    { "idPeriod": 0, "isFirstRow": false, "dayQuantity": 2, "price": 30 }
  ]
}
```

Modifiche, campo per campo, rispetto a `SavePriceListRequest`:

- `kind`: il frontend non lo manda: questa API crea sempre un pacchetto.
- `translations`: al posto di `name` + `translations`. Arrivano sempre tutte e 7 le lingue con `packageName`, null se
  non scritto. È obbligatorio il nome nella lingua di `Accept-Language`, quella dell'utente, che nel pannello è la
  principale (non sempre l'italiano).
  Oggi il backend vuole obbligatorio il nome italiano (`name`) e non accetta l'italiano tra le `translations`: con
  questa API salva i 7 nomi come arrivano e controlla come obbligatorio solo quello nella lingua di `Accept-Language`.
  Nella colonna `name` (non vuota, usata per ricerca, ordinamento e copie) mette il nome italiano se c'è, altrimenti
  quello nella lingua di `Accept-Language`. Esempio: utente inglese che scrive solo "Umbrella" → si salva, con
  `name` = "Umbrella".
- `maxAdditionalItemQuantity` e `maxPeopleQuantity`: oggi `maxAdditionalItems` e `maxPeople`. Il frontend manda da 1 a
  15 oppure null.
- `umbrellaQuantity`, `sunbedQuantity`, `deckchairQuantity`, `directorChairQuantity`, `cabinQuantity`, `tentQuantity`:
  al posto di `items` (`inventoryItemId` + `quantity`). Quantità da 1 a 10, null = articolo non nel pacchetto. Il backend
  le abbina ai suoi articoli di magazzino: ombrellone, lettino, sdraio, sedia regista, cabina, tenda.
- `includedServices`: testo libero (campo "Servizi inclusi (opzionale)"), al posto di `includedServiceIds`.
- `idSectorAreas`: id delle aree scelte in "Aree abilitate", gli stessi di `POST /api/catalog/map/sector-area-combobox`.
  Oggi esiste solo `isAreaPackage` (sì/no).
- `idSectors`: al posto di `isSectorRestricted` + `sectorIds`. `[]` = tutti i settori e file.
  Vanno salvati a parte, non ricavati dalle righe dei prezzi. Oggi `PriceListsService.ApplyAsync` non salva
  `sectorIds` e in lettura i settori abilitati sono quelli con una riga in `sectorPrices`; il frontend manda una riga di
  prezzo per ogni settore, quindi risulterebbero abilitati tutti. Esempio: `idSectors` = [Fila 1, Fila 2] e prezzi per
  10 settori → `GET /api/catalog/packages/{idPackage}` risponde `idSectors` = [Fila 1, Fila 2].
- `isOnlineEnabled` e `isOnlineOnly`: al posto di `salesChannel`. Entrambi false = `manager_only`; `isOnlineEnabled`
  true = `manager_and_online`; `isOnlineOnly` true = `online_only`.
- `reducedSeasonalPrices`: i prezzi stagionali ridotti del pacchetto, quanti se ne vogliono (`[]` se nessuno), ognuno
  con nome, periodo e i suoi prezzi per settore (`sectorPrices`: gli stessi campi dei `sectorPrices` del pacchetto,
  "Prezzo stagionale mezza giornata" e "Prezzo stagionale", una riga per settore). I periodi di due prezzi ridotti dello
  stesso pacchetto non possono avere giorni in comune. Oggi il prezzo stagionale ridotto è
  una voce dello stabilimento (`/api/catalog/reduced-seasons`) e nel pacchetto ci sono solo i suoi prezzi
  (`reducedPrices`). Per ogni elemento il backend crea, o riusa, la voce con questo nome e questo periodo, la lega al
  pacchetto e ci salva i prezzi di `sectorPrices`. Il frontend li manda in ordine di `startsOn`. Le date
  sono giorni in UTC ISO 8601 (`"2026-06-01T00:00:00.000Z"`), come in tutte le API del frontend, al posto di `DateOnly`.
- `sectorPrices`: `idSector` al posto di `sectorId`, il resto uguale.
- `periodPrices`: `idSector` al posto di `sectorId`. `idPeriod` (la `key` del periodo di `catalog/tariff-periods`) al
  posto di `periodKey`. Prezzi rinominati: `halfDayPrice` (`halfDay`), `dailyPrice` (`daily`), `weekendPrice` (`weekend`),
  `halfDayWeekendPrice` (`halfDayWeekend`), `halfDaySevenDaysPrice` (`halfDay7`), `sevenDaysPrice` (`days7`),
  `halfDayFourteenDaysPrice` (`halfDay14`), `fourteenDaysPrice` (`days14`), `halfDayMonthlyPrice` (`halfDayMonthly`),
  `monthlyPrice` (`monthly`). Non c'è `stayOverrides`.
- `sectorPrices` e `periodPrices` arrivano vuoti se l'utente non ha aperto la tab Listino: il pacchetto si crea senza
  prezzi.
- `calculationMethod`: tendina "Calcolo del prezzo", null se non scelta. I valori sono i nomi .NET, come in tutti gli
  enum del frontend: `Proportional`, `Daily`, `Logarithmic`, `DailyWithWeekend` (oggi `proportional`, `daily`,
  `logarithmic`, `daily_with_weekend`).
- `monthlySetting`: tendina "Impostazione del mensile", null se non scelta: `EffectiveMonthDuration` (durata
  effettiva del mese) o `Fixed30Days` (30 giorni); oggi `effective_month_duration` e `fixed30days`. Il frontend non
  usa `actual_month_duration`.
- `weekendSetting`: tendina "Impostazione del weekend", null se non scelta: `SaturdaySunday` (sabato e domenica) o
  `SundayOnly` (solo domenica); oggi `saturday_sunday` e `sunday_only`.
- `isPublicHolidaysIncluded`: checkbox nell'intestazione della sezione "Festivi", al posto di `allowsPublicHolidays`.
- `publicHolidayDates`: i giorni della sezione "Festivi" (scelti nel calendario di "Aggiungi festivi", tolti con il
  cestino), al posto di `holidayDates`. Giorni in UTC ISO 8601, in ordine, ognuno una volta sola, `[]` se nessuno.
  Spegnere il checkbox, dopo una conferma, svuota la lista.
- `isIntermediatePricesIncluded` (nuovo, oggi non esiste): checkbox nell'intestazione della sezione "Modifica prezzi
  intermedi" della tab Listino.
- `intermediatePrices` (nuovo): i prezzi per numero di giorni scritti nella sezione "Modifica prezzi intermedi", solo
  quelli con un prezzo (`[]` se nessuno), in ordine (prima `isFirstRow` true, poi per `idPeriod`, numerico, e
  `dayQuantity`). Ognuno ha `idPeriod` (la `key` di un periodo della stagione corrente, di `catalog/tariff-periods`),
  `isFirstRow` (true = "Fila 1", false = "Altre file"), `dayQuantity` (giorni di soggiorno, da 1 a 90) e `price` (non negativo). Una sola riga per
  `idPeriod` + `isFirstRow` + `dayQuantity`. Oggi i prezzi per durata sono `catalog_pricing_price_list_stay_prices`,
  per periodo tariffario e settore: il backend li lega alle sue righe ("Fila 1" = il primo settore di
  `catalog/sector-combobox`, "Altre file" = tutti gli altri). I prezzi di 7, 14 e 30 giorni il frontend li scrive anche
  in `sevenDaysPrice`, `fourteenDaysPrice` e `monthlyPrice` delle righe di `periodPrices` di quel periodo (della prima
  fila o delle altre), così i due valori arrivano uguali. Spegnere il checkbox, dopo una conferma, svuota la lista.
- Non arrivano ancora, perché il pannello non li ha: `isMapAssignable`, `vatRate`, `vatNature`. Se mancano valgono
  null, false o vuoti.
- Risposta: 204 senza corpo.
- Errori: 400 `validation.failed` con `errors[]`, un elemento per campo con i nomi del corpo qui sopra (es.
  `translations[0].packageName`, `maxPeopleQuantity`, `reducedSeasonalPrices[0].endsOn`; un prezzo ridotto con giorni in comune con un altro:
  `reducedSeasonalPrices[i].startsOn` `validation.invalid`; un giorno non valido o ripetuto: `publicHolidayDates`
  `validation.invalid`; un prezzo intermedio non valido o ripetuto: `intermediatePrices` `validation.invalid`); 401 `auth.unauthenticated`.

## 6. GET /api/catalog/packages/{idPackage}

Apertura del pannello "Modifica pacchetto" (matita della tabella Pacchetti, pagina Listino): il frontend legge tutto il
pacchetto in una sola chiamata e ci riempie il modulo delle due tab. Per il frontend sostituisce
`GET /api/catalog/price-lists/{id}`, che risponde `PriceListDto`. Lo stabilimento è quello del token. La risposta ha
`idPackage` e gli stessi campi, con gli stessi nomi e valori, che il frontend manda a
`POST /api/catalog/packages/create-package` (vedi sopra).

```json
{
  "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
  "translations": [
    { "language": "it", "packageName": "Ombrellone e 2 lettini" },
    { "language": "en", "packageName": "Umbrella and 2 sunbeds" }
  ],
  "maxAdditionalItemQuantity": 2,
  "maxPeopleQuantity": 4,
  "umbrellaQuantity": 1,
  "sunbedQuantity": 2,
  "deckchairQuantity": null,
  "directorChairQuantity": null,
  "cabinQuantity": null,
  "tentQuantity": null,
  "includedServices": "Cassetta di sicurezza",
  "idSectorAreas": [],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isDefault": true,
  "isDefaultOnline": false,
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "reducedSeasonalPrices": [
    {
      "startsOn": "2026-06-01T00:00:00.000Z",
      "endsOn": "2026-06-30T00:00:00.000Z",
      "reducedSeasonalPriceName": "Giugno",
      "sectorPrices": [
        { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800 }
      ]
    },
    {
      "startsOn": "2026-09-01T00:00:00.000Z",
      "endsOn": "2026-09-15T00:00:00.000Z",
      "reducedSeasonalPriceName": "Inizio settembre",
      "sectorPrices": []
    }
  ],
  "sectorPrices": [
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 600, "seasonalPrice": 1100 }
  ],
  "periodPrices": [],
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": false,
  "publicHolidayDates": [],
  "isIntermediatePricesIncluded": false,
  "intermediatePrices": []
}
```

- `translations`: solo le lingue con un nome. Il frontend aggiunge le altre vuote.
- `idSectors`: quelli salvati con `create-package` o `update-package`, non i settori che hanno una riga di prezzo.
- `sectorPrices` e `periodPrices`: solo le righe con dei prezzi salvati. Il frontend fa una riga per ogni settore di
  `catalog/sector-combobox`, e per ogni settore in ogni periodo della stagione corrente (`catalog/tariff-periods`), e ci
  mette i prezzi letti. Le righe di settori o periodi che non mostra (es. di un'altra stagione) le tiene da parte e le
  rimanda invariate con `update-package`.
- `publicHolidayDates` e `intermediatePrices`: in qualsiasi ordine (il frontend li ordina).
- `reducedSeasonalPrices`: in qualsiasi ordine (il frontend li ordina per `startsOn`). Anche i loro `sectorPrices`
  possono avere solo le righe salvate: il frontend fa una riga per ogni settore e le righe di settori che non conosce le
  tiene senza mostrarle e le rimanda invariate con `update-package`.
- Errori: 404 `resource.not_found` per un pacchetto che non è dello stabilimento; 401 `auth.unauthenticated`.

## 7. POST /api/catalog/packages/update-package

Bottone "Salva" del pannello "Modifica pacchetto": il frontend manda tutto il pacchetto, con lo stesso corpo di
`POST /api/catalog/packages/create-package` (vedi sopra) più `idPackage`. Il pacchetto prende tutti i valori mandati:
`sectorPrices` e `periodPrices` hanno tutte le righe del pacchetto, anche quelle di settori o periodi che il pannello
non mostra (es. di un'altra stagione), così nessun prezzo salvato si perde; lo stesso per i `sectorPrices` di ogni
prezzo stagionale ridotto. `reducedSeasonalPrices` ha tutti i prezzi ridotti rimasti, in ordine di `startsOn`: quelli
tolti con il cestino non ci sono più e il backend li toglie dal pacchetto. Lo stesso per `publicHolidayDates`: tutti
i giorni festivi rimasti, in ordine; e per `intermediatePrices`: tutti i prezzi intermedi scritti, in ordine (un prezzo
cancellato non c'è più).
Per il frontend sostituisce `PUT /api/catalog/price-lists/{id}`, che accetta solo `SavePriceListRequest`.

```json
{
  "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
  "translations": [
    { "language": "it", "packageName": "Ombrellone e 2 lettini" },
    { "language": "en", "packageName": "Umbrella and 2 sunbeds" }
  ],
  "maxPeopleQuantity": 6
}
```

(nell'esempio solo alcuni campi: arrivano sempre tutti, come nella creazione)

- Risposta: 204 senza corpo.
- Righe di settori o periodi che non esistono più: non devono far fallire il salvataggio. Oggi `ApplyAsync` risponde
  404 `catalog.sector_not_found` per un settore eliminato e 422 `pricing.period_not_found` per una `key` che non è un
  periodo della stagione corrente. Il backend tiene come arrivano le righe dei periodi di un'altra stagione e scarta in
  silenzio quelle dei settori eliminati (anche dentro i `sectorPrices` dei prezzi stagionali ridotti). Esempio: Fila 9 eliminata dopo l'apertura del pannello → "Salva"
  risponde 204 e la riga di Fila 9 non si salva. Le righe tenute restano legate al loro periodo: se la loro `key` viene
  riusata da un periodo nuovo, non devono finire su quello.
- Errori: come `POST /api/catalog/packages/create-package`, più 404 `resource.not_found` per un `idPackage` che non è
  dello stabilimento.

## 8. POST /api/catalog/additional-services-articles-list

Tabella della tab "Articoli/Servizi aggiuntivi" della pagina Listino (`/settings/catalog/2`), come `packages-list`.
Per il frontend sostituisce `GET /price-lists?kind=additional`.

- Corpo: la richiesta standard delle tabelle, con le stesse regole di `packages-list` (`sortField`
  `additionalItemName`).

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "",
  "filters": [],
  "sortField": "",
  "sortDirection": "Ascending"
}
```

- Risposta 200 nello stile delle altre tabelle: `total` e `rows`, con `idAdditionalItem` (oggi `id`) e
  `additionalItemName` (oggi `name`).

```json
{
  "total": 2,
  "rows": [
    { "idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01", "additionalItemName": "Lettino" },
    { "idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a02", "additionalItemName": "Sdraio" }
  ]
}
```

- Errori come `packages-list`.

## 9. POST /api/catalog/delete-additional-services-article-item

Cestino di una riga e "elimina selezionati" della tabella della tab "Articoli/Servizi aggiuntivi" (pagina Listino),
come `delete-package-item`. Per il frontend sostituisce `DELETE /price-lists/{id}` e `POST /price-lists/bulk-delete`
per gli articoli aggiuntivi: qui arrivano **uno o più id insieme**, tutto o niente.

- Corpo: `idAdditionalItems`, gli `idAdditionalItem` delle righe di
  `additional-services-articles-list`; uno solo con il cestino della riga, più con "elimina selezionati".

```json
{
  "idAdditionalItems": ["4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01", "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a02"]
}
```

- **Tutto o niente**: se un id non va bene non si elimina nessuno.
- Risposta: 204 senza corpo.
- Errori: 404 `resource.not_found` per un id che non è un articolo aggiuntivo dello stabilimento (anche un pacchetto)
  o per `idAdditionalItems` vuoto; 403 `auth.forbidden` e 401 `auth.unauthenticated` come le altre tabelle.

```json
{
  "type": "https://errors.summerbooking/resource/not-found",
  "status": 404,
  "code": "resource.not_found",
  "title": "Resource not found"
}
```

## 10. POST /api/catalog/duplicate-additional-services-article-item

Bottone duplica di una riga e "duplica selezionati" della tabella della tab "Articoli/Servizi aggiuntivi" (pagina
Listino), come `duplicate-package-item`. Per il frontend sostituisce `POST /price-lists/{id}/duplicate` e
`POST /price-lists/bulk-duplicate` per gli articoli aggiuntivi: qui arrivano **uno o più id insieme**, tutto o niente.

- Corpo: `idAdditionalItems`, come `delete-additional-services-article-item`; uno solo con il bottone
  della riga, più con "duplica selezionati".

```json
{
  "idAdditionalItems": ["4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01"]
}
```

- **Tutto o niente**: se un id non va bene non si duplica nessuno.
- Per ogni articolo una copia con gli stessi dati e un nome scelto dal backend che non si ripete fra gli articoli
  aggiuntivi dello stabilimento, senza distinguere maiuscole e minuscole: per esempio "Lettino (copia)", poi
  "Lettino (copia 2)".
- Risposta: 204 senza corpo.
- Errori: come `delete-additional-services-article-item`.

## 11. POST /api/catalog/package-combobox

Tendina a scelta multipla "Pacchetti" del pannello "Aggiungi nuovo articolo/servizio aggiuntivo" (pagina Listino),
riletta a ogni apertura del pannello. Combobox standard come le altre del frontend (`catalog/sector-combobox`,
`warehouse/article-combobox`): tutti i pacchetti dello stabilimento, senza pagine, nel loro ordine.

- POST senza corpo: lo stabilimento lo prende il backend dal token.
- Risposta 200: un array di `{ id, value }`, con `id` = `idPackage` e `value` = nome del pacchetto, scelto
  come `packageName` di `packages-list` (lingua di `Accept-Language`, altrimenti `name`); array vuoto se non ce ne sono.

```json
[
  { "id": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "value": "Ombrellone e 2 lettini" },
  { "id": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02", "value": "Ombrellone, sdraio e lettino" }
]
```

- Errori: 403 `auth.forbidden` per un utente senza il permesso per questa operazione; 401 `auth.unauthenticated`.

## 12. POST /api/warehouse/article-combobox

Tendina obbligatoria "Seleziona articolo" dei pannelli "Aggiungi nuovo articolo/servizio aggiuntivo" e "Modifica
articolo/servizio aggiuntivo" (pagina Listino) e tendina "Articolo" del popup "Aggiungi articolo" (Impostazioni
magazzino), riletta a ogni apertura del pannello o del popup. Combobox standard come le altre del frontend
(`catalog/sector-combobox`, `catalog/package-combobox`): tutti gli articoli di magazzino dello stabilimento, senza
pagine, in ordine di nome.

Oggi il backend non ha questa rotta: ha `GET /api/catalog/inventory-items/lookup` (`InventoryService.LookupAsync`), con
gli stessi articoli e lo stesso ordine ma in un'altra forma (`{ id, name, articleCode }`). Quella resta com'è; la nuova
può riusarne la logica e cambia solo rotta, metodo e nomi dei campi.

- POST senza corpo: lo stabilimento lo prende il backend dal token.
- Risposta 200: un array di `{ id, value }`, con `id` = l'id pubblico dell'articolo (`catalog.inventory_items.public_id`,
  lo stesso `Id` di `inventory-items/lookup`) e `value` = il suo nome; solo gli articoli non eliminati, in ordine di
  nome; array vuoto se non ce ne sono.

```json
[
  { "id": "6f1d2c3b-4a5e-4f60-8b7c-9d0e1f2a3b01", "value": "Lettino" },
  { "id": "6f1d2c3b-4a5e-4f60-8b7c-9d0e1f2a3b02", "value": "Ombrellone" },
  { "id": "6f1d2c3b-4a5e-4f60-8b7c-9d0e1f2a3b03", "value": "Sdraio" }
]
```

- L'`id` scelto è l'`idArticle` che il frontend manda in `create-additional-services-article-item` e
  `update-additional-services-article-item` e che legge in `get-additional-services-article-item`: deve essere lo
  stesso id.
- Permesso: basta uno tra lettura del catalogo (`CatalogRead`, come le altre tendine del Listino) e lettura del
  magazzino (`InventoryRead`, come `inventory-items/lookup`), così vede la tendina chi apre il Listino o le Impostazioni
  magazzino.
- Errori: 403 `auth.forbidden` per un utente senza il permesso; 403 `catalog.property_required` per un account senza
  stabilimento; 401 `auth.unauthenticated`. Il frontend mostra l'errore sotto la tendina.

## 13. POST /api/catalog/create-additional-services-article-item

Bottone "Crea articolo/servizio" del pannello "Aggiungi nuovo articolo/servizio aggiuntivo" (pagina Listino): il
frontend manda tutto il modulo in una sola chiamata. Per il frontend sostituisce `POST /api/catalog/price-lists` con
`kind: "additional"`, che accetta solo `SavePriceListRequest`. Lo stabilimento lo prende il backend dal token.

```json
{
  "idArticle": "afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62",
  "idPackages": ["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"],
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "isMapAssignable": true,
  "sectorPrices": [
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 60, "seasonalPrice": 120 }
  ],
  "periodPrices": [],
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": false,
  "publicHolidayDates": [],
  "isIntermediatePricesIncluded": false,
  "intermediatePrices": []
}
```

Modifiche, campo per campo, rispetto a `SavePriceListRequest`:

- `idArticle`: l'articolo di magazzino scelto in "Seleziona articolo" (id di `warehouse/article-combobox`),
  obbligatorio, al posto di `items` (che per un articolo aggiuntivo ha un solo elemento). Il nome dell'articolo
  aggiuntivo è quello dell'articolo: il frontend non manda `name` né `translations`.
- `idPackages`: pacchetti scelti in "Pacchetti" (id di `catalog/package-combobox`), `[]` = tutti i pacchetti, al
  posto di `packageLinks` (il frontend non manda `maxCount`).
- `isOnlineEnabled` e `isOnlineOnly` ("Prenotazioni online" e "Solo online"): al posto di `salesChannel`, come in
  `create-package`.
- `isMapAssignable`: uguale.
- `sectorPrices`, `periodPrices`, `calculationMethod`, `isCumulative`, `monthlySetting`, `weekendSetting`,
  `isPublicHolidaysIncluded`, `publicHolidayDates`, `isIntermediatePricesIncluded`, `intermediatePrices`: gli stessi
  campi e valori di `create-package` (vedi sopra, con le stesse differenze).
  `sectorPrices` e `periodPrices` hanno una riga per ogni settore e per ogni settore in ogni periodo della stagione
  corrente.
- Risposta: 204 senza corpo.
- Errori: 404 `resource.not_found` per un `idArticle` che non è un articolo attivo dello stabilimento o un `idPackage`
  che non è dei suoi; 400 `validation.failed` con `errors[]` per i valori fuori dagli enum (es. `calculationMethod`);
  403 `auth.forbidden` e 401 `auth.unauthenticated` come le altre.

## 14. GET /api/catalog/get-additional-services-article-item

Apertura del pannello "Modifica articolo/servizio aggiuntivo" (matita della tabella Articoli/Servizi aggiuntivi,
pagina Listino): il frontend legge tutto l'articolo aggiuntivo in una sola chiamata e ci riempie il modulo. Per il
frontend sostituisce `GET /api/catalog/price-lists/{id}`, che risponde `PriceListDto`. L'id va nella query
(`?idAdditionalItem=...`); lo stabilimento è quello del token. La risposta ha `idAdditionalItem` e gli stessi campi,
con gli stessi nomi e valori, che il frontend manda a `POST /api/catalog/create-additional-services-article-item`
(vedi sopra).

```json
{
  "idAdditionalItem": "2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02",
  "idArticle": "afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62",
  "idPackages": ["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"],
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "isMapAssignable": true,
  "sectorPrices": [
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 60, "seasonalPrice": 120 }
  ],
  "periodPrices": [],
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": false,
  "publicHolidayDates": [],
  "isIntermediatePricesIncluded": false,
  "intermediatePrices": []
}
```

- `sectorPrices` e `periodPrices`: solo le righe con dei prezzi salvati. Il frontend fa una riga per ogni settore di
  `catalog/sector-combobox`, e per ogni settore in ogni periodo della stagione corrente (`catalog/tariff-periods`), e ci
  mette i prezzi letti. Le righe di settori o periodi che non mostra (es. di un'altra stagione) le tiene da parte e le
  rimanda invariate con `update-additional-services-article-item`.
- `publicHolidayDates` e `intermediatePrices`: in qualsiasi ordine (il frontend li ordina).
- Errori: 404 `resource.not_found` per un articolo aggiuntivo che non è dello stabilimento; 401 `auth.unauthenticated`.

## 15. POST /api/catalog/update-additional-services-article-item

Bottone "Salva" del pannello "Modifica articolo/servizio aggiuntivo": il frontend manda tutto il modulo, con lo stesso
corpo di `POST /api/catalog/create-additional-services-article-item` (vedi sopra) più `idAdditionalItem`. L'articolo
aggiuntivo prende tutti i valori mandati: `sectorPrices` e `periodPrices` hanno tutte le sue righe, anche quelle di
settori o periodi che il pannello non mostra (es. di un'altra stagione), così nessun prezzo salvato si perde.
`publicHolidayDates` ha tutti i giorni festivi rimasti, in ordine: quelli tolti con il cestino non ci sono più;
`intermediatePrices` ha tutti i prezzi intermedi scritti, in ordine.
Per il frontend sostituisce `PUT /api/catalog/price-lists/{id}`, che accetta solo `SavePriceListRequest`.

```json
{
  "idAdditionalItem": "2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02",
  "idArticle": "afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62",
  "isOnlineOnly": true
}
```

(nell'esempio solo alcuni campi: arrivano sempre tutti, come nella creazione)

- Risposta: 204 senza corpo.
- Righe di settori o periodi che non esistono più: non devono far fallire il salvataggio. Oggi `ApplyAsync` risponde
  404 `catalog.sector_not_found` per un settore eliminato e 422 `pricing.period_not_found` per una `key` che non è un
  periodo della stagione corrente. Il backend tiene come arrivano le righe dei periodi di un'altra stagione e scarta in
  silenzio quelle dei settori eliminati. Esempio: Fila 9 eliminata dopo l'apertura del pannello → "Salva"
  risponde 204 e la riga di Fila 9 non si salva. Le righe tenute restano legate al loro periodo: se la loro `key` viene
  riusata da un periodo nuovo, non devono finire su quello.
- Errori: come `POST /api/catalog/create-additional-services-article-item`, più 404 `resource.not_found` per un
  `idAdditionalItem` che non è dello stabilimento.

## Verifica

- `packages-list` e `package-combobox` danno il nome nella lingua di `Accept-Language`, altrimenti quello di `name`;
  ricerca e ordinamento lavorano su quel nome.
- `create-package` con `Accept-Language: en` e solo il nome inglese risponde 204; `name` = il nome inglese. Senza il
  nome nella lingua di `Accept-Language` risponde 400 `validation.failed` su `translations[i].packageName`.
- `idSectors` salvati con prezzi per tutti i settori tornano uguali in `GET /api/catalog/packages/{idPackage}`.
- `update-package` e `update-additional-services-article-item` con una riga di un settore eliminato, o di un periodo
  di un'altra stagione, rispondono 204: la prima non si salva, la seconda resta com'era.
- I salvataggi rispondono 204 senza corpo; cancella e duplica fanno tutti o nessuno.
- Ovunque: lo stabilimento è quello del token, nessuna richiesta lo contiene.
