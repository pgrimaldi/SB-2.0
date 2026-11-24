# Servizi - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Servizi** del gestionale funzioni come la vuole il frontend.
Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare o cambiare, con regole,
errori attesi e prove.

La pagina è `/settings/services` e ha cinque tab: Servizi Bar, Servizi (servizi generici), Servizi Orari, Prezzi
Automatici e Prenotazione tavoli. Ogni tab è una tabella; la matita di una riga apre un pannello di modifica con due o
tre tab interne, ognuna letta con la sua GET e salvata con la sua API, una tab alla volta.

Le API della pagina (`/api/catalog/services/bar-service-list`, `/api/catalog/services/get-bar-service-general-info`,
`/api/catalog/services/update-bar-service-general`…) non ci sono ancora nel backend, che ha `/api/catalog/services`,
`/api/catalog/automatic-prices` e `/api/catalog/table-services` con altre rotte e altri corpi
(`Endpoints/ServiceEndpoints.cs`). Rotte, richieste e risposte sono nei contratti delle API
(`shared documentation/API_CONTRACTS.docx` e `shared documentation/API - Full list.md`); questo documento chiede quello
che va aggiunto a quei contratti e, una sezione alla volta, le API che il backend deve creare (sezioni 2-6, le tabelle
delle cinque tab; sezioni 7-11, le eliminazioni; sezioni 12-16, le duplicazioni; sezioni 17-24, i pannelli di Servizi
Bar; sezione 25, la tendina dei settori, che serve anche a Listino e Prenotazioni online; sezioni 26-32, i pannelli di
Servizi; sezioni 33-39, i pannelli di Servizi Orari; sezioni 40-46, i pannelli di Prezzi Automatici;
sezioni 47-51, i pannelli di Prenotazione tavoli).

Le rotte nuove stanno sempre **sotto la radice che il backend ha già per quei dati**: `/api/catalog/services`,
`/api/catalog/automatic-prices`, `/api/catalog/table-services`, `/api/catalog/sectors`.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET + POST | Le 13 coppie lettura e salvataggio dei pannelli di modifica (tabella della sezione 1) | Controllo di versione, una versione per tab: `version` nelle letture, `If-Match` ed `ETag` nei salvataggi, 409 `resource.version_conflict` |
| 2 | POST | `/api/catalog/services/bar-service-list` | Tabella della tab Servizi Bar, a pagine |
| 3 | POST | `/api/catalog/services/standard-service-list` | Tabella della tab Servizi, a pagine |
| 4 | POST | `/api/catalog/services/hourly-service-list` | Tabella della tab Servizi Orari, a pagine |
| 5 | POST | `/api/catalog/automatic-prices/automatic-price-list` | Tabella della tab Prezzi Automatici, a pagine |
| 6 | POST | `/api/catalog/table-services/table-service-list` | Tabella della tab Prenotazione tavoli, a pagine |
| 7 | POST | `/api/catalog/services/delete-bar-service-item` | Elimina uno o più servizi bar, tutto o niente |
| 8 | POST | `/api/catalog/services/delete-standard-service-item` | Elimina uno o più servizi, tutto o niente |
| 9 | POST | `/api/catalog/services/delete-hourly-service-item` | Elimina uno o più servizi orari, tutto o niente |
| 10 | POST | `/api/catalog/automatic-prices/delete-automatic-price-item` | Elimina uno o più prezzi automatici, tutto o niente |
| 11 | POST | `/api/catalog/table-services/delete-table-service-item` | Elimina uno o più servizi tavoli, tutto o niente |
| 12 | POST | `/api/catalog/services/duplicate-bar-service-item` | Duplica uno o più servizi bar, tutto o niente |
| 13 | POST | `/api/catalog/services/duplicate-standard-service-item` | Duplica uno o più servizi, tutto o niente |
| 14 | POST | `/api/catalog/services/duplicate-hourly-service-item` | Duplica uno o più servizi orari, tutto o niente |
| 15 | POST | `/api/catalog/automatic-prices/duplicate-automatic-price-item` | Duplica uno o più prezzi automatici, tutto o niente |
| 16 | POST | `/api/catalog/table-services/duplicate-table-service-item` | Duplica uno o più servizi tavoli, tutto o niente |
| 17 | POST | `/api/catalog/services/create-bar-service` | Crea un servizio bar con i dati delle tre tab e risponde con il suo id |
| 18 | GET | `/api/catalog/services/get-bar-service-general-info` | Legge la tab Info Generali di un servizio bar |
| 19 | GET | `/api/catalog/services/get-bar-service-prices` | Legge la tab Prezzi di un servizio bar |
| 20 | GET | `/api/catalog/services/get-bar-service-booking-options` | Legge la tab Prenotazioni di un servizio bar |
| 21 | POST | `/api/catalog/services/update-bar-service-general` | Salva la tab Info Generali di un servizio bar |
| 22 | POST | `/api/catalog/services/update-bar-service-prices` | Salva la tab Prezzi di un servizio bar |
| 23 | POST | `/api/catalog/services/update-bar-service-booking` | Salva la tab Prenotazioni di un servizio bar |
| 24 | POST + DELETE | `/api/catalog/services/{id}/image` | Ci sono già e restano come sono: il frontend le usa per caricare e togliere l'immagine del servizio |
| 25 | POST | `/api/catalog/sectors/sector-combobox` | Settori e file per le tendine, tutti insieme (anche Listino e Prenotazioni online) |
| 26 | POST | `/api/catalog/services/create-standard-service` | Crea un servizio con i dati delle tre tab e risponde con il suo id |
| 27 | GET | `/api/catalog/services/get-standard-service-general-info` | Legge la tab Info Generali di un servizio |
| 28 | GET | `/api/catalog/services/get-standard-service-prices` | Legge la tab Prezzi di un servizio |
| 29 | GET | `/api/catalog/services/get-standard-service-booking-options` | Legge la tab Prenotazioni di un servizio |
| 30 | POST | `/api/catalog/services/update-standard-service-general` | Salva la tab Info Generali di un servizio |
| 31 | POST | `/api/catalog/services/update-standard-service-prices` | Salva la tab Prezzi di un servizio |
| 32 | POST | `/api/catalog/services/update-standard-service-booking` | Salva la tab Prenotazioni di un servizio |
| 33 | POST | `/api/catalog/services/create-hourly-service` | Crea un servizio orario con i dati delle tre tab e risponde con il suo id |
| 34 | GET | `/api/catalog/services/get-hourly-service-general-info` | Legge la tab Info Generali di un servizio orario |
| 35 | GET | `/api/catalog/services/get-hourly-service-prices` | Legge la tab Prezzi di un servizio orario |
| 36 | GET | `/api/catalog/services/get-hourly-service-booking-options` | Legge la tab Prenotazioni di un servizio orario |
| 37 | POST | `/api/catalog/services/update-hourly-service-general` | Salva la tab Info Generali di un servizio orario |
| 38 | POST | `/api/catalog/services/update-hourly-service-prices` | Salva la tab Prezzi di un servizio orario |
| 39 | POST | `/api/catalog/services/update-hourly-service-booking` | Salva la tab Prenotazioni di un servizio orario |
| 40 | POST | `/api/catalog/automatic-prices/create-automatic-price` | Crea un prezzo automatico con i dati delle due tab e risponde con il suo id |
| 41 | GET | `/api/catalog/automatic-prices/get-automatic-price-services` | I servizi dello stabilimento per "Validità sui servizi" |
| 42 | GET | `/api/catalog/automatic-prices/get-automatic-price-general-info` | Legge la tab Info Generali di un prezzo automatico |
| 43 | GET | `/api/catalog/automatic-prices/get-automatic-price-prices-validity` | Legge la tab Prezzi e Validità di un prezzo automatico |
| 44 | POST | `/api/catalog/automatic-prices/update-automatic-price-general` | Salva la tab Info Generali di un prezzo automatico |
| 45 | POST | `/api/catalog/automatic-prices/update-automatic-price-prices-validity` | Salva la tab Prezzi e Validità di un prezzo automatico |
| 46 | POST + DELETE | `/api/catalog/automatic-prices/{id}/image` | POST: c'è già e resta com'è. DELETE: nuova, toglie l'immagine |
| 47 | POST | `/api/catalog/table-services/create-table-service` | Crea un servizio tavoli con i dati delle due tab |
| 48 | GET | `/api/catalog/table-services/get-table-service-general-info` | Legge la tab Info Generali di un servizio tavoli |
| 49 | GET | `/api/catalog/table-services/get-table-service-availability-bookings` | Legge la tab Disponibilità e Prenotazioni di un servizio tavoli |
| 50 | POST | `/api/catalog/table-services/update-table-service-general` | Salva la tab Info Generali di un servizio tavoli |
| 51 | POST | `/api/catalog/table-services/update-table-service-availability-bookings` | Salva la tab Disponibilità e Prenotazioni di un servizio tavoli |

## 1. Controllo di versione sui salvataggi dei pannelli di modifica

Senza un controllo, se due utenti modificano la stessa tab dello stesso servizio vince chi salva per ultimo: ogni Salva
rimanda tutta la tab letta all'apertura (il frontend rimanda sempre tutto il modello letto) e cancella, senza che
nessuno se ne accorga, quello che l'altro utente ha cambiato nel frattempo. Il frontend chiede la **concorrenza
ottimistica** degli altri documenti, con un **codice di errore unico per tutte le API**: `resource.version_conflict`.

**Non è quello che Booking ha già per le prenotazioni** (`Endpoints.cs:253-255`, `ReservationService.cs:114-115`): lì
l'`If-Match` è un numero, se manca o non si legge non si controlla niente, e il conflitto è 412 o 409
`booking.version_conflict`. Qui valgono le regole sotto: con un altro codice il frontend non rileggerebbe i dati.

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
      "weekdays": ["Saturday", "Sunday"],
      "managementStartTime": "08:00",
      "managementEndTime": "12:00",
      "onlineStartTime": "08:30",
      "onlineEndTime": "11:30",
      "maxBookingPerHourQuantity": 20,
      "maxBookingPerDayQuantity": 80,
      "maxPerSlot": 5,
      "intervalMinutes": 30,
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
  "variants": [{ "variantName": "Doppio", "price": 0.8 }],
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
- Il 409 si controlla dopo il 400 `validation.failed` e il 404 dell'elemento (`service.not_found`,
  `service.automatic_price_not_found` o `service.table_service_not_found`), e **prima dei controlli sui dati collegati**: IVA,
  icona, periodi, settori e, nei Prezzi automatici, i servizi scelti. Esempio: un altro utente toglie un settore dal servizio e lo
  elimina; con l'ordine opposto chi salva riceverebbe 404 `catalog.sector_not_found`, toglierebbe il settore e solo al secondo
  Salva un 409 che gli fa perdere le modifiche.
- **Controllo e salvataggio sono una cosa sola**: di due salvataggi della stessa tab nello stesso momento con lo stesso
  `If-Match` uno riesce e l'altro risponde 409 `resource.version_conflict`; mai i dati di tutti e due mescolati, mai un 500.
- La copia fatta da Duplica ha le sue versioni: salvarla non cambia quelle del servizio di partenza.
- **Come farlo lo sceglie lo sviluppatore del backend** (un hash dei dati coperti, un contatore per tab...): l'agente
  glielo chiede prima di scrivere codice. Il frontend tratta la versione come una stringa opaca e non la confronta mai.

### API coinvolte

Le letture hanno l'id nella query string, i salvataggi nel corpo. I dati coperti dalla versione sono i campi del corpo
del salvataggio, tranne l'id.

| Pannello e tab | Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|---|
| Servizio bar, Info Generali | `GET /api/catalog/services/get-bar-service-general-info?idBarService=…` | `POST /api/catalog/services/update-bar-service-general` | `translations`, `category`, `icon`, `isHighlighted` |
| Servizio bar, Prezzi | `GET /api/catalog/services/get-bar-service-prices?idBarService=…` | `POST /api/catalog/services/update-bar-service-prices` | `price`, `vatRate`, `variants`, `isPeriodPricingEnabled`, `periodPrices` (anche dei periodi che la tab non mostra), `idSectors`, `isOnlinePurchaseEnabled` |
| Servizio bar, Prenotazioni | `GET /api/catalog/services/get-bar-service-booking-options?idBarService=…` | `POST /api/catalog/services/update-bar-service-booking` | `bookingOptions` |
| Servizio, Info Generali | `GET /api/catalog/services/get-standard-service-general-info?idStandardService=…` | `POST /api/catalog/services/update-standard-service-general` | `translations`, `icon`, `isHighlighted` |
| Servizio, Prezzi | `GET /api/catalog/services/get-standard-service-prices?idStandardService=…` | `POST /api/catalog/services/update-standard-service-prices` | `price`, `vatRate`, `isPeriodPricingEnabled`, `periodPrices` (anche dei periodi che la tab non mostra), `idSectors`, `isOnlinePurchaseEnabled` |
| Servizio, Prenotazioni | `GET /api/catalog/services/get-standard-service-booking-options?idStandardService=…` | `POST /api/catalog/services/update-standard-service-booking` | `bookingOptions` |
| Servizio orario, Info Generali | `GET /api/catalog/services/get-hourly-service-general-info?idHourlyService=…` | `POST /api/catalog/services/update-hourly-service-general` | `translations`, `icon`, `isHighlighted` |
| Servizio orario, Prezzi | `GET /api/catalog/services/get-hourly-service-prices?idHourlyService=…` | `POST /api/catalog/services/update-hourly-service-prices` | `price`, `vatRate`, `isPeriodPricingEnabled`, `availabilityOptions`, `periodAvailabilities` (anche dei periodi che la tab non mostra), `idSectors`, `isOnlinePurchaseEnabled` |
| Servizio orario, Prenotazioni | `GET /api/catalog/services/get-hourly-service-booking-options?idHourlyService=…` | `POST /api/catalog/services/update-hourly-service-booking` | `bookingOptions` |
| Prezzo automatico, Info Generali | `GET /api/catalog/automatic-prices/get-automatic-price-general-info?idAutomaticPrice=…` | `POST /api/catalog/automatic-prices/update-automatic-price-general` | `translations`, `icon` |
| Prezzo automatico, Prezzi e Validità | `GET /api/catalog/automatic-prices/get-automatic-price-prices-validity?idAutomaticPrice=…` | `POST /api/catalog/automatic-prices/update-automatic-price-prices-validity` | `price`, `vatRate`, `isOnlinePurchaseEnabled`, `validityPeriods`, `idBarServices`, `idStandardServices`, `idHourlyServices`, `isPricePerServiceEnabled`, `idSectors` |
| Servizio tavoli, Info Generali | `GET /api/catalog/table-services/get-table-service-general-info?idTableService=…` | `POST /api/catalog/table-services/update-table-service-general` | `translations`, `isHighlighted` |
| Servizio tavoli, Disponibilità e Prenotazioni | `GET /api/catalog/table-services/get-table-service-availability-bookings?idTableService=…` | `POST /api/catalog/table-services/update-table-service-availability-bookings` | `availabilities`, `availabilityMode`, `tableGroups`, `totalSeatQuantity`, `averageMealMinutes`, `maxBookingPerTimeSlotQuantity`, `minSeatPerBookingQuantity`, `maxSeatPerBookingQuantity`, `bookingPayment`, `depositMode`, `depositPercentage`, `depositAmount`, `isOnlineBookingEnabled` |

Le altre API della pagina non ne hanno bisogno: elenchi, aggiunta, eliminazione e duplicazione non rimandano dati letti,
e `get-automatic-price-services` e `catalog/tariff-periods` servono solo a riempire le tab.

### Cosa fa il frontend

Con 409 `resource.version_conflict` il pannello mostra l'avviso "Nel frattempo un altro utente ha modificato questi
dati: li abbiamo ricaricati. Rifai le tue modifiche.", la tab torna in sola lettura e si rilegge con la sua GET, e la
tabella si ricarica; il messaggio sopra i bottoni non compare. Dopo un salvataggio riuscito il pannello si chiude e alla riapertura rilegge le
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
`serviceName` lavorano su questo nome, come `packageName` di `POST /api/catalog/price-lists/packages-list`
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

