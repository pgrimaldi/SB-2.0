# Configurazione email

Rotta `/settings/email-configuration`, vista `views/settings/email-configuration/` (`EmailConfiguration`), testi
`management.settings.email_configuration.*`.

## Struttura

- In cima, fuori dal riquadro dei campi, un secondo riquadro chiede "Vuoi sovrascrivere le impostazioni predefinite?"
  con il bottone "Sì" (stesso stile e misura di Salva, testi `management.settings.email_configuration.override.*`).
- Fra i due riquadri una riga dice come sta la configurazione **salvata** (vedi "Esito della prova").
- Riquadro bianco (mixin `settings-panel` di `styles/_settings-panel.scss`, condiviso con Assistenza e guide): ogni riga
  (`app-form-row`) ha a sinistra il nome dell'impostazione, con l'asterisco se è obbligatoria, e la spiegazione, a destra
  il campo. Il nome dei campi con l'asterisco usa la pipe `translate`, non `appI18nText`, perché l'asterisco gli stia accanto.
- Righe, nell'ordine: Usa questa configurazione (`isActive`, interruttore), Indirizzo email mittente*, Nome Mittente,
  Server SMTP*, Porta SMTP*, Nome utente*, Password, Tipo di Sicurezza*, Verifica il certificato (`verifyCertificate`,
  interruttore), Destinatari dell'email di test. Le sei righe centrali sono in `EMAIL_CONFIGURATION_MASKED_FIELDS`
  (`entities/settings/email-configuration/email-configuration-field.ts`).
- Quei sei campi sono nascosti come password: cinque `app-filled-text-field` con `[isPasswordField]="true"` e la porta
  `app-filled-number-field` con `[isMasked]="true"`. L'occhietto in fondo mostra e rinasconde il testo (icone
  `GeneralIcon_EyeStart.svg` con il testo nascosto, `GeneralIcon_EyeClose.svg` con il testo visibile).
- I sei campi hanno `autocomplete="new-password"`, perché il browser non ci inserisca la password salvata dell'utente.
- Tipo di Sicurezza è una tendina con Nessuna, STARTTLS e SSL (enum `SmtpSecurity`: `none`, `starttls`, `ssl`, i codici
  del backend).
- I destinatari si scrivono in un campo solo, separati da virgole (anche punto e virgola, spazi o a capo): la pagina li
  manda come elenco.
- Sotto, a destra, "Invia email test" e "Reset" (blu come Salva, icone bianche `EmailConfiguration_MailSend.svg` e
  `GeneralIcon_DeleteWhite.svg`) e "Salva", della misura del mockup.
- Il resto è più grande del mockup: colonna fino a 64rem, titoli 0.875rem, spiegazioni 0.8125rem, campi alti 2.25rem con
  testo 0.875rem (variabili `--filled-field-*`).

## Sola lettura e "Sì"

- Finché non si preme "Sì" tutti i campi sono in sola lettura (regola `readonly` dei Signal Forms sul form intero, che i
  campi grigi ricevono con `[formField]`; la tendina non si apre) e i due interruttori spenti (`disabled`). Dopo diventano
  modificabili e "Sì" si disattiva.
- Con una configurazione salvata "Sì" mostra subito gli errori di quello che c'è (per esempio una porta 465 senza SSL);
  senza, gli errori arrivano campo per campo.
- Niente si modifica prima che arrivino i dati del server: "Sì" resta spento finché la lettura non è arrivata (dopo un
  errore di lettura, finché Reset non la rilegge). Durante ogni lettura i campi tornano in sola lettura, perché la
  risposta sovrascriverebbe quello che si sta scrivendo.
- Senza configurazione salvata il backend risponde con i testi vuoti e i valori di partenza (porta 465, SSL, certificato
  verificato, configurazione accesa): la pagina li mostra e niente è salvato finché l'utente non salva.

## Esito della prova

