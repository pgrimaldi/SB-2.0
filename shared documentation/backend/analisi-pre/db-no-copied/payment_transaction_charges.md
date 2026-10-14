# Righe non migrate — `payment.transaction_charges`

> Modulo **payment** · sorgente legacy **`online_transaction_details`** · **3487 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/payment_transaction_charges.jsonl`](dati/payment_transaction_charges.jsonl).

## Diagnosi

### Esclusa da una regola — 3487 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
nessuno stabilimento ricavabile: propertyId assente e nessuno fra booking_id, reservation_id e stripe_connected_id porta a uno stabilimento
```
*(1547 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
duplicato Stripe: stesso stripe_transaction_id di una riga gia' migrata (doppio webhook), si tiene la prima per id
```
*(93 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#2976 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#2982 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#2985 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#3006 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#3042 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#3117 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#4469 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#4968 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#4969 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#4970 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#5039 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#8283 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#8776 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#8790 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#8793 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#8794 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#8801 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#8830 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#8833 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#8841 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#9065 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#9071 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#9794 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#12023 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#23762 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#24373 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25046 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25149 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25278 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25303 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25329 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25507 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25536 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25541 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25549 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25555 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25568 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25634 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25691 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25697 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25723 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25730 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25731 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25740 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25756 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25820 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25838 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25844 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25846 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25857 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25860 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#25869 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#26145 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#26185 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#26569 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#26759 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#26775 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#26795 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#26830 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#26964 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27026 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27052 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27098 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27110 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27129 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27159 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27172 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27179 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27191 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27193 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27237 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27239 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27245 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27248 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27249 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27251 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27397 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27439 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27475 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27494 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27531 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27534 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27550 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27559 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27575 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27573 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27609 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27614 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27637 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27642 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27670 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27672 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27675 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27682 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27684 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27685 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27689 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27690 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27693 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27695 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27696 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27699 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#27786 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28018 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28231 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28478 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28483 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28513 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28587 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28589 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28612 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28668 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28687 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28734 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28736 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28913 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28925 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28926 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28928 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28930 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28949 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28951 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#28956 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29015 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29074 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29144 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29145 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29154 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29226 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29364 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29377 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29396 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29408 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29467 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29516 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29522 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29555 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29584 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29588 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29591 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29592 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29609 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29613 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29616 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29631 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29648 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29736 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29802 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29840 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29845 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#29991 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30240 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30265 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30358 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30391 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30509 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30512 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30530 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30531 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30549 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30584 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30591 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30661 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30665 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30737 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30739 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30748 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30764 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30771 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30773 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30780 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30782 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30783 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30826 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30854 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30895 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30951 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#30979 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31023 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31035 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31043 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31068 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31079 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31081 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31094 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31144 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31240 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31243 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31245 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31267 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31297 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31335 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31346 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31353 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31359 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31362 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31371 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31383 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31387 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31394 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31395 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31397 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31398 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31399 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31403 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31405 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31415 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31422 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31423 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31438 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31450 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31449 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31478 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31486 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31489 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31500 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31523 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31557 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31591 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31607 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31668 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31676 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31683 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31734 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31768 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31785 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31799 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31815 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31828 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31839 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31862 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31887 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31890 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31908 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31931 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31956 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31957 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31974 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31979 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31980 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#31996 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32000 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32003 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32006 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32005 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32039 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32049 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32059 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32058 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32066 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32067 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32074 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32091 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32092 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32093 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32094 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32096 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32108 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32110 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32111 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32113 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32114 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32121 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32125 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32123 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32126 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32128 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32129 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32136 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32139 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32142 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32178 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32180 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32197 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32203 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32226 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32282 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32298 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32473 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32553 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32622 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32691 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32798 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32886 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#32919 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33063 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33067 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33079 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33092 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33136 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33166 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33170 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33338 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33366 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33380 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33409 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33422 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33423 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33433 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33487 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33489 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33503 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33530 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33541 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33587 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33626 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33631 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33657 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33673 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33700 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33702 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33715 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33717 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33725 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33764 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33775 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33782 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33785 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33792 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33799 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33822 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33846 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33914 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33954 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33971 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33974 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33976 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33977 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33978 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#33995 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34004 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34024 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34026 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34042 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34050 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34056 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34072 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34073 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34094 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34095 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34100 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34102 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34110 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34111 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34112 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34120 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34130 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34153 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34173 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34184 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34188 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34204 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34257 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34293 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34292 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34319 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34332 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34381 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34403 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34435 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34453 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34477 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34524 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34540 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34548 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34547 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34552 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34554 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34557 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34560 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34569 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34582 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34591 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34667 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34669 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34704 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34715 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34757 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34756 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34759 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34789 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34797 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34800 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34807 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34811 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34897 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#34954 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35008 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35042 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35115 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35201 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35204 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35230 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35259 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35271 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35411 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35456 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35457 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35467 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35487 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35532 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35541 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35552 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35609 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35789 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35803 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35829 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35844 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35849 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35862 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35865 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35864 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35877 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35878 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#35882 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36046 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36144 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36166 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36180 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36181 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36183 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36188 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36259 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36386 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36421 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36424 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36462 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36470 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36481 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36496 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36504 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36508 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36537 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36562 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36564 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36615 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36648 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36699 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36666 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36715 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36785 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36813 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36821 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36825 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36826 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36849 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36907 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36909 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36920 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36921 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36932 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36952 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36964 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36973 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36974 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36975 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36993 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#36997 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37005 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37006 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37010 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37011 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37028 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37032 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37068 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37070 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37099 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37114 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37196 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37209 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37220 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37233 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37235 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37240 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37241 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37243 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37264 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37272 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37277 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37244 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37324 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37336 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37343 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37342 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37344 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37351 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37398 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37415 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37418 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37426 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37442 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37466 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37472 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37485 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37483 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37495 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37503 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37512 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37514 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37517 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37519 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37521 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37524 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37526 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37528 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37537 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37542 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37611 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37642 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37805 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37844 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37976 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37978 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#37993 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38017 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38242 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38289 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38351 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38359 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38368 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38455 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38540 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38559 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38582 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38609 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38744 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38756 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38757 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38758 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38760 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38763 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38772 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38777 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38792 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38794 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38796 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38806 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38810 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38812 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38813 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38815 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38814 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38816 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38821 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#38912 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39018 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39019 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39024 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39026 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39053 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39084 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39101 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39113 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39129 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39135 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39137 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39204 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39213 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39245 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39291 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39312 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39350 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39360 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39408 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39415 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39444 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39462 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39505 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39515 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39526 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39528 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39531 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39532 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39540 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39542 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39546 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39555 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39560 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39561 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39578 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39596 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39595 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39606 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39624 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39631 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39634 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39643 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39665 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39736 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39787 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39812 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39815 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39816 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39822 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39827 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39851 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39860 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39864 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39914 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39922 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39956 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39971 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39980 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39990 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39995 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39996 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#39998 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40001 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40010 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40013 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40023 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40038 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40046 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40050 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40078 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40092 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40094 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40096 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40101 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40105 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40107 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40109 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40110 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40114 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40115 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40191 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40458 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40479 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40525 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40570 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40575 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40574 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40633 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40662 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40736 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40827 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40828 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40847 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40878 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#40945 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41079 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41090 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41101 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41117 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41120 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41123 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41126 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41129 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41134 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41152 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41208 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41218 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41271 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41363 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41399 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41417 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41418 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41591 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41616 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41641 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41673 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41694 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41708 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41754 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41761 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41774 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41836 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41853 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41965 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41967 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41985 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#41993 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42012 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42026 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42047 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42051 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42054 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42064 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42066 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42076 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42111 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42114 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42135 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42149 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42179 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42219 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42225 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42231 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42236 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42286 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42287 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42289 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42296 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42301 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42307 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42528 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42534 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42584 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42645 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42689 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42771 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42808 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42836 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42856 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42884 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42888 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42891 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42910 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42937 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42952 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42956 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42959 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#42961 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43115 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43130 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43166 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43171 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43173 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43206 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43237 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43250 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43261 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43268 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43282 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43287 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43293 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43296 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43360 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43370 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43444 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43479 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43509 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43514 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43526 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43535 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43543 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43570 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43576 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43579 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43590 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43593 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43619 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43621 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43624 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43626 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43634 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43635 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43639 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43646 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43650 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43653 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43658 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43661 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43665 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43681 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43812 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43853 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43854 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43882 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43922 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#43976 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44010 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44015 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44026 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44030 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44041 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44051 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44052 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44053 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44056 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44063 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44064 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44065 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44074 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44075 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44078 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44084 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44220 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44233 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44324 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44351 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44370 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44376 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44379 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44391 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44406 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44420 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44436 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44437 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44442 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44472 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44488 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44496 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44530 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44541 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44543 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44545 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44546 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44547 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44552 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44553 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44556 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44559 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44560 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44582 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44610 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44614 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44641 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44741 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44751 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44775 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44776 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44814 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44820 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44827 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44839 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44840 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44844 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44859 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44863 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44878 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44883 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44889 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44893 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44907 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44910 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44915 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44946 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44950 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44978 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44983 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#44994 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45004 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45014 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45028 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45030 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45032 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45052 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45067 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45072 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45213 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45222 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45227 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45283 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45286 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45293 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45348 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45357 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45404 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45420 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45429 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45435 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45459 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45462 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45474 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45479 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45482 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45491 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45492 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45493 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45496 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45608 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45697 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45699 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45724 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45731 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45743 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45764 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45769 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45778 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45798 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45833 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45837 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45842 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45843 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45848 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45873 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45874 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45922 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45932 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45937 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45938 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45939 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45942 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45948 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45947 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45950 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#45988 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46021 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46056 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46067 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46093 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46111 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46115 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46128 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46138 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46158 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46169 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46197 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46198 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46206 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46214 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46321 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46331 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46337 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46348 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46355 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46469 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46489 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46490 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46507 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46513 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46545 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46549 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46565 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46566 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46567 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46568 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46569 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46570 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46575 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46579 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46582 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46596 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46726 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46775 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46829 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46832 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46861 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46876 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46888 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46895 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46917 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46922 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46934 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46939 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46946 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46953 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46954 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46956 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46957 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46959 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46960 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46966 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46969 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46972 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46979 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#46992 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47070 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47112 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47197 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47243 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47248 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47251 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47253 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47263 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47277 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47292 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47304 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47306 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47308 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47329 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47334 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47336 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47362 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47368 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47371 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47373 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47377 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47378 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47382 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47392 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47393 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47398 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47405 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47406 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47407 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47416 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47420 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47462 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47495 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47541 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47574 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47597 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47675 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47679 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47681 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47682 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47687 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47692 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47702 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47724 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47761 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47766 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47768 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47804 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47808 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47832 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47841 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47847 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47850 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47852 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47865 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47866 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47867 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47870 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47869 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#47875 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48033 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48104 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48127 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48130 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48131 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48135 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48136 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48158 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48186 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48395 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48406 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48431 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48455 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48460 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48461 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48485 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48487 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48489 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48801 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48841 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48851 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48854 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48868 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#48882 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49106 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49132 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49134 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49145 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49164 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49183 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49205 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49446 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49611 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49675 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49715 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49741 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49767 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49794 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49802 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49803 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49804 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49820 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49826 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49841 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49843 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49853 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49880 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49881 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49884 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#49904 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50004 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50155 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50182 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50192 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50213 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50220 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50224 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50272 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50274 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50276 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50277 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50278 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50711 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#50718 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51010 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51016 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51019 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51051 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51081 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51194 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51231 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51264 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51870 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#51951 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#56351 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#57167 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#57197 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#57239 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#57677 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#57671 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#57915 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58101 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58129 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58164 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58170 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58203 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58241 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58309 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58347 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58352 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58355 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58354 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58360 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58409 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58551 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58648 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58743 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58783 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#58785 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59006 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59445 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59447 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59472 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59474 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59679 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59680 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59800 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59813 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59903 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#59975 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60053 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60127 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60129 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60145 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60293 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60332 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60374 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60508 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60507 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60580 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60594 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60605 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60624 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#60683 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61133 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61235 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61296 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61334 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61372 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61509 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61524 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61806 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61808 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61818 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61832 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61842 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#61985 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#62104 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#62335 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#62377 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#62697 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#62742 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#62754 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#62991 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#63710 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#63884 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#64209 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#64533 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#64540 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#64548 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#64865 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#64916 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#64971 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#64972 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65037 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65125 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65179 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65393 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65404 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65415 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65491 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65557 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65559 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65582 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65595 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65596 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65597 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65623 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65628 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65673 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65801 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65867 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65908 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65948 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#65955 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66107 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66140 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66150 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66168 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66199 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66216 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66290 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66328 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66353 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66378 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66389 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66408 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66423 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66438 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66566 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66643 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66674 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66700 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66685 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66721 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66724 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66744 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66787 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66789 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66811 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66958 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#66966 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67039 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67082 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67083 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67422 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67425 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67431 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67467 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67627 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67714 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67715 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67723 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67725 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67841 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67913 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67944 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67945 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67948 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67953 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#67955 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68077 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68127 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68180 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68279 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68316 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68331 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68344 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68369 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68453 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68663 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68761 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68770 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#68769 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69004 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69026 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69116 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69147 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69189 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69312 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69325 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69324 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69340 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69455 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69514 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69531 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69533 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69554 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69593 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69624 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69726 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69756 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69787 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69797 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69807 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69814 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69836 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69839 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69838 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69848 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69871 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69872 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69873 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69883 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69890 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69897 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69919 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#69933 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#70102 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#70354 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#70413 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#70429 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#70434 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#70597 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#70845 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71127 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71191 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71208 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71329 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71334 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71336 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71604 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71651 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71665 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71692 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71821 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71825 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71828 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71829 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71835 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#71916 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72126 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72137 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72212 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72290 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72296 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72353 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72377 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72408 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72415 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72434 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72438 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72441 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72443 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72446 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72588 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72594 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72625 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72702 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72779 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72817 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72832 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72846 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72891 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72894 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72952 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#72974 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73049 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73050 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73058 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73061 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73065 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73115 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73141 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73226 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73266 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73300 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73398 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73414 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73425 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73445 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73444 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73457 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73526 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73565 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73597 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73636 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73717 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73812 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73845 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73848 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73852 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73873 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73876 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#73917 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74063 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74227 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74323 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74573 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74601 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74681 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74858 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74912 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74992 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74995 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#74998 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75284 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75296 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75390 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75403 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75446 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75648 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75804 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75954 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75959 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75962 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#75978 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76229 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76301 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76398 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76424 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76439 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76682 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76694 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76725 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76755 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76758 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76835 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76875 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#76969 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77045 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77057 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77200 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77202 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77281 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77314 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77352 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77464 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77508 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77527 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77561 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77609 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77691 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77705 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77755 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77795 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77796 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77852 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77858 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77876 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77879 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77880 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77884 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77886 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77895 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77908 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#77990 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78024 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78034 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78403 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78431 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78472 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78476 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78478 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78479 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78577 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78606 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78653 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78836 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#78984 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79007 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79113 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79334 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79363 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79492 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79497 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79498 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79499 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79500 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79503 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79511 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79542 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79732 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79805 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79870 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79898 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79918 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79932 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#79935 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80213 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80251 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80277 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80306 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80420 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80453 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80537 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80538 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80539 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80559 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80560 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80565 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80570 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80583 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80627 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80661 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80760 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80899 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80902 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80909 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#80936 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81049 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81116 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81237 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81247 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81250 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81285 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81286 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81287 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81313 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81344 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81372 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81403 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81406 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81481 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81482 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81501 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81616 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81719 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81731 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81858 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81948 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#81959 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82354 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82365 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82570 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82603 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82654 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82661 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82668 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82727 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82767 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82913 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#82915 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83246 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83251 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83558 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83626 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83699 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83731 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83777 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83806 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83808 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#83817 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84077 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84078 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84167 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84221 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84253 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84328 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84428 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84566 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84588 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84593 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84629 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84694 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84759 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84785 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84809 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84858 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84854 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84894 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84982 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84996 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#84998 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85012 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85065 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85131 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85164 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85165 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85171 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85188 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85206 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85236 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85298 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85355 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85450 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85469 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85518 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85523 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85536 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85613 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85645 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85703 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#85772 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#86035 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#86132 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#86205 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#86254 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#86321 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#86699 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#86754 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#86818 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#86821 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87010 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87084 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87160 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87323 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87335 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87396 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87399 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87621 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87702 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87701 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#87763 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88165 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88218 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88249 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88314 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88378 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88438 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88451 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88453 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88464 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88467 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88471 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88623 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88654 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88832 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88861 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88916 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#88920 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89115 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89133 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89220 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89219 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89283 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89406 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89418 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89445 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89494 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89632 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89641 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89702 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89861 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#89893 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90099 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90105 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90106 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90108 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90116 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90144 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90273 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90343 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90342 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90372 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90399 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90403 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90417 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90438 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90459 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90488 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90572 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90625 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90677 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90837 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90840 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90872 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90952 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90972 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90975 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#90986 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91016 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91047 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91168 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91157 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91381 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91512 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91508 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91516 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91564 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91592 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91593 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91619 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91656 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91677 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91684 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91696 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91773 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91779 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91782 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#91888 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92072 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92074 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92158 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92336 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92387 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92415 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92499 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92524 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92584 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#92713 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93075 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93225 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93273 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93278 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93284 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93437 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93571 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93576 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93789 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93916 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93948 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#93982 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94111 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94151 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94215 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94222 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94226 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94228 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94233 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94364 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94406 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94417 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94445 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94464 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94549 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94804 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94816 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94826 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94833 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#94840 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95337 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95352 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95395 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95500 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95555 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95628 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95688 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95693 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95817 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95829 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95830 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95856 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95908 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#95949 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#96040 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#96082 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#96093 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#96148 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#96442 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#96769 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#96985 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97230 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97325 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97369 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97370 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97426 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97557 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97651 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97672 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97680 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97683 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97693 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97695 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97874 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#97885 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98048 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98053 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98057 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98060 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98082 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98354 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98429 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98494 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98498 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98502 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#98526 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#99067 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#99481 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#99545 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#99697 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#100376 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#100385 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#100389 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#100449 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#100520 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#100545 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#100562 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#100564 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#100675 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#101027 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#104400 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#105259 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#105437 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#105364 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#105630 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#106026 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#106103 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#106203 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#106262 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#106446 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#106528 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#106640 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#106671 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#107236 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#109724 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#110204 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#110633 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#110746 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#110770 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#110772 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#111612 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#111371 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#111672 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#111830 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#111961 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#112066 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#112140 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#113527 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#115466 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#115739 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#115740 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#115795 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#116256 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#116452 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#116726 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#117343 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#118222 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#118872 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#119004 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#120011 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#120251 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#120299 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#121077 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#121103 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#121348 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#121635 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#122769 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#123266 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#124174 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#124446 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#124720 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#125574 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#125670 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#125711 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#126887 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#126959 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#126964 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#127567 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#127593 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#127615 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#127631 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#127870 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#128643 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#128666 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#128703 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#129557 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131445 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131474 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131515 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131580 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131632 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131676 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131795 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131901 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131913 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#131952 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#132200 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#132228 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#132355 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#132442 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#132564 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#137907 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#138074 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#141370 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#141718 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#142495 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#142564 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#144376 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#145859 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#148559 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#148768 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#149066 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#149657 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#151099 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#155694 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#156700 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#157952 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: booking.reservation_public_id_by_bookings_id#165648 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna reservation_public_id lasciata NULL
```
*(1 righe su 3487)*

**Cosa significa** — Due casi distinti, entrambi corretti: **carrelli abbandonati** (1.547) e **duplicati Stripe** (93).

**Causa a monte** — Fino al 2026-09-23 questa voce contava **8.866 righe** e l'intero blocco era attribuito alla regola `not propertyId`. Due cose sono cambiate. **(1) La catena di ripiego.** `propertyId` e' NULL su 8.773 righe perche' il legacy crea la transazione all'apertura del checkout, prima di qualunque collegamento. Ma la riga porta altri riferimenti, e ognuno sa a quale stabilimento appartiene: `booking_id` (65,2%), `reservation_id` (3,8%), `stripe_connected_id` (13,3%). Provandoli in quest'ordine si recuperano **7.226 righe su 8.773**, misurate e verificate. Restano 1.547 tentativi che non hanno alcun riferimento valido: hanno `status` NULL, importo pagato **zero**, e un `booking_id` che in `bookings` non esiste. Sono carrelli abbandonati, non pagamenti persi. **(2) I 93 non erano quello che sembrava.** Hanno eccome un `propertyId`: sono duplicati Stripe (stesso `stripe_transaction_id`, doppio webhook) scartati deliberatamente da un controllo nel custom transform. Comparivano sotto la regola sbagliata perche' `_RowExcluded` non portava un motivo e il registro ripiegava sulla regola della spec — attribuzione **falsa**, non informazione mancante. Corretto.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. Quadratura verificata: **36.613 migrate + 1.640 scartate = 38.253** righe sorgente, senza residui. Togliere l'esclusione ha fatto emergere due difetti che stavano sotto, invisibili finche' quelle righe non arrivavano al database: `paid_amount` NOT NULL violato (un `NULL` esplicito non attiva il `DEFAULT 0` della colonna) e `occurred_at` NOT NULL violato (`transaction_date` e' valorizzata solo sulle transazioni completate). Entrambi corretti nel mapping; il secondo usando `createdAt`, che per un PaymentIntent creato e mai completato **e'** l'istante dell'evento, non una stima.

## Righe

| id legacy | motivo | `id` | `pending_payment` | `booking_id` | `createdAt` | `updatedAt` |
|---|---|---|---|---|---|---|
| `5` | Esclusa da una regola | 5 | 10 | 3007 | 2021-08-04T11:50:12 | 2021-08-04T11:50:12 |
| `8` | Esclusa da una regola | 8 | 15 | 4181 | 2021-08-24T13:38:43 | 2021-08-24T13:38:43 |
| `14` | Esclusa da una regola | 14 | 190 | 4466 | 2021-10-01T12:39:39 | 2021-10-01T12:39:39 |
| `20` | Esclusa da una regola | 20 | 12430 | 4473 | 2021-10-11T12:55:26 | 2021-10-11T12:55:26 |
| `24` | Esclusa da una regola | 24 | 380 | 4476 | 2021-10-12T09:45:27 | 2021-10-12T09:45:27 |
| `26` | Esclusa da una regola | 26 | 390 | 4477 | 2021-10-12T11:46:53 | 2021-10-12T11:46:53 |
| `29` | Esclusa da una regola | 29 | 260 | 4478 | 2021-10-12T12:12:52 | 2021-10-12T12:12:52 |
| `32` | Esclusa da una regola | 32 | 5 | 4588 | 2022-01-14T17:50:53 | 2022-01-14T17:50:53 |
| `33` | Esclusa da una regola | 33 | 5 | 4589 | 2022-01-14T17:53:12 | 2022-01-14T17:53:12 |
| `35` | Esclusa da una regola | 35 | 5 | 4859 | 2022-03-08T17:37:23 | 2022-03-08T17:37:23 |
| `36` | Esclusa da una regola | 36 | 830.7 | 4860 | 2022-03-08T18:00:55 | 2022-03-08T18:00:55 |
| `37` | Esclusa da una regola | 37 | 8 | 4944 | 2022-03-24T13:58:54 | 2022-03-24T13:58:54 |
| `42` | Esclusa da una regola | 42 | 120 | 6009 | 2022-05-20T20:21:04 | 2022-05-20T20:21:04 |
| `43` | Esclusa da una regola | 43 | 2 | 6026 | 2022-05-22T10:02:56 | 2022-05-22T10:02:56 |
| `46` | Esclusa da una regola | 46 | 198 | 6350 | 2022-05-24T13:38:03 | 2022-05-24T13:38:03 |
| `47` | Esclusa da una regola | 47 | 2 | 6541 | 2022-05-25T07:17:22 | 2022-05-25T07:17:22 |
| `48` | Esclusa da una regola | 48 | 2 | 6544 | 2022-05-25T07:41:08 | 2022-05-25T07:41:08 |
| `49` | Esclusa da una regola | 49 | 2 | 6556 | 2022-05-25T08:37:57 | 2022-05-25T08:37:57 |
| `50` | Esclusa da una regola | 50 | 2 | 6559 | 2022-05-25T08:50:46 | 2022-05-25T08:50:46 |
| `51` | Esclusa da una regola | 51 | 35 | 6560 | 2022-05-25T09:04:28 | 2022-05-25T09:04:28 |
| `52` | Esclusa da una regola | 52 | 99 | 6563 | 2022-05-25T09:14:54 | 2022-05-25T09:14:54 |
| `53` | Esclusa da una regola | 53 | 99 | 6564 | 2022-05-25T09:16:28 | 2022-05-25T09:16:28 |
| `54` | Esclusa da una regola | 54 | 92 | 6566 | 2022-05-25T09:21:01 | 2022-05-25T09:21:01 |
| `55` | Esclusa da una regola | 55 | 92 | 6568 | 2022-05-25T09:27:27 | 2022-05-25T09:27:27 |
| `56` | Esclusa da una regola | 56 | 2 | 6579 | 2022-05-25T09:58:05 | 2022-05-25T09:58:05 |
| `57` | Esclusa da una regola | 57 | 2 | 6580 | 2022-05-25T09:58:54 | 2022-05-25T09:58:54 |
| `58` | Esclusa da una regola | 58 | 15 | 6581 | 2022-05-25T10:07:50 | 2022-05-25T10:07:50 |
| `59` | Esclusa da una regola | 59 | 2 | 6582 | 2022-05-25T10:37:04 | 2022-05-25T10:37:04 |
| `60` | Esclusa da una regola | 60 | 10 | 6583 | 2022-05-25T10:44:12 | 2022-05-25T10:44:12 |
| `61` | Esclusa da una regola | 61 | 4 | 6585 | 2022-05-25T13:12:11 | 2022-05-25T13:12:11 |
| `62` | Esclusa da una regola | 62 | 4 | 6586 | 2022-05-25T13:19:29 | 2022-05-25T13:19:29 |
| `63` | Esclusa da una regola | 63 | 2 | 6587 | 2022-05-25T13:24:04 | 2022-05-25T13:24:04 |
| `64` | Esclusa da una regola | 64 | 25 | 6594 | 2022-05-25T16:47:43 | 2022-05-25T16:47:43 |
| `65` | Esclusa da una regola | 65 | 12 | 6595 | 2022-05-25T16:52:57 | 2022-05-25T16:52:57 |
| `66` | Esclusa da una regola | 66 | 12 | 6597 | 2022-05-25T16:55:57 | 2022-05-25T16:55:57 |
| `67` | Esclusa da una regola | 67 | 12 | 6598 | 2022-05-25T17:14:37 | 2022-05-25T17:14:37 |
| `68` | Esclusa da una regola | 68 | 92 | 6599 | 2022-05-25T17:30:15 | 2022-05-25T17:30:15 |
| `70` | Esclusa da una regola | 70 | 2 | 6601 | 2022-05-26T06:49:33 | 2022-05-26T06:49:33 |
| `71` | Esclusa da una regola | 71 | 15 | 6615 | 2022-05-26T11:50:35 | 2022-05-26T11:50:35 |
| `72` | Esclusa da una regola | 72 | 94 | 6616 | 2022-05-26T11:58:04 | 2022-05-26T11:58:04 |
| `73` | Esclusa da una regola | 73 | 60 | 6617 | 2022-05-26T12:10:48 | 2022-05-26T12:10:48 |
| `75` | Esclusa da una regola | 75 | 12 | 6970 | 2022-05-29T16:38:55 | 2022-05-29T16:38:55 |
| `76` | Esclusa da una regola | 76 | 31 | 7099 | 2022-05-31T16:59:32 | 2022-05-31T16:59:32 |
| `77` | Esclusa da una regola | 77 | 50 | 7137 | 2022-06-01T13:00:40 | 2022-06-01T13:00:40 |
| `78` | Esclusa da una regola | 78 | 8 | 7145 | 2022-06-01T14:57:32 | 2022-06-01T14:57:32 |
| `79` | Esclusa da una regola | 79 | 9 | 7191 | 2022-06-02T06:06:54 | 2022-06-02T06:06:54 |
| `80` | Esclusa da una regola | 80 | 9 | 7377 | 2022-06-02T16:55:39 | 2022-06-02T16:55:39 |
| `81` | Esclusa da una regola | 81 | 28 | 7428 | 2022-06-03T11:12:14 | 2022-06-03T11:12:14 |
| `82` | Esclusa da una regola | 82 | 7 | 7430 | 2022-06-03T11:28:11 | 2022-06-03T11:28:11 |
| `83` | Esclusa da una regola | 83 | 18 | 7432 | 2022-06-03T11:32:44 | 2022-06-03T11:32:44 |
| `84` | Esclusa da una regola | 84 | 12 | 7433 | 2022-06-03T11:52:01 | 2022-06-03T11:52:01 |
| `85` | Esclusa da una regola | 85 | 8 | 7437 | 2022-06-03T12:01:26 | 2022-06-03T12:01:26 |
| `86` | Esclusa da una regola | 86 | 11 | 7458 | 2022-06-03T14:59:53 | 2022-06-03T14:59:53 |
| `87` | Esclusa da una regola | 87 | 11 | 7459 | 2022-06-03T15:03:05 | 2022-06-03T15:03:05 |
| `88` | Esclusa da una regola | 88 | 63 | 7543 | 2022-06-04T09:37:06 | 2022-06-04T09:37:06 |
| `89` | Esclusa da una regola | 89 | 8 | 7571 | 2022-06-04T11:36:09 | 2022-06-04T11:36:09 |
| `90` | Esclusa da una regola | 90 | 36 | 7574 | 2022-06-04T11:38:36 | 2022-06-04T11:38:36 |
| `91` | Esclusa da una regola | 91 | 15 | 7575 | 2022-06-04T11:42:11 | 2022-06-04T11:42:11 |
| `92` | Esclusa da una regola | 92 | 2 | 7601 | 2022-06-04T14:23:31 | 2022-06-04T14:23:31 |
| `93` | Esclusa da una regola | 93 | 9 | 7863 | 2022-06-05T18:13:17 | 2022-06-05T18:13:17 |
| `94` | Esclusa da una regola | 94 | 5 | 7944 | 2022-06-06T12:48:25 | 2022-06-06T12:48:25 |
| `95` | Esclusa da una regola | 95 | 2 | 7946 | 2022-06-06T13:46:41 | 2022-06-06T13:46:41 |
| `96` | Esclusa da una regola | 96 | 2 | 7958 | 2022-06-07T03:38:36 | 2022-06-07T03:38:36 |
| `97` | Esclusa da una regola | 97 | 10.3 | 7959 | 2022-06-07T03:58:44 | 2022-06-07T03:58:44 |
| `98` | Esclusa da una regola | 98 | 64 | 7961 | 2022-06-07T04:34:53 | 2022-06-07T04:34:53 |
| `99` | Esclusa da una regola | 99 | 44 | 7965 | 2022-06-07T05:44:15 | 2022-06-07T05:44:15 |
| `100` | Esclusa da una regola | 100 | 2 | 8004 | 2022-06-07T12:24:01 | 2022-06-07T12:24:01 |
| `101` | Esclusa da una regola | 101 | 30 | 8007 | 2022-06-07T12:49:48 | 2022-06-07T12:49:48 |
| `102` | Esclusa da una regola | 102 | 2 | 8015 | 2022-06-07T14:08:23 | 2022-06-07T14:08:23 |
| `103` | Esclusa da una regola | 103 | 7 | 8018 | 2022-06-07T14:22:38 | 2022-06-07T14:22:38 |
| `104` | Esclusa da una regola | 104 | 7 | 8019 | 2022-06-07T14:28:50 | 2022-06-07T14:28:50 |
| `105` | Esclusa da una regola | 105 | 3 | 8046 | 2022-06-08T03:08:49 | 2022-06-08T03:08:49 |
| `106` | Esclusa da una regola | 106 | 10 | 8083 | 2022-06-08T11:48:38 | 2022-06-08T11:48:38 |
| `107` | Esclusa da una regola | 107 | 8 | 8087 | 2022-06-08T12:38:27 | 2022-06-08T12:38:27 |
| `108` | Esclusa da una regola | 108 | 8 | 8088 | 2022-06-08T12:44:10 | 2022-06-08T12:44:10 |
| `109` | Esclusa da una regola | 109 | 2 | 8103 | 2022-06-08T16:43:44 | 2022-06-08T16:43:44 |
| `110` | Esclusa da una regola | 110 | 3 | 8115 | 2022-06-09T04:44:49 | 2022-06-09T04:44:49 |
| `111` | Esclusa da una regola | 111 | 2 | 8119 | 2022-06-09T04:51:09 | 2022-06-09T04:51:09 |
| `112` | Esclusa da una regola | 112 | 74 | 8121 | 2022-06-09T05:02:57 | 2022-06-09T05:02:57 |
| `113` | Esclusa da una regola | 113 | 74 | 8123 | 2022-06-09T06:13:18 | 2022-06-09T06:13:18 |
| `114` | Esclusa da una regola | 114 | 2 | 8124 | 2022-06-09T06:39:47 | 2022-06-09T06:39:47 |
| `115` | Esclusa da una regola | 115 | 2 | 8125 | 2022-06-09T07:05:13 | 2022-06-09T07:05:13 |
| `116` | Esclusa da una regola | 116 | 42 | 8154 | 2022-06-09T11:49:57 | 2022-06-09T11:49:57 |
| `117` | Esclusa da una regola | 117 | 488 | 8171 | 2022-06-10T03:39:31 | 2022-06-10T03:39:31 |
| `118` | Esclusa da una regola | 118 | 179.4 | 8176 | 2022-06-10T03:47:21 | 2022-06-10T03:47:21 |
| `119` | Esclusa da una regola | 119 | 62 | 8177 | 2022-06-10T03:49:16 | 2022-06-10T03:49:16 |
| `120` | Esclusa da una regola | 120 | 410 | 8178 | 2022-06-10T03:58:35 | 2022-06-10T03:58:35 |
| `121` | Esclusa da una regola | 121 | 210 | 8180 | 2022-06-10T04:14:58 | 2022-06-10T04:14:58 |
| `122` | Esclusa da una regola | 122 | 8 | 8183 | 2022-06-10T06:51:07 | 2022-06-10T06:51:07 |
| `123` | Esclusa da una regola | 123 | 8 | 8228 | 2022-06-10T12:00:38 | 2022-06-10T12:00:38 |
| `124` | Esclusa da una regola | 124 | 8 | 8239 | 2022-06-10T13:31:28 | 2022-06-10T13:31:28 |
| `125` | Esclusa da una regola | 125 | 28 | 8252 | 2022-06-10T15:05:28 | 2022-06-10T15:05:28 |
| `127` | Esclusa da una regola | 127 | 2 | 8291 | 2022-06-11T06:18:05 | 2022-06-11T06:18:05 |
| `128` | Esclusa da una regola | 128 | 62 | 8294 | 2022-06-11T06:25:11 | 2022-06-11T06:25:11 |
| `129` | Esclusa da una regola | 129 | 2 | 8306 | 2022-06-11T06:42:30 | 2022-06-11T06:42:30 |
| `130` | Esclusa da una regola | 130 | 72 | 8398 | 2022-06-11T10:20:43 | 2022-06-11T10:20:43 |
| `131` | Esclusa da una regola | 131 | 2 | 8407 | 2022-06-11T10:27:52 | 2022-06-11T10:27:52 |
| `132` | Esclusa da una regola | 132 | 28 | 8424 | 2022-06-11T11:45:00 | 2022-06-11T11:45:00 |
| `133` | Esclusa da una regola | 133 | 5 | 8450 | 2022-06-11T14:20:56 | 2022-06-11T14:20:56 |
| `134` | Esclusa da una regola | 134 | 152 | 8455 | 2022-06-11T14:35:26 | 2022-06-11T14:35:26 |
| `135` | Esclusa da una regola | 135 | 39 | 8466 | 2022-06-11T15:01:40 | 2022-06-11T15:01:40 |
| `136` | Esclusa da una regola | 136 | 11 | 8473 | 2022-06-11T15:24:06 | 2022-06-11T15:24:06 |
| `137` | Esclusa da una regola | 137 | 8 | 8518 | 2022-06-11T16:47:01 | 2022-06-11T16:47:01 |
| `138` | Esclusa da una regola | 138 | 38 | 8557 | 2022-06-11T18:10:58 | 2022-06-11T18:10:58 |
| `139` | Esclusa da una regola | 139 | 56 | 8558 | 2022-06-11T18:38:44 | 2022-06-11T18:38:44 |
| `140` | Esclusa da una regola | 140 | 41 | 8560 | 2022-06-11T19:29:10 | 2022-06-11T19:29:10 |
| `141` | Esclusa da una regola | 141 | 32 | 8750 | 2022-06-13T06:08:05 | 2022-06-13T06:08:05 |
| `143` | Esclusa da una regola | 143 | 5 | 8775 | 2022-06-13T10:52:15 | 2022-06-13T10:52:15 |
| `150` | Esclusa da una regola | 150 | 224.3 | 8832 | 2022-06-14T09:29:18 | 2022-06-14T09:29:18 |
| `153` | Esclusa da una regola | 153 | 2 | 8842 | 2022-06-14T10:35:34 | 2022-06-14T10:35:34 |
| `154` | Esclusa da una regola | 154 | 2 | 8845 | 2022-06-14T10:43:20 | 2022-06-14T10:43:20 |
| `155` | Esclusa da una regola | 155 | 35 | 8872 | 2022-06-14T14:31:44 | 2022-06-14T14:31:44 |
| `156` | Esclusa da una regola | 156 | 290 | 8918 | 2022-06-15T10:45:26 | 2022-06-15T10:45:26 |
| `157` | Esclusa da una regola | 157 | 429 | 8921 | 2022-06-15T10:57:46 | 2022-06-15T10:57:46 |
| `158` | Esclusa da una regola | 158 | 334 | 8922 | 2022-06-15T11:02:17 | 2022-06-15T11:02:17 |
| `159` | Esclusa da una regola | 159 | 2 | 8984 | 2022-06-16T10:56:31 | 2022-06-16T10:56:31 |
| `160` | Esclusa da una regola | 160 | 70 | 8994 | 2022-06-16T12:03:28 | 2022-06-16T12:03:28 |
| `161` | Esclusa da una regola | 161 | 28 | 9002 | 2022-06-16T13:25:53 | 2022-06-16T13:25:53 |
| `162` | Esclusa da una regola | 162 | 8 | 9016 | 2022-06-16T16:11:30 | 2022-06-16T16:11:30 |
| `163` | Esclusa da una regola | 163 | 32 | 9038 | 2022-06-17T08:02:56 | 2022-06-17T08:02:56 |
| `167` | Esclusa da una regola | 167 | 28 | 9075 | 2022-06-17T10:49:33 | 2022-06-17T10:49:33 |
| `168` | Esclusa da una regola | 168 | 27 | 9085 | 2022-06-17T12:22:46 | 2022-06-17T12:22:46 |
| `169` | Esclusa da una regola | 169 | 56 | 9086 | 2022-06-17T12:27:24 | 2022-06-17T12:27:24 |
| `170` | Esclusa da una regola | 170 | 29 | 9140 | 2022-06-17T18:55:33 | 2022-06-17T18:55:33 |
| `171` | Esclusa da una regola | 171 | 63 | 9141 | 2022-06-17T19:00:09 | 2022-06-17T19:00:09 |
| `172` | Esclusa da una regola | 172 | 42 | 9142 | 2022-06-17T20:25:35 | 2022-06-17T20:25:35 |
| `173` | Esclusa da una regola | 173 | 6 | 9151 | 2022-06-18T07:11:35 | 2022-06-18T07:11:35 |
| `174` | Esclusa da una regola | 174 | 28 | 9166 | 2022-06-18T07:54:01 | 2022-06-18T07:54:01 |
| `175` | Esclusa da una regola | 175 | 8 | 9211 | 2022-06-18T11:38:23 | 2022-06-18T11:38:23 |
| `176` | Esclusa da una regola | 176 | 4 | 9251 | 2022-06-18T12:51:58 | 2022-06-18T12:51:58 |
| `177` | Esclusa da una regola | 177 | 6 | 9266 | 2022-06-18T13:08:22 | 2022-06-18T13:08:22 |
| `178` | Esclusa da una regola | 178 | 28 | 9327 | 2022-06-18T15:06:48 | 2022-06-18T15:06:48 |
| `179` | Esclusa da una regola | 179 | 4 | 9334 | 2022-06-18T16:11:29 | 2022-06-18T16:11:29 |
| `180` | Esclusa da una regola | 180 | 8 | 9338 | 2022-06-18T16:18:55 | 2022-06-18T16:18:55 |
| `181` | Esclusa da una regola | 181 | 2 | 9604 | 2022-06-20T10:10:33 | 2022-06-20T10:10:33 |
| `182` | Esclusa da una regola | 182 | 25 | 9614 | 2022-06-20T11:36:53 | 2022-06-20T11:36:53 |
| `183` | Esclusa da una regola | 183 | 35 | 9639 | 2022-06-20T14:03:25 | 2022-06-20T14:03:25 |
| `184` | Esclusa da una regola | 184 | 88 | 9644 | 2022-06-20T15:04:49 | 2022-06-20T15:04:49 |
| `185` | Esclusa da una regola | 185 | 5 | 9673 | 2022-06-21T07:14:21 | 2022-06-21T07:14:21 |
| `186` | Esclusa da una regola | 186 | 20 | 9674 | 2022-06-21T07:15:15 | 2022-06-21T07:15:15 |
| `188` | Esclusa da una regola | 188 | 19 | 9797 | 2022-06-22T06:50:11 | 2022-06-22T06:50:11 |
| `189` | Esclusa da una regola | 189 | 15 | 9800 | 2022-06-22T07:15:15 | 2022-06-22T07:15:15 |
| `190` | Esclusa da una regola | 190 | 8 | 10014 | 2022-06-23T16:32:00 | 2022-06-23T16:32:00 |
| `191` | Esclusa da una regola | 191 | 9 | 10059 | 2022-06-24T09:01:53 | 2022-06-24T09:01:53 |
| `192` | Esclusa da una regola | 192 | 8 | 10082 | 2022-06-24T10:09:29 | 2022-06-24T10:09:29 |
| `193` | Esclusa da una regola | 193 | 9 | 10096 | 2022-06-24T10:58:48 | 2022-06-24T10:58:48 |
| `194` | Esclusa da una regola | 194 | 29 | 10108 | 2022-06-24T12:07:04 | 2022-06-24T12:07:04 |
| `195` | Esclusa da una regola | 195 | 24 | 10162 | 2022-06-24T16:28:35 | 2022-06-24T16:28:35 |
| `196` | Esclusa da una regola | 196 | 28 | 10173 | 2022-06-24T20:06:01 | 2022-06-24T20:06:01 |
| `197` | Esclusa da una regola | 197 | 9 | 10193 | 2022-06-25T07:35:10 | 2022-06-25T07:35:10 |
| `198` | Esclusa da una regola | 198 | 10 | 10243 | 2022-06-25T09:06:31 | 2022-06-25T09:06:31 |
| `199` | Esclusa da una regola | 199 | 28 | 10252 | 2022-06-25T09:31:59 | 2022-06-25T09:31:59 |
| `200` | Esclusa da una regola | 200 | 35 | 10299 | 2022-06-25T13:17:59 | 2022-06-25T13:17:59 |
| `201` | Esclusa da una regola | 201 | 8 | 10312 | 2022-06-25T14:34:45 | 2022-06-25T14:34:45 |
| `202` | Esclusa da una regola | 202 | 28 | 10313 | 2022-06-25T14:43:38 | 2022-06-25T14:43:38 |
| `203` | Esclusa da una regola | 203 | 28 | 10362 | 2022-06-25T19:25:41 | 2022-06-25T19:25:41 |
| `204` | Esclusa da una regola | 204 | 16 | 10564 | 2022-06-27T21:10:38 | 2022-06-27T21:10:38 |
| `205` | Esclusa da una regola | 205 | 2 | 10688 | 2022-06-29T15:18:38 | 2022-06-29T15:18:38 |
| `206` | Esclusa da una regola | 206 | 4 | 10689 | 2022-06-29T15:20:28 | 2022-06-29T15:20:28 |
| `207` | Esclusa da una regola | 207 | 5 | 10690 | 2022-06-29T15:51:13 | 2022-06-29T15:51:13 |
| `208` | Esclusa da una regola | 208 | 50 | 10716 | 2022-06-30T07:35:39 | 2022-06-30T07:35:39 |
| `209` | Esclusa da una regola | 209 | 8 | 10737 | 2022-06-30T09:38:44 | 2022-06-30T09:38:44 |
| `210` | Esclusa da una regola | 210 | 8 | 10738 | 2022-06-30T09:39:42 | 2022-06-30T09:39:42 |
| `211` | Esclusa da una regola | 211 | 8 | 10740 | 2022-06-30T09:40:59 | 2022-06-30T09:40:59 |
| `212` | Esclusa da una regola | 212 | 28 | 10754 | 2022-06-30T11:11:05 | 2022-06-30T11:11:05 |
| `213` | Esclusa da una regola | 213 | 8 | 10813 | 2022-06-30T18:44:07 | 2022-06-30T18:44:07 |
| `214` | Esclusa da una regola | 214 | 4 | 10920 | 2022-07-01T10:43:52 | 2022-07-01T10:43:52 |
| `215` | Esclusa da una regola | 215 | 15 | 10980 | 2022-07-01T16:18:35 | 2022-07-01T16:18:35 |
| `216` | Esclusa da una regola | 216 | 4 | 11002 | 2022-07-01T19:04:17 | 2022-07-01T19:04:17 |
| `217` | Esclusa da una regola | 217 | 8 | 11004 | 2022-07-01T19:19:26 | 2022-07-01T19:19:26 |
| `218` | Esclusa da una regola | 218 | 2 | 11007 | 2022-07-01T20:48:50 | 2022-07-01T20:48:50 |
| `219` | Esclusa da una regola | 219 | 28 | 11008 | 2022-07-01T22:19:36 | 2022-07-01T22:19:36 |
| `220` | Esclusa da una regola | 220 | 29 | 11101 | 2022-07-02T10:07:07 | 2022-07-02T10:07:07 |
| `221` | Esclusa da una regola | 221 | 32 | 11124 | 2022-07-02T11:42:17 | 2022-07-02T11:42:17 |
| `222` | Esclusa da una regola | 222 | 32 | 11126 | 2022-07-02T11:45:33 | 2022-07-02T11:45:33 |
| `223` | Esclusa da una regola | 223 | 26 | 11142 | 2022-07-02T12:39:29 | 2022-07-02T12:39:29 |
| `224` | Esclusa da una regola | 224 | 38 | 11144 | 2022-07-02T13:04:49 | 2022-07-02T13:04:49 |
| `225` | Esclusa da una regola | 225 | 8 | 11167 | 2022-07-02T14:15:55 | 2022-07-02T14:15:55 |
| `226` | Esclusa da una regola | 226 | 6 | 11173 | 2022-07-02T14:27:29 | 2022-07-02T14:27:29 |
| `227` | Esclusa da una regola | 227 | 8 | 11178 | 2022-07-02T14:28:57 | 2022-07-02T14:28:57 |
| `228` | Esclusa da una regola | 228 | 8 | 11181 | 2022-07-02T14:30:41 | 2022-07-02T14:30:41 |
| `229` | Esclusa da una regola | 229 | 8 | 11182 | 2022-07-02T14:30:51 | 2022-07-02T14:30:51 |
| `230` | Esclusa da una regola | 230 | 8 | 11183 | 2022-07-02T14:33:56 | 2022-07-02T14:33:56 |
| `231` | Esclusa da una regola | 231 | 8 | 11184 | 2022-07-02T14:35:30 | 2022-07-02T14:35:30 |
| `232` | Esclusa da una regola | 232 | 8 | 11186 | 2022-07-02T14:36:36 | 2022-07-02T14:36:36 |
| `233` | Esclusa da una regola | 233 | 8 | 11187 | 2022-07-02T14:38:27 | 2022-07-02T14:38:27 |
| `234` | Esclusa da una regola | 234 | 8 | 11188 | 2022-07-02T14:39:57 | 2022-07-02T14:39:57 |
| `235` | Esclusa da una regola | 235 | 8 | 11189 | 2022-07-02T14:41:29 | 2022-07-02T14:41:29 |
| `236` | Esclusa da una regola | 236 | 2 | 11190 | 2022-07-02T14:44:11 | 2022-07-02T14:44:11 |
| `237` | Esclusa da una regola | 237 | 8 | 11191 | 2022-07-02T14:45:36 | 2022-07-02T14:45:36 |
| `238` | Esclusa da una regola | 238 | 6 | 11194 | 2022-07-02T15:03:29 | 2022-07-02T15:03:29 |
| `239` | Esclusa da una regola | 239 | 4 | 11198 | 2022-07-02T15:10:15 | 2022-07-02T15:10:15 |
| `240` | Esclusa da una regola | 240 | 38 | 11219 | 2022-07-02T16:08:50 | 2022-07-02T16:08:50 |
| `241` | Esclusa da una regola | 241 | 38 | 11243 | 2022-07-02T18:14:39 | 2022-07-02T18:14:39 |
| `242` | Esclusa da una regola | 242 | 28 | 11245 | 2022-07-02T18:21:26 | 2022-07-02T18:21:26 |
| `243` | Esclusa da una regola | 243 | 28 | 11249 | 2022-07-02T19:11:37 | 2022-07-02T19:11:37 |
| `244` | Esclusa da una regola | 244 | 4 | 11252 | 2022-07-03T01:23:02 | 2022-07-03T01:23:02 |
| `245` | Esclusa da una regola | 245 | 6 | 11603 | 2022-07-04T20:56:09 | 2022-07-04T20:56:09 |
| `246` | Esclusa da una regola | 246 | 28 | 11657 | 2022-07-05T11:13:05 | 2022-07-05T11:13:05 |
| `247` | Esclusa da una regola | 247 | 6 | 11699 | 2022-07-05T18:25:51 | 2022-07-05T18:25:51 |
| `248` | Esclusa da una regola | 248 | 5 | 11702 | 2022-07-05T20:19:44 | 2022-07-05T20:19:44 |
| `249` | Esclusa da una regola | 249 | 6 | 11711 | 2022-07-06T07:41:54 | 2022-07-06T07:41:54 |
| `250` | Esclusa da una regola | 250 | 35 | 11782 | 2022-07-06T20:02:11 | 2022-07-06T20:02:11 |
| `251` | Esclusa da una regola | 251 | 16 | 11873 | 2022-07-07T11:55:32 | 2022-07-07T11:55:32 |
| `252` | Esclusa da una regola | 252 | 70 | 11887 | 2022-07-07T12:54:54 | 2022-07-07T12:54:54 |
| `253` | Esclusa da una regola | 253 | 36 | 11914 | 2022-07-07T18:04:47 | 2022-07-07T18:04:47 |
| `254` | Esclusa da una regola | 254 | 28 | 11916 | 2022-07-07T20:38:58 | 2022-07-07T20:38:58 |
| `255` | Esclusa da una regola | 255 | 8 | 11940 | 2022-07-08T08:38:16 | 2022-07-08T08:38:16 |
| `256` | Esclusa da una regola | 256 | 11 | 12017 | 2022-07-08T17:35:11 | 2022-07-08T17:35:11 |
| `258` | Esclusa da una regola | 258 | 8 | 12024 | 2022-07-09T06:48:14 | 2022-07-09T06:48:14 |
| `259` | Esclusa da una regola | 259 | 2 | 12066 | 2022-07-09T08:13:41 | 2022-07-09T08:13:41 |
| `260` | Esclusa da una regola | 260 | 6 | 12079 | 2022-07-09T08:42:19 | 2022-07-09T08:42:19 |
| `261` | Esclusa da una regola | 261 | 38 | 12138 | 2022-07-09T10:55:10 | 2022-07-09T10:55:10 |
| `262` | Esclusa da una regola | 262 | 28 | 12148 | 2022-07-09T12:02:03 | 2022-07-09T12:02:03 |
| `263` | Esclusa da una regola | 263 | 26 | 12169 | 2022-07-09T13:29:37 | 2022-07-09T13:29:37 |
| `264` | Esclusa da una regola | 264 | 28 | 12208 | 2022-07-09T16:20:19 | 2022-07-09T16:20:19 |
| `265` | Esclusa da una regola | 265 | 4 | 12220 | 2022-07-09T17:51:11 | 2022-07-09T17:51:11 |
| `266` | Esclusa da una regola | 266 | 29 | 12465 | 2022-07-11T09:45:17 | 2022-07-11T09:45:17 |
| `267` | Esclusa da una regola | 267 | 4 | 13204 | 2022-07-15T08:04:21 | 2022-07-15T08:04:21 |
| `268` | Esclusa da una regola | 268 | 8 | 13387 | 2022-07-16T07:10:30 | 2022-07-16T07:10:30 |
| `269` | Esclusa da una regola | 269 | 28 | 13588 | 2022-07-16T15:57:20 | 2022-07-16T15:57:20 |
| `270` | Esclusa da una regola | 270 | 8 | 13617 | 2022-07-16T16:44:58 | 2022-07-16T16:44:58 |
| `271` | Esclusa da una regola | 271 | 38 | 13637 | 2022-07-16T20:16:02 | 2022-07-16T20:16:02 |
| `272` | Esclusa da una regola | 272 | 8 | 13781 | 2022-07-17T12:21:31 | 2022-07-17T12:21:31 |
| `273` | Esclusa da una regola | 273 | 8 | 13803 | 2022-07-17T14:13:18 | 2022-07-17T14:13:18 |
| `274` | Esclusa da una regola | 274 | 36 | 13832 | 2022-07-17T16:41:06 | 2022-07-17T16:41:06 |
| `275` | Esclusa da una regola | 275 | 24 | 13878 | 2022-07-18T07:34:44 | 2022-07-18T07:34:44 |
| `276` | Esclusa da una regola | 276 | 36 | 13945 | 2022-07-18T12:43:38 | 2022-07-18T12:43:38 |
| `277` | Esclusa da una regola | 277 | 15 | 13980 | 2022-07-19T04:30:46 | 2022-07-19T04:30:46 |
| `278` | Esclusa da una regola | 278 | 32 | 14080 | 2022-07-19T09:58:53 | 2022-07-19T09:58:53 |
| `279` | Esclusa da una regola | 279 | 28 | 14098 | 2022-07-19T11:18:14 | 2022-07-19T11:18:14 |
| `280` | Esclusa da una regola | 280 | 8 | 14150 | 2022-07-19T20:56:49 | 2022-07-19T20:56:49 |
| `281` | Esclusa da una regola | 281 | 42 | 14163 | 2022-07-20T07:38:56 | 2022-07-20T07:38:56 |
| `282` | Esclusa da una regola | 282 | 3 | 14224 | 2022-07-20T11:00:04 | 2022-07-20T11:00:04 |
| `283` | Esclusa da una regola | 283 | 8 | 14289 | 2022-07-20T19:00:44 | 2022-07-20T19:00:44 |
| `284` | Esclusa da una regola | 284 | 16 | 14311 | 2022-07-21T00:45:30 | 2022-07-21T00:45:30 |
| `285` | Esclusa da una regola | 285 | 41 | 14313 | 2022-07-21T06:13:56 | 2022-07-21T06:13:56 |
| `286` | Esclusa da una regola | 286 | 32 | 14327 | 2022-07-21T07:38:04 | 2022-07-21T07:38:04 |
| `287` | Esclusa da una regola | 287 | 2 | 14421 | 2022-07-21T12:48:56 | 2022-07-21T12:48:56 |
| `288` | Esclusa da una regola | 288 | 21 | 14543 | 2022-07-22T07:36:12 | 2022-07-22T07:36:12 |
| `289` | Esclusa da una regola | 289 | 5 | 14585 | 2022-07-22T09:03:30 | 2022-07-22T09:03:30 |
| `290` | Esclusa da una regola | 290 | 2 | 14622 | 2022-07-22T10:24:54 | 2022-07-22T10:24:54 |
| `291` | Esclusa da una regola | 291 | 2 | 14623 | 2022-07-22T10:27:30 | 2022-07-22T10:27:30 |
| `292` | Esclusa da una regola | 292 | 28 | 14642 | 2022-07-22T11:07:31 | 2022-07-22T11:07:31 |
| `293` | Esclusa da una regola | 293 | 2 | 14727 | 2022-07-22T19:02:26 | 2022-07-22T19:02:26 |
| `294` | Esclusa da una regola | 294 | 2 | 14728 | 2022-07-22T19:07:59 | 2022-07-22T19:07:59 |
| `295` | Esclusa da una regola | 295 | 2 | 14879 | 2022-07-23T10:57:47 | 2022-07-23T10:57:47 |
| `296` | Esclusa da una regola | 296 | 9 | 14896 | 2022-07-23T11:57:14 | 2022-07-23T11:57:14 |
| `297` | Esclusa da una regola | 297 | 26 | 14918 | 2022-07-23T13:16:52 | 2022-07-23T13:16:52 |
| `298` | Esclusa da una regola | 298 | 69 | 14927 | 2022-07-23T13:40:06 | 2022-07-23T13:40:06 |
| `299` | Esclusa da una regola | 299 | 2 | 14968 | 2022-07-23T16:40:41 | 2022-07-23T16:40:41 |
| `300` | Esclusa da una regola | 300 | 28 | 14984 | 2022-07-23T17:56:05 | 2022-07-23T17:56:05 |
| `301` | Esclusa da una regola | 301 | 14 | 14985 | 2022-07-23T18:05:25 | 2022-07-23T18:05:25 |
| `302` | Esclusa da una regola | 302 | 6 | 14989 | 2022-07-23T18:40:03 | 2022-07-23T18:40:03 |
| `303` | Esclusa da una regola | 303 | 2 | 14990 | 2022-07-23T18:42:25 | 2022-07-23T18:42:25 |
| `304` | Esclusa da una regola | 304 | 2 | 15300 | 2022-07-25T12:11:26 | 2022-07-25T12:11:26 |
| `305` | Esclusa da una regola | 305 | 42 | 15502 | 2022-07-26T19:06:46 | 2022-07-26T19:06:46 |
| `306` | Esclusa da una regola | 306 | 29 | 15507 | 2022-07-26T20:54:08 | 2022-07-26T20:54:08 |
| `307` | Esclusa da una regola | 307 | 20 | 15627 | 2022-07-27T19:23:07 | 2022-07-27T19:23:07 |
| `308` | Esclusa da una regola | 308 | 15 | 15686 | 2022-07-28T09:39:27 | 2022-07-28T09:39:27 |
| `309` | Esclusa da una regola | 309 | 8 | 15718 | 2022-07-28T14:13:54 | 2022-07-28T14:13:54 |
| `310` | Esclusa da una regola | 310 | 2 | 15733 | 2022-07-28T16:31:03 | 2022-07-28T16:31:03 |
| `311` | Esclusa da una regola | 311 | 2 | 15750 | 2022-07-28T19:35:18 | 2022-07-28T19:35:18 |
| `312` | Esclusa da una regola | 312 | 2 | 15767 | 2022-07-29T07:36:33 | 2022-07-29T07:36:33 |
| `313` | Esclusa da una regola | 313 | 44 | 15803 | 2022-07-29T08:54:17 | 2022-07-29T08:54:17 |
| `314` | Esclusa da una regola | 314 | 2 | 15834 | 2022-07-29T10:20:16 | 2022-07-29T10:20:16 |
| `315` | Esclusa da una regola | 315 | 8 | 15870 | 2022-07-29T13:12:15 | 2022-07-29T13:12:15 |
| `316` | Esclusa da una regola | 316 | 4 | 15920 | 2022-07-29T19:43:39 | 2022-07-29T19:43:39 |
| `317` | Esclusa da una regola | 317 | 28 | 15921 | 2022-07-29T19:50:37 | 2022-07-29T19:50:37 |
| `318` | Esclusa da una regola | 318 | 25 | 15927 | 2022-07-30T06:55:26 | 2022-07-30T06:55:26 |
| `319` | Esclusa da una regola | 319 | 15 | 15946 | 2022-07-30T07:23:56 | 2022-07-30T07:23:56 |
| `320` | Esclusa da una regola | 320 | 56 | 16058 | 2022-07-30T11:10:13 | 2022-07-30T11:10:13 |
| `321` | Esclusa da una regola | 321 | 32 | 16088 | 2022-07-30T13:32:18 | 2022-07-30T13:32:18 |
| `322` | Esclusa da una regola | 322 | 4 | 16096 | 2022-07-30T14:10:58 | 2022-07-30T14:10:58 |
| `323` | Esclusa da una regola | 323 | 4 | 16098 | 2022-07-30T14:14:02 | 2022-07-30T14:14:02 |
| `324` | Esclusa da una regola | 324 | 24 | 16101 | 2022-07-30T14:27:08 | 2022-07-30T14:27:08 |
| `325` | Esclusa da una regola | 325 | 6 | 16124 | 2022-07-30T15:48:55 | 2022-07-30T15:48:55 |
| `326` | Esclusa da una regola | 326 | 5 | 16143 | 2022-07-30T16:29:57 | 2022-07-30T16:29:57 |
| `327` | Esclusa da una regola | 327 | 4 | 16161 | 2022-07-30T17:41:05 | 2022-07-30T17:41:05 |
| `328` | Esclusa da una regola | 328 | 2 | 16170 | 2022-07-30T21:36:19 | 2022-07-30T21:36:19 |
| `329` | Esclusa da una regola | 329 | 2 | 16172 | 2022-07-30T22:47:05 | 2022-07-30T22:47:05 |
| `330` | Esclusa da una regola | 330 | 2 | 16173 | 2022-07-30T23:27:23 | 2022-07-30T23:27:23 |
| `331` | Esclusa da una regola | 331 | 15 | 17788 | 2022-08-10T11:57:34 | 2022-08-10T11:57:34 |
| `332` | Esclusa da una regola | 332 | 20 | 17842 | 2022-08-10T20:40:05 | 2022-08-10T20:40:05 |
| `333` | Esclusa da una regola | 333 | 15 | 17936 | 2022-08-11T11:38:50 | 2022-08-11T11:38:50 |
| `334` | Esclusa da una regola | 334 | 9 | 17997 | 2022-08-11T16:46:50 | 2022-08-11T16:46:50 |
| `335` | Esclusa da una regola | 335 | 8 | 18021 | 2022-08-12T07:41:11 | 2022-08-12T07:41:11 |
| `336` | Esclusa da una regola | 336 | 8 | 18031 | 2022-08-12T08:18:28 | 2022-08-12T08:18:28 |
| `337` | Esclusa da una regola | 337 | 59 | 18059 | 2022-08-12T10:33:16 | 2022-08-12T10:33:16 |
| `338` | Esclusa da una regola | 338 | 41 | 18109 | 2022-08-12T15:40:11 | 2022-08-12T15:40:11 |
| `339` | Esclusa da una regola | 339 | 8 | 18121 | 2022-08-12T16:41:11 | 2022-08-12T16:41:11 |
| `340` | Esclusa da una regola | 340 | 39 | 18212 | 2022-08-13T10:10:05 | 2022-08-13T10:10:05 |
| `341` | Esclusa da una regola | 341 | 18 | 18303 | 2022-08-13T20:23:43 | 2022-08-13T20:23:43 |
| `342` | Esclusa da una regola | 342 | 32 | 18304 | 2022-08-13T20:25:56 | 2022-08-13T20:25:56 |
| `343` | Esclusa da una regola | 343 | 28 | 18419 | 2022-08-14T16:02:18 | 2022-08-14T16:02:18 |
| `344` | Esclusa da una regola | 344 | 82 | 18451 | 2022-08-14T20:54:52 | 2022-08-14T20:54:52 |
| `345` | Esclusa da una regola | 345 | 13 | 18455 | 2022-08-15T06:31:06 | 2022-08-15T06:31:06 |
| `346` | Esclusa da una regola | 346 | 13 | 18456 | 2022-08-15T06:35:52 | 2022-08-15T06:35:52 |
| `347` | Esclusa da una regola | 347 | 19.71 | 18613 | 2022-08-16T10:30:50 | 2022-08-16T10:30:50 |
| `348` | Esclusa da una regola | 348 | 16.64 | 18626 | 2022-08-16T12:38:58 | 2022-08-16T12:38:58 |

> Mostrate le prime **300** di **3487**. Le restanti 3187 sono in [`dati/payment_transaction_charges.jsonl`](dati/payment_transaction_charges.jsonl), con tutte le colonne sorgente.
