# Servizi - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Servizi** del gestionale funzioni come la vuole il frontend.
Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare o cambiare, con regole,
errori attesi e prove.

La pagina è `/settings/services` e ha cinque tab: Servizi Bar, Servizi (servizi generici), Servizi Orari, Prezzi
Automatici e Prenotazione tavoli. Ogni tab è una tabella; la matita di una riga apre un pannello di modifica con due o
tre tab interne, ognuna letta con la sua GET e salvata con la sua API, una tab alla volta.

Le API della pagina (`/api/catalog/bar-service-list`, `/api/catalog/get-bar-service-general-info`,
`/api/catalog/update-bar-service-general`…) non ci sono ancora nel backend, che ha `/api/catalog/services`,
`/api/catalog/automatic-prices` e `/api/catalog/table-services` con altre rotte e altri corpi
(`Endpoints/ServiceEndpoints.cs`). Rotte, richieste e risposte sono nei contratti delle API
(`shared documentation/API_CONTRACTS.docx` e `shared documentation/API - Full list.md`); questo documento chiede quello
che va aggiunto a quei contratti e, una sezione alla volta, le API che il backend deve creare (sezioni 2-6, le tabelle
delle cinque tab; sezioni 7-11, le eliminazioni; sezioni 12-16, le duplicazioni).

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET + POST | Le 13 coppie lettura e salvataggio dei pannelli di modifica (tabella della sezione 1) | Controllo di versione, una versione per tab: `version` nelle letture, `If-Match` ed `ETag` nei salvataggi, 409 `resource.version_conflict` |
| 2 | POST | `/api/catalog/bar-service-list` | Tabella della tab Servizi Bar, a pagine |
| 3 | POST | `/api/catalog/standard-service-list` | Tabella della tab Servizi, a pagine |
| 4 | POST | `/api/catalog/hourly-service-list` | Tabella della tab Servizi Orari, a pagine |
| 5 | POST | `/api/catalog/automatic-price-list` | Tabella della tab Prezzi Automatici, a pagine |
| 6 | POST | `/api/catalog/table-service-list` | Tabella della tab Prenotazione tavoli, a pagine |
| 7 | POST | `/api/catalog/delete-bar-service-item` | Elimina uno o più servizi bar, tutto o niente |
| 8 | POST | `/api/catalog/delete-standard-service-item` | Elimina uno o più servizi, tutto o niente |
| 9 | POST | `/api/catalog/delete-hourly-service-item` | Elimina uno o più servizi orari, tutto o niente |
| 10 | POST | `/api/catalog/delete-automatic-price-item` | Elimina uno o più prezzi automatici, tutto o niente |
| 11 | POST | `/api/catalog/delete-table-service-item` | Elimina uno o più servizi tavoli, tutto o niente |
| 12 | POST | `/api/catalog/duplicate-bar-service-item` | Duplica uno o più servizi bar, tutto o niente |
| 13 | POST | `/api/catalog/duplicate-standard-service-item` | Duplica uno o più servizi, tutto o niente |
| 14 | POST | `/api/catalog/duplicate-hourly-service-item` | Duplica uno o più servizi orari, tutto o niente |
| 15 | POST | `/api/catalog/duplicate-automatic-price-item` | Duplica uno o più prezzi automatici, tutto o niente |
| 16 | POST | `/api/catalog/duplicate-table-service-item` | Duplica uno o più servizi tavoli, tutto o niente |

## 1. Controllo di versione sui salvataggi dei pannelli di modifica

