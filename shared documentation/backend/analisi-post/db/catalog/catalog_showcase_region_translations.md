# Tabella `catalog_showcase_region_translations`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_showcase_region_translations` · **forma canonica**: `catalog.showcase_region_translations`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Nome delle regioni della vetrina nelle altre lingue.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### region_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Regione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_showcase_regions.id`](catalog_showcase_regions.md) `ON DELETE CASCADE`.

### language
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Lingua della traduzione.
- **Tipo**: `catalog.content_language`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### name
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Nome tradotto.
- **Tipo**: `character varying(120)`.
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
