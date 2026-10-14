# Righe non migrate — `payment.request_links`

> Modulo **payment** · sorgente legacy **`bookings_send_payment_summaries`** · **2 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/payment_request_links.jsonl`](dati/payment_request_links.jsonl).

## Diagnosi

### Esclusa da una regola — 2 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
not booking_id
```

**Cosa significa** — Link di pagamento che non si riesce ad attribuire a uno stabilimento.

**Causa a monte** — `property_id` `NULL`, e la catena di ripiego non ha nulla su cui lavorare: lo stabilimento si ricava dalla prenotazione, ma in entrambe le righe (legacy 728 e 729) `booking_id` e' **`NULL`** e `stripe_session_id` e' la **stringa vuota**. Non sono link rimasti orfani di una prenotazione cancellata: sono link che **non hanno mai avuto** ne' una prenotazione ne' una sessione di pagamento. Create il 2026-05-27 a due secondi di distanza, 35,00 EUR ciascuna, `email_sent = 1`.

**Verdetto: ❓ Da decidere** — il meccanismo e' corretto, ma serve una scelta di business sul dato

**Azione** — 2 righe, e vanno lasciate fuori: senza prenotazione e senza sessione Stripe non esiste alcuna via per attribuirle a uno stabilimento, e un link di pagamento non attribuibile non ha nulla da incassare. La correzione utile non e' recuperarle ma **dichiararle**: la spec dovrebbe portare `exclude_row_if: "not booking_id"`, cosi' diventano uno scarto previsto invece di un errore di inserimento, e smettono di sporcare il conteggio degli errori veri. Da guardare a parte, come segnale: due richieste di pagamento generate a due secondi di distanza senza prenotazione suggeriscono un doppio invio dall'interfaccia, quindi vale la pena chiedersi se il meccanismo che le ha prodotte sia ancora attivo.

## Righe

| id legacy | motivo | `id` | `amount` | `payment_status` | `cancel_payment_flag` | `booking_cancel_flag` | `method_flag` |
|---|---|---|---|---|---|---|---|
| `728` | Esclusa da una regola | 728 | 35 | unpaid | 0 | 0 | 2 |
| `729` | Esclusa da una regola | 729 | 35 | unpaid | 0 | 0 | 2 |