Senza un controllo, se due utenti modificano la stessa tab dello stesso servizio vince chi salva per ultimo: ogni Salva
rimanda tutta la tab letta all'apertura (il frontend rimanda sempre tutto il modello letto) e cancella, senza che
nessuno se ne accorga, quello che l'altro utente ha cambiato nel frattempo. Il frontend chiede lo schema di
**concorrenza ottimistica** che il backend ha già progettato per le prenotazioni offline
(`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un **codice di errore
unico per tutte le API**: `resource.version_conflict`.

### Cosa ci serve

- **Una versione per tab**: ogni lettura della tabella sotto restituisce, insieme ai dati, il campo `version` (stringa
  opaca, mai `null`). Copre **solo i dati che scrive il salvataggio della stessa tab** (colonna "Dati coperti"): cambia
  ogni volta che cambiano quei dati, anche quando li cambia un'altra API, e solo allora. Così il Salva di una tab non fa
  mai fallire quello di un'altra tab dello stesso servizio (per esempio salvare Prezzi non cambia la versione di Info
  Generali).
- **Le tre letture delle opzioni di prenotazione cambiano forma**: `get-bar-service-booking-options`,
  `get-standard-service-booking-options` e `get-hourly-service-booking-options` rispondono un oggetto con la lista di
  prima in `bookingOptions` e la versione in `version`, non più la sola lista: una lista non può portare la versione.
  Senza opzioni `bookingOptions` è `[]`.

```json
{
  "bookingOptions": [
    {
      "dateFrom": "2026-07-01T00:00:00.000Z",
      "dateTo": "2026-07-31T00:00:00.000Z",
      "managementStartTime": "08:00",
      "managementEndTime": "12:00",
      "onlineStartTime": "08:30",
      "onlineEndTime": "11:30",
      "maxBookingPerHourQuantity": 20,
      "maxBookingPerDayQuantity": 80,
      "timeInterval": "ThirtyMinutes",
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 4
    }
  ],
  "version": "3e9a51c0"
}
```

- Le altre dieci letture restituiscono l'oggetto dei contratti con in più `version`, per esempio
  `get-bar-service-prices`:

```json
{
  "price": 1,
  "vatRate": 10,
  "variants": [{ "variantName": "Doppio", "price": 1.8 }],
  "isPeriodPricingEnabled": true,
  "periodPrices": [{ "idPeriod": 0, "price": 1.2 }],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isOnlinePurchaseEnabled": true,
  "version": "a07c42d8"
}
```

- **`If-Match` nei salvataggi**: il frontend manda la versione letta nell'header `If-Match: "<version>"`, mai nel
  corpo; i corpi restano quelli dei contratti. Se la versione non è più quella attuale, il backend non salva niente e
  risponde **409 `resource.version_conflict`**, senza `errors[]`. Senza `If-Match` risponde allo stesso modo: il
  frontend lo manda sempre.
- **`ETag` nelle risposte**: ogni salvataggio riuscito risponde 204 senza corpo, come nei contratti, con la nuova
  versione nell'header `ETag: "<version>"`.
- Il 409 si controlla dopo il 404 dell'elemento (`service.not_found`, `service.automatic_price_not_found` o
  `service.table_service_not_found`, e `service.not_found` dei servizi non dello stabilimento nei Prezzi automatici) e
  dopo il 400 `validation.failed`, subito prima del salvataggio.
- La copia fatta da Duplica ha le sue versioni: salvarla non cambia quelle del servizio di partenza.
- **Come farlo lo sceglie lo sviluppatore del backend** (un hash dei dati coperti, un contatore per tab...): l'agente
  glielo chiede prima di scrivere codice. Il frontend tratta la versione come una stringa opaca e non la confronta mai.

### API coinvolte

Le letture hanno l'id nella query string, i salvataggi nel corpo. I dati coperti dalla versione sono i campi del corpo
del salvataggio, tranne l'id.

| Pannello e tab | Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|---|
| Servizio bar, Info Generali | `GET /api/catalog/get-bar-service-general-info?idBarService=…` | `POST /api/catalog/update-bar-service-general` | `translations`, `category`, `icon`, `isHighlighted` |
| Servizio bar, Prezzi | `GET /api/catalog/get-bar-service-prices?idBarService=…` | `POST /api/catalog/update-bar-service-prices` | `price`, `vatRate`, `variants`, `isPeriodPricingEnabled`, `periodPrices` (anche dei periodi che la tab non mostra), `idSectors`, `isOnlinePurchaseEnabled` |
| Servizio bar, Prenotazioni | `GET /api/catalog/get-bar-service-booking-options?idBarService=…` | `POST /api/catalog/update-bar-service-booking` | `bookingOptions` |
| Servizio, Info Generali | `GET /api/catalog/get-standard-service-general-info?idStandardService=…` | `POST /api/catalog/update-standard-service-general` | `translations`, `icon`, `isHighlighted` |
| Servizio, Prezzi | `GET /api/catalog/get-standard-service-prices?idStandardService=…` | `POST /api/catalog/update-standard-service-prices` | `price`, `vatRate`, `isPeriodPricingEnabled`, `periodPrices` (anche dei periodi che la tab non mostra), `idSectors`, `isOnlinePurchaseEnabled` |
| Servizio, Prenotazioni | `GET /api/catalog/get-standard-service-booking-options?idStandardService=…` | `POST /api/catalog/update-standard-service-booking` | `bookingOptions` |
| Servizio orario, Info Generali | `GET /api/catalog/get-hourly-service-general-info?idHourlyService=…` | `POST /api/catalog/update-hourly-service-general` | `translations`, `icon`, `isHighlighted` |
| Servizio orario, Prezzi | `GET /api/catalog/get-hourly-service-prices?idHourlyService=…` | `POST /api/catalog/update-hourly-service-prices` | `isPeriodPricingEnabled`, `periodAvailabilities` (anche dei periodi che la tab non mostra), `idSectors`, `isOnlinePurchaseEnabled` |
| Servizio orario, Prenotazioni | `GET /api/catalog/get-hourly-service-booking-options?idHourlyService=…` | `POST /api/catalog/update-hourly-service-booking` | `bookingOptions` |
| Prezzo automatico, Info Generali | `GET /api/catalog/get-automatic-price-general-info?idAutomaticPrice=…` | `POST /api/catalog/update-automatic-price-general` | `translations`, `icon` |
| Prezzo automatico, Prezzi e Validità | `GET /api/catalog/get-automatic-price-prices-validity?idAutomaticPrice=…` | `POST /api/catalog/update-automatic-price-prices-validity` | `price`, `vatRate`, `isOnlinePurchaseEnabled`, `validityPeriods`, `idBarServices`, `idStandardServices`, `idHourlyServices`, `isPricePerServiceEnabled`, `idSectors` |
| Servizio tavoli, Info Generali | `GET /api/catalog/get-table-service-general-info?idTableService=…` | `POST /api/catalog/update-table-service-general` | `translations`, `isHighlighted` |
| Servizio tavoli, Disponibilità e Prenotazioni | `GET /api/catalog/get-table-service-availability-bookings?idTableService=…` | `POST /api/catalog/update-table-service-availability-bookings` | `availabilities`, `availabilityMode`, `tableGroups`, `totalSeatQuantity`, `averageMealDuration`, `maxBookingPerTimeSlotQuantity`, `bookingPayment`, `depositMode`, `depositPercentage`, `isOnlineBookingEnabled` |

Le altre API della pagina non ne hanno bisogno: elenchi, aggiunta, eliminazione e duplicazione non rimandano dati letti,
e `get-automatic-price-services` e `catalog/tariff-periods` servono solo a riempire le tab.

### Cosa fa il frontend

Con 409 `resource.version_conflict` il pannello mostra l'avviso "Nel frattempo un altro utente ha modificato questi
dati: li abbiamo ricaricati. Rifai le tue modifiche.", la tab torna in sola lettura e si rilegge con la sua GET; il
messaggio sopra i bottoni non compare. Dopo un salvataggio riuscito il pannello si chiude e alla riapertura rilegge le
tab.

## Regole comuni delle tabelle (sezioni 2-6)

- **API nuove, accanto a quelle di oggi**: stanno in Catalog (`Module.Catalog`), accanto a `/api/catalog/services`,
  `/api/catalog/automatic-prices` e `/api/catalog/table-services` (`Endpoints/ServiceEndpoints.cs`, servizi
  `ServiceItemsService`, `AutomaticPricesService` e `TableServicesService`, `ServiceCatalogRepository` in
  `Infrastructure/Repositories/ServiceCatalogRepository.cs`). Le API di oggi restano come sono: stesse rotte, richieste,
  risposte e comportamento.
- **Dati**: servizi bar, servizi e servizi orari sono le righe di `catalog.service_sale_items` con `kind` `bar`,
  `normal` e `hourly` (`ServiceSaleItemConfiguration.cs`); i prezzi automatici sono `catalog.automatic_prices`, i servizi
  tavoli `catalog.table_services`. I nomi nelle altre lingue stanno in `catalog.service_sale_item_translations`,
  `catalog.automatic_price_translations` e `catalog.table_service_translations` (colonne `language` e `name`).
- **Permesso**: `CatalogRead`, come le `GET` di oggi (`ServiceEndpoints.cs:16`). Senza permesso 403 `auth.forbidden`;
  senza token valido 401 `auth.unauthenticated`.
- **Stabilimento**: lo prende dal token (`CurrentProperty.IdAsync`), come le altre API del Catalog: nessuna richiesta lo
  contiene. Token senza stabilimento: 403 `catalog.property_required`; stabilimento che non esiste: 404
  `catalog.property_not_found`.
- **Righe eliminate**: le righe con `deleted_at` valorizzato non compaiono mai e non contano in `total`. Lo fa già il
  filtro globale delle entità `ISoftDeletable` (`ServiceSaleItem`, `AutomaticPrice` e `TableService` lo sono).
- **JSON** in camelCase, con i nomi esatti di questo documento. **Errori** nel formato Problem Details del backend
  (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed `errors[]` quando ci sono.

### Richiesta

Il corpo è la richiesta standard delle tabelle del frontend, uguale per tutte le tabelle del gestionale (per esempio
`POST /api/catalog/inventory-items-list` di `Impostazioni magazzino - Frontend requests.md`, sezione 1):

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "caff",
  "filters": [],
  "sortField": "serviceName",
  "sortDirection": "Ascending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `page` | number (intero) | Pagina richiesta, da 1. |
| `pageSize` | number (intero) | Righe per pagina: 10, 20, 50 o 100. Non valgono gli altri valori di `CatalogQueryRules.PageSizes` (25, 200, 300). |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca. Il frontend lo manda senza spazi ai lati, da 3 caratteri in su. Trova le righe il cui nome (`serviceName`, quello mostrato) contiene il testo, senza distinguere maiuscole e minuscole; `%` e `_` contano come caratteri normali, non come caratteri jolly (`LikePattern.Contains`, come `ServiceCatalogRepository.ListAsync`). Al massimo 100 caratteri (`CatalogQueryRules.SearchMaxLength`). |
| `filters` | array | Sempre `[]`: le tabelle non hanno filtri. |
| `sortField` | string | Uno dei campi della sezione della tabella, oppure `""`: senza un ordinamento scelto dall'utente la tabella mostra le righe per nome, come con `serviceName` e `Ascending`. |
| `sortDirection` | string | `Ascending` o `Descending`. Con `sortField` `""` il frontend manda `Ascending`. |

### Nome nella lingua dell'utente

`serviceName` è il nome nella lingua dell'header `Accept-Language`, che il frontend manda sempre con la lingua
dell'utente (`it`, `en`, `fr`, `es`, `de`, `zh`, `ar`): per `it` la colonna `name`, per le altre lingue il `name` della
riga di traduzione in quella lingua; se la traduzione non c'è, la colonna `name`. La ricerca e l'ordinamento per
`serviceName` lavorano su questo nome, come `packageName` di `POST /api/catalog/packages-list`
(`Listino - Frontend requests.md`, sezione 1). Esempio: servizio "Caffè" con la traduzione inglese "Coffee" → con
`Accept-Language: en` la riga ha "Coffee" e la ricerca "coff" la trova; con `Accept-Language: fr`, senza traduzione
francese, ha "Caffè".

### Ordinamento

- **`serviceName`** (e `""`): **ordine naturale** del nome mostrato, cioè i numeri dentro il nome si confrontano come
  numeri, non cifra per cifra. Maiuscole e minuscole non contano e le lettere accentate stanno accanto a quelle senza
  accento.

  | `sortDirection` | Ordine atteso |
  |---|---|
  | `Ascending` | Cabina 1, Cabina 2, Cabina 9, Cabina 10, Caffè, Ècole, Pedalò |
  | `Descending` | Pedalò, Ècole, Caffè, Cabina 10, Cabina 9, Cabina 2, Cabina 1 |

  Il semplice `ORDER BY name` di oggi (`ServiceCatalogRepository.ListAsync`) non basta: un ordinamento testuale mette
  "Cabina 10" prima di "Cabina 2". Come ottenere l'ordine naturale (per esempio una collation ICU con l'ordine numerico,
  `und-u-kn-true`, o un'altra soluzione) lo decide lo sviluppatore backend, come per la tabella di Impostazioni
  magazzino: l'agente glielo chiede prima di scrivere codice; il risultato deve essere quello della tabella sopra, con
  paginazione e `total` fatti dal database su quell'ordine, anche quando il nome viene da una traduzione.
- **`price`** (dove la tabella lo ha): per prezzo, come numero.
- A parità di valore (stesso prezzo, stessa categoria, nomi che differiscono solo per maiuscole o accenti) l'ordine è
  quello del nome, naturale e crescente, poi l'id interno (`id`, come oggi `ThenBy(s => s.Id)`).

Valori non validi: 400 `validation.failed` con un errore `validation.invalid` sul campo: `page` (meno di 1),
`pageSize` (non 10, 20, 50 o 100), `sortField` (non uno dei campi della sezione né `""`), `sortDirection` (non
`Ascending` o `Descending`), `filters` (un elemento qualsiasi). `search` oltre 100 caratteri: `validation.max_length`
su `search`, con `args.max` 100 (e `args.min` 0). Il frontend non manda questi valori.

### Risposta

200 nello stile delle tabelle: `total` conta tutte le righe dello stabilimento che rispettano la ricerca, `rows` è la
pagina richiesta (al massimo `pageSize` righe), con solo i campi della sezione. Una `page` oltre l'ultima risponde 200
con `rows` vuoto e il `total` vero: il frontend passa da solo all'ultima pagina che c'è.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogRead`. |
| 400 | `validation.failed` | Corpo non valido (sopra, "Valori non validi"), con `errors[]`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |

Il frontend mostra ogni errore di queste API nel popup di errore della tabella (testo fisso, con "Riprova"), senza il
testo del codice.

## 2. POST /api/catalog/bar-service-list

Tabella della tab Servizi Bar (`/settings/services/1`). Per il frontend sostituisce
`GET /api/catalog/services?kind=bar`, che ha la stessa funzione con un'altra richiesta (parametri nell'indirizzo) e
un'altra risposta (`{ total, page, pageSize, items }` con `ServiceListItemDto`).

