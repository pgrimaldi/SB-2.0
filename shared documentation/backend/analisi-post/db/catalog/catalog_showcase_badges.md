# Tabella `catalog_showcase_badges`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_showcase_badges` · **forma canonica**: `catalog.showcase_badges`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Badge della vetrina creati dal systemadmin.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### public_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Id pubblico (UUIDv7) usato dalle API.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### name
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Nome in italiano.
- **Tipo**: `character varying(80)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### color
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Colore esadecimale #RRGGBB.
- **Tipo**: `character varying(7)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### sort_order
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Ordine di visualizzazione.
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
