# Configurazione email - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Configurazione email** del gestionale funzioni come la vuole
il frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi, e quali scelte vanno chieste allo sviluppatore prima di scrivere codice.

La pagina è `/settings/email-configuration`: la configurazione SMTP con cui lo stabilimento manda le email (mittente,
server, porta, utente, password, sicurezza, controllo del certificato, destinatari della prova e interruttore), in sola
lettura finché l'utente non sceglie di sovrascriverla, con i bottoni Invia email test, Reset e Salva. La password SMTP
non arriva mai al browser: la lettura dice solo se è salvata.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET | `/api/notification/email-configuration` | La configurazione dello stabilimento, con l'esito dell'ultima prova |
| 2 | POST | `/api/notification/send-test-email` | Manda l'email di prova con i valori scritti nella pagina |
| 3 | POST | `/api/notification/save-email-configuration` | Salva la configurazione, con il controllo di versione |

Sono tutte API nuove.

## Prima di tutto

- **Enum nelle opzioni EF** (`PROMEMORIA-85-8` di `85-promemoria.md`, da chiudere prima della prima API di ogni modulo): in
  `Module.Notification` `MapEnum` sta solo sul data source (`ModulePersistenceRegistration.cs`, `DesignTimeDbContextFactory.cs`),
  non in `UseNpgsql`. EF tratta gli enum come interi e ogni lettura o scrittura di `EmailChannelSettings` darebbe 500. Serve
  `NotificationEnumMappings` come `Module.Payment/Infrastructure/Infras/PaymentEnumMappings.cs`, usato nel data source e in
  `UseNpgsql`, a runtime e a design-time. Non ne nasce nessuna migration.
- **Progetto**: `Module.Notification.csproj` non ha `FrameworkReference Microsoft.AspNetCore.App`, FluentValidation 12.1.1 e il
  riferimento a `BuildingBlocks.Application`, come li ha `Module.Catalog` (con il `FrameworkReference` i due pacchetti
  `Microsoft.Extensions.*` che ha oggi possono diventare superflui). I validatori si registrano come in `Module.Booking/Module.cs`
  (singleton, senza stato: un controllo asincrono come il DNS non sta lì).
- **Struttura**: endpoint, `DbContext` e dipendenze come li controllano gli `ArchitectureTests`
  (`ModuleConventionArchitectureTests.cs`); porte in `Application/Abstractions`, repository in `Infrastructure/Repositories` come
  vuole `50-standard-di-codifica.md`; `NotificationErrors` in `Domain`, come `Module.Payment/Domain/PaymentErrors.cs`.
- **Entità**: `EmailChannelSettings` ha solo il costruttore per EF ("unico costruttore"): servono la creazione e le modifiche, e il
  suo commento e quello di `Module.cs` vanno aggiornati.
- **`none` al primo salvataggio**: `security` ha `HasDefaultValueSql("'ssl'")` e `SmtpSecurity.None` vale 0, quindi nell'`INSERT` EF
  omette la colonna e il database scrive `ssl`. Serve `.HasSentinel((SmtpSecurity)(-1))`, come in
  `ResourceConsumptionConfiguration.cs:81-85` e in `Informazioni stabilimento - Frontend requests.md`, sezione 8. Non ne nasce
  nessuna migration. `verify_certificate` e `is_active` non hanno il problema.
- **Vecchi dati**: le righe importate hanno `smtp_password_secret_ref` preso dalla migrazione. Che i segreti esistano davvero l'agente
  lo chiede allo sviluppatore. Nel vecchio sistema 13 stabilimenti usavano la **casella e la password di piattaforma**: con quelle
  righe un utente può cambiare `fromEmail` e mandare la prova con la password di Summer Booking. Cosa farne (per esempio non
  importarle) lo decide lo sviluppatore: l'agente glielo segnala.

### Scelte che l'agente chiede allo sviluppatore

1. **Dove si scrive la password**: in tabella c'è solo `smtp_password_secret_ref`, ma `ISecretResolver` legge soltanto, dalla
   configurazione (`ConfigurationSecretResolver`), che si aggiorna solo con un nuovo avvio (17-secrets.md, 3.2). La password salvata
   dalla pagina deve essere **rileggibile subito e da tutte le istanze**. Come scriverla e rileggerla, con quale riferimento e cosa
   succede al segreto vecchio lo decide lo sviluppatore (è la stessa domanda di `Fiscalizzazione - Frontend requests.md`, sezione 7).
2. **Con cosa si invia**: oggi l'unico mittente è `SmtpTransactionalEmailSender`, su `System.Net.Mail.SmtpClient`, che conosce solo
   STARTTLS (`EnableSsl`), non il TLS implicito della porta 465 (`ssl`), e non ha un controllo del certificato per singolo invio
   (`verify_certificate`). Nella soluzione non ci sono altre librerie SMTP: quale usare lo decide lo sviluppatore.
3. **L'uso vero della configurazione**: `SmtpTransactionalEmailSender` legge solo le opzioni di piattaforma (`Notification:Smtp`). Finché
   gli invii dello stabilimento non usano `email_channel_settings`, salvare la pagina non cambia il mittente delle email. Collegarli
   (l'`ISmtpSender` di `notification_email_channel_settings.md`) non fa parte di questo documento; quando succederà, la regola è
   quella del progetto: riga assente o `is_active` `false` → mittente di piattaforma.
4. **`verify_certificate` `false`**: il commento della colonna dice "ammesso solo con motivazione", ma non c'è dove scriverla. Finché lo
   sviluppatore non decide diversamente il backend accetta `false` senza altro: la pagina avverte l'utente.
   Commento e scheda restano in contrasto: l'agente lo segnala (cambiare il commento è una migration).
5. **Quante prove al minuto** (sezione 2): anche se un limite in memoria, che conta per ogni istanza (PROMEMORIA-85-9), va bene.
6. **Il server finto dei test**: sta su loopback o in una rete privata, che la regola sugli indirizzi interni (sezione 2) rifiuta. Come
   provarlo (una porta in `Application/Abstractions` con un finto, o un'opzione che in Production non vale mai).
7. **La versione** (sezione 3), **il tempo massimo** e **il testo** dell'email di prova (sezione 2).
8. **`notification.deliveries`**: esiste `DeliveryKind.Test`, ma il vecchio sistema non registrava la prova. Se la prova crea una riga.
9. **Lunghezza massima della password**: nessuna colonna la dà. Se l'archivio dei segreti ne ha una, 400 `validation.max_length` su
   `smtpPassword`, e il documento si aggiorna perché il frontend la aggiunga.

## Regole comuni

- **Modulo**: `Module.Notification` (schema `notification`), tabella `notification.email_channel_settings`. Oggi il modulo ha dominio,
  persistenza, l'invio delle email di servizio, `OutboundGuard` e `BookingActivityHandler`, ma nessun endpoint. `OutboundGuard` sta in
  `Infrastructure/Outbound`: dall'Application si usa con una porta.
- **Integrazioni esterne** (`35-integrazioni-esterne.md`): contratto in `Application/Abstractions`, client SMTP in
  `Infrastructure/ExternalApi`, checklist della sezione 7, server mai contattato dai test. La prova non si ritenta da sola (non è
  idempotente) e non passa dall'outbox, perché risponde con l'esito.
- **Rotte**: gruppo `/api/notification`, con il `GroupName` che `Boot/Program.cs` ha già fra gli `ApiDocs` (`notification`),
  registrato con `MapNotificationEndpoints` accanto a `app.MapPaymentEndpoints()`.
- **Stabilimento**: lo prende dal token (`ICurrentUser.PropertyId`): nessuna richiesta lo contiene. Ogni API legge e cambia solo la
  riga di quello stabilimento. Token senza stabilimento: 403 `notification.property_required` (nuovo, come
  `payment.property_required`). Che lo stabilimento esista ancora non si controlla, come in `Module.Payment`: nessun 404.
- **Permesso** di tutte e tre, anche della lettura, sulla singola rotta (nel gruppo ci sono anche quelle di
  `Assistenza e guide - Frontend requests.md`, che non lo chiedono): `catalog.settings` (`Permissions.CatalogSettings`: superadmin, admin,
  user1). Senza: 403 `auth.forbidden`. Senza token valido: 401 `auth.unauthenticated`.
- **JSON** in camelCase, con i nomi esatti di questo documento (sono quelli delle colonne). Le date sono istanti UTC in ISO 8601.
- **`security`**: i codici `none`, `starttls`, `ssl` (`Codes.Of(SmtpSecurity)`, uguali ai valori dell'enum del database).
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono.
- **Ordine dei controlli**: 401, 403 `auth.forbidden`, 400 sulle sole regole del corpo, 403 `notification.property_required`, poi gli
  errori della sezione nell'ordine della sua tabella. Si valida prima di leggere lo stabilimento, come `InventoryService.ListAsync`
  (`EnsureValid` e poi `IdAsync`): di `PropertyOf` (`Module.Payment/Endpoints/Endpoints.cs`) si copia solo il 403.
- **Sessione impersonata** dal systemadmin: le sezioni 2 e 3 rispondono 403 `auth.forbidden` (`ICurrentUser.ImpersonatorUserId`
  valorizzato, come `ForbidWhenImpersonated` in `MeService`): usano e cambiano le credenziali dello stabilimento, come il conto Stripe e
  Fisconline. La lettura resta permessa.
- **Valori che non si leggono**: un numero che non entra in un intero a 32 bit, un tipo sbagliato o un corpo non JSON non arrivano ai
  validatori: in Development 400 `validation.failed` senza `errors[]` (`BadHttpRequestException`), negli altri ambienti 400 senza corpo
  (`Boot/Program.cs` non imposta `RouteHandlerOptions.ThrowOnBadRequest`). Nei DTO i campi sono nullable (`string?`, `int?`, `bool?`,
  `string?[]?`), così assente e `null` danno `validation.required`. Il frontend non manda questi valori.
- **Configurazione eliminata** (`deleted_at` valorizzato: le configurazioni disattivate del vecchio sistema): vale come assente. Il
  filtro globale la esclude già, e l'indice unico parziale lascia creare la riga nuova. In tabella un testo vuoto o di soli spazi si
  legge come `null` (senza `fromEmail` per la pagina non c'è configurazione).
- **Codici nuovi**: `notification.property_required`, `notification.test_email_failed` (422 con `args`:
  `DomainException.Unprocessable(code, args)`), `notification.test_email_blocked` (422), `notification.unavailable` (503:
  `new DomainException(code, 503)`). `resource.version_conflict` è il codice comune chiesto anche dalle altre pagine (in
  `CommonErrorCodes`), non `booking.version_conflict`.
- **Password**: mai nelle risposte, nei log, nel registro, nelle tabelle o nei messaggi d'errore. Vale anche per quella scritta nella
  pagina e non salvata.
- **Log** (`15-logs.md`, 3 e 8): vietate le credenziali SMTP e i dati personali: né password né utente, mittente o destinatari; niente
  log di protocollo della libreria SMTP e niente eccezioni con gli indirizzi.
- **Registro**: classe `AuditActionCodes.Notification` (`Module` `notification`, `Targets.EmailSettings` `email_settings`), con la sua
  voce in `All` scritta come `new(codice, AuditCategory.Operator, Notification.Module)`: l'helper `Operator(...)` mette il modulo
  `identity`. Serve il testo `Action_…` nei 7 `AuditExport*.resx`. La voce si scrive con `IAuditTrail` dopo il salvataggio, con `TargetId`
  uguale allo stabilimento (la tabella non ha un `public_id`). Una voce per salvataggio riuscito che cambia qualcosa, nessuna dopo un
  errore, nessuna per la prova. È un codice in più rispetto a `Gestione utenti - Frontend requests.md`, sezione 9; il frontend lo
  aggiungerà alle azioni del Log.

| Azione | `code` | Metadati |
|---|---|---|
| Sezione 3 | `notification.email_settings_updated` | `isActive`, `passwordChanged`; mai indirizzi, server, utente o password |

## 1. GET /api/notification/email-configuration

La pagina la chiama all'apertura, a Reset e a Riprova, al cambio di lingua, dopo un conflitto di versione e, per aggiornare
l'esito della prova, dopo un'email di test se non ci sono modifiche non salvate (l'esito vale solo con la `version` che ha
già). Nessun parametro.

### Risposta

200:

```json
{
  "fromEmail": "prenotazioni@lido-esempio.example",
  "fromName": "Lido Esempio",
  "smtpHost": "smtp.lido-esempio.example",
  "smtpPort": 587,
  "smtpUsername": "prenotazioni@lido-esempio.example",
  "hasSmtpPassword": true,
  "security": "starttls",
  "verifyCertificate": true,
  "testRecipients": ["prova@lido-esempio.example"],
  "isActive": true,
  "lastVerificationAt": "2026-11-20T10:15:00Z",
  "lastVerificationError": null,
  "version": "5c0e7d19"
}
```

| Campo | Tipo | Da dove | Senza configurazione |
|---|---|---|---|
| `fromEmail` | string \| null | `from_email` | `null` |
| `fromName` | string \| null | `from_name` | `null` |
| `smtpHost` | string \| null | `smtp_host` | `null` |
| `smtpPort` | number (intero) | `smtp_port` | `465` |
| `smtpUsername` | string \| null | `smtp_username` | `null` |
| `hasSmtpPassword` | boolean | `smtp_password_secret_ref` valorizzato | `false` |
| `security` | `none` \| `starttls` \| `ssl` | `security` | `ssl` |
| `verifyCertificate` | boolean | `verify_certificate` | `true` |
| `testRecipients` | string[] | `test_recipients` | `[]` |
| `isActive` | boolean | `is_active` | `true` |
| `lastVerificationAt` | string (UTC) \| null | `last_verification_at` | `null` |
| `lastVerificationError` | string \| null | `last_verification_error` | `null` |
| `version` | string | vedi sezione 3 | una stringa, come gli altri |

Senza configurazione risponde **200** con i valori dell'ultima colonna (i default delle colonne), **non 404**: la pagina li mostra come
punto di partenza e da `fromEmail` `null` capisce che non c'è niente di salvato. Non crea la riga.

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso |
| 403 | `notification.property_required` | Token senza stabilimento |

## 2. POST /api/notification/send-test-email

"Invia email test": prova i valori che l'utente vede nella pagina, anche non ancora salvati.

### Richiesta

Lo stesso corpo della sezione 3, con le stesse regole sui campi e sulla password, e in più:

- **almeno un destinatario**: `testRecipients` vuoto dà `validation.required` su `testRecipients`.

Non riceve `If-Match` e **non cambia la versione**: l'utente prova la configurazione e subito dopo la salva con la versione letta
all'apertura. `isActive` resta obbligatorio ma non conta: si prova anche una configurazione spenta. Senza versione, se un altro
utente ha cambiato la configurazione, la prova può rispondere 400 su `smtpPassword`: è giusto così.

Con i campi ancora in sola lettura la pagina manda i valori letti, se c'è una configurazione salvata.

### Regole

- **Invio**: dal server, porta, utente, password, sicurezza e controllo del certificato della richiesta, con `smtpPassword` `null`
  quella salvata; senza nessuna password salvata, senza autenticazione. `none` non cifra, `starttls` e `ssl` falliscono se il canale
  non si cifra: mai un ripiego in chiaro.
- **Segreto mancante**: se `smtp_password_secret_ref` è valorizzato ma il segreto non si trova (`ISecretResolver.Resolve` dà `null`),
  503 `notification.unavailable`: mai un invio senza autenticazione, e nessun esito.
- **Messaggio**: uno solo, a tutti i `testRecipients`, da `"fromName" <fromEmail>` (solo l'indirizzo con `fromName` `null`). Oggetto e
  testo fissi, nella lingua della richiesta (`IRequestLanguage`, le 7 lingue: oggi Notification ha solo it ed en, PROMEMORIA-85-12),
  senza dati dello stabilimento o dei clienti: le parole le decide lo sviluppatore.
- **Rifiuto di una parte dei destinatari**: basta un destinatario rifiutato per il 422. Gli altri possono aver ricevuto il messaggio,
  e anche dopo un tempo scaduto può essere arrivato.
- **`OutboundGuard`**: ogni destinatario passa da `Allows`, con l'indirizzo davvero usato. Se anche uno solo non è ammesso l'email non
  parte, l'esito non si registra e risponde 422 `notification.test_email_blocked` (oggi il mittente di piattaforma la sopprime in
  silenzio: qui l'utente crederebbe che sia arrivata).
- **Esito**: se la richiesta è uguale alla configurazione salvata e `smtpPassword` è `null`, si scrivono `last_verification_at` (l'istante
  del tentativo) e `last_verification_error` (`null` se riuscita, altrimenti il testo di `detail`). Uguale vuol dire **tutti i campi**,
  `isActive` e l'ordine dei destinatari compresi, maiuscole comprese, senza gli spazi ai lati né nella richiesta né in tabella. Con
  valori diversi, o con una password scritta, non si registra niente: l'avviso della pagina parla della configurazione salvata.
- **Prova e salvataggio insieme**: la prova dura secondi. L'esito si scrive solo se in quel momento, in modo atomico, configurazione e
  password sono ancora quelle provate; altrimenti si scarta e la risposta non cambia. Mai un 500, e mai un 409 a chi salva per colpa
  della prova.
- **Pagina chiusa durante la prova**: la richiesta si annulla; se il messaggio è già partito l'esito si scrive lo stesso. Se scrivere
  l'esito fallisce a email inviata, la risposta resta 204.
- **Prova fallita** (server che non risponde entro il tempo, nome che non si risolve, certificato, autenticazione, mittente o
  destinatario rifiutati): 422 `notification.test_email_failed` con `args.detail`, la risposta del server SMTP o il motivo, che la
  pagina mostra così com'è dopo "Risposta del server:": **sempre presente, al massimo 200 caratteri, su una riga, senza la password,
  senza lo scambio di autenticazione e senza dettagli interni** (stack, IP interni, percorsi). Quando non è la risposta del server
  (tempo scaduto, nome, certificato) è un breve testo tecnico in inglese (es. `Connection timed out`): si salva, e lo leggono utenti
  di lingue diverse.

  ```json
  {
    "type": "https://errors.summerbooking/notification/test_email_failed",
    "title": "notification.test_email_failed",
    "status": 422,
    "code": "notification.test_email_failed",
    "traceId": "00-...",
    "args": { "detail": "535 5.7.8 Authentication failed" }
  }
  ```

- **Niente indirizzi interni** (SSRF, OWASP A10:2021): il server SMTP lo scrive l'utente. Un `smtpHost` di loopback, privato, link-local,
  condiviso o riservato dà 400 `validation.invalid` su `smtpHost`, senza aprire la connessione: `127.0.0.0/8`, `10.0.0.0/8`,
  `172.16.0.0/12`, `192.168.0.0/16`, `169.254.0.0/16`, `100.64.0.0/10`, `0.0.0.0/8`, `::1`, `fc00::/7`, `fe80::/10`, gli IPv4 scritti
  come IPv6, `localhost` e le forme numeriche che .NET legge come indirizzi (`127.1`, `2130706433`). Nella prova e, quando ci saranno,
  negli invii veri si controlla **l'indirizzo a cui ci si collega**: un nome che si risolve in più indirizzi, di cui uno interno, si
  rifiuta; ci si collega all'indirizzo controllato, e il certificato si verifica sul nome scritto in `smtpHost`.
- **Nel salvataggio niente DNS**: si rifiuta solo un indirizzo o un nome interno scritto com'è (sezione 3). Un nome che non si risolve
  si salva: il controllo vero è al collegamento, dove dà 422.
- **Limite di frequenza**: oltre il limite 429 `auth.too_many_attempts` (`CommonErrorCodes.TooManyRequests`), che il frontend conosce
  già. Contano tutti i tentativi, anche quelli non validi. Oggi ci sono solo i limiti `Auth` e `PublicForms`, per IP: qui serve per
  stabilimento, dal claim del token. `UseRateLimiter` (`Program.cs:150`) viene prima dell'endpoint, quindi il 429 arriva prima del
  corpo; se invece il limite sta nel servizio deve venire prima di ogni DNS. Quante prove lo decide lo sviluppatore.
- **Tempo massimo** totale (DNS, connessione, TLS, autenticazione e invio): lo decide lo sviluppatore, al di sotto di quello del proxy.
  DNS, segreto e invio non stanno dentro una transazione del database.
- **Risposta**: 204 senza corpo, solo se il server SMTP ha accettato il messaggio.

Errori, nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso |
| 429 | `auth.too_many_attempts` | Troppe prove |
| 403 | `auth.forbidden` | Sessione impersonata |
| 400 | `validation.failed` | Le regole dei campi (sezione 3) e almeno un destinatario, in `errors[]` |
| 403 | `notification.property_required` | Token senza stabilimento |
| 400 | `validation.failed` | `validation.required` su `smtpPassword` (regola della password, sezione 3) e `validation.invalid` su `smtpHost` per un indirizzo interno, insieme in `errors[]` |
| 422 | `notification.test_email_blocked` | `OutboundGuard` non ammette un destinatario: niente è partito |
| 503 | `notification.unavailable` | Segreto che non si trova o archivio che non risponde: niente è partito, nessun esito |
| 422 | `notification.test_email_failed` | La prova è fallita, con `args.detail` |

## 3. POST /api/notification/save-email-configuration

"Salva". Crea la configurazione dello stabilimento o la aggiorna.

### Richiesta

Header `If-Match: "<version>"` con la versione letta. Il corpo non la contiene:

```json
{
  "fromEmail": "prenotazioni@lido-esempio.example",
  "fromName": "Lido Esempio",
  "smtpHost": "smtp.lido-esempio.example",
  "smtpPort": 587,
  "smtpUsername": "prenotazioni@lido-esempio.example",
  "smtpPassword": null,
  "security": "starttls",
  "verifyCertificate": true,
  "testRecipients": ["prova@lido-esempio.example"],
  "isActive": true
}
```

Il frontend manda **sempre tutti i campi**, anche quelli non cambiati, con i testi senza spazi ai lati; il backend li toglie comunque,
e conta le lunghezze senza. Un testo vuoto o di soli spazi vale `null`. La pagina salva solo dopo "Sì".

| Campo | Tipo | Regola | Errore |
|---|---|---|---|
| `fromEmail` | string | Obbligatorio, al massimo 320 caratteri, un indirizzo email | `validation.required`, `validation.max_length`, `validation.email`, `validation.invalid` |
| `fromName` | string \| null | Facoltativo, al massimo 160 caratteri | `validation.max_length`, `validation.invalid` |
| `smtpHost` | string | Obbligatorio, al massimo 255 caratteri; non interno, dopo il controllo di versione | `validation.required`, `validation.max_length`, `validation.invalid` |
| `smtpPort` | number (intero) | Obbligatorio, da 1 a 65535 | `validation.required`, `validation.out_of_range` con `from` 1 e `to` 65535 |
| `smtpUsername` | string | Obbligatorio, al massimo 320 caratteri | `validation.required`, `validation.max_length`, `validation.invalid` |
| `smtpPassword` | string \| null | `null` mantiene quella salvata; vedi sotto | `validation.required`, `validation.invalid` |
| `security` | string | Obbligatorio, `none`, `starttls` o `ssl`; 465 vuole `ssl`, 587 vuole `starttls` | `validation.required`, `validation.invalid` |
| `verifyCertificate` | boolean | Obbligatorio | `validation.required` |
| `testRecipients` | string[] | Obbligatorio, anche vuoto; al massimo 10, ognuno un indirizzo email di al massimo 320 caratteri | `validation.required`, `validation.invalid` (sul campo, non sull'elemento) |
| `isActive` | boolean | Obbligatorio | `validation.required` |

- **Email**: `CommonRules.OptionalEmail` si ferma a 255 caratteri, la colonna ne ha 320: serve una regola propria, con lo stesso
  `EmailAddress()`. Il frontend controlla allo stesso modo (una sola `@`, né prima né ultima). Quella regola è più larga della libreria
  SMTP: `foo,bar@x` passa ma `MailAddress` lancia un'eccezione, `<a@b>` e `a b@c` vengono interpretati. Un testo che la libreria non
  legge come **solo indirizzo** dà 400 (`validation.email` su `fromEmail`, `validation.invalid` su `testRecipients`), mai un 500.
- **`testRecipients`**: un elemento `null` o vuoto dà `validation.invalid`. I ripetuti non si vietano né si uniscono: il frontend li
  manda una volta sola.
- **Coerenza di porta e sicurezza**: è quella del progetto della tabella (465 ⇒ `ssl`, 587 ⇒ `starttls`); sulle altre porte vale
  qualsiasi sicurezza. Una configurazione vecchia incoerente si legge com'è: la pagina chiede di correggerla.
- **Testi su una riga**: dopo aver tolto gli spazi ai lati, un carattere di controllo (a capo, tabulazione, separatori di riga Unicode) in
  un testo, nella password (dove gli spazi non si tolgono) o in un destinatario dà `validation.invalid` su quel campo: finirebbe nelle
  intestazioni dell'email o nei comandi SMTP. Se il campo ha già un altro errore resta quello. Dalla pagina può arrivare solo
  incollando una tabulazione.
- **`smtpPassword`**: la password si tiene così come arriva; vuota o di soli spazi vale `null`. `null` mantiene quella salvata.
  **La password salvata non va a un'altra destinazione**: se ce n'è una e la richiesta ha `smtpPassword` `null` con `smtpHost`,
  `smtpPort`, `smtpUsername`, `security` o `verifyCertificate` diversi da quelli salvati (confronto esatto, maiuscole comprese, senza gli
  spazi ai lati né nella richiesta né in tabella), 400 `validation.required` su `smtpPassword`.
  Altrimenti chi può cambiare la configurazione la farebbe arrivare a un server proprio o in chiaro. Senza una password salvata
  `null` è ammesso (server senza autenticazione). Una password salvata si cambia ma non si toglie.
- **Esito della prova**: se cambiano la password, `fromEmail` o uno di quei cinque campi, `last_verification_at` e
  `last_verification_error` tornano `null` (la prova vecchia non vale più). Altrimenti restano.
- **Salvataggio che non cambia niente**: con la versione attuale 204 e la stessa versione nell'`ETag`, senza voce nel registro.
- **Password e riga insieme o per niente**: se la scrittura del segreto o della riga fallisce, restano la configurazione e la password
  di prima. Segreto che non si riesce a scrivere: 503 `notification.unavailable`.
- **Due salvataggi insieme** della stessa versione che cambiano in modo diverso i dati coperti, anche il primo di uno stabilimento
  senza riga: uno 204 e uno 409, mai un 500 per l'indice unico e mai due righe. Se cambia solo la password la versione resta la stessa
  e possono riuscire tutti e due. **Mai i dati di una richiesta con la password dell'altra**: controllo, segreto e riga sono una cosa
  sola.
- **Risposta**: 204 senza corpo, con la nuova versione nell'header `ETag: "<version>"`. Frontend e API stanno sulla stessa origine
  (`/api`): se non fosse così, il CORS (`SecuritySetup.cs`) dovrebbe esporre l'header `ETag`.
- **Registro**: `notification.email_settings_updated`.

Errori, nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso, o sessione impersonata |
| 400 | `validation.failed` | Le regole dei campi, in `errors[]` con `field` uguale al nome nel corpo |
| 403 | `notification.property_required` | Token senza stabilimento |
| 409 | `resource.version_conflict` | Configurazione cambiata dopo la lettura, o `If-Match` assente o illeggibile: niente cambia |
| 400 | `validation.failed` | `validation.required` su `smtpPassword` e `validation.invalid` su `smtpHost` per un indirizzo interno, insieme in `errors[]` |
| 503 | `notification.unavailable` | L'archivio dei segreti non risponde: niente cambia |

### Controllo di versione

Senza un controllo, se due utenti cambiano la configurazione email vince chi salva per ultimo: Salva rimanda tutta la
configurazione letta e cancella, senza che nessuno se ne accorga, quello che un altro utente ha salvato nel frattempo
(per esempio un altro server). Il frontend chiede lo schema di **concorrenza ottimistica** che il backend ha già progettato
per le prenotazioni offline (`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un
**codice di errore unico per tutte le API**: `resource.version_conflict`, senza l'entità e la versione attuale nel corpo.

**Non è quello che Booking ha già per le prenotazioni** (`Endpoints.cs:253-255`, `ReservationService.cs:114-115`): lì l'`If-Match` è un
numero, se manca o non si legge non si controlla niente, e il conflitto è 412 o 409 `booking.version_conflict`. Qui valgono le regole
sotto: con un altro stato o un altro codice la pagina non rileggerebbe i dati.

- **`version` nella lettura**: stringa opaca, allo stesso livello degli altri campi. Cambia ogni volta che cambia uno dei dati coperti,
  e solo allora: due letture senza salvataggi in mezzo danno la stessa versione.
- **Cosa copre**: i dati che la lettura restituisce e il salvataggio riscrive (da `fromEmail` a `isActive`, `hasSmtpPassword` compreso).
  **Non la password**: il frontend non la legge mai e la manda solo quando l'utente ne scrive una nuova, quindi un cambio della sola
  password non cambia la versione (la **prima** password sì, perché cambia `hasSmtpPassword`). **Non l'esito della prova**
  (`lastVerificationAt`, `lastVerificationError`): chi prova e subito dopo salva non deve ricevere un 409. Ogni stabilimento ha la
  sua.
- **`If-Match`**: assente, illeggibile o con una versione che non è più quella attuale, 409 `resource.version_conflict` senza `errors[]`.
- **Ordine**: prima le regole dei campi (400), poi la versione (409), poi password e server interno (400). Con una versione vecchia
  l'utente non ha mai visto la configurazione salvata e deve ricevere il 409, non un errore sulla password.
- **Configurazione mai salvata**: anche senza riga la lettura dà una `version`, e il primo salvataggio con quella riesce. Se un altro
  utente salva per primo, chi salva dopo con la stessa versione riceve il 409.
- **Come farlo lo sceglie lo sviluppatore** (né il token `xmin` di Npgsql né `updated_at`, che `AuditInterceptor` aggiorna, vanno bene
  da soli, perché cambiano anche con l'esito della prova; un hash dei dati coperti, un contatore...): l'agente glielo chiede prima di
  scrivere codice. Il frontend tratta la versione come una stringa opaca e non la confronta con altre, tranne che per sapere se l'esito
  riletto è della configurazione che mostra.

## Cosa fa il frontend

- **Lettura fallita**: messaggio e Riprova; Sì, Salva e Invia email test restano spenti. Salva rimanda sempre tutto il modello letto,
  con le modifiche.
- **Avviso** sopra i campi, sulla configurazione salvata: nessuna configurazione, configurazione spenta, mai provata, ultima prova
  fallita (con `lastVerificationError`) o riuscita.
- **Validazioni**: le stesse della sezione 3 (obbligatori, lunghezze, email, porta, porta e sicurezza, al massimo 10 destinatari,
  password da riscrivere), prima di chiamare. Gli errori su un campo vanno sotto quel campo, gli altri sopra i bottoni.
- **Dopo un salvataggio** non rilegge: azzera da sé l'esito della prova con la stessa regola della sezione 3.
- **409**: avviso "Nel frattempo un altro utente ha modificato questi dati: li abbiamo ricaricati. Rifai le tue modifiche." e rilettura,
  come Reset; l'avviso compare a rilettura riuscita. Dopo ogni salvataggio riuscito tiene la versione dell'`ETag`.

## Verifica

Con un token di un utente dello stabilimento che può cambiare le impostazioni. Configurazioni, password ed esiti si preparano
nel database di prova; il server SMTP è sempre un finto (scelta 6) e `Notification:Outbound:Mode` è `Allowlist` con i destinatari
usati.

1. Ogni API con un token senza il permesso: 403 `auth.forbidden`; senza token: 401; con un token senza stabilimento: 403
   `notification.property_required`. Sessione impersonata: 403 `auth.forbidden` nelle due `POST`, 200 nella `GET`. Un corpo che non si
   legge: 400. `ArchitectureTests` passano.
2. `GET` senza configurazione, e con una configurazione eliminata: 200 con `fromEmail`, `fromName`, `smtpHost` e `smtpUsername` `null`,
   `smtpPort` 465, `security` `ssl`, `verifyCertificate` e `isActive` `true`, `testRecipients` `[]`, `hasSmtpPassword` `false`, esito
   `null` e una `version`; nessuna riga creata. Con una configurazione: i suoi valori, mai la password né il riferimento al segreto.
3. `save-email-configuration` con quella `version`: 204 con `ETag`, una riga; la `GET` risponde con i valori salvati, i testi senza
   spazi ai lati, `hasSmtpPassword` `true` se c'era una password e la versione dell'`ETag`. Un secondo `POST` con la versione di
   prima: 409 `resource.version_conflict` senza `errors[]`; niente cambia, nemmeno la password. Due `POST` insieme con la stessa
   versione: uno 204 e uno 409, una sola riga.
4. Senza `If-Match`, o illeggibile: 409. Due `GET` senza salvataggi in mezzo: la stessa `version`. Un salvataggio di un altro
   stabilimento, o che non cambia niente, non la cambia. Primo salvataggio con `none` e la porta 25: la `GET` risponde `none`.
5. 400 con il suo `errors[]`, e niente cambia: ogni campo obbligatorio assente, `null` o vuoto; `fromEmail` di 321 caratteri e senza
   chiocciola; `fromName` di 161; `smtpHost` di 256; `smtpUsername` di 321; porta 0 e 65536; `security` sconosciuto, `starttls` con 465,
   `ssl` e `none` con 587; 11 destinatari, un destinatario `null`, vuoto, non valido e uno di 321 caratteri; un a capo in
   mezzo a `fromName`, alla password, all'utente e a un destinatario; `foo,bar@x` e `<a@b>` come mittente e come destinatario.
   `fromName` `null` e `testRecipients` `[]`: 204. `none` con la porta 25: 204.
6. Con una password salvata: un `POST` con `smtpPassword` `null` e gli stessi parametri di connessione: 204 e la password resta quella;
   con `smtpHost`, `smtpPort`, `smtpUsername`, `security` o `verifyCertificate` diversi: 400 `validation.required` su `smtpPassword`; con
   la password nuova: 204, e quella nuova si rilegge subito, anche da un'altra istanza. Un cambio della sola password: `ETag` uguale
   alla versione di prima; la prima password la cambia. Password di soli spazi: resta quella salvata. Senza password salvata `null` è
   ammesso. Due salvataggi insieme con server e password diversi: mai il server di uno con la password dell'altro.
7. L'utente A legge; l'utente B salva con un altro `smtpHost`; A salva con la sua versione e `smtpPassword` `null`: 409, non 400. Con la
   versione vecchia e `smtpPort` 0: 400 su `smtpPort`.
8. `send-test-email` con i valori salvati e `smtpPassword` `null`: 204, l'email arriva a ogni destinatario con il mittente
   `"fromName" <fromEmail>`, `lastVerificationAt` è valorizzato, `lastVerificationError` `null` e la `version` è quella di prima; poi
   `save-email-configuration` con la versione letta prima: 204. Con `isActive` `false` salvato: 204 e l'esito si registra.
9. `send-test-email` con il server che rifiuta le credenziali: 422 `notification.test_email_failed` con `args.detail` di al massimo 200
   caratteri, senza password, e lo stesso testo in `lastVerificationError`. Con valori non salvati o con una password scritta, riuscita
   o fallita: l'esito nel database non cambia. Un salvataggio di un altro utente durante la prova: 204 e l'esito resta `null`.
10. `send-test-email` con `testRecipients` `[]`: 400 `validation.required` su `testRecipients`. Con `Notification:Outbound:Mode` `Off`, e con
    `Allowlist` e un destinatario fuori elenco: 422 `notification.test_email_blocked`, nessuna email e nessun esito. Oltre il limite di
    frequenza: 429, anche con un corpo non valido; un altro stabilimento può ancora provare.
11. `smtpHost` `127.0.0.1`, `localhost`, `10.0.0.1`, `169.254.169.254` o un nome che si risolve in uno di questi: 400 `validation.invalid` su
    `smtpHost`, senza connessioni aperte (nel salvataggio solo gli indirizzi e i nomi interni scritti com'è). Lo stesso con `::1`,
    `fd00::1`, `0.0.0.0`, `100.64.0.1`, `127.1` e `2130706433`. Un nome che non si risolve: 204 nel salvataggio, 422 nella prova.
12. Con `starttls` un server che non lo offre, e con `verifyCertificate` `true` un certificato non valido: 422; con
    `verifyCertificate` `false` lo stesso certificato passa. Mai un invio in chiaro se la sicurezza non è `none`.
13. Un salvataggio che cambia `smtpHost` (con la password) o `fromEmail` azzera l'esito della prova; uno che cambia solo `fromName` no.
14. Archivio dei segreti che non risponde: 503 `notification.unavailable` in tutte e due le `POST`, e configurazione e password restano
    quelle di prima. Segreto che non si trova: la prova risponde 503, senza invio e senza esito.
15. Nei log e nel registro non compaiono password, utente, mittente e destinatari; il registro ha una voce
    `notification.email_settings_updated` (modulo `notification`, `TargetId` lo stabilimento, i metadati della tabella, tradotta
    nell'esportazione) per ogni salvataggio che cambia qualcosa, e nessuna dopo un errore o una prova.
16. `GET` con una riga vecchia 587 + `ssl`: 200 con quei valori; la prova con quei valori: 400 su `security`. Un destinatario rifiutato
    su due: 422. Email di prova in italiano e in tedesco: oggetto e testo in quella lingua.