- Righe: i servizi con `kind` `bar` dello stabilimento.
- `sortField`: `serviceName`, `category`, `price` oppure `""`.
- **`category`** nell'ordinamento: nell'ordine dell'enum `BarCategory` (quello dell'elenco qui sotto, non alfabetico,
  uguale in ogni lingua); i servizi senza categoria in fondo, sia con `Ascending` sia con `Descending`.

```json
{
  "total": 2,
  "rows": [
    {
      "idBarService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01",
      "serviceName": "Caffè",
      "category": "Bar",
      "price": 1.2
    },
    {
      "idBarService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f02",
      "serviceName": "Spritz",
      "category": "Drinks",
      "price": 6
    }
  ]
}
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `idBarService` | `public_id` | string (UUID) | Non si mostra: serve alle azioni della riga. |
| `serviceName` | `name` o traduzione | string | Colonna "Servizio"; vedi "Nome nella lingua dell'utente". |
| `category` | `category` | string o `null` | Colonna "Categoria", con il nome .NET dell'enum `BarCategory`; il frontend mostra il testo della categoria nella lingua dell'utente. |
| `price` | `price` | number | Colonna "Prezzo", in euro. |

Le categorie sono quelle dell'enum del backend `BarCategory` (`Domain/Enums/BarCategory.cs`, tipo
`catalog.bar_category`), con il nome .NET: `Bar`, `Pizzeria`, `Restaurant`, `FryShop`, `SandwichShop`, `Appetizers`,
`FirstCourses`, `SecondCourses`, `Desserts`, `Fruit`, `IceCreamShop`, `Drinks`. Il frontend usa lo stesso elenco
(`BarServiceCategory`).

## 3. POST /api/catalog/standard-service-list

Tabella della tab Servizi (`/settings/services/2`). Per il frontend sostituisce `GET /api/catalog/services?kind=normal`.

- Righe: i servizi con `kind` `normal` dello stabilimento.
- `sortField`: `serviceName`, `price` oppure `""`.

```json
{
  "total": 1,
  "rows": [
    {
      "idStandardService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f11",
      "serviceName": "Doccia calda",
      "price": 3
    }
  ]
}
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `idStandardService` | `public_id` | string (UUID) | Non si mostra: serve alle azioni della riga. |
| `serviceName` | `name` o traduzione | string | Colonna "Servizio"; vedi "Nome nella lingua dell'utente". |
| `price` | `price` | number | Colonna "Prezzo", in euro. |

