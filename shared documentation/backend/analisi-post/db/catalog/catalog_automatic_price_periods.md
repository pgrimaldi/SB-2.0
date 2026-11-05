# Tabella `catalog_automatic_price_periods`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_automatic_price_periods` · **forma canonica**: `catalog.automatic_price_periods`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Periodi di validità di un prezzo automatico, ciascuno con i suoi giorni e le sue fasce orarie. Dal 2026-11-05 giorni e fasce orarie stanno nel periodo, non nel prezzo.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### automatic_price_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Prezzo automatico.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_automatic_prices.id`](catalog_automatic_prices.md) `ON DELETE CASCADE`.

### starts_on
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Nota**: facoltativo dal 2026-11-05 (migration ServicesAndNextSeason).
- **Descrizione**: Primo giorno; NULL = dall'inizio della stagione.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### ends_on
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Nota**: facoltativo dal 2026-11-05 (migration ServicesAndNextSeason).
- **Descrizione**: Ultimo giorno; NULL = fino alla fine della stagione.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### weekdays
- **Legacy**: nessuno (campo nuovo).
- **Nota**: campo aggiunto il 2026-11-05 (migration ServicesAndNextSeason).
- **Descrizione**: Giorni di validità nel periodo; vuoto = tutti.
- **Tipo**: `catalog.weekday[]`.
- **Default**: `'{}'::catalog.weekday[]`.
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
