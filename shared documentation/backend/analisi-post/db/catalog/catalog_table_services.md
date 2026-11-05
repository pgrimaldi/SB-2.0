# Tabella `catalog_table_services`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_table_services` · **forma canonica**: `catalog.table_services`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Servizio tavoli (pranzo, cena): disponibilità, capienza per tavoli o posti, durata del pasto, limiti e caparra.

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
- **Descrizione**: Nome del servizio (es. Pranzo).
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

### is_online_bookable
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Prenotabile online.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —

### is_highlighted
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Proposto al cliente insieme alla prenotazione dell'ombrellone.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —

### capacity_mode
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Disponibilita per numero di tavoli o per posti totali.
- **Tipo**: `catalog.table_capacity_mode`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### total_seats
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Posti totali, con capacity_mode = seats.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### average_meal_minutes
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Durata media del pasto in minuti: libera il tavolo per la fascia successiva.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### max_bookings_per_slot
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Massimo di prenotazioni per fascia oraria.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### min_seats_per_booking
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Minimo di posti prenotabili in una volta.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### max_seats_per_booking
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Massimo di posti prenotabili in una volta.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### deposit_mode
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Caparra: nessuna, totale o a persona.
- **Tipo**: `catalog.deposit_mode`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### deposit_type
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Caparra in percentuale o importo.
- **Tipo**: `catalog.deposit_type`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —

### deposit_value
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Percentuale o importo della caparra.
- **Tipo**: `numeric(12,2)`.
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
