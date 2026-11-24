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
  riferimento a `BuildingBlocks.Application`, come li ha `Module.Catalog`. Valgono le regole di `ArchitectureTests`: endpoint
  senza Infrastructure né entità, `DbContext` solo in Infrastructure, porte in `Application/Abstractions`, repository in
  `Infrastructure/Repositories`, errori in una classe `NotificationErrors`.
- **Entità**: `EmailChannelSettings` ha solo il costruttore per EF: servono la creazione e le modifiche. Non cambiano le colonne.

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
5. **Quante prove al minuto** (sezione 2).

## Regole comuni

- **Modulo**: `Module.Notification` (schema `notification`), tabella `notification.email_channel_settings`. Oggi il modulo ha dominio,
  persistenza e l'invio delle email di servizio, ma nessun endpoint.
- **Rotte**: gruppo `/api/notification`, con il `GroupName` che `Boot/Program.cs` ha già fra gli `ApiDocs` (`notification`),
  registrato con `MapNotificationEndpoints` accanto a `app.MapPaymentEndpoints()`.
- **Stabilimento**: lo prende dal token (`ICurrentUser.PropertyId`): nessuna richiesta lo contiene. Ogni API legge e cambia solo la
  riga di quello stabilimento. Token senza stabilimento: 403 `notification.property_required` (nuovo, come
  `payment.property_required`). Che lo stabilimento esista ancora non si controlla, come in `Module.Payment`: nessun 404.
- **Permesso** di tutte, anche della lettura: `catalog.settings` (`Permissions.CatalogSettings`: superadmin, admin, user1). Senza: 403
  `auth.forbidden`. Senza token valido: 401 `auth.unauthenticated`.
