# Tabella `additional_items_counts_details`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/additionalItemCountDetail.model.js`](../../back-pre/app/models/additionalItemCountDetail.model.js)
> (model `"additional_items_counts_details"`, 4 argomenti → 4° ignorato), scrittura **solo** in
> [`helpers/booking.helper.js`](../../back-pre/app/controllers/helpers/booking.helper.js)
> `additionalItemForCertainDaysCountsDetails` (righe ~3056-3135), invocata da
> [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js) nel flusso `booking`
> quando `req.body.data[index].certainAddItemData` non è vuoto; lettura/aggregazione insieme a
> [`booking_items_counts_details`](booking_items_counts_details.md) nelle stesse funzioni di verifica
> disponibilità (`getBookingItemCountData`, `checkAvailabilityRow`, `warehouse.controller.bookingdetails`).
>
> **Nota di contesto** — variante di [`booking_items_counts_details`](booking_items_counts_details.md)
> per gli **articoli aggiuntivi selezionati solo per certe date** (non per tutta la durata della
> prenotazione). Mentre `booking_items_counts_details` registra il consumo del pacchetto,
> `additional_items_counts_details` registra **una riga per ogni articolo aggiuntivo × data scelta**
> (`bookings.certainAddItemData[]`).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK). Indicizzato.
- **Descrizione**: prenotazione di riferimento. In update viene cancellata e ricreata
  (`AdditionalItemsCountDetails.destroy({ where: { booking_id: id } })`).
- **Esempio di valori**: 269270

### bookingNumber
- **Tipo**: `varchar(255)` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber`
- **Esempio di valori**: `"1054814816"`

### booking_element
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome dell'articolo aggiuntivo (`certainAddItemData[].name`). Valori reali osservati:
  `Sunbed`, `Deckchair`, `Directors Chair`, `Ingresso`, `Lettino Standard\t` (con **tab finale** — vedi
  criticità). Stringa libera, come in `booking_items_counts_details`.
- **Candidato a enumerazione**: **parziale** — stesso nucleo articoli di
  [`warehouses.md`](warehouses.md) più nomi personalizzati; da modellare come FK verso l'anagrafica
  articoli, non enum.
- **Esempio di valori**: `"Sunbed"`, `"Ingresso"`, `"Lettino Standard\t"`

### selected_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: la **data specifica** per cui l'articolo aggiuntivo è stato scelto
  (`new Date(val.dates)` a inizio giornata). A differenza di
  [`booking_items_counts_details.booking_date`](booking_items_counts_details.md), qui c'è una riga per
  ciascuna data selezionata → l'intervallo è coperto correttamente.
- **Esempio di valori**: `2026-08-01 00:00:00`, `2026-07-31 00:00:00`

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Esempio di valori**: 10548

### quantity
- **Tipo**: `int`, `NOT NULL` — default `0`
- **Nullabile**: No
- **Descrizione**: quantità dell'articolo per quella data (`certainAddItemData[].value`).
- **Esempio di valori**: `1`

### status
- **Tipo**: `tinyint(1)` — default `1` (`true`)
- **Nullabile**: Sì
- **Descrizione**: `= payment_status` della prenotazione (come in
  [`booking_items_counts_details.status`](booking_items_counts_details.md)). Nel campione è sempre `1`.
- **Candidato a enumerazione**: **Sì** — stesso vocabolario di `bookings.status`/`payment_status`.
- **Esempio di valori**: `1`

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW`
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-27 07:50:14`

---

## Osservazioni verificate nel codice (criticità)

- **Duplicazione strutturale con `booking_items_counts_details`**: stessa semantica (consumo articolo),
  stessa quasi-struttura, si differenziano solo per il nome del campo data (`selected_date` vs
  `booking_date`) e per la presenza di questa solo quando la selezione è "a certe date". Le funzioni di
  verifica disponibilità devono interrogare/unire entrambe. Da unificare in un'unica tabella di
  consumo inventario con `date` sempre valorizzata per-giorno.
- **`booking_element` con dati sporchi**: presente `"Lettino Standard\t"` (tab finale) → confronto per
  stringa esatta con `warehouses`/altre tabelle fallisce silenziosamente. Stessa fragilità di
  `booking_items_counts_details`.
- **Update = destroy + recreate non transazionale** (in `additionalItemForCertainDaysCountsDetails` si
  fa `destroy` sia su `bookingItemCountDetails` sia su `AdditionalItemsCountDetails`, poi si ricrea).
- **`status` riusa `payment_status`**, senza `ENUM`/`CHECK`.
- **Scritta da un solo punto**: se `certainAddItemData` è vuoto la funzione non viene chiamata; nessun
  altro percorso popola questa tabella.
