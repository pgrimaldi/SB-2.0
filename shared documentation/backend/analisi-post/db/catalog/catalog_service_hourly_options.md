# Tabella `catalog_service_hourly_options`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_service_hourly_options` · **forma canonica**: `catalog.service_hourly_options`.
>
> Tabella nuova, aggiunta il 2026-11-05 (migration ServicesAndNextSeason) per allineare le API al frontend (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Disponibilità di un servizio orario: fascia del giorno, giorni, durate con prezzo, IVA e massimo per fascia. Appartiene ai prezzi del servizio (per periodo tariffario o senza periodo) oppure a un'opzione di prenotazione con date.

Una riga per disponibilità: la fascia del giorno e i giorni in cui il servizio orario si prenota, con le durate prenotabili e il loro prezzo (righe di [`catalog_service_sale_item_time_slots`](catalog_service_sale_item_time_slots.md) con `option_id`), l'IVA e il massimo di prenotazioni per fascia. Appartiene ai **prezzi** del servizio (`period_group_key` = periodo tariffario, o NULL senza periodi) oppure a un'**opzione di prenotazione** con date (`booking_option_id`), mai a entrambi.

**Migrazione**: ogni servizio orario del legacy ha una sola configurazione (`services_types.hourlyServices[]`: `HourFrom`, `HourTo`, `day_constant`, `percentIVA`, `maximumReservations`, `fixedTimeSlotStatus`) e diventa una disponibilità senza periodo (passo `catalog_service_hourly_options` dell'utility 01, stessa conversione della migration). Sul DB locale: 152 disponibilità, 1.661 durate agganciate.

## Campi

### id
- **Legacy**: nessuno.
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### public_id
- **Legacy**: nessuno.
- **Descrizione**: Id pubblico.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### sale_item_id
- **Legacy**: servizio orario di `hourlyServices[]`.
- **Descrizione**: Servizio orario.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_service_sale_items.id`](catalog_service_sale_items.md) `ON DELETE CASCADE`.

### period_group_key
- **Legacy**: nessuno.
- **Descrizione**: Periodo tariffario dei prezzi (pricing_tariff_periods.period_group_key); NULL = senza periodo o legata a un'opzione di prenotazione.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### booking_option_id
- **Legacy**: nessuno.
- **Descrizione**: Opzione di prenotazione con date a cui appartiene; NULL = fa parte dei prezzi del servizio.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_service_booking_options.id`](catalog_service_booking_options.md) `ON DELETE CASCADE`.

### time_from
- **Legacy**: `hourlyServices[].HourFrom`.
- **Descrizione**: Inizio della fascia del giorno in cui si prenota; NULL = dall'apertura.
- **Tipo**: `time without time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### time_to
- **Legacy**: `hourlyServices[].HourTo`.
- **Descrizione**: Fine della fascia del giorno; NULL = fino alla chiusura.
- **Tipo**: `time without time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### weekdays
- **Legacy**: `hourlyServices[].day_constant[]` (giorni con `status`).
- **Descrizione**: Giorni in cui vale; vuoto = tutti.
- **Tipo**: `catalog.weekday[]`.
- **Default**: `'{}'::catalog.weekday[]`.
- **Nullabile**: No.
- **Relazioni**: —

### vat_rate
- **Legacy**: `hourlyServices[].percentIVA`.
- **Descrizione**: Aliquota IVA in percentuale; NULL = quella del servizio.
- **Tipo**: `numeric(5,2)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### max_per_slot
- **Legacy**: `hourlyServices[].maximumReservations`.
- **Descrizione**: Massimo di prenotazioni per fascia; NULL = senza limite.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### has_fixed_time_slots
- **Legacy**: `hourlyServices[].fixedTimeSlotStatus`.
- **Descrizione**: Le prenotazioni partono a orari fissi, uno dopo l'altro, invece che a qualsiasi orario.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —

### sort_order
- **Legacy**: nessuno.
- **Descrizione**: Ordine delle disponibilità del servizio.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —

### created_at
- **Legacy**: nessuno.
- **Descrizione**: Creazione della riga.
- **Tipo**: `timestamp with time zone`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —

### updated_at
- **Legacy**: nessuno.
- **Descrizione**: Ultima modifica della riga.
- **Tipo**: `timestamp with time zone`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —

### deleted_at
- **Legacy**: nessuno.
- **Descrizione**: Soft-delete.
- **Tipo**: `timestamp with time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —
