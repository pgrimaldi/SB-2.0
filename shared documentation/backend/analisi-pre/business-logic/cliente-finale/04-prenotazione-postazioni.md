# cliente finale — Prenotazione postazioni (ombrelloni/lettini)

> **Ambito**: il flusso con cui il cliente prenota online una o più postazioni con un pacchetto, per
> un intervallo di date, con eventuale sconto/voucher, e sceglie come pagare. Il pagamento vero è in
> [07-pagamento](07-pagamento.md).
>
> **Metodo**: `front-customer-pre/src/components/consumer/propertyDetails/*` (`Summary.js`,
> `PropertySignIn.js`, `CheckoutForm.js`) + `services/BookingCalcService.js` + `services/ArithmaticService.js`
> (`calculateBookingTotal`) + `services/voucherServices.js` + `booking.controller.js` (`booking`,
> `checkLocationIsAvailable`, `checkAvailabilityRow`, `confirmBookingByReservationCode`,
> `getBookingDetailsByReservationCode`) + `discounts.controller.js` (`checkdiscount`) +
> `customers.controller.js` (`isUserBlacklisted`). Motore di prezzo:
> [`../stabilimento/_motore-di-prezzo-e-sconti.md`](../stabilimento/_motore-di-prezzo-e-sconti.md).

---

## 1. Composizione della prenotazione e calcolo del totale (client)

Il cliente sceglie date, postazione/i, pacchetto, articoli aggiuntivi, eventuali servizi; il totale è
calcolato **interamente nel browser**:

- **Prezzo pacchetto/postazione**: motore di prezzo (`getPackagePriceCalc` e derivate) — combinazioni
  per periodo, scaglioni 7/14/mensile, sovrapprezzo weekend+festivi, mezza giornata, deroghe per data,
  prezzi ridotti per fascia, prezzo suggerito Revenue, ridistribuzione cumulativa multi‑postazione.