- La riga sopra i campi (`email__configuration__verification`, testi `verification.*`) parla sempre della configurazione
  salvata, non di quello che si sta scrivendo: nessuna configurazione (`fromEmail` `null`), spenta, mai provata, ultima
  prova fallita (in rosso, con `lastVerificationError` così com'è) o riuscita, con giorno e ora.
- Dopo "Invia email test" (riuscita o no) e dopo "Salva" la pagina rilegge la configurazione solo per l'esito, senza
  toccare i campi, e solo se non ci sono modifiche non salvate (`readVerification`): il backend registra l'esito solo dei
  valori salvati. Se questa rilettura fallisce la riga resta com'è.

## Password SMTP

- La password SMTP non arriva mai al browser: è il segreto di un sistema esterno (la casella email del lido), quindi la
  risposta dice solo se è salvata (`hasSmtpPassword`).
- Il campo parte vuoto con il testo grigio "Impostata: scrivi la nuova" o "Nessuna password impostata"
  (`fields.smtp_password.saved` / `.missing`); l'occhietto mostra solo quello che si sta scrivendo.
- "Salva" e "Invia email test" mandano `smtpPassword: null` (il server usa quella salvata; vale anche per soli spazi)
  oppure la password nuova così com'è scritta. Dopo un salvataggio riuscito il campo torna vuoto.
- La password salvata non può andare a un'altra destinazione: se server, porta, nome utente, tipo di sicurezza o verifica
  del certificato sono diversi da quelli salvati (`SMTP_CONNECTION_FIELDS`, confrontati con `comparableJson`: i testi senza
  spazi ai lati, un campo vuoto come `null`), va riscritta. Errore subito sotto il campo, "Hai cambiato i parametri di
  connessione: riscrivi la password o inseriscine una nuova." (`invalidate.email_configuration.smtp_password.retype`);
  Salva spento e Invia email test non parte. Il server fa lo stesso controllo.
- Nessun hash nel browser: il server deve poterla usare; il trasporto lo protegge HTTPS.

## Bottoni

- "Reset" rifà la GET e riscrive tutti i campi (ciò che era stato scritto si perde), con lo spinner sul bottone, e
  rimette la pagina in sola lettura con "Sì" di nuovo premibile.
- "Salva" e "Invia email test" restano spenti finché la lettura della configurazione non è riuscita, anche dopo un
  Reset la cui rilettura fallisce (`loaded`), e nel codice non partono senza i dati letti: mai una configurazione
  vuota al posto di quella salvata.
- "Invia email test" manda i valori presenti nei campi, anche non salvati, con lo spinner; senza destinatari è spento. Se
  riesce compare `app-message-popup` "Email di test inviata".
- "Salva" manda gli stessi valori, con lo stesso corpo, con lo spinner. Se riesce compare `app-message-popup`
  "Configurazione salvata" e tutti i campi tornano in sola lettura con "Sì" di nuovo attivo, come dopo Reset. Se fallisce
  i campi restano modificabili, per correggere.
- I testi partono senza spazi ai lati (`trimmedText`); tutti i campi partono sempre, anche quelli non cambiati.
- Controllo di versione: la lettura porta `version`, che "Salva" rimanda nell'header `If-Match`; dopo un salvataggio
  riuscito la pagina tiene la versione nuova dell'`ETag`. La versione copre la configurazione letta (anche se una
  password è salvata), non la password né l'esito della prova. Con 409 `resource.version_conflict` (un altro utente ha
  cambiato la configurazione dopo la lettura) compare `app-version-conflict-popup`, nessun errore sotto il riquadro, e la
  pagina rilegge la configurazione come con Reset (sola lettura, "Sì" di nuovo premibile): l'utente rifà le modifiche.
  "Invia email test" non salva e non manda la versione.
- Mentre uno dei tre bottoni lavora, gli altri due sono spenti.

## Validazioni ed errori

- Obbligatori: mittente, server, porta, nome utente e tipo di sicurezza. Il mittente è un'email (`emailAddress`).
- Lunghezze massime, come le colonne del backend (costanti in `email-configuration-request.ts`): mittente e nome utente
  320, nome mittente 160, server 255; nei campi non si scrive di più (`MAX_LENGTH`).
- La porta va da 1 a 65535; si possono scrivere solo cifre.
- Porta e sicurezza vanno d'accordo: 465 vuole SSL, 587 vuole STARTTLS; l'errore sta sotto il tipo di sicurezza
  (`invalidate.email_configuration.security.*`). Con le altre porte va bene tutto.
- Destinatari: ognuno un'email di al massimo 320 caratteri, al massimo 10
  (`invalidate.email_configuration.test_recipients.*`).
- Salva resta spento finché un campo non va bene; "Invia email test" con un errore non invia nulla e mostra gli errori.
- Un errore del backend su un campo va sotto quel campo e Salva resta spento finché il campo non cambia; un errore che
  non riguarda un campo va sotto il riquadro. La pagina scorre fino al primo (`showFirstError`). Una nuova lettura toglie
  gli errori rimasti (`clearApiFieldErrors`).
- In sola lettura (prima di "Sì") i campi non mostrano errori: lì il messaggio va sotto il riquadro.
- Codici propri: `notification.property_required`, `notification.test_email_failed` (mostra la risposta del server SMTP,
  `args.detail`), `notification.test_email_blocked`, `notification.unavailable`.
- Le chiamate passano da `submit()` (vedi `form-e-validazioni.md`).

## API

In `NotificationService` (`services/api/notification/`; lo stabilimento lo prende il backend dal token), chieste al backend in
`Modifiche per il backend/impostazioni/Configurazione email - Frontend requests.md`:

- `GET /api/notification/email-configuration` (`emailConfiguration`), senza parametri: all'apertura, a Reset, quando cambia
  la lingua e dopo un conflitto di versione (le modifiche non salvate si perdono); solo per l'esito della prova dopo un'email
  di test o un salvataggio. Risposta `EmailConfigurationData`: `fromEmail`, `fromName`, `smtpHost`, `smtpPort`,
  `smtpUsername`, `hasSmtpPassword`, `security`, `verifyCertificate`, `testRecipients`, `isActive`, `lastVerificationAt`,
  `lastVerificationError`, `version`.
- `POST /api/notification/send-test-email` (`sendTestEmail`): corpo `EmailConfigurationRequest`, cioè gli stessi campi della
  GET fino a `isActive`, con `smtpPassword` invece di `hasSmtpPassword`.
- `POST /api/notification/save-email-configuration` (`saveEmailConfiguration(request, version)`): stesso corpo e header
  `If-Match` con la versione letta; risposta 204 con l'header `ETag` della nuova versione; 409
  `resource.version_conflict`.

Nei mock (`services/mocks/notification/`) un server SMTP che comincia con "errore" fa fallire la prova.
