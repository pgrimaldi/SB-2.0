# stabilimento — Statistiche

> **Voce di menù**: settima icona della sidebar (`_gotoStatistics` → `components/statistics/...`,
> componente `StatisticsParent`, rotta `/statistics`). Cruscotti di **sole letture aggregate** su
> prenotazioni, presenze, incassi, occupazione mappa, servizi, sconti.
>
> **Ruoli**: nascosta a `cashoperator`, `waiter`, `marketing`, `affiliate`, `lifeguard`,
> `advancedlifeguard`.
>
> **Metodo**: action file `statistics.js` + `booking.controller.js`
> (`getStatisticsByPeriod`, `getLocationStatistics(Data|BySector)`, `getPresenceBookings`,
> `getArrivalBookings`, `getDepartureBookings`, `getPaidAndUnpaidBookings`, `getSeasonalBookings`,
> `getUnpaidBookings`, `getBookingCounts`, `bookingTypeCount`) + `property.controller.js`
> (`statistics`, `packageStatisticsQuantity`, `additionalListQuantity`, `barServicesQuantity`,
> `normalServicesQuantity`, `hourlyServicesQuantity`) + `discounts.controller.js`
> (`getBookingDiscountList`).
>
> **Nessun evento in tutta la voce**: nessuna email, notifica, API esterna, job. Tutte le azioni sono
> query di lettura. Di seguito solo le **logiche** (filtri/iterazioni) e i **calcoli** (formule).

---

## 1. Statistiche per periodo tariffario

**Endpoint**: `getStatisticsByPeriod`.

**Logiche**: per ogni **periodo** (alta/bassa/media stagione, da `seasons_periods`) itera su tutte le
prenotazioni e tiene quelle le cui date **ricadono in un intervallo del periodo**.

**Calcoli per periodo**:

- `totalBookings` = numero di prenotazioni del periodo (incremento in ciclo).
- `totalDays` = Σ `(dataFine − dataInizio in giorni) + 1` (estremi inclusi).
- `totalAmount` = Σ `booking_payments.subTotal` (dal JSON già salvato).
- `averageAmount` = `totalAmount ÷ totalBookings` (attenzione: divisione per zero se il periodo non ha
  prenotazioni — bug potenziale, da gestire).

---

## 2. Statistiche di occupazione mappa

**Endpoint**: `getLocationStatisticsData`, `getLocationStatisticsBySector`, `getLocationStatistics`.

**Logiche/calcoli**: per ogni giorno (e per settore) conta le postazioni **occupate** vs **totali**;
tasso di occupazione = `occupate ÷ totali`. Aggregazioni per data/settore/fila. Le date di assenza e
gli hold incidono sul conteggio "occupate".

---

## 3. Presenze, arrivi, partenze

**Endpoint**: `getPresenceBookings`, `getArrivalBookings`, `getDepartureBookings`,
`getYesterdayAndTodayBookings`.

**Logiche**: per una data (o intervallo) elenca/conta le prenotazioni con **check‑in effettuato**
(presenze), quelle che **iniziano** quel giorno (arrivi), quelle che **finiscono** (partenze).

**Calcoli**: conteggi; eventuale confronto ieri/oggi.

---

## 4. Prenotazioni pagate / non pagate / in scadenza

**Endpoint**: `getPaidAndUnpaidBookings`, `getUnpaidBookings`, `getBookingCounts`,
`bookingTypeCount`, `getSeasonalBookings`.

**Calcoli**: per ogni prenotazione, `residuo = totale − incassato` (dagli importi salvati);
classificazione pagata / acconto / non pagata; conteggi per tipo (postazione / servizio / pass
stagionale), per canale (online / operatore), per stato.

---

## 5. Statistiche servizi e articoli

**Endpoint**: `property/packageStatistics` (`packageStatisticsQuantity`),
`property/additionalListQuantity`, `property/barServicesQuantity`,
`property/normalServicesQuantity`, `property/hourlyServicesQuantity`.

**Logiche/calcoli**: per ogni pacchetto / articolo aggiuntivo / servizio bar / servizio normale /
servizio a ore, somma le **quantità vendute** nel periodo (iterazione sulle prenotazioni e sui
`servicesData` / `PackageItemSelected`), con eventuale valore economico associato.

---

## 6. Statistiche sconti / voucher

**Endpoint**: `getBookingDiscountList`, `associateRevenueDiscount` (vedi
[10-revenue](10-revenue.md)).

**Logiche/calcoli**: elenca le prenotazioni con `booking_discount` valorizzato nel periodo; somma
l'importo scontato totale; conta gli utilizzi per codice voucher.

---

## 7. Cruscotto sintetico stabilimento

**Endpoint**: `property/statistics` (`property.controller.js:statistics`).

**Logiche/calcoli**: KPI di sintesi (prenotazioni totali, incasso, occupazione media, ecc.) per la
home del cruscotto, aggregando i dati sopra.

---

## 8. Riepilogo "calcoli / logiche / eventi" della voce Statistiche

- **Calcoli**: conteggi (prenotazioni, giorni, presenze/arrivi/partenze, quantità servizi/articoli,
  utilizzi voucher); somme (`subTotal`, importi, residui, sconti); medie (`totalAmount ÷ totalBookings`);
  tassi di occupazione (`occupate ÷ totali`). Tutti su **importi già salvati** (nessun ricalcolo del
  prezzo di listino).
- **Logiche**: filtri per periodo/data/settore/canale/stato; iterazioni su prenotazioni, giorni,
  postazioni, voci servizio; assegnazione di una prenotazione al periodo tariffario per
  sovrapposizione di date.
- **Eventi**: **nessuno**.