## 4. POST /api/catalog/hourly-service-list

Tabella della tab Servizi Orari (`/settings/services/3`). Per il frontend sostituisce
`GET /api/catalog/services?kind=hourly`.

- Righe: i servizi con `kind` `hourly` dello stabilimento.
- `sortField`: `serviceName` oppure `""`.

```json
{
  "total": 1,
  "rows": [
    {
      "idHourlyService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f21",
      "serviceName": "Pedalò"
    }
  ]
}
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `idHourlyService` | `public_id` | string (UUID) | Non si mostra: serve alle azioni della riga. |
| `serviceName` | `name` o traduzione | string | Colonna "Servizio"; vedi "Nome nella lingua dell'utente". |

## 5. POST /api/catalog/automatic-price-list

Tabella della tab Prezzi Automatici (`/settings/services/4`). Per il frontend sostituisce
`GET /api/catalog/automatic-prices`, che oggi restituisce tutti i prezzi automatici insieme, senza pagine né ricerca,
con tutti i dati di `AutomaticPriceDto`.

- Righe: i prezzi automatici dello stabilimento (`catalog.automatic_prices`).
- `sortField`: `serviceName`, `price` oppure `""`.

```json
{
  "total": 1,
  "rows": [
    {
      "idAutomaticPrice": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f31",
      "serviceName": "Coperto",
      "price": 2
    }
  ]
}
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `idAutomaticPrice` | `public_id` | string (UUID) | Non si mostra: serve alle azioni della riga. |
| `serviceName` | `name` o traduzione | string | Colonna "Servizio": il nome del prezzo automatico; vedi "Nome nella lingua dell'utente". |
| `price` | `price` | number | Colonna "Prezzo", in euro. |

## 6. POST /api/catalog/table-service-list

Tabella della tab Prenotazione tavoli (`/settings/services/5`). Per il frontend sostituisce
`GET /api/catalog/table-services`, che oggi restituisce tutti i servizi tavoli insieme, senza pagine né ricerca, con
tutti i dati di `TableServiceDto`.

- Righe: i servizi tavoli dello stabilimento (`catalog.table_services`).
- `sortField`: `serviceName` oppure `""`.

