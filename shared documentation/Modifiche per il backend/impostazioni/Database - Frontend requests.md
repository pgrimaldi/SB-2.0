# Database - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Database** del gestionale funzioni come la vuole il frontend. Il
documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload, risposta, regole ed
errori attesi, e quali scelte vanno chieste allo sviluppatore prima di scrivere codice.

La pagina è `/settings/database`: la tabella delle prenotazioni da cancellare ("Cancella dati") e, su `/settings/database/trash`,
la stessa tabella con le prenotazioni del cestino (Ripristina, Elimina definitivamente, Svuota cestino).

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/booking/reservations/reservation-list` | Elenco delle prenotazioni per la tabella, a pagine |
| 2 | POST | `/api/booking/trash/trash-list` | Elenco del cestino per la tabella, a pagine |
| 3 | POST | `/api/catalog/agreement-partners/agreement-partner-combobox` | Tendina "Strutture convenzionate" del popup Filtri |

Le azioni usano le API che ci sono già, così come sono: "Cancella dati" `POST /api/booking/cleanup/move-to-trash`, "Ripristina"
`POST /api/booking/trash/restore`, "Elimina definitivamente" `POST /api/booking/trash/purge` e "Svuota cestino"
`POST /api/booking/trash/empty`.

## Regole comuni

- **Le API esistenti non cambiano**: `GET /api/booking/reservations`, `GET /api/booking/trash` e
  `GET /api/catalog/agreement-partners/lookup` restano come sono. Le tre API nuove servono perché le tabelle e le tendine del
  frontend hanno tutte la stessa richiesta e la stessa risposta.
- **Moduli**: le API 1 e 2 in Booking (`Endpoints/Endpoints.cs`, `MapReservations` e `MapTrash`; `ReservationService.ListAsync`,
  `ReservationRepository.ListAsync`), la 3 in Catalog (`Endpoints/AgreementEndpoints.cs`, `AgreementsService`). I nomi delle rotte non si
  confondono con `/{id:guid}`, perché non sono UUID.
- **Permessi**: come le API che sostituiscono. API 1 `bookings.list`, API 2 `bookings.delete`, API 3 `CatalogRead`. Senza: 403
  `auth.forbidden`. Senza token valido: 401 `auth.unauthenticated`.
- **Stabilimento**: lo prende dal token, nessuna richiesta lo contiene. API 1 e 2: `BookingScope.PropertyId`, 403
  `booking.property_required` senza. API 3: `CurrentProperty.IdAsync`, 403 `catalog.property_required` e 404
  `catalog.property_not_found`.
- **Codici**: `reservationKind`, `reservationStatus` e `paymentStatus` sono quelli che il backend ha già (`Codes.Of` e `PaymentStatuses`).
- **JSON** in camelCase, con i nomi esatti di questo documento. Gli id sono i `public_id` (UUID).
- **Errori** nel formato Problem Details del backend, con `code`, `traceId` ed `errors[]` quando ci sono.
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo: l'agente glielo chiede, in particolare per
  ordinare e cercare sul nome del cliente (modulo Identity) e su quello della convenzione (modulo Catalog), che non stanno nello schema
  `booking`. Paginazione e `total` devono valere su tutto l'elenco ordinato, non sulla sola pagina.

## 1. POST /api/booking/reservations/reservation-list

Tabella della pagina Database. Le righe sono quelle di `GET /api/booking/reservations`: le prenotazioni dello stabilimento che non sono
nel cestino, in qualsiasi stato.

### Richiesta

La richiesta standard delle tabelle del frontend, con le stesse regole e gli stessi errori su `page`, `pageSize`, `sortDirection` e
`search` di `POST /api/catalog/price-lists/packages-list` (`Listino - Frontend requests.md`, sezione 1):

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "rossi",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-07-01T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-08-31T23:59:59.999Z"] },
    { "field": "bookingTypology", "value": ["Daily", "SevenDays"] },
    { "field": "agreementBooking", "value": ["Yes"] },
    { "field": "idAffiliatedHotel", "value": ["5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41"] },
    { "field": "paymentStatus", "value": ["paid", "partial"] }
  ],
  "sortField": "dateFrom",
  "sortDirection": "Ascending"
}
```

- `pageSize`: 10, 20, 50 o 100.
- `search`: `""` = nessuna ricerca, al massimo 100 caratteri. Trova quello che trova già `GET /api/booking/reservations`
  (`ReservationQuery.Search`): numero di prenotazione, nome, email o telefono del cliente, nome della postazione.
- `filters`: un oggetto per campo, `value` sempre un array con almeno un valore. I valori dello stesso campo sono alternative, i campi
  diversi valgono tutti insieme. `[]` = nessun filtro.
- `sortField`: un campo della tabella "Ordine", oppure `""`.

### Filtri

