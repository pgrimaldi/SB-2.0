# Assistenza e guide - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Assistenza e guide** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi, e quali scelte vanno chieste allo sviluppatore prima di scrivere codice.

La pagina è `/settings/contact-support`: i contatti dell'assistenza di **Summer Booking** (telefono, email e orari, uguali per
tutti gli stabilimenti) e il form "Contattaci", con cui l'utente del gestionale scrive all'assistenza.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET | `/api/notification/support/info` | Telefono, email e orari dell'assistenza |
| 2 | POST | `/api/notification/support/contact` | Manda all'assistenza il messaggio del form |

Sono tutte e due API nuove: nel backend oggi non ci sono endpoint, tabelle, impostazioni o tipi di email per l'assistenza.

## Prima di tutto

- **Il modulo `Notification` non ha ancora endpoint**: il progetto, i validatori, gli enum nelle opzioni EF, il gruppo
  `/api/notification` e i codici `notification.property_required` e `notification.unavailable` sono già chiesti in
  `Configurazione email - Frontend requests.md` ("Prima di tutto" e "Regole comuni"). Quello che c'è già si riusa.
- **Altre richieste simili, da non usare qui**: `EstablishmentClaimRequest` (`notification`) è la rivendicazione di uno stabilimento dal
  sito pubblico, `POST /api/catalog/property-registrations` è il form pubblico "Registra la tua struttura".

### Scelte che l'agente chiede allo sviluppatore

1. **Dove stanno** telefono, email e orari (configurazione, risorse `.resx`, tabella) e **i loro valori**. Gli orari sono testi scritti
   da Summer Booking, nelle sette lingue di `SupportedLanguages`.
2. **A quale indirizzo** arriva il messaggio: lo stesso `info@` a cui andrà l'avviso di `PropertyRegistered` (`CatalogEvents.cs`, oggi senza
   gestore) o un altro. Non si prende mai dalla richiesta.
3. **Con cosa si manda**: `ITransactionalEmailSender` non basta com'è. `TransactionalEmailKind` non ha un tipo per l'assistenza,
   `TransactionalEmail` non ha l'indirizzo a cui rispondere (`Reply-To`), e `SmtpTransactionalEmailSender` non segnala un invio non
   fatto perché manca l'SMTP (`Host` vuoto: scrive solo nel log, con il destinatario). Si decide insieme alla scelta 2 di
   `Configurazione email - Frontend requests.md`: una sola libreria SMTP.
4. **Se il messaggio si salva** (una tabella con la sua policy in `privacy_retention_policies`, di durata decisa dallo sviluppatore) e se
   l'email parte subito o da una coda.
5. **Il nome dello stabilimento** nell'email: `Notification` oggi non ha un contratto per leggerlo (in `Shared/Contracts/Catalog` si
   legge solo da `OnlineBookingRules`, che è di Booking). Serve un modo, oppure basta l'id dello stabilimento.

## Regole comuni

- **Rotte**: gruppo `/api/notification`, con il `GroupName` `notification` che `Boot/Program.cs` ha già fra gli `ApiDocs`.
- **Permesso**: nessuno in particolare. Basta un utente collegato (`RequireAuthorization()` senza policy): l'assistenza serve a chiunque usi
  il gestionale. Il `catalog.settings` di Configurazione email sta sulle sue tre rotte, non sul gruppo (come in
  `Module.Payment/Endpoints/Endpoints.cs`). Senza token valido: 401 `auth.unauthenticated`.
- **Sessione impersonata**: permessa. Il `sub` del token resta quello del titolare (`JwtTokenIssuer.cs`): l'email riporta anche
  `ICurrentUser.ImpersonatorUserId`.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed `errors[]` quando ci
  sono.
- **Log** (`15-logs.md`): mai il testo del messaggio, il nome, l'email o il cellulare; al massimo l'id dell'utente e dello stabilimento. Un
  invio non riuscito si scrive nel log (`GlobalExceptionHandler` non scrive le `DomainException`), con il tipo dell'errore ma senza
  l'eccezione o il suo messaggio, che può contenere l'email del form.
- **Registro**: nessuna voce.

## 1. GET `/api/notification/support/info`

Nessun corpo né parametri. Il frontend la chiama all'apertura della pagina e di nuovo quando l'utente cambia lingua, se l'utente ha uno
stabilimento (senza, non chiama nessuna delle due API).

Risposta: 200.