```json
{
  "total": 2,
  "rows": [
    {
      "idTableService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f41",
      "serviceName": "Cena"
    },
    {
      "idTableService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f42",
      "serviceName": "Pranzo"
    }
  ]
}
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `idTableService` | `public_id` | string (UUID) | Non si mostra: serve alle azioni della riga. |
| `serviceName` | `name` o traduzione | string | Colonna "Nome servizio"; vedi "Nome nella lingua dell'utente". |

## Regole comuni delle eliminazioni (sezioni 7-11)

**L'unica API con cui il frontend elimina le righe di una tab**, dopo la conferma nel popup: il cestino di una riga la
chiama con un id, "Elimina selezione" della tabella (compare con almeno due righe scelte) con tutti gli id scelti.
Elimina **uno o più elementi in una sola chiamata, tutto o niente**, come `POST /api/catalog/delete-warehouse-item`
(`Impostazioni magazzino - Frontend requests.md`, sezione 5).

### Cosa ci serve

- **Tutto o niente**: se anche un solo elemento della lista non si può eliminare, non si elimina nessuno e la risposta è
  l'errore di quell'elemento. Il popup chiede all'utente di eliminare quegli elementi insieme: non deve ritrovarsi con
  una parte eliminata e una no.
- **API nuove**: le API di oggi restano come sono e il frontend non le usa. `DELETE /api/catalog/services/{id}`,
  `/automatic-prices/{id}` e `/table-services/{id}` eliminano un elemento solo; `POST .../bulk-delete`
  (`DeleteManyAsync` di `ServiceItemsService`, `AutomaticPricesService` e `TableServicesService`, con `BulkIdsRequest` e
  `BulkResult` di `Shared/Dtos/BulkResult.cs`) elimina quelli possibili e restituisce gli altri come falliti, con un
  200: per questa pagina non va bene.
- **Una API per tab**: ogni API elimina solo le righe della sua tab. Per esempio `delete-bar-service-item` accetta solo
  servizi con `kind` `bar`: l'id di un servizio di un'altra tab risponde 404 come un id che non esiste e non si elimina
  niente. Il `bulk-delete` di oggi non guarda il tipo.
- **Stessi controlli ed effetti della `DELETE` di oggi** per ogni elemento (`DeleteAsync` dei tre servizi): l'elemento
  deve essere dello stabilimento, si elimina con il soft delete di sempre e nel registro va una voce per elemento. Per
  i servizi bar, i servizi e i servizi orari vale anche quello che `RemoveAsync` fa già: il servizio esce dai prezzi
  automatici che lo comprendono (`ServiceItemsService.cs:199-204`).
- **Elementi in uso**: oggi nessuna `DELETE` controlla se l'elemento è usato (per esempio da prenotazioni o ordini).
  L'agente chiede allo sviluppatore se servono controlli; se ne nasce un errore nuovo (per esempio un 409 come
  `inventory.item_in_use` del magazzino), va scritto qui con status, `code` e `args`, perché il frontend lo mostri.
- **Come farlo lo sceglie lo sviluppatore del backend** (metodi nuovi nei servizi, controlli su più elementi insieme,
  transazione...), purché il risultato sia questo.
- **Permesso**: `CatalogSettings`, come le `DELETE` e i `bulk-delete` di oggi (`ServiceEndpoints.cs:17`).
- **Stabilimento**: lo prende dal token (`CurrentProperty.IdAsync`); nessuna richiesta lo contiene.

### Richiesta

Il corpo ha un solo campo, con il nome della sezione: un array di id (UUID, il `public_id` delle righe della tabella),
almeno uno. Uno solo per il cestino di una riga, tutti quelli scelti per "Elimina selezione". Lo stesso id ripetuto
conta una volta. La selezione della tabella può comprendere righe di più pagine, quindi anche tutte le righe della tab.

Il frontend non ha un tetto al numero di id. Se ne serve uno (come i 300 di `BulkIdsRequest.MaxIds`), lo decide lo
sviluppatore backend: l'agente glielo chiede prima di scrivere codice e lo scrive qui, così il frontend lo rispetta.

### Risposta

204 senza corpo. Dopo il 204 gli id escono dalla selezione e la tabella si ricarica.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 400 | `validation.failed` | Il campo degli id assente o vuoto: `validation.required` su quel campo in `errors[]`. Il frontend non lo manda. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | il `code` della sezione | Un id non è un elemento dello stabilimento di quella tab (anche perché già eliminato, o perché è di un'altra tab). Non si elimina niente. |

Il frontend mostra ogni errore di queste API nell'avviso "Operazione non riuscita", con il testo del suo `code`; con il
404 della sezione toglie gli id dalla selezione e ricarica la tabella, perché mostri che quegli elementi non ci sono
più.

```json
{
  "type": "https://errors.summerbooking/service/not_found",
  "title": "service.not_found",
  "status": 404,
  "code": "service.not_found",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

## 7. POST /api/catalog/delete-bar-service-item

Cestino e "Elimina selezione" della tab Servizi Bar.

```json
{
  "idBarServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01", "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f02"]
}
```

- `idBarServices`: gli `idBarService` delle righe di `bar-service-list` (sezione 2); solo servizi con `kind` `bar`.
- Un id che non è un servizio bar dello stabilimento: 404 `service.not_found` (`CatalogErrors.ServiceNotFound`).
- Registro: una voce `catalog.service_deleted` per servizio.

## 8. POST /api/catalog/delete-standard-service-item

Cestino e "Elimina selezione" della tab Servizi.

```json
{
  "idStandardServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f11"]
}
```

- `idStandardServices`: gli `idStandardService` delle righe di `standard-service-list` (sezione 3); solo servizi con
  `kind` `normal`.
- Un id che non è un servizio `normal` dello stabilimento: 404 `service.not_found`.
- Registro: una voce `catalog.service_deleted` per servizio.

## 9. POST /api/catalog/delete-hourly-service-item

Cestino e "Elimina selezione" della tab Servizi Orari.

```json
{
  "idHourlyServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f21"]
}
```

- `idHourlyServices`: gli `idHourlyService` delle righe di `hourly-service-list` (sezione 4); solo servizi con `kind`
  `hourly`.
- Un id che non è un servizio orario dello stabilimento: 404 `service.not_found`.
- Registro: una voce `catalog.service_deleted` per servizio.

## 10. POST /api/catalog/delete-automatic-price-item

Cestino e "Elimina selezione" della tab Prezzi Automatici.

```json
{
  "idAutomaticPrices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f31"]
}
```

- `idAutomaticPrices`: gli `idAutomaticPrice` delle righe di `automatic-price-list` (sezione 5).
- Un id che non è un prezzo automatico dello stabilimento: 404 `service.automatic_price_not_found`
  (`CatalogErrors.AutomaticPriceNotFound`).
- Registro: una voce `catalog.automatic_price_deleted` per prezzo automatico.

## 11. POST /api/catalog/delete-table-service-item

Cestino e "Elimina selezione" della tab Prenotazione tavoli.

```json
{
  "idTableServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f41", "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f42"]
}
```

- `idTableServices`: gli `idTableService` delle righe di `table-service-list` (sezione 6).
- Un id che non è un servizio tavoli dello stabilimento: 404 `service.table_service_not_found`
  (`CatalogErrors.TableServiceNotFound`).
- Registro: una voce `catalog.table_service_deleted` per servizio tavoli.

## Regole comuni delle duplicazioni (sezioni 12-16)

**L'unica API con cui il frontend duplica le righe di una tab**, dopo la conferma nel popup: il bottone "Duplica" di
una riga la chiama con un id, "Duplica selezione" della tabella con tutti gli id scelti. Duplica **uno o più elementi
in una sola chiamata, tutto o niente**, come `POST /api/catalog/duplicate-warehouse-item`
(`Impostazioni magazzino - Frontend requests.md`, sezione 6).

### Cosa ci serve

- **Tutto o niente**, come le eliminazioni (sezioni 7-11): se un id non va bene non si duplica nessun elemento.
- **API nuove**: le API di oggi restano come sono e il frontend non le usa. `POST /api/catalog/services/{id}/duplicate`,
  `/automatic-prices/{id}/duplicate` e `/table-services/{id}/duplicate` duplicano un elemento solo e rispondono 201
  con la copia; `POST .../bulk-duplicate` (`DuplicateManyAsync` dei tre servizi) duplica quelli possibili e
  restituisce gli altri come falliti, con un 200: per questa pagina non va bene.
- **Una API per tab**, come le eliminazioni: ogni API duplica solo le righe della sua tab; l'id di un elemento di
  un'altra tab risponde 404 come un id che non esiste e non si duplica niente. Il `bulk-duplicate` di oggi non guarda
  il tipo.
- **Ogni copia è quella che il backend fa già** con la duplicazione di oggi (`CopyAsync` di `ServiceItemsService` e di
  `AutomaticPricesService`, `Copy` di `TableServicesService`):
  - un elemento nuovo dello stesso stabilimento e dello stesso tipo, con un `public_id` nuovo e tutti i dati
    dell'elemento di partenza (`ToRequest` del suo DTO: prezzi, IVA, varianti, prezzi per periodo, settori, opzioni
    di prenotazione, disponibilità, traduzioni...); servizi e prezzi automatici con la stessa icona o la stessa
    immagine caricata (`ServiceItemsService.cs:160-161`, `AutomaticPricesService.cs:140-141`);
  - il **nome lo sceglie il backend** con `CopyNames.Next` (`Domain/CopyNames.cs`): "Caffè copia", poi
    "Caffè copia 2", "Caffè copia 3"; la copia di una copia riparte dal nome di base (se "Caffè copia" e
    "Caffè copia 2" esistono già, la copia di "Caffè copia" è "Caffè copia 3"). Il nome è libero fra quelli dello
    stesso tipo per i servizi (`TakenNamesAsync` di `ServiceItemsService`), fra i prezzi automatici o fra i servizi
    tavoli, senza distinguere maiuscole e minuscole, e al massimo 255 caratteri (`NameMaxLength`), accorciando il
    nome di partenza se serve;
  - **anche i nomi tradotti prendono il suffisso della copia**, ognuno nella sua lingua: `en` " copy", `fr`
    " copie", `es` " copia", `de` " Kopie", `zh` " 副本", `ar` " نسخة", con " 2", " 3"... come in italiano ("Coffee
    copy", "Coffee copy 2"). Oggi la copia tiene i nomi tradotti dell'originale (`ToRequest` copia `Translations` così
    come sono), quindi chi usa un'altra lingua vede in tabella due righe con lo stesso nome ("Coffee"), perché la
    tabella mostra il nome nella lingua dell'utente (sezioni 2-6). Per ogni lingua vale la regola di `CopyNames.Next`
    sui nomi tradotti in quella lingua degli elementi dello stesso tipo: primo nome libero senza distinguere maiuscole
    e minuscole, la copia di una copia riparte dal nome di base, al massimo 255 caratteri (colonna `name` di
    `catalog.service_sale_item_translations`, `catalog.automatic_price_translations` e
    `catalog.table_service_translations`). Il numero di ogni lingua è il primo libero in quella lingua e può essere
    diverso da quello italiano. Le descrizioni restano quelle dell'originale;
  - per i servizi, `created_by_user_id` dell'utente del token (`ServiceSaleItem.Create`), come oggi;
  - la copia di un servizio non entra nei prezzi automatici del servizio di partenza; la copia di un prezzo
    automatico si applica agli stessi servizi del prezzo di partenza; come oggi;
  - nel registro una voce per ogni copia.
