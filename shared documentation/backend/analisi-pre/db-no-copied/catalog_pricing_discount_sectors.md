# Righe non migrate — `catalog.pricing_discount_sectors`

> Modulo **catalog** · sorgente legacy **`discounts`** · **10 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_pricing_discount_sectors.jsonl`](dati/catalog_pricing_discount_sectors.jsonl).

## Diagnosi

### Esclusa da una regola — 10 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
nome di settore di un solo carattere: dato sporco del legacy, il nome era stato salvato lettera per lettera
```
*(6 righe su 10)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
nome di settore non risolvibile per questo stabilimento: i settori si creano dai listini, non dagli sconti
```
*(4 righe su 10)*

**Cosa significa** — Settori citati da uno sconto che non esistono per quello stabilimento.

**Causa a monte** — I settori mancanti si creano dai **listini** (decisione del committente del 2026-09-30), non dagli sconti: un nome che compare solo in un vecchio sconto non e' un settore dello stabilimento, e crearlo inventerebbe una zona di spiaggia. Che non siano nomi veri si vede dallo sconto id=17 dello stabilimento 98, il cui `selectedRow` contiene alla lettera `["F","i","l","a"," ","1","Fila 2"]`: `Fila 2` si risolve, le sei lettere no - era `Fila 1` salvato carattere per carattere da un baco dell'interfaccia legacy.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. Prima erano gli ultimi 10 ERRORI della migrazione, perche' la riga arrivava al database e veniva rifiutata su `sector_id NOT NULL`; ora sono scarti dichiarati, che e' quello che sono.

## Righe

| id legacy | motivo | `id` | `items` | `createdBy` | `propertyId` | `createdAt` | `updatedAt` |
|---|---|---|---|---|---|---|---|
| `17` | Esclusa da una regola | 17 | "[{\"name\":\"Free5\",\"type\":\"fixed\",\"value\":\"5\",\"… | 200 | 98 | 2021-10-26T09:43:54 | 2025-01-28T04:56:19 |
| `17` | Esclusa da una regola | 17 | "[{\"name\":\"Free5\",\"type\":\"fixed\",\"value\":\"5\",\"… | 200 | 98 | 2021-10-26T09:43:54 | 2025-01-28T04:56:19 |
| `17` | Esclusa da una regola | 17 | "[{\"name\":\"Free5\",\"type\":\"fixed\",\"value\":\"5\",\"… | 200 | 98 | 2021-10-26T09:43:54 | 2025-01-28T04:56:19 |
| `17` | Esclusa da una regola | 17 | "[{\"name\":\"Free5\",\"type\":\"fixed\",\"value\":\"5\",\"… | 200 | 98 | 2021-10-26T09:43:54 | 2025-01-28T04:56:19 |
| `17` | Esclusa da una regola | 17 | "[{\"name\":\"Free5\",\"type\":\"fixed\",\"value\":\"5\",\"… | 200 | 98 | 2021-10-26T09:43:54 | 2025-01-28T04:56:19 |
| `17` | Esclusa da una regola | 17 | "[{\"name\":\"Free5\",\"type\":\"fixed\",\"value\":\"5\",\"… | 200 | 98 | 2021-10-26T09:43:54 | 2025-01-28T04:56:19 |
| `43` | Esclusa da una regola | 43 | "[{\"name\":\"CONVENZIONE\",\"type\":\"fixed\",\"voucherTyp… | 2888 | 10762 | 2026-03-08T11:07:44 | 2026-03-08T11:07:44 |
| `43` | Esclusa da una regola | 43 | "[{\"name\":\"CONVENZIONE\",\"type\":\"fixed\",\"voucherTyp… | 2888 | 10762 | 2026-03-08T11:07:44 | 2026-03-08T11:07:44 |
| `43` | Esclusa da una regola | 43 | "[{\"name\":\"CONVENZIONE\",\"type\":\"fixed\",\"voucherTyp… | 2888 | 10762 | 2026-03-08T11:07:44 | 2026-03-08T11:07:44 |
| `43` | Esclusa da una regola | 43 | "[{\"name\":\"CONVENZIONE\",\"type\":\"fixed\",\"voucherTyp… | 2888 | 10762 | 2026-03-08T11:07:44 | 2026-03-08T11:07:44 |