- **JSON** in camelCase, con i nomi esatti di questo documento (sono quelli delle colonne). Le date sono istanti UTC in ISO 8601.
- **`security`**: i codici `none`, `starttls`, `ssl` (`Codes.Of(SmtpSecurity)`, uguali ai valori dell'enum del database).
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono.
- **Ordine dei controlli**: 401, 403 `auth.forbidden`, 400 sulle sole regole del corpo, 403 `notification.property_required`, poi gli
  errori della sezione nell'ordine della sua tabella.
- **Valori che non si leggono**: un numero che non entra in un intero a 32 bit o un tipo sbagliato non arrivano ai validatori: 400
  `validation.failed` senza `errors[]` (`BadHttpRequestException` in `ProblemDetailsWriter.cs`). Nei DTO i campi sono nullable
  (`string?`, `int?`, `bool?`, `string[]?`), così assente e `null` danno `validation.required`. Il frontend non manda questi valori.
- **Configurazione eliminata** (`deleted_at` valorizzato: le configurazioni disattivate del vecchio sistema): vale come assente. Il
  filtro globale la esclude già, e l'indice unico parziale lascia creare la riga nuova.
- **Codici nuovi**: `notification.property_required`, `notification.test_email_failed` (422 con `args`:
  `DomainException.Unprocessable(code, args)`), `notification.test_email_blocked` (422), `notification.unavailable` (503:
  `new DomainException(code, 503)`). `resource.version_conflict` è il codice comune chiesto anche dalle altre pagine (in
  `CommonErrorCodes`), non `booking.version_conflict`.
- **Password**: mai nelle risposte, nei log, nel registro, nelle tabelle o nei messaggi d'errore. Vale anche per quella scritta nella
  pagina e non salvata.
- **Registro**: classe `AuditActionCodes.Notification` (`Module` `notification`, `Targets.EmailSettings` `email_settings`), con la sua
  voce in `All` scritta come `new(codice, AuditCategory.Operator, Notification.Module)`: l'helper `Operator(...)` mette il modulo
  `identity`. Serve il testo `Action_…` nei 7 `AuditExport*.resx`. La voce si scrive con `IAuditTrail` dopo il salvataggio, con `TargetId`
  uguale allo stabilimento (la tabella non ha un `public_id`). Una voce per salvataggio riuscito, nessuna dopo un errore. È un codice
  in più rispetto a `Gestione utenti - Frontend requests.md`, sezione 9; il frontend lo aggiungerà alle azioni del Log.

| Azione | `code` | Metadati |
|---|---|---|
| Sezione 3 | `notification.email_settings_updated` | `isActive`, `passwordChanged`; mai indirizzi, server, utente o password |

## 1. GET /api/notification/email-configuration

La pagina la chiama all'apertura, a Reset, al cambio di lingua, dopo un conflitto di versione e, per aggiornare l'esito
della prova, dopo un'email di test o un salvataggio. Nessun parametro.

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
all'apertura. Il campo `isActive` non conta: si prova anche una configurazione spenta.

### Regole

- **Invio**: dal server, porta, utente, password, sicurezza e controllo del certificato della richiesta, con `smtpPassword` `null`
  quella salvata; senza nessuna password, senza autenticazione. Mittente `"fromName" <fromEmail>`. Un solo messaggio a tutti i
  `testRecipients`, con un testo fisso che decide lo sviluppatore e nessun dato dello stabilimento o dei clienti. `none` non cifra,
  `starttls` e `ssl` falliscono se il canale non si cifra: mai un ripiego in chiaro.
- **`OutboundGuard`**: ogni destinatario passa da `Allows`. Se anche uno solo non è ammesso l'email non parte, l'esito non si registra
  e risponde 422 `notification.test_email_blocked` (oggi il mittente di piattaforma la sopprime in silenzio: qui l'utente crederebbe
  che sia arrivata).
- **Esito**: se la richiesta è uguale alla configurazione salvata (tutti i campi, testi senza spazi ai lati) e `smtpPassword` è `null`,
  si scrivono `last_verification_at` (l'istante del tentativo) e `last_verification_error` (`null` se riuscita, altrimenti il testo di
  `detail`). Con valori diversi da quelli salvati, o con una password scritta, non si registra niente: l'avviso della pagina parla
  della configurazione salvata.
- **Prova fallita** (server che non risponde entro il tempo, nome che non si risolve, certificato, autenticazione, mittente o
  destinatario rifiutati): 422 `notification.test_email_failed` con `args.detail`, la risposta del server SMTP o il motivo, che la
  pagina mostra così com'è all'utente: **al massimo 200 caratteri, su una riga, senza la password, senza lo scambio di
  autenticazione e senza dettagli interni** (stack, IP interni, percorsi).

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

- **Niente indirizzi interni** (OWASP A10, SSRF): il server SMTP lo scrive l'utente. Un `smtpHost` che è o si risolve in un indirizzo di
  loopback, privato, link-local (compreso `169.254.169.254`) o riservato dà 400 `validation.invalid` su `smtpHost`, senza aprire la
  connessione. Si controlla l'indirizzo a cui ci si collega davvero, non solo il nome. Lo stesso vale nella sezione 3 e, quando ci
  sarà, per gli invii veri.
- **Limite di frequenza**: oltre il limite 429 `auth.too_many_attempts` (`CommonErrorCodes.TooManyRequests`), che il frontend conosce
  già. Oggi ci sono solo i limiti `Auth` e `PublicForms`, per IP: qui serve per stabilimento. Quante prove lo decide lo sviluppatore.
- **Tempo massimo**: la chiamata aspetta l'esito; il tempo massimo della connessione lo decide lo sviluppatore, al di sotto di quello
  del proxy.
- **Risposta**: 204 senza corpo, solo se il server SMTP ha accettato il messaggio.

Errori, nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso |
| 400 | `validation.failed` | Le regole dei campi (sezione 3) e almeno un destinatario, in `errors[]` |
| 403 | `notification.property_required` | Token senza stabilimento |
| 400 | `validation.failed` | `validation.required` su `smtpPassword` (regola della password, sezione 3); `validation.invalid` su `smtpHost` per un indirizzo interno |
| 429 | `auth.too_many_attempts` | Troppe prove |
| 422 | `notification.test_email_blocked` | `OutboundGuard` non ammette un destinatario: niente è partito |
| 422 | `notification.test_email_failed` | La prova è fallita, con `args.detail` |
| 503 | `notification.unavailable` | L'archivio dei segreti non risponde |

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

Il frontend manda **sempre tutti i campi**, anche quelli non cambiati, con i testi senza spazi ai lati; il backend li toglie comunque.
Un testo vuoto o di soli spazi vale `null`.

| Campo | Tipo | Regola | Errore |
|---|---|---|---|
| `fromEmail` | string | Obbligatorio, al massimo 320 caratteri, un indirizzo email | `validation.required`, `validation.max_length`, `validation.email` |
| `fromName` | string \| null | Facoltativo, al massimo 160 caratteri | `validation.max_length` |
| `smtpHost` | string | Obbligatorio, al massimo 255 caratteri, non interno (sezione 2) | `validation.required`, `validation.max_length`, `validation.invalid` |
| `smtpPort` | number (intero) | Obbligatorio, da 1 a 65535 | `validation.required`, `validation.out_of_range` con `from` 1 e `to` 65535 |
| `smtpUsername` | string | Obbligatorio, al massimo 320 caratteri | `validation.required`, `validation.max_length` |
| `smtpPassword` | string \| null | `null` mantiene quella salvata; vedi sotto | `validation.required` |
| `security` | string | Obbligatorio, `none`, `starttls` o `ssl`; 465 vuole `ssl`, 587 vuole `starttls` | `validation.required`, `validation.invalid` |
| `verifyCertificate` | boolean | Obbligatorio | `validation.required` |
| `testRecipients` | string[] | Obbligatorio, anche vuoto; al massimo 10, ognuno un indirizzo email di al massimo 320 caratteri | `validation.required`, `validation.invalid` (sul campo, non sull'elemento) |
| `isActive` | boolean | Obbligatorio | `validation.required` |

- **Email**: `CommonRules.OptionalEmail` si ferma a 255 caratteri, la colonna ne ha 320: serve una regola propria, con lo stesso
  `EmailAddress()`. Il frontend controlla allo stesso modo (una sola `@`, né prima né ultima).
- **Coerenza di porta e sicurezza**: è quella del progetto della tabella (465 ⇒ `ssl`, 587 ⇒ `starttls`); sulle altre porte vale
  qualsiasi sicurezza. Una configurazione vecchia incoerente si legge com'è: la pagina chiede di correggerla.
- **Testi su una riga**: un carattere di controllo (a capo compreso) in un testo, nella password o in un destinatario dà
  `validation.invalid` su quel campo: finirebbe nelle intestazioni dell'email o nei comandi SMTP. Dalla pagina non si scrive.
- **`smtpPassword`**: la password si tiene così come arriva; vuota o di soli spazi vale `null`. `null` mantiene quella salvata.
  **La password salvata non va a un'altra destinazione**: se ce n'è una e la richiesta ha `smtpPassword` `null` con `smtpHost`,
  `smtpPort`, `smtpUsername`, `security` o `verifyCertificate` diversi da quelli salvati, 400 `validation.required` su `smtpPassword`.
  Altrimenti chi può cambiare la configurazione la farebbe arrivare a un server proprio o in chiaro. Senza una password salvata
  `null` è ammesso (server senza autenticazione).
- **Esito della prova**: se cambiano la password o uno di quei cinque campi, `last_verification_at` e `last_verification_error` tornano
  `null` (la prova vecchia non vale più). Altrimenti restano.
- **Password e riga insieme o per niente**: se la scrittura del segreto o della riga fallisce, restano la configurazione e la password
  di prima. Segreto che non si riesce a scrivere: 503 `notification.unavailable`.
- **Due salvataggi insieme** della stessa versione, anche il primo di uno stabilimento senza riga: uno 204 e uno 409, mai un 500 per
  l'indice unico e mai due righe.
- **Risposta**: 204 senza corpo, con la nuova versione nell'header `ETag: "<version>"`.
- **Registro**: `notification.email_settings_updated`.

Errori, nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso |
| 400 | `validation.failed` | Le regole dei campi, in `errors[]` con `field` uguale al nome nel corpo |
| 403 | `notification.property_required` | Token senza stabilimento |
| 409 | `resource.version_conflict` | Configurazione cambiata dopo la lettura, o `If-Match` assente: niente cambia |
| 400 | `validation.failed` | `validation.required` su `smtpPassword`; `validation.invalid` su `smtpHost` per un indirizzo interno |
| 503 | `notification.unavailable` | L'archivio dei segreti non risponde: niente cambia |

### Controllo di versione

Senza un controllo, se due utenti cambiano la configurazione email vince chi salva per ultimo: Salva rimanda tutta la
configurazione letta e cancella, senza che nessuno se ne accorga, quello che un altro utente ha salvato nel frattempo
(per esempio un altro server). Il backend non ha un controllo di concorrenza sugli aggiornamenti. Il frontend chiede lo
schema di **concorrenza ottimistica** che il backend ha già progettato per le prenotazioni offline
(`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un **codice di errore
unico per tutte le API**: `resource.version_conflict`.

- **`version` nella lettura**: stringa opaca, allo stesso livello degli altri campi. Cambia ogni volta che cambia uno dei dati coperti,
  e solo allora: due letture senza salvataggi in mezzo danno la stessa versione.
- **Cosa copre**: i dati che la lettura restituisce e il salvataggio riscrive (da `fromEmail` a `isActive`, `hasSmtpPassword` compreso).
  **Non la password**: il frontend non la legge mai e la manda solo quando l'utente ne scrive una nuova, quindi un cambio della sola
  password non cambia la versione. **Non l'esito della prova** (`lastVerificationAt`, `lastVerificationError`): chi prova e subito
  dopo salva non deve ricevere un 409. Ogni stabilimento ha la sua.
- **`If-Match`**: senza, o con una versione che non è più quella attuale, 409 `resource.version_conflict` senza `errors[]`.
- **Ordine**: prima le regole dei campi (400), poi la versione (409), poi la regola della password (400). Con una versione vecchia
  l'utente non ha mai visto la configurazione salvata e deve ricevere il 409, non un errore sulla password.
- **Configurazione mai salvata**: anche senza riga la lettura dà una `version`, e il primo salvataggio con quella riesce. Se un altro
  utente salva per primo, chi salva dopo con la stessa versione riceve il 409.
- **Come farlo lo sceglie lo sviluppatore** (il token `xmin` di Npgsql non va bene da solo, perché cambia anche con l'esito della
  prova; un hash dei dati coperti, un contatore...): l'agente glielo chiede prima di scrivere codice. Il frontend tratta la versione
  come una stringa opaca e non la confronta mai.

## Cosa fa il frontend

- **Lettura fallita**: Sì, Salva e Invia email test restano spenti. Salva rimanda sempre tutto il modello letto, con le modifiche.
- **Avviso** sopra i campi, sulla configurazione salvata: nessuna configurazione, configurazione spenta, mai provata, ultima prova
  fallita (con `lastVerificationError`) o riuscita.
- **Validazioni**: le stesse della sezione 3 (obbligatori, lunghezze, email, porta, porta e sicurezza, al massimo 10 destinatari,
  password da riscrivere), prima di chiamare. Gli errori su un campo vanno sotto quel campo, gli altri sopra i bottoni.
- **409**: avviso "Nel frattempo un altro utente ha modificato questi dati: li abbiamo ricaricati. Rifai le tue modifiche." e rilettura,
  come Reset. Dopo ogni salvataggio riuscito tiene la versione dell'`ETag`.

## Verifica

Con un token di un utente dello stabilimento che può cambiare le impostazioni. Configurazioni, password ed esiti si preparano
nel database di prova; il server SMTP è sempre un finto.

1. Ogni API con un token senza il permesso: 403 `auth.forbidden`; senza token: 401; con un token senza stabilimento: 403
   `notification.property_required`. `ArchitectureTests` passano.
2. `GET` senza configurazione, e con una configurazione eliminata: 200 con `fromEmail`, `fromName`, `smtpHost` e `smtpUsername` `null`,
   `smtpPort` 465, `security` `ssl`, `verifyCertificate` e `isActive` `true`, `testRecipients` `[]`, `hasSmtpPassword` `false`, esito
   `null` e una `version`; nessuna riga creata. Con una configurazione: i suoi valori, mai la password né il riferimento al segreto.
3. `save-email-configuration` con quella `version`: 204 con `ETag`, una riga; la `GET` risponde con i valori salvati, i testi senza
   spazi ai lati, `hasSmtpPassword` `true` se c'era una password e la versione dell'`ETag`. Un secondo `POST` con la versione di
   prima: 409 `resource.version_conflict` senza `errors[]`; niente cambia, nemmeno la password. Due `POST` insieme con la stessa
   versione: uno 204 e uno 409, una sola riga.
4. Senza `If-Match`: 409. Due `GET` senza salvataggi in mezzo: la stessa `version`. Il salvataggio di un altro stabilimento non la cambia.
5. 400 con il suo `errors[]`, e niente cambia: ogni campo obbligatorio assente, `null` o vuoto; `fromEmail` di 321 caratteri e senza
   chiocciola; `fromName` di 161; `smtpHost` di 256; `smtpUsername` di 321; porta 0 e 65536; `security` sconosciuto, `starttls` con 465,
   `ssl` e `none` con 587; 11 destinatari, un destinatario non valido e uno di 321 caratteri; un a capo in `fromName`. `fromName` `null` e
   `testRecipients` `[]`: 204. `none` con la porta 25: 204.
6. Con una password salvata: un `POST` con `smtpPassword` `null` e gli stessi parametri di connessione: 204 e la password resta quella;
   con `smtpHost`, `smtpPort`, `smtpUsername`, `security` o `verifyCertificate` diversi: 400 `validation.required` su `smtpPassword`; con
   la password nuova: 204, e quella nuova si rilegge subito, anche da un'altra istanza. Un cambio della sola password: `ETag` uguale
   alla versione di prima. Senza password salvata `null` è ammesso.
7. L'utente A legge; l'utente B salva con un altro `smtpHost`; A salva con la sua versione e `smtpPassword` `null`: 409, non 400. Con la
   versione vecchia e `smtpPort` 0: 400 su `smtpPort`.
8. `send-test-email` con i valori salvati e `smtpPassword` `null`: 204, l'email arriva a ogni destinatario con il mittente
   `"fromName" <fromEmail>`, `lastVerificationAt` è valorizzato, `lastVerificationError` `null` e la `version` è quella di prima; poi
   `save-email-configuration` con la versione letta prima: 204.
9. `send-test-email` con il server che rifiuta le credenziali: 422 `notification.test_email_failed` con `args.detail` di al massimo 200
   caratteri, senza password, e lo stesso testo in `lastVerificationError`. Con valori non salvati o con una password scritta, riuscita
   o fallita: l'esito nel database non cambia.
10. `send-test-email` con `testRecipients` `[]`: 400 `validation.required` su `testRecipients`. Con `Notification:Outbound:Mode` `Off`, e con
    `Allowlist` e un destinatario fuori elenco: 422 `notification.test_email_blocked`, nessuna email e nessun esito. Oltre il limite di
    frequenza: 429.
11. `smtpHost` `127.0.0.1`, `localhost`, `10.0.0.1`, `169.254.169.254` o un nome che si risolve in uno di questi: 400 `validation.invalid` su
    `smtpHost`, in tutte e due le `POST`, senza connessioni aperte.
12. Con `starttls` un server che non lo offre, e con `verifyCertificate` `true` un certificato non valido: 422; con
    `verifyCertificate` `false` lo stesso certificato passa. Mai un invio in chiaro se la sicurezza non è `none`.
13. Un salvataggio che cambia `smtpHost` (con la password) azzera l'esito della prova; uno che cambia solo `fromName` no.
14. Archivio dei segreti che non risponde: 503 `notification.unavailable`, e configurazione e password restano quelle di prima.
15. Nei log e nel registro non compaiono password; il registro ha una voce `notification.email_settings_updated` per ogni salvataggio
    riuscito e nessuna dopo un errore.
