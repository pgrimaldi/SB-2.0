# API del modulo Booking (e nucleo di Payment)

> Catalogo degli endpoint di `Module.Booking`, prima fase scritta il 2026-11-05: prenotazioni di postazioni dal
> gestionale, mappa e planner, riserve, incassi, check-in, ingressi, cestino e pulizia dati. Con lei il nucleo minimo
> di `Module.Payment` (incassi manuali). Seconda fase (2026-11-06): gruppi e pagamento unico, rinnovo, dividi conto,
> scambio, ricerca smart, ricerca guidata, prenotazione ripetuta, storico della postazione. Regole comuni in [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md)
> e [pre/standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md); dettaglio dei campi nello Swagger
> (*Booking API*, *Payment API*). Decisioni e analisi: `documentazione-locale/booking-da-chiarire.md` (fuori dal repo).

## Regole comuni

- **Prefisso**: `/api/booking` (e `/api/payment`). Token utente obbligatorio.
- **Stabilimento**: sempre dal claim `property_id` del token; senza, 403 `booking.property_required`.
- **Identificativi**: solo `public_id` (uuid). Le postazioni sono gli id di `GET /api/catalog/map`.
- **Liste**: `page`, `pageSize` (10, 20, 25, 50, 100, 200, 300), `sortBy`, `sortDir`, risposta `{ total, page, pageSize, items }`.
- **Operazioni su più elementi**: `{ ids }` (massimo 300), esito `{ succeeded, failed: [{ id, code, args }] }`.
- **Date**: giorni `YYYY-MM-DD`; «oggi» è il giorno civile italiano.
- **Fasce** (`dayPart`): `full_day` (predefinita), `morning`, `afternoon`. La giornata intera occupa entrambe le mezze.
- **Importi**: IVA compresa, calcolati sempre dal backend con il preventivo di Catalog; il client non manda mai un
  totale da salvare, solo le scelte (pacchetto, prezzo scritto a mano, sconto manuale, incremento, prezzo applicato).
- **Audit**: ogni modifica finisce nel registro (categoria `operator`, codici `booking.*` e `payment.*`).

## Permessi

Matrice in `Shared/Security/RolePermissions.cs`, ricavata dal vecchio gestionale (I1: menu, pulsanti e redirect per ruolo
in `front-pre`; il vecchio backend non controllava quasi nulla) e ora controllata dal backend. Il frontend la riceve da
`GET /api/identity/me`.

| Permesso | Ruoli | Serve a |
|---|---|---|
| `bookings.read` | superadmin, admin, user1, manager, cashoperator, dailycashoperator, lifeguard, advancedlifeguard, affiliate | mappa, dettaglio, ricerche, gruppi (lettura) |
| `bookings.list` | superadmin, admin, user1, manager | pagina «Prenotazioni» (`GET /reservations`): la cassa non la vede |
| `bookings.planner` | superadmin, admin, user1, manager, cashoperator, dailycashoperator | planner delle postazioni, dei servizi e dei tavoli |
| `bookings.amounts` | come `bookings.read` tranne lifeguard e advancedlifeguard | vedere gli importi (senza, `amounts` e i prezzi delle righe sono `null`) |
| `bookings.write` | superadmin, admin, user1, manager, cashoperator, dailycashoperator, affiliate | creare, modificare, riserve, sharing e assenze, dividi conto, scambio, allegati |
| `bookings.move` | come `bookings.write` + advancedlifeguard | spostare, gruppi, rinnovo |
| `bookings.online` | superadmin, admin, manager, cashoperator, dailycashoperator | confermare o rifiutare le richieste online |
| `bookings.pricing` | superadmin, admin, manager, cashoperator, dailycashoperator | prezzo scritto a mano, sconto manuale, incremento (403 `booking.pricing_forbidden`); user1 no |
| `bookings.checkin` | cassa + affiliate, advancedlifeguard | check-in, check-out, ingressi (il bagnino semplice no) |
| `bookings.delete` | superadmin, admin, user1, manager, cashoperator, affiliate | eliminare, cestino, ripristino |
| `bookings.cleanup` | superadmin, admin | eliminazione definitiva, svuota cestino, «Cancella dati» |
| `payments.record` | superadmin, admin, user1, manager, cashoperator, dailycashoperator | incassi, spunta «Pagato», annullo di un movimento |

Scelte fatte dove il vecchio gestionale non dice nulla (da confermare, vedi §11 di `booking-da-chiarire.md`): il cassiere
giornaliero (ruolo nuovo) vale come la cassa ma non elimina; cestino e «Cancella dati» (nuovi) come nella tabella;
nessun limite in base al tipo di abbonamento (nel vecchio planner ed elenco erano nascosti con la licenza «basic»).

**Affiliato**: vede e prenota solo le postazioni assegnate in Utenti (`resource_scopes`); valgono sia gli id delle
postazioni sia le posizioni del vecchio gestionale (`R2C17` = fila 2, colonna 17). Postazione non sua: 403
`booking.spot_not_allowed`; prenotazione senza sue postazioni: 404. Non incassa (`payments.record` assente).

