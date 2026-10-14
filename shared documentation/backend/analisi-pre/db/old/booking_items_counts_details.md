# Tabella `booking_items_counts_details`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/bookingItemsCountNew.model.js`](../../back-pre/app/models/bookingItemsCountNew.model.js)
> (model `"booking_items_counts_details"`, definito con 4 argomenti → il 4°
> `{ freezeTableName, timestamps }` è ignorato ma il nome non viene pluralizzato ulteriormente),
> scrittura in [`helpers/booking.helper.js`](../../back-pre/app/controllers/helpers/booking.helper.js):
> `bookingCountsDetails1` (attiva, righe ~442-522), `bookingCountsDetails` (legacy),
> `additionalItemForCertainDaysCountsDetails` (righe ~3056-3135) e `aggregateBookingItems` (righe
> ~3476-3511); lettura/aggregazione in
> [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js)
> `getBookingItemCountData` / `getBookingData` / `checkAvailabilityRow` / `checkItemsAvailability` e in
> [`warehouse.controller.js`](../../back-pre/app/controllers/warehouse.controller.js) `bookingdetails`
> (confronto con `warehouses` per la disponibilità); associazione
> `db.BookingItemsCountDetails.belongsTo(db.booking, { foreignKey: "booking_id" })`.
> Frontend `front-pre`/`front-customer-pre`: `src/services/beachmapService.js`,
> `src/components/reserveArticle/**` (verifica disponibilità articoli prima di confermare).
>
> **Nota di contesto** — è il **registro di consumo dell'inventario fisico** (ombrelloni, lettini,
> sdraio, cabine, tende, ...) **una riga per (prenotazione × articolo × giorno)**. Alla creazione della
> prenotazione, per ogni giorno dell'intervallo e per ogni tipo di articolo aggregato dalle
> `packageDetails`, viene inserita una riga con la quantità. Sommando `quantity` per
> `(property_id, booking_element, booking_date, status)` e confrontando con
> `warehouses.<articolo>_count` si ottiene la disponibilità residua.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (`belongsTo`, nessuna FK). Indicizzato.
- **Descrizione**: prenotazione che consuma l'articolo. In update la riga della prenotazione viene
  **cancellata e ricreata** (`bookingItemCountDetails.destroy({ where: { booking_id: id } })`).
- **Esempio di valori**: 171899, 269270

### bookingNumber
- **Tipo**: `varchar(255)` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber`
- **Descrizione**: numero prenotazione condiviso (aggiunto rispetto alla vecchia
  `booking_items_counts`).
- **Esempio di valori**: `"1055718222"`

### booking_element
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome dell'articolo consumato, così come compare in
  `packageDetails[].name` / `additionalList[].name`. `aggregateBookingItems` normalizza solo
  `"Sedia regista"` → `"Directors Chair"`. Valori reali osservati (campione): `Umbrella`, `Sunbed`,
  `Deckchair`, `Directors Chair`, `Tent`, `Tavolo`, `SpecialUmbrella1`, `Lettino - Dipendente`,
  `Sdraio cani`, `Ingresso ALTRI` — **stringhe libere**, incluse voci personalizzate dello
  stabilimento.
- **Candidato a enumerazione**: **parziale** — esiste un nucleo fisso (`Umbrella`, `Sunbed`, `Cabin`,
  `Parking`, `Tent`, `Deckchair`, `Directors Chair`, `Chair` — gli stessi contatori di
  [`warehouses.md`](warehouses.md)) ma il campo contiene anche nomi articolo arbitrari definiti nei
  listini. Da modellare come **FK verso l'anagrafica articoli del modulo Catalogo**, non enum di
  stringhe; il confronto attuale per stringa esatta con `warehouses` è fragile. Vedi
  [`docs/03-gestione-dati.md` §2](../../docs/03-gestione-dati.md).
- **Esempio di valori**: `"Deckchair"`, `"Sunbed"`, `"Umbrella"`, `"Directors Chair"`,
  `"Lettino - Dipendente"`

### booking_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: giorno di consumo. In `bookingCountsDetails1` viene impostato per **tutte** le righe
  a `booking_start_date + 1 giorno` a inizio giornata (`startDate.setDate(getDate()+1)` +
  `setHours(0,0,0,0)`) — vedi criticità: non c'è un ciclo per-giorno, quindi la data non copre
  l'intero intervallo. In `additionalItemForCertainDaysCountsDetails` la data è invece quella specifica
  scelta (`val.dates`), ma scritta nel campo `selected_date` di
  [`additional_items_counts_details`](additional_items_counts_details.md), non qui.
- **Esempio di valori**: `2026-07-28 00:00:00`

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Esempio di valori**: 10557

### quantity
- **Tipo**: `int`, `NOT NULL` — default `0`
- **Nullabile**: No
- **Descrizione**: numero di unità di quell'articolo consumate dalla prenotazione per quel giorno.
  Calcolata da `aggregateBookingItems` sommando le `packageDetails[].quantity` per articolo (per le
  postazioni singole senza `packageDetails` vale `1`).
- **Esempio di valori**: `1`, `2`

### status
- **Tipo**: `tinyint(1)` — default `1` (`true`)
- **Nullabile**: Sì
- **Descrizione**: **coincide con il `payment_status` della prenotazione** al momento della scrittura
  (`status: payment_status` in `bookingCountsDetails1`). Valori reali osservati: `1` (94), `0` (4),
  `3` (2). `0` viene forzato quando la prenotazione è a mezza giornata e per la stessa data/postazione
  esiste già una prenotazione `Morning`/`Afternoon` (`payment_status = 0` nel ramo `sameDayItems`).
  Le query di disponibilità considerano solo `status = 1`.
- **Candidato a enumerazione**: **Sì**, ma è di fatto lo stesso vocabolario di
  [`bookings.status`](bookings.md) / `payment_status` (`0`/`1`/`3`) — da consolidare con quello.
- **Esempio di valori**: `1`, `0`, `3`

### packageName
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome del pacchetto listino di provenienza dell'articolo. In `bookingCountsDetails1`
  (versione attiva) viene **sempre scritto `""`** (`packageName: ""`); solo la versione commentata lo
  valorizzava con `pkgData.PackageName`. → di fatto **campo non popolato** dal flusso corrente.
- **Deprecato**: parzialmente — colonna **scritta sempre come `""`** dal flusso attivo
  (`bookingCountsDetails1`); solo la versione precedente (commentata) la valorizzava con
  `pkgData.PackageName`.
- **Esempio di valori**: `""` (valore reale, sempre); il valore *inteso* sarebbe un nome pacchetto
  tipo `"Ombrellone e 2 lettini"`

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW`
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-27 08:20:20`

---

## Osservazioni verificate nel codice (criticità)

- **`booking_date` non copre l'intervallo**: `bookingCountsDetails1` inserisce le righe articolo
  **tutte con la stessa data** (`booking_start_date + 1 giorno`), senza iterare i giorni
  `start..end`. La vecchia `bookingCountsDetails`/`bookingCounts` avevano un ciclo giorno-per-giorno
  ricorsivo; quello attivo no. Conseguenza: per prenotazioni multi-giorno il consumo inventario
  risulta registrato su un solo giorno → la disponibilità degli altri giorni è sovrastimata.
  Inoltre il `+1 giorno` sembra una compensazione di fuso non documentata.
- **Update = destroy + recreate non transazionale**: in modifica si fa `destroy({ where: { booking_id } })`
  e poi si reinserisce; nel mezzo la disponibilità calcolata è inconsistente e, in caso di errore
  dopo il destroy, il consumo sparisce del tutto.
- **`booking_element` stringa libera confrontata per uguaglianza**: la disponibilità si calcola
  facendo `GROUP BY booking_element` e confrontando con colonne fisse di `warehouses`
  (`umbrella_count`, `sunbed_count`, ...). Articoli con nomi personalizzati (`"Lettino - Dipendente"`,
  `"Sdraio cani"`, `"Ingresso ALTRI"`) non hanno una colonna corrispondente in `warehouses` → non
  vengono limitati. Refusi/spazi (`"Lettino Standard\t"` in
  [`additional_items_counts_details`](additional_items_counts_details.md)) diventano permanenti.
- **`status` sovraccarico**: riusa i codici di `payment_status` della prenotazione (`0`/`1`/`3`) come
  stato riga inventario; nessun `ENUM`/`CHECK`.
- **`packageName` morto** nel flusso attivo (sempre `""`).
- **Nessun timestamp automatico** (`timestamps: false` di fatto): `createdAt`/`updatedAt` gestiti a
  mano.
- **Esiste una serie di tabelle gemelle legacy** — `booking_items_counts` (vecchia, contatori a colonne
  fisse, ultimi dati 2021), `booking_items_counts_details_news` (scratch di migrazione, 0 righe,
  popolata solo dall'endpoint `databaseChanges`), `booking_items_counts_details_archive_03072026`
  (archivio). Vedi [nota tabelle legacy in `_feature-tabelle.md`](_feature-tabelle.md).
