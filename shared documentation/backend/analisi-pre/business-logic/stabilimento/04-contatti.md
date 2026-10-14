# stabilimento — Contatti

> **Voce di menù**: quarta icona della sidebar (`_gotoContact` → `components/contact`, rotta
> `/contact`). È l'**anagrafica clienti** dello stabilimento (`customers`): rubrica, blacklist, note,
> sconti personali, elenco ospiti, storico prenotazioni del cliente.
>
> **Ruoli**: nascosta a `cashoperator`, `waiter`, `affiliate`, `lifeguard`, `advancedlifeguard`
> (visibile ad `admin`, `superadmin`, `manager`, `marketing` con i distinguo del menù).
>
> **Metodo**: action file `contactActions.js` + `customers.controller.js` (501 righe) +
> `booking.controller.js` (`checkContactInProperty`, storico) + `servicereservation.controller.js`
> (storico servizi). Schede DB: [`customers.md`](../../db/customers.md),
> [`contact_list.md`](../../db/contact_list.md) (le liste marketing sono nella voce
> [09-marketing](09-marketing.md)).

---

## 1. Elenco contatti

**Azione**: l'operatore apre la rubrica; può cercare e paginare.

**Endpoint**: `customerlist` (`customerlist`), `getPropertyCustomerList` (versione con cache di gruppo),
`searchcustomerlist`, `guest-list` (`guestlist`).

**Logiche**: elenco `customers` con `propertyId` = stabilimento e `isDelete = false`, ordinato per id
decrescente; la ricerca filtra per nome/email/telefono. `getPropertyCustomerList` usa una **cache di
gruppo** (`customer:list:group:<propertyId>`) invalidata ad ogni scrittura.

**Calcoli**: nessuno. **Eventi**: nessuno (lettura).

---

## 2. Aggiungere un contatto

**Azione** — `POST addcustomer`: salva un nuovo cliente con anagrafica (nome, telefoni, email,
indirizzo, CAP, P.IVA, codice fiscale, data di nascita, `otherNames`, `otherMobileNumber`),
**note**, **sconto personale** (`anyDiscounts`), flag **blacklist** (`isBlackList`), `createdBy`,
`propertyId`.

**Logiche**: nessuna validazione server oltre ai tipi; `isDelete = false`. Nessun controllo di
**duplicati** (stesso email/telefono sulla stessa property).

**Calcoli**: nessuno. **Eventi**: **invalidazione della cache** lista contatti
(`clearGroupCache`). Nessuna email/notifica.

---

## 3. Visualizzare / modificare un contatto

- **Dettaglio** — `viewcustomer` (per id): restituisce tutti i campi.
- **Modifica** — `updatecustomer`: aggiorna i campi anagrafici, note, sconto, blacklist. Invalida la
  cache. **Effetto della blacklist**: se `isBlackList = 1`, il cliente non può più prenotare (il
  controllo scatta alla creazione prenotazione/servizio — vedi §5) salvo, per le sole postazioni, la
  deroga esplicita `allowBookingForBlacklistedCustomer`.

**Calcoli/eventi**: nessuno oltre alla cache.

---

## 4. Eliminare un contatto

**Azione** — `deletecustomer`: **soft delete** (`isDelete = true`), la riga resta a DB ma sparisce da
liste e ricerche. Invalida la cache. Nessun controllo su prenotazioni collegate. Nessuna email.

---

## 5. Blacklist e "solo contatti registrati dal gestore"

Due regole che agiscono **fuori** da questa voce ma dipendono dai dati inseriti qui:

- **Blacklist** (`isUserBlacklisted`, e controllo dentro `booking` / `serviceReservation`): alla
  creazione di una prenotazione si cercano i `customers` per email/telefono su quella property; se uno
  ha `isBlackList = 1` la prenotazione è bloccata ("User is blacklisted"). Per le postazioni esiste la
  deroga `allowBookingForBlacklistedCustomer = true`; per i servizi **non** c'è deroga.
- **Contatto registrato con la property** (`checkContactInProperty`): se le impostazioni online della
  property hanno `allowOnlineBooking = "contacts_only"`, una prenotazione **non fatta dall'operatore**
  è ammessa solo se esiste un `customers` con **email e telefono coincidenti** e `isDelete = false`;
  altrimenti "Only contacts registered by the property can make a booking". Con `"all"` o valore
  assente, sempre ammessa.

**Calcoli**: nessuno. **Eventi**: nessuno (sono controlli, non azioni).

---

## 6. Storico del cliente

**Azione**: dal dettaglio contatto si vedono le prenotazioni (postazioni e servizi) del cliente.

**Endpoint**: `booking/detailsByCustomerId` (`bookingDetailsByCustomerId`),
`service/reservation/detailsByCustomerId` (`reservationDetailsByCustomerId`).

**Logiche**: raggruppa le prenotazioni per `bookingNumber` (il cliente può essere `customerId` oppure
comparire in `guest_ids`), con date, postazioni, pagamenti, servizi, stato check‑in.

**Calcoli**: aggregazioni (elenco date/postazioni per prenotazione). **Eventi**: nessuno.

---

## 7. Riepilogo "calcoli / logiche / eventi" della voce Contatti

- **Calcoli**: nessuno (solo aggregazioni di sola lettura nello storico).
- **Logiche**: soft‑delete; cache di gruppo della lista; regola blacklist e regola "solo contatti
  registrati" applicate a valle nelle prenotazioni; assenza di deduplica sui contatti.
- **Eventi**: **nessuna email, nessuna notifica, nessuna API esterna**. Unico effetto collaterale:
  invalidazione della cache lista contatti ad ogni scrittura.