## 2. POST /api/catalog/services/bar-service-list

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

## 3. POST /api/catalog/services/standard-service-list

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

## 4. POST /api/catalog/services/hourly-service-list

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

## 5. POST /api/catalog/automatic-prices/automatic-price-list

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

## 6. POST /api/catalog/table-services/table-service-list

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
- **Nessun altro controllo**, come le `DELETE` di oggi: un elemento usato da prenotazioni o ordini si può eliminare, con il
  soft delete.
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

## 7. POST /api/catalog/services/delete-bar-service-item

Cestino e "Elimina selezione" della tab Servizi Bar.

```json
{
  "idBarServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01", "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f02"]
}
```

- `idBarServices`: gli `idBarService` delle righe di `bar-service-list` (sezione 2); solo servizi con `kind` `bar`.
- Un id che non è un servizio bar dello stabilimento: 404 `service.not_found` (`CatalogErrors.ServiceNotFound`).
- Registro: una voce `catalog.service_deleted` per servizio.

## 8. POST /api/catalog/services/delete-standard-service-item

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

## 9. POST /api/catalog/services/delete-hourly-service-item

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

## 10. POST /api/catalog/automatic-prices/delete-automatic-price-item

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

## 11. POST /api/catalog/table-services/delete-table-service-item

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
  stesso momento possono dare due copie con lo stesso nome (non un 500, come succedeva nel magazzino). Va bene così:
  l'utente può rinominarle.
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
- **Un elemento che non si può copiare** (sezioni 12-15) risponde con un errore suo, che dice quale elemento e perché, e
  non si duplica niente. Oggi la copia rifà i controlli del salvataggio (`CopyAsync` chiama `ApplyAsync`) e può rispondere
  422 `catalog.vat_rate_not_allowed`, 422 `pricing.period_not_found` o 404 `service.icon_not_found`, senza dire di quale
  elemento: con più righe scelte l'utente non può saperlo. Codici nuovi (`CatalogErrors`), dopo il 404 della sezione, sul
  primo elemento che non va nell'ordine degli id:

  | Status | `code` | Quando | `args` |
  |---|---|---|---|
  | 422 | `service.duplicate_vat_rate_not_allowed` | L'IVA dell'elemento, o di una disponibilità di un servizio orario, non è più tra le aliquote ammesse (`VatPolicy.ResolveAsync`). | `serviceName`, `rates` |
  | 422 | `service.duplicate_period_not_found` | Un servizio ha prezzi o disponibilità per un periodo eliminato. | `serviceName` |
  | 422 | `service.duplicate_icon_not_found` | L'icona dell'elemento non è più nel catalogo. | `serviceName` |
  | 422 | `service.duplicate_sector_not_found` | L'elemento è limitato anche a un settore eliminato. | `serviceName` |

  `serviceName` è il nome nella lingua di `Accept-Language`, come nelle tabelle. I servizi tavoli (sezione 16) non hanno
  controlli da rifare.
- **Un settore eliminato fa sempre fallire la copia**, anche se ce ne sono altri validi. Oggi invece si perde in silenzio
  (`ToDtoAsync` di `ServiceItemsService` e di `AutomaticPricesService` lo esclude) e la copia è diversa dall'originale.
  L'utente risolve togliendo quel settore nel pannello, che lo mostra come "Settore/fila non più disponibile" ("Settori
  eliminati" nelle regole comuni dei pannelli di Servizi Bar).
- **Un periodo eliminato fa fallire la copia**, come un settore: eliminato = ha solo righe eliminate in
  `catalog.pricing_tariff_periods`. I prezzi e le disponibilità con una `key` senza nessuna riga in quella tabella invece **si
  copiano come sono**, come li tiene il salvataggio: oggi `ApplyAsync` li rifiuta con 422
  `pricing.period_not_found`. L'utente risolve salvando la tab Prezzi: le letture non danno i prezzi dei periodi eliminati
  (sezioni 19, 28 e 35), quindi il salvataggio li toglie.
- Il frontend mostra ogni errore nell'avviso "Operazione non riuscita", con il testo del suo `code`; con il 404 della
  sezione toglie gli id dalla selezione e ricarica la tabella.

## 12. POST /api/catalog/services/duplicate-bar-service-item

Bottone "Duplica" e "Duplica selezione" della tab Servizi Bar.

```json
{
  "idBarServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01"]
}
```

- `idBarServices`: gli `idBarService` delle righe di `bar-service-list` (sezione 2); solo servizi con `kind` `bar`.
- Un id che non è un servizio bar dello stabilimento: 404 `service.not_found`.
- Registro: una voce `catalog.service_duplicated` per copia.

## 13. POST /api/catalog/services/duplicate-standard-service-item

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

## 14. POST /api/catalog/services/duplicate-hourly-service-item

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

## 15. POST /api/catalog/automatic-prices/duplicate-automatic-price-item

Bottone "Duplica" e "Duplica selezione" della tab Prezzi Automatici.

```json
{
  "idAutomaticPrices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f31"]
}
```

- `idAutomaticPrices`: gli `idAutomaticPrice` delle righe di `automatic-price-list` (sezione 5).
- Un id che non è un prezzo automatico dello stabilimento: 404 `service.automatic_price_not_found`.
- Registro: una voce `catalog.automatic_price_duplicated` per copia.

## 16. POST /api/catalog/table-services/duplicate-table-service-item

Bottone "Duplica" e "Duplica selezione" della tab Prenotazione tavoli.

```json
{
  "idTableServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f41"]
}
```

- `idTableServices`: gli `idTableService` delle righe di `table-service-list` (sezione 6).
- Un id che non è un servizio tavoli dello stabilimento: 404 `service.table_service_not_found`.
- Registro: una voce `catalog.table_service_duplicated` per copia.

## Regole comuni dei pannelli di Servizi Bar (sezioni 17-24)

I pannelli "Aggiungi nuovo servizio bar" e "Modifica servizio bar" hanno tre tab: Info Generali, Prezzi e Prenotazioni.
L'aggiunta manda le tre tab insieme; la modifica legge e salva **una tab alla volta**, ognuna con la sua GET e il suo
salvataggio.

### Cosa ci serve

- **API nuove sotto `/api/catalog/services`**, la radice che il backend ha già (`Endpoints/ServiceEndpoints.cs:19`). I
  nomi non si confondono con `/{id:guid}`, perché non sono UUID. `GET` e `POST /api/catalog/services`,
  `GET` e `PUT /api/catalog/services/{id}` restano come sono: per questi pannelli il frontend non le usa.
- **Corpo e risposta sono quelli di questo documento**, con questi nomi. Le tabelle delle sezioni dicono a quale dato
  di `ServiceSaleItem` (`catalog.service_sale_items` e tabelle collegate) corrisponde ogni campo.
- **Una tab non tocca le altre**: ogni salvataggio scrive solo i dati della sua tab. Restano com'erano i dati delle
  altre due e tutto quello che il corpo non contiene, per esempio `allergens` e l'immagine caricata
  (`icon_asset_id`). Il `PUT /api/catalog/services/{id}` di oggi invece riscrive tutto il servizio
  (`ServiceItemsService.ApplyAsync`), quindi non si può riusare così com'è.
- **Quello che il form non mostra torna com'era**: le letture danno anche i dati che il pannello non fa vedere (i giorni
  `weekdays`, il massimo per fascia `maxPerSlot`, un intervallo diverso da quelli della tendina, un'icona fuori dalle 22
  del frontend) e il pannello li rimanda uguali, finché l'utente non sceglie un altro valore. Un Salva non li toglie mai.
- **Settori eliminati**: `idSectors` dà tutti i settori a cui l'elemento è limitato, **anche quelli eliminati** (oggi `ToDtoAsync`
  li esclude): il pannello li mostra come "Settore/fila non più disponibile" e li rimanda finché l'utente non li toglie. Un
  salvataggio che ne contiene uno risponde 404 `catalog.sector_not_found`. Così un limite non diventa mai "tutti i settori"
  (`[]`) senza che l'utente lo scelga, nemmeno se i suoi settori sono tutti eliminati. Riguarda i dati di prima: da ora
  l'eliminazione di un settore usato è bloccata (`Settori e file - Frontend requests.md`, sezione 2). Vale per le letture
  delle sezioni 19, 28, 35 e 43 e i loro salvataggi.
- **Solo servizi bar**: le letture e i salvataggi accettano solo servizi con `kind` `bar` dello stabilimento; ogni altro
  id risponde 404 `service.not_found`.
- **Permesso**: `CatalogRead` per le letture, `CatalogSettings` per creazione e salvataggi, come oggi
  (`ServiceEndpoints.cs:16-17`). **Stabilimento** dal token (`CurrentProperty.IdAsync`): nessuna richiesta lo contiene.
- **Controllo di versione**: le tre letture restituiscono `version`, i tre salvataggi la ricevono in `If-Match` e
  rispondono con `ETag`, come dice la sezione 1.
- **Registro**: `catalog.service_created` per la creazione, `catalog.service_updated` per ogni salvataggio, come oggi.
- **JSON** in camelCase. Enum con il nome .NET. Giorni in UTC ISO 8601 (`"2026-07-01T00:00:00.000Z"`), orari `"HH:mm"`,
  nella richiesta e nella risposta.
- **Errori**: gli stessi codici che il backend usa già per questi dati (`SaveServiceRequestValidator`, `VatPolicy`,
  `ContentMapping`, `UploadRules`). Il frontend li conosce e li traduce tutti; un codice nuovo va prima scritto qui.
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.

### Nome e descrizione nelle lingue

- `translations` ha nella richiesta sempre tutte e 7 le lingue (`it`, `en`, `fr`, `de`, `es`, `ar`, `zh`), con
  `serviceName` e `description` `null` dove l'utente non ha scritto niente. Nella risposta ci sono solo le lingue con
  almeno uno dei due testi: le altre le aggiunge vuote il frontend.
- Sono obbligatori **nome e descrizione nella lingua di `Accept-Language`**, quella dell'utente, che nel pannello è la
  principale e non è sempre l'italiano. Oggi invece il backend vuole sempre il nome italiano (`name`) e non accetta
  l'italiano fra le `translations` (`ContentRules.Translations`).
- Nella colonna `name`, che non può essere vuota, va il nome italiano se c'è, altrimenti quello nella lingua di
  `Accept-Language`, come per i pacchetti (`Listino - Frontend requests.md`, sezione 5). Se
  l'italiano non è stato scritto, la lettura non deve restituire quel nome come italiano.
- Nelle altre lingue il **nome è obbligatorio se c'è la descrizione**, come vuole già il backend (`ContentRules.Translations`
  e la colonna `name` di `catalog.service_sale_item_translations`); la descrizione da sola non si può salvare. Il
  frontend non la manda.
- Nome al massimo 255 caratteri (`ServiceSaleItem.NameMaxLength`), descrizione al massimo 10.000.

### Valori che il modello di oggi non può salvare

Il frontend li manda e si aspetta di rileggerli uguali: il risultato è questo, come salvarli lo decide lo sviluppatore.

1. **Italiano non scritto**: il nome italiano **non è obbligatorio**, come per i pacchetti (`Listino - Frontend
   requests.md`, sezione 5). Un utente inglese che scrive solo "Coffee" salva: la colonna `name` contiene "Coffee", ma
   la lettura deve dare "Coffee" in `en` e nessuna traduzione `it`. Come ricordare che quel `name` non è italiano lo
   decide lo sviluppatore, allo stesso modo per servizi e pacchetti.
