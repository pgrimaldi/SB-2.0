# Righe non migrate — `catalog.map_sectors_from_price_lists`

> Modulo **catalog** · sorgente legacy **`price_lists`** · **8 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_map_sectors_from_price_lists.jsonl`](dati/catalog_map_sectors_from_price_lists.jsonl).

## Diagnosi

### Esclusa da una regola — 8 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
listino senza stabilimento: nessun settore da creare
```

**Cosa significa** — Listini senza stabilimento: non c'e' alcun settore da creare.

**Causa a monte** — Il passo che crea i settori dai nomi citati nei listini salta i listini che la spec padre esclude, per lo stesso motivo per cui li esclude lei: senza stabilimento non esiste la riga a cui attaccare il settore.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna.

## Righe

| id legacy | motivo | `id` | `mainPrices` |
|---|---|---|---|
| `271` | Esclusa da una regola | 271 | [{"name":"Fila 1-2","value":"13 E"},{"name":"Fila 3-4-5","v… |
| `272` | Esclusa da una regola | 272 | [{"name":"Fila 1-2","value":"16 €"},{"name":"Fila 3-4-5","v… |
| `273` | Esclusa da una regola | 273 | [{"name":"Fila 1-2","value":"18 €"},{"name":"Fila 3-4-5","v… |
| `274` | Esclusa da una regola | 274 | [{"name":"Fila 1-2","value":"320 €"},{"name":"Fila 3-4-5","… |
| `275` | Esclusa da una regola | 275 | [{"name":"Fila 1-2","value":"340 €"},{"name":"Fila 3-4-5","… |
| `276` | Esclusa da una regola | 276 | [{"name":"Fila 3-4-5","value":"600 €"},{"name":"Fila 1-2","… |
| `277` | Esclusa da una regola | 277 | [{"name":"Fila 1-2","value":"700 €"},{"name":"Fila 3-4-5","… |
| `278` | Esclusa da una regola | 278 | [{"name":"Fila 1-2","value":"13"},{"name":"Fila 3-4-5","val… |
