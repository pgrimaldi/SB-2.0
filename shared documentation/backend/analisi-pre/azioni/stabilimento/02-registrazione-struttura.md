# stabilimento — Registrazione di una nuova struttura

> Area funzionale: form pubblico **"Registra la tua struttura"** (componente `RegisterProperty`,
> presente sia in `front-pre` sia in `front-customer-pre`, aperto da una modale dal menù/nav pubblico).
> È l'azione con cui un potenziale gestore si candida sulla piattaforma; alimenta la coda di
> approvazione del systemadmin.
> **Schermate**: riquadro "Login and register your property" nelle board Hub (pp. 42–43); voce
> "Registra la tua struttura" nell'header del sito live (p. 45). Form di dettaglio non disegnato → i
> campi sono quelli descritti in
> [`business-logic/systemadmin/01-gestione-stabilimenti.md`](../../business-logic/systemadmin/01-gestione-stabilimenti.md) §1.
> Endpoint attuale: `POST /api/public/property` (pubblico, nessuna autenticazione).

---

## 1. Invio della candidatura "Registra la tua struttura"

**Descrizione**: il potenziale gestore compila nome struttura, città, referente, contatti ed eventuali
foto e invia la richiesta; riceve un'email di benvenuto e lo stabilimento entra in stato "in attesa di
approvazione".
**Schermata**: modale "Registra la tua struttura".
**Ruolo**: gestore (potenziale), non autenticato.

**Validazioni lato client** (le uniche presenti oggi):
- tutti i campi testuali obbligatori non vuoti
- `email` conforme a regex di formato
- `phone` accetta solo cifre
- `phonePrefix` default `+39`

**Input → API** (`public/property`)
- `propertyName` (string) — nome della struttura
- `city` (string) — città
- `fullName` (string) — nome e cognome del referente (nel form due campi, concatenati)
- `firstName` (string) / `lastName` (string) — se raccolti separatamente
- `phonePrefix` (string) — prefisso internazionale, default `+39`
- `phone` (string) — numero di telefono
- `email` (string) — email del referente
- `photos` (array, opzionale) — immagini come data URL base64 (nell'attuale UI il caricamento è
  disattivato; lato server oggi la registrazione **senza foto va in errore** — vedi business-logic §1)
- `languageCode` (int) — `1` = EN, `2` = IT (default `2`); determina la lingua dell'email di benvenuto
- `privacyAccepted` (bool)
- `country` (string) — oggi **non raccolto** (il prefisso non viene mappato su un paese) → sempre
  stringa vuota; da introdurre nella riscrittura

**Output ← API**
- `status` / `message` — "property registered" / errore
- `propertyId` (int) — id dello stabilimento creato (stato `0` = in attesa)
- `welcomeEmailSent` (bool)

**Eventi collaterali lato server** (per contesto, non output UI): email di benvenuto al referente con le
foto in allegato; inserimento in `email_logs`; aggiunta dell'email alla lista `subscribes`; upload foto
su S3. L'email di notifica al systemadmin è oggi disattivata (codice commentato).

**Note per la riscrittura**:
- decidere se le **foto** sono obbligatorie (contraddizione form opzionale ↔ backend in errore senza
  foto);
- raccogliere e valorizzare `country` (dal prefisso o da un selettore paese);
- ripristinare una notifica al systemadmin all'arrivo di una nuova candidatura;
- nessun utente (`users`) viene creato in questo passo: l'account del gestore nasce in un secondo
  momento (flusso `auth/usersignup` con ruolo `superadmin`, che imposta `properties.propertyOwner`).

---

## 2. Verifica preliminare della disponibilità dell'email / struttura

**Descrizione**: mentre compila il form, la UI può segnalare se l'email o la struttura risultano già
registrate.
**Schermata**: non disegnata → firma ipotizzata.
**Ruolo**: gestore (potenziale).

**Input → API** (`auth/checkemail` per l'email; `sb/locationAutosuggestion` per il nome struttura)
- `email` (string) — controllo email già usata
- `propertyName` + `city` (string) — ricerca di uno stabilimento omonimo già presente

**Output ← API**
- `emailAvailable` (bool)
- `possibleDuplicates` (array) — `{ propertyId, name, city }` per avvisare "questa struttura esiste
  già"

---

## 3. Completamento registrazione account del gestore (dopo approvazione)

**Descrizione**: approvata la struttura, il referente crea le proprie credenziali di accesso al
gestionale (da link email).
**Schermata**: riquadro "Login and register your property" (pp. 42–43); form non disegnato → firma
ipotizzata.
**Ruolo**: gestore (potenziale) → gestore.

**Input → API** (`auth/usersignup` con ruolo `superadmin`)
- `inviteToken` (string) — token dal link email, lega l'utente al `propertyId`
- `firstName` (string) / `lastName` (string)
- `email` (string) — precompilata dall'invito
- `password` (string) / `confirmPassword` (string)
- `mobileCountryCode` (string) / `mobile` (string)
- `languageCode` (int)
- `roleName` (string, fisso) — `"superadmin"` (titolare dello stabilimento)

**Output ← API**
- `status` / `message`
- `userId` (int)
- `propertyId` (int) — stabilimento collegato (`properties.propertyOwner` impostato)
- `accessToken` (string) — JWT per entrare subito nel gestionale
