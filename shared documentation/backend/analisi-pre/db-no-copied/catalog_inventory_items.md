# Righe non migrate — `catalog.inventory_items`

> Modulo **catalog** · sorgente legacy **`warehouses`** · **2 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_inventory_items.jsonl`](dati/catalog_inventory_items.jsonl).

## Diagnosi

### Esclusa da una regola — 2 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
quantita' assente, non numerica o fuori range
```
*(1 righe su 2)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
articolo duplicato nello stesso magazzino: vince la prima occorrenza
```
*(1 righe su 2)*

**Cosa significa** — Articolo di magazzino senza denominazione in nessuna delle due lingue.

**Causa a monte** — Un articolo senza nome non e' selezionabile in alcuna interfaccia.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna.

## Righe

| id legacy | motivo | `id` | `items` | `createdBy` | `propertyId` | `createdAt` | `updatedAt` |
|---|---|---|---|---|---|---|---|
| `27` | Esclusa da una regola | 27 | [{"name":"Umbrella","it":"Ombrellone","quantity":"1000","th… | 199 | 96 | 2021-10-12T21:35:16 | 2026-08-18T13:04:44 |
| `27` | Esclusa da una regola | 27 | [{"name":"Umbrella","it":"Ombrellone","quantity":"1000","th… | 199 | 96 | 2021-10-12T21:35:16 | 2026-08-18T13:04:44 |