```json
{
  "phoneNumber": "050 7916620",
  "mailAddress": "info@summerbooking.it",
  "supportHour": [
    "1 maggio - 30 settembre, dal lunedì alla domenica: dalle 8:30 alle 20:00",
    "1 ottobre - 30 aprile, dal lunedì al venerdì: dalle 9:00 alle 18:00"
  ]
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `phoneNumber` | string | Il telefono come va mostrato. Il frontend ne fa un link `tel:` tenendo solo le cifre e il `+`. |
| `mailAddress` | string | L'email dell'assistenza, un solo indirizzo: il frontend ne fa un link `mailto:`. |
| `supportHour` | string[] | Gli orari, una riga per periodo, nella lingua di `Accept-Language` (`IRequestLanguage`). `[]` se non ce ne sono. |

I valori dell'esempio sono quelli che la pagina mostra oggi con i dati di prova: quelli veri li dà lo sviluppatore (scelta 1). I campi
non sono mai `null`. Con una lingua che il backend non gestisce `IRequestLanguage` dà l'italiano, come nelle altre API.

Lo stabilimento non serve: un token che non lo ha riceve 200.

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido |

## 2. POST `/api/notification/support/contact`

Il frontend la chiama con "Invia messaggio".

```json
{
  "firstName": "Mario",
  "lastName": "Rossi",
  "email": "mario.rossi@example.com",
  "mobilePhone": "333 123 4567",
  "message": "Buongiorno, vorrei sapere come esportare il listino."
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `firstName` | string | Obbligatorio, al massimo 100 caratteri (come `FirstName` in `AccountValidators.cs`) |
| `lastName` | string | Obbligatorio, al massimo 100 caratteri |
| `email` | string | Obbligatoria, `CommonRules.OptionalEmail` (al massimo 255 caratteri) |
| `mobilePhone` | string \| null | Facoltativo, `CommonRules.PhoneNumber` (al massimo 32 caratteri, solo cifre e spazi) |
| `message` | string | Obbligatorio, al massimo 2000 caratteri (come `Notes` in `Module.Identity/Application/Validators/PlatformValidators.cs`); può avere più righe |

Risposta: **204** senza corpo.

- **Testi**: il backend toglie gli spazi ai lati e controlla i testi così. Un testo vuoto o di soli spazi è mancante; per `mobilePhone`
  vale `null`. Il frontend li manda già così.
- **Errori dei campi**: `validation.required`, `validation.max_length` (con `args.max`), `validation.email`, `validation.phone`, con `field`
  uguale al nome nel corpo. Un errore per campo, tutti insieme in `errors[]`. Nei DTO i campi sono `string?`, così assente e `null` danno
  `validation.required`.
- **Una riga sola**: un carattere di controllo (a capo, tabulazione, separatori di riga Unicode) in `firstName`, `lastName` o `email` dà
  `validation.invalid` su quel campo.
- **Email che la libreria non accetta** come un solo indirizzo a cui rispondere: `validation.email` su `email`, mai un 500 o un 503. Passano
  `EmailAddress()` ma non vanno bene `foo,bar@x`, `<a@b>`, `a b@c`, una parte locale non ASCII (`mariò@example.com`, che `SmtpClient` rifiuta
  solo all'invio) e un dominio non valido.
- **Ordine su `email`**: obbligatoria, lunghezza, `EmailAddress()`, carattere di controllo, libreria; vale il primo errore. Sono regole dei
  campi, insieme agli altri errori.
- **L'email all'assistenza**: testo semplice, mai HTML. Contiene nome, cognome, email, cellulare, messaggio, **stabilimento** (dal token,
  `ICurrentUser.PropertyId`) e id dell'utente. Chi risponde deve scrivere all'`email` del form (`Reply-To`); il mittente resta quello di
  piattaforma, mai l'`email` del form. Nelle intestazioni (oggetto, `Reply-To`) niente testo del messaggio.
- **204 solo se il messaggio è preso in carico**: consegnato al server di posta, oppure salvato in una coda che lo manda. Il frontend
  mostra "Messaggio inviato con successo" e svuota il form, quindi un messaggio perso in silenzio non si può recuperare. Se non si può
  (server che non risponde o che rifiuta, SMTP non configurato): 503 `notification.unavailable`, e l'utente può riprovare.
- **`OutboundGuard`**: si controlla per primo, come in `SmtpTransactionalEmailSender`. Un ambiente che blocca l'invio risponde 204, con
  l'avviso nel log, anche con `Host` vuoto (è il caso di Development). È l'unica eccezione alla regola precedente.
- **Tempo massimo** totale dell'invio: lo decide lo sviluppatore, sotto quello del proxy (oggi vale quello predefinito di `SmtpClient`). Un
  solo tentativo dentro la richiesta.
- **Troppi messaggi**: 5 chiamate ogni 10 minuti **per utente** (`sub` del token), anche quelle non valide, poi 429
  `auth.too_many_attempts`. Non si usa `RateLimitPolicies.PublicForms`, che conta per indirizzo IP: gli utenti di uno stabilimento hanno
  spesso lo stesso. Serve una policy propria in `SecuritySetup.cs` (`UseRateLimiter` viene dopo `UseAuthentication`,
  `Boot/Program.cs:148-150`: l'utente c'è già), con il numero in configurazione come `Security:PublicForms:PermitPer10Minutes`. Un limite
  in memoria, che conta per ogni istanza (PROMEMORIA-85-9), va bene.

Errori, nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido |
| 429 | `auth.too_many_attempts` | Troppi messaggi dello stesso utente |
| 400 | `validation.failed` | Le regole dei campi, in `errors[]` |
| 403 | `notification.property_required` | Token senza stabilimento |
| 503 | `notification.unavailable` | Il messaggio non si può mandare né mettere in coda |

Un corpo assente, non JSON o con un tipo sbagliato non arriva ai validatori: 400 come nelle altre API. Il frontend non lo manda.

## Cosa fa il frontend

- **Contatti**: finché non arrivano i riquadri hanno solo i titoli; se la lettura fallisce compare il messaggio del `code` sotto i
  riquadri. Il form funziona lo stesso.
- **Validazioni** prima di chiamare: campi obbligatori, lunghezze, formato dell'email e cellulare di sole cifre e spazi; non i caratteri di
  controllo né le email che solo la libreria rifiuta. "Invia messaggio" è spento finché il form non è valido.
- **Errori**: quelli di un campo sotto quel campo, gli altri sopra il bottone; i dati scritti restano.
- **204**: il form si svuota e compare il popup "Messaggio inviato con successo".

## Verifica

Il server SMTP è sempre un finto e `Notification:Outbound:Mode` è `Allowlist` con i destinatari usati. Fuori dalla prova 8 il limite dei
messaggi si alza in configurazione.

1. Le due API senza token: 401. Con un utente dello stabilimento senza permessi di impostazione (per esempio il bagnino): 200 e 204.
   Token senza stabilimento: 200 nella `GET`, 403 `notification.property_required` nella `POST`. Sessione impersonata: 200 e 204, e l'email
   riporta anche chi impersona. Corpo non JSON: 400. `ArchitectureTests` passano.
2. `GET` con `Accept-Language` `it`, `en`, `fr`, `es`, `de`, `zh` e `ar`: 200 con gli stessi contatti e gli orari in quella lingua; con `pt`:
   in italiano. Nessun campo `null`.
3. `POST` con il corpo dell'esempio: 204. All'assistenza arriva un'email di testo con tutti i dati, lo stabilimento e il `Reply-To`
   `mario.rossi@example.com`; il mittente è quello di piattaforma.
4. `mobilePhone` assente, `null`, `""` e di soli spazi: 204.
5. 400 con il suo `errors[]`, e nessuna email: ogni campo obbligatorio assente, `null`, vuoto e di soli spazi (`validation.required`);
   `firstName` e `lastName` di 101 caratteri, `email` di 256, `mobilePhone` di 33, `message` di 2001 (`validation.max_length`); `email` senza
   chiocciola (`validation.email`); `+39 333 123 4567` come `mobilePhone` (`validation.phone`); un a capo in mezzo a `firstName`, `lastName` ed
   `email` (`validation.invalid`); `foo,bar@x`, `<a@b>` e `mariò@example.com` come `email` (`validation.email`). Con due campi sbagliati: due voci.
6. `message` di 2000 caratteri, con più righe e con `<b>ciao</b>`: 204; nell'email il testo è uguale, non interpretato.
7. Il server SMTP che non risponde o rifiuta, e `Host` vuoto: 503 `notification.unavailable`, entro il tempo massimo, con una riga nel log;
   oppure 204 se c'è una coda e l'email parte quando il server torna.
8. Sei `POST` dello stesso utente in 10 minuti, anche con corpi non validi: la sesta è 429 `auth.too_many_attempts`, senza email. Un altro utente dello stesso
   stabilimento e dello stesso indirizzo IP: 204.
9. Con `Notification:Outbound:Mode` `Off`, con e senza `Host`: 204, nessuna email.
10. Nei log di tutte le prove non ci sono messaggio, nome, email e cellulare.
