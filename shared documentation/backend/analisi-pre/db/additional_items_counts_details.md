# Tabella `additional_items_counts_details`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **3.055 righe**,
> query completa), model
> [`back-pre/app/models/additionalItemCountDetail.model.js`](../../../../../../back-pre/app/models/additionalItemCountDetail.model.js)
> (model `"additional_items_counts_details"`, 4 argomenti → 4° ignorato), scrittura **solo** in
> [`helpers/booking.helper.js`](../../../../../../back-pre/app/controllers/helpers/booking.helper.js)
> `additionalItemForCertainDaysCountsDetails` (righe ~3056-3135), invocata da
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) nel flusso `booking`
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
- **Esempio di valori** (prod): interi non contigui `174` … `9555` (3.055 righe presenti su un
  `AUTO_INCREMENT` molto più alto → tabella soggetta a destroy+recreate massivo, vedi criticità).

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK). Indicizzato.
- **Descrizione**: prenotazione di riferimento. In update viene cancellata e ricreata
  (`AdditionalItemsCountDetails.destroy({ where: { booking_id: id } })`).
- **Esempio di valori** (prod): `83017`, `53807`, `72453` — range `53.807` … `325.986`.

### bookingNumber
- **Tipo**: `varchar(255)` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber`
- **Esempio di valori** (prod): `"1054811081"`, `"1055710036"`, `"1055711311"`

### booking_element
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome dell'articolo aggiuntivo (`certainAddItemData[].name`). Stringa libera, come in
  `booking_items_counts_details`. **Distribuzione reale di produzione** (19 valori distinti su 3.055
  righe): `Sunbed` 1887, `Deckchair` 736, `Directors Chair` 304, `MARINELLA` 30, `Ingresso junior` 25,
  `Ingresso` 12, `Umbrella` 10, `XLSunbed` 10, `Cabin` 9, `Lettino Standard\t` 8 (**tab finale** — vedi
  criticità), `Sedia regista` 7, `Opzione Full (cabina, tavolo, parcheggio)` 5, `Parking` 3,
  `Lettino - Dipendente` 2, `Marinella` 2 (**duplicato di `MARINELLA` per case**), `CustomItem` 2,
  `Sdraio - Dipendente` 1, `Tent` 1, `Lettino maxi` 1.
- **Candidato a enumerazione**: **parziale** — poche decine di nomi, un nucleo condiviso con
  [`warehouses.md`](warehouses.md) più nomi personalizzati per stabilimento; da modellare come FK verso
  l'anagrafica articoli, non enum.
- **Esempio di valori**: `"Sunbed"`, `"Directors Chair"`, `"Lettino Standard\t"`

### selected_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: la **data specifica** per cui l'articolo aggiuntivo è stato scelto
  (`new Date(val.dates)` a inizio giornata). A differenza di
  [`booking_items_counts_details.booking_date`](booking_items_counts_details.md), qui c'è una riga per
  ciascuna data selezionata → l'intervallo è coperto correttamente.
- **Esempio di valori** (prod): sempre a mezzanotte, range `2024-07-14` … `2026-09-11` (include date
  future rispetto all'export: prenotazioni per la stagione successiva).

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Esempio di valori** (prod): `10548`, `10557`, `10588`, `228` — range `58` … `10843`; pochi
  stabilimenti usano davvero la selezione "a certe date".

### quantity
- **Tipo**: `int`, `NOT NULL` — default `0`
- **Nullabile**: No
- **Descrizione**: quantità dell'articolo per quella data (`certainAddItemData[].value`).
- **Distribuzione reale di produzione**: `1` (2620), `2` (363), `3` (36), `0` (23), poi **valori
  negativi**: `-20` (4), `-11` (2), `-8` (2), `-10` (1); massimo `17` (1). I `0` e i negativi sono dati
  sporchi (vedi criticità) — nessun `CHECK (quantity > 0)`.
- **Esempio di valori**: `1`, `2`, `0`, `-20`

### status
- **Tipo**: `tinyint(1)` — default `1` (`true`)
- **Nullabile**: Sì
- **Descrizione**: `= payment_status` della prenotazione (come in
  [`booking_items_counts_details.status`](booking_items_counts_details.md)). **In produzione è `1` per
  tutte le 3.055 righe** — la colonna non ha mai assunto un altro valore.
- **Candidato a enumerazione**: **Sì** — stesso vocabolario di `bookings.status`/`payment_status`.
- **Esempio di valori**: `1`

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW`
- **Nullabile**: Sì
- **Esempio di valori** (prod): range `2024-07-31 13:12:37` … `2026-09-07 09:27:55`; `createdAt` e
  `updatedAt` coincidono su tutte le righe campionate (la riga non viene mai aggiornata: si distrugge e
  si ricrea).

---

## Osservazioni verificate nel codice (criticità)

- **Duplicazione strutturale con `booking_items_counts_details`**: stessa semantica (consumo articolo),
  stessa quasi-struttura, si differenziano solo per il nome del campo data (`selected_date` vs
  `booking_date`) e per la presenza di questa solo quando la selezione è "a certe date". Le funzioni di
  verifica disponibilità devono interrogare/unire entrambe. Da unificare in un'unica tabella di
  consumo inventario con `date` sempre valorizzata per-giorno.
- **`booking_element` con dati sporchi** (confermato su produzione): presente `"Lettino Standard\t"`
  (tab finale, 8 righe) → confronto per stringa esatta con `warehouses`/altre tabelle fallisce
  silenziosamente; inoltre `"MARINELLA"` (30) e `"Marinella"` (2) coesistono come voci distinte solo per
  differenza di maiuscole. Stessa fragilità di `booking_items_counts_details`.
- **`quantity` con `0` e valori negativi** (confermato su produzione: 23 righe a `0`, 9 righe fra `-8` e
  `-20`): la colonna `NOT NULL DEFAULT 0` accetta qualsiasi intero. Un articolo "consumato" con quantità
  negativa falsa il calcolo di disponibilità che somma questa tabella. Nel target: `CHECK (quantity > 0)`
  e nessuna riga a quantità 0.
- **Update = destroy + recreate non transazionale** (in `additionalItemForCertainDaysCountsDetails` si
  fa `destroy` sia su `bookingItemCountDetails` sia su `AdditionalItemsCountDetails`, poi si ricrea).
- **`status` riusa `payment_status`**, senza `ENUM`/`CHECK`.
- **Scritta da un solo punto**: se `certainAddItemData` è vuoto la funzione non viene chiamata; nessun
  altro percorso popola questa tabella.
