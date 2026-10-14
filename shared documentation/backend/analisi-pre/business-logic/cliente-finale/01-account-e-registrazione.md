# cliente finale — Account e registrazione

> **Tipo di utente**: cliente finale (visitatore del sito pubblico `front-customer-pre` / app mobile
> `front-app-pre`). Non ha un menù a voci: le aree funzionali sostituiscono le voci di menù.
>
> **Ambito**: creazione account (email/password e social), login, recupero password, OTP,
> aggiornamento dati personali, cancellazione account, iscrizione automatica alla newsletter.
>
> **Metodo**: `front-customer-pre/src/components/auth/*` + `auth.controller.js` (`signup`, `signin`,
> `social`, `verifyAppleToken`, `sendToken`, `sendOtp`, `verifyOtp`, `logOut`) + `user.controller.js`
> (`getpersonalDetails`, `updatePersonalDetails`, `deleteAccount`) + `password.controller.js`. Schede
> DB: [`users.md`](../../db/users.md), [`customers.md`](../../db/customers.md),
> [`subscribes.md`](../../db/subscribes.md), [`roles.md`](../../db/roles.md).

---

## 1. Registrazione con email e password

**Azione** — `POST /api/sb/signup` (dal form `SignUp.js`): il cliente inserisce username, email,
password, nome, cognome, prefisso+telefono, paese, lingua; il form invia sempre `roles: ["consumer"]`.

**Logiche**:

- Crea `users` con password **hashata bcrypt** (10 round → `hashSync(pwd, 8)` nel codice),
  `loginMethod = "normal"`, `userStatus = "active"`, `languageCode` (1 = EN, 2 = IT).
- Assegna il ruolo `consumer` (`setRoles`).
- Nessun controllo di **email già esistente** in fase di `create` (si affida al vincolo DB / gestione
  errore).

**Calcoli**: nessuno. **Eventi**:

- **Iscrizione automatica alla newsletter**: l'email viene aggiunta a `subscribes`
  (`Commonhelper.createSubscribes`).
- **Email di benvenuto** al cliente (IT o EN da `languageCode`), oggetto "Il tuo account è stato
  creato…" con link di accesso; riga in `email_logs` (`emailType = "Normal Signup"`).

---

## 2. Registrazione / login con social (Google, Facebook, Apple)

**Azione** — `POST /api/sb/social` (+ `verifyAppleToken`, `handleFbAccessToken`).

**Logiche**:

- Verifica il token del provider; se l'utente non esiste lo crea (`loginMethod` = provider),
  altrimenti effettua il login.
- Ruolo assegnato: `consumer` (default). Anche qui l'email entra in `subscribes`.
- Se un utente **non ha righe in `user_roles`**, al login il campo `roles` restituito vale comunque
  `"consumer"` per default applicativo (vedi [`roles.md`](../../db/roles.md#3--consumer)).

**Calcoli**: nessuno. **Eventi**: email di benvenuto (come §1) nei rami di prima creazione.

---

## 3. Login

**Azione** — `POST /api/sb/signin` (`signin`).

**Logiche**:

- Verifica email/password (bcrypt compare).
- Controlla `userStatus == "active"` — se `inactive` (account cancellato/sospeso) il login è
  **rifiutato** con messaggio "Account has deleted" (fuorviante anche per stati non di cancellazione).
- **2FA**: se `roles != "consumer"` **e** `bathouses.isTwoFactorEnabled == 1` per lo stabilimento
  dell'utente → invia un OTP e richiede la verifica (per il **cliente finale** puro il 2FA non
  scatta, essendo `consumer`).
- Emette un **JWT** (`x-access-token`).

**Calcoli**: nessuno. **Eventi**: eventuale email OTP (solo utenti non‑consumer).

---

## 4. Recupero password

**Azione** — `POST /api/sb/sendToken` / `password.controller.js` (+ pagina `/reset-password/:email`).

**Logiche**: genera un token/codice di reset con scadenza, lo salva sull'utente, invia il link/codice
via email; alla conferma la nuova password (bcrypt) sostituisce quella vecchia.

**Calcoli**: scadenza token. **Eventi**: **email** con link/codice di reset + `email_logs`.

---

## 5. OTP (verifica in due passaggi)

**Azioni** — `sendOtp`, `verifyOtp`, `updateAuthenticationFlag`.

**Logiche**: OTP a 4 cifre generato con `Math.random()`, salvato su `users.two_factor_otp` + `expiry`,
inviato via email; `verifyOtp` confronta il valore ma **non lo invalida** dopo l'uso (riutilizzabile
fino alla scadenza) — criticità nota (vedi [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §7).

**Calcoli**: generazione OTP; scadenza. **Eventi**: **email** con OTP.

---

## 6. Dati personali

**Azioni** — `getpersonalDetails`, `updatePersonalDetails`.

**Logiche**: legge/aggiorna nome, cognome, telefono, paese, foto profilo, preferenze lingua e
notifiche sull'account del cliente. Nessuna validazione particolare.

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 7. Cancellazione account

**Azione** — `POST /api/sb/deleteAccount` (`user.controller.js:deleteAccount`).

**Logiche**:

- **Soft delete anonimizzante**: `email` e `username` vengono riscritti come
  `del_<gg/mm/aaaa>_<valore originale>` e `userStatus = "inactive"` (login bloccato).
- Le prenotazioni e i dati collegati **non** vengono cancellati.

**Calcoli**: composizione del prefisso data. **Eventi**: nessuno (nessuna email di conferma
cancellazione).

---

## 8. Logout

**Azione** — `logOut`. Client: se la sessione arriva dall'app cliente (`userSource ===
"customer_app"`) pulisce `localStorage` e reindirizza alla home con `?logout=true`. Nessun evento
server rilevante.

---

## 9. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: hashing password (bcrypt); generazione/scadenza OTP e token di reset; prefisso data
  nell'anonimizzazione account.
- **Logiche**: ruolo `consumer` di default; `userStatus` come soft‑delete che blocca il login; 2FA
  solo per utenti non‑consumer; `roles = "consumer"` restituito anche senza righe `user_roles`.
- **Eventi**: **email** di benvenuto (signup, IT/EN) + `email_logs`; **email** di reset password;
  **email** OTP; **iscrizione automatica** dell'email in `subscribes` ad ogni registrazione (email/
  social). Nessuna notifica in‑app, nessuna chiamata WhatsApp.
