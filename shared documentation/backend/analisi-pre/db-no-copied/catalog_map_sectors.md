# Righe non migrate — `catalog.map_sectors`

> Modulo **catalog** · sorgente legacy **`sectors_rows`** · **1 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_map_sectors.jsonl`](dati/catalog_map_sectors.jsonl).

## Diagnosi

### Esclusa da una regola — 1 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
not propertyId
```

**Cosa significa** — Settore non collegato ad alcuno stabilimento.

**Causa a monte** — Come sopra.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. Da notare che questo settore orfano **non** e' fra le cause dei 287 `sector_id` NULL di `pricing_price_list_sector_prices`: quelli cercano settori per nome e non li trovano affatto.

## Righe

| id legacy | motivo | `id` | `sectors` | `createdBy` | `createdAt` | `updatedAt` |
|---|---|---|---|---|---|---|
| `45` | Esclusa da una regola | 45 | [{"name":"Fila 1-2","currentId":25.501290550023324},{"name"… | 269 | 2022-05-12T13:18:09 | 2022-05-22T12:18:34 |
