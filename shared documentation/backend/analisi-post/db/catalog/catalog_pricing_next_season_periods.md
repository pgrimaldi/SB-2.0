# Tabella `catalog_pricing_next_season_periods`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_pricing_next_season_periods` · **forma canonica**: `catalog.pricing_next_season_periods`.
>
> Tabella nuova, aggiunta il 2026-11-05 (migration ServicesAndNextSeason) per allineare le API al frontend (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Periodi tariffari della stagione successiva, preparati in anticipo: una riga per intervallo. Separati da pricing_tariff_periods perché listini, preventivi e ricerca usano solo la stagione corrente.

Una riga per intervallo, come [`catalog_pricing_tariff_periods`](catalog_pricing_tariff_periods.md): gli intervalli dello stesso periodo hanno la stessa `period_group_key`. Sta in una tabella a parte perché listini, convenzioni, servizi, preventivi e ricerca usano solo la stagione corrente. Si svuota togliendo la stagione successiva. Il passaggio alla stagione corrente non è ancora deciso (PROMEMORIA-85-19).

**Migrazione**: nessuna, il legacy non aveva periodi della stagione successiva.

## Campi

### id
- **Legacy**: nessuno.
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### season_id
- **Legacy**: nessuno.
- **Descrizione**: Stagione (pricing_seasons) di cui è la stagione successiva.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_seasons.id`](catalog_pricing_seasons.md) `ON DELETE CASCADE`.

### name
- **Legacy**: nessuno.
- **Descrizione**: Nome del periodo tariffario (es. Alta stagione).
- **Tipo**: `character varying(80)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### period_group_key
- **Legacy**: nessuno.
- **Descrizione**: Chiave che raggruppa gli intervalli dello stesso periodo nella stagione successiva.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### starts_on
- **Legacy**: nessuno.
- **Descrizione**: Inizio dell'intervallo.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### ends_on
- **Legacy**: nessuno.
- **Descrizione**: Fine dell'intervallo.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### created_at
- **Legacy**: nessuno.
- **Descrizione**: Creazione della riga.
- **Tipo**: `timestamp with time zone`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —

### updated_at
- **Legacy**: nessuno.
- **Descrizione**: Ultima modifica della riga.
- **Tipo**: `timestamp with time zone`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —

### deleted_at
- **Legacy**: nessuno.
- **Descrizione**: Soft-delete.
- **Tipo**: `timestamp with time zone`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —
