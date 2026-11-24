# Configurazione email

Rotta `/settings/email-configuration`, vista `views/settings/email-configuration/` (`EmailConfiguration`), testi
`management.settings.email_configuration.*`.

## Struttura

- In cima, fuori dal riquadro dei campi, un secondo riquadro chiede "Vuoi sovrascrivere le impostazioni predefinite?"
  con il bottone "Sì" (stesso stile e misura di Salva, testi `management.settings.email_configuration.override.*`).
- Fra i due riquadri una riga dice come sta la configurazione **salvata** (vedi "Esito della prova").
- Riquadro bianco (mixin `settings-panel` di `styles/_settings-panel.scss`, condiviso con Assistenza e guide): ogni riga
  (`app-form-row`) ha a sinistra il nome dell'impostazione, con l'asterisco se è obbligatoria, e la spiegazione, a destra
  il campo. I nomi delle sei righe centrali e di Tipo di Sicurezza usano la pipe `translate`, non `appI18nText`, perché
  l'asterisco stia accanto al nome.
- Righe, nell'ordine: Usa questa configurazione (`isActive`, interruttore), Indirizzo email mittente*, Nome Mittente,
  Server SMTP*, Porta SMTP*, Nome utente*, Password, Tipo di Sicurezza*, Verifica il certificato (`verifyCertificate`,
  interruttore), Destinatari dell'email di test. Le sei righe centrali sono in `EMAIL_CONFIGURATION_MASKED_FIELDS`, le chiavi
  dei testi di tutte in `EMAIL_CONFIGURATION_FIELD_KEYS` (`entities/settings/email-configuration/email-configuration-field.ts`).
- Quei sei campi sono nascosti come password: cinque `app-filled-text-field` con `[isPasswordField]="true"` e la porta
  `app-filled-number-field` con `[isMasked]="true"`. L'occhietto in fondo mostra e rinasconde il testo (icone
  `GeneralIcon_EyeStart.svg` con il testo nascosto, `GeneralIcon_EyeClose.svg` con il testo visibile).
- I sei campi hanno `autocomplete="new-password"`, perché il browser non ci inserisca la password salvata dell'utente.
- Tipo di Sicurezza è una tendina con Nessuna, STARTTLS e SSL (enum `SmtpSecurity`: `none`, `starttls`, `ssl`, i codici
  del backend).
- I destinatari si scrivono in un campo solo, separati da virgole (anche punto e virgola, spazi, a capo e i segni cinesi e
  arabi): la pagina li manda come elenco, ognuno una volta sola (senza distinguere maiuscole e minuscole).
- Sotto, a destra, "Invia email test" e "Reset" (blu come Salva, icone bianche `EmailConfiguration_MailSend.svg` e
  `GeneralIcon_DeleteWhite.svg`) e "Salva", della misura del mockup.
- Il resto è più grande del mockup: colonna fino a 64rem, titoli 0.875rem, spiegazioni 0.8125rem, campi alti 2.25rem con
  testo 0.875rem (variabili `--filled-field-*`).

## Sola lettura e "Sì"

- Finché non si preme "Sì" tutti i campi sono in sola lettura (regola `readonly` dei Signal Forms sul form intero, che i
  campi grigi ricevono con `[formField]`; la tendina non si apre) e i due interruttori spenti (`disabled`); "Salva" è spento.
  Dopo diventano modificabili e "Sì" si disattiva.
- Con una configurazione salvata "Sì" mostra subito gli errori di quello che c'è (per esempio una porta 465 senza SSL);
  senza, gli errori arrivano campo per campo. Toglie anche il messaggio rimasto sotto il riquadro.
- Niente si modifica prima che arrivino i dati del server: "Sì" resta spento durante ogni chiamata e finché la lettura non è
  riuscita. Durante ogni chiamata, e dopo una lettura fallita, i campi sono in sola lettura.
- Lettura fallita: al posto della riga dell'esito compaiono il messaggio e "Riprova" (`table.load_error.retry`), che rilegge come
  Reset.
- Senza configurazione salvata il backend risponde con i testi vuoti e i valori di partenza (porta 465, SSL, certificato
  verificato, configurazione accesa): la pagina li mostra e niente è salvato finché l'utente non salva.

## Esito della prova

- La riga sopra i campi (`email__configuration__verification`, testi `verification.*`) parla sempre della configurazione
  salvata, non di quello che si sta scrivendo: nessuna configurazione (`fromEmail` `null`), spenta, mai provata, ultima
  prova fallita (in rosso, con `lastVerificationError` così com'è, al massimo 200 caratteri) o riuscita, con giorno e ora.
  I valori possibili sono l'enum `EmailVerification`.
- Dopo "Invia email test" (riuscita o no) la pagina rilegge la configurazione solo per l'esito, senza toccare i campi, e solo se
  non ci sono modifiche non salvate (`readVerification`): il backend registra l'esito solo dei valori salvati. L'esito si mostra solo
  se la `version` riletta è quella della pagina. Se questa rilettura fallisce la riga resta com'è.
- Dopo "Salva" non rilegge: se è cambiata la password o un campo di `SMTP_VERIFIED_FIELDS` (connessione e mittente) azzera
  l'esito, come fa il backend.

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

- "Reset" chiede prima di perdere le modifiche (`confirmDiscard`), poi rimette i valori salvati, toglie gli errori del backend
  e rifà la GET, con lo spinner sul bottone; la pagina torna in sola lettura con "Sì" di nuovo premibile.
- "Salva" e "Invia email test" restano spenti finché la lettura della configurazione non è riuscita, anche dopo un
  Reset la cui rilettura fallisce (`loaded`), e nel codice non partono senza i dati letti: mai una configurazione
  vuota al posto di quella salvata.
- "Invia email test" manda i valori presenti nei campi, anche non salvati, con lo spinner; senza destinatari, o prima di "Sì"
  senza una configurazione salvata, è spento. Se riesce compare `app-message-popup` "Email di test inviata".
- "Salva" è spento prima di "Sì".
- "Salva" manda gli stessi valori, con lo stesso corpo, con lo spinner. Se riesce compare `app-message-popup`
  "Configurazione salvata" e tutti i campi tornano in sola lettura con "Sì" di nuovo attivo, come dopo Reset. Se fallisce
  i campi restano modificabili, per correggere.
- I testi partono senza spazi ai lati (`trimmedText`); tutti i campi partono sempre, anche quelli non cambiati.
- Controllo di versione: la lettura porta `version`, che "Salva" rimanda nell'header `If-Match`; dopo un salvataggio
  riuscito la pagina tiene la versione nuova dell'`ETag`. La versione copre la configurazione letta (anche se una
  password è salvata), non la password né l'esito della prova. Con 409 `resource.version_conflict` (un altro utente ha
  cambiato la configurazione dopo la lettura) la pagina rilegge la configurazione come con Reset (sola lettura, "Sì" di nuovo
  premibile) e, a rilettura riuscita, mostra `app-version-conflict-popup`: l'utente rifà le modifiche.
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
- Dopo "Sì" Salva resta spento finché un campo non va bene; "Invia email test" con un errore non invia nulla e mostra gli errori.
- Un errore del backend su un campo va sotto quel campo e Salva resta spento finché il campo non cambia o finché Reset non lo
  toglie (`clearApiFieldErrors`); un errore che non riguarda un campo va sotto il riquadro. La pagina scorre fino al primo
  (`showFirstError`).
- In sola lettura (prima di "Sì") Signal Forms non valida e i campi non mostrano errori: un errore del backend su un campo va
  sotto il riquadro con il nome del campo davanti (`errorField`, es. "Server SMTP: Il valore non è valido."), e non resta sul campo.
- Codici propri: `notification.property_required`, `notification.test_email_failed` (seguito da "Risposta del server: …",
  `server_answer`, quando c'è `args.detail`), `notification.test_email_blocked`, `notification.unavailable`; in più 429
  `auth.too_many_attempts` (troppe prove). Un server interno (`127.0.0.1`, `localhost`…) e i caratteri di controllo li rifiuta
  solo il backend, con `validation.invalid` sotto il campo.
- Una password salvata si cambia ma non si toglie.
- Le chiamate passano da `submit()` (vedi `form-e-validazioni.md`).

## API

In `NotificationService` (`services/api/notification/`; lo stabilimento lo prende il backend dal token), chieste al backend in
`Modifiche per il backend/impostazioni/Configurazione email - Frontend requests.md`:

- `GET /api/notification/email-configuration` (`emailConfiguration`), senza parametri: all'apertura, a Reset e a Riprova, quando
  cambia la lingua e dopo un conflitto di versione (le modifiche non salvate si perdono); solo per l'esito della prova dopo
  un'email di test. Non la ripete quando dell'utente cambia altro (ruolo, email). Risposta `EmailConfigurationData`: `fromEmail`, `fromName`, `smtpHost`, `smtpPort`,
  `smtpUsername`, `hasSmtpPassword`, `security`, `verifyCertificate`, `testRecipients`, `isActive`, `lastVerificationAt`,
  `lastVerificationError`, `version`.
- `POST /api/notification/send-test-email` (`sendTestEmail`): corpo `EmailConfigurationRequest`, cioè gli stessi campi della
  GET fino a `isActive`, con `smtpPassword` invece di `hasSmtpPassword`.
- `POST /api/notification/save-email-configuration` (`saveEmailConfiguration(request, version)`): stesso corpo e header
  `If-Match` con la versione letta; risposta 204 con l'header `ETag` della nuova versione; 409
  `resource.version_conflict`.

Nei mock (`services/mocks/notification/`) il nome del server SMTP sceglie la risposta della prova: se comincia con "errore" la
prova fallisce, con "bloccato" l'ambiente la blocca, con "attesa" sono troppe (429), con "spento" il servizio non risponde.
