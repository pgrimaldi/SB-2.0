# cliente-finale — Account e registrazione

> Area funzionale: creazione account, accesso, recupero password, gestione sessione e lingua.
> **Schermate**: nei mockup il sito cliente mostra sempre nell'header i pulsanti **"Registrati"** e
> **"Accedi"** (pp. 1–10, 26, 27–28) e il selettore di **lingua IT/EN** (bandiera con menù a tendina).
> I **form** di login/registrazione/recupero non sono disegnati in dettaglio: le firme sotto sono
> **ipotizzate** dai campi minimi necessari e dagli endpoint oggi usati da `front-customer-pre`
> (cartella `src/components/auth`).
>
> Logica di business correlata: [`business-logic/cliente-finale`](../../business-logic/) (area "Account &
> registrazione", da redigere) e [`db/users.md`](../../db/users.md), [`db/roles.md`](../../db/roles.md).

---

## 1. Registrazione di un nuovo cliente

**Descrizione**: dall'header ("Registrati") l'ospite apre il form di registrazione e crea un account
cliente con email e password (o tramite social login).
**Schermata**: header pp. 1–10, 26 (form non disegnato → firma ipotizzata).
**Ruolo**: ospite.

**Input → API** (`auth/usersignup`)
- `firstName` (string) — nome
- `lastName` (string) — cognome
- `email` (string) — email, usata come username
- `password` (string) — password in chiaro (hash lato server)
- `mobileCountryCode` (string) — prefisso internazionale, default `+39`
- `mobile` (string) — numero di telefono (solo cifre)
- `languageCode` (int) — `1` = EN, `2` = IT; deriva dal selettore lingua
- `notifyEmail` (bool) — consenso comunicazioni via email
- `notifyWhatsapp` (bool) — consenso comunicazioni via WhatsApp
- `privacyAccepted` (bool) — spunta informativa privacy
- `roleName` (string, fisso) — `"consumer"` (cliente finale; **non** `superadmin`)

**Output ← API**
- `status` (bool) / `message` (string) — esito ("user registered" / errore "email già registrata")
- `userId` (int) — id del nuovo utente
- `emailVerificationRequired` (bool) — se serve verificare l'email/OTP prima del login
- `accessToken` (string, opzionale) — JWT se l'auto-login è previsto
- `user` ({ id, firstName, lastName, email, languageCode, roles[] }) — profilo minimo per la UI

**Note**: `auth/signup` (variante) crea utenti con ruoli diversi; per il cliente finale va usata la
variante `usersignup` con ruolo consumer. La registrazione aggiunge l'email alla lista `subscribes`
(effetto collaterale lato server, vedi business-logic).

---

## 2. Verifica dell'email / OTP

**Descrizione**: dopo la registrazione il cliente inserisce il codice OTP ricevuto via email/SMS per
attivare l'account.
**Schermata**: non nei mockup → firma ipotizzata (endpoint presenti in `front-customer-pre`).
**Ruolo**: ospite.

**Input → API** (invio codice: `auth/sendOtp`; verifica: `auth/verifyOtp`)
- `sendOtp`: `email` (string) **o** `mobile` + `mobileCountryCode` (string) — destinatario del codice
- `verifyOtp`: `email` / `mobile` (string) + `otp` (string) — codice a 4–6 cifre

**Output ← API**
- `sendOtp`: `status` / `message`, `otpExpiresIn` (int, secondi)
- `verifyOtp`: `status` / `message`, `verified` (bool), `accessToken` (string, se il login segue la
  verifica), `user` (profilo minimo)

---

## 3. Accesso (login con email e password)

**Descrizione**: dall'header ("Accedi") il cliente inserisce email e password ed entra nell'area
personale.
**Schermata**: header pp. 1–10 (form non disegnato → firma ipotizzata).
**Ruolo**: ospite → cliente autenticato.

**Input → API** (`auth/signin` / `auth/verifypassword`)
- `email` (string) — email/username
- `password` (string) — password in chiaro
- `rememberMe` (bool) — durata sessione lunga
- `deviceInfo` (object, opzionale) — user-agent/piattaforma per audit

**Output ← API**
- `status` (bool) / `message` (string) — esito ("login ok" / "credenziali errate" / "account non
  attivo")
- `accessToken` (string) — JWT da inviare in `x-access-token` nelle chiamate successive
- `refreshToken` (string, opzionale) — per `/api/v1/refresh`
- `expiresIn` (int) — scadenza token in secondi
- `user` ({ id, firstName, lastName, email, profilePic, languageCode, roles[], notifyEmail,
  notifyWhatsapp }) — profilo per popolare header e area personale

**Note**: la UI deve gestire il caso `userStatus != "active"` (account disabilitato) con messaggio
dedicato.

---

## 4. Accesso con social (Google / Facebook / Apple)

**Descrizione**: il cliente entra usando un provider social; se è il primo accesso l'account viene
creato al volo.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `socialsign`, `handleFbAccessToken`,
`verifyAppleToken` presenti).
**Ruolo**: ospite → cliente autenticato.

**Input → API** (`auth/socialsign` / `auth/handleFbAccessToken` / `sb/verifyAppleToken`)
- `provider` (string) — `"google" | "facebook" | "apple"`
- `idToken` / `accessToken` (string) — token rilasciato dal provider
- `languageCode` (int) — lingua corrente
- `email`, `firstName`, `lastName` (string, opzionali) — dati dal provider quando disponibili

**Output ← API**
- `status` / `message`
- `accessToken` (string) — JWT applicativo
- `isNewUser` (bool) — se l'account è stato appena creato
- `user` (profilo minimo, come al punto 3)

---

## 5. Recupero della password

**Descrizione**: dal form di login ("Password dimenticata?") il cliente richiede il reset e imposta una
nuova password tramite link/codice.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `forgotpassword`, `resetPassword`).
**Ruolo**: ospite.

