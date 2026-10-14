# stabilimento — Assistente virtuale, Notifiche, Account (header)

> **Voci dell'header a destra** non ancora trattate: **Assistente virtuale** (icona messaggio →
> chatbot), **Notifiche** (icona campanella + contatore), **Account** (cerchio con l'iniziale →
> `UpdateAccountDialog` / logout). Sono raccolte qui perché ciascuna ha pochissima logica propria.
>
> **Metodo**: `layout/header/AdminHeader/*`, `layout/footer/Footer.js`,
> `components/Common/ChatBot/*`, `components/account/UpdateAccountDialog`, `notification.controller.js`,
> `data/actions/*`. Scheda DB: [`notification_details.md`](../../db/notification_details.md).

---

## 1. Assistente virtuale (chatbot)

**Azione**: l'operatore apre il widget chat dall'header (o dal footer) e scrive un messaggio.

**Logiche (interamente lato client, `components/Common/ChatBot/`)**:

- `MessageParser` normalizza il testo (minuscolo) e cerca in `responses.json` un **intento** i cui
  `patterns` sono contenuti nel messaggio.
- Se trova un intento → `ActionProvider.handleIntent` restituisce la/e risposta/e predefinite
  (`responses[]`): singola stringa o elenco puntato.
- Se **non** trova nulla → intento `"unknown"`; il codice cita un fallback "verso OpenAI" ma
  `getResponse` in realtà restituisce solo la stringa fissa *"I'm not sure how to respond to that."*
  **Nessuna chiamata di rete** viene effettuata.

**Calcoli**: nessuno. **Eventi**: **nessuno** (nessuna API, nessun salvataggio della conversazione).
È una FAQ statica basata su pattern‑matching. Da chiarire se nella riscrittura deve diventare un vero
assistente (LLM) o resta una guida statica.

> **Nota — feature "Centralino/Switchboard" (fuori menù)**: il backend contiene un modulo esteso
> `switchboard` (~159 rotte: IVR, log chiamate, registri, template e log WhatsApp, audio, webhook
> Telnyx/Meta) e le tabelle `switchboard_*` + `chatbot_*`. **Nessuno dei cinque frontend del
> repository ha una schermata per questo modulo** (il portale `systemadmin`, invece, ora ce l'ha:
> `front-admin/`): non è una voce
> del menù analizzata qui. Va documentato a parte quando/se ne sarà disponibile l'interfaccia.

---

## 2. Notifiche

**Azione**: l'operatore apre il pannello notifiche dall'header; le notifiche non lette hanno un
contatore; aprendo il pannello vengono marcate lette.

**Endpoint**: `getNotification` (`GET`, `notification.controller.js`), `updateNotification` /
`update-notification` (`POST`).

**Logiche**:

- `getNotification`: elenca le `notification_details` con `property_id` = stabilimento e `status = 1`,
  ordinate per data decrescente, **paginate** (10 per pagina).
- `updateNotification`: imposta `read = true` su una notifica (per `id` + `property_id`) o su **tutte**
  quelle della property se `id` non è passato.
- Il **contatore** nell'header (`countUpdate`) è il numero di non lette; `_resetCount` lo azzera
  all'apertura del pannello. Visibile solo a `manager`, `cashoperator`, `admin`, `superadmin`.
- Le notifiche **non vengono create qui**: sono prodotte dai flussi operativi —
  - nuova prenotazione / spostamento (`booking.controller.js`, `type = 1`) — vedi
    [01-mappa](01-mappa.md) §2,
  - nuova prenotazione servizio (`servicereservation.controller.js`) — vedi [03-cassa](03-cassa.md) §1,
  - conferma pagamento Stripe (`stripe.controller.js`) — vedi [05-prenotazioni](05-prenotazioni.md) §3,
  - errori dati scontrino Fiskaly (`fiskaly.helper.js`) — vedi [03-cassa](03-cassa.md) §6,
  - blocco spostamento di prenotazione già scontrinata — vedi [01-mappa](01-mappa.md) §2.
- Il testo è bilingue (`notification_text` / `notification_text_it`); `status` (1 = visibile) e `read`
  governano visibilità e conteggio (vedi [`notification_details.md`](../../db/notification_details.md)).
- Consegna in tempo reale via **socket.io** (canale `property_<id>`, evento
  `NOTIFIATION_TRIGGER_NAME`).

**Calcoli**: conteggio non lette; paginazione. **Eventi**: nessuno generato da questa voce (solo
lettura + marca‑letto); l'evento socket.io è emesso da chi crea la notifica.

---

## 3. Account

**Azione**: dal cerchio con l'iniziale l'operatore apre `UpdateAccountDialog` (dati del proprio
account) oppure esegue il **logout**.

**Endpoint**: `getpersonalDetails` / `updatePersonalDetails` / `edituser` (gli stessi di
[12-impostazioni](12-impostazioni.md) §2), `logOut`.

**Logiche**:

- **Modifica account**: aggiorna nome, cognome, telefono, email, foto, preferenze notifiche/suoni sul
  proprio `users`. Nessun calcolo.
- **Logout**: conferma con dialog; se la sessione proviene dall'app cliente (`userSource ===
  "customer_app"`) pulisce `localStorage` e reindirizza alla home del sito cliente con `?logout=true`;
  altrimenti torna al login. Lato server `logOut` invalida/registra la disconnessione.

**Calcoli**: nessuno. **Eventi**: nessuno (nessuna email/notifica).

---

## 4. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: solo conteggio notifiche non lette e paginazione.
- **Logiche**: chatbot = pattern‑matching statico su `responses.json`; notifiche = filtro
  `property_id` + `status = 1`, marca‑letto singolo/massivo, contatore; logout con ramo speciale per
  sessione da app cliente.
- **Eventi**: **nessuno** originato da queste tre voci. Le notifiche mostrate qui sono create altrove;
  la loro consegna real‑time avviene via **socket.io**.
