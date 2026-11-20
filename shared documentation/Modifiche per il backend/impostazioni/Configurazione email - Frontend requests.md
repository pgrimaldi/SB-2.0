# Configurazione email - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Configurazione email** del gestionale funzioni come la vuole
il frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/email-configuration`: la configurazione SMTP con cui lo stabilimento manda le email (mittente,
server, porta, utente, password e sicurezza), in sola lettura finché l'utente non sceglie di sovrascriverla, con i
bottoni Invia email test, Reset e Salva. La password SMTP non arriva mai al browser: la lettura dice solo se è salvata.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET + POST | `/api/system/email-configuration` e `/api/system/save-email-configuration` | Controllo di versione: `version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict` |

## Regole comuni

- **Le API della pagina**: rotte, corpi e risposte sono quelli del contratto del frontend (`docs/API_CONTRACTS.md` del
  frontend e `shared documentation/API_CONTRACTS.docx`, sezione `/system`). Oggi il backend non le ha: la
  configurazione sta in `EmailChannelSettings` (`notification.email_channel_settings`, modulo Notification), che non
  espone endpoint; la password è un riferimento a un segreto (`SmtpPasswordSecretRef`). Questo documento chiede solo
  quello che la lettura e il salvataggio devono avere in più per il controllo di versione.
- **Stabilimento**: lo prende dal token, come le altre API del gestionale: nessuna richiesta lo contiene.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono.

## 1. Controllo di versione sui salvataggi

Senza un controllo, se due utenti cambiano la configurazione email vince chi salva per ultimo: Salva rimanda tutta la
configurazione letta e cancella, senza che nessuno se ne accorga, quello che un altro utente ha salvato nel frattempo
(per esempio un altro server o una password nuova). Il backend non ha un controllo di concorrenza sugli aggiornamenti
(nessun token di concorrenza nelle configurazioni delle entità, nessun `If-Match`). Il frontend chiede lo schema di
**concorrenza ottimistica** che il backend ha già progettato per le prenotazioni offline
(`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un **codice di errore
unico per tutte le API**: `resource.version_conflict`.

### Cosa ci serve

- **Versione nella lettura**: `GET /api/system/email-configuration` restituisce, insieme alla configurazione, il campo
  `version` (stringa opaca), allo stesso livello degli altri campi:

  ```json
  {
    "senderMailAddress": "prenotazioni@lido-esempio.example",
    "senderName": "Lido Esempio",
    "smtpServerAddress": "smtp.lido-esempio.example",
    "smtpPort": 587,
    "smtpUsername": "prenotazioni@lido-esempio.example",
    "hasSmtpPassword": true,
    "smtpSecurity": "Tls",
    "version": "5c0e7d19"
  }
  ```

- **Cosa copre la versione**: i dati della configurazione che la lettura restituisce e il salvataggio riscrive
  (mittente, server, porta, utente, sicurezza e se una password è salvata, `hasSmtpPassword`), **non la password**: il
  frontend non la legge mai e la manda solo quando l'utente ne scrive una nuova, quindi un cambio della sola password
  (che sta in un segreto a parte, `SmtpPasswordSecretRef`) non cambia la versione e non fa fallire il salvataggio di
  un altro utente. Cambia ogni volta che cambia uno dei dati coperti, e solo allora: due letture senza salvataggi in
  mezzo danno la stessa versione. In particolare **`POST /api/system/send-test-email` non la cambia**,
  anche se il backend registra l'esito della prova (per esempio `LastVerificationAt` e `LastVerificationError` di
  `EmailChannelSettings`): l'utente prova la configurazione e subito dopo la salva, con la versione letta all'inizio.
  Ogni stabilimento ha la sua: un salvataggio di un altro stabilimento non la cambia.
- **`If-Match` nel salvataggio**: il frontend manda la versione letta nell'header `If-Match: "<version>"`; il corpo di
  `save-email-configuration` non cambia e non contiene la versione. Se non è più quella attuale, il backend non salva
  niente e risponde **409 `resource.version_conflict`**, senza `errors[]`. Senza `If-Match` risponde allo stesso modo:
  il frontend lo manda sempre. `send-test-email` non salva e non riceve `If-Match`.