- **Nomi in contemporanea**: i nomi non hanno un indice unico, quindi due duplicazioni dello stesso elemento nello
  stesso momento possono dare due copie con lo stesso nome (non un 500, come succedeva nel magazzino). L'agente chiede
  allo sviluppatore se va evitato.
- Se lo stesso id compare più volte, si fa una copia sola.
- **Come farlo lo sceglie lo sviluppatore del backend** (metodi nuovi nei servizi, transazione...), purché il
  risultato sia questo.
- **Permesso**: `CatalogSettings`, come la duplicazione di oggi (`ServiceEndpoints.cs:17`). **Stabilimento** dal token.

### Richiesta, risposta ed errori

- Corpo: un solo campo, con il nome della sezione, come nelle eliminazioni: un array di id (UUID, il `public_id` delle
  righe della tabella), almeno uno; uno solo per il bottone "Duplica" di una riga, tutti quelli scelti per "Duplica
  selezione". La selezione può comprendere righe di più pagine. Per un eventuale tetto al numero di id vale la nota
  delle eliminazioni.
- Risposta: 204 senza corpo. Dopo il 204 la selezione si svuota e la tabella si ricarica: le copie compaiono al loro
  posto nell'ordine della tabella.
- Errori: gli stessi delle eliminazioni, nello stesso ordine (401, 403 `auth.forbidden` senza `CatalogSettings`, 400
  `validation.failed` con `validation.required` sul campo degli id assente o vuoto, 403 `catalog.property_required`, 404
  `catalog.property_not_found`, 404 con il `code` della sezione per un id che non è un elemento dello stabilimento di
  quella tab). Con il 404 della sezione non si duplica niente.
