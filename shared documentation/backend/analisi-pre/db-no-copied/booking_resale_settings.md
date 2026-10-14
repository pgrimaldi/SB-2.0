# Righe non migrate — `booking.resale_settings`

> Modulo **booking** · sorgente legacy **`configurations`** · **1 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/booking_resale_settings.jsonl`](dati/booking_resale_settings.jsonl).

## Diagnosi

### Esclusa da una regola — 1 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
not propertyId
```

**Cosa significa** — Impostazioni di rivendita non collegate ad alcuno stabilimento.

**Causa a monte** — Nel nuovo schema `property_id` e' `NOT NULL`: una configurazione senza stabilimento non ha una destinazione possibile, non e' un dato incompleto ma un dato senza soggetto.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. Verificare solo, se qualcuno se lo chiede, che nel legacy quella riga non fosse usata da nessuna interfaccia.

## Righe

| id legacy | motivo | `id` | `beachMapData` | `createdBy` | `createdAt` | `updatedAt` | `sharing_enable` |
|---|---|---|---|---|---|---|---|
| `106` | Esclusa da una regola | 106 | [{"row":1,"rec":[{"empty_item_image":"https://d26kxhigi9o4u… | 269 | 2022-05-19T13:46:49 | 2022-05-19T13:54:31 | 0 |
