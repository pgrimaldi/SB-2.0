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
che va aggiunto a quei contratti.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET + POST | Le 13 coppie lettura e salvataggio dei pannelli di modifica (tabella della sezione 1) | Controllo di versione, una versione per tab: `version` nelle letture, `If-Match` ed `ETag` nei salvataggi, 409 `resource.version_conflict` |

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
- Il 409 si controlla dopo il 404 `resource.not_found` dell'elemento (e dei servizi non dello stabilimento, nei Prezzi
  automatici) e dopo il 400 `validation.failed`, subito prima del salvataggio.
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
6. Un salvataggio con un id che non esiste e una `If-Match` vecchia risponde 404 `resource.not_found`, non 409; uno con
   un corpo non valido e una `If-Match` vecchia risponde 400 `validation.failed`.
7. Duplicato un servizio, un salvataggio riuscito della copia non cambia le versioni del servizio di partenza.
