# stabilimento — Impostazioni

> **Voce di menù**: icona chiave inglese nell'header a destra (`_gotoSetting` →
> `components/Settings/SettingParent` → `SettingContent`/`SettingLeftMenu`, rotta `/settings`).
> Comprende: **gestione utenti/staff** (ruoli, licenza, abbonamento), **dati personali** dell'operatore,
> **preferenze notifiche/suoni**, **impostazioni prenotazione online** (opzioni di pagamento, mappa
> online, servizi online), **limiti di soggiorno per settore**, **URL/slug pubblico**, **2FA**.
>
> **Ruoli**: **nascosta** a `manager`, `lifeguard`, `cashoperator`, `waiter`, `advancedlifeguard`,
> `affiliate` (visibile ad `admin`/`superadmin`). La gestione utenti è protetta da `isSuperAdmin` a
> livello di route.
>
> **Metodo**: `accountAction.js` / `manageAccountAction.js` / `managerActions.js` + `user.controller.js`
> (`userlist`, `edituser`, `updateuser`, `deleteuser`, `getpersonalDetails`, `updatePersonalDetails`,
> `disableNotification`) + `auth.controller.js` (`usersignup` per creare staff, `updateAuthenticationFlag`,
> `sendOtp`, `verifyOtp`) + `property.controller.js` (`getOnlineBookingSettings`,
> `updateOnlineBookingSettings`) + `booking.controller.js` (`checkBookingLimit`) +
> `searchRegion.controller.js`. Schede DB: [`users.md`](../../db/users.md),
> [`user_roles.md`](../../db/user_roles.md), [`roles.md`](../../db/roles.md),
> [`properties.md`](../../db/properties.md) (`paymentOptions`, `onlineBookingOptions`),
> [`bathouses.md`](../../db/bathouses.md) (`slug`, `isTwoFactorEnabled`).

---

## 1. Gestione utenti / staff

**Azioni**: `userlist` (elenco), `AddUserForm` → `POST /api/sb/usersignup` (crea), `edituser`
(anagrafica), `updateuser` (licenza/abbonamento/stato), `deleteuser`.

**Logiche**:

- **Elenco** (`userlist`): SQL con join `user_roles`/`roles`, filtra `propertyId` = stabilimento e
  `userStatus = "active"`.
- **Creazione** (`usersignup`): crea `users` con password hashata (bcrypt), `userStatus = "active"`,
  `propertyId`; assegna i ruoli richiesti (`setRoles`); **eredita** `license`/`startDate`/`endDate` da
  un altro utente attivo della stessa property se non specificati esplicitamente; se fra i ruoli c'è
  `superadmin` imposta `properties.propertyOwner` al nuovo id. **Fallback pericoloso**: se il body non
  contiene `roles`, assegna `[1]` = `systemadmin` (vedi [`roles.md`](../../db/roles.md)). I ruoli
  assegnabili da UI sono `admin`, `manager`, `cashoperator`, `waiter`, `lifeguard`,
  `advancedlifeguard`, `affiliate`, `user1`, `marketing` (`superadmin` **non** assegnabile da qui).