| `field` | Valori | Regola |
|---|---|---|
| `datetimeFrom`, `datetimeTo` | un valore; sempre insieme | Le prenotazioni con almeno un giorno nel periodo, come `From` e `To` della `GET`. Sono **giorni**, non istanti: conta la data scritta (`2026-07-01`), senza fuso; `datetimeTo` è compreso. |
| `bookingTypology` | `Daily`, `MultiDay`, `SevenDays`, `FourteenDays`, `Monthly`, `Seasonal` | Vedi sotto. |
| `agreementBooking` | `Yes`, `No` | `Yes`: la prenotazione ha una convenzione (`AgreementPartnerId`, o una riga di convenzione non eliminata). `No`: le altre. |
| `idAffiliatedHotel` | id delle convenzioni (API 3) | Le prenotazioni con una di quelle convenzioni, come `AgreementPartnerId` della `GET`. Un id che non c'è non è un errore: non trova niente. |
| `paymentStatus` | `unpaid`, `paid`, `partial` | Come `PaymentStatus` della `GET`, ma anche più di uno. |

**`bookingTypology`** oggi nel backend non esiste: c'è solo `HasSeasonalPass`. Il frontend propone questa regola, sui giorni della
prenotazione (da `StayFrom` a `StayTo`, compresi), che l'agente **conferma con lo sviluppatore**. Ogni prenotazione ha una sola
tipologia:

| Valore | Nella pagina | Regola proposta |
|---|---|---|
| `Seasonal` | Stagionale | `HasSeasonalPass`, qualunque sia la durata |
| `Daily` | Giornaliera | 1 giorno |
| `SevenDays` | 7 giorni | 7 giorni |
| `FourteenDays` | 14 giorni | 14 giorni |
| `Monthly` | Mensile | da 28 a 31 giorni |
| `MultiDay` | Più giorni | tutte le altre durate |

Una prenotazione senza date, e senza abbonamento, non ha tipologia.

Un `field` non previsto o ripetuto, un array vuoto, un valore fuori elenco, una data o un UUID che non si legge: 400 `validation.failed`
con `validation.invalid` su `filters`. `datetimeTo` prima di `datetimeFrom`: `validation.end_before_start` su `filters`. Il frontend non li
manda.

### Ordine

| `sortField` | Colonna | Ordine |
|---|---|---|
| `customerName` | Cliente | Alfabetico, senza contare maiuscole e accenti; senza cliente in fondo, con tutti e due i versi |
| `reservationKind` | Tipologia prenotazione | `spot`, poi `service` |
| `dateFrom`, `dateTo` | Dal, al | Per giorno; senza data in fondo, con tutti e due i versi |
| `agreementName` | Convenzioni | Alfabetico, come il cliente; senza convenzione in fondo, con tutti e due i versi |
| `reservationStatus` | Stato | `pending`, `confirmed`, `cancelled` |
| `""` | | Come `dateFrom` crescente, che è l'ordine di oggi |

A parità di valore: per `dateFrom` crescente, poi per id interno.

### Risposta

200: `total` conta tutte le prenotazioni trovate, `rows` è la pagina richiesta. Una `page` oltre l'ultima risponde 200 con `rows` vuoto e
il `total` vero. È un DTO con `total` e `rows`, non `PagedResult`.

