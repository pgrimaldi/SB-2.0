# Tabella `catalog_table_service_translations`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_table_service_translations` · **forma canonica**: `catalog.table_service_translations`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Nome e descrizione di un servizio tavoli nelle altre lingue.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### table_service_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Riferimento a un servizio tavoli.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_table_services.id`](catalog_table_services.md) `ON DELETE CASCADE`.

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
- **Tipo**: `character varying(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### description
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Descrizione tradotta.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: Sì.
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
