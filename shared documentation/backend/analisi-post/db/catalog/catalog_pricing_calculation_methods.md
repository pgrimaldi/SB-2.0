# Tabella `catalog_pricing_calculation_methods`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_pricing_calculation_methods` · **forma canonica**: `catalog.pricing_calculation_methods`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Lookup dei metodi di calcolo del prezzo multi-giorno di una voce di listino.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `smallint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### code
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Codice canonico del metodo (proportional, daily, daily_with_weekend, logarithmic).
- **Tipo**: `character varying(40)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### is_active
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Metodo selezionabile nell'editor prezzi.
- **Tipo**: `boolean`.
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
- **Descrizione**: —
- **Tipo**: `timestamp with time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —
