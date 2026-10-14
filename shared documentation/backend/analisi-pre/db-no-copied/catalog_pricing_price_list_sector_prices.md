# Righe non migrate — `catalog.pricing_price_list_sector_prices`

> Modulo **catalog** · sorgente legacy **`price_lists`** · **9 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_pricing_price_list_sector_prices.jsonl`](dati/catalog_pricing_price_list_sector_prices.jsonl).

## Diagnosi

### Esclusa da una regola — 9 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
id == 0 or not propertyId
```

**Cosa significa** — Prezzi di listini che la spec padre esclude (senza stabilimento, o id 0).

**Causa a monte** — La spec figlia ora **eredita** l'`exclude_row_if` del padre (`id == 0 or not propertyId`). Prima non lo faceva: provava a inserire i prezzi di listini che a destinazione non esistono, e il database li rifiutava sulla chiave esterna - 24 righe contate fra gli ERRORI invece che fra gli scarti previsti.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. Quelle righe vanno scartate in ogni caso: un prezzo che punta a un listino inesistente non e' recuperabile. Il guadagno non e' nei dati ma nel conteggio, che ora distingue uno scarto previsto da un errore vero.

## Righe

| id legacy | motivo | `id` | `type` | `details` | `mainPrices` | `detailPrices` | `createdBy` |
|---|---|---|---|---|---|---|---|
| `0` | Esclusa da una regola | 0 | additional | [] | [] | [] | 76 |
| `271` | Esclusa da una regola | 271 | package | {"PackageName":"Giornaliero GIUGNO","AdditionalItem":"1","P… | [{"name":"Fila 1-2","value":"13 E"},{"name":"Fila 3-4-5","v… | [] | 269 |
| `272` | Esclusa da una regola | 272 | package | {"PackageName":"Giornaliero Luglio S+L","AdditionalItem":"1… | [{"name":"Fila 1-2","value":"16 €"},{"name":"Fila 3-4-5","v… | [] | 269 |
| `273` | Esclusa da una regola | 273 | package | {"PackageName":"Giornaliero Agosto","AdditionalItem":"1","P… | [{"name":"Fila 1-2","value":"18 €"},{"name":"Fila 3-4-5","v… | [] | 269 |
| `274` | Esclusa da una regola | 274 | package | {"PackageName":"LUGLIO","AdditionalItem":"1","PackageItemSe… | [{"name":"Fila 1-2","value":"320 €"},{"name":"Fila 3-4-5","… | [] | 269 |
| `275` | Esclusa da una regola | 275 | package | {"PackageName":"Agosto","AdditionalItem":"1","PackageItemSe… | [{"name":"Fila 1-2","value":"340 €"},{"name":"Fila 3-4-5","… | [] | 269 |
| `276` | Esclusa da una regola | 276 | package | {"PackageName":"Luglio + Agosto","AdditionalItem":"1","Pack… | [{"name":"Fila 3-4-5","value":"600 €"},{"name":"Fila 1-2","… | [] | 269 |
| `277` | Esclusa da una regola | 277 | package | {"PackageName":"Stagione Completa","AdditionalItem":"1","Pa… | [{"name":"Fila 1-2","value":"700 €"},{"name":"Fila 3-4-5","… | [] | 269 |
| `278` | Esclusa da una regola | 278 | package | {"PackageName":"GIORNALIERO GIUGNO S+L","AdditionalItem":"1… | [{"name":"Fila 1-2","value":"13"},{"name":"Fila 3-4-5","val… | [] | 269 |
