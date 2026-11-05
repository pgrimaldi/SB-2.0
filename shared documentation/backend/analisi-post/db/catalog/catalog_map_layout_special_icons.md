# Tabella `catalog_map_layout_special_icons`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_map_layout_special_icons` · **forma canonica**: `catalog.map_layout_special_icons`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Icone degli articoli personalizzati (1-8) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### layout_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Layout a cui appartiene la variante.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_map_layouts.id`](catalog_map_layouts.md) `ON DELETE CASCADE`.

### variant
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Numero dell'articolo personalizzato (1-8, i primi 5 corrispondono a SpecialUmbrella1..5 del legacy).
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### state
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Stato rappresentato dall'icona (occupied/free).
- **Tipo**: `catalog.map_icon_state`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### media_asset_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Icona della variante (catalog.media_assets).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) `ON DELETE RESTRICT`.

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
- **Descrizione**: Soft-delete della variante.
- **Tipo**: `timestamp with time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —
