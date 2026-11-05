# Tabella `catalog_service_booking_options`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_service_booking_options` · **forma canonica**: `catalog.service_booking_options`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Opzioni di prenotazione di un servizio: periodo, giorni, orari da gestionale e online, capienza e limiti per prenotazione.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### sale_item_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Servizio.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_service_sale_items.id`](catalog_service_sale_items.md) `ON DELETE CASCADE`.

### starts_on
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Inizio del periodo in cui vale l'opzione; NULL = tutta la stagione.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### ends_on
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Fine del periodo.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### weekdays
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Giorni in cui vale l'opzione; vuoto = tutti.
- **Tipo**: `catalog.weekday[]`.
- **Default**: `'{}'::catalog.weekday[]`.
- **Nullabile**: No.
- **Relazioni**: —

### management_from
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Orari prenotabili dal gestionale: inizio.
- **Tipo**: `time without time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### management_to
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Orari prenotabili dal gestionale: fine.
- **Tipo**: `time without time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### online_from
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Orari prenotabili online: inizio.
- **Tipo**: `time without time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### online_to
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Orari prenotabili online: fine.
- **Tipo**: `time without time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### max_per_hour
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Massimo di prenotazioni all'ora.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### max_per_day
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Massimo di prenotazioni al giorno.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### max_per_slot
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Servizi orari: massimo di prenotazioni per fascia.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### interval_minutes
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Tempo minimo fra due ordini dello stesso cliente.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### min_items
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Minimo di articoli per prenotazione.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### max_items
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Massimo di articoli per prenotazione.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### sort_order
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Ordine delle opzioni.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —

### created_at
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Creazione della riga.
- **Tipo**: `timestamp with time zone`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —

### updated_at
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Ultima modifica della riga.
- **Tipo**: `timestamp with time zone`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —

### deleted_at
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Soft-delete.
- **Tipo**: `timestamp with time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

> **Nota 2026-11-05** (migration ServicesAndNextSeason): per i servizi orari l'opzione senza date che portava orari e massimo per fascia è stata tolta: quei dati stanno nella disponibilità ([`catalog_service_hourly_options`](catalog_service_hourly_options.md)). Un'opzione con date di un servizio orario può avere la sua disponibilità (`service_hourly_options.booking_option_id`).
