# Tabella `booking_entrances`

> **Dominio**: C1 — `booking` · **schema**: `booking` · **forma canonica**: `booking.entrances`. Creata il 2026-11-05
> (migration `BookingApiFoundation`). Nuova: nel legacy non esiste.
>
> «Segnala ingresso»: il cliente è entrato oggi su questa postazione. Decisioni del committente: ingresso **per
> ombrellone** (si sceglie su quale, A6), si azzera da solo a mezzanotte, lo toglie il gestore; lo useranno solo alcuni
> stabilimenti. Non tocca check-in e check-out, che sono della prenotazione intera.

## Colonne

| Colonna | Tipo | Null | Note |
|---|---|---|---|
| `id` | `bigint` identity | No | PK `pk_booking_entrances` |
| `property_id` | `uuid` | No | `public_id` di `catalog.properties`, copiato |
| `booking_line_id` | `bigint` | No | FK `fk_booking_entrances_line` → `booking.lines`, `ON DELETE CASCADE` |
| `entry_date` | `date` | No | giorno civile italiano dell'ingresso |
| `entered_at` | `timestamptz` | No | quando l'operatore l'ha segnato |
| `operator_user_id` | `uuid` | Sì | `public_id` di `identity.access_users`, dal token |
| `created_at` / `updated_at` | `timestamptz` | No | default `now()` |

## Indici

| Indice | Colonne | Motivo |
|---|---|---|
| `uq_booking_entrances_line_date` | `(booking_line_id, entry_date)` UNIQUE | un ingresso per postazione e giorno |
| `ix_booking_entrances_property_date` | `(property_id, entry_date)` | ingressi del giorno sulla mappa |

## Retention

Cancellazione dopo una stagione da `entry_date` (policy `01a1ac85-013b-717b-8668-8556fc57ef39`, migration Privacy
`AddBookingEntranceRetention`, nasce in prova): dato operativo del giorno con l'operatore che l'ha segnato.

## Migrazione

Nessuna (tabella nuova, vuota dopo l'ETL).
