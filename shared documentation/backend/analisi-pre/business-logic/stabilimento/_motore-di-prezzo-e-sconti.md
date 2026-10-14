# stabilimento — Motore di prezzo e sconti (documento trasversale)

> **Non è una voce di menù**: è il riepilogo, in un unico posto, di **come vengono calcolati gli
> importi** di una prenotazione (postazioni + pacchetti + articoli aggiuntivi + servizi) e di **come si
> applicano sconti e voucher**. Le voci di menù [01-mappa](01-mappa.md), [02-planner](02-planner.md),
> [03-cassa](03-cassa.md) e [05-prenotazioni](05-prenotazioni.md) rimandano qui per la parte "calcoli".
>
> **Fatto strutturale (verificato, vedi [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md)
> §11.3–11.5)**: **tutto il calcolo del prezzo e degli sconti avviene nel browser**. Il backend
> **salva e riusa** gli importi ricevuti dal client (`payableAmount`, `toPay`, `TotalSumItem`,
> `booking_payments`, `booking_discount`, …) **senza ricalcolarli**. Non esiste, in nessun punto della
> catena (booking admin, prenotazione online cliente, API mobile), un ricalcolo server‑side. Nella
> riscrittura questo motore va portato lato server come servizio di dominio unico (`IPricingEngine`), con
> il client che tiene al più una copia "ottimistica" per la reattività.
>
> **Fonti**: `front-pre/src/services/BookingCalcService.js` (~1650 righe: motore prezzo postazioni/
> pacchetti), `front-pre/src/services/voucherServices.js` (sconti/voucher),
> `front-pre/src/services/ArithmaticService.js` (utilità numeriche), `front-pre/src/services/configureServices.js`
> (limiti di soggiorno), `front-customer-pre/src/services/BookingCalcService.js` (variante lato cliente,
> con in più `getReducedPrices`). Backend: `discounts.controller.js` (`checkdiscount`), `booking.helper.js`
> (`Booking`, salvataggio importi). Schede DB: [`price_lists.md`](../../db/price_lists.md),
> [`discounts.md`](../../db/discounts.md), [`seasons_periods.md`](../../db/seasons_periods.md),
> [`sectors_rows.md`](../../db/sectors_rows.md), [`warehouses.md`](../../db/warehouses.md).

---

## 1. Dati di ingresso del calcolo

- **Pacchetto** (`price_lists`, `type = "package"`): per ogni **periodo tariffario** (`detailPrices[]`,
  es. "Alta stagione") sono definiti: `half_day_price`, `half_day_weekend_price`, `daily_price`,
  `weekend_price`, `days_7_price`, `days_14_price`, `monthly_price`, più `edit_price_data[]` (deroghe di
  prezzo su singole date). `mainPrices[]` dà un prezzo forfettario per settore, usato come
  fallback/riferimento.
- **Metodo di calcolo** (`price_lists.priceCalculation`): valore stringa "sporco" con varianti storiche;
  i concetti reali sono **`proportional`** (proporzionale ai giorni), **`daily`** (a giornata),
  **`Daily price with weekend`** (a giornata con sovrapprezzo weekend) e **`logarithmic`** (metodo
  distinto, poco usato). Da consolidare in una lookup canonica.
- **Impostazione weekend** (`price_lists.weekendSetting`): quali giorni contano come weekend
  (sab+dom, solo dom, …).
- **Impostazione mensile** (`price_lists.monthlySetting`): come si conta "un mese" ai fini dello
  scaglione mensile (durata effettiva del mese vs "monthly" fisso).
- **Festività** (`price_lists.allowPublicHolidays` + `holidayDates[]`): se attive, i giorni festivi
  sono trattati come weekend ai fini del sovrapprezzo.
- **Periodi stagione** (`seasons_periods.periods[]`): il periodo di una data si determina per **match di
  sottostringa** fra `detailPrices[].name` e `periods[].name` (accoppiamento debole per stringa).
- **Assenze** (`booking_absences` / date di assenza): giorni in cui la postazione **non** è occupata dal
  cliente, da **scalare** dal conteggio.
- **Articoli del pacchetto** (`details.PackageItemSelected[]`): articoli di magazzino
  (`warehouses.items`) inclusi, con quantità — usati per il **controllo disponibilità**, non per il
  prezzo.
- **Articoli aggiuntivi** (`price_lists`, `type = "additional"`): venduti a parte, eventualmente
  vincolati a specifici pacchetti (`selectedPackages`).

## 2. Calcolo del prezzo di una singola postazione/pacchetto (per periodo)

Funzione centrale: `getPackagePriceCalc` (per ogni combinazione riga/periodo), che a sua volta usa:

1. **Suddivisione in combinazioni periodo** (`setRowSeasonCombination`): l'intervallo prenotato è
   spezzato nei sotto‑intervalli che cadono in periodi tariffari diversi; ogni sotto‑intervallo è
   calcolato con i prezzi del proprio periodo e poi sommato.
2. **Conteggio giorni** (`getNoOfDaysFunc`): giorni dell'intervallo, **estremi inclusi**; poi si
   **sottraggono i giorni di assenza** (`getAbsenceCount`, deduplicati per data e per intervallo di
   pari durata).
3. **Scaglioni a durata** (`getRemainingDays`): se la durata copre **un mese intero** e c'è
   `monthly_price` → si applica la tariffa mensile ai primi N giorni e si prosegue sul residuo; altrimenti
   se ≥ 14 giorni e c'è `days_14_price` → tariffa 14 giorni sui primi 14 e residuo; altrimenti se ≥ 7 e
   c'è `days_7_price` → tariffa 7 giorni sui primi 7 e residuo. Il residuo torna al calcolo giornaliero.
4. **Weekend e festivi** (`calcHDWPrice` + `getWeekendsCount` + `getHolidayDateDays`): si contano i
   giorni di weekend (secondo `weekendSetting`) e i festivi (esclusi quelli già contati come weekend);
   per quei giorni si applica la tariffa "weekend" (piena o metà giornata) al posto della standard.
   Formula: `giorni_feriali = giorni_totali − giorni_weekend − giorni_festivi`; il prezzo weekend/festivo
   è `numero_giorni_speciali × tariffa_weekend`, sommato al prezzo dei giorni feriali.
5. **Mezza giornata** (`bookingPeriod` = mattina/pomeriggio): usa `half_day_price` /
   `half_day_weekend_price` invece delle tariffe piene.
6. **Deroghe su singola data** (`edit_price_data` / `getSelectedRecPrice`): se una data prenotata ha un
   prezzo override, quello prevale sul calcolo standard per quel giorno.
7. **Prezzo ridotto per fascia** (solo lato cliente, `getReducedPrices` + `details.setReducedPrice[]`):
   se la data rientra in una fascia a prezzo ridotto, si usa il valore ridotto.
8. **Prezzo suggerito Revenue** (`getMatchedSuggestedPrice` + `price_lists.suggestedPriceDetails` /
   `revenue_notifications`): se attivo, il prezzo base può essere sostituito dal prezzo suggerito
   dinamico calcolato dalla feature Revenue (vedi [10-revenue](10-revenue.md)).

## 3. Prezzo cumulativo multi‑postazione

Quando la stessa prenotazione ha **più postazioni** collegate (gruppo), il prezzo non è la semplice
somma: `applyCumulativePricingToFinalLocData` / `getDistributedGroupLocationPrice` /
`getCumulativePriceForTotalDays` **ridistribuiscono** un prezzo cumulativo sul totale dei giorni‑
postazione, arrotondando ogni quota (`roundCumulativeDistributedPrice`) e ri‑sincronizzando il prezzo su
ogni voce (`syncPackagePriceOnLocItem`, `syncAdditionalPriceOnLocItem`). La logica ha una chiave di
raggruppamento (`getCumulativeGroupKey`, `getAdditionalCumulativeGroupKey`) e un "trigger key" per
capire quando ricalcolare (`getCumulativePricingTriggerKey`, `hasCumulativeFinalLocPricingChanges`).
Il risultato per‑postazione confluisce in `getFinalLocData` e nella somma complessiva
(`calcAllLocationSumsFromFinalLocData`, `sumPackagePricesFromLocArray`).

## 4. Articoli aggiuntivi e servizi

- **Articoli aggiuntivi**: `recalculateAdditionalItemPrices` ricalcola i prezzi degli additional item
  in base a giorni/date e all'eventuale cumulativo; possono essere legati a un pacchetto specifico.
- **Servizi bar / normali / a ore** (`voucherServices.js`):
  - bar: `Σ (prezzo_servizio × quantità) + somma varianti selezionate`
  - normali: `Σ (prezzo_servizio × quantità)`
  - a ore: `Σ totalPrice` di ogni slot; gli slot orari sono validati **solo lato client**
    (`checkAndAddTimeSlot`: capienza `maximumReservation`, sovrapposizione) — il backend non ricontrolla.

