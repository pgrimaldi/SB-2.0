# systemadmin — Gestione stabilimenti (azioni client↔API)

> **Ruolo**: systemadmin. **Fonte**: codice del portale [`front-admin/`](../../../../../../../front-admin/) — moduli
> `Auth` e `PropertyManagement`, file `src/config.js`, `src/app/modules/Auth/_redux/authCrud.js`,
> `src/app/modules/PropertyManagement/api/propertyapicalls.js`. Logica di business in
> [`business-logic/systemadmin/01-gestione-stabilimenti.md`](../../business-logic/systemadmin/01-gestione-stabilimenti.md).
>
> **Contesto tecnico (WIP, da normalizzare nella riscrittura)**:
> - Base URL non uniformi: `config.js` → `https://api.summerbooking.it/api`; login →
>   `https://development.summerbooking.it/api/auth/signin`; forgot/reset → `https://api.summerbooking.it/api`.
> - Le risposte di `config.js#Api` sono **cifrate AES-256-GCM** (`{iv, tag, payload}` base64) e
>   decifrate nel client con chiave hardcoded (`DecryptionService.js`). Sotto si descrive il **payload
>   in chiaro** già decifrato.
> - Auth: header `x-access-token: <accessToken>`; `accessToken` + oggetto utente in `localStorage`
>   (`signInUser`). Su HTTP `401` il client fa `localStorage.clear()` + redirect a `/home`.
> - Tutti gli endpoint `sb/*` richiedono (lato backend) token + ruolo `systemadmin`; `propertydetails`
>   ha però il controllo di ruolo commentato (vedi business-logic §0.1).

---

## 1. Login al portale

**Descrizione**: il systemadmin inserisce email e password nella pagina `/auth/login`.
**Schermata**: `Auth/pages/Login.js`
**Ruolo**: systemadmin (qualsiasi altro ruolo viene rifiutato dopo la risposta)

**Input → API** (`POST auth/signin`; URL attuale: `https://development.summerbooking.it/api/auth/signin`)
- `username` (string) — il campo "Email" del form, forzato `toLowerCase()`
- `password` (string)

**Output ← API**
- `status` (bool) — esito autenticazione
- `roles` (string) — ruolo dell'utente; il client procede **solo se** `=== "systemadmin"`, altrimenti
  mostra "Unauthorized Access" e non salva nulla
- `accessToken` (string, JWT) — salvato in `localStorage.accessToken` e usato come `x-access-token`
- resto dell'oggetto utente (`id`, `email`, `firstName`/`lastName`, `avatar`, …) — salvato in
  `localStorage.signInUser`; `id` è riusato come `createdBy` nelle azioni successive
- `message` (string) — messaggio d'errore mostrato in caso di `status = false`

**Note**: esiste un percorso di login "statico" (`staticAuthData.js`, `USE_STATIC_AUTH = false`) usato
solo in sviluppo. Nessuna scadenza/refresh gestita lato client.

---

## 2. Password dimenticata

**Descrizione**: dalla pagina di login il systemadmin richiede il reset via email.
**Schermata**: `Auth/pages/ForgotPassword.js`
**Ruolo**: systemadmin (endpoint di per sé pubblico)

**Input → API** (`POST auth/forgotpassword`; URL attuale: `https://api.summerbooking.it/api/auth/forgotpassword`)
- `email` (string)

**Output ← API**
- `status` (bool)
- `message` (string) — mostrato come alert di successo o errore; dopo 5 s redirect a `/auth`

---

## 3. Reset password

**Descrizione**: il systemadmin apre il link ricevuto via email (`/auth/reset-password/:token`) e
imposta la nuova password.
**Schermata**: `Auth/pages/ResetPassword.js`
**Ruolo**: systemadmin (endpoint di per sé pubblico)

**Input → API** (`POST auth/resetpassword`; URL attuale: `https://api.summerbooking.it/api/auth/resetpassword`)
- `email` (string) — **non digitato**: ottenuto decifrando `:token` dall'URL con `cryptr`
  (chiave hardcoded `"myTotalySecretKey"`)
- `password` (string) — nuova password (il client verifica solo che coincida con la conferma)

**Output ← API**
- `status` (bool)
- `message` (string) — su successo, dopo 5 s redirect a `/auth/login`

---

## 4. Code di lavorazione (elenchi per stato)

**Descrizione**: il systemadmin apre una delle tre viste a coda dal menù laterale "Property Management".
**Schermata**: `PropertyManagement/NewProperty.js`, `ApproverdProperties.js`, `RejectedProperties.js`
**Ruolo**: systemadmin

**Input → API** — nessun parametro
- `GET sb/newproperties` (coda "New", `status = 0`)
- `GET sb/approvedproperties` (coda "Approved", `status = 1`)
- `GET sb/rejectedproperties` (coda "Rejected", `status = 2`)

**Output ← API**
- `status` (bool) — `false` = lista vuota (il client mostra `message`)
- `data` (array) — righe stabilimento; il client legge: `id`, `propertyName`, `city`, `fullName`
  (referente), `mobile`, `email`, `isDelete` (mostra badge "Deleted" e nasconde il tasto elimina), `id`
  reale usato per il link al dettaglio
- `message` (string)

**Note**: nessuna paginazione lato client (ricerca/filtro in memoria con `react-table-6`). Payload
"piatto" con tutti i campi dello stabilimento (vedi business-logic §2).

---

## 5. Dettaglio di uno stabilimento

**Descrizione**: dalla coda il systemadmin apre `/property-management/property-details/:id`.
**Schermata**: `PropertyManagement/PropertyDetails.js`
**Ruolo**: systemadmin (controllo di ruolo backend commentato)

**Input → API** (`POST sb/propertydetails`)
- `id` (int) — id stabilimento (da URL)

**Output ← API** (`data`, oggetto)
- `id`, `propertyName`, `fullName`, `mobile`, `email`, `city` — anagrafica, editabili nel form
- `status` (int `0|1|2`) — mostrato come Pending / Approved / Rejected
- `adminComment` (string) — nota interna
- `photos` (array<string>) — URL immagini (carosello)
- `propertyOwner` (int|null) — id titolare; se `null` e `status = 1` il client mostra "Add Super Admin"
- `userData` (object) — titolare: `firstname`, `lastname`, `mobile`, `email`, `username`, `license`,
  `licenseMonths`, `subscriptionStatus` (`enable|disable`), `subscriptionDisableDate`, `startDate`,
  `endDate`, `userStatus`, `birthDate`, `gender`, `notifyWhatsapp`, `notifyEmail`
- `commissionAmount` (number|string) — percentuale commissione (campo editabile, 0–100)
- `superAdminOnlineFlag` (0|1) — passato come `onlineReservation` alla modale "Update Super Admin"
- `paymentOptions` (object) — configurazione pagamenti (non modificata da questo pannello)

**Note**: il client calcola `totaldays` (giorni residui abbonamento) da `userData.startDate/endDate` e
`subscriptionStatus`/`subscriptionDisableDate`; il valore, con il suo delta rispetto all'originale,
viene rimandato come `additionalDays` in §6.

---

## 6. Approvazione / rifiuto / modifica dello stabilimento

**Descrizione**: dal dettaglio, il systemadmin sceglie "Approve"/"Reject", scrive una nota, aggiusta i
giorni di abbonamento e la commissione, poi preme "Submit".
**Schermata**: `PropertyManagement/PropertyDetails.js` (`SubmitApproveReject`)
**Ruolo**: systemadmin

**Input → API** (`POST sb/updateproperty`)
- `id` (int) — id stabilimento
- `status` (int) — `1` se "Approve", `2` se "Reject"
- `adminComment` (string) — nota interna (il client filtra i non-lettera: `replace(/[^A-Za-z]/gi,"")`)
- `propertyOwner` (int|null) — ricopiato dal dettaglio
- `propertyName`, `propertyOwnerName` (= campo "Owner Name"), `mobile`, `email`, `city` — anagrafica
- `additionalDays` (string) — delta giorni abbonamento, formato `"+N"` / `"-N"` / `"N"` (calcolato
  client-side da `totaldays - oldDate`)
- `ownerfirstName`, `ownerlastName` (string) — split del campo "Super Admin" name
- `ownermobile` (string)
- `ownerlicensetype` (string) — valore `type` da `public/licensetype`
- `ownersubscription` (string) — `"enable"` / `"disable"` (tendina "Subscription")
- `commissionAmount` (number|string) — percentuale

**Output ← API**
- `status` (bool)
- `message` (string) — mostrato in Snackbar; su successo il client ricarica il dettaglio

**Note**: un'unica chiamata fa modifica anagrafica + workflow di stato + commissione + abbonamento/
licenza dello staff + email al titolare (vedi business-logic §4). Nessun controllo di transizione di
stato lato client.

---

## 7. Eliminazione di uno stabilimento

**Descrizione**: dalla coda (o dal dettaglio) il systemadmin conferma l'eliminazione nella modale.
**Schermata**: `PropertyManagement/DeletePropertyDialog.js`
**Ruolo**: systemadmin

**Input → API** (`POST sb/deleteproperty`)
- `id` (int) — **unico campo inviato dal client** (`propertyapicalls._deleteProperty`), nonostante il
  backend accetti anche `adminComment` e `isDelete` (vedi business-logic §5 — divergenza)

**Output ← API**
- `status` (bool)
- `message` (string) — Snackbar; su successo ricarica la coda

**Note**: soft-delete backend (`isDelete = true`) + disattivazione login del solo `propertyOwner`.

---

## 8. Aggiunta di un Super Admin allo stabilimento

**Descrizione**: se lo stabilimento è approvato e senza titolare, il systemadmin crea l'account
`superadmin` dalla modale.
**Schermata**: `PropertyManagement/AddSuperAdmin.js`
**Ruolo**: systemadmin

**Input → API** (`POST auth/usersignup`)
- `username`, `email`, `password` (string)
- `firstname`, `lastname` (string)
- `mobile` (string), `mobileCountryCode` (`""`), `country` (`""`)
- `roles` (array) — sempre `["superadmin"]`
- `createdBy` (int) — `id` del systemadmin loggato (da `localStorage.signInUser`)
- `propertyId` (int) — id dello stabilimento
- `notifyEmail`, `notifyWhatsapp` (int `0|1`) — checkbox
- `license` (string) — `type` da `public/licensetype`; `licenseMonths` (int) — `months` della stessa riga
- `subscriptionStatus` (string) — `"enable"` / `"disable"`
- `onlineReservation` (int `0|1`) — tendina "Online Reservation"

**Output ← API**
- `status` (bool)
- `message` (string) — Snackbar; su successo il client chiama `callBack()` (ricarica dettaglio) e chiude
  la modale dopo 3 s

**Note**: validazioni solo client (lunghezze 3–30/60, regex email, password forte 6–15 con maiuscola/
minuscola/speciale, almeno un canale di notifica). Nessuna ripetuta lato server.

---

## 9. Aggiornamento del Super Admin

**Descrizione**: se lo stabilimento ha già un titolare, il systemadmin ne modifica anagrafica, licenza,
abbonamento e "Online Reservation" dalla modale.
**Schermata**: `PropertyManagement/UpdateSuperAdmin.js`
**Ruolo**: systemadmin

**Input → API** (`POST sb/updatesuperadmin`)
- `id` (int) — id utente titolare (da `userData.id`)
- `propertyId` (int)
- `firstname`, `lastname`, `mobile` (string) — editabili; `username` ed `email` mostrati **disabilitati**
- `mobileCountryCode` (`""`), `country` (`""`)
- `userStatus`, `birthDate`, `gender`, `profilePic` — ricopiati invariati da `userData`
- `createdBy` (int) — `id` del systemadmin loggato
- `notifyEmail`, `notifyWhatsapp` (int `0|1`)
- `license` (string), `licenseMonths` (int) — da `public/licensetype`
- `subscriptionStatus` (string `enable|disable`)
- `onlineReservation` (int `0|1`) — inizializzato da `superAdminOnlineFlag` del dettaglio

**Output ← API**
- `status` (bool)
- `message` (string) — Snackbar; su successo `callBack()` + chiusura dopo 3 s

**Note**: aggiorna il **singolo** utente (non tutto lo staff — cfr. `updateproperty`); imposta anche
`properties.superAdminOnlineFlag` e `online_reservations.isDisable` lato server (business-logic §6).

---

## 10. Accesso diretto al gestionale come titolare

**Descrizione**: dalla coda "Approved", il systemadmin apre il gestionale dello stabilimento
autenticato come suo titolare ("login as").
**Schermata**: `PropertyManagement/ApproverdProperties.js` (`handleDirectAccess`)
**Ruolo**: systemadmin

**Input → API** (`POST auth/send-token`; URL attuale hardcoded verso un tunnel di sviluppo
`https://825t8bv2-8081.inc1.devtunnels.ms/api/auth/send-token`)
- `email` (string) — email del titolare della riga

**Output ← API**
- `status` (bool)
- `data` (object) — payload utente + token emesso conoscendo **solo l'email** (nessuna password/OTP;
  vedi [`docs/01-criticita-e-indicazioni-backend.md`](../../../progettazione/01-criticita-e-indicazioni-backend.md))
- `message` (string) — su errore, Snackbar

**Comportamento client**: cifra `data` con AES (`crypto-js`, chiave hardcoded `"SB_USER_DATA"`),
`encodeURIComponent`, e apre in una nuova scheda `http://localhost:5001/home?user=<enc>`.

**Note**: funzione di impersonation da riprogettare completamente nel nuovo sistema (token monouso
server-side a scadenza breve, URL da configurazione, audit dell'accesso "come gestore").
