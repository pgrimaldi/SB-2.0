# Tabella `catalog_country_vat_rates`

> **Dominio**: C2 — `catalog` · **schema**: `catalog` · **nome flat**: `catalog_country_vat_rates` · **forma canonica**: `catalog.country_vat_rates`.
>
> Tabella nuova (nessuna legacy diretta), aggiunta nel 2026-10 con le API Catalog (catalogo `progettazione/89-api-catalog.md`).

---

## Descrizione

Aliquote IVA selezionabili per Paese dello stabilimento (pacchetti, articoli aggiuntivi, servizi); lookup di piattaforma caricata con le migration.

## Campi

### id
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: —
- **Tipo**: `integer`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: —

### country_code
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Codice ISO del Paese (catalog.countries.code), senza FK: le aliquote arrivano con le migration, i paesi con l'ETL.
- **Tipo**: `character(2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### rate
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Aliquota in percentuale (22.00 = 22%).
- **Tipo**: `numeric(5,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —

### sort_order
- **Legacy**: — (aggiunta nel 2026-10 con le API Catalog)
- **Descrizione**: Ordine nella tendina: la prima è l'aliquota proposta.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —
