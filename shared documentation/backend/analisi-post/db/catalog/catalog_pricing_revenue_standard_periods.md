# Tabella `catalog_pricing_revenue_standard_periods`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_pricing_revenue_standard_periods` · **forma canonica**: `catalog.pricing_revenue_standard_periods`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Periodi standard del prezzo suggerito (systemadmin), uguali per tutti e ripetuti ogni anno; fuori dai periodi vale la bassa stagione.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### name
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Nome del periodo.
- **Tipo**: `character varying(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### start_month
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Mese di inizio.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### start_day
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Giorno di inizio.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### end_month
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Mese di fine.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### end_day
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Giorno di fine (può essere prima dell'inizio: il periodo scavalca il capodanno).
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### tier
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Bassa o alta stagione.
- **Tipo**: `catalog.revenue_season_tier`.
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
