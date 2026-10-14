# Tabella `booking_items_counts_details`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **461.251 righe**,
> query completa; lo storico anteriore a gennaio 2025 è in `booking_items_counts_details_archive_03072026`,
> 435.427 righe), model
> [`back-pre/app/models/bookingItemsCountNew.model.js`](../../../../../../back-pre/app/models/bookingItemsCountNew.model.js)
> (model `"booking_items_counts_details"`, definito con 4 argomenti → il 4°
> `{ freezeTableName, timestamps }` è ignorato ma il nome non viene pluralizzato ulteriormente),
> scrittura in [`helpers/booking.helper.js`](../../../../../../back-pre/app/controllers/helpers/booking.helper.js):
> `bookingCountsDetails1` (attiva, righe ~442-522), `bookingCountsDetails` (legacy),
> `additionalItemForCertainDaysCountsDetails` (righe ~3056-3135) e `aggregateBookingItems` (righe
> ~3476-3511); lettura/aggregazione in
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js)
> `getBookingItemCountData` / `getBookingData` / `checkAvailabilityRow` / `checkItemsAvailability` e in
> [`warehouse.controller.js`](../../../../../../back-pre/app/controllers/warehouse.controller.js) `bookingdetails`
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
- **Esempio di valori** (prod): interi `342.157` … `1.179.278` per 461.251 righe presenti — enorme
  scarto fra `AUTO_INCREMENT` e righe vive, coerente col pattern destroy+recreate ad ogni modifica di
  prenotazione.

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (`belongsTo`, nessuna FK). Indicizzato.
- **Descrizione**: prenotazione che consuma l'articolo. In update la riga della prenotazione viene
  **cancellata e ricreata** (`bookingItemCountDetails.destroy({ where: { booking_id: id } })`).
- **Esempio di valori** (prod): `100895`, `100898` — range `53.774` … `328.210`.