**Input → API**
- richiesta (`auth/forgotpassword`): `email` (string), `languageCode` (int)
- reset (`auth/resetPassword` / `auth/resetpassword`): `token` (string, dal link email) **o** `otp`
  (string), `newPassword` (string), `confirmPassword` (string)

**Output ← API**
- richiesta: `status` / `message` ("email di reset inviata")
- reset: `status` / `message` ("password aggiornata"), `passwordChanged` (bool)

---

## 6. Modifica della password (utente loggato)

**Descrizione**: dall'area personale il cliente cambia la password inserendo quella attuale e la nuova.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `updatepassword`).
**Ruolo**: cliente autenticato.

**Input → API** (`auth/updatepassword`)
- `currentPassword` (string)
- `newPassword` (string)
- `confirmPassword` (string)

**Output ← API**
- `status` / `message` — esito ("password aggiornata" / "password attuale errata")

---

## 7. Controllo disponibilità email

**Descrizione**: mentre compila il form di registrazione, la UI verifica in tempo reale se l'email è
già registrata.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `checkemail`).
**Ruolo**: ospite.

**Input → API** (`auth/checkemail`)
- `email` (string)

**Output ← API**
- `available` (bool) — `true` se l'email non è ancora usata
- `message` (string)

---

## 8. Uscita (logout)

**Descrizione**: dall'header/area personale il cliente termina la sessione.
**Schermata**: non nei mockup → firma ipotizzata.
**Ruolo**: cliente autenticato.

**Input → API** (`auth/logout`, se previsto lato server; altrimenti solo scarto token client)
- `refreshToken` (string, opzionale) — per invalidazione lato server

**Output ← API**
- `status` / `message`

---

## 9. Rinnovo del token di sessione

**Descrizione**: alla scadenza del JWT la UI ottiene un nuovo access token senza rifare il login.
**Schermata**: nessuna (comportamento tecnico) → firma ipotizzata (endpoint `/api/v1/refresh` già
usato).
**Ruolo**: cliente autenticato.

**Input → API** (`/api/v1/refresh`)
- `refreshToken` (string)

**Output ← API**
- `accessToken` (string) — nuovo JWT
- `expiresIn` (int)

---

## 10. Cambio della lingua dell'interfaccia

**Descrizione**: dal selettore bandiera nell'header (IT/EN) il visitatore cambia la lingua di tutto il
sito.
**Schermata**: header pp. 1–10, 26; menù a tendina con bandiera.
**Ruolo**: ospite e cliente autenticato.

**Input → API**
- per l'ospite: **nessuna chiamata** — la scelta è salvata in locale (cookie/localStorage) e passata
  come `languageCode` / header `Accept-Language` nelle chiamate successive
- per il cliente autenticato (`sb/updatePersonalDetails`, campo singolo): `languageCode` (int) `1`|`2`

**Output ← API**
- cliente autenticato: `status` / `message`, `user.languageCode` aggiornato

**Note**: i contenuti localizzati (etichette, pagine editoriali, magazine, FAQ) vanno richiesti con il
`languageCode` corrente — vedi [`10-contenuti-editoriali-e-newsletter.md`](10-contenuti-editoriali-e-newsletter.md).

---

## 11. "Sei un gestore?" — passaggio al sito vetrina Hub

**Descrizione**: il link "Sei un gestore?" nell'header/footer del sito cliente porta al sito vetrina
Summer Booking Hub.
**Schermata**: header/footer pp. 1–10, 26–28.
**Ruolo**: ospite.

**Input → API**: nessuna (navigazione a URL esterno/altro dominio).
**Output ← API**: nessuno.

**Note**: le azioni del sito Hub sono in [`../stabilimento/01-sito-vetrina-hub-e-lead.md`](../stabilimento/01-sito-vetrina-hub-e-lead.md).
