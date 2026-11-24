# Assistenza e guide

Rotta `/settings/contact-support`, vista `views/settings/contact-support/` (`ContactSupport`), testi
`management.settings.contact_support.*`.

## Struttura

- Una colonna centrata, con i riquadri bianchi del mixin `settings-panel` (condiviso con Configurazione email).
- Riquadro "Hai bisogno di aiuto?" con telefono (link `tel:`), email (link `mailto:`) e orari di assistenza.
- Sotto, il form "Contattaci": Nome, Cognome, Email, Cellulare (campi di testo, con esempi grigi come placeholder) e
  Messaggio (`app-filled-textarea`), poi il bottone "Invia messaggio".

## Riquadro "Hai bisogno di aiuto?"

- I dati arrivano dall'API delle informazioni di assistenza, chiamata all'apertura e di nuovo quando cambia la lingua
  (`DATA_RELOAD`).
- Finché l'API non risponde i riquadri mostrano solo i titoli; se fallisce compare il messaggio d'errore sotto i
  riquadri.

## Form "Contattaci"

- Il form è `ContactSupportRequest`, creato con `new ContactSupportRequest()` all'apertura della pagina e dopo ogni invio
  riuscito.
- Nome, Cognome, Email e Messaggio sono obbligatori (`requiredText`, che conta come vuoto anche un testo di soli spazi;
  messaggio generico "Campo obbligatorio", `invalidate.required`); il Cellulare è facoltativo.
- I limiti sono quelli del backend: Nome e Cognome 100 caratteri, Email 255 e con il formato di un indirizzo
  (`emailAddress`), Cellulare 32 e solo cifre e spazi (`phoneNumberWithoutPrefix`; il riempimento automatico è
  `tel-national`, senza `+`), Messaggio 2000.
- "Invia messaggio" è attivo solo con il form valido e chiama l'invio con lo spinner sul bottone.
- Se riesce, il form si svuota e compare `app-message-popup` "Messaggio inviato con successo" con Chiudi.
- Se fallisce, i dati restano: l'errore di un campo va sotto quel campo, gli altri sopra il bottone.
- L'invio della mail lo gestisce il backend: 429 `auth.too_many_attempts` con troppi messaggi dello stesso utente, 503
  `notification.unavailable` se non può partire.

## API

In `NotificationService` (lo stabilimento lo prende il backend dal token), chieste al backend in
`Assistenza e guide - Frontend requests.md`:

- `GET /api/notification/support/info` (`supportInfo`): risposta `SupportInfo` con `phoneNumber`, `mailAddress` e `supportHour`
  (una riga per periodo, già nella lingua dell'utente).
- `POST /api/notification/support/contact` (`contactSupport`): corpo `ContactSupportRequest` con `firstName`, `lastName`,
  `email`, `mobilePhone` (`null` se vuoto) e `message`, testi senza spazi ai lati.
