# Righe non migrate — `catalog.pricing_discounts`

> Modulo **catalog** · sorgente legacy **`discounts`** · **3 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_pricing_discounts.jsonl`](dati/catalog_pricing_discounts.jsonl).

## Diagnosi

### Esclusa da una regola — 3 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
voucher duplicato per lo stesso stabilimento: vince l'ultimo
```

**Cosa significa** — Sconto senza nome.

**Causa a monte** — `name` e' `NOT NULL` a destinazione, ed e' anche la chiave con cui gli sconti vengono correlati per nome dalle tabelle figlie.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. Ma vedi la nota sui 39 nomi di sconto non risolti nel log: uno sconto senza nome e uno sconto il cui nome nessuno trova sono due problemi diversi.

## Righe

| id legacy | motivo | `id` | `items` | `createdBy` | `propertyId` | `createdAt` | `updatedAt` |
|---|---|---|---|---|---|---|---|
| `3` | Esclusa da una regola | 3 | "[{\"name\":\"Junebeach\",\"start\":\"2021-06-18T15:23:06.0… | 85 | 32 | 2021-04-17T15:23:02 | 2025-01-28T04:56:19 |
| `4` | Esclusa da una regola | 4 | "[{\"name\":\"hotel\",\"type\":\"percentage\",\"value\":\"2… | 85 | 85 | 2021-04-20T15:01:06 | 2025-01-28T04:56:19 |
| `33` | Esclusa da una regola | 33 | "[{\"name\":\"TOP5\",\"type\":\"percentage\",\"voucherType\… | 993 | 10553 | 2024-04-04T22:44:32 | 2025-01-28T04:56:19 |
