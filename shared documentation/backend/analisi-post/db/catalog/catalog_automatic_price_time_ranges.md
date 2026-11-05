# Tabella `catalog_automatic_price_time_ranges`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_automatic_price_time_ranges` · **forma canonica**: `catalog.automatic_price_time_ranges`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Fasce orarie di un periodo di validità di un prezzo automatico. Fino al 2026-11-05 (migration ServicesAndNextSeason) la fascia apparteneva al prezzo automatico (`automatic_price_id`), per tutti i periodi.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### period_id
- **Legacy**: nessuno (campo nuovo).
- **Nota**: campo aggiunto il 2026-11-05 (migration ServicesAndNextSeason).
- **Descrizione**: Periodo di validità (automatic_price_periods).
- **Tipo**: `bigint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_automatic_price_periods.id`](catalog_automatic_price_periods.md) `ON DELETE CASCADE`.

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
