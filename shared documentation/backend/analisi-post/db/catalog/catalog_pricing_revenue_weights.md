# Tabella `catalog_pricing_revenue_weights`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_pricing_revenue_weights` · **forma canonica**: `catalog.pricing_revenue_weights`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Pesi del prezzo suggerito (systemadmin), uguali per tutti gli stabilimenti: riga unica id = 1.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Sempre 1.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### rain_low_season_factor
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Moltiplicatore con pioggia in bassa stagione.
- **Tipo**: `numeric(5,3)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### rain_high_season_factor
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Moltiplicatore con pioggia in alta stagione.
- **Tipo**: `numeric(5,3)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### before_weekend_factor
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Moltiplicatore del giorno prima del weekend.
- **Tipo**: `numeric(5,3)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### high_season_factor
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Moltiplicatore di alta stagione (se non è il giorno prima del weekend).
- **Tipo**: `numeric(5,3)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### free_spots_threshold_percent
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Soglia dei posti liberi in percentuale.
- **Tipo**: `numeric(5,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### free_spots_above_factor
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Moltiplicatore con posti liberi dalla soglia in su.
- **Tipo**: `numeric(5,3)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### free_spots_below_factor
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Moltiplicatore con posti liberi sotto la soglia.
- **Tipo**: `numeric(5,3)`.
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
