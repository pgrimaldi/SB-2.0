# cliente finale — Scheda stabilimento

> **Ambito**: la pagina pubblica di un singolo stabilimento (rotta `/property-details/:slug`): dati e
> foto, servizi, mappa spiaggia interattiva con lo stato delle postazioni, listini/pacchetti
> disponibili online, opzioni di pagamento, banner sconto Revenue, avvio della prenotazione.
>
> **Metodo**: `front-customer-pre/src/components/consumer/propertyDetails/*` +
> `sysadmin.controller.js:propertyAndBathHousedetails` (`getPropertyWithBathHouseDetails`) +
> `property.controller.js` (`getOnlineBookingSettings`, `getPaymentOptions`) +
> `sysadmin.controller.js:getOnlineReservationStatusByAdmin` + `booking.controller.js`
> (`getbookingLocationData`, `checkLocationIsAvailable`, `getBookedLocation`, `getAbsenceDates`,
> `getSharedLocationsByDate`, `location/availability`) + `configuration`/`confimaps`/`activeconfimages`
> (render mappa) + `pricelist.controller.js` (`allpricelist`, `seasonalprice`) +
> `seasons.controller.js` (`viewseasonsperiods`) + `revenue.controller.js` (`fetchNotificationDetails`).
> Schede DB: [`bathouses.md`](../../db/bathouses.md), [`properties.md`](../../db/properties.md),
> [`configurations.md`](../../db/configurations.md), [`online_reservations.md`](../../db/online_reservations.md),
> [`revenue_notifications.md`](../../db/revenue_notifications.md).

---

## 1. Caricamento della scheda

**Azione** — `POST getPropertyWithBathHouseDetails` (`propertyId`, `bathHouseId`, `userId` opzionale).

**Logiche** (vedi anche [systemadmin/01](../systemadmin/01-gestione-stabilimenti.md) §3.2, stesso
endpoint):

- Recupera `properties` + `bathouses` + titolare + configurazione prenotazione online
  (`online_reservations`: `isEnableMap`, `period`, `locations`, `isDisable`) + presenza configurazione
  mappa (`isConfiguration`).
- **Mascheramento**: se la richiesta **non** è autenticata, username/email/nome/cognome del titolare
  sono restituiti **mascherati** (`maskEmail`/`maskName`); se autenticata, in chiaro.
- Se è passato `userId`, calcola il flag `isLike` (preferito) per quello stabilimento.
- Restituisce anche `paymentOptions` (parsato), `period`, `locations`,
  `isAdminDisableOnLinePayment`.

**Calcoli**: solo parsing/mascheramento. **Eventi**: nessuno.

---

## 2. Stato "prenotazione online abilitata"

**Azioni** — `getOnlineReservationStatusByAdmin` (`propertyId`), `getOnlineBookingSettings`,
`getPaymentOptions`.

**Logiche**:

- `isAdminDisableOnLinePayment` = `true` se `properties.superAdminOnlineFlag = 1` (attenzione alla
  semantica potenzialmente invertita — vedi [`da-chiarire.md`](../da-chiarire.md)); combinato con
  `online_reservations.isDisable`, decide se mostrare/abilitare il flusso di prenotazione online.
- **Opzioni di pagamento** (`properties.paymentOptions` / `servicebookingPaymentOptions`): quali
  modalità sono offerte al cliente — "paga ora tutto", "acconto (metà)", "paga in struttura" — con i
  relativi testi informativi.
- **`allowOnlineBooking`** (`onlineBookingOptions.locationOptionObj`): `"all"` = chiunque; `"contacts_only"`
  = solo contatti già registrati dallo stabilimento (verificato lato server da `checkContactInProperty`
  al momento della prenotazione — vedi [04-prenotazione-postazioni](04-prenotazione-postazioni.md) §2).

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 3. Mappa spiaggia interattiva (lato cliente)

**Azioni** — `viewconfiguration`/`viewconfimaps` (layout + icone), `activeconfimages` (icone attive
per categoria), `getbookingLocationData` / `get-booked-location` / `checkLocationIsAvailable` /
`location/availability` / `checkLocationIsAvailable` / `getAbsenceLocationByDate` /
`getSharedLocationsByDate` (stato celle per data).

**Logiche**:

- Rende la griglia postazioni con lo **stato per la data selezionata**: libera / occupata / in
  assenza / condivisa (sharing). Per il cliente sono selezionabili solo le postazioni **libere** e,
  se lo stabilimento lo prevede, solo quelle in `online_reservations.locations`.
- Il ruolo `affiliate` (se il visitatore arriva da un canale affiliato) può vedere solo il
  sottoinsieme di postazioni assegnate — vincolo **solo lato client** (vedi [`roles.md`](../../db/roles.md#11--affiliate)).

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 4. Listini e pacchetti disponibili online

**Azioni** — `allpricelist`, `pricelistdetail`, `seasonalprice`, `viewseasonsperiods`,
`getpropertyDiscount`.

**Logiche**: mostra i pacchetti con `details.enableOnlineReservation = true` e gli articoli
aggiuntivi online‑abilitati per il periodo delle date scelte; il prezzo mostrato è calcolato dal
**motore di prezzo lato client** (variante `front-customer-pre`, vedi
[`../stabilimento/_motore-di-prezzo-e-sconti.md`](../stabilimento/_motore-di-prezzo-e-sconti.md)),
inclusi eventuali **prezzi ridotti per fascia** (`getReducedPrices` / `details.setReducedPrice`).

**Calcoli**: prezzo pacchetto/articolo per le date scelte (client). **Eventi**: nessuno.

---

## 5. Banner sconto (Revenue)

**Azione** — `fetchNotificationDetails` (`revenue_notifications`): se `is_price_discount_enabled = 1`
e l'ora corrente cade in uno degli slot giornalieri **e** il contatore giornaliero non è esaurito, il
sito mostra un **banner sconto** con il prezzo suggerito/scontato per le postazioni in
`selectedLocations`. La logica di slot orario e contatore è **lato client** (vedi
[../stabilimento/10-revenue](../stabilimento/10-revenue.md) §3).

**Calcoli**: suddivisione fascia 10–20 in `dailyNotificationsCount` slot; confronto contatore.
**Eventi**: incremento lato client di `dailyNotificationsDisplayedCount`.

---

## 6. Avvio prenotazione

Dalla scheda il cliente sceglie date, postazione/i, pacchetto, servizi, e prosegue al checkout — vedi
[04-prenotazione-postazioni](04-prenotazione-postazioni.md),
[05-prenotazione-servizi-e-listino](05-prenotazione-servizi-e-listino.md),
[07-pagamento](07-pagamento.md).

---

## 7. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: prezzo pacchetto/articolo per le date scelte (motore lato client, con prezzi ridotti
  per fascia); slot orari e contatore del banner sconto.
- **Logiche**: mascheramento contatti titolare se non autenticato; visibilità/abilitazione del flusso
  online da `superAdminOnlineFlag` + `isDisable`; postazioni selezionabili = libere ∩ `locations`
  online; `allowOnlineBooking` `all`/`contacts_only`; restrizioni affiliato (solo client).
- **Eventi**: **nessuno** (sola lettura); unico effetto client‑side: incremento contatore banner.
