# Tabella `catalog_table_service_slots`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_table_service_slots` · **forma canonica**: `catalog.table_service_slots`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Fasce orarie prenotabili di un periodo del servizio tavoli; senza giorno valgono per tutti i giorni.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### availability_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Periodo di disponibilità.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_table_service_availabilities.id`](catalog_table_service_availabilities.md) `ON DELETE CASCADE`.

### weekday
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Giorno; NULL = tutti i giorni del periodo.
- **Tipo**: `catalog.weekday`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### time_from
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Inizio della fascia.
- **Tipo**: `time without time zone`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### time_to
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Fine della fascia.
- **Tipo**: `time without time zone`.
- **Default**: nessuno.
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