- Il frontend mostra ogni errore nell'avviso "Operazione non riuscita", con il testo del suo `code`; con il 404 della
  sezione toglie gli id dalla selezione e ricarica la tabella.

## 12. POST /api/catalog/duplicate-bar-service-item

Bottone "Duplica" e "Duplica selezione" della tab Servizi Bar.

```json
{
  "idBarServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01"]
}
```

- `idBarServices`: gli `idBarService` delle righe di `bar-service-list` (sezione 2); solo servizi con `kind` `bar`.
- Un id che non è un servizio bar dello stabilimento: 404 `service.not_found`.
- Registro: una voce `catalog.service_duplicated` per copia.

## 13. POST /api/catalog/duplicate-standard-service-item

Bottone "Duplica" e "Duplica selezione" della tab Servizi.

```json
{
  "idStandardServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f11"]
}
```

- `idStandardServices`: gli `idStandardService` delle righe di `standard-service-list` (sezione 3); solo servizi con
  `kind` `normal`.
- Un id che non è un servizio `normal` dello stabilimento: 404 `service.not_found`.
- Registro: una voce `catalog.service_duplicated` per copia.

## 14. POST /api/catalog/duplicate-hourly-service-item

Bottone "Duplica" e "Duplica selezione" della tab Servizi Orari.

```json
{
  "idHourlyServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f21"]
}
```

- `idHourlyServices`: gli `idHourlyService` delle righe di `hourly-service-list` (sezione 4); solo servizi con `kind`
  `hourly`.
- Un id che non è un servizio orario dello stabilimento: 404 `service.not_found`.
- Registro: una voce `catalog.service_duplicated` per copia.

## 15. POST /api/catalog/duplicate-automatic-price-item

Bottone "Duplica" e "Duplica selezione" della tab Prezzi Automatici.

```json
{
  "idAutomaticPrices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f31"]
}
```

- `idAutomaticPrices`: gli `idAutomaticPrice` delle righe di `automatic-price-list` (sezione 5).
- Un id che non è un prezzo automatico dello stabilimento: 404 `service.automatic_price_not_found`.
- Registro: una voce `catalog.automatic_price_duplicated` per copia.

## 16. POST /api/catalog/duplicate-table-service-item

Bottone "Duplica" e "Duplica selezione" della tab Prenotazione tavoli.

```json
{
  "idTableServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f41"]
}
```

- `idTableServices`: gli `idTableService` delle righe di `table-service-list` (sezione 6).
- Un id che non è un servizio tavoli dello stabilimento: 404 `service.table_service_not_found`.
- Registro: una voce `catalog.table_service_duplicated` per copia.

## Verifica

API 1, con uno stabilimento di prova, un servizio bar con le tre tab compilate e il token di un utente che può
modificare i servizi:

1. `get-bar-service-general-info` risponde con `version` (per esempio `v1`); `update-bar-service-general` con
   `If-Match: "v1"` risponde 204 con l'header `ETag` di una versione nuova, diversa da `v1`; una nuova
   `get-bar-service-general-info` risponde con quella versione.
2. Lo stesso `update-bar-service-general` con `If-Match: "v1"` dopo il primo: 409 `resource.version_conflict` senza
   `errors[]`, e la lettura restituisce i dati di prima. Senza `If-Match`: 409 `resource.version_conflict`.
3. Lette le versioni delle tre tab: dopo un salvataggio riuscito di Prezzi le versioni di Info Generali e Prenotazioni
   non cambiano; dopo uno di Prenotazioni non cambiano quelle di Info Generali e Prezzi; dopo uno di Info Generali non
   cambiano quelle di Prezzi e Prenotazioni.
4. `get-bar-service-booking-options` risponde `{ "bookingOptions": [...], "version": "..." }`; per un servizio senza
   opzioni `bookingOptions` è `[]`. Lo stesso per `get-standard-service-booking-options` e
   `get-hourly-service-booking-options`.
5. Le prove 1, 2 e 3 riescono anche per ogni altra coppia della tabella della sezione 1 (servizi, servizi orari,
   prezzi automatici, servizi tavoli).
6. Un salvataggio con un id che non esiste e una `If-Match` vecchia risponde 404 con il codice dell'elemento (per
   esempio `service.not_found`), non 409; uno con un corpo non valido e una `If-Match` vecchia risponde 400
   `validation.failed`.