- **`edituser`**: aggiorna l'anagrafica; può cambiare i ruoli.
- **`updateuser`** (licenza/abbonamento):
  - se la **licenza cambia** → `startDate = adesso`, `endDate = adesso + licenseMonths mesi`
    (`dateWithMonthsDelay`); altrimenti eredita da un altro utente attivo della property.
  - **logica sospensione** (`subscriptionStatus` passa a `"disable"` e prima non lo era):
    `subscriptionDisableDate = adesso`; poi `diffInDays = (endDate − adesso) / giorno` e
    `endDate = endDate + diffInDays` — **codice aritmeticamente sospetto** (raddoppia di fatto la
    distanza residua invece di congelarla), da verificare prima di replicarlo (vedi
    [`users.md`](../../db/users.md#subscriptionstatus) e la logica **diversa e più corretta** in
    [systemadmin/01](../systemadmin/01-gestione-stabilimenti.md) §4.2).
  - il ramo `else` imposta `subscriptionEnableDate` ad **ogni** salvataggio non‑disable (non solo alla
    riattivazione).
- **`deleteuser`**: soft delete (`userStatus = "inactive"`), che **blocca il login** di
  quell'account.

**Calcoli**: `endDate` da mesi di licenza; aritmetica sospensione abbonamento.
**Eventi**: nessuna email in questi flussi (la creazione staff non invia credenziali via email nel
codice attuale — il ramo email è presente ma non attivo). Nessuna notifica.

---

## 2. Dati personali dell'operatore

**Azioni**: `getpersonalDetails` (`getpersonalDetails`), `updatePersonalDetails`, `edituser` per sé.

**Logiche**: aggiorna nome, cognome, telefono, paese, foto profilo, preferenze; nessun calcolo.
**Eventi**: nessuno.

---

## 3. Preferenze notifiche e suoni

**Azioni**: `disableNotification` (`disableNotification`), più i flag su `users`
`enableManagementBookingSound` / `enableOnlineBookingSound`, `notifyWhatsapp` / `notifyEmail`.

**Logiche**:

- `disableNotification`: `notificationStatus` `0` = notifiche abilitate, `1` = disabilitate → scrive
  `users.disableNotification`. Effetto: l'operatore non riceve più il badge/suono delle notifiche in
  app.
- `enableManagementBookingSound` / `enableOnlineBookingSound`: attivano un **suono** all'arrivo di una
  prenotazione da gestionale / online (gestito lato client sul canale socket.io).

**Calcoli**: nessuno. **Eventi**: nessuno (solo preferenze).

---

## 4. Impostazioni prenotazione online

**Azioni**: `getOnlineBookingSettings` (`property.controller.js:getOnlineBookingSettings`),
`updateOnlineBookingSettings`, `getPaymentOptions`, `addonlinereservation`/`viewonlinereservation`
(`online_reservations`), `getOnlineReservationStatusByAdmin` (lettura del flag "abilitato da
amministratore", vedi [systemadmin/01](../systemadmin/01-gestione-stabilimenti.md) §7).

**Logiche**:

- **Opzioni di pagamento online** (`properties.paymentOptions` / `servicebookingPaymentOptions`,
  JSON): quali modalità sono offerte al cliente — "paga ora tutto", "acconto (metà)", "paga in
  struttura" — con eventuali testi informativi (vedi [`properties.md`](../../db/properties.md#paymentoptions)).
- **`allowOnlineBooking`** (`properties.onlineBookingOptions.locationOptionObj.allowOnlineBooking`):
  `"all"` = chiunque può prenotare online; `"contacts_only"` = solo contatti già registrati dal
  gestore (controllo `checkContactInProperty`, vedi [04-contatti](04-contatti.md) §5); assente = come
  `"all"`.
- **Mappa online** (`online_reservations.isEnableMap`, `period`, `locations`): se il cliente può
  scegliere la postazione su mappa e quali periodi/postazioni sono prenotabili.
- **`isDisable`** (`online_reservations`) / **`superAdminOnlineFlag`** (`properties`): interruttori che
  bloccano il pagamento online (il primo impostabile dal gestore, il secondo dal systemadmin / dal job
  di scadenza abbonamento).
- **Servizi online**: quali servizi/pacchetti sono prenotabili online (`enableOnlineReservation` sui
  `price_lists` / `services_types`).
- **`useFulInfo`**: testo informativo allegato alle email di conferma servizio.

**Calcoli**: nessuno. **Eventi**: nessuno (sono impostazioni). La **commissione** applicata a valle
alle prenotazioni online è `properties.commissionAmount` (gestita dal systemadmin — vedi
[`properties.md`](../../db/properties.md#commissionamount)).

---

## 5. Limiti di soggiorno per settore

**Azione**: `BookingLimit.js` → definisce min/max giorni prenotabili per settore.

**Logiche**: il vincolo è salvato nelle impostazioni e applicato **solo lato client**
(`configureServices.js:_allowBookingLimitCustomer`). L'endpoint server `check-bookinglimit`
(`checkBookingLimit`) **esiste ma non è chiamato da nessuna parte** (vedi
[`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §11.6).

**Calcoli**: confronto giorni prenotati vs min/max. **Eventi**: nessuno.

---

## 6. URL / slug pubblico e regioni di ricerca

**Azioni**: `SetUrl.js` → imposta lo **slug** pubblico dello stabilimento (`bathouses.slug`, usato
nella pagina `/property-details/:slug`); `searchRegion.controller.js` → associa lo stabilimento a
**regioni di ricerca** (`property_search_regions`) per comparire nei filtri geografici della ricerca
pubblica.

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 7. Autenticazione a due fattori (2FA)

**Azioni**: `updateAuthenticationFlag` (`auth.controller.js`), `sendOtp`, `verifyOtp`. Il flag è
**dello stabilimento** (`bathouses.isTwoFactorEnabled`), non del singolo utente.

**Logiche**:

- Se `isTwoFactorEnabled = 1` per lo stabilimento, al login di un utente **non `consumer`** viene
  richiesto un OTP.
- `sendOtp`: genera un OTP a 4 cifre (con `Math.random()`), lo salva su `users.two_factor_otp` +
  `expiry` e lo **invia via email** (`common.helper.js:sendOtpOnMail`).
- `verifyOtp`: confronta l'OTP; **non lo invalida** dopo l'uso (resta valido fino alla scadenza) —
  criticità già segnalata (vedi [`users.md`](../../db/users.md) e
  [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §7).

**Calcoli**: generazione OTP; scadenza. **Eventi**: **email** con l'OTP.

---

## 8. Riepilogo "calcoli / logiche / eventi" della voce Impostazioni

- **Calcoli**: `endDate` licenza = `adesso + licenseMonths mesi`; aritmetica sospensione abbonamento
  (sospetta); confronto giorni vs limiti settore; generazione/scadenza OTP.
- **Logiche**: filtro staff attivo per property; ereditarietà licenza fra utenti della stessa
  property; fallback ruolo `systemadmin` se `roles` assente (da eliminare); soft‑delete che blocca il
  login; `allowOnlineBooking` `all`/`contacts_only`; interruttori `isDisable`/`superAdminOnlineFlag`;
  2FA a livello di stabilimento; limite soggiorno enforced solo client.
- **Eventi**: **email OTP** (2FA). Nessun'altra email/notifica/API esterna in quest'area (la creazione
  staff **non** invia credenziali; le preferenze notifiche non generano eventi).