2. **`icon`**: oggi l'icona è una riga di `catalog.service_type_icons` (`type`, `name`), scelta con il suo id
   (`GET /api/catalog/lookups/service-icons`). Il frontend ha un elenco fisso di 22 nomi (sezione 17), che devono valere
   **tutti**: come farli corrispondere alle righe, e quali aggiungere al catalogo (vedi "Icone da controllare con lo
   sviluppatore", in fondo). Per un servizio con un'icona fuori da questi 22 la lettura dà un nome che identifica quella
   riga (per esempio il suo `name`), diverso dai 22, e `update-bar-service-general` lo riceve uguale e lascia l'icona
   com'è.

### Errori

Nell'ordine in cui si controllano. Le righe 404 `service.not_found` e 409 valgono per letture e salvataggi dove la
sezione lo dice.

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso (`CatalogRead` o `CatalogSettings`). |
| 400 | `validation.failed` | Corpo non valido, con un elemento di `errors[]` per campo (tabella qui sotto). |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `service.not_found` | `idBarService` non è un servizio bar dello stabilimento (anche perché già eliminato). Niente cambia. |
| 409 | `resource.version_conflict` | `If-Match` non è più la versione dei dati della tab (sezione 1). Niente cambia. |
| 422 | `catalog.vat_rate_not_allowed` | `vatRate` non è un'aliquota del Paese dello stabilimento, con `args` `{ "rates": [4, 5, 10, 22] }` come oggi (`VatPolicy.ResolveAsync`). Niente cambia. |
| 404 | `service.icon_not_found` | `icon` non ha un'icona corrispondente nel catalogo (`CatalogErrors.ServiceIconNotFound`). Niente cambia. |
| 422 | `pricing.period_not_found` | Un `idPeriod` di `periodPrices` non è un periodo della stagione corrente e non è già salvato su quel servizio. Niente cambia. |
| 404 | `catalog.sector_not_found` | Un id di `idSectors` non è un settore dello stabilimento (`ContentMapping.SectorIdsAsync`). Niente cambia. |

Un controllo che ha bisogno del servizio salvato si fa dopo il 404 `service.not_found`, e risponde sempre con lo stesso 400
`validation.failed` e il suo `errors[]`: qui l'icona che il servizio ha già (`icon` di `update-bar-service-general`).

Errori di campo del 400, con `field` uguale al nome nel corpo:

| Campo | `code` | Quando |
|---|---|---|
| `translations[i].serviceName`, `translations[i].description` | `validation.required` | Vuoti nella lingua di `Accept-Language` (`i` è la sua posizione nell'elenco). |
| `translations[i].serviceName`, `translations[i].description` | `validation.max_length` | Oltre 255 o 10.000 caratteri nella lingua di `Accept-Language`. |
| `translations` | `validation.invalid` | Un'altra lingua con la descrizione ma senza il nome, o con un testo oltre i limiti, come oggi (`ContentRules.Translations`). |
| `category` | `validation.invalid` | Valore fuori dall'enum. |
| `icon` | `validation.invalid` | Non è uno dei 22 nomi e, in `update-bar-service-general`, nemmeno l'icona che il servizio ha già. |
| `price` | `validation.required` | `null` o assente. |
| `price` | `validation.out_of_range` | Fuori da 0-100.000. |
| `vatRate` | `validation.invalid` | Non maggiore di 0 e minore di 100 (`ContentRules.Vat`). |
| `variants` | `validation.invalid` | Più di 50 (`ServiceSaleItem.MaxVariants`). |
| `variants[i].variantName` | `validation.required` | Vuoto o di soli spazi. |
| `variants[i].variantName` | `validation.max_length` | Oltre 120 caratteri (`ServiceSaleItemVariant.NameMaxLength`). |
| `variants[i].price` | `validation.required` | `null` o assente. |
| `variants[i].price` | `validation.out_of_range` | Fuori da 0-100.000. |
| `periodPrices` | `validation.invalid` | Un `price` fuori da 0-100.000, o lo stesso `idPeriod` due volte. |
| `idSectors` | `validation.invalid` | Più di 500, come oggi (`ServiceValidators.cs:97`). |
| `bookingOptions` | `validation.invalid` | Più di 20 (`ServiceSaleItem.MaxBookingOptions`). |
| `bookingOptions[i].dateTo` | `validation.end_before_start` | Prima di `dateFrom`. |
| `bookingOptions[i].managementEndTime`, `bookingOptions[i].onlineEndTime` | `validation.invalid` | Non dopo l'ora di inizio: nemmeno la stessa ora, come `ContentRules.TimeRange` e i vincoli `ck_catalog_service_booking_options_management` e `_online`. |
| `bookingOptions[i].weekdays` | `validation.invalid` | Un valore fuori dall'enum `Weekday` (`ContentRules.Weekdays`). |
| `bookingOptions[i].maxBookingPerHourQuantity`, `.maxBookingPerDayQuantity`, `.maxPerSlot`, `.intervalMinutes`, `.minItemPerBookingQuantity`, `.maxItemPerBookingQuantity` | `validation.invalid` | Non un intero da 1 a 32.767, oppure massimo di articoli minore del minimo (sul massimo). |

Il frontend controlla già prima di mandare: testi obbligatori e loro lunghezza (anche il nome nelle lingue con una
descrizione), prezzo e varianti, prezzi massimi, orari, numeri delle opzioni di prenotazione. Non controlla i limiti
dell'IVA (tra 0 e 100), delle varianti (50), dei settori (500) e delle opzioni di prenotazione (20): lì mostra l'errore del
backend. Gli errori dei campi di primo livello (per esempio `price`) vanno sotto il campo, come
`catalog.vat_rate_not_allowed` (sotto IVA, con le aliquote di `rates`) e `service.icon_not_found` (sotto le icone); tutti
gli altri sopra i bottoni, con il testo del loro `code`. Un errore su un id (`idSectors`) mostra sopra i bottoni il testo di
`validation.failed`. Con 404 `service.not_found` ricarica anche la tabella.

## 17. POST /api/catalog/services/create-bar-service

Salva del pannello "Aggiungi nuovo servizio bar". Crea un servizio con `kind` `bar`.

```json
{
  "translations": [
    { "language": "it", "serviceName": "Caffè ginseng", "description": "Caffè al ginseng in tazza piccola" },
    { "language": "en", "serviceName": "Ginseng coffee", "description": "Ginseng coffee in a small cup" },
    { "language": "fr", "serviceName": null, "description": null },
    { "language": "de", "serviceName": null, "description": null },
    { "language": "es", "serviceName": null, "description": null },
    { "language": "ar", "serviceName": null, "description": null },
    { "language": "zh", "serviceName": null, "description": null }
  ],
  "category": "Bar",
  "icon": "Coffee",
  "isHighlighted": true,
  "price": 1.5,
  "vatRate": 10,
  "variants": [{ "variantName": "Grande", "price": 0.5 }],
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    { "idPeriod": 0, "price": 1.5 },
    { "idPeriod": 1, "price": null }
  ],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isOnlinePurchaseEnabled": true,
  "bookingOptions": [
    {
      "dateFrom": "2026-07-01T00:00:00.000Z",
      "dateTo": "2026-07-31T00:00:00.000Z",
      "weekdays": [],
      "managementStartTime": "09:00",
      "managementEndTime": "12:00",
      "onlineStartTime": "09:00",
      "onlineEndTime": "12:00",
      "maxBookingPerHourQuantity": 2,
      "maxBookingPerDayQuantity": 5,
      "maxPerSlot": null,
      "intervalMinutes": 30,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 2
    }
  ]
}
```

| Campo | Tipo | Corrisponde a | Note |
|---|---|---|---|
| `translations` | array | `name`, `description` e `catalog.service_sale_item_translations` | "Nome e descrizione nelle lingue". |
| `category` | string o `null` | `category` | Enum `BarCategory` (sezione 2). |
| `icon` | string o `null` | `icon_id` | Una di: `Coffee`, `Croissant`, `Cocktail`, `Juice`, `Drink`, `WaterBottle`, `Beer`, `Jam`, `Cookie`, `Macaron`, `IceCream`, `Cupcake`, `RoomService`, `Mug`, `PizzaSlice`, `Sandwich`, `Cutlery`, `Burger`, `FastFood`, `RiceBowl`, `Cake`, `Pancakes`. `null` = nessuna icona scelta. Nella lettura e in `update-bar-service-general` può essere anche il nome di un'icona fuori da queste, che il servizio ha già. |
| `isHighlighted` | boolean | `is_highlighted` | "Metti in evidenza". |
| `price` | number | `price` | In euro, da 0 a 100.000. Obbligatorio. |
| `vatRate` | number o `null` | `vat_rate` | In percentuale. `null` = senza aliquota: si salva la natura predefinita (`VatPolicy.ResolveAsync`, `VatRules.DefaultExemptNature`), come vuole il vincolo `ck_catalog_service_sale_items_vat`. |
| `variants` | array | `catalog.service_sale_item_variants` | `variantName` = `name`, `price`, tutti e due obbligatori; nell'ordine ricevuto. `[]` = nessuna variante. `price` è il **sovrapprezzo**, che si somma al prezzo del servizio o del periodo come oggi (`ServiceSaleItemVariant`, `QuoteService.PriceServicesAsync`): un servizio a 1,00 con "Doppio" 0,80 costa 1,80. |
| `isPeriodPricingEnabled` | boolean | `has_period_prices` | |
| `periodPrices` | array | `catalog.service_sale_item_period_prices` | `idPeriod` = la `key` del periodo di `catalog/tariff-periods` (da 0), `price`. Il frontend manda una riga per ogni periodo della stagione corrente: una riga con `price` `null` non si salva. |
| `idSectors` | array di string (UUID) | `is_sector_restricted` e `catalog.service_sale_item_sectors` | Gli `id` della sezione 25. `[]` = tutti i settori (`is_sector_restricted` falso); altrimenti solo questi (vero). |
| `isOnlinePurchaseEnabled` | boolean | `is_purchasable` | "Abilita acquisto online". |
| `bookingOptions` | array | `catalog.service_booking_options` | Nell'ordine ricevuto; i campi qui sotto, tutti anche `null` tranne `weekdays`. |

Campi di ogni elemento di `bookingOptions`:

| Campo | Tipo | Corrisponde a |
|---|---|---|
| `dateFrom`, `dateTo` | string (giorno UTC) o `null` | `starts_on`, `ends_on` |
| `weekdays` | array di string | `weekdays`: nomi .NET dell'enum `Weekday` (`Monday`…`Sunday`); `[]` = tutti i giorni. Il pannello non lo mostra: lo rimanda come l'ha letto, `[]` per le opzioni nuove. |
| `managementStartTime`, `managementEndTime` | string `"HH:mm"` o `null` | `management_from`, `management_to` |
| `onlineStartTime`, `onlineEndTime` | string `"HH:mm"` o `null` | `online_from`, `online_to` |
| `maxBookingPerHourQuantity`, `maxBookingPerDayQuantity` | number (intero) o `null` | `max_per_hour`, `max_per_day` |
| `maxPerSlot` | number (intero) o `null` | `max_per_slot`. Il pannello non lo mostra: lo rimanda come l'ha letto, `null` per le opzioni nuove. |
| `intervalMinutes` | number (intero) o `null` | `interval_minutes`, in minuti. La tendina fa scegliere 15, 30, 45, 60 e 120; un altro valore già salvato torna uguale finché l'utente non ne sceglie uno. |
| `minItemPerBookingQuantity`, `maxItemPerBookingQuantity` | number (intero) o `null` | `min_items`, `max_items` |

`allergens` non c'è nel corpo: il servizio nasce senza allergeni.

Risposta: **201 con il solo id del servizio creato** (il suo `public_id`), come stringa JSON, `Content-Type`
`application/json`:

```json
"0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01"
```

Il frontend ne ha bisogno per l'immagine scelta nel pannello, che carica subito dopo con la sezione 24. Poi il pannello
si chiude e la tabella si ricarica.

## 18. GET /api/catalog/services/get-bar-service-general-info

Tab Info Generali del pannello "Modifica servizio bar", letta all'apertura. `idBarService` (l'id della riga di
`bar-service-list`) viaggia nella query string: `?idBarService=0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01`.

Risposta 200:

```json
{
  "idBarService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01",
  "translations": [
    { "language": "it", "serviceName": "Caffè", "description": "Espresso in tazzina." },
    { "language": "en", "serviceName": "Coffee", "description": "Espresso in a small cup." }
  ],
  "category": "Bar",
  "icon": "Coffee",
  "imageUrl": null,
  "isHighlighted": true,
  "version": "feac3ca6"
}
```

I campi sono quelli della sezione 17, più `idBarService` (`public_id`), `imageUrl` e `version`.

`imageUrl` (string o `null`) è l'indirizzo dell'**immagine caricata** (`icon_asset_id`), con `UrlsAsync` come la
`imageUrl` di `ServiceDto`; il pannello la mostra nel riquadro dell'immagine. Con un'icona, o senza immagine, è `null`:
la `imageUrl` di oggi invece vale anche per le icone. La `version` **non** la comprende, perché l'indirizzo può cambiare
a ogni lettura: copre solo i campi del salvataggio (sezione 1).

Errori: 404 `service.not_found` e gli altri comuni.

## 19. GET /api/catalog/services/get-bar-service-prices

Tab Prezzi del pannello "Modifica servizio bar", letta la prima volta che si apre. `idBarService` nella query string.

Risposta 200:

```json
{
  "price": 1,
  "vatRate": 10,
  "variants": [{ "variantName": "Doppio", "price": 0.8 }],
  "isPeriodPricingEnabled": true,
  "periodPrices": [{ "idPeriod": 0, "price": 1.2 }],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isOnlinePurchaseEnabled": true,
  "version": "37b6a44c"
}
```

I campi sono quelli della sezione 17, più `version`. `periodPrices` ha solo i periodi con un prezzo, **anche quelli che
non sono più nella stagione corrente** (non quelli eliminati): il frontend non li mostra ma li rimanda con il salvataggio,
per non perderli. `idSectors` è `[]` quando `is_sector_restricted` è falso. Errori: 404 `service.not_found` e gli altri comuni.

## 20. GET /api/catalog/services/get-bar-service-booking-options

Tab Prenotazioni del pannello "Modifica servizio bar", letta la prima volta che si apre. `idBarService` nella query
string.

Risposta 200, un oggetto con le opzioni nell'ordine salvato (`[]` se non ce ne sono) e la versione:

```json
{
  "bookingOptions": [
    {
      "dateFrom": "2026-07-01T00:00:00.000Z",
      "dateTo": "2026-07-31T00:00:00.000Z",
      "weekdays": ["Saturday", "Sunday"],
      "managementStartTime": "08:00",
      "managementEndTime": "12:00",
      "onlineStartTime": "08:30",
      "onlineEndTime": "11:30",
      "maxBookingPerHourQuantity": 20,
      "maxBookingPerDayQuantity": 80,
      "maxPerSlot": 5,
      "intervalMinutes": 30,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 4
    }
  ],
  "version": "96f5de5b"
}
```

I campi sono quelli della sezione 17. Errori: 404 `service.not_found` e gli altri comuni.

## 21. POST /api/catalog/services/update-bar-service-general

Salva della tab Info Generali, con l'header `If-Match` della versione letta (sezione 18). Scrive solo nomi,
descrizioni, categoria, icona e "Metti in evidenza".

```json
{
  "idBarService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01",
  "translations": [
    { "language": "it", "serviceName": "Caffè espresso", "description": "Espresso in tazzina." },
    { "language": "en", "serviceName": "Coffee", "description": "Espresso in a small cup." },
    { "language": "fr", "serviceName": null, "description": null },
    { "language": "de", "serviceName": null, "description": null },
    { "language": "es", "serviceName": null, "description": null },
    { "language": "ar", "serviceName": null, "description": null },
    { "language": "zh", "serviceName": null, "description": null }
  ],
  "category": "Bar",
  "icon": "Coffee",
  "isHighlighted": true
}
```

I campi sono quelli della sezione 17, più `idBarService`. Una lingua con i due testi `null` si toglie. `icon` uguale a
quella letta, anche fuori dalle 22, lascia l'icona com'è. Con `icon` `null` l'immagine caricata resta; con un'icona
l'immagine caricata non vale più, come oggi (`ServiceItemsService.ApplyAsync`). Se nel pannello è stata scelta
un'immagine nuova, il frontend manda `icon` com'era e, dopo il 204, la carica con la sezione 24: è il caricamento a
togliere l'icona, così se fallisce l'icona resta.

Risposta: **204 senza corpo**, con l'header `ETag` della nuova versione. Errori: 404 `service.not_found`, 409
`resource.version_conflict` e gli altri comuni.

## 22. POST /api/catalog/services/update-bar-service-prices

Salva della tab Prezzi, con l'header `If-Match` della versione letta (sezione 19). Scrive solo prezzo, IVA, varianti,
prezzi per periodo, settori e acquisto online.

```json
{
  "idBarService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01",
  "price": 1.3,
  "vatRate": 10,
  "variants": [{ "variantName": "Doppio", "price": 0.8 }],
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    { "idPeriod": 0, "price": 1.2 },
    { "idPeriod": 1, "price": null }
  ],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isOnlinePurchaseEnabled": true
}
```

I campi sono quelli della sezione 17, più `idBarService`. `variants`, `periodPrices` e `idSectors` arrivano sempre tutti
e prendono il posto di quelli salvati. Un servizio senza aliquota che ha già una natura dell'IVA la mantiene se
`vatRate` è ancora `null`.

Risposta: **204 senza corpo**, con l'header `ETag` della nuova versione. Errori: 404 `service.not_found`, 409
`resource.version_conflict`, 404 `catalog.sector_not_found`, 422 `pricing.period_not_found` e gli altri comuni.

## 23. POST /api/catalog/services/update-bar-service-booking

Salva della tab Prenotazioni, con l'header `If-Match` della versione letta (sezione 20). Scrive solo le opzioni di
prenotazione.

```json
{
  "idBarService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01",
  "bookingOptions": [
    {
      "dateFrom": "2026-07-01T00:00:00.000Z",
      "dateTo": "2026-07-31T00:00:00.000Z",
      "weekdays": ["Saturday", "Sunday"],
      "managementStartTime": "08:00",
      "managementEndTime": "12:00",
      "onlineStartTime": "08:30",
      "onlineEndTime": "11:30",
      "maxBookingPerHourQuantity": 20,
      "maxBookingPerDayQuantity": 80,
      "maxPerSlot": 5,
      "intervalMinutes": 30,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 4
    }
  ]
}
```

I campi sono quelli della sezione 17, più `idBarService`. L'elenco arriva sempre tutto e prende il posto di quello
salvato: un'opzione tolta nel pannello non c'è più, `[]` le toglie tutte.

Risposta: **204 senza corpo**, con l'header `ETag` della nuova versione. Errori: 404 `service.not_found`, 409
`resource.version_conflict` e gli altri comuni.

## 24. POST e DELETE /api/catalog/services/{id}/image

**Ci sono già e restano come sono** (`ServiceEndpoints.cs:72-83`, `ServiceItemsService.UploadImageAsync` e
`RemoveImageAsync`): qui si descrive solo come le usa il frontend, per l'immagine che l'utente sceglie dal computer al
posto di un'icona.

### POST

- Vale per i pannelli di Servizi Bar, Servizi e Servizi Orari. Parte con Salva: nell'aggiunta dopo
  `create-bar-service`, `create-standard-service` o `create-hourly-service`, con l'id della risposta; nella modifica dopo
  `update-bar-service-general`, `update-standard-service-general` o `update-hourly-service-general`, con l'id del
  servizio. Non manda `If-Match`.
- `id`: l'id del servizio nella rotta. Corpo `multipart/form-data` con il file nel campo `file`: PNG, JPEG o WebP fino a
  5 MB (il frontend lo controlla prima di mandarlo).
- Risposta 200 con `ServiceDto`, che il frontend non legge. L'immagine prende il posto dell'icona o dell'immagine di
  prima, come oggi.
- Errori, come oggi: 401 `auth.unauthenticated`, 403 `auth.forbidden` senza `CatalogSettings`, 422
  `catalog.invalid_image` (`args` `maxBytes` e `contentTypes`), 403 `catalog.property_required`, 404
  `catalog.property_not_found`, 404 `service.not_found`.
- Se il caricamento fallisce i dati del servizio sono già salvati: il pannello si chiude, la tabella si ricarica e un
  avviso ("Immagine non caricata") dice perché, con il testo del `code`.

### DELETE

- La chiama la X sull'immagine già caricata, in Modifica, dopo la conferma dell'utente: subito, senza Salva. Una X su
  un'immagine scelta e non ancora salvata non chiama niente.
- `id`: l'id del servizio nella rotta. Nessun corpo, nessun `If-Match`.
- Risposta 200 con `ServiceDto`, che il frontend non legge: il servizio resta senza immagine e senza icona, come oggi. La
  `version` di Info Generali non cambia, perché non comprende l'immagine (sezioni 18, 27 e 34).
- Errori, come oggi: 401 `auth.unauthenticated`, 403 `auth.forbidden` senza `CatalogSettings`, 403
  `catalog.property_required`, 404 `catalog.property_not_found`, 404 `service.not_found`. Il frontend li mostra sotto
  l'immagine con il testo del `code`; con `service.not_found` ricarica anche la tabella.

## 25. POST /api/catalog/sectors/sector-combobox

Tutti i settori (le file) dello stabilimento, per le tendine. È **una sola API per quattro pagine**:

- **Servizi**: tendina a scelta multipla "Servizio acquistabile nelle seguenti file/settori" (sezione Limiti) della tab
  Prezzi dei pannelli di Servizi Bar, Servizi e Servizi Orari e della tab Prezzi e Validità di quelli dei Prezzi
  Automatici, riletta ogni volta che la tab si apre;
- **Listino**: tendina "Settori e file" della tab Info Generali dei pannelli dei pacchetti, riletta ogni volta che la tab
  si apre, e righe dei prezzi per settore dei pannelli dei pacchetti e degli articoli aggiuntivi
  (`Listino - Frontend requests.md`);
- **Sconti e voucher**: tendina a scelta multipla "Settori e file" dei popup, letta la prima volta che compare in ogni apertura
  (`Sconti e voucher - Frontend requests.md`);
- **Prenotazioni online**: tendina "Fila/settore" delle regole di "Configurazione limiti di booking", riletta a ogni
  apertura del pannello "Opzioni prenotazioni online".

Segue la regola di tutte le tendine del frontend: `POST`, senza corpo né pagine, risposta `[{ id, value }]`, come
`article-combobox` (`Impostazioni magazzino - Frontend requests.md`, sezione 2).

### Cosa ci serve

- **Un'API nuova nel gruppo `/api/catalog/sectors`** (`Endpoints/SeasonEndpoints.cs:83`). Il nome non si confonde con
  `/{id:guid}`, perché non è un UUID. `GET`, `POST`, `PUT` e `DELETE /api/catalog/sectors` restano come sono:
  `GET /api/catalog/sectors` risponde `[{ id, name, sortOrder }]` (`SectorDto`) e per le tendine il frontend non la usa.
- **I settori sono quelli di `GET /api/catalog/sectors`** (`SectorService.ListAsync`, `SectorRepository.ListAsync`):
  tutti quelli dello stabilimento, senza gli eliminati, **nello stesso ordine** (`sort_order`, poi l'id interno).
- **Permesso**: `CatalogRead`, come `GET /api/catalog/sectors`. **Stabilimento** dal token (`CurrentProperty.IdAsync`).
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.

### Richiesta

`POST` senza corpo.

### Risposta

200 con un array di `{ id, value }`, vuoto se lo stabilimento non ha settori. Nessun altro campo:

```json
[
  { "id": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "value": "Fila 1" },
  { "id": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02", "value": "Fila 2" },
  { "id": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a09", "value": "Settore Est" }
]
```

| Campo | In `SectorDto` | Tipo | Note |
|---|---|---|---|
| `id` | `id` | string (UUID) | Il `public_id` del settore: lo stesso `idSector` delle righe di `sectors-list` e quello che le pagine mandano e leggono in `idSectors` e `idSector`. |
| `value` | `name` | string | Il nome del settore, così com'è: non ha traduzioni. |

### Errori

Nell'ordine in cui si controllano, come `GET /api/catalog/sectors`:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogRead`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |

Il frontend mostra l'errore sotto la tendina, con il testo del suo `code`, e rilegge l'elenco quando l'utente prova ad
aprirla; dove l'elenco serve alle righe dei prezzi (Listino) vale quello che il pannello fa quando una sua lettura
fallisce.

## Regole comuni dei pannelli di Servizi (sezioni 26-32)

I pannelli "Aggiungi nuovo servizio" e "Modifica servizio" della tab Servizi hanno le stesse tre tab di Servizi Bar: Info
Generali, Prezzi e Prenotazioni. **Valgono tutte le regole comuni dei pannelli di Servizi Bar** (API nuove sotto
`/api/catalog/services`, una tab non tocca le altre, quello che il form non mostra torna com'era, controllo di versione,
permessi, registro, JSON, nome e descrizione nelle lingue, errori e il loro ordine), con queste differenze:

- **Solo servizi con `kind` `normal`**: le letture e i salvataggi accettano solo quelli dello stabilimento; ogni altro id
  risponde 404 `service.not_found`. La creazione li fa di questo tipo.
- **Niente `category` e niente `variants`** nei corpi e nelle risposte: questi servizi non hanno categoria, varianti e
  allergeni, come oggi (`SaveServiceRequestValidator`, `ServiceSaleItem.ReplaceVariants` e `ServiceSaleItem.Update`).
- **`icon`** ha un altro elenco, di 21 nomi (sezione 26); le righe del catalogo sono quelle con `type` `normal`.
- **Id**: `idStandardService`, il `public_id` del servizio.
- Gli errori dei campi sono quelli della tabella di Servizi Bar, senza le righe di `category` e `variants`.

## 26. POST /api/catalog/services/create-standard-service

Salva del pannello "Aggiungi nuovo servizio". Crea un servizio con `kind` `normal`.

```json
{
  "translations": [
    { "language": "it", "serviceName": "Doccia calda", "description": "Doccia calda a gettone" },
    { "language": "en", "serviceName": "Hot shower", "description": "Coin-operated hot shower" },
    { "language": "fr", "serviceName": null, "description": null },
    { "language": "de", "serviceName": null, "description": null },
    { "language": "es", "serviceName": null, "description": null },
    { "language": "ar", "serviceName": null, "description": null },
    { "language": "zh", "serviceName": null, "description": null }
  ],
  "icon": "Shower",
  "isHighlighted": true,
  "price": 3,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    { "idPeriod": 0, "price": 3 },
    { "idPeriod": 1, "price": null }
  ],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isOnlinePurchaseEnabled": true,
  "bookingOptions": [
    {
      "dateFrom": "2026-06-15T00:00:00.000Z",
      "dateTo": "2026-09-15T00:00:00.000Z",
      "weekdays": [],
      "managementStartTime": "09:00",
      "managementEndTime": "19:00",
      "onlineStartTime": "09:00",
      "onlineEndTime": "18:00",
      "maxBookingPerHourQuantity": 6,
      "maxBookingPerDayQuantity": 50,
      "maxPerSlot": null,
      "intervalMinutes": 15,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 2
    }
  ]
}
```

- I campi, i loro tipi e i dati del backend a cui corrispondono sono quelli delle tabelle della sezione 17.
- **`icon`** (string o `null`, `icon_id`): una di `Surfing`, `Surfboard`, `Sailing`, `Kitesurfing`, `Volleyball`,
  `Baseball`, `Basketball`, `Cricket`, `SportsField`, `Tennis`, `Pool`, `Locker`, `Lock`, `Cabin`, `Shower`, `Baby`,
  `Accessible`, `Parking`, `Pets`, `Motorcycle`, `Bicycle`; `null` = nessuna icona scelta. Nella lettura e in
  `update-standard-service-general` può essere anche il nome di un'icona fuori da queste, che il servizio ha già.

Risposta: **201 con il solo id del servizio creato** (il suo `public_id`), come stringa JSON, come la sezione 17. Serve
all'immagine, caricata subito dopo con la sezione 24.

## 27. GET /api/catalog/services/get-standard-service-general-info

Tab Info Generali del pannello "Modifica servizio", letta all'apertura. `idStandardService` (l'id della riga di
`standard-service-list`) viaggia nella query string.

Risposta 200:

```json
{
  "idStandardService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f11",
  "translations": [
    { "language": "it", "serviceName": "Doccia calda", "description": "Doccia calda a gettone." },
    { "language": "en", "serviceName": "Hot shower", "description": "Coin-operated hot shower." }
  ],
  "icon": "Shower",
  "imageUrl": null,
  "isHighlighted": true,
  "version": "1f0c77ab"
}
```

`imageUrl` e `version` come nella sezione 18. Errori: 404 `service.not_found` e gli altri comuni.

## 28. GET /api/catalog/services/get-standard-service-prices

Tab Prezzi del pannello "Modifica servizio", letta la prima volta che si apre. `idStandardService` nella query string.

Risposta 200:

```json
{
  "price": 3,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "periodPrices": [{ "idPeriod": 1, "price": 2.5 }],
  "idSectors": [],
  "isOnlinePurchaseEnabled": true,
  "version": "b81d04e2"
}
```

Come la sezione 19, senza `variants`: `periodPrices` ha solo i periodi con un prezzo, anche quelli che non sono più nella
stagione corrente (non quelli eliminati); `idSectors` è `[]` quando `is_sector_restricted` è falso. Errori: 404 `service.not_found` e gli altri
comuni.

## 29. GET /api/catalog/services/get-standard-service-booking-options

Tab Prenotazioni del pannello "Modifica servizio", letta la prima volta che si apre. `idStandardService` nella query
string. Risposta 200 come la sezione 20: `{ "bookingOptions": [...], "version": "..." }`, con le opzioni nell'ordine
salvato (`[]` se non ce ne sono) e i campi della sezione 17. Errori: 404 `service.not_found` e gli altri comuni.

## 30. POST /api/catalog/services/update-standard-service-general

Salva della tab Info Generali, con l'header `If-Match` della versione letta (sezione 27). Scrive solo nomi, descrizioni,
icona e "Metti in evidenza".

```json
{
  "idStandardService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f11",
  "translations": [
    { "language": "it", "serviceName": "Doccia calda", "description": "Doccia calda, 3 minuti." },
    { "language": "en", "serviceName": "Hot shower", "description": "Hot shower, 3 minutes." },
    { "language": "fr", "serviceName": null, "description": null },
    { "language": "de", "serviceName": null, "description": null },
    { "language": "es", "serviceName": null, "description": null },
    { "language": "ar", "serviceName": null, "description": null },
    { "language": "zh", "serviceName": null, "description": null }
  ],
  "icon": "Shower",
  "isHighlighted": true
}
```

Come la sezione 21: una lingua con i due testi `null` si toglie; `icon` uguale a quella letta lascia l'icona com'è, con
`null` l'immagine caricata resta, con un'icona l'immagine caricata non vale più; un'immagine nuova si carica dopo il 204
con la sezione 24, che toglie l'icona.

Risposta: **204 senza corpo**, con l'header `ETag` della nuova versione. Errori: 404 `service.not_found`, 409
`resource.version_conflict`, 404 `service.icon_not_found` e gli altri comuni.

## 31. POST /api/catalog/services/update-standard-service-prices

Salva della tab Prezzi, con l'header `If-Match` della versione letta (sezione 28). Scrive solo prezzo, IVA, prezzi per
periodo, settori e acquisto online.

```json
{
  "idStandardService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f11",
  "price": 3.5,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    { "idPeriod": 0, "price": null },
    { "idPeriod": 1, "price": 2.5 }
  ],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isOnlinePurchaseEnabled": true
}
```

Come la sezione 22, senza `variants`, che restano com'erano: `periodPrices` e `idSectors` arrivano sempre tutti e prendono
il posto di quelli salvati; un servizio senza aliquota che ha già una natura dell'IVA la mantiene se `vatRate` è ancora
`null`.

Risposta: **204 senza corpo**, con l'header `ETag` della nuova versione. Errori: 404 `service.not_found`, 409
`resource.version_conflict`, 422 `catalog.vat_rate_not_allowed`, 422 `pricing.period_not_found`, 404
`catalog.sector_not_found` e gli altri comuni.

## 32. POST /api/catalog/services/update-standard-service-booking

Salva della tab Prenotazioni, con l'header `If-Match` della versione letta (sezione 29). Scrive solo le opzioni di
prenotazione: il corpo ha `idStandardService` e `bookingOptions`, l'elenco intero, che prende il posto di quello salvato
(`[]` le toglie tutte), con i campi della sezione 17.

```json
{
  "idStandardService": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f11",
  "bookingOptions": [
    {
      "dateFrom": "2026-06-15T00:00:00.000Z",
      "dateTo": "2026-09-15T00:00:00.000Z",
      "weekdays": [],
      "managementStartTime": "09:00",
      "managementEndTime": "19:00",
      "onlineStartTime": "09:00",
      "onlineEndTime": "18:00",
      "maxBookingPerHourQuantity": 6,
      "maxBookingPerDayQuantity": 60,
      "maxPerSlot": null,
      "intervalMinutes": 15,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 2
    }
  ]
}
```

Risposta: **204 senza corpo**, con l'header `ETag` della nuova versione. Errori: 404 `service.not_found`, 409
`resource.version_conflict` e gli altri comuni.

## Regole comuni dei pannelli di Servizi Orari (sezioni 33-39)

I pannelli "Aggiungi nuovo servizio orario" e "Modifica servizio orario" hanno le tab Info Generali, Prezzi e Prenotazioni.
**Valgono le regole comuni dei pannelli di Servizi Bar** (API nuove sotto `/api/catalog/services`, una tab non tocca le
altre, quello che il form non mostra torna com'era, controllo di versione, permessi, registro, JSON, nome e descrizione
nelle lingue, ordine degli errori), con queste differenze:

- **Solo servizi con `kind` `hourly`**: ogni altro id risponde 404 `service.not_found`. La creazione li fa di questo tipo.
- **Info Generali** come Servizi (sezione 26): niente `category`, stesse 21 icone, righe del catalogo con `type` `hourly`.
- **Niente `variants`** e allergeni, che questi servizi non hanno (`ServiceSaleItem.ReplaceVariants` e
  `ServiceSaleItem.Update`), e niente prezzi per periodo del servizio (`catalog.service_sale_item_period_prices`): quelli
  che un servizio dovesse avere restano com'erano.
- **`price`** obbligatorio, come Servizi.
- **`vatRate`** del servizio: può essere `null` solo se **tutte** le disponibilità (quelle in vigore e quelle delle opzioni
  di prenotazione, almeno una) hanno la loro, come vuole già `SaveServiceRequestValidator`; il servizio prende allora
  l'aliquota della prima, come fa già `ServiceItemsService.ApplyAsync`. **Un servizio che ha già una natura dell'IVA
  (esente) resta esente** se `vatRate` è ancora `null`, anche quando le disponibilità hanno la loro aliquota: nel `PUT` di
  oggi la natura arriva insieme e vince (`ApplyAsync`), e il preventivo di un servizio esente non guarda le aliquote delle
  disponibilità (`QuoteService.PriceServicesAsync`). Smette di esserlo solo quando arriva un `vatRate`.
- **Id**: `idHourlyService`, il `public_id` del servizio.

### Disponibilità

Lo stesso oggetto nella tab Prezzi e nella tab Prenotazioni (una per opzione). Corrisponde a una riga di
`catalog.service_hourly_options` con le sue fasce in `catalog.service_sale_item_time_slots` (`HourlyOptionDto` e
`HourlySlotDto` di oggi).

Nella tab Prezzi sono di due tipi, e **vale sempre uno solo**, come nel backend (`SaveServiceRequestValidator`,
`hourlyPrices`):

- con `isPeriodPricingEnabled` falso, `availabilityOptions`: valgono per tutta la stagione (`period_group_key` e
  `booking_option_id` `null`);
- con `isPeriodPricingEnabled` vero, `periodAvailabilities`: quelle di ogni periodo (`period_group_key`).

L'altro elenco è sempre `[]`, nelle letture e nei salvataggi: se l'utente cambia l'interruttore e salva, le disponibilità
dell'altro tipo non ci sono più.

**Arrivano solo le disponibilità in cui l'utente ha scritto qualcosa**: il pannello mostra un riquadro vuoto dove non ce ne
sono, ma non lo manda. `availabilityOptions` può essere `[]`, `periodAvailabilities` ha solo i periodi con almeno una
disponibilità (`[]` se nessuno) e `availabilityOption` di un'opzione di prenotazione può essere `null`, come il backend
accetta già (`SaveServiceRequestValidator`, `HourlyOptions`). Una disponibilità vuota che era già salvata torna come è stata
letta, finché l'utente non la toglie.

```json
{
  "startTime": "09:00",
  "endTime": "19:00",
  "daysOfWeek": [1, 2, 3, 4, 5, 6, 7],
  "timeSlotPrices": [
    { "timeSlot": "TenMinutes", "isEnabled": true, "price": 3 },
    { "timeSlot": "FifteenMinutes", "isEnabled": false, "price": null },
    { "timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8 },
    { "timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null },
    { "timeSlot": "OneHour", "isEnabled": true, "price": 15 },
    { "timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null },
    { "timeSlot": "TwoHours", "isEnabled": true, "price": 25 },
    { "timeSlot": "ThreeHours", "isEnabled": false, "price": null }
  ],
  "isCustomTimeSlotEnabled": true,
  "customTimeSlotMinuteQuantity": 20,
  "customTimeSlotPrice": 6,
  "vatRate": 22,
  "maxBookingPerTimeSlotQuantity": 4,
  "hasFixedTimeSlots": false
}
```

| Campo | Tipo | Corrisponde a |
|---|---|---|
| `startTime`, `endTime` | string `"HH:mm"` o `null` | `time_from`, `time_to` |
| `daysOfWeek` | array di number (ISO: 1 lunedì … 7 domenica) | `weekdays`. `[]` vuol dire tutti i giorni, come nel backend, e il pannello li mostra tutti scelti. Il frontend ne manda almeno uno per le disponibilità che mostra; quelle dei periodi che non mostra tornano come sono state lette, anche con `[]`. |
| `timeSlotPrices` | array, sempre tutte e otto nell'ordine qui sopra, poi quelle di "Durate non integrate" | Le fasce con `is_custom` falso: `timeSlot` = `duration_minutes` (`TenMinutes` 10, `FifteenMinutes` 15, `ThirtyMinutes` 30, `FortyFiveMinutes` 45, `OneHour` 60, `OneHourThirtyMinutes` 90, `TwoHours` 120, `ThreeHours` 180), `isEnabled` = `is_active`, `price` = `price` (number o `null`) |
| `isCustomTimeSlotEnabled`, `customTimeSlotMinuteQuantity`, `customTimeSlotPrice` | boolean, number o `null`, number o `null` | La fascia con `is_custom` vero: `is_active`, `duration_minutes`, `price`. Con i minuti `null` la fascia non c'è. |
| `vatRate` | number o `null` | `vat_rate`; `null` = quella del servizio |
| `maxBookingPerTimeSlotQuantity` | number (intero) o `null` | `max_per_slot` |
| `hasFixedTimeSlots` | boolean | `has_fixed_time_slots`. Quello del servizio (`ServiceSaleItem.HasFixedTimeSlots`) è vero se lo è una disponibilità qualsiasi (`ReplaceHourly`): il salvataggio di una tab lo ricalcola su **tutte**, quelle dei prezzi e quelle delle opzioni di prenotazione, non solo sulle sue. |

### Durate non integrate

Il backend ha 11 durate (`ContentRules.HourlyDurations`), il frontend per ora solo le 8 dell'esempio: **240, 300 e 360 minuti
non sono state integrate**. Il pannello non le mostra e non le fa scegliere. Se una disponibilità le ha già, la lettura le dà
in `timeSlotPrices` dopo le otto, come `FourHours`, `FiveHours` e `SixHours`, e il frontend le rimanda uguali: un Salva non le
toglie. Il frontend non le controlla: una di queste accesa ma senza prezzo, già salvata così (la colonna `price` lo
permette), si accetta com'è, perché l'utente non può correggerla.

### Opzioni di prenotazione

Ogni elemento di `bookingOptions` ha **tutti i campi della sezione 17** più `availabilityOption`, la sua disponibilità (la
riga di `catalog.service_hourly_options` con il suo `booking_option_id`, `Hourly` di `BookingOptionDto`). Il pannello mostra
solo `dateFrom`, `dateTo` e `availabilityOption`: gli altri campi li rimanda come li ha letti, e per un'opzione nuova sono
`null` (`weekdays` `[]`). `availabilityOption` è `null` per un'opzione che non l'ha, in lettura e nel salvataggio: il
pannello ne mostra una vuota e la manda solo se l'utente ci scrive qualcosa.

### Le fasce che restano tengono il loro id

Oggi ogni salvataggio ricrea le fasce (`ServiceSaleItem.ReplaceHourly`), quindi cambia il loro id, che preventivi e prenotazioni
usano come `timeSlotId` (`QuoteServiceRequest`, `BookingServiceTimeSlot`, `ResourceConsumption`). Con queste API una fascia già
salvata che resta (stessa disponibilità, stessa durata) **tiene il suo id**, anche se cambiano il prezzo o gli orari: un
salvataggio non deve toccare le fasce già prenotate. Come farlo lo sceglie lo sviluppatore.

### Errori

Quelli comuni dei pannelli di Servizi Bar, nello stesso ordine (401, 403 `auth.forbidden`, 400, 403
`catalog.property_required`, 404 `catalog.property_not_found`, 404 `service.not_found`, 409 `resource.version_conflict`, 422
`catalog.vat_rate_not_allowed` per il `vatRate` del servizio o di una disponibilità, 404 `service.icon_not_found`, 422
`pricing.period_not_found`, 404 `catalog.sector_not_found`). I controlli che hanno bisogno
del servizio salvato (l'icona che ha già, e `vatRate` `null` di un servizio esente o con le aliquote nelle disponibilità
delle opzioni di prenotazione, in `update-hourly-service-prices`) si fanno dopo il 404 `service.not_found`, con lo stesso
400 `validation.failed`. Errori di campo del 400:

| Campo | `code` | Quando |
|---|---|---|
| `translations…`, `icon`, `idSectors` | come Servizi Bar | Come Servizi Bar, con le icone di Servizi. |
| `price` | `validation.required` o `validation.out_of_range` | Assente, o fuori da 0-100.000. |
| `vatRate` | `validation.invalid` | Non maggiore di 0 e minore di 100, oppure `null` quando non può esserlo (regole qui sopra). |
| `availabilityOptions` | `validation.invalid` | Più di 20 (`ContentRules.MaxHourlyOptions`), o non vuoto con `isPeriodPricingEnabled` vero. |
| `periodAvailabilities` | `validation.invalid` | Più di 50 periodi, lo stesso `idPeriod` due volte, più di 20 disponibilità in un periodo, o non vuoto con `isPeriodPricingEnabled` falso. |
| `bookingOptions` | `validation.invalid` | Più di 20 (`ServiceSaleItem.MaxBookingOptions`). |
| `bookingOptions[i].dateTo` | `validation.end_before_start` | Prima di `dateFrom`. |
| `bookingOptions[i].managementEndTime`, `.onlineEndTime`, `.weekdays`, `.maxBookingPerHourQuantity`, `.maxBookingPerDayQuantity`, `.maxPerSlot`, `.intervalMinutes`, `.minItemPerBookingQuantity`, `.maxItemPerBookingQuantity` | `validation.invalid` | Come Servizi Bar. Il pannello non li mostra: li rimanda come li ha letti. |
| `<disponibilità>.endTime` | `validation.invalid` | Non dopo `startTime`: nemmeno la stessa ora (`ContentRules.TimeRange`, `ck_catalog_service_hourly_options_hours`). |
| `<disponibilità>.daysOfWeek` | `validation.invalid` | Un valore fuori da 1-7. |
| `<disponibilità>.timeSlotPrices` | `validation.invalid` | Una fascia sconosciuta o ripetuta. |
| `<disponibilità>.timeSlotPrices[j].price`, `<disponibilità>.customTimeSlotPrice` | `validation.required` o `validation.out_of_range` | Assente con la fascia accesa (non per le "Durate non integrate", qui sopra), o fuori da 0-100.000. |
| `<disponibilità>.customTimeSlotMinuteQuantity` | `validation.required` o `validation.out_of_range` | Assente con la fascia accesa o con il suo prezzo scritto, o fuori da 1-1.440. |
| `<disponibilità>.vatRate` | `validation.out_of_range` | Non maggiore di 0 e minore di 100. |
| `<disponibilità>.maxBookingPerTimeSlotQuantity` | `validation.out_of_range` | Fuori da 1-32.767 (`smallint`). |

`<disponibilità>` è `availabilityOptions[i]`, `periodAvailabilities[i].availabilityOptions[j]` o
`bookingOptions[i].availabilityOption`. Il frontend controlla già tutto tranne i limiti dell'IVA e i numeri massimi di
elementi. Mostra sotto il campo gli errori dei campi di primo livello e `service.icon_not_found` (sotto le icone), sopra i
bottoni tutti gli altri con il testo del loro `code`, compreso `catalog.vat_rate_not_allowed`, che non dice di quale
aliquota parla.

## 33. POST /api/catalog/services/create-hourly-service

Salva del pannello "Aggiungi nuovo servizio orario". Crea un servizio con `kind` `hourly`.

```json
{
  "translations": [
    { "language": "it", "serviceName": "Sup", "description": "Tavola da stand up paddle a ore" },
    { "language": "en", "serviceName": "Sup", "description": "Stand up paddle board by the hour" },
    { "language": "fr", "serviceName": null, "description": null },
    { "language": "de", "serviceName": null, "description": null },
    { "language": "es", "serviceName": null, "description": null },
    { "language": "ar", "serviceName": null, "description": null },
    { "language": "zh", "serviceName": null, "description": null }
  ],
  "icon": "Surfing",
  "isHighlighted": false,
  "price": 15,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "availabilityOptions": [],
  "periodAvailabilities": [{ "idPeriod": 0, "availabilityOptions": ["…una o più disponibilità…"] }],
  "idSectors": [],
  "isOnlinePurchaseEnabled": true,
  "bookingOptions": [
    {
      "dateFrom": "2026-06-01T00:00:00.000Z",
      "dateTo": "2026-09-15T00:00:00.000Z",
      "weekdays": [],
      "managementStartTime": null,
      "managementEndTime": null,
      "onlineStartTime": null,
      "onlineEndTime": null,
      "maxBookingPerHourQuantity": null,
      "maxBookingPerDayQuantity": null,
      "maxPerSlot": null,
      "intervalMinutes": null,
      "minItemPerBookingQuantity": null,
      "maxItemPerBookingQuantity": null,
      "availabilityOption": "…una disponibilità…"
    }
  ]
}
```

| Campo | Corrisponde a | Note |
|---|---|---|
| `translations`, `icon`, `isHighlighted` | come la sezione 26 | |
| `price`, `vatRate` | `price`, `vat_rate` | Regole qui sopra. |
| `isPeriodPricingEnabled` | `has_period_prices` | |
| `availabilityOptions` | `catalog.service_hourly_options` senza `period_group_key` né `booking_option_id` | "Disponibilità"; nell'ordine ricevuto. |
| `periodAvailabilities` | `catalog.service_hourly_options` con `period_group_key` | `idPeriod` = la `key` di `catalog/tariff-periods`; una voce per ogni periodo della stagione corrente che ha almeno una disponibilità, con le sue disponibilità nell'ordine ricevuto. |
| `idSectors`, `isOnlinePurchaseEnabled` | come la sezione 17 | |
| `bookingOptions` | `catalog.service_booking_options` con la disponibilità in `catalog.service_hourly_options` (`booking_option_id`) | "Opzioni di prenotazione"; nell'ordine ricevuto. `availabilityOption` `null` = senza disponibilità. |

Risposta: **201 con il solo id del servizio creato**, come stringa JSON, come la sezione 17. Serve all'immagine (sezione 24).

## 34. GET /api/catalog/services/get-hourly-service-general-info

Tab Info Generali del pannello "Modifica servizio orario", letta all'apertura, con `idHourlyService` nella query string.
Risposta 200 come la sezione 27, con `idHourlyService` al posto di `idStandardService`: `translations`, `icon`, `imageUrl`,
`isHighlighted`, `version`. Errori: 404 `service.not_found` e gli altri comuni.

## 35. GET /api/catalog/services/get-hourly-service-prices

Tab Prezzi, letta la prima volta che si apre, con `idHourlyService` nella query string. Risposta 200:

```json
{
  "price": 15,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "availabilityOptions": [],
  "periodAvailabilities": [{ "idPeriod": 0, "availabilityOptions": ["…una o più disponibilità…"] }],
  "idSectors": [],
  "isOnlinePurchaseEnabled": true,
  "version": "5b8c0e1f"
}
```

`periodAvailabilities` ha solo i periodi con almeno una disponibilità, **anche quelli che non sono più nella stagione
corrente** (non quelli eliminati): il frontend non li mostra ma li rimanda con il salvataggio. Errori: 404 `service.not_found` e gli altri comuni.

## 36. GET /api/catalog/services/get-hourly-service-booking-options

Tab Prenotazioni, letta la prima volta che si apre, con `idHourlyService` nella query string. Risposta 200:
`{ "bookingOptions": [...], "version": "..." }`, con le opzioni come nella sezione 33 (`[]` se non ce ne sono;
`availabilityOption` `null` dove non c'è), nell'ordine salvato. Errori: 404 `service.not_found` e gli altri comuni.

## 37. POST /api/catalog/services/update-hourly-service-general

Salva della tab Info Generali, con l'header `If-Match` della versione letta (sezione 34). Corpo e regole come la sezione 30,
con `idHourlyService`: scrive solo nomi, descrizioni, icona e "Metti in evidenza". Risposta: **204 senza corpo**, con
l'header `ETag`. Errori: 404 `service.not_found`, 409 `resource.version_conflict`, 404 `service.icon_not_found` e gli altri
comuni.

## 38. POST /api/catalog/services/update-hourly-service-prices

Salva della tab Prezzi, con l'header `If-Match` della versione letta (sezione 35). Corpo: `idHourlyService`, `price`,
`vatRate`, `isPeriodPricingEnabled`, `availabilityOptions`, `periodAvailabilities`, `idSectors` e `isOnlinePurchaseEnabled`,
come nella sezione 33. Le disponibilità in vigore arrivano sempre tutte, anche quelle di periodi letti e non mostrati, e
prendono il posto di quelle salvate. Risposta: **204 senza corpo**, con l'header `ETag`. Errori: 404 `service.not_found`, 409
`resource.version_conflict`, 422 `catalog.vat_rate_not_allowed`, 422 `pricing.period_not_found`, 404
`catalog.sector_not_found` e gli altri comuni.

## 39. POST /api/catalog/services/update-hourly-service-booking

Salva della tab Prenotazioni, con l'header `If-Match` della versione letta (sezione 36). Corpo: `idHourlyService` e
`bookingOptions`, l'elenco intero come nella sezione 33, che prende il posto di quello salvato (`[]` le toglie tutte).
Risposta: **204 senza corpo**, con l'header `ETag`. Errori: 404 `service.not_found`, 409 `resource.version_conflict`, 422
`catalog.vat_rate_not_allowed` e gli altri comuni.

## Regole comuni dei pannelli di Prezzi Automatici (sezioni 40-46)

I pannelli "Aggiungi nuovo prezzo automatico" e "Modifica prezzo automatico" hanno due tab: Info Generali e Prezzi e
Validità. **Valgono le regole comuni dei pannelli di Servizi Bar** (una tab non tocca l'altra, quello che il form non mostra
torna com'era, controllo di versione, permessi, JSON, nome e descrizione nelle lingue), con queste differenze:

- **API nuove sotto `/api/catalog/automatic-prices`**, la radice che il backend ha già (`Endpoints/ServiceEndpoints.cs:85`).
  `GET` e `POST /api/catalog/automatic-prices`, `GET` e `PUT /api/catalog/automatic-prices/{id}` restano come sono: il
  frontend non le usa.
- **Solo prezzi automatici dello stabilimento** (`catalog.automatic_prices`, tabelle `catalog.automatic_price_periods`,
  `automatic_price_time_ranges`, `automatic_price_services`, `automatic_price_sectors` e `automatic_price_translations`):
  ogni altro id risponde 404 `service.automatic_price_not_found` (`CatalogErrors.AutomaticPriceNotFound`).
- **Niente `category`, `isHighlighted`, varianti e opzioni di prenotazione**: il modello non li ha.
- **Registro**: `catalog.automatic_price_created` per la creazione, `catalog.automatic_price_updated` per ogni salvataggio e
  per l'immagine, come oggi.
- **Id**: `idAutomaticPrice`, il `public_id` del prezzo automatico.

### Servizi e giorni

- **Servizi**: il prezzo vale per i servizi dei tre elenchi di id, **almeno uno** in tutto, come vuole già il backend
  (`SaveAutomaticPriceRequestValidator`, `serviceIds`). "Si applica a tutti i servizi" non c'è, come nel backend (migration
  `ServicesAlignedToFront`): l'interruttore "Tutti i servizi" della pagina è solo del frontend, che manda nei tre elenchi
  gli id di tutti i servizi letti (sezione 41) e, in lettura, lo mostra acceso se ci sono tutti. Un servizio creato dopo
  non entra da solo.
- **Giorni**: il frontend manda sempre almeno un giorno per periodo; `[]` in lettura vuol dire tutti i giorni, come nel
  backend (`AutomaticPricePeriod.Covers`), e il pannello li mostra tutti scelti.

### Errori

Nell'ordine in cui si controllano. Le righe 404 `service.automatic_price_not_found` e 409 valgono per letture e salvataggi
dove la sezione lo dice.

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso (`CatalogRead` o `CatalogSettings`). |
| 400 | `validation.failed` | Corpo non valido, con un elemento di `errors[]` per campo (tabella qui sotto). |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `service.automatic_price_not_found` | `idAutomaticPrice` non è un prezzo automatico dello stabilimento. Niente cambia. |
| 409 | `resource.version_conflict` | `If-Match` non è più la versione dei dati della tab (sezione 1). Niente cambia. |
| 422 | `catalog.vat_rate_not_allowed` | `vatRate` non è un'aliquota del Paese dello stabilimento, con `args.rates` come oggi (`VatPolicy.ResolveAsync`). Niente cambia. |
| 404 | `service.icon_not_found` | `icon` non ha un'icona corrispondente nel catalogo (`CatalogErrors.ServiceIconNotFound`). Niente cambia. |
| 404 | `service.not_found` | Un id dei tre elenchi non è un servizio dello stabilimento di quel tipo (`CatalogErrors.ServiceNotFound`). Niente cambia. |
| 404 | `catalog.sector_not_found` | Un id di `idSectors` non è un settore dello stabilimento (`ContentMapping.SectorIdsAsync`). Niente cambia. |

L'icona che il prezzo automatico ha già (sezione 44) si controlla dopo il 404 `service.automatic_price_not_found`, con lo
stesso 400 `validation.failed`.

Errori di campo del 400, con `field` uguale al nome nel corpo:

| Campo | `code` | Quando |
|---|---|---|
| `translations…` | come Servizi Bar | Come Servizi Bar. |
| `icon` | `validation.invalid` | Un nome fuori dalle 43 icone (sezione 40); nella sezione 44 va bene anche l'icona già salvata. |
| `price` | `validation.required` o `validation.out_of_range` | Assente, o fuori da 0-100.000. |
| `vatRate` | `validation.invalid` | Non maggiore di 0 e minore di 100 (`ContentRules.Vat`). |
| `idBarServices` | `validation.required` | Nessun servizio nei tre elenchi (vuoti o assenti): un solo errore, sul primo. |
| `validityPeriods` | `validation.invalid` | Più di 50. |
| `validityPeriods[i].dateTo` | `validation.end_before_start` | Prima di `dateFrom`. |
| `validityPeriods[i].daysOfWeek` | `validation.invalid` | Un valore fuori da 1-7. |
| `validityPeriods[i].timeSlots` | `validation.invalid` | Più di 20, un'ora assente o `endTime` non dopo `startTime`: nemmeno la stessa ora (`ck_catalog_automatic_price_time_ranges_range`). |

Il frontend controlla già nomi, limiti dei testi, prezzo, servizi, giorni, ore e date; non i limiti dell'IVA e i numeri
massimi di elementi. Mostra sotto il campo gli errori dei campi di primo livello, `catalog.vat_rate_not_allowed` (sotto IVA, con le aliquote di `rates`) e `service.icon_not_found` (sotto le icone),
sopra i bottoni tutti gli altri con il testo del loro `code`. Con `service.automatic_price_not_found` ricarica la tabella;
con `service.not_found` rilegge i servizi (sezione 41) e toglie dalla scelta quelli che non ci sono più.

## 40. POST /api/catalog/automatic-prices/create-automatic-price

Salva del pannello "Aggiungi nuovo prezzo automatico".

```json
{
  "translations": [
    { "language": "it", "serviceName": "Coperto", "description": "Coperto a persona per i servizi del bar" },
    { "language": "en", "serviceName": "Cover charge", "description": "Cover charge per person for the bar services" },
    { "language": "fr", "serviceName": null, "description": null },
    { "language": "de", "serviceName": null, "description": null },
    { "language": "es", "serviceName": null, "description": null },
    { "language": "ar", "serviceName": null, "description": null },
    { "language": "zh", "serviceName": null, "description": null }
  ],
  "icon": "Cutlery",
  "price": 2,
  "vatRate": 10,
  "isOnlinePurchaseEnabled": true,
  "validityPeriods": [
    {
      "dateFrom": "2026-06-01T00:00:00.000Z",
      "dateTo": "2026-09-30T00:00:00.000Z",
      "daysOfWeek": [1, 2, 3, 4, 5, 6, 7],
      "timeSlots": [{ "startTime": "12:00", "endTime": "15:00" }]
    }
  ],
  "idBarServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01"],
  "idStandardServices": [],
  "idHourlyServices": [],
  "isPricePerServiceEnabled": false,
  "idSectors": []
}
```

| Campo | Tipo | Corrisponde a | Note |
|---|---|---|---|
| `translations` | array | `name`, `description` e `catalog.automatic_price_translations` | Come Servizi Bar. |
| `icon` | string o `null` | `icon_id` | Una delle 22 di Servizi Bar (sezione 17, righe del catalogo con `type` `bar`) o delle 21 di Servizi (sezione 26, righe con `type` `normal`); i nomi non si ripetono. Un prezzo automatico che ha già un'icona di una riga `hourly` (stessi nomi di `normal`) la mantiene se `icon` è il nome letto. |
| `price` | number | `price` | In euro, da 0 a 100.000. Obbligatorio. |
| `vatRate` | number o `null` | `vat_rate` | Come Servizi Bar: `null` = senza aliquota, con la natura predefinita (`VatPolicy.ResolveAsync`); un prezzo che ha già una natura la mantiene se `vatRate` è ancora `null`. |
| `isOnlinePurchaseEnabled` | boolean | `is_purchasable` | |
| `validityPeriods` | array | `catalog.automatic_price_periods` | `[]` = sempre. `dateFrom`, `dateTo` (giorno UTC o `null`) = `starts_on`, `ends_on`; `daysOfWeek` (ISO: 1 lunedì … 7 domenica) = `weekdays`; `timeSlots` = `catalog.automatic_price_time_ranges`, con `startTime` ed `endTime` obbligatori (`"HH:mm"`); `[]` = tutto il giorno. |
| `idBarServices`, `idStandardServices`, `idHourlyServices` | array di string (UUID) | `catalog.automatic_price_services` | I servizi scelti, con gli `id` della sezione 41 ("Servizi e giorni"). |
| `isPricePerServiceEnabled` | boolean | `applies_per_service` | |
| `idSectors` | array di string (UUID) | `is_sector_restricted` e `catalog.automatic_price_sectors` | Come la sezione 17. |

Risposta: **201 con il solo id del prezzo automatico creato**, come stringa JSON, come la sezione 17. Serve all'immagine
(sezione 46).

## 41. GET /api/catalog/automatic-prices/get-automatic-price-services

I servizi dello stabilimento per i tre pannelli di "Validità sui servizi", letti all'apertura dell'aggiunta e con la tab
Prezzi e Validità della modifica. Nessun parametro. Permesso `CatalogRead`. Risposta 200:

```json
{
  "barServices": [{ "id": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01", "value": "Caffè" }],
  "standardServices": [{ "id": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f11", "value": "Doccia calda" }],
  "hourlyServices": [{ "id": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f21", "value": "Sup" }]
}
```

- `barServices`, `standardServices` e `hourlyServices`: tutti i servizi non eliminati dello stabilimento con `kind` `bar`,
  `normal` e `hourly`; `[]` se non ce ne sono. Nessuna pagina.
- `id`: il `public_id` del servizio. `value`: il nome nella lingua dell'utente, come in "Nome nella lingua dell'utente" delle
  tabelle. Ogni elenco è in ordine di `value`, crescente e naturale, come `serviceName` in "Ordinamento" delle tabelle.
- Errori: 401, 403 `auth.forbidden`, 403 `catalog.property_required`, 404 `catalog.property_not_found`.

## 42. GET /api/catalog/automatic-prices/get-automatic-price-general-info

Tab Info Generali del pannello "Modifica prezzo automatico", letta all'apertura, con `idAutomaticPrice` nella query string.
Risposta 200:

```json
{
  "idAutomaticPrice": "0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f31",
  "translations": [
    { "language": "it", "serviceName": "Coperto", "description": "Coperto a persona per i servizi del bar" },
    { "language": "en", "serviceName": "Cover charge", "description": "Cover charge per person for the bar services" }
  ],
  "icon": "Cutlery",
  "imageUrl": null,
  "version": "c293ede7"
}
```

Come la sezione 18, senza `category` e `isHighlighted`: `icon` può essere anche un nome fuori dalle 43, `imageUrl` è
l'indirizzo dell'immagine caricata (`null` con un'icona o senza) e non fa parte di `version`. Errori: 404
`service.automatic_price_not_found` e gli altri comuni.

## 43. GET /api/catalog/automatic-prices/get-automatic-price-prices-validity

Tab Prezzi e Validità, letta la prima volta che si apre, con `idAutomaticPrice` nella query string. Risposta 200 con i campi
della sezione 40 da `price` a `idSectors`, più `version`:

```json
{
  "price": 2,
  "vatRate": 10,
  "isOnlinePurchaseEnabled": true,
  "validityPeriods": [],
  "idBarServices": ["0193b6d1-4a2e-7c3f-8d1e-5b6c7d8e9f01"],
  "idStandardServices": [],
  "idHourlyServices": [],
  "isPricePerServiceEnabled": false,
  "idSectors": [],
  "version": "afabd0e9"
}
```

I periodi arrivano in ordine di `starts_on` e le fasce in ordine di inizio, come oggi. Errori: 404
`service.automatic_price_not_found` e gli altri comuni.

## 44. POST /api/catalog/automatic-prices/update-automatic-price-general

Salva della tab Info Generali, con l'header `If-Match` della versione letta (sezione 42). Corpo: `idAutomaticPrice`,
`translations` (tutte e 7 le lingue) e `icon`, come nella sezione 40; `icon` può essere anche quella letta fuori dalle 43, o
`null` se c'è un'immagine, che resta. Se l'utente ha scelto un'immagine, `icon` è quella di prima: la toglie il caricamento
(sezione 46), che parte dopo il 204. Scrive solo nomi, descrizioni e icona. Risposta: **204 senza corpo**, con l'header
`ETag`. Errori: 404 `service.automatic_price_not_found`, 409 `resource.version_conflict`, 404 `service.icon_not_found` e gli
altri comuni.

## 45. POST /api/catalog/automatic-prices/update-automatic-price-prices-validity

Salva della tab Prezzi e Validità, con l'header `If-Match` della versione letta (sezione 43). Corpo: `idAutomaticPrice` e i
campi della sezione 40 da `price` a `idSectors`; gli elenchi arrivano interi e prendono il posto di quelli salvati. Risposta:
**204 senza corpo**, con l'header `ETag`. Errori: 404 `service.automatic_price_not_found`, 409 `resource.version_conflict`,
422 `catalog.vat_rate_not_allowed`, 404 `service.not_found`, 404 `catalog.sector_not_found` e gli altri comuni.

## 46. POST e DELETE /api/catalog/automatic-prices/{id}/image

### POST

**C'è già e resta com'è** (`ServiceEndpoints.cs:133-138`, `AutomaticPricesService.UploadImageAsync`). Il frontend la usa come
la sezione 24: parte con Salva, dopo `create-automatic-price` con l'id della risposta, o dopo
`update-automatic-price-general`; corpo `multipart/form-data` con il campo `file` (PNG, JPEG o WebP fino a 5 MB); risposta
200 con `AutomaticPriceDto`, che il frontend non legge. Errori, come oggi: 401, 403 `auth.forbidden` senza `CatalogSettings`,
422 `catalog.invalid_image`, 403 `catalog.property_required`, 404 `catalog.property_not_found`, 404
`service.automatic_price_not_found`. Se fallisce, i dati sono già salvati e un avviso ("Immagine non caricata") dice perché.

### DELETE (nuova)

Come la DELETE dei servizi (sezione 24, `ServiceItemsService.RemoveImageAsync`), che i prezzi automatici non hanno.

- La chiama la X sull'immagine già caricata, in Modifica, dopo la conferma dell'utente: subito, senza Salva.
- `id`: l'id del prezzo automatico nella rotta. Nessun corpo, nessun `If-Match`. Permesso `CatalogSettings`.
- Il prezzo resta senza immagine e senza icona; se non ha un'immagine non cambia niente. La `version` di Info Generali non
  cambia. Registro: `catalog.automatic_price_updated`.
- Risposta 200 con `AutomaticPriceDto`, come la DELETE dei servizi; il frontend non la legge.
- Errori: 401 `auth.unauthenticated`, 403 `auth.forbidden`, 403 `catalog.property_required`, 404
  `catalog.property_not_found`, 404 `service.automatic_price_not_found`. Il frontend li mostra sotto l'immagine; con
  `service.automatic_price_not_found` ricarica anche la tabella.

## Regole comuni dei pannelli di Prenotazione tavoli (sezioni 47-51)

I pannelli "Aggiungi nuovo servizio tavoli" e "Modifica servizio tavoli" hanno due tab: Info Generali e Disponibilità e
Prenotazioni. **Valgono le regole comuni dei pannelli di Servizi Bar** (una tab non tocca l'altra, quello che il form non
mostra torna com'era, controllo di versione, permessi, JSON, nome e descrizione nelle lingue), con queste differenze:

- **API nuove sotto `/api/catalog/table-services`**, la radice che il backend ha già (`Endpoints/ServiceEndpoints.cs:142`).
  `GET` e `POST /api/catalog/table-services`, `GET` e `PUT /api/catalog/table-services/{id}` restano come sono, tranne la
  durata media facoltativa (qui sotto): il frontend non le usa.
- **Solo servizi tavoli dello stabilimento** (`catalog.table_services`, con `table_service_availabilities`,
  `table_service_slots`, `table_service_table_types` e `table_service_translations`): ogni altro id risponde 404
  `service.table_service_not_found` (`CatalogErrors.TableServiceNotFound`).
- **Info Generali** ha solo `translations` e `isHighlighted`: niente categoria, icona e immagine.
- **Registro**: `catalog.table_service_created` per la creazione, `catalog.table_service_updated` per ogni salvataggio, come
  oggi.
- **Id**: `idTableService`, il `public_id` del servizio tavoli.

### Disponibilità

Ogni elemento di `availabilities` è una riga di `catalog.table_service_availabilities` con le sue fasce
(`catalog.table_service_slots`):

```json
{
  "dateFrom": "2026-06-01T00:00:00.000Z",
  "dateTo": "2026-09-30T00:00:00.000Z",
  "days": [
    { "dayOfWeek": 1, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": [] },
    { "dayOfWeek": 2, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": [] }
  ]
}
```

| Campo | Tipo | Corrisponde a |
|---|---|---|
| `dateFrom`, `dateTo` | string (giorno UTC) | `starts_on`, `ends_on`. Obbligatori; i periodi tornano in ordine di `starts_on`, come oggi. |
| `days` | array | Nella richiesta sempre i sette giorni; un giorno che non c'è vale come non scelto. Nella risposta bastano quelli con almeno una fascia. |
| `days[].dayOfWeek` | number (ISO: 1 lunedì … 7 domenica) | `weekday` |
| `days[].isEnabled` | boolean | Vero = il giorno ha almeno una fascia. Falso = non ne ha: le sue ore e `otherTimeSlots` non si salvano. |
| `days[].startTime`, `days[].endTime` | string `"HH:mm"` o `null` | `time_from`, `time_to` della **prima** fascia del giorno (per ora di inizio). |
| `days[].otherTimeSlots` | array di `{ startTime, endTime }` | Le altre fasce dello stesso giorno, in ordine di inizio. Il pannello ne fa modificare una sola: le altre le mostra soltanto e le rimanda come le ha lette. |

- Una fascia senza giorno (`weekday` `null`, "stessa fascia per tutti i giorni") si legge come la stessa fascia in tutti e
  sette i giorni.
- La casella "Applica stessi orari per tutti i giorni" **non viaggia**: il frontend la mostra accesa quando tutti i giorni
  scelti, almeno due, hanno gli stessi orari, e nel salvataggio manda gli orari di ogni giorno. Niente da aggiungere al database.

### Tavoli, posti e caparra

- `availabilityMode`: `Tables` o `Seats` (nomi .NET di `TableCapacityMode`, `capacity_mode`). Con `Tables` vale `tableGroups`
  (`catalog.table_service_table_types`: `tableQuantity` = `count`, `seatPerTableQuantity` = `seats`) e `totalSeatQuantity` è
  `null`; con `Seats` vale `totalSeatQuantity` (`total_seats`) e `tableGroups` è `[]`.
- `bookingPayment` `None` = `deposit_mode` `none`, senza tipo e valore; `depositMode` (sempre presente) non conta. `Deposit` =
  `deposit_mode` `total` o `per_person`, secondo `depositMode` (`Total` o `PerPerson`). La lettura con `None` dà `depositMode`
  `Total`.
- La caparra è una **percentuale** (`depositPercentage`, `deposit_type` `percentage`) oppure un **importo** in euro
  (`depositAmount`, `deposit_type` `amount`), mai tutti e due: il pannello fa scrivere solo la percentuale. Un importo già
  salvato torna uguale in `depositAmount` finché l'utente non scrive la percentuale; allora `depositAmount` è `null`.

### Durata media del pasto: serve una modifica al backend

`averageMealMinutes` (`average_meal_minutes`, in minuti) **può essere `null`**: nel frontend è facoltativa e fa scegliere 30,
45, 60, 75, 90, 105, 120, 150 o 180 minuti. Oggi invece il backend la vuole sempre (colonna `NOT NULL`,
`SaveTableServiceRequestValidator`). Va tolto l'obbligo e aggiunta la migration della colonna. Un valore diverso da quei nove
già salvato torna uguale finché l'utente non ne sceglie uno.

La durata facoltativa riguarda tutto il modello, non solo le API nuove: `TableService.AverageMealMinutes` e
`TableServiceData`, `TableServiceDto` e `SaveTableServiceRequest` (`short?`), così le API di oggi leggono anche un servizio
senza durata, e la duplicazione (sezione 16), che passa da quei due DTO (`TableServicesService.ToRequest`), ne fa una copia
senza durata. Il vincolo `ck_catalog_table_services_meal` (`average_meal_minutes > 0`) può restare: con `NULL` non blocca.

**Booking la usa**: quando la prenotazione di un tavolo parte a un'ora che non è l'inizio di una fascia (dal gestionale, con
conferma), la fine è l'inizio più la durata (`ServiceWriter`, `slot?.To ?? input.SlotStart.AddMinutes(...)`). Ci arriva con
`BookableTableService.AverageMealMinutes` (`Shared/Contracts/Catalog/IBookingServices.cs`, oggi `short`) e
`BookingCatalog.ListTableServicesAsync`: anche questi diventano `short?`, altrimenti non si compila. Quale durata vale lì senza
un valore lo decide lo sviluppatore: l'agente glielo chiede. Per la pagina non cambia niente.

### Errori

Nell'ordine in cui si controllano: 401 `auth.unauthenticated`, 403 `auth.forbidden`, 400 `validation.failed`, 403
`catalog.property_required`, 404 `catalog.property_not_found`, 404 `service.table_service_not_found`, 409
`resource.version_conflict` (gli ultimi due dove la sezione lo dice). Errori di campo del 400, con `field` uguale al nome nel
corpo:

| Campo | `code` | Quando |
|---|---|---|
| `translations…` | come Servizi Bar | Come Servizi Bar. |
| `availabilityMode`, `bookingPayment`, `depositMode` | `validation.invalid` | Valore fuori dal suo enum. |
| `tableGroups` | `validation.required` o `validation.invalid` | Con `Tables`: vuoto; oppure tavoli fuori da 1-500, posti fuori da 1-100, o due righe con gli stessi posti (`uq_catalog_table_service_table_types_seats`). |
| `totalSeatQuantity` | `validation.required` o `validation.out_of_range` | Con `Seats`: assente, o fuori da 1-10.000. |
| `averageMealMinutes` | `validation.out_of_range` | Fuori da 10-600. |
| `maxBookingPerTimeSlotQuantity` | `validation.out_of_range` | Fuori da 1-32.767 (`smallint`). |
| `minSeatPerBookingQuantity`, `maxSeatPerBookingQuantity` | `validation.out_of_range` | Non maggiore di 0, o il massimo minore del minimo. |
| `depositPercentage` | `validation.required` o `validation.out_of_range` | Con `Deposit`: assente senza `depositAmount`; oppure non maggiore di 0 o oltre 100. Il frontend manda al massimo due decimali, come la colonna (`numeric(12,2)`). |
| `depositAmount` | `validation.out_of_range` o `validation.invalid` | Non maggiore di 0, o insieme a `depositPercentage`. |
| `availabilities` | `validation.invalid` | Più di 20. |
| `availabilities[i].dateFrom`, `availabilities[i].dateTo` | `validation.required` | Assente. |
| `availabilities[i].dateTo` | `validation.end_before_start` | Prima di `dateFrom`. |
| `availabilities[i].days` | `validation.required` o `validation.invalid` | Nessun giorno scelto; oppure un giorno scelto senza le due ore, una fine non dopo l'inizio (nemmeno la stessa ora: `ck_catalog_table_service_slots_range`), un giorno ripetuto o fuori da 1-7, o più di 50 fasce **nello stesso giorno**. Le stesse regole valgono per ogni fascia di `otherTimeSlots` di un giorno scelto. |

Il limite di 50 fasce è per giorno, non per disponibilità come oggi (`SaveTableServiceRequestValidator`): una fascia senza
giorno si legge come sette e, rimandata, diventa sette righe, quindi una disponibilità valida oggi (per esempio otto fasce
senza giorno) supererebbe le 50 e non si potrebbe più salvare.

Il frontend controlla già tutto tranne i numeri massimi di elementi e i campi che non fa modificare. Mostra sotto il campo gli
errori dei campi di primo
livello, sopra i bottoni tutti gli altri con il testo del loro `code`. Con `service.table_service_not_found` ricarica la
tabella.

## 47. POST /api/catalog/table-services/create-table-service

Salva del pannello "Aggiungi nuovo servizio tavoli".

```json
{
  "translations": [
    { "language": "it", "serviceName": "Pranzo", "description": "Tavoli del ristorante per il pranzo" },
    { "language": "en", "serviceName": "Lunch", "description": "Restaurant tables for lunch" },
    { "language": "fr", "serviceName": null, "description": null },
    { "language": "de", "serviceName": null, "description": null },
    { "language": "es", "serviceName": null, "description": null },
    { "language": "ar", "serviceName": null, "description": null },
    { "language": "zh", "serviceName": null, "description": null }
  ],
  "isHighlighted": true,
  "availabilities": ["…una o più disponibilità, anche nessuna…"],
  "availabilityMode": "Tables",
  "tableGroups": [
    { "tableQuantity": 10, "seatPerTableQuantity": 2 },
    { "tableQuantity": 8, "seatPerTableQuantity": 4 }
  ],
  "totalSeatQuantity": null,
  "averageMealMinutes": 90,
  "maxBookingPerTimeSlotQuantity": 8,
  "minSeatPerBookingQuantity": null,
  "maxSeatPerBookingQuantity": null,
  "bookingPayment": "Deposit",
  "depositMode": "PerPerson",
  "depositPercentage": 20,
  "depositAmount": null,
  "isOnlineBookingEnabled": true
}
```

| Campo | Tipo | Corrisponde a | Note |
|---|---|---|---|
| `translations` | array | `name`, `description` e `catalog.table_service_translations` | Come Servizi Bar. |
| `isHighlighted` | boolean | `is_highlighted` | |
| `availabilities` | array | `catalog.table_service_availabilities` | "Disponibilità"; `[]` se nessuna. |
| `availabilityMode`, `tableGroups`, `totalSeatQuantity` | string, array, number o `null` | `capacity_mode`, `catalog.table_service_table_types`, `total_seats` | "Tavoli, posti e caparra". |
| `averageMealMinutes` | number (intero) o `null` | `average_meal_minutes` | "Durata media del pasto". |
| `maxBookingPerTimeSlotQuantity` | number (intero) o `null` | `max_bookings_per_slot` | |
| `minSeatPerBookingQuantity`, `maxSeatPerBookingQuantity` | number (intero) o `null` | `min_seats_per_booking`, `max_seats_per_booking` | Il pannello non li mostra: li rimanda come letti, `null` per un servizio nuovo. |
| `bookingPayment`, `depositMode`, `depositPercentage`, `depositAmount` | string, string, number o `null`, number o `null` | `deposit_mode`, `deposit_type`, `deposit_value` | "Tavoli, posti e caparra". |
| `isOnlineBookingEnabled` | boolean | `is_online_bookable` | |

Risposta: **204 senza corpo** (qui l'id non serve: non c'è un'immagine da caricare).

## 48. GET /api/catalog/table-services/get-table-service-general-info

Tab Info Generali del pannello "Modifica servizio tavoli", letta all'apertura, con `idTableService` nella query string.
Risposta 200: `idTableService`, `translations` (solo le lingue con almeno un testo), `isHighlighted` e `version`, come la
sezione 18 senza `category`, `icon` e `imageUrl`. Errori: 404 `service.table_service_not_found` e gli altri comuni.

## 49. GET /api/catalog/table-services/get-table-service-availability-bookings

Tab Disponibilità e Prenotazioni, letta la prima volta che si apre, con `idTableService` nella query string. Risposta 200: i
campi della sezione 47 da `availabilities` a `isOnlineBookingEnabled`, più `version`. Le righe di `tableGroups` arrivano in
ordine di posti, come oggi. Errori: 404 `service.table_service_not_found` e gli altri comuni.

## 50. POST /api/catalog/table-services/update-table-service-general

Salva della tab Info Generali, con l'header `If-Match` della versione letta (sezione 48). Corpo: `idTableService`,
`translations` (tutte e 7 le lingue) e `isHighlighted`; scrive solo questi. Risposta: **204 senza corpo**, con l'header `ETag`.
Errori: 404 `service.table_service_not_found`, 409 `resource.version_conflict` e gli altri comuni.

## 51. POST /api/catalog/table-services/update-table-service-availability-bookings

Salva della tab Disponibilità e Prenotazioni, con l'header `If-Match` della versione letta (sezione 49). Corpo:
`idTableService` e i campi della sezione 47 da `availabilities` a `isOnlineBookingEnabled`; gli elenchi arrivano interi e
prendono il posto di quelli salvati (i tipi di tavolo sono unici per posti: vedi `TableServicesService.UpdateAsync`).
Risposta: **204 senza corpo**, con l'header `ETag`. Errori: 404 `service.table_service_not_found`, 409
`resource.version_conflict` e gli altri comuni.

## Icone da controllare con lo sviluppatore

Il frontend ha i suoi elenchi fissi di icone. Il catalogo del backend (`catalog.service_type_icons`, letto da
`GET /api/catalog/lookups/service-icons?type=`) è fatto di righe del database, che il frontend non conosce: l'analisi
`shared documentation/backend/analisi-post/db/catalog/catalog_service_type_icons.md` ne conta 11 `bar`, 25 `normal` e 25
`hourly` nel catalogo di partenza, senza l'elenco dei nomi. **Lo sviluppatore controlla quali mancano e le aggiunge**: ogni
nome di queste liste deve corrispondere a una riga attiva del suo `type`; altrimenti la scelta di quell'icona risponde 404
`service.icon_not_found` e l'utente non può salvarla.

| `type` | Nomi del frontend | Quanti |
|---|---|---|
| `bar` | `Coffee`, `Croissant`, `Cocktail`, `Juice`, `Drink`, `WaterBottle`, `Beer`, `Jam`, `Cookie`, `Macaron`, `IceCream`, `Cupcake`, `RoomService`, `Mug`, `PizzaSlice`, `Sandwich`, `Cutlery`, `Burger`, `FastFood`, `RiceBowl`, `Cake`, `Pancakes` | 22 |
| `normal` | `Surfing`, `Surfboard`, `Sailing`, `Kitesurfing`, `Volleyball`, `Baseball`, `Basketball`, `Cricket`, `SportsField`, `Tennis`, `Pool`, `Locker`, `Lock`, `Cabin`, `Shower`, `Baby`, `Accessible`, `Parking`, `Pets`, `Motorcycle`, `Bicycle` | 21 |
| `hourly` | Gli stessi nomi di `normal` | 21 |

- **`bar`**: se il catalogo è ancora quello di partenza **ne mancano almeno 11** (11 contro 22).
- **`normal` e `hourly`**: il numero basterebbe (25 contro 21), ma i nomi vanno confrontati uno per uno.
- I prezzi automatici usano i 22 nomi di `bar` e i 21 di `normal`: non hanno un `type` loro, e il backend accetta già
  un'icona di qualunque `type` (`ServiceCatalogRepository.FindIconIdAsync`).
- Le icone del catalogo che non sono in queste liste non si possono scegliere dal frontend; un servizio che ne ha già una
  la tiene (sezioni 18, 27, 34 e 42).
- Nella richiesta e nella risposta viaggia il **nome** del frontend, non l'id del catalogo.

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
32. Un servizio con un'IVA che non è più tra le aliquote ammesse, da solo o insieme a uno valido: 422
    `service.duplicate_vat_rate_not_allowed` con `serviceName` e `rates`, e nessuna copia. Lo stesso con un prezzo automatico,
    con una disponibilità di un servizio orario, con un'icona tolta dal catalogo (`service.duplicate_icon_not_found`) e con un
    settore eliminato fra quelli a cui l'elemento è limitato (`service.duplicate_sector_not_found`; salvati i suoi settori
    senza quello, la duplicazione riesce). Con prezzi per un periodo eliminato `service.duplicate_period_not_found`; salvata
    la tab Prezzi la duplicazione riesce. Con prezzi per una `key` senza nessuna riga nei periodi: 204, e la copia li
    ha. Un servizio con il nome in tutte e 7 le lingue: la copia ha in ogni lingua il suffisso della sua lingua (" copia",
    " copy", " copie", " copia", " Kopie", " 副本", " نسخة") e le stesse descrizioni. Se in inglese "Coffee copy" esiste
    già ma in italiano "Caffè copia" no, la copia è "Caffè copia" e "Coffee copy 2".

API 17-24, con un token con `CatalogRead` e `CatalogSettings`:

33. `create-bar-service` con il corpo della sezione 17 e `Accept-Language: it`: 201 con il solo id, come stringa JSON; il
    servizio compare in `bar-service-list` con quell'id, nome, categoria e prezzo, e le tre letture restituiscono gli
    stessi dati (`periodPrices` senza la riga con `price` `null`, `translations` solo `it` e `en`). Nel registro una voce
    `catalog.service_created`.
34. `create-bar-service` con la descrizione francese ma senza il nome francese: 400 con `validation.invalid` su
    `translations`. Con `Accept-Language: en` e solo i testi inglesi: 201; la tabella mostra il nome inglese a un
    utente italiano e `get-bar-service-general-info` restituisce solo la traduzione `en`, non una `it`. Senza i testi
    inglesi, anche con quelli italiani: 400 con `validation.required` su `translations[1].serviceName` e
    `translations[1].description`.
35. Su un servizio con allergeni e un'immagine caricata: dopo `update-bar-service-prices` e `update-bar-service-booking`
    allergeni, immagine, nomi e categoria non cambiano; dopo `update-bar-service-general` con `icon` `null` non cambiano
    prezzi, opzioni di prenotazione, allergeni e immagine.
36. `vatRate` `null`: 204 e la lettura restituisce `null`. `vatRate` 7 in Italia: 422 `catalog.vat_rate_not_allowed` con
    le aliquote ammesse in `args.rates`; `vatRate` 0 o 100: 400 con `validation.invalid` su `vatRate`. Niente cambia.
37. `idSectors` `[]`: il servizio non è limitato e la lettura restituisce `[]`; con un id sconosciuto 404
    `catalog.sector_not_found` e niente cambia. Un servizio limitato solo a un settore poi eliminato (dati di prima): la
    lettura lo restituisce ancora in `idSectors`; rimandarlo risponde 404 `catalog.sector_not_found`, mandare `[]` o un altro
    settore 204.
38. Più di 500 id in `idSectors`: 400 con `validation.invalid` su `idSectors`. Un `idPeriod` che non esiste: 422
    `pricing.period_not_found`. Un `idPeriod` già salvato sul servizio ma non più nella
    stagione corrente, rimandato da `update-bar-service-prices`: 204 e resta salvato.
39. `bookingOptions` con `dateTo` prima di `dateFrom`, un'ora di fine uguale a quella di inizio, 0 in un massimo o il
    massimo di articoli minore del minimo: 400 con gli errori della tabella; mai un 500 per un vincolo del database.
40. Le letture e i salvataggi con l'id di un servizio `normal` o `hourly`, di un altro stabilimento o eliminato: 404
    `service.not_found`.
41. Letture senza `CatalogRead` e salvataggi senza `CatalogSettings`: 403 `auth.forbidden`; senza token 401
    `auth.unauthenticated`; token senza stabilimento 403 `catalog.property_required`.
42. Giorni e orari tornano come sono stati mandati: `"2026-07-01T00:00:00.000Z"`, `"08:30"`.
43. `price` `null` o assente: 400 con `validation.required` su `price`; -1 o 100.001: `validation.out_of_range`. Una
    variante con il nome vuoto o senza prezzo: `validation.required` su `variants[0].variantName` o `variants[0].price`.
    Un servizio a 1 con una variante a 0.8: il preventivo con quella variante dà 1.8, come oggi.
44. `POST` di `image` con un PNG sull'id appena creato: 200; `get-bar-service-general-info` ha `icon` `null` e l'indirizzo
    del file in `imageUrl`. Dopo `update-bar-service-general` con `icon` `null`, `imageUrl` resta; con `icon` `"Coffee"` è
    `null`. Due letture di fila hanno la stessa `version` anche se l'indirizzo cambia. Su un servizio con un'icona,
    `update-bar-service-general` con la stessa icona e poi il `POST` di `image`: l'icona non c'è più e c'è l'immagine; se il
    `POST` fallisce (per esempio 422 con un PDF) l'icona resta. `DELETE` di `image`: 200, `imageUrl` `null` e la stessa
    `version` di prima.
45. Su un'opzione con `weekdays` sabato e domenica, `max_per_slot` 5 e `interval_minutes` 20:
    `get-bar-service-booking-options` li restituisce (`["Saturday", "Sunday"]`, 5, 20) e `update-bar-service-booking` con
    gli stessi valori e un `maxBookingPerHourQuantity` diverso cambia solo quest'ultimo. `weekdays` con `"Lunedi"`: 400 con
    `validation.invalid` su `bookingOptions[0].weekdays`.
46. Su un servizio con un'icona fuori dalle 22: `get-bar-service-general-info` ne dà il nome in `icon`, e
    `update-bar-service-general` con quel nome cambia il nome del servizio ma non l'icona; con `"Cake"` l'icona cambia.


API 25, con uno stabilimento con i settori "Fila 1", "Fila 2" e "Settore Est" (in quest'ordine), un settore eliminato e
il token di un utente con `CatalogRead`:

47. `sector-combobox` senza corpo: 200 con i tre settori nell'ordine di `GET /api/catalog/sectors`, ognuno con solo `id` e
    `value`; gli `id` sono gli `idSector` di `sectors-list`. Il settore eliminato e quelli di un altro stabilimento non ci
    sono. Per uno stabilimento senza settori: 200 con `[]`.
48. Un `id` della risposta mandato in `idSectors` da `create-bar-service`: 201, e `get-bar-service-prices` lo restituisce
    uguale.
49. Senza token 401 `auth.unauthenticated`; senza `CatalogRead` 403 `auth.forbidden`; token senza stabilimento 403
    `catalog.property_required`. `GET /api/catalog/sectors` risponde come prima.

API 26-32, con un token con `CatalogRead` e `CatalogSettings`:

50. `create-standard-service` con il corpo della sezione 26: 201 con il solo id; il servizio compare in
    `standard-service-list` con quell'id, nome e prezzo, non in `bar-service-list`, e le tre letture restituiscono gli stessi
    dati. Nel registro una voce `catalog.service_created`.
51. Le prove 34-39 e 43-46 riescono anche con le API di Servizi (`idStandardService`), con queste differenze: senza
    categoria, varianti e allergeni (35 e 43), con `Shower` al posto di `Coffee` (44) e `Pool` al posto di `Cake` (46), su
    un servizio con un'icona fuori dalle 21.
52. Un servizio `normal` non ha categoria, varianti e allergeni, né dopo la creazione né dopo i tre salvataggi.
53. Le letture e i salvataggi di Servizi con l'id di un servizio `bar` o `hourly`, e quelli di Servizi Bar con l'id di un
    servizio `normal`: 404 `service.not_found`.
54. Ogni nome della tabella "Icone da controllare con lo sviluppatore", mandato in `icon` con la sua API (`bar` con
    `update-bar-service-general`, `normal` con `update-standard-service-general`, `hourly` con
    `update-hourly-service-general`), risponde 204 e la lettura lo restituisce uguale.
55. Letture senza `CatalogRead` e salvataggi senza `CatalogSettings`: 403 `auth.forbidden`; senza token 401
    `auth.unauthenticated`; token senza stabilimento 403 `catalog.property_required`.

API 33-39, con un token con `CatalogRead` e `CatalogSettings`:

56. `create-hourly-service` con due periodi e una opzione di prenotazione: 201 con il solo id; il servizio compare in
    `hourly-service-list` e le tre letture restituiscono gli stessi dati, con tutte e otto le fasce di ogni disponibilità
    nello stesso ordine. Nel registro una voce `catalog.service_created`.
57. `update-hourly-service-prices` non cambia nomi, icona, immagine e opzioni di prenotazione; `update-hourly-service-booking`
    non cambia le disponibilità dei periodi; `update-hourly-service-general` non cambia né le une né le altre.
58. `endTime` uguale a `startTime`, `daysOfWeek` con 8, un prezzo di 100.001 o 0 in `maxBookingPerTimeSlotQuantity`: 400 con
    gli errori della tabella; mai un 500 per un vincolo del database.
59. Un `idPeriod` che non esiste: 422 `pricing.period_not_found`; uno già salvato ma non più nella stagione corrente: 204.
60. Le prove 34, 37, 44, 46 e 55 riescono anche con le API di Servizi Orari, con `Shower` al posto di `Coffee` e `Pool` al
    posto di `Cake`; le letture e i salvataggi con l'id di un servizio
    `bar` o `normal`: 404 `service.not_found`.
61. Con `isPeriodPricingEnabled` falso e una `availabilityOptions`: 201, e la lettura la restituisce con
    `periodAvailabilities` `[]`. Poi `update-hourly-service-prices` con `isPeriodPricingEnabled` vero e un periodo: 204, e la
    lettura dà solo quello. I due elenchi pieni insieme: 400 `validation.invalid` su quello non in vigore. Su un servizio con
    due periodi, un salvataggio con uno solo: 204, la lettura dà solo quello e l'altro periodo non è più usato dal servizio.
62. `price` `null`: 400 `validation.required`. `vatRate` `null` con una disponibilità senza aliquota: 400 `validation.invalid`
    su `vatRate`; con tutte le disponibilità a 10: 201 e la lettura dà `vatRate` 10. Su un servizio con una natura dell'IVA,
    `vatRate` `null`: 204 e la natura non cambia, anche con le disponibilità a 10 (la lettura dà ancora `null`); con `vatRate`
    22 la natura non c'è più.
63. Una fascia accesa senza prezzo, la personalizzata accesa senza minuti o senza prezzo: 400 `validation.required` sul
    campo; i minuti a 0 o 1.441: 400 `validation.out_of_range`.
64. Una disponibilità con 240 minuti accesi: la lettura dà `FourHours` dopo le otto fasce, e rimandata uguale resta salvata,
    anche accesa senza prezzo.
    Una disponibilità salvata senza giorni: la lettura dà `daysOfWeek` `[]`.
65. Un'opzione di prenotazione con giorni, orari, massimi e intervallo già salvati: `update-hourly-service-booking` con gli
    stessi valori letti non li cambia. Un'opzione senza disponibilità: la lettura dà `availabilityOption` `null`, e rimandata
    con `null` resta senza.

API 40-46, con un token con `CatalogRead` e `CatalogSettings`:

66. `create-automatic-price` con un periodo, due fasce e un servizio per tipo: 201 con il solo id; il prezzo compare in
    `automatic-price-list` e le due letture restituiscono gli stessi dati. Nel registro una voce
    `catalog.automatic_price_created`. La prova 34 riesce anche con `create-automatic-price`.
67. `update-automatic-price-general` non cambia prezzo, periodi, servizi e settori; `update-automatic-price-prices-validity`
    non cambia nomi, icona e immagine. Nel registro una voce `catalog.automatic_price_updated` per salvataggio.
68. `price` `null`: 400 `validation.required`; 100.001: 400 `validation.out_of_range`. Una fascia senza `endTime` o con le due
    ore uguali, o 21 fasce: 400 `validation.invalid` su `validityPeriods[0].timeSlots`; `dateTo` prima di `dateFrom`: 400
    `validation.end_before_start` su `validityPeriods[0].dateTo`; un giorno 8: 400 `validation.invalid` su
    `validityPeriods[0].daysOfWeek`; 51 periodi: 400 `validation.invalid` su `validityPeriods`. `vatRate` 7 in Italia: 422
    `catalog.vat_rate_not_allowed` con `args.rates`; 100: 400 `validation.invalid`. Niente cambia; mai un 500 per un vincolo
    del database.
69. Un id di servizio bar in `idHourlyServices`, o di un altro stabilimento: 404 `service.not_found`; un settore sconosciuto
    in `idSectors`: 404 `catalog.sector_not_found`. Niente cambia. Letta la
    tab, si elimina un servizio del prezzo automatico e poi si salva con la `If-Match` letta: 409 `resource.version_conflict`,
    non 404.
70. `get-automatic-price-services` con `Accept-Language: en`: i nomi inglesi dove ci sono, gli altri in italiano; un servizio
    eliminato non compare. Letture senza `CatalogRead`, e creazione, salvataggi e immagine senza `CatalogSettings`: 403
    `auth.forbidden`; senza token 401 `auth.unauthenticated`; token senza stabilimento 403 `catalog.property_required`.
71. Ogni nome delle 43 icone: 204, e la lettura lo restituisce uguale. Un nome fuori dalle 43: 400 `validation.invalid` su
    `icon`, tranne quello che il prezzo automatico ha già. Con la riga dell'icona tolta dal catalogo: 404
    `service.icon_not_found`. Niente cambia.
72. `POST …/image` e poi `DELETE …/image`: la lettura dà `imageUrl`, poi `null` con `icon` `null`; la `version` di Info
    Generali non cambia con la DELETE. Letture, salvataggi e immagine con un id di un altro stabilimento o eliminato: 404
    `service.automatic_price_not_found`.
73. Due salvataggi con la stessa `If-Match` e dati diversi: il primo 204 con l'`ETag` nuovo, il secondo 409
    `resource.version_conflict`.
74. I tre elenchi vuoti, o assenti: 400 con il solo `validation.required` su `idBarServices`. Con gli id di tutti i servizi: 204 e
    la lettura li dà tutti; un servizio creato dopo non prende il prezzo automatico (preventivo). Poi con un servizio solo:
    204, e la lettura dà solo quello.
75. Un periodo salvato senza giorni: la lettura dà `daysOfWeek` `[]` e il periodo vale tutti i giorni.

API 47-51, con un token con `CatalogRead` e `CatalogSettings`:

76. `create-table-service` con due disponibilità e due righe di tavoli: 204; il servizio compare in `table-service-list` e le
    due letture restituiscono gli stessi dati. Nel registro una voce `catalog.table_service_created`. La prova 34 riesce anche
    con `create-table-service`.
77. `update-table-service-general` non cambia disponibilità, tavoli e caparra; `update-table-service-availability-bookings` non
    cambia nomi e "Metti in evidenza". Nel registro una voce `catalog.table_service_updated` per salvataggio.
78. Con `Tables`: nessuna riga → 400 `validation.required` su `tableGroups`; due righe con gli stessi posti, 501 tavoli o 101
    posti → 400 `validation.invalid`. Con `Seats`: `totalSeatQuantity` `null` → 400 `validation.required`; 10.001 → 400
    `validation.out_of_range`. Mai un 500 per un vincolo del database.
79. Una disponibilità senza `dateTo`, senza giorni scelti, o con un giorno senza `endTime` o con le due ore uguali: 400 con gli
    errori della tabella.
80. Una fascia salvata senza giorno: la lettura dà gli stessi orari in tutti e sette i giorni. Un giorno con due fasce: la
    prima in `startTime` ed `endTime`, l'altra in `otherTimeSlots`; rimandate uguali restano tutte e due. Otto fasce senza
    giorno: rimandate uguali 204, non un errore per le 50 fasce.
81. `averageMealMinutes` `null`: 204, e la lettura dà `null`; 5 → 400 `validation.out_of_range`; 100 resta 100. Un servizio
    senza durata si duplica (sezione 16) e si legge con `GET /api/catalog/table-services/{id}`; una prenotazione di quel
    servizio a un'ora fuori fascia si salva con la fine decisa dallo sviluppatore, mai un 500.
82. `Deposit` senza percentuale né importo: 400 `validation.required` su `depositPercentage`; 0 o 101 → 400
    `validation.out_of_range`. Un importo di 15 già salvato: la lettura dà `depositAmount` 15 e `depositPercentage` `null`;
    rimandato uguale resta. `None`: 204, e tipo e valore non ci sono più.
83. Posti minimi e massimi già salvati: un salvataggio con gli stessi valori letti non li cambia.
84. Le letture e i salvataggi con un id di un altro stabilimento o eliminato: 404 `service.table_service_not_found`. Letture
    senza `CatalogRead`, creazione e salvataggi senza `CatalogSettings`: 403 `auth.forbidden`; senza token 401
    `auth.unauthenticated`; token senza stabilimento 403 `catalog.property_required`. Due salvataggi con la stessa `If-Match`
    e dati diversi: il secondo 409 `resource.version_conflict`.