### bookingNumber
- **Tipo**: `varchar(255)` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber`
- **Descrizione**: numero prenotazione condiviso (aggiunto rispetto alla vecchia
  `booking_items_counts`).
- **Esempio di valori** (prod): `"1058710067"`, `"1058710068"`

### booking_element
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì (`NULL` su appena 7 righe su 461.251)
- **Descrizione**: nome dell'articolo consumato, così come compare in
  `packageDetails[].name` / `additionalList[].name`. `aggregateBookingItems` normalizza solo
  `"Sedia regista"` → `"Directors Chair"`. Valori reali di produzione: oltre al nucleo `Umbrella`,
  `Sunbed`, `Deckchair`, `Cabin`, `Tent`, `Directors Chair`, anche voci come `SpecialUmbrella1`,
  `Tassa di soggiorno`, e molte stringhe personalizzate per stabilimento — **stringhe libere**.
  (`booking_items_counts_details_archive_03072026` conserva le vecchie righe con i nomi in minuscolo
  `umbrella`/`sunbed`/`parking`: la convenzione di naming è cambiata nel tempo.)
- **Candidato a enumerazione**: **parziale** — esiste un nucleo fisso (`Umbrella`, `Sunbed`, `Cabin`,
  `Parking`, `Tent`, `Deckchair`, `Directors Chair`, `Chair` — gli stessi contatori di
  [`warehouses.md`](warehouses.md)) ma il campo contiene anche nomi articolo arbitrari definiti nei
  listini. Da modellare come **FK verso l'anagrafica articoli del modulo Catalogo**, non enum di
  stringhe; il confronto attuale per stringa esatta con `warehouses` è fragile. Vedi
  [`docs/03-gestione-dati.md` §2](../../progettazione/03-gestione-dati.md).
- **Esempio di valori** (prod): `"Umbrella"`, `"Cabin"`, `"SpecialUmbrella1"`, `"Tassa di soggiorno"`

### booking_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: giorno di consumo. In `bookingCountsDetails1` viene impostato per **tutte** le righe
  a `booking_start_date + 1 giorno` a inizio giornata (`startDate.setDate(getDate()+1)` +
  `setHours(0,0,0,0)`) — vedi criticità: non c'è un ciclo per-giorno, quindi la data non copre
  l'intero intervallo. In `additionalItemForCertainDaysCountsDetails` la data è invece quella specifica
  scelta (`val.dates`), ma scritta nel campo `selected_date` di
  [`additional_items_counts_details`](additional_items_counts_details.md), non qui.
- **Esempio di valori** (prod): sempre a mezzanotte, range `2022-05-02` … **`2027-08-27`** (date molto
  future: prenotazioni stagionali pluriennali).

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Esempio di valori** (prod): `10587`, `10554`, `231` — range `42` … `10856`.

### quantity
- **Tipo**: `int`, `NOT NULL` — default `0`
- **Nullabile**: No
- **Descrizione**: numero di unità di quell'articolo consumate dalla prenotazione per quel giorno.
  Calcolata da `aggregateBookingItems` sommando le `packageDetails[].quantity` per articolo (per le
  postazioni singole senza `packageDetails` vale `1`).
- **Esempio di valori** (prod): `1` (valore prevalente), fino a `98`; sono presenti anche righe a `0`.

### status
- **Tipo**: `tinyint(1)` — default `1` (`true`)
- **Nullabile**: Sì
- **Descrizione**: **coincide con il `payment_status` della prenotazione** al momento della scrittura
  (`status: payment_status` in `bookingCountsDetails1`). `0` viene forzato quando la prenotazione è a
  mezza giornata e per la stessa data/postazione esiste già una prenotazione `Morning`/`Afternoon`.
  Le query di disponibilità considerano solo `status = 1`.
  **Distribuzione reale di produzione** (461.251 righe): `1` = 398.143 (86%), `0` = 40.669, `3` = 22.438,
  `2` = 1 (una sola riga anomala).
- **Candidato a enumerazione**: **Sì**, ma è di fatto lo stesso vocabolario di
  [`bookings.status`](bookings.md) / `payment_status` (`0`/`1`/`2`/`3`) — da consolidare con quello.
- **Esempio di valori** (prod): `1`, `0`, `3`.

### packageName
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome del pacchetto listino di provenienza dell'articolo. **In produzione il campo è
  invece popolato**: solo 3.533 righe su 461.251 (1%) sono `NULL`, tutte le altre riportano un nome
  pacchetto reale (`"Ombrellone e 2 lettini"`, `"Ombrellone e 2 lettini - Hotel Mexico"`,
  `"Matrimoniale"`, `"Ombrellone 2 lettini sdraio sedia cabina"`, …). Quindi il flusso di scrittura
  attivo **valorizza** `packageName` — l'ipotesi precedente ("sempre `""`", basata sul campione dev e su
  una lettura di `bookingCountsDetails1`) **non è confermata dai dati reali**; da riverificare quale
  percorso di codice lo scrive (`bookingCountsDetails1` aggiornata, o gli altri writer
  `additionalItemForCertainDaysCountsDetails` / `aggregateBookingItems`).
- **Esempio di valori** (prod): `"Ombrellone e 2 lettini"`, `"Matrimoniale"`, `NULL` (1%).

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW`
- **Nullabile**: Sì (`NULL` su 139 righe)
- **Esempio di valori** (prod): range `2025-01-09 13:48:57` … `2026-09-08 09:02:10` — le righe più
  vecchie di gennaio 2025 sono state spostate nell'archivio; la tabella live copre ~20 mesi.

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
- **`status` sovraccarico**: riusa i codici di `payment_status` della prenotazione (`0`/`1`/`3`, più `2`
  su 1 riga in prod) come stato riga inventario; nessun `ENUM`/`CHECK`.
- **`packageName`**: contrariamente a quanto ipotizzato dal campione dev, **in produzione è popolato**
  (99% delle righe) con nomi pacchetto reali — la colonna non è morta; va tenuta e mappata a una FK
  verso il listino nel target.
- **Nessun timestamp automatico** (`timestamps: false` di fatto): `createdAt`/`updatedAt` gestiti a
  mano.
- **Esiste una serie di tabelle gemelle legacy** — `booking_items_counts` (vecchia, contatori a colonne
  fisse; in produzione **34.694 righe tutte del 2021**, ~98% su una property di test, contatori quasi
  tutti a `0`: dead legacy), `booking_items_counts_details_news` (scratch di migrazione, **0 righe** in
  produzione), `booking_items_counts_details_archive_03072026` (archivio, **435.427 righe**, `createdAt`
  fino al 2026-07-03 — lo split storico). Vedi
  [nota tabelle legacy in `_feature-tabelle.md`](_feature-tabelle.md).