## 5. Sconti e voucher

### 5.1 Validazione (backend — `POST` `checkdiscount`)

Il backend fa **solo controlli di validità**, non calcola l'importo:

1. Esiste una riga `discounts` per quel `propertyId`? (altrimenti "discounts not found")
2. Il **codice** esiste dentro il JSON `items`? (ricerca `discountCodeSearch`)
3. La **data corrente** rientra nella validità del voucher?
4. `maximunNoUse − totalNoOfUsage > 0` (utilizzi residui). Se sì → restituisce i parametri del voucher
   (`type`, `voucher_value`, righe/postazioni ammesse, ecc.); altrimenti "discount not valid".

### 5.2 Calcolo dell'importo scontato (client — `voucherServices.js`)

- `type = "fixed"` → sconto = `voucher_value` (importo fisso).
- `type` percentuale → sconto = `totale × voucher_value ÷ 100`.
- Per i **servizi** la percentuale è applicata come `totale − (totale × valore ÷ 100)`.
- Lo sconto per postazione è tenuto per chiave‑postazione in un reducer
  (`_checkDiscountAppliedForPackage`, `_checkDiscountForServices`); l'importo scontato cumulato è
  salvato in `localStorage` (`discountAmount`) — **alterabile da DevTools**.
- Il risultato confluisce nel totale inviato e salvato (`booking_discount`, importi in
  `booking_payments`), **senza ricalcolo server‑side**.

### 5.3 Configurazione dei voucher (gestore)

`adddiscounts` (upsert per `propertyId`): salva l'intero array `items` come JSON (codice, tipo,
valore, validità, righe ammesse, `maximunNoUse`, `totalNoOfUsage`, …). `updateVoucherDateRange`
aggiorna solo le date. Vedi [11-configurazione](11-configurazione.md) e
[10-revenue](10-revenue.md) (`associateRevenueDiscount`).

## 6. Limiti di soggiorno per settore

`configureServices.js:_allowBookingLimitCustomer` applica il vincolo **min/max giorni** prenotabili per
settore **solo lato client**. Esiste un endpoint server equivalente (`checkBookingLimit`) ma **non è
chiamato da nessuna parte** (né dal frontend né internamente da `booking`): il vincolo oggi non è
imposto lato server.

## 7. Importi salvati e loro riuso

- Il client invia `data[].booking_payments` (oggetto con `payableAmount`, `toPay`, acconto,
  commissione, sub‑totali, ecc.), `booking_discount`, `TotalSumItem`.
- `booking.helper.js:Booking` li **persiste così come sono** su `bookings`, `booking_payments`,
  `booking_number_informations` (`payments_new`, `details_payments_new`).
- Gli stessi importi sono poi usati da: creazione link/addebito **Stripe** (`stripe.controller.js`),
  scontrino **Fiskaly** (line items), riepiloghi, contabilità/credito.
- Nessuna validazione che `toPay ≤ saldo residuo`, che gli importi non siano `NaN`/`Infinity`, che la
  commissione sia coerente col canale, ecc. Tutti punti raccolti in [`../da-chiarire.md`](../da-chiarire.md).

---

## 8. Riepilogo "calcoli / logiche / eventi" di questo motore

- **Calcoli**: prezzo per periodo (proporzionale / a giornata / con weekend / logaritmico); scaglioni
  7/14/mensile; sovrapprezzo weekend+festivi; mezza giornata; deroghe per data; prezzo ridotto per
  fascia; prezzo suggerito Revenue; ridistribuzione cumulativa multi‑postazione; totali servizi
  bar/normali/orari; sconto fisso/percentuale; commissione online (vedi
  [`properties.md`](../../db/properties.md#commissionamount)).
- **Logiche**: iterazione su combinazioni periodo, su postazioni del gruppo, su date di assenza, su
  slot orari; condizioni su `weekendSetting`/`monthlySetting`/`allowPublicHolidays`; match periodo per
  sottostringa; validità voucher (data + utilizzi residui + righe ammesse).
- **Eventi**: **nessuno** in questo modulo (è puro calcolo). Gli eventi (email, notifiche, Stripe,
  Fiskaly) scattano nelle azioni che *usano* questi importi, descritte nelle singole voci di menù.
