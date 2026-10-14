# Righe non migrate — `booking.service_lines`

> Modulo **booking** · sorgente legacy **`bookings`** · **3 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/booking_service_lines.jsonl`](dati/booking_service_lines.jsonl).

## Diagnosi

### Esclusa da una regola — 3 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
prenotazione non migrata: la riga di servizio non ha a cosa attaccarsi
```

**Cosa significa** — Righe di servizio di prenotazioni o stabilimenti non migrati.

**Causa a monte** — Una riga di servizio appesa a una prenotazione che non esiste, o a uno stabilimento che non esiste, non e' inseribile: entrambi i riferimenti sono obbligatori a destinazione.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna di per se'. Questa voce esiste perche' prima quegli scarti **non si vedevano**: la spec usa un `aggregate_step`, che non passa dai punti in cui l'orchestrator registra, e una corsa intermedia ha perso 10.611 righe senza lasciare traccia nel registro. Vedi la voce B2 di _mancanti-da-sistemare.md: la copertura e' ancora parziale.

## Righe

| id legacy | motivo | `id` | `property_id` | `bookingNumber` | `servicesData` | `bookingByAdmin` | `isOrderFilled` |
|---|---|---|---|---|---|---|---|
| `71724` | Esclusa da una regola | 71724 | 10560 | -1 | {"barServices":[],"normalServices":[],"houlryServices":{"bo… | 1 | 0 |
| `71725` | Esclusa da una regola | 71725 | 10560 | -1 | {"barServices":[],"normalServices":[],"houlryServices":{"bo… | 1 | 0 |
| `284655` | Esclusa da una regola | 284655 | 96 | 0 | {"barServices":[],"normalServices":[],"houlryServices":{"bo… | 1 | 0 |
