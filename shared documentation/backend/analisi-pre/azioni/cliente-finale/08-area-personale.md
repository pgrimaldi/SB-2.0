# cliente-finale — Area personale e gestione prenotazioni

> Area funzionale: profilo del cliente autenticato, elenco e dettaglio delle proprie prenotazioni,
> modifica/annullamento, saldo credito/wallet, preferenze di notifica, eliminazione account.
> **Schermate**: **non disegnate nei mockup** (nell'header compare solo "Accedi"/"Registrati"; la
> voce di menù dell'area personale non è mostrata). Tutte le firme qui sono **ipotizzate**, basate sugli
> endpoint oggi usati da `front-customer-pre` (`sb/mybookingsByUserId`, `sb/getpersonalDetails`,
> `sb/updatePersonalDetails`, `sb/getCreditLedgerDetails`, `sb/accountBalance`, `sb/deleteAccount`,
> `sb/updateNotification`).

---

## 1. Apertura del profilo personale

**Descrizione**: dall'area personale il cliente vede e può modificare i propri dati anagrafici e le
preferenze.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/getpersonalDetails`)
- `userId` (int)

**Output ← API**
- `profile` ({ id, firstName, lastName, email, mobileCountryCode, mobile, birthDate, gender,
  country, profilePic, languageCode })
- `preferences` ({ notifyEmail, notifyWhatsapp, marketingConsent })
- `stats` ({ bookingsCount, favouritesCount, creditBalance })

---

## 2. Aggiornamento dei dati del profilo

**Descrizione**: il cliente salva le modifiche a nome, telefono, data di nascita, foto, lingua,
consensi.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/updatePersonalDetails`)
- `userId` (int)
- campi modificabili: `firstName`, `lastName`, `mobileCountryCode`, `mobile`, `birthDate`, `gender`,
  `country`, `profilePic` (upload/URL), `languageCode`, `notifyEmail`, `notifyWhatsapp`,
  `marketingConsent`

**Output ← API**
- `status` / `message`
- `profile` (oggetto aggiornato, come azione 1)

---

## 3. Elenco delle proprie prenotazioni

**Descrizione**: il cliente vede le prenotazioni (postazioni, servizi, eventi) divise per stato:
imminenti, passate, annullate.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/mybookingsByUserId`)
- `userId` (int)
- `status` (string, opzionale) — `"upcoming"|"past"|"cancelled"|"all"`
- `type` (string, opzionale) — `"location"|"service"|"event"|"all"`
- `page` (int) / `pageSize` (int)

**Output ← API**
- `total` (int)
- `bookings` (array) — `{ id, reservationCode, type, propertyId, propertyName, propertyThumbnail,
  checkIn, checkOut, seasonal, items[], status, paymentStatus, amountPaid, amountDueAtProperty,
  total, currency, canModify (bool), canCancel (bool), cancellationDeadline }`

---

## 4. Dettaglio di una prenotazione

**Descrizione**: il cliente apre una prenotazione per vederne voci, mappa della postazione, stato
pagamento, policy di cancellazione e ricevuta.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/getBookingByReservationCode` / `sb/bookingSummaryById`)
- `bookingId` (int) **o** `reservationCode` (string)
- `userId` (int)

**Output ← API**
- `booking` (come card dell'azione 3, con in più: `locationElements[]`, `contact`, `notes`,
  `priceBreakdown`, `receiptUrl`, `timeline[]` (stati con timestamp))

---

## 5. Modifica di una prenotazione (date / postazione)

**Descrizione**: entro i termini consentiti il cliente cambia le date o la postazione.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/updateBooking` — da definire)
- `bookingId` (int)
- `changes` ({ checkIn?, checkOut?, elementIds?, extras? })

**Output ← API**
- `status` / `message`
- `available` (bool) — se le nuove date/postazioni sono libere
- `pricing` ({ oldTotal, newTotal, delta, currency }) — eventuale conguaglio
- `paymentRequired` (bool) — se il delta è positivo → [`07-pagamento.md`](07-pagamento.md)

---

## 6. Annullamento di una prenotazione

**Descrizione**: il cliente annulla una prenotazione; in base alla policy ottiene rimborso totale,
parziale o nullo.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/cancelBooking` — da definire)
- `bookingId` (int)
- `reason` (string, opzionale)

**Output ← API**
- `status` (string) — `"cancelled"`
- `refund` ({ amount, method: "card"|"credit"|"none", currency, expectedDays })
- `cancellationFee` (number)

---

## 7. Consultazione del credito / wallet

**Descrizione**: il cliente vede il saldo del proprio credito (es. da rimborsi o promozioni) e il
movimento.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/accountBalance` + `sb/getCreditLedgerDetails`)
- `userId` (int)
- `page` (int) / `pageSize` (int)

**Output ← API**
- `balance` (number) / `currency` (string)
- `ledger` (array) — `{ id, date, type: "earn"|"spend"|"refund", amount, description, bookingRef }`

---

## 8. Gestione delle preferenze di notifica

**Descrizione**: il cliente attiva/disattiva le notifiche email/WhatsApp per conferme, promemoria e
comunicazioni marketing.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/updateNotification` / `sb/disableNotification`)
- `userId` (int)
- `channels` ({ email: bool, whatsapp: bool, push: bool })
- `topics` ({ bookingUpdates: bool, reminders: bool, marketing: bool })

**Output ← API**
- `status` / `message`
- `preferences` (oggetto aggiornato)

---

## 9. Eliminazione dell'account

**Descrizione**: il cliente richiede la cancellazione del proprio account e dei dati associati.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/deleteAccount` / `sb/deleteuser`)
- `userId` (int)
- `password` (string) — conferma identità
- `reason` (string, opzionale)

**Output ← API**
- `status` / `message`
- `deletionScheduledAt` (datetime, opzionale) — se la cancellazione è differita

**Note**: ✅ **deciso il 2026-10-07 — la cancellazione procede** anche con prenotazioni future attive.
L'account viene disattivato e anonimizzato subito; le prenotazioni restano valide, la riga della rubrica
dello stabilimento viene scollegata dall'account e lo stabilimento continua a vedere il nominativo. La
cancellazione effettiva dei dati personali della rubrica passa dal flusso GDPR di `Module.Privacy`
([`docs/11-modulo-privacy.md` §5](../../../progettazione/11-modulo-privacy.md)). Gli account senza password
(solo social) confermano con un nuovo login presso il provider invece che con la password.

---

## 10. Notifiche in-app

**Descrizione**: il cliente consulta l'elenco delle notifiche (conferme, promemoria check-in,
comunicazioni dello stabilimento).
**Schermata**: non nei mockup → firma ipotizzata (endpoint `getNotification` / `update-notification`).
**Ruolo**: cliente autenticato.

**Input → API** (`sb/getNotification`)
- `userId` (int)
- `page` (int) / `pageSize` (int)
- `onlyUnread` (bool)

**Output ← API**
- `total` (int) / `unreadCount` (int)
- `notifications` (array) — `{ id, type, title, body, createdAt, read (bool), actionUrl }`

E per marcare come lette (`sb/update-notification`): `{ userId, notificationIds[] | markAllRead: true }`
→ `{ status, unreadCount }`.
