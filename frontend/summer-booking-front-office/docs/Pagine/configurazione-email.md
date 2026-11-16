# Configurazione email

Rotta `/settings/email-configuration`, vista `views/settings/email-configuration/` (`EmailConfiguration`), testi
`management.settings.email_configuration.*`.

## Struttura

- In cima, fuori dal riquadro dei campi, un secondo riquadro chiede "Vuoi sovrascrivere le impostazioni predefinite?"
  con il bottone "Sì" (stesso stile e misura di Salva, testi `management.settings.email_configuration.override.*`).
- Riquadro bianco (mixin `settings-panel` di `styles/_settings-panel.scss`, condiviso con Assistenza e guide): ogni riga
  ha a sinistra il nome dell'impostazione con la chiave fra parentesi (`from_email`, `from_name`, `smtp_server`,
  `smtp_port`, `smtp_username`, `smtp_password`, `smtp_security`) e la spiegazione, a destra il campo.
- I sei campi sono nascosti come password: cinque `app-filled-text-field` con `[isPasswordField]="true"` e la porta
  `app-filled-number-field` con `[isMasked]="true"`. L'occhietto in fondo mostra e rinasconde il testo (icone
  `GeneralIcon_EyeStart.svg` con il testo nascosto, `GeneralIcon_EyeClose.svg` con il testo visibile).
- I campi hanno `autocomplete="new-password"`, perché il browser non ci inserisca la password salvata dell'utente.
- `smtp_security` è una tendina con Nessuna, SSL e TLS (valori `None`, `Ssl`, `Tls`, come un enum .NET, da confermare).
- Sotto, a destra, "Invia email test" e "Reset" (blu come Salva, icone bianche `EmailConfiguration_MailSend.svg` e
  `GeneralIcon_DeleteWhite.svg`) e "Salva", della misura del mockup.
- Il resto è più grande del mockup: colonna fino a 64rem, titoli 0.875rem, spiegazioni 0.8125rem, campi alti 2.25rem con
  testo 0.875rem (variabili `--filled-field-*`).

## Sola lettura e "Sì"

- Finché non si preme "Sì" tutti i campi sono in sola lettura (regola `readonly` dei Signal Forms sul form intero, che i
  campi grigi ricevono con `[formField]`; la tendina non si apre). Dopo diventano modificabili e "Sì" si disattiva.
- Niente si modifica prima che arrivino i dati del server: "Sì" resta spento finché la lettura non è arrivata (dopo un
  errore di lettura, finché Reset non la rilegge). Durante ogni lettura i campi tornano in sola lettura, perché la
  risposta sovrascriverebbe quello che si sta scrivendo.

## Password SMTP

- La password SMTP non arriva mai al browser: è il segreto di un sistema esterno (la casella email del lido), quindi la
  risposta dice solo se è salvata (`hasSmtpPassword`).
- Il campo parte vuoto con il testo grigio "Password impostata: scrivi per cambiarla" o "Nessuna password impostata"
  (`fields.smtp_password.saved` / `.missing`); l'occhietto mostra solo quello che si sta scrivendo.
- "Salva" e "Invia email test" mandano `smtpPassword: null` (il server usa quella salvata) oppure la password nuova
  scritta. Dopo un salvataggio riuscito il campo torna vuoto.
- La password salvata non può andare a un'altra destinazione: se server, porta, nome utente o tipo di sicurezza sono
  diversi da quelli salvati, va riscritta. Errore subito sotto il campo, "Hai cambiato i parametri di connessione:
  riscrivi la password o inseriscine una nuova." (`invalidate.email_configuration.smtp_password.retype`); Salva spento e
  Invia email test non parte. Il contratto chiede lo stesso controllo al server.
- Nessun hash nel browser: il server deve poterla usare e la salva cifrata; il trasporto lo protegge HTTPS.

## Bottoni

- "Reset" rifà la GET e riscrive tutti i campi (ciò che era stato scritto si perde), con lo spinner sul bottone, e
  rimette la pagina in sola lettura con "Sì" di nuovo premibile.
- "Invia email test" manda i valori presenti nei campi, anche non salvati, con lo spinner. Se riesce compare
  `app-message-popup` "Email di test inviata".
- "Salva" manda gli stessi valori, con lo stesso corpo, con lo spinner. Se riesce compare `app-message-popup`
  "Configurazione salvata" e tutti i campi tornano in sola lettura con "Sì" di nuovo attivo, come dopo Reset. Se fallisce
  i campi restano modificabili, per correggere.
- Mentre uno dei tre bottoni lavora, gli altri due sono spenti.

## Validazioni ed errori

- Validazioni del frontend: la porta e la password da riscrivere quando cambiano i parametri di connessione.
- La porta è obbligatoria, da 1 a 65535 (`required`, `min`, `max` dei Signal Forms); si possono scrivere solo cifre.
  L'errore compare sotto il campo e Salva resta spento finché non è corretto.
- "Invia email test" con un errore non invia nulla e mostra gli errori.
- Un errore del backend su un campo (es. server SMTP vuoto) va sotto quel campo e Salva resta spento finché il campo non
  cambia; un errore che non riguarda un campo va sotto il riquadro.
- In sola lettura (prima di "Sì") i campi non mostrano errori: lì il messaggio va sotto il riquadro.
- Le chiamate passano da `submit()` (vedi `form-e-validazioni.md`).

## API

In `SystemService`:

- `GET /api/system/email-configuration?idProperty=…` (`emailConfiguration`): all'apertura, a Reset e quando cambia la
  lingua (le modifiche non salvate si perdono). Con GET lo stabilimento viaggia nell'indirizzo, non nel corpo.
  Risposta `EmailConfigurationData`: `senderMailAddress`, `senderName`, `smtpServerAddress`, `smtpPort` (numero),
  `smtpUsername`, `hasSmtpPassword`, `smtpSecurity` (`None`, `Ssl`, `Tls`).
- `POST /api/system/send-test-email` (`sendTestEmail`): corpo `EmailConfigurationRequest`, cioè `idProperty` e gli
  stessi campi della GET, ma con `smtpPassword` invece di `hasSmtpPassword`, e la porta come numero.
- `POST /api/system/save-email-configuration` (`saveEmailConfiguration`): stesso corpo.