## Prenotazioni

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /reservations` | `bookings.list` | elenco: `from`, `to` (prenotazioni che toccano l'intervallo), `status`, `paymentStatus`, `channel`, `agreementPartnerId`, `seasonal`, `spotId` (storico di una postazione), `search` (numero, nome, email o telefono del cliente, nome della postazione); `sortBy` = `from`, `bookingNumber`, `total`, `balanceDue`, `createdAt` |
| `GET /reservations/{id}` | `bookings.read` | dettaglio (anche nel cestino) |
| `POST /reservations` | `bookings.write` | `{ reservation, paidWith }` → 201 |
| `PUT /reservations/{id}` | `bookings.write` | stato completo della prenotazione (stesso corpo di `reservation`) |
| `POST /reservations/{id}/move` | `bookings.move` | `{ lineId, toSpotId, from?, to? }` |
| `DELETE /reservations/{id}?refund=&voidReceipts=` · `POST /reservations/bulk-delete` | `bookings.delete` | nel cestino; `{ ids, refund, voidReceipts }` |
| `POST /reservations/{id}/payments` | `payments.record` | `{ amount, mode, isDeposit, confirmOverBalance }` |
| `POST /reservations/{id}/mark-paid` · `DELETE` | `payments.record` | spunta «Pagato»: `{ mode }` |
| `POST /reservations/{id}/check-in` · `DELETE` · `POST /reservations/{id}/check-out` | `bookings.checkin` | |
| `POST /reservations/{id}/entrances` · `DELETE /reservations/{id}/entrances/{lineId}` | `bookings.checkin` | `{ lineId }`, per oggi |

**Corpo** (`reservation`): `{ customer, from, to, dayPart, seasonal, spots, discountCode, manualDiscount, surcharge,
priceOverride, note, customerTags, guests, confirmBlacklisted }`.

- `customer`: `{ customerId }` (contatto esistente), oppure `{ fullName, email, phoneCountryCode, phone }` (con il solo
  nome si crea sempre un contatto nuovo; con email o telefono si riusa quello che coincide), oppure `{ isGeneric: true }`
  (utente generico, nessun contatto). Obbligatorio alla creazione; nella modifica `null` = invariato.
- Cliente in blacklist: 409 `booking.customer_blacklisted`; si prenota rimandando con `confirmBlacklisted: true`.
- `spots[]`: `{ spotId, from?, to?, priceListId | agreementPriceListId, agreementPartnerId, voucherPriceListId,
  manualPrice, seats, additionals: [{ priceListId, quantity, dates }] }`. Senza date valgono quelle della prenotazione;
  con date proprie (dentro quelle della prenotazione) la prenotazione passa da una postazione all'altra. `seats` solo
  per le aree. Convenzione per postazione e tratto: `agreementPartnerId` con il suo listino o voucher.
- `manualDiscount`, `surcharge`: `{ type: percentage | amount, value, label }`; l'incremento è uno solo, con etichetta.
- **Importi** (B5, B7): codice sconto e sconto manuale si calcolano entrambi sul subtotale; l'incremento si aggiunge;
  `priceOverride` (prezzo applicato) vince su tutto e resta anche cambiando le date. Il prezzo del pacchetto scritto a
  mano (`manualPrice`) si perde se cambiano le date di quella postazione: vale di nuovo il listino.
- `paidWith` (solo creazione): `cash`, `credit_card`, `bank_transfer`, `other` = spunta «Pagato», incasso del totale.

**Modifica**: le postazioni con stessa postazione, date, fascia e posti restano (con i loro consumi e ingressi); le
altre si rifanno. Il prezzo si ricalcola con il listino di oggi, la differenza si applica da sola.

**Spostamento**: una riga (postazione) su un'altra postazione, anche solo per alcuni giorni: la riga si spezza in tre
tratti al massimo; il resto della prenotazione non cambia. Giorni fuori dalla riga: 422 `booking.move_outside_line`.

**Disponibilità**: una postazione a uso esclusivo (ombrellone, lettino, lettino XL, cabina, cabina personalizzata,
tenda, parcheggio) si occupa una volta per giorno e fascia; occupata: 409 `booking.spot_unavailable` con
`args.conflicts: [{ spotId, label, day }]`. Le aree hanno una capienza: 409 `booking.area_full` con `args: { capacity,
days }`. Prenotando sopra una riserva, la riserva si toglie senza conferma. Articoli personalizzati e biglietti non
hanno vincolo. Lo stesso controllo lo fa il database (vincolo di esclusione), per le richieste contemporanee.

**Risposta** (`ReservationDto`): `{ id, bookingNumber, reservationCode, status (pending, confirmed, cancelled),
cancellationReason, channel, isSeasonal, from, to, customer, paymentStatus, amounts, note, noteUpdatedAt, noteUpdatedBy,
customerTags, guests, lines, checkedInAt, checkedOutAt, isTrashed, trashedAt, createdAt, createdBy, updatedAt, warnings,
group, isRenewal, isSplitPart }`.
- `group`: `{ id, name, reservationCount }` se la prenotazione è in un gruppo, altrimenti `null`.
- `paymentStatus`: `paid` (totale 0 o pagato ≥ totale), `unpaid` (nulla pagato), `partial`.
- `amounts`: `{ subtotal, codeDiscount, discountCode, manualDiscount, manualDiscountAmount, surcharge, surchargeAmount,
  priceOverride, total, deposit, paid, balanceDue, creditAmount (pagato in più), prevalentPaymentMode, isMarkedPaid }`.
- `lines[]`: `{ id, lineNumber, spotId, spotLabel, spotType, sectorId, sectorName, from, to, dayPart, seats, isArea,
  priceListId, packageName, manualPrice, amount, agreementPartnerId, agreementPriceListId, voucherPriceListId,
  additionals, enteredToday }`.
- `warnings`: quelli del preventivo (`days_without_period`, `period_without_price`, `seasonal_price_missing`).

**Numero e codice**: `{id dello stabilimento}-{progressivo}` (es. `10611-36164`), contatore continuo creato alla
prima prenotazione; i numeri migrati restano nel formato del vecchio gestionale. Codice di 8 caratteri casuali.

**Incassi**: un incasso oltre il residuo vuole `confirmOverBalance: true` (409 `booking.payment_exceeds_balance` con
`args.balanceDue`). Spunta «Pagato»: incasso del residuo (422 `booking.nothing_to_pay`, 409 `booking.already_marked_paid`);
togliendola si annulla quell'incasso (409 `booking.not_marked_paid`); se era stato emesso lo scontrino il front avvisa
il gestore di annullarlo.

**Check-in e check-out** valgono per l'intera prenotazione. Il check-out, anche anticipato, libera subito la
postazione da oggi compreso, senza rimborso. Togliere il check-in toglie anche il check-out e rioccupa i giorni
liberati (409 se nel frattempo qualcuno li ha presi).

**Ingressi** («Segnala ingresso»): per postazione e per oggi; il giorno dopo non risultano più. Giorno fuori dalla
riga: 422 `booking.day_not_in_reservation`. Togliere un ingresso va nel registro.

## Mappa, planner, riserve

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /map?date=` | `bookings.read` | chi occupa le postazioni nel giorno (oggi senza `date`) |
| `GET /planner?from=&to=` | `bookings.planner` | righe e riserve, al massimo 62 giorni |
| `GET /availability?from=&to=&dayPart=&excludeReservationId=` | `bookings.read` | per ogni postazione prenotabile `{ spotId, status, busyDays }`; `status` = `free` · `held` (occupata solo da riserve: si può prenotare, le riserve si tolgono) · `occupied`; per le aree `busyDays` sono i giorni pieni |
| `POST /holds` | `bookings.write` | `{ spotIds, from, to, dayPart }`, oppure `{ segments: [{ spotId, from, to }], dayPart }` (ricerca smart: ognuna con il suo periodo) → 201 con le riserve |
| `DELETE /holds/{id}` | `bookings.write` | |
| `GET /map/free-ranges?from=&to=&sectorId=&spotType=` | `bookings.read` | ricerca smart: per ogni postazione prenotabile `{ spotId, label, sectorId, spotType, status (free, partial, occupied), freeRanges: [{ from, to }] }` |
| `GET /map/guided-search?from=&to=&dayPart=&spotType=&sectorId=&maxMoves=&nearSpotId=` | `bookings.read` | ricerca guidata del planner: fino a 2 soluzioni `{ moves, segments: [{ spotId, label, sectorId, from, to }] }` |
| `POST /reservations/repeat-plan` | `bookings.write` | prenotazione ripetuta: `{ spotId, from, to, weekdays (1 = lunedì … 7 = domenica), dayPart, excludeReservationId }` → blocchi `{ from, to, busyDays }` |