7. Duplicato un servizio, un salvataggio riuscito della copia non cambia le versioni del servizio di partenza.

API 2-6, con uno stabilimento di prova e il token di un utente con `CatalogRead`:

8. `bar-service-list` con 12 servizi bar, `page` 1 e `pageSize` 10: `total` 12 e 10 righe con solo `idBarService`,
   `serviceName`, `category` e `price`; `page` 2 dà le ultime 2; `page` 3 dà `rows` vuoto e `total` 12.
9. Le righe di `bar-service-list` sono solo i servizi `bar`, quelle di `standard-service-list` solo i `normal`, quelle
   di `hourly-service-list` solo gli `hourly`; un servizio eliminato o di un altro stabilimento non compare in nessuna.
10. Con nomi "Cabina 10", "Cabina 2", "Caffè", "Ècole" e `sortField` `serviceName`: con `Ascending` Cabina 2,
    Cabina 10, Caffè, Ècole; con `Descending` l'inverso. Con `sortField` `""` lo stesso ordine di `Ascending`.
11. Un servizio "Caffè" con la traduzione inglese "Coffee": con `Accept-Language: en` la riga ha "Coffee" e `search`
    "coff" la trova, "caff" no; con `Accept-Language: fr` ha "Caffè". Lo stesso per prezzi automatici e servizi
    tavoli.
12. `sortField` `price` con prezzi 3, 1.2, 3: con `Ascending` 1.2 e poi i due da 3 per nome; con `Descending` i due da
    3 per nome e poi 1.2.
13. `search` "50%" non trova "Sconto 500" (il `%` non è un carattere jolly); `search` di 101 caratteri: 400 con
    `validation.max_length` su `search`.
14. `pageSize` 25, `page` 0, `sortField` `name`, `sortDirection` `asc` o `filters` con un elemento: 400
    `validation.failed` con `validation.invalid` sul campo.
15. `automatic-price-list` e `table-service-list` con 11 righe e `pageSize` 10: `total` 11 e 10 righe.
16. Senza token 401 `auth.unauthenticated`; senza `CatalogRead` 403 `auth.forbidden`; token senza stabilimento 403
    `catalog.property_required`.

API 7-11, con un token con `CatalogSettings`:

17. `delete-bar-service-item` di due servizi bar: 204; non compaiono più in `bar-service-list` e nel registro ci sono
    due voci `catalog.service_deleted`. Con un solo id: 204 e una voce.
18. Un servizio bar dello stabilimento e uno di un altro stabilimento o già eliminato: 404 `service.not_found`; nessuno
    dei due è eliminato.
19. `delete-bar-service-item` con l'id di un servizio `normal` o `hourly`: 404 `service.not_found` e il servizio non
    cambia. Lo stesso per `delete-standard-service-item` e `delete-hourly-service-item` con gli id delle altre tab.
20. Un servizio compreso in un prezzo automatico: 204 e il prezzo automatico non lo comprende più, come con la
    `DELETE` di oggi.
21. Lo stesso id due volte (`[A, A]`): 204 e una sola voce nel registro.
22. Array vuoto: 400 `validation.failed` con `validation.required` sul campo degli id; senza `CatalogSettings` 403
    `auth.forbidden`; token senza stabilimento 403 `catalog.property_required`.
23. Le prove 17, 18, 21 e 22 riescono anche con `delete-automatic-price-item` (404
    `service.automatic_price_not_found`, voci `catalog.automatic_price_deleted`) e con `delete-table-service-item`
    (404 `service.table_service_not_found`, voci `catalog.table_service_deleted`).

API 12-16, con un token con `CatalogSettings` e il servizio bar "Caffè" con la traduzione inglese "Coffee", un
prezzo e un'immagine caricata:

24. `duplicate-bar-service-item` con l'id di "Caffè": 204; in `bar-service-list` c'è un servizio nuovo "Caffè copia",
    con un id diverso, lo stesso prezzo, la stessa categoria, la stessa immagine e la traduzione inglese
    "Coffee copy"; "Caffè" non cambia; nel registro una voce `catalog.service_duplicated`.
25. Di nuovo con l'id di "Caffè": "Caffè copia 2" e "Coffee copy 2". Poi con l'id di "Caffè copia": "Caffè copia 3" e
    "Coffee copy 3".
26. Con due id diversi: 204 e due copie; con lo stesso id ripetuto due volte: una copia sola.
27. Un servizio bar dello stabilimento e uno di un altro stabilimento o già eliminato: 404 `service.not_found` e
    nessuna copia, nemmeno del servizio valido.
28. `duplicate-bar-service-item` con l'id di un servizio `normal` o `hourly`: 404 `service.not_found` e nessuna copia.
    Lo stesso per `duplicate-standard-service-item` e `duplicate-hourly-service-item` con gli id delle altre tab.
29. Un servizio con un nome di 255 caratteri: la copia ha al massimo 255 caratteri e finisce con " copia".
30. Array vuoto: 400 `validation.failed` con `validation.required` sul campo degli id; senza `CatalogSettings` 403
    `auth.forbidden`; senza token 401 `auth.unauthenticated`.
31. Le prove 24, 25, 26, 27, 30 e 32 riescono anche con `duplicate-automatic-price-item` (404
    `service.automatic_price_not_found`, voce `catalog.automatic_price_duplicated`; la copia si applica agli stessi
    servizi) e con `duplicate-table-service-item` (404 `service.table_service_not_found`, voce
    `catalog.table_service_duplicated`).
32. Un servizio con il nome in tutte e 7 le lingue: la copia ha in ogni lingua il suffisso della sua lingua (" copia",
    " copy", " copie", " copia", " Kopie", " 副本", " نسخة") e le stesse descrizioni. Se in inglese "Coffee copy" esiste
    già ma in italiano "Caffè copia" no, la copia è "Caffè copia" e "Coffee copy 2".