- **Commissione online** (`ArithmaticService.calculateBookingTotal`): si sceglie il tasso —
  `commissionAmount.iframeCommission` se la prenotazione avviene dentro un **iframe** su sito terzo
  (`isIframe === "iframe"`), altrimenti `commissionAmount.onlineCommission`; fallback `?? 6`. L'importo
  commissione è:
  - se **acconto** (metà): `(prezzoBase / 2) × percentuale / 100 + 0,50`;
  - se **paga ora tutto**: `prezzoBase × percentuale / 100 + 0,50`;
  - se **paga in struttura**: `0`.
  - **5 `propertyId` sono esenti** da commissione per una lista hardcoded nel bundle del sito (vedi
    [`properties.md`](../../db/properties.md#commissionamount)).
- **Sconto/voucher**: `checkdiscount` (backend) valida esistenza codice, validità temporale, utilizzi
  residui; l'importo scontato (fisso o `totale × valore/100`) è calcolato **lato client**
  (`voucherServices.js`) e sommato nel totale.
- Il risultato (`bookingTotalPrice`, `payableAmount`, `toPay`, `commissionAmount`, `subTotal`,
  `isCumstomerBookPrice`) finisce in `booking_payments` inviato al backend.

**Calcoli**: tutti sopra. **Eventi**: nessuno in questa fase.

---

## 2. Validazioni lato server alla creazione

**Azione** — `POST /api/sb/booking` con `bookingBy != 1` (cliente).

**Logiche** (vedi anche [../stabilimento/01-mappa](../stabilimento/01-mappa.md) §2):

1. `propertyId`, `bookingBy` obbligatori; per il cliente serve `name` + almeno uno fra `email`/`mobile`.
2. **Blacklist**: se esiste un `customers` con quell'email/telefono su quella property e
   `isBlackList = 1` → blocco "User is blacklisted".
3. **Contatto registrato** (`checkContactInProperty`): se `allowOnlineBooking = "contacts_only"` e non
   esiste un `customers` con **email e telefono coincidenti** (`isDelete = false`) → blocco "Only
   contacts registered by the property can make a booking".
4. **Disponibilità magazzino** (`checkItemsAvailability`): somma articoli richiesti vs giacenze; se
   insufficiente → blocco "Insufficient inventory". (Il controllo di disponibilità **postazione** non
   è atomico — vedi [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §11.7.)
5. **`payment_status`**: `2` (in attesa/acconto) viene convertito internamente a `3`; `1` = pagato.

**Persistenza**: crea le righe `bookings` (una per postazione), `booking_consumer`, `booking_payments`
(importi **as‑is** dal client), `booking_locations`; genera `bookingNumber` e un **`reservation_code`**
(codice prenotazione mostrato al cliente); `is_booking_confirmed` iniziale secondo il flusso
(`confirm_request`).

**Calcoli**: nessun ricalcolo del prezzo. **Eventi**: vedi §3.

---

## 3. Eventi alla creazione della prenotazione online

- **Notifica in‑app allo stabilimento** (`createNotification`, `type = 1`): testo
  `"<nome> - Online, <dd-mm-yyyy> - <dd-mm-yyyy> - <postazioni>"`, con `status = 1` se confermata,
  `status = 2` se in attesa.
- **socket.io**: evento su canale `property_<id>` ("Booking created successfully") per aggiornare la
  mappa/planner dello stabilimento in tempo reale.
- **Email di conferma** (solo se `bookingByAdmin == 0`, `payment_status == 1`, nuova prenotazione):
  `emailHelper.sendEmailToCustomerOnBooking` invia al **cliente** e agli **indirizzi dello
  stabilimento** (`PropertyHelper.propertyEmailList`), oggetto "Thank you! Your booking is confirmed"
  (IT/EN), con `useFulInfo` dalle impostazioni online e, per lo staff, un **QR code**
  (`generateQrCode`) con il codice prenotazione. Riga in `email_logs`.
- **Voucher**: se è stato usato un voucher, il contatore `totalNoOfUsage` della voce corrispondente
  in `discounts.items` viene incrementato (aggiornamento del JSON).
- **Nessun invio WhatsApp** in questo flusso (a differenza della prenotazione servizio online).

---

## 4. Conferma tramite codice prenotazione

**Azione** — `POST confirmBookingByReservationCode` (`reservation_code`, `languageCode`).

**Logiche**: trova le prenotazioni con quel `reservation_code`; imposta `is_booking_confirmed = 1` su
tutte. **Eventi**: invia **email** "Your booking is confirmed" al cliente + indirizzi stabilimento
(`sendEmailToCustomerOnBooking`) + `email_logs`.

`getBookingDetailsByReservationCode` / `getBookingByReservationCode` restituiscono il dettaglio a
partire dal codice (per la pagina di riepilogo pubblica `/onlinebooking/consumer/:language/:code`).

---

## 5. Regole di disponibilità e limiti

- **Disponibilità postazione**: `checkLocationIsAvailable` / `checkAvailabilityRow` sono endpoint di
  pre‑check ma **non** richiamati internamente da `booking` prima dell'INSERT → possibile doppia
  prenotazione concorrente.
- **Limiti di soggiorno per settore**: min/max giorni verificati **solo lato client**
  (`configureServices.js`); l'endpoint server `check-bookinglimit` esiste ma non è mai chiamato.

---

## 6. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: prezzo pacchetto/postazione (motore client); commissione online (tasso iframe/online,
  `± /2` per acconto, `+ 0,50`, esenzioni hardcoded); sconto fisso/percentuale; totale/`toPay`.
- **Logiche**: blacklist; contatto registrato (`contacts_only`); disponibilità magazzino;
  `payment_status 2 → 3`; `is_booking_confirmed`; incremento `totalNoOfUsage` voucher; limiti settore
  solo client; disponibilità postazione non atomica.
- **Eventi**: **notifica in‑app** allo stabilimento; **socket.io**; **email di conferma** al cliente +
  stabilimento (con QR per lo staff) + `email_logs`; incremento contatore voucher. Pagamento e sue
  chiamate Stripe: [07-pagamento](07-pagamento.md).
