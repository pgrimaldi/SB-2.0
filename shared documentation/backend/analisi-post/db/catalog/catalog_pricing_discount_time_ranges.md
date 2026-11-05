# Tabella `catalog_pricing_discount_time_ranges`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_pricing_discount_time_ranges` · **forma canonica**: `catalog.pricing_discount_time_ranges`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Fasce orarie di validità di uno sconto.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### discount_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Sconto.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_discounts.id`](catalog_pricing_discounts.md) `ON DELETE CASCADE`.

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
