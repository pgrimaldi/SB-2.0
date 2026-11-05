# Tabella `catalog_showcase_regions`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_showcase_regions` · **forma canonica**: `catalog.showcase_regions`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Regioni della vetrina gestite dal systemadmin; assegnate alle strutture dalla regione dell'indirizzo, modificabili a mano.

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

### country_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Paese della regione.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_countries.id`](catalog_countries.md) `ON DELETE RESTRICT`.

### name
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Nome in italiano.
- **Tipo**: `character varying(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
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
