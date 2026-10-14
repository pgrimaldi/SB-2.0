# Righe non migrate — `payment.ledger_entries`

> Modulo **booking** · sorgente legacy **`reservation_public_id_by_bookings_id`** · **1 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/payment_ledger_entries.jsonl`](dati/payment_ledger_entries.jsonl).

## Diagnosi

### Esclusa da una regola — 1 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#328410 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```

> **Diagnosi non disponibile** per questa combinazione: e' comparsa dopo l'ultima revisione di `app/diagnosi_scarti.py`. Il messaggio qui sopra e' il dato grezzo; il verdetto va stabilito a mano.

## Righe

| id legacy | motivo | `target_row_id` | `ref_legacy_id` |
|---|---|---|---|
| `328410` | Esclusa da una regola | 2621 | 328410 |
