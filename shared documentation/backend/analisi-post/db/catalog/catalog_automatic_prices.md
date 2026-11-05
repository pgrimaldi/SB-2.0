# Tabella `catalog_automatic_prices`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_automatic_prices` · **forma canonica**: `catalog.automatic_prices`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Prezzi automatici (es. Coperto): aggiunti da soli agli ordini dei servizi indicati nei periodi, giorni e fasce di validità.

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

### property_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Stabilimento.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.

### name
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Nome mostrato al cliente.
- **Tipo**: `character varying(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### description
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Descrizione.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### price
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Prezzo aggiunto.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### vat_rate
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Aliquota IVA in percentuale; NULL = esente (vat_nature).
- **Tipo**: `numeric(5,2)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### vat_nature
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Natura dell'esenzione IVA.
- **Tipo**: `catalog.vat_nature`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### icon_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Icona del catalogo (service_type_icons).
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_service_type_icons.id`](catalog_service_type_icons.md) `ON DELETE SET NULL`.

### icon_asset_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Immagine caricata dal gestore.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) `ON DELETE RESTRICT`.

### is_purchasable
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Vale anche per gli ordini online.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —

### applies_per_service
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: true = per ogni servizio dell'ordine; false = una volta per ordine.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —

### applies_to_all_services
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: true = tutti i servizi; false = solo quelli di automatic_price_services.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —

### is_sector_restricted
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Vale solo nelle file/settori di automatic_price_sectors.
- **Tipo**: `boolean`.
- **Default**: `false`.
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

> **Nota 2026-11-05** (migration ServicesAndNextSeason): tolta la colonna `weekdays`. Giorni e fasce orarie stanno ora in ogni periodo di validità ([`catalog_automatic_price_periods`](catalog_automatic_price_periods.md), [`catalog_automatic_price_time_ranges`](catalog_automatic_price_time_ranges.md)).
