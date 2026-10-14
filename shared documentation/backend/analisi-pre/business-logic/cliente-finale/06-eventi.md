# cliente finale — Eventi

> **Ambito**: la lista pubblica degli **eventi** organizzati da uno stabilimento (serate, tornei,
> feste) con calendario e schede, mostrata sulla scheda stabilimento e in una pagina dedicata.
>
> **Metodo**: `front-customer-pre/src/components/consumer/propertyDetails/MarketingEvents.js` +
> `marketingEvent.controller.js` (`getEventList`, `getEventById`) + `Service/marketingEventService.js`.
> Schede DB: [`marketing_event.md`](../../db/marketing_event.md),
> [`marketing_event_notification.md`](../../db/marketing_event_notification.md).

---

## 1. Elenco eventi di uno stabilimento

**Azione** — `POST /api/sb/events` (`getEventList`, richiede `propertyId`): il visitatore vede gli
eventi futuri/in corso dello stabilimento, in formato calendario + card.

**Logiche**:

- Gli eventi ricorrenti sono già stati **espansi in occorrenze** al momento della creazione (una riga
  per data, `startDate = endDate = <data occorrenza>` — vedi
  [../stabilimento/09-marketing](../stabilimento/09-marketing.md) §5).
- La lista filtra per `propertyId` e, tipicamente, per data ≥ oggi; ordina per data.

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 2. Dettaglio evento

**Azione** — `GET /api/sb/events/:id` (`getEventById`): scheda dell'evento con descrizione, data/ora,
immagine, eventuale collegamento allo stabilimento.

**Logiche**: raw SQL con `JSON_ARRAYAGG` delle notifiche associate (`marketing_event_notification`,
`status = 1`) — ma le notifiche **non vengono mai inviate** (feature incompleta, vedi
[`marketing_event_notification.md`](../../db/marketing_event_notification.md)).

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 3. Nessuna azione transazionale

Il cliente **non può iscriversi/prenotare** un evento da questa area nel codice attuale: è solo
consultazione. Non esiste un flusso di "partecipazione" o "biglietto" lato backend.

---

## 4. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: nessuno.
- **Logiche**: eventi già espansi in occorrenze; filtro per `propertyId` e data.
- **Eventi**: **nessuno** (sola lettura pubblica). La feature "notifiche pianificate evento" è
  presente nei dati ma priva di mittente.