- **`ETag` nella risposta**: il salvataggio riuscito risponde 204 senza corpo, come oggi nel contratto, con la nuova
  versione nell'header `ETag: "<version>"`.
- **Ordine dei controlli del salvataggio**: prima le regole dei singoli campi del contratto (per esempio la porta da 1
  a 65535: 400 `validation.failed`), poi la versione (409), poi la regola della password (con `smtpPassword` `null`
  server, porta, utente e sicurezza devono essere quelli salvati: 400 `validation.failed` su `smtpPassword`). La
  regola della password confronta la richiesta con la configurazione salvata: con una versione vecchia l'utente non ha
  mai visto quella configurazione e deve ricevere il 409, non un errore sulla password.
- **Configurazione mai salvata**: anche uno stabilimento senza configurazione propria (nessuna riga in
  `email_channel_settings`) riceve dalla lettura una `version`, e il primo salvataggio con quella versione riesce. Se un
  altro utente salva per primo, chi salva dopo con la stessa versione riceve il 409.
- **Come farlo lo sceglie lo sviluppatore del backend** (il token `xmin` di Npgsql come nel documento offline, un hash
  dei dati coperti, un contatore...), rispettando le regole sopra sulla password e su `send-test-email`: l'agente
  glielo chiede prima di scrivere codice. Il frontend tratta la versione come una stringa opaca e non la confronta mai.

### API coinvolte

| Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| `GET /api/system/email-configuration` | `POST /api/system/save-email-configuration` | La configurazione SMTP dello stabilimento che la lettura restituisce (`hasSmtpPassword` compreso), non la password |

### Cosa fa il frontend

Con 409 `resource.version_conflict` la pagina mostra l'avviso "Nel frattempo un altro utente ha modificato questi
dati: li abbiamo ricaricati. Rifai le tue modifiche." e rilegge la configurazione con la `GET`, come fa Reset: i campi
tornano in sola lettura finché l'utente non sceglie di nuovo di sovrascriverli. Dopo ogni salvataggio riuscito tiene la
versione dell'`ETag`.

## Verifica

API 1, con un token di un utente dello stabilimento che può leggere e salvare la configurazione email:

1. `GET /api/system/email-configuration` risponde con `version` (per esempio `v1`);
   `POST /api/system/save-email-configuration` con la configurazione letta, `senderName` cambiato, `smtpPassword`
   `null` e `If-Match: "v1"`: 204 senza corpo con l'header `ETag` di una versione nuova, diversa da `v1`; una nuova
   `GET` risponde con `senderName` cambiato e con quella versione.
2. Lo stesso `POST` con `If-Match: "v1"` dopo il primo: 409 `resource.version_conflict` senza `errors[]`; la
   configurazione e la password non cambiano.
3. `POST` senza `If-Match`: 409 `resource.version_conflict`; niente cambia.
4. Due `GET` senza salvataggi in mezzo: la stessa `version`. Un salvataggio della configurazione di un altro
   stabilimento non cambia la `version` di questo.
5. `GET` (versione `v1`, con una password salvata), poi un `POST` con `If-Match: "v1"` che cambia solo la password
   (`smtpPassword` nuova, gli altri campi uguali): 204 con `ETag` uguale a `v1`; un `POST` successivo con
   `If-Match: "v1"` e `smtpPassword` `null`: 204, e la password resta quella nuova.
6. `GET` (versione `v1`), poi `POST /api/system/send-test-email` con la configurazione letta, poi
   `POST /api/system/save-email-configuration` con `If-Match: "v1"`: 204.
7. L'utente A legge (versione `v1`); l'utente B salva con un altro `smtpServerAddress`; A salva con
   `If-Match: "v1"`, il server letto e `smtpPassword` `null`: 409 `resource.version_conflict`, non 400 su
   `smtpPassword`.
8. `POST` con `If-Match` di una versione vecchia e `smtpPort` 0: 400 `validation.failed` con l'errore su `smtpPort`;
   niente cambia.
9. Uno stabilimento senza configurazione propria: la `GET` risponde con una `version`; il `POST` con quella versione:
   204 con `ETag`. Un secondo `POST` con la stessa versione di prima: 409 `resource.version_conflict`.
