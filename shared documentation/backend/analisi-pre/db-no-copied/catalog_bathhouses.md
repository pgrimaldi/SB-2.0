# Righe non migrate — `catalog.bathhouses`

> Modulo **catalog** · sorgente legacy **`bathouses`** · **1 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_bathhouses.jsonl`](dati/catalog_bathhouses.jsonl).

## Diagnosi

### Esclusa da una regola — 1 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
not bathouseName or not propertyId
```

**Cosa significa** — Vetrina pubblica senza nome oppure senza stabilimento.

**Causa a monte** — Entrambi i campi sono obbligatori a destinazione: una vetrina senza nome non e' pubblicabile, una senza stabilimento non e' collegabile.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna.

## Righe

| id legacy | motivo | `id` | `bathouseName` | `address` | `city` | `cap` | `province` |
|---|---|---|---|---|---|---|---|
| `112` | Esclusa da una regola | 112 | Nashik godavari damp | 301, F-10, Mumbai Hwy, Nashik | Banglore | 124578 | Maharashtra |
