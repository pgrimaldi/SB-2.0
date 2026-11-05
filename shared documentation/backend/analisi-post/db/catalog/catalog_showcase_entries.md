# Tabella `catalog_showcase_entries`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_showcase_entries` · **forma canonica**: `catalog.showcase_entries`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Strutture nelle sezioni della vetrina («In evidenza», «Più frequentati»), scelte e ordinate dal systemadmin.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `bigint`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### section
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Sezione della vetrina.
- **Tipo**: `catalog.showcase_section`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### bathhouse_id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Struttura.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_bathhouses.id`](catalog_bathhouses.md) `ON DELETE CASCADE`.

### sort_order
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Posizione nella sezione.
- **Tipo**: `smallint`.
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
