# Righe non migrate — `catalog.map_layouts`

> Modulo **catalog** · sorgente legacy **`configure_maps`** · **1 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_map_layouts.jsonl`](dati/catalog_map_layouts.jsonl).

## Diagnosi

### Esclusa da una regola — 1 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
not propertyId
```

**Cosa significa** — Mappa spiaggia non collegata ad alcuno stabilimento.

**Causa a monte** — Una mappa senza stabilimento non e' raggiungibile da nessun flusso: `property_id` e' `NOT NULL` a destinazione.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna.

## Righe

| id legacy | motivo | `id` | `templateImg` | `isCustomTemplate` | `rows` | `items` | `umbrellaImg` |
|---|---|---|---|---|---|---|---|
| `48` | Esclusa da una regola | 48 | https://d26kxhigi9o4u2.cloudfront.net/images/configure/2yja… | 0 | 1 | 10 | https://d26kxhigi9o4u2.cloudfront.net/images/configure/2yja… |