```json
{
  "total": 2,
  "rows": [
    {
      "idBooking": "3d1e5f7a-9b2c-4d6e-8f1a-2b3c4d5e6f70",
      "customerName": "Mario Rossi",
      "reservationKind": "spot",
      "dateFrom": "2026-07-05T00:00:00.000Z",
      "dateTo": "2026-07-12T00:00:00.000Z",
      "agreementName": "Hotel Esempio",
      "reservationStatus": "confirmed",
      "isTrashed": false
    },
    {
      "idBooking": "7a8b9c0d-1e2f-4a3b-9c4d-5e6f7a8b9c01",
      "customerName": null,
      "reservationKind": "service",
      "dateFrom": "2026-08-02T00:00:00.000Z",
      "dateTo": "2026-08-02T00:00:00.000Z",
      "agreementName": null,
      "reservationStatus": "pending",
      "isTrashed": false
    }
  ]
}
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `idBooking` | `PublicId` | string (UUID) | Non si mostra: serve alle azioni. |
| `customerName` | `CustomerCard.FullName` | string o `null` | Come `CustomerName` di `ReservationSummaryDto`. |
| `reservationKind` | `Kind` | `spot` o `service` | `ReservationSummaryDto` oggi non lo ha. |
| `dateFrom`, `dateTo` | `StayFrom`, `StayTo` | string (giorno UTC) o `null` | Il giorno a mezzanotte UTC. |
| `agreementName` | nome della convenzione | string o `null` | La convenzione è quella di `AgreementPartnerId` di `ReservationSummaryDto`; la `GET` dà solo l'id. `null` senza convenzione o se è stata eliminata. |
| `reservationStatus` | `Status` | `pending`, `confirmed` o `cancelled` | |
| `isTrashed` | `IsTrashed` | boolean | Sempre `false`. |

### Errori

Nell'ordine in cui si controllano: 401 `auth.unauthenticated`, 403 `auth.forbidden`, 400 `validation.failed` con `errors[]`, 403
`booking.property_required`. Il frontend mostra ogni errore nel popup di errore della tabella (testo fisso, con "Riprova").

## 2. POST /api/booking/trash/trash-list

Tabella del Cestino. Richiesta, filtri, ordine, risposta ed errori sono quelli della sezione 1, con queste differenze:

- **Righe**: quelle di `GET /api/booking/trash`, cioè le prenotazioni nel cestino dello stabilimento (`DeletedAt` e `CancelledAt` valorizzati).
  Quelle eliminate nel vecchio gestionale non ci sono.
- **Permesso**: `bookings.delete`.
- **`sortField` `""`**: le ultime messe nel cestino prima (`DeletedAt` decrescente), come oggi.
- `reservationStatus` è `cancelled` e `isTrashed` `true` in ogni riga.

Il frontend la chiama anche senza ricerca né filtri dopo un ripristino o un'eliminazione, per sapere se il cestino è vuoto (`total` 0).

## 3. POST /api/catalog/agreement-partners/agreement-partner-combobox

Tutte le convenzioni dello stabilimento, per la tendina a scelta multipla "Strutture convenzionate" del popup Filtri, riletta a ogni
apertura del popup. Segue la regola di tutte le tendine del frontend: `POST`, senza corpo né pagine, risposta `[{ id, value }]`, come
`sector-combobox` (`Servizi - Frontend requests.md`, sezione 25).

- **Le convenzioni sono quelle di `GET /api/catalog/agreement-partners/lookup`** (`AgreementsService.LookupPartnersAsync`): tutte quelle dello
  stabilimento, senza le eliminate, in ordine di nome.

### Richiesta

`POST` senza corpo.

### Risposta

200 con un array di `{ id, value }`, vuoto se lo stabilimento non ha convenzioni. Nessun altro campo:

```json
[
  { "id": "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a42", "value": "Beach Hotel" },
  { "id": "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41", "value": "Hotel Miramare" }
]
```

| Campo | In `AgreementPartnerLookupDto` | Tipo |
|---|---|---|
| `id` | `id` | string (UUID) |
| `value` | `name` | string |

### Errori

401 `auth.unauthenticated`, 403 `auth.forbidden`, 403 `catalog.property_required`, 404 `catalog.property_not_found`. Il frontend mostra
l'errore sotto la tendina e rilegge l'elenco quando l'utente prova ad aprirla.

## Da segnalare allo sviluppatore

**Il ripristino rimette sempre "Confermata"**: `Reservation.RestoreFromTrash` (`Reservation.cs:356`) imposta `Confirmed` qualunque fosse lo
stato prima, e `TrashService.RestoreAsync` riattiva i consumi rilasciati. Nel cestino ci vanno prenotazioni in qualsiasi stato
(`CleanupAsync` e `DeleteAsync` non lo guardano), e `CancelForTrash` non conserva quello di prima. Quindi dopo un ripristino:

- una prenotazione online **in attesa** di conferma torna confermata, senza che nessuno l'abbia accettata;
- una prenotazione già **annullata**, spostata nel cestino con "Cancella dati", torna confermata e riprende le sue postazioni.

Per il ripristino di uno stabilimento il caso è già gestito: `ReactivateAfterPropertyRestore` rimette `Pending` quando
`OnlineConfirmationState` è in attesa e non tocca quelle annullate per un altro motivo. Se e come correggerlo lo decide lo sviluppatore:
l'agente glielo segnala. Il frontend non cambia.

## Verifica

1. Senza token: 401; senza il permesso: 403 `auth.forbidden` (`bookings.list` per la 1, `bookings.delete` per la 2, `CatalogRead` per la 3);
   con un token senza stabilimento: 403. Le API esistenti rispondono come prima.
2. `reservation-list` con 12 prenotazioni e `pageSize` 10: `total` 12, 10 righe nella pagina 1, 2 nella 2, nessuna nella 3 con `total` 12.
   Nessuna prenotazione del cestino o di un altro stabilimento. Una di soli servizi ha `reservationKind` `service`.
3. Ricerca per nome del cliente, numero di prenotazione e nome della postazione: la prenotazione si trova in tutti e tre i casi.
4. Ordine per ognuno dei sei campi, nei due versi, con i valori mancanti in fondo e le pagine coerenti fra loro.
5. Ogni filtro da solo, due valori dello stesso campo (alternative) e due campi insieme. Periodo di un giorno: solo le prenotazioni che
   lo toccano. Una prenotazione di 7 giorni con l'abbonamento è solo `Seasonal`.
6. `page` 0, `pageSize` 25, `sortField` sconosciuto, un filtro con un valore fuori elenco o vuoto, `search` di 101 caratteri: 400 con il suo
   `errors[]`.
7. `trash-list`: solo le prenotazioni del cestino, le ultime eliminate prima, `isTrashed` `true`. Dopo `POST /api/booking/trash/empty`: `total` 0.
8. `agreement-partner-combobox`: le stesse convenzioni di `lookup`, con `id` e `value`; una convenzione eliminata non c'è.