- Il disegno della mappa (postazioni, file, icone) è di Catalog (`GET /api/catalog/map`): qui solo l'occupazione.
- `MapDayDto.occupants[]`: `{ spotId, dayPart, kind (reservation | hold), reservationId, lineId, holdId, bookingNumber,
  customerName, paymentStatus, balanceDue, isSeasonal, isFirstDay, isLastDay, isCheckedIn, isCheckedOut, enteredToday,
  seats, hasNote }`. Il «punto rosso» è `isFirstDay && !isCheckedIn`.
- Riserve (C1): senza cliente, senza scadenza, senza motivo; online la postazione risulta occupata.
- **Ricerca smart** (bl/04): un giorno è occupato se una prenotazione o una riserva lo tiene, anche solo per mezza
  giornata; le aree quando sono piene. Postazioni non prenotabili escluse; nessun limite di intervallo (dieci anni
  come limite di sicurezza). «Prenota» manda le postazioni con le loro date a `POST /reservations` (una prenotazione
  sola), «Riserva» le manda a `POST /holds` con `segments`.
- **Ricerca guidata** (B11): copre il periodo con meno cambi di postazione possibili (a ogni cambio la postazione
  libera più a lungo; a parità la più vicina alla precedente e poi a `nearSpotId`, in caselle del reticolo). Due
  alternative al massimo, che partono da postazioni diverse; con `maxMoves` restano solo quelle che lo rispettano
  (nessuna soluzione: elenco vuoto). «Prenota» manda i tratti a `POST /reservations`.
- **Prenotazione ripetuta** (B9): i giorni scelti in blocchi di giorni consecutivi, con quelli già occupati; il
  gestore decide e salva i blocchi come postazioni con date proprie (`POST` o `PUT /reservations`). Ogni blocco si
  prezza per la sua lunghezza; il cumulativo sui giorni totali della stessa postazione è da fare nel preventivo.

