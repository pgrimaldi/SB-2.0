# Righe non migrate — `identity.customer_customers`

> Modulo **identity** · sorgente legacy **`customers`** · **2 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/identity_customer_customers.jsonl`](dati/identity_customer_customers.jsonl).

## Diagnosi

### Esclusa da una regola — 2 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
not propertyId
```

**Cosa significa** — Anagrafica cliente senza stabilimento di appartenenza.

**Causa a monte** — Nel nuovo schema l'anagrafica cliente e' **per stabilimento** (`property_id` `NOT NULL`): lo stesso cliente fisico che frequenta due stabilimenti ha due righe. Un cliente senza stabilimento non e' collocabile.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna, a questo volume. Sono 2 righe su 224.526.

## Righe

| id legacy | motivo | `id` | `fullName` | `mobile` | `email` | `createdBy` | `createdAt` |
|---|---|---|---|---|---|---|---|
| `534` | Esclusa da una regola | 534 | hitesh | 9999999999 | thesolacetesting@yopmail.com | 0 | 2021-05-11T08:35:36 |
| `4316` | Esclusa da una regola | 4316 | SIVORI | 3303030300 | pamelamacrina@gmail.com | 269 | 2022-05-22T12:40:54 |