## Gruppi

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /groups?search=&page=&pageSize=&sortBy=&sortDir=` | `bookings.read` | `sortBy` = `name`, `reservations`, `createdAt` (predefinito, dal più recente); righe `{ id, name, reservationCount, customerNames, createdAt }` |
| `GET /groups/{id}` | `bookings.read` | `{ id, name, createdAt, createdBy, reservations (come l'elenco delle prenotazioni), total, balanceDue }` |
| `POST /groups` | `bookings.move` | `{ name, reservationIds }` → 201 `{ group, failed }` |
| `PUT /groups/{id}` | `bookings.move` | `{ name }` |
| `POST /groups/{id}/members` · `DELETE /groups/{id}/members/{reservationId}` | `bookings.move` | `{ reservationIds }` → `{ group, failed }` |
| `POST /groups/bulk-delete` | `bookings.move` | `{ ids }`; le prenotazioni restano |
| `POST /groups/{id}/payments` | `payments.record` | pagamento unico `{ amount, mode, isDeposit, payerCustomerId, reservationIds? }` → `{ shares: [{ reservationId, bookingNumber, amount }], group }` |

- Prenotazioni anche di clienti diversi; ognuna in un solo gruppo. Quelle già in un altro gruppo
  (`booking.already_in_group`, con `groupId` e `groupName`), annullate o non visibili restano fuori, in `failed`; se non
  ne resta nessuna: 422 `booking.group_empty`. Nome unico nello stabilimento senza distinguere maiuscole: 409
  `booking.group_name_taken`.
- Il gruppo non cambia i prezzi: il cumulativo vale solo dentro la stessa prenotazione (D2).
- **Pagamento unico** (D1): un incasso per ogni prenotazione con un residuo. Se l'importo copre il residuo del gruppo
  ognuna riceve il suo, altrimenti si divide in proporzione ai residui (il centesimo all'ultima); può essere un
  acconto. Oltre il residuo del gruppo: 409 `booking.payment_exceeds_balance` (`args.balanceDue`), perché l'eccedenza
  non saprebbe a quale prenotazione andare. `payerCustomerId` deve essere uno dei clienti del gruppo. Il **pagamento
  separato** è l'incasso della singola prenotazione (`POST /reservations/{id}/payments`). Lo scontrino (uno per
  prenotazione o uno solo) è da fare con Fiskaly.

## Prenotazione online e area cliente

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /public/{slug}/availability?from=&to=&dayPart=` | anonimo | postazioni prenotabili online e libere `{ freeSpotIds }` (escluse quelle riservate alle convenzioni, occupate o con una riserva) |
| `POST /public/{slug}/reservations` | anonimo o cliente registrato | prenotazione dal sito → 201 `{ reservation, payment: { required, amount, holdUntil, provider, redirectUrl, clientSecret } }` |
| `POST /reservations/{id}/confirm` · `POST /reservations/{id}/reject` | `bookings.online` | conferma o rifiuto del gestore |
| `GET /me/reservations` · `GET /me/reservations/{id}` | cliente registrato | prenotazioni dell'account in tutti gli stabilimenti |
| `POST /me/reservations/{id}/cancel` | cliente registrato | annullamento → `{ reservation, paid, penaltyPercent, penalty, refundDue, refunded, refundPendingOnline }` |
| `POST /me/reservations/{id}/released-days` | cliente registrato | sharing dall'area cliente (stesso corpo del gestionale, solo `kind: sharing`) |

- **Corpo** (`OnlineReservationRequest`): `{ customer: { fullName, email, phoneCountryCode, phone }, from, to, dayPart, seasonal,
  spots: [{ spotId, priceListId, additionals }], services, discountCode, paymentMethod, note, humanToken }`. Senza accesso
  serve la verifica anti-robot (`humanToken`). Il preventivo è quello online di Catalog: periodi, postazioni ammesse,
  mezza giornata, stagionali, giorno stesso, orizzonte, limiti di soggiorno, massimo di postazioni.
- **Regole di Booking**: solo il nome è obbligatorio, ma email **e** telefono lo sono con il massimo giornaliero o con
  «solo contatti registrati» (422 `booking.online_contacts_required`); «solo contatti registrati» vuole email e telefono
  uguali a un contatto (403 `booking.online_contacts_only`); cliente in blacklist: 403 `booking.online_not_allowed`;
  massimo di **postazioni** al giorno per cliente, contando solo le prenotazioni online (422 `booking.online_daily_limit`
  con `args: { max, day }`); postazione riservata alle convenzioni: 422 `booking.spot_not_online`; le riserve del gestore
  sono occupate (409 `booking.spot_unavailable`); i limiti dei servizi bloccano (409 senza conferma); metodo di pagamento
  non dello stabilimento: 422 `booking.online_payment_method_not_allowed`.
- **Cliente registrato**: la prenotazione va sul contatto dell'account in quello stabilimento (quello collegato o con la
  stessa email, altrimenti uno nuovo collegato all'account).
- **Conferma del gestore** (con «richiede conferma»): la prenotazione resta `pending` con le postazioni tenute; scade alla
  prima fra 48 ore dalla richiesta, le 23:00 della vigilia e, per il giorno stesso (o dopo le 23:00 della vigilia), un'ora
  dalla richiesta. Confermata o rifiutata (`cancellationReason: rejected`); scaduta: `confirmation_expired`. Il cliente
  riceve un'email a richiesta, conferma, rifiuto e scadenza; il codice prenotazione è nelle email. Con la conferma il
  pagamento online si chiede dopo: alla conferma Booking chiede a Payment il pagamento del residuo
  (`IOnlineCardPayments.RequestPaymentAsync`) e mette il link nell'email di conferma. Al rifiuto si rimborsa l'incassato
  (`IReservationPayments.RefundAsync`).
- **Pagamento online** (`pay_now`, `deposit_half`): le postazioni restano tenute 5 minuti (`payment.holdUntil`), 10 dopo
  «Paga»; senza pagamento la prenotazione si annulla (`payment_failed`). Booking apre il pagamento con
  `IOnlineCardPayments.StartCheckoutAsync` (la risposta porta `provider`, `redirectUrl` o `clientSecret`; tutti `null`
  finché Payment non ha Stripe) e lo chiude con `CancelCheckoutAsync` se la prenotazione scade o si annulla prima. L'esito
  arriva da Payment con `IOnlineBookingPayments` (`PaymentStartedAsync`, `PaymentSucceededAsync`, `PaymentFailedAsync`). Pagamento arrivato in ritardo: se le postazioni sono ancora libere la prenotazione torna valida,
  altrimenti l'esito chiede il rimborso totale e il cliente riceve l'email (C2). Senza pagamento online
  (`pay_at_property`…) la prenotazione è subito confermata.
- **Job delle scadenze** (`OnlineExpiryJob`, ogni `Booking:Jobs:OnlineExpiryEvery`, predefinito un minuto): pagamenti non arrivati e richieste non confermate.
- **Annullamento del cliente** (A2): consentito fino al preavviso dello stabilimento (`cancellationDeadline`, calcolato
  dall'inizio del primo giorno), altrimenti 422 `booking.cancellation_not_allowed`; sotto la soglia di una trattenuta si
  trattiene la sua percentuale del pagato (la più alta fra quelle superate). `refundDue` è il rimborso dovuto e Booking lo
  chiede a Payment (`RefundAsync`): `refunded` è la parte registrata subito (incassi al banco), `refundPendingOnline` quella
  sulla carta, che farà il circuito dei pagamenti; i costi di servizio non si rimborsano mai.
- **Area cliente**: le prenotazioni dei contatti dell'account in tutti gli stabilimenti (anche quelle fatte al banco con
  la sua email); niente modifiche; `canShare` se lo stabilimento ha lo sharing online. Un utente non cliente: 403
  `booking.customer_area_only`.

## Servizi e tavoli

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `POST /reservations` · `PUT /reservations/{id}` | `bookings.write` | `services` e `tables` nel corpo della prenotazione |
| `GET /services/planner?date=` | `bookings.planner` | planner dei servizi orari: per servizio `{ serviceId, name, isAvailable, from, to, hasFixedTimeSlots, maxPerSlot, slots: [{ start, end, booked, capacity }], bookings: [{ reservationId, bookingNumber, customerName, start, end, quantity }] }` |
| `GET /tables/planner?date=` | `bookings.planner` | per servizio tavoli e fascia `{ from, to, bookings, covers, maxBookings, seatsLeft, freeTables (posti → tavoli liberi), reservations }` |

- **Servizi** (`services[]`): `{ serviceId, quantity, variant, date, timeSlotId, startTime, manualPrice, spotId }`. `date`
  dentro le date della prenotazione (senza: il primo giorno); i servizi orari vogliono la durata (`timeSlotId`, gli id
  delle durate del servizio in Catalog) e l'ora di inizio. `manualPrice` = importo della riga scritto a mano (permesso sui
  prezzi). `spotId` lega il servizio a una postazione della prenotazione. Senza postazioni è una prenotazione di **soli
  servizi** (`kind: service`). **Assenti = invariati** (spostamenti e scambi non li toccano); lista vuota = tolti.
- Importi dal preventivo di Catalog, insieme alle postazioni (lo sconto del codice vale anche sui servizi); i **prezzi
  automatici** (es. «Coperto») diventano righe a sé con `kind: automatic_price`.
- **Limiti** (F1): servizio non disponibile quel giorno, fuori orario, inizio fuori dalle fasce fisse, massimo al
  giorno, all'ora, per fascia, minimo e massimo di articoli. Dal gestionale: 409 `booking.service_capacity_exceeded` con
  `args.violations: [{ serviceId, name, date, rule, limit, requested }]`; si prenota lo stesso rimandando con
  `confirmCapacity: true`. Online bloccheranno.
- **Tavoli** (`tables[]`): `{ tableServiceId, date, slotStart, covers, tableSeats? }`. `slotStart` = inizio di una fascia del
  servizio tavoli quel giorno. Senza `tableSeats` si assegna il tavolo libero con i posti uguali o più vicini per eccesso
  (P3); con `tableSeats` il gestore sceglie (F2). Limiti (fascia inesistente `table_slot`, coperti `table_seats`, massimo
  di prenotazioni per fascia, `no_table`, `seats_full` con la capienza a posti) come per i servizi, con `confirmCapacity`.
  `depositDue` = caparra del servizio tavoli (a persona o fissa; si incassa come acconto).
- **Risposta**: `kind` (`spot`, `service`), `services: [{ id, serviceId, kind (bar, normal, hourly, automatic_price), name,
  variant, quantity, date, slotStart, slotEnd, timeSlotId, amount, spotId }]`, `tables: [{ id, tableServiceId, name, date,
  slotStart, slotEnd, covers, tableSeats, depositDue }]`.
- Nel cestino i servizi liberano il loro posto e lo riprendono col ripristino.
- **Intervallo fra due ordini** (`intervalMinutes`): se lo stesso cliente ha ordinato lo stesso servizio in un'altra
  prenotazione da meno dei minuti impostati, il servizio aggiunto (o la quantità aumentata) supera il limite `interval`
  (`limit` = minuti da attendere, `requested` = minuti passati); dal gestionale si conferma, online blocca. Senza cliente
  non si controlla.
- «Paga con tessera» online è il metodo `pay_at_property_prepaid`: si paga allo stabilimento con la tessera, quindi
  Booking non chiama nulla; l'incasso con la tessera lo registrerà Payment quando la tessera ci sarà.

## Sharing e assenze

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `POST /reservations/{id}/released-days` | `bookings.write` | `{ kind: absence | sharing, lineId, dates, dayPart, recalculatePrice }` → la prenotazione |
| `POST /reservations/{id}/released-days/withdraw` | `bookings.write` | `{ offerIds }` → la prenotazione |
| `GET /sharing?from=&to=&kind=&status=&search=&page=&pageSize=` | `bookings.read` | pagina «Sharing creati»: righe `{ reservationId, bookingNumber, customerName, lineId, spotLabel, kind, status (available, completed), dates, availableDates, availableOfferIds }`; `kind` = `sharing` (predefinito), `absence`, `all` |
| `POST /sharing/withdraw` | `bookings.write` | ritiro dalla pagina `{ offerIds }` → `{ succeeded, failed }` |
| `GET /sharing/settings` · `PUT /sharing/settings` | `bookings.read` · `catalog.settings` | Impostazioni › Sharing: `{ isEnabled, minBookingDays, seasonalOnly, allArticles, absencesAsSharing, manualCreditEnabled, manualCreditUnitsPerDay, manualCreditAmount, voucherCreditEnabled, voucherCreditAmount, cardTopUpEnabled, cardTopUpAmount }` |
| `GET /sharing/credits/{customerId}` | `bookings.amounts` | crediti del cliente: `{ units, amount, entries: [{ occurredAt, type (earned, reversed, adjustment), units, amount, bookingNumber, note }] }` |
| `POST /sharing/credits/adjustments` | `payments.record` | il gestore usa i crediti (valori negativi) o ne aggiunge: `{ customerId, units, amount, note }` |

- **Giorni ceduti**: il titolare cede alcuni giorni di una postazione (anche mezza giornata, su una riga di giornata
  intera); la postazione torna prenotabile e sulla mappa del giorno compare in `released` (`{ spotId, offerId, dayPart,
  kind, state, reservationId, bookingNumber }`). Nel dettaglio della prenotazione ogni riga ha `releasedDays: [{ offerId,
  date, dayPart, kind, state (available, reassigned), excludesFromPrice }]`. Giorni passati, fuori dalla riga o già
  liberati: 422 `booking.day_not_releasable` / `booking.day_not_in_reservation`; già ceduti: 409
  `booking.day_already_released`.
- **Sharing**: solo se attivo (422 `booking.sharing_disabled`), con la prenotazione lunga almeno `minBookingDays` (422
  `booking.sharing_min_days`), stagionale se `seasonalOnly` (422 `booking.sharing_seasonal_only`), sugli ombrelloni salvo
  `allArticles` (422 `booking.sharing_not_for_article`). I giorni ceduti con lo sharing si pagano comunque.
- **Assenze programmate** (C3): sempre possibili; con `recalculatePrice` i giorni non si pagano più (il prezzo si
  ricalcola con il listino di oggi e lo resta anche nelle modifiche successive), senza restano pagati. Con
  `absencesAsSharing` un'assenza ripresa da un altro cliente fa maturare il credito come uno sharing.
- **Ripresa**: chi prenota la postazione in un giorno ceduto lo riprende (`state: reassigned`, la pagina mostra
  «Completato»). Il titolare matura il credito impostato: crediti manuali nel registro dei crediti
  (`manualCreditUnitsPerDay` × `manualCreditAmount`), voucher e ricarica della tessera come evento
  `ResaleCreditEarned` per Catalog e Payment (non ancora gestito: voucher e tessera non esistono). Se chi aveva ripreso
  il giorno lo toglie (modifica, cestino) il giorno torna disponibile e il credito si storna (`ResaleCreditReversed`);
  se ripristina dal cestino lo riprende di nuovo.
- **Ritiro**: solo dei giorni non ancora ripresi (409 `booking.offer_already_taken`, C3: anche per lo sharing); la
  postazione torna del titolare e un'assenza fuori prezzo torna a pagamento. Se il titolare cambia o toglie la riga,
  i suoi giorni non ancora ripresi si ritirano da soli.

## Rinnovo, dividi conto, scambio

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `POST /reservations/{id}/renew` | `bookings.move` | → `{ sourceId, reservationId, bookingNumber, total }` |
| `POST /reservations/renew` | `bookings.move` | «Duplica» dall'elenco: `{ ids }` → `{ succeeded: [...], failed: [{ id, code, args }] }` |
| `POST /reservations/{id}/split` | `bookings.write` | `{ mode: proportional | manual, shares: [{ spotId, total, paid? }] }` → `{ reservations }` |
| `POST /reservations/{id}/swap` | `bookings.write` | `{ lineId, withLineId, from?, to? }` → `{ first, second, from, to }` |

- **Rinnovo** (B4, D3): prenotazione nuova, confermata e da pagare, con le stesse postazioni e le stesse date
  dell'anno dopo; le stagionali prendono le date della nuova stagione. Prezzo con il listino di oggi sui periodi
  della stagione successiva (stessa chiave di periodo, in Catalog). Restano pacchetti, articoli aggiuntivi (non quelli
  «solo per determinati giorni»), convenzioni e ospiti; non passano note, sconti, incremento, prezzi scritti a mano,
  servizi e gruppo. Nessun messaggio al cliente. Errori: 422 `booking.next_season_missing` (stagione successiva non
  impostata), `booking.renewal_outside_next_season` (date fuori dalla nuova stagione, con `args`),
  `booking.renewal_period_missing` (giorni senza periodo o senza prezzo), `booking.renewal_package_missing`
  (pacchetto non più nel listino); 409 `booking.already_renewed` (una prenotazione si rinnova una volta, finché il
  rinnovo non va nel cestino). Nel rinnovo multiplo ogni prenotazione ha il suo esito.
- **Dividi conto** (B8, BK-14): una prenotazione per postazione (almeno due, altrimenti 422
  `booking.split_needs_two_spots`). La prima resta sulla prenotazione di partenza; le altre nascono con un numero
  nuovo, stesso cliente, date, pacchetti, aggiuntivi, convenzioni, note e check-in, **non** collegate fra loro né al
  gruppo (`isSplitPart: true`). Il prezzo non si ricalcola: il totale di prima si divide in proporzione al prezzo delle
  postazioni (`proportional`, predefinito) o come lo assegna il gestore (`manual`: una quota per postazione, la somma
  uguale al totale; `paid` facoltativo, tutti o nessuno, la somma uguale al pagato; altrimenti 422
  `booking.split_shares_invalid` con `args: { total, paid }`). Ogni prenotazione resta con il totale fissato (prezzo
  applicato). L'incassato segue le quote e passa alle nuove con un movimento di Payment (`source: reservation_split`)
  che non tocca la cassa; la spunta «Pagato» della partenza non si può più togliere. Lo scontrino già emesso resta uno.
- **Scambio** (B12): quando la postazione di destinazione è occupata, le due prenotazioni si scambiano le postazioni
  nei giorni in comune (o solo fra `from` e `to`); le righe si spezzano dove serve e la differenza di prezzo si applica
  da sola a entrambe. Nessun giorno in comune: 422 `booking.swap_no_common_days`; due righe della stessa prenotazione:
  422 `booking.swap_same_reservation`. Nessun blocco con scontrino emesso.

## Eliminazione, cestino e pulizia dati

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /trash` | `bookings.delete` | stessi filtri dell'elenco; ordinato per data di eliminazione |
| `POST /trash/restore` | `bookings.delete` | `{ ids }` |
| `POST /trash/purge` | `bookings.cleanup` | «Elimina definitivamente» `{ ids }` |
| `POST /trash/empty` | `bookings.cleanup` | «Svuota cestino» → `{ purged }` |
| `POST /cleanup/move-to-trash` | `bookings.cleanup` | «Cancella dati» della pagina Database: `{ ids, password }` |

- **Elimina** (pagina Prenotazioni): una scelta sola per tutta la selezione. `refund: true` rimborsa gli incassi
  (un'uscita di cassa per metodo, l'incasso originale resta); gli incassi online con carta non si rimborsano ancora da
  soli e tornano in `refundPendingOnline`. Con il rimborso l'annullo degli scontrini è obbligatorio (422
  `booking.refund_requires_void_receipts`). Risposta `{ succeeded, failed, refunded, refundPendingOnline }`.
- **Cestino**: la prenotazione è annullata e le postazioni libere. Si ripristina entro 30 giorni (422
  `booking.restore_window_expired`), se le postazioni sono ancora libere (`booking.spot_unavailable` nell'esito);
  torna confermata e da pagare per quanto rimborsato. Dopo 30 giorni il job notturno la elimina.
- **Elimina definitivamente / Svuota cestino**: anche prima dei 30 giorni. Della prenotazione resta la traccia nel
  registro per 12 mesi; incassi e scontrini restano in Payment e Fiskaly per il tempo di legge.
- **Cancella dati** (Impostazioni › Database): pulizia di prenotazioni vecchie, concluse o no, con la password
  dell'utente (422 `booking.password_invalid`); nessun rimborso, nessun annullo degli scontrini; vanno nello stesso
  cestino. Elenco e filtri sono quelli di `GET /reservations`.
- Le prenotazioni eliminate nel vecchio gestionale arrivano dalla migrazione già cancellate: non sono nel cestino e
  non si ripristinano.

## Payment (nucleo minimo)

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /api/payment/reservations/{reservationId}/entries` | `bookings.amounts` | movimenti `{ id, type (deposit, payment, refund), mode, source, amount (+ entra, − esce), occurredAt, recordedByUserId }` |
| `DELETE /api/payment/entries/{id}` | `payments.record` | annulla un incasso manuale (409 `payment.entry_not_voidable` per gli altri, compresi gli spostamenti di «Dividi conto») |

Gli incassi si registrano da Booking, che verifica la prenotazione. Payment pubblica `ReservationPaymentsChanged` con
i totali e Booking aggiorna pagato, acconto, residuo e metodo prevalente.

**Da fare in Payment**: Stripe (pagamento online, link, preautorizzazione, rimborsi sul circuito), tessera prepagata e
voucher, scadenziario, limiti per metodo, chiusura di cassa, ricevute, ripartizione dell'IVA per aliquota (oggi
l'incasso è tutto imponibile con aliquota 0), riconciliazione notturna dei totali, outbox degli eventi
(PROMEMORIA-85-7).

## Job e retention

- **Job notturno di Booking** (`BookingNightlyJob`, all'ora italiana di `Booking:Jobs:NightlyAt`, predefinita 02:30): elimina le prenotazioni nel cestino da più di
  30 giorni, con una voce nel registro per ciascuna (`booking.reservation_purged`, `automatic: true`).
- **Retention**: nuova policy su `booking.entrances` (cancellazione dopo una stagione da `entry_date`), migration
  `AddBookingEntranceRetention` di Privacy; nasce in prova come tutte. Le altre tabelle di Booking avevano già le loro.

## Eventi gestiti

- `ReservationPaymentsChanged` (Payment): totali degli incassi.
- `PropertyTrashChanged` (Catalog): con `trashed` le prenotazioni non ancora finite si annullano (motivo
  `property_trashed`), senza rimborsi né messaggi, e le postazioni si liberano; con `restored` tornano attive solo
  quelle, anche con date passate. Da fare: annullo dei pagamenti online in sospeso.

## Contratti fra moduli (Shared/Contracts)

| Contratto | Implementa | Usa |
|---|---|---|
| `Catalog/IBookingCatalog` | Catalog | postazioni della mappa (tipo, fila, colonna, prenotabile), prefisso del numero, date della stagione e della successiva |
| `Catalog/IQuoteCalculator` | Catalog | preventivo del gestionale (stesse regole di `POST /api/catalog/quotes`); con `nextSeason` il listino di oggi sui periodi della stagione successiva (rinnovo) |
| `Identity/ICustomerDirectory` | Identity | contatti: crea, cerca, elenca, blacklist |
| `Identity/IUserDirectory` | Identity | password dell'utente (pulizia dati), postazioni dell'affiliato |
| `Payment/IReservationPayments` | Payment | incassi, annullo, rimborso totale e parziale (`RefundAsync`), spostamento dell'incassato fra prenotazioni (dividi conto), totali |
| `Payment/IOnlineCardPayments` | Payment (oggi non attivo: risponde `null`) | apertura del pagamento con carta, richiesta di pagamento dopo la conferma, chiusura del pagamento aperto |
| `Fiskaly/IReservationReceipts` | Fiskaly (oggi non attivo: `available: false`) | annullo degli scontrini di una prenotazione |
| `Booking/IOnlineBookingPayments` | Booking | esito del pagamento online, chiamato da Payment |
| eventi `BookingActivity`, `ResaleCreditEarned`, `ResaleCreditReversed` | ricevuti da Notification, Payment e Catalog (oggi senza effetto) | campanella; ricarica della tessera e voucher dello sharing |
| `Catalog/IQuoteCalculator.QuoteCumulativeBlocksAsync` | Catalog | cumulativo della prenotazione ripetuta (B9) |

## Schema

Migration `BookingApiFoundation` (Booking): check-in e check-out sulla testata (copiati dalle righe), motivo e data
dell'annullamento, scadenza e autore della conferma online, sconto manuale e incremento, rinnovo e dividi conto, autore
della nota, spunta «Pagato»; sulle righe fila, colonna e tipo della postazione e prezzo del pacchetto scritto a mano;
date degli articoli aggiuntivi; id pubblico delle riserve; tabella `booking.entrances`. Tolti lo stato `no_show` (il
tipo si ricrea), le azioni rapide (`booking.quick_action_dates`) e `is_split` della testata. La migrazione dei dati
(utility 01) è allineata.

Ultima fase: migration `OfflineSync` (cursore `sync_seq` con sequenza e trigger su prenotazioni e riserve, tabella
`processed_commands`; la versione è `xmin`) e `BookingAttachments` (tabella `booking.media_assets`).

Online: migration `OnlineBooking` (Booking: `payment_hold_until`, valori `rejected` ed `expired` della conferma online) e
`CancellationPenalties` (Catalog: trattenute sull'annullamento sulla scheda dello stabilimento).

Servizi e tavoli: migration `ServiceAndTableLines` (Booking: tabella `booking.table_lines`) e `ServicesAlignedToFront`
(Catalog: tolto `applies_to_all_services`, impostazioni orarie più d'una per periodo).

Sharing: migration `SharingAlignedToFront` (impostazioni come il front: giorni minimi, assenze come sharing, tre crediti
in euro; tolti `mode`, `credit_calc`, `both_options_disabled` e i loro tipi, valore `seasonal_workers` →
`seasonal_only`; i dati esistenti convertiti; `public_id` ed `excludes_from_price` sulle offerte).
Il cestino ora cancella logicamente solo la testata: righe, aggiuntivi e ospiti restano, e il ripristino ritrova anche
quelli delle prenotazioni messe nel cestino prima della correzione.

Seconda fase: migration `BookingGroupsPublicId` (Booking: `public_id` dei gruppi, le righe migrate lo ricevono dal
default) e `LedgerSourceReservationSplit` (Payment: valore `reservation_split` del tipo `ledger_source`).

## Magazzino, codici sconto, prezzo unico

- **Magazzino** (BK-10): i pacchetti e gli articoli aggiuntivi usano gli articoli di magazzino di Catalog (quantità per
  unità venduta) in ogni giorno della riga; per ogni articolo e giorno l'impegnato non supera la quantità totale. Dal
  gestionale il superamento è un limite come quelli dei servizi (409 `booking.service_capacity_exceeded`, `rule:
  inventory`, si conferma con `confirmCapacity`); online blocca. Sotto la scorta minima la risposta ha l'avviso
  `inventory_low` e parte un'attività per la campanella.
- **Utilizzi dei codici sconto**: un codice con il massimo di utilizzi si conta quando una prenotazione lo prende (un solo
  UPDATE, sicuro anche con prenotazioni contemporanee: 409 `booking.discount_exhausted`) e l'utilizzo torna disponibile
  quando la prenotazione lo perde, va nel cestino o si annulla (al ripristino si riprende). La prenotazione che ha preso
  l'ultimo utilizzo si può modificare tenendo lo sconto.
- **Cumulativo della prenotazione ripetuta** (B9): blocchi separati della stessa postazione con lo stesso pacchetto
  cumulativo (es. ven-sab-dom per 4 settimane) si prezzano sui giorni totali di ogni periodo (la griglia a giornata intera
  di 12 giorni), ripartiti per i giorni di ciascun blocco. Solo a giornata intera, dal gestionale, senza stagionale né
  prezzi scritti a mano; aggiuntivi invariati. Avviso `cumulative_blocks`.
- **Prezzo unico** (B10): una prenotazione continua che passa da una postazione all'altra con lo stesso pacchetto (tratti
  senza buchi, anche dalla ricerca smart, dallo spostamento o dallo scambio) prezza il pacchetto come un periodo unico:
  ogni tratto vale il prezzo del periodo intero sulla sua fila per la quota dei suoi giorni (il centesimo all'ultimo).
  Aggiuntivi e prezzi scritti a mano restano quelli del tratto. Avviso `continuous_price`.

## Reimposta mappa

- Catalog pubblica `MapSpotsChanged` quando l'editor toglie o crea postazioni («Reimposta mappa», righe o colonne tolte,
  postazioni eliminate o aggiunte).
- Con «Reimposta mappa elimina le prenotazioni» (impostazione della mappa) le prenotazioni non ancora finite delle
  postazioni tolte si annullano (`cancellationReason: spot_removed`), anche se pagate.
- Altrimenti le loro righe restano senza postazione (`GET /map/orphans`: `{ reservationId, bookingNumber, customerName,
  lineId, spotLabel, spotType, row, column, from, to }`, il rapporto dei casi da sistemare) e si ricollegano alla
  postazione nuova nella **stessa posizione** se il tipo coincide; se la posizione non c'è più, a quella con la stessa
  etichetta e lo stesso tipo. Tipo diverso nella stessa posizione: niente ricollegamento (H1). Il ricollegamento sposta
  anche i giorni occupati e va nel registro.
- Identity toglie le postazioni eliminate da quelle assegnate agli affiliati (restano le assegnazioni per posizione).
- Il rapporto si scarica anche in PDF (H1): `GET /map/orphans/pdf` (`bookings.read`), nella lingua dell'operatore
  (tutte e 7 le lingue: in arabo la pagina va da destra a sinistra con le colonne invertite; gli ideogrammi usano un font
  a parte). Font incorporati: DejaVu Sans in una copia con i glifi composti scomposti (PDFsharp perdeva parte delle
  lettere arabe) e Droid Sans Fallback per cinese/giapponese/coreano; licenze in `Export/Fonts/README.md`.
  Libreria PDFsharp-MigraDoc 6.2.4 (MIT) in `BuildingBlocks.Infrastructure/Export/PdfReportWriter.cs`, riusabile dagli
  altri moduli; non aggiornarla oltre la 6.2.x (dalla 6.5.0 è AGPL).

## Offline e tempo reale

- **Versione**: `ReservationDto.version` (e `ETag` del dettaglio) è la versione della riga; `PUT /reservations/{id}` con
  `If-Match` rifiuta con 412 `booking.version_conflict` se la prenotazione è cambiata nel frattempo. Due salvataggi
  contemporanei: 409 `booking.version_conflict`.
- **Idempotenza**: ogni comando con l'header `Idempotency-Key` (al massimo 100 caratteri) si esegue una volta sola per
  stabilimento; rimandato riceve la stessa risposta con `Idempotent-Replayed: true`. La stessa chiave su un altro comando:
  422. Le chiavi si dimenticano dopo 7 giorni (job notturno).
- **Cursore**: `GET /sync?since=&limit=` restituisce `{ cursor, hasMore, reservations: [{ id, bookingNumber, status,
  removed, version, seq, from, to, lines }], holds: [{ id, spotId, from, to, dayPart, active, seq }] }` cambiate dopo
  `since`, solo quelle più vecchie di 3 secondi (le transazioni ancora aperte non fanno saltare numeri). Il dispositivo
  rimanda `cursor`; con `hasMore` richiama subito. Righe, servizi, tavoli e ospiti cambiati fanno avanzare la loro
  prenotazione.
- **Tempo reale** (T-12): hub SignalR `/hubs/property` (token del gestionale, anche come `access_token` in querystring);
  ogni connessione è nel gruppo del suo stabilimento e riceve `changed` con `{ topic: reservations | holds, ids }` dopo
  ogni salvataggio. Una sola istanza dell'API: con più istanze serve un backplane (Redis).

## Allegati

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /reservations/{id}/attachments` | `bookings.read` | `[{ id, fileName, contentType, sizeBytes, createdAt, uploadedBy, url, fileDeletedAt }]` |
| `POST /reservations/{id}/attachments` | `bookings.write` | multipart, campo `file` |
| `DELETE /reservations/{id}/attachments/{attachmentId}` | `bookings.write` | |

- B13: PDF, JPEG o PNG (riconosciuti dal contenuto: 422 `booking.attachment_type`), al massimo 2 MB (422
  `booking.attachment_too_large`), al massimo 10 per prenotazione (422 `booking.attachment_limit`). `url` è un link di
  download a tempo. Il job notturno cancella i file caricati negli anni passati (dal 1° gennaio): resta il nome. Archivio:
  sezione `Booking:Attachments` (bucket e prefisso propri).

## Campanella

Booking pubblica `BookingActivity` (BK-35) per: prenotazione nuova dal gestionale e online, richiesta online da
confermare, pagamento ricevuto, annullamento del cliente, magazzino sotto la scorta. Le notifiche in-app le crea
Notification (da fare: oggi il modulo non ha ancora i suoi servizi).

## Non ancora disponibile

Le chiamate verso gli altri moduli ci sono già; mancano le implementazioni dall'altra parte, oggi vuote:
- **Pagamento con carta** (Payment, Stripe): `IOnlineCardPayments` risponde `null` (nessun pagamento aperto: la
  prenotazione che lo richiede scade dopo 5 minuti), i rimborsi sulla carta restano in `refundPendingOnline`.
- **Annullo degli scontrini** (Fiskaly): `IReservationReceipts` risponde `available: false`; il cestino lo conta in
  `receiptsVoidPending`.
- **Campanella** (Notification): `BookingActivity` arriva a un gestore vuoto.
- **Voucher e ricarica della tessera dello sharing** (Catalog, Payment): `ResaleCreditEarned`/`Reversed` arrivano a gestori
  vuoti. **Modelli delle email** nelle 7 lingue (oggi testi fissi IT/EN).

Da confermare (vedi §11 di `booking-da-chiarire.md`): le scelte sui permessi dove il vecchio gestionale non dice nulla.

## Errori del modulo

| Status | `code` | Quando |
|---|---|---|
| 403 | `booking.property_required` | token senza stabilimento |
| 404 | `booking.property_not_found` | stabilimento del token sconosciuto a Catalog |
| 404 | `booking.reservation_not_found` · `booking.line_not_found` · `booking.hold_not_found` | inesistente o di un altro stabilimento |
| 404 | `booking.spot_not_found` | postazione non sulla mappa |
| 422 | `booking.spot_not_bookable` | postazione non prenotabile (`args: { spotId, label }`) |
| 403 | `booking.spot_not_allowed` | postazione non assegnata all'affiliato |
| 409 | `booking.spot_unavailable` | postazione già occupata (`args.conflicts`) |
| 409 | `booking.area_full` | area senza posti sufficienti (`args: { capacity, days }`) |
| 404 | `booking.customer_not_found` | contatto inesistente |
| 422 | `booking.customer_name_required` | cliente senza nome e non generico |
| 409 | `booking.customer_blacklisted` | cliente in blacklist: rimandare con `confirmBlacklisted` |
| 409 | `booking.reservation_cancelled` | operazione su una prenotazione annullata |
| 403 | `booking.pricing_forbidden` | prezzo scritto a mano, sconto manuale o incremento senza permesso |
| 422 | `booking.move_outside_line` | spostamento fuori dalle date della riga |
| 422 | `booking.day_not_in_reservation` | ingresso in un giorno fuori dalla riga |
| 409 | `booking.payment_exceeds_balance` | incasso oltre il residuo senza conferma (`args.balanceDue`) |
| 422 | `booking.nothing_to_pay` | spunta «Pagato» senza residuo |
| 409 | `booking.already_marked_paid` · `booking.not_marked_paid` | spunta «Pagato» già messa o assente |
| 404 | `booking.group_not_found` | gruppo inesistente o di un altro stabilimento |
| 409 | `booking.group_name_taken` | nome del gruppo già usato nello stabilimento |
| 422 | `booking.group_empty` | nessuna delle prenotazioni può entrare nel gruppo (`args.failed`) |
| — | `booking.already_in_group` | nell'esito: prenotazione già in un altro gruppo (`args: { groupId, groupName }`) |
| 422 | `booking.next_season_missing` · `booking.renewal_outside_next_season` · `booking.renewal_period_missing` · `booking.renewal_package_missing` | rinnovo non possibile |
| 409 | `booking.already_renewed` | prenotazione già rinnovata |
| 422 | `booking.split_needs_two_spots` · `booking.split_shares_invalid` | dividi conto non possibile |
| 422 | `booking.swap_no_common_days` · `booking.swap_same_reservation` | scambio non possibile |
| 422 | `booking.sharing_disabled` · `booking.sharing_min_days` · `booking.sharing_seasonal_only` · `booking.sharing_not_for_article` | sharing non consentito |
| 422 | `booking.day_not_releasable` | giorno passato, già liberato o di un'area |
| 409 | `booking.day_already_released` | giorno già ceduto |
| 409 | `booking.service_capacity_exceeded` con `rule: interval` | stesso servizio ordinato dallo stesso cliente da meno di `intervalMinutes` |
| 409 | `booking.discount_exhausted` | codice sconto arrivato al massimo di utilizzi |
| 412 · 409 | `booking.version_conflict` | `If-Match` con una versione vecchia, o salvataggi contemporanei |
| 422 | `booking.attachment_type` · `booking.attachment_too_large` · `booking.attachment_limit` | allegato non ammesso |
| 404 | `booking.attachment_not_found` | allegato inesistente |
| 422 | `booking.online_unavailable` | lo stabilimento non accetta prenotazioni online |
| 403 | `booking.human_verification_failed` | verifica anti-robot non riuscita |
| 422 | `booking.online_payment_method_not_allowed` · `booking.online_contacts_required` · `booking.online_daily_limit` · `booking.spot_not_online` | regole online |
| 403 | `booking.online_contacts_only` · `booking.online_not_allowed` | solo contatti registrati, cliente bloccato |
| 409 | `booking.not_awaiting_confirmation` | conferma o rifiuto di una prenotazione che non aspetta conferma |
| 403 | `booking.customer_area_only` | area cliente con un utente che non è un cliente |
| 422 | `booking.cancellation_not_allowed` | annullamento del cliente oltre il preavviso o non permesso |
| 404 | `booking.service_not_found` · `booking.table_service_not_found` | servizio o servizio tavoli inesistente |
| 409 | `booking.service_capacity_exceeded` | limiti dei servizi o dei tavoli superati (`args.violations`): rimandare con `confirmCapacity` |
| 422 | `booking.service_spot_not_in_reservation` | servizio legato a una postazione che non è nella prenotazione |
| 404 | `booking.offer_not_found` | giorno ceduto inesistente o di un'altra prenotazione |
| 409 | `booking.offer_already_taken` | giorno già ripreso: non si ritira |
| 422 | `booking.refund_requires_void_receipts` | rimborso senza annullo degli scontrini |
| 422 | `booking.password_invalid` | password sbagliata in «Cancella dati» |
| — | `booking.reservation_not_in_trash` · `booking.restore_window_expired` | nell'esito di ripristino ed eliminazione definitiva |
| 404 | `payment.entry_not_found` | movimento inesistente |
| 409 | `payment.entry_not_voidable` | movimento non manuale |
| 422 | `payment.invalid_amount` · `payment.invalid_mode` | importo o metodo non validi |
