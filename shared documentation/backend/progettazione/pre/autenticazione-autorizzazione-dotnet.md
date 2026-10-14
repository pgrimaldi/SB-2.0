# Autenticazione e autorizzazione (.NET 10)

> Linee guida per la sicurezza degli endpoint nella riscrittura in **.NET 10 (Web API)**.
> Complemento a [linee-guida-implementative-dotnet.md](linee-guida-implementative-dotnet.md) e
> [architettura-evolutiva-microservizi.md](architettura-evolutiva-microservizi.md).

---

## 1. Principi

- **Secure-by-default**: tutto richiede autenticazione salvo dove esplicitamente aperto. Se dimentichi
  un attributo, l'endpoint resta **protetto**, non esposto per sbaglio.
- **Ruolo (e scoping) nel token**: l'autorizzazione è **decentralizzata** — ogni modulo decide chi può
  fare cosa leggendo il **token già validato**, **senza chiamare Identity a ogni richiesta**. Cruciale
  nel monolite modulare e ancor più dopo lo split in microservizi.
- **Identity minimale sul lato accesso**: il modulo Identity possiede l'autenticazione + autorizzazione
  **degli utenti umani** (account, credenziali, login esterni, ruoli/RBAC) ed **emette** il token anche
  per i client M2M, ma senza possederne il registro (in **Infra**, vedi §9bis). Ospita anche la
  **rubrica clienti** (sottoarea `customer`, tabella `identity_customer_customers`) e la sottoarea
  `account` (abbonamenti, licenze, contratti) — decisione del 2026-10-07, vedi §9.

---

## 2. Ruolo e scoping nel token (claims)

Ruoli e info di autorizzazione vanno nel **JWT come claim**; ASP.NET Core li usa per
`[Authorize(Roles=...)]` / policy.

```jsonc
// payload del JWT utente
{ "sub": "0192f0c4-…", "role": "manager", "property_id": "5b1f3c7e-…", "exp": 1719000000 }
```

Cosa mettere: `sub` (il `public_id` dell'account, mai l'`id` bigint), `role` (**un solo** ruolo per
account, il `code` minuscolo di `identity_access_roles`: `manager`, `superadmin`, …), **scoping
stabilimento** `property_id` (il `public_id` della `property`; assente per `consumer` e `systemadmin`).
**Non** mettere l'anagrafica: il token va tenuto snello.

> **Nome del claim (2026-10-07).** Le versioni precedenti dei documenti chiamavano il claim di scoping
> `establishmentId`; il termine canonico del dizionario è `property` → il claim è **`property_id`**,
> e nei DTO il campo è `propertyId`.

Caveat: i claim sono uno **snapshot al login**. Se revochi un ruolo, il token vecchio resta valido fino
alla scadenza → **access token a vita breve** (15-60 min) + **refresh token**; per operazioni molto
sensibili, un check server-side puntuale.

> **Decisione del 2026-10-12**: l'access token utente dura **7 giorni** (`Auth:AccessTokenMinutes` = 10080), in
> tutti gli ambienti, per scelta del committente. Conseguenze accettate: un token rubato resta valido fino a 7
> giorni; cambio di ruolo, cancellazione di un utente dello staff, reset o cambio password e logout chiudono le
> sessioni (refresh token) ma **non** il token già emesso, che porta ruolo e permessi vecchi fino alla scadenza.
> Per tornare a una durata breve basta cambiare la configurazione.

---

## 3. I tre livelli di accesso

| Livello | Chi | Meccanismo |
|---|---|---|
| **Token utente** (maggior parte) | Manager, operatori, clienti finali loggati | JWT schema `UserBearer` |
| **Nessun token** (pochi) | Login, registrazione, ricerca/prenotazione pubblica, health check | `[AllowAnonymous]` |
| **Token da key/secret** | Sistemi esterni (M2M) che consumano le API | JWT schema `ApiBearer`, ottenuto scambiando key/secret |
| *(caso a parte)* **Webhook** | Stripe, Fiskaly | **Verifica della firma** del provider, non JWT |

L'idea: **due schemi JWT distinti** con **issuer/audience diversi**, così il token utente non vale sugli
endpoint esterni e viceversa.

---

## 4. Configurazione degli schemi (host)

```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer("UserBearer", o =>   // utenti interattivi
    {
        o.TokenValidationParameters = new() {
            ValidIssuer   = "summerbooking-identity",
            ValidAudience = "summerbooking-app",
            IssuerSigningKey = userSigningKey /* ... */ };
    })
    .AddJwtBearer("ApiBearer", o =>    // client esterni (M2M)
    {
        o.TokenValidationParameters = new() {
            ValidIssuer   = "summerbooking-identity",
            ValidAudience = "summerbooking-external-api",   // audience DIVERSO
            IssuerSigningKey = apiSigningKey /* può essere una chiave diversa */ };
    });
```

## 4bis. Algoritmo di firma (decisione del 2026-10-07)

- **Si parte con HS256** (chiave simmetrica): oggi c'è un solo processo e la chiave sta in un solo posto.
- Ogni token porta l'header **`kid`** e la configurazione contiene un **elenco** di chiavi per schema
  (`Jwt:UserKeys`, `Jwt:ApiKeys`): la prima firma, tutte verificano. Così una rotazione, o il cambio di
  algoritmo, è una modifica di configurazione con una finestra pari alla vita dei token.
- **Prima di estrarre il primo microservizio si passa a RS256** (asimmetrica): Identity firma con la
  chiave privata, gli altri servizi verificano con la pubblica pubblicata su `/.well-known/jwks.json`.
  Con HS256 il servizio estratto riceverebbe il segreto e potrebbe falsificare token. Il passaggio non
  fa cadere le sessioni: i refresh token sono opachi a database, non JWT. Promemoria in
  [85-promemoria.md](../85-promemoria.md) (PROMEMORIA-85-6).
- RS256 e non ES256: la verifica avviene a ogni richiesta e la firma solo a login e refresh; RS256
  verifica più in fretta, al prezzo di un token più lungo.

---

### Token legati all'ambiente (decisione del 2026-10-12)

Un token emesso in un ambiente non vale in un altro (nel legacy un login sul server di sviluppo dava un token
accettato in produzione, perché la chiave era nel codice e uguale ovunque):

- **chiavi diverse per ambiente**, dal secret manager; quelle di sviluppo stanno in `appsettings.Development.json`;
- **issuer diverso per ambiente**: `summerbooking-identity-dev` in sviluppo, `summerbooking-identity-prod` in
  `appsettings.json`; ogni altro ambiente (staging, test) imposta il proprio `Jwt:Issuer`;
- **controllo all'avvio** (`Boot/Web/JwtConfigurationGuard.cs`): fuori dallo sviluppo il backend non parte se
  l'issuer o una chiave coincidono con quelli di sviluppo, se un `kid` inizia con `dev-` o se una chiave è più
  corta di 256 bit. Nessun costo a runtime: l'issuer è già verificato su ogni token, il controllo gira una volta.
- convenzione per il `kid`: nome dell'ambiente + scopo + numero (`prod-user-1`, `prod-api-1`), utile nei log.

## 5. Policy e secure-by-default

```csharp
builder.Services.AddAuthorization(options =>
{
    // default (endpoint senza attributi) = token UTENTE
    var userPolicy = new AuthorizationPolicyBuilder("UserBearer")
        .RequireAuthenticatedUser().Build();
    options.DefaultPolicy  = userPolicy;
    options.FallbackPolicy = userPolicy;   // tutto protetto se non specificato

    // endpoint per l'esterno = solo token da key/secret
    options.AddPolicy("ExternalApi", p =>
        p.AddAuthenticationSchemes("ApiBearer")
         .RequireAuthenticatedUser()
         .RequireClaim("client_id"));       // opzionale: scope/claim del client
});
```

`[AllowAnonymous]` e gli `[Authorize(...)]` espliciti **sovrascrivono** la fallback policy.

---

## 5bis. Durate dei token — da configurazione, mai hardcoded

Tutte le durate legate all'autenticazione sono **valori di configurazione** (`appsettings.{env}.json` +
override da **variabili d'ambiente**, secret manager per le chiavi), **non** costanti nel codice. Il
legacy ha il difetto opposto: JWT a **365 giorni** cablati in tre punti
([`docs/01-criticita-e-indicazioni-backend.md` §7](../01-criticita-e-indicazioni-backend.md)).

| Parametro | Chiave di config (esempio) | Env var (esempio) | Valore di partenza |
|---|---|---|---|
| TTL access token utente (`UserBearer`) | `Auth:AccessTokenMinutes` | `AUTH__ACCESSTOKENMINUTES` | 7 gg (10080 min, decisione 2026-10-12) |
| TTL refresh token | `Auth:RefreshTokenDays` | `AUTH__REFRESHTOKENDAYS` | 30 gg |
| TTL refresh token "ricordami" | `Auth:RefreshTokenRememberMeDays` | `AUTH__REFRESHTOKENREMEMBERMEDAYS` | 90 gg |
| TTL token M2M (`ApiBearer`) | `Auth:ExternalTokenMinutes` | `AUTH__EXTERNALTOKENMINUTES` | 60 min (niente refresh) |
| TTL sfide OTP / verifica email | `Auth:OtpChallengeMinutes` | `AUTH__OTPCHALLENGEMINUTES` | 5–10 min |
| TTL token reset password | `Auth:PasswordResetMinutes` | `AUTH__PASSWORDRESETMINUTES` | 30–60 min |
| `ClockSkew` di validazione | `Auth:ClockSkewSeconds` | `AUTH__CLOCKSKEWSECONDS` | 30–60 s |

- In .NET si legano con le **Options** (`builder.Services.Configure<AuthOptions>(config.GetSection("Auth"))`)
  e si iniettano dove si emette/valida il token; `TokenValidationParameters.LifetimeValidator` o il
  semplice `expires:` alla creazione usano quei valori.
- Le **chiavi di firma** (`userSigningKey`, `apiSigningKey`) e l'eventuale **pepper** delle password
  vengono dal **secret manager** ([`docs/17-secrets.md`](../17-secrets.md)), non da `appsettings`.
- Cambiare una durata è una **modifica di configurazione/deploy**, non di codice; va documentata e
  rivista periodicamente insieme ai parametri di hashing password (vedi
  [`analisi-post/db/identity_access_users.md`](../../analisi-post/db/identity/identity_access_users.md) →
  `password_hash`).

---

## 5ter. Sessione utente: endpoint e cookie del refresh token

Decisione del 2026-10-07, che recepisce la richiesta del frontend (`analisi-post/docs/AUTH_API_CONTRACTS 1.docx`)
riallineandola a nomenclatura e standard del backend.

| Endpoint | Accesso | Esito |
|---|---|---|
| `POST /api/identity/auth/login` | anonimo | 200 `{ accessToken, expiresIn, user }` + cookie; oppure 200 `{ twoFactorRequired: true, mfaToken, expiresIn }` senza token se lo stabilimento ha il 2FA attivo (codice inviato per email) |
| `POST /api/identity/auth/login/verify-otp` | anonimo | `{ mfaToken, code }` → 200 come il login riuscito. Il `mfaToken` è un JWT firmato a vita breve, con audience proprio: non vale come accesso |
| `POST /api/identity/auth/refresh` | solo cookie | 200 come il login riuscito (nuovo cookie, rotazione) |
| `POST /api/identity/auth/logout` | solo cookie | **sempre 204** (idempotente), cookie cancellato e sessione revocata se presente |

- **Access token**: nel corpo della risposta; il frontend lo tiene **solo in memoria** (mai
  `localStorage`), lo manda come `Authorization: Bearer`.
- **Refresh token**: solo nel cookie `HttpOnly; Secure; SameSite=Strict; Path=/api/identity`,
  nome con prefisso `__Secure-` (il prefisso `__Host-` richiederebbe `Path=/`). Opaco, ruotato a ogni
  uso, riuso di un token consumato = revoca della famiglia ([identity_access_refresh_tokens](../../analisi-post/db/identity/identity_access_refresh_tokens.md)).
- **"Ricordami"** (`remember`): `true` → cookie persistente (`Max-Age` = `Auth:RefreshTokenRememberMeDays`);
  `false` → cookie di sessione, senza `Max-Age`. La scelta è salvata sul token (`is_persistent`) e
  ereditata a ogni rotazione.
- **`user`** nella risposta: `{ id, email, role, propertyId }` — `id` è il `public_id`, `propertyId`
  può essere `null`. Il profilo completo si legge da `GET /api/identity/me`.
- **Applicazione di provenienza**: il login riceve `app` (`hub` | `customer` | `admin`); il server
  rifiuta i ruoli non ammessi per quell'applicazione con 403 `auth.app_not_allowed` (consumer sull'hub,
  non-systemadmin sul back-office). Nel legacy il controllo era solo nel frontend.
- **Login social** (Google, Apple, Facebook): il token del provider è **sempre verificato dal backend**
  presso il provider; mai fidarsi di email o id mandati dal client. Verifica email del consumer: non
  prevista (BL e mockup non la richiedono).
- **Password** (decisione del 2026-10-07): minimo 8 caratteri, massimo 128, nessun obbligo di
  composizione, rifiuto delle password più comuni (indicazioni NIST SP 800-63B): circa 46.000 voci da
  elenchi pubblici (SecLists, licenza MIT) più parole del contesto, aggiornate da un job mensile. Vale solo quando se ne
  imposta una nuova: le password legacy più corte continuano a funzionare.
- **Account del titolare**: lo crea il systemadmin **scegliendo la password**, come nel legacy; il
  titolare può poi cambiarla dal proprio profilo. Nessun link di invito.
- **Audit**: login riusciti e falliti, blocchi, reset password, 2FA, gestione di utenti e ruoli e
  impersonazione sono pubblicati come eventi `AuditEntryRecorded` (categorie `security` e `platform`) e
  conservati dal modulo Audit ([86-modulo-audit.md](../86-modulo-audit.md)). Il conteggio dei tentativi
  per il blocco resta in Identity, perché serve in modo sincrono.
- **CSRF**: oltre a `SameSite=Strict`, refresh e logout verificano l'header `Origin` contro le origini
  ammesse da configurazione.
- **Errori** (Problem Details, [standard-risposta-api-dotnet.md](standard-risposta-api-dotnet.md)):
  `auth.invalid_credentials` (401, anche per account disattivato: niente enumerazione),
  `auth.session_expired` (401), `auth.otp_invalid` (401), `auth.too_many_attempts` (429),
  `validation.*` (400), `server.unexpected` (500).
- **Hosting del frontend**: file statici su S3 nello stesso account AWS del backend. Il cookie
  funziona così com'è se frontend e API rispondono sullo stesso dominio (CloudFront con un behavior
  `/api/*` verso il backend); con sottodomini diversi servono CORS con credenziali e origini esplicite.

---

## 6. Marcatura degli endpoint

```csharp
// maggior parte: token utente (default policy)
[Authorize] [HttpGet("bookings")]                     public ... List(...)

// pubblici: nessun token
[AllowAnonymous] [HttpPost("login")]                  public ... Login(...)
[AllowAnonymous] [HttpGet("public/availability")]     public ... Search(...)

// ruolo specifico
[Authorize(Roles = "Manager")] [HttpGet("admin/report")] public ... Report(...)

// consumati da esterni: token da key/secret
[Authorize(Policy = "ExternalApi")] [HttpGet("external/reservations")] public ... ForPartner(...)
// equivalente puntuale: [Authorize(AuthenticationSchemes = "ApiBearer")]
```

Regola: senza attributo → token utente; `[AllowAnonymous]` → aperto; `Policy = "ExternalApi"` → token API.

---

## 7. Token per client esterni (key/secret → JWT)

Il client esterno **scambia key+secret per un JWT** (pattern OAuth2 *client_credentials*), poi chiama con
quel token. Endpoint dedicato, `[AllowAnonymous]` (il client si autentica con key/secret nel body):

```csharp
[AllowAnonymous] [HttpPost("external/token")]
public async Task<IActionResult> IssueToken([FromBody] ClientCredentials cred)
{
    // valida key/secret tramite il contratto verso il registro dei client M2M
    // (IPartnerClientAuthenticator, implementato contro Module.Infra — vedi nota sotto)
    var client = await _partnerClients.ValidateAsync(cred.Key, cred.Secret);
    if (client is null) return Unauthorized();

    // JWT per lo schema "ApiBearer": issuer/audience "external", claim client_id + scope
    var token = _tokenFactory.CreateExternalToken(client.Id, client.Scopes);
    return Ok(new { access_token = token, expires_in = 3600 });
}
```

Punti chiave:
- Il **secret non vive mai nel database**, nemmeno hashato (a differenza delle password utente): la
  verifica HMAC / lo scambio OAuth *client_credentials* richiedono il valore in chiaro, quindi si salva
  solo un **riferimento** (`client_secret_ref`) al secret manager, risolto a runtime con una cache breve
  ([17-secrets.md](../17-secrets.md)).
- **Il registro dei client** (`client_id`, `client_secret_ref`, scoping per stabilimento, stato
  attivo/scaduto) vive nel modulo **Infra** (`ApiClient`/`IntegrationApp`, ex `keysecret`) come
  **registro di integrazioni partner** (POS/cassa esterni, export dati, futuri client MCP — vedi
  [28-mcp.md §5](../28-mcp.md)), non in Identity: coerente con dove vivono già gli altri segreti di
  integrazione (`fiskaly_accounts`, `switchboard_configs`, `notification_email_channel_settings`, ognuno
  nel proprio modulo di dominio, mai centralizzati in Identity). **Identity resta comunque il modulo che
  emette il JWT** — l'endpoint sopra vive in Identity e valida il client tramite un **contratto
  cross-modulo** verso Infra (stesso pattern di [linee-guida-implementative-dotnet.md §6](linee-guida-implementative-dotnet.md),
  interfaccia pubblica sincrona), non possedendo direttamente la tabella. Dettaglio in §9bis.
- Il token esterno ha **audience diverso** → lo accetta **solo** `ApiBearer`, mai gli endpoint utente; e
  simmetricamente un token utente non passa la policy `ExternalApi`. È questo che separa i due mondi.
- Per M2M: **niente refresh token**, access token a vita medio-breve, il client ne richiede uno nuovo
  alla scadenza.
- **Compatibilità con i partner già integrati (decisione del 2026-10-07)**: i contratti legacy restano
  validi. OfSale e Transactions si autenticano con firma HMAC (`apiKey`, `signature`, `timestamp`) come
  su `auth/signinApi`; TeamSystem con `GET .../oauth/access_token` (client_credentials in querystring,
  token passato in querystring). Si mantengono forma delle richieste e delle risposte, correggendo i
  difetti di sicurezza (controllo del `timestamp` contro il replay, `expiresAt` rispettato, secret mai a DB).

---

## 8. Webhook (firma, non token)

Gli endpoint webhook (Stripe, Fiskaly) sono `[AllowAnonymous]` rispetto al JWT, ma **non** sono aperti:
si autenticano **verificando la firma** del provider nell'handler (es. la signature dello Stripe webhook).
Niente JWT utente, niente token API: verifica crittografica della firma.

---

## 9. Identity vs Customers vs Infra (dove stanno i dati)

| Dato | Modulo | Note |
|---|---|---|
| Account, `PasswordHash`, login esterni, ruoli/RBAC, scoping stabilimento | **Identity** | Minimale: solo autenticazione/autorizzazione **degli utenti umani** |
| Registro dei client M2M (`client_id`, `client_secret_ref`, scoping property, stato attivo/scaduto) | **Infra** (`ApiClient`/`IntegrationApp`) | Registro di integrazioni partner, non credenziale utente — vedi §9bis |
| Anagrafica cliente finale / rubrica per stabilimento (nome, contatti, indirizzo, cod. fiscale, blacklist) | **Identity**, sottoarea `customer` | Usata da Booking/Payment/Fiskaly tramite contratto; walk-in senza login |

Rubrica e account sono **due aggregati distinti** nello stesso modulo: `Customer.UserId` **nullable**
(null per i walk-in, il 97% delle righe). L'anchor di chi fa login è sempre l'`Account`. Gli altri
moduli referenziano il cliente copiando il suo `public_id`, mai l'`id`. Poiché il ruolo è nel token
(§2), gli altri moduli non chiamano Identity per autorizzare.

> **Decisione del 2026-10-07**: la rubrica resta in Identity, non in un modulo Customers/CRM separato
> come indicavano le versioni precedenti di questo documento. Se in futuro servisse estrarla, il
> prefisso `identity_customer_*` e i riferimenti per `public_id` la rendono separabile.

---

## 9bis. Registro dei client M2M: perché in Infra e non in Identity

Versione precedente di questo documento (§7, §9) assegnava il registro dei client esterni (key/secret)
a Identity, leggendolo come "un tipo di credenziale" alla pari di un account utente. Quando la tabella è
stata modellata sui dati reali ([`analisi-post/db/infra/infra_integration_api_clients.md`](../../analisi-post/db/infra/infra_integration_api_clients.md)),
è finita nel modulo **Infra** — correzione recepita qui, per questo motivo:

- **Il contenuto è un registro di integrazioni partner** (`app_name`, scoping `property_id`,
  attivo/scaduto), non un dato di autenticazione umana: niente ruolo, niente RBAC, niente login.
  Struttura e ciclo di vita (onboarding di un partner, rotazione, routing per integrazione) sono più
  vicini a `fiskaly_accounts`/`switchboard_configs`/`notification_email_channel_settings` — segreti di
  integrazione, ognuno nel modulo di dominio che possiede quell'integrazione — che a `identity_access_users`.
- **Coerenza con quanto già deciso altrove**: nessuno di quei segreti di integrazione è stato
  centralizzato in Identity; farlo solo per il registro M2M sarebbe un'eccezione senza un criterio che la
  giustifichi. I partner registrati oggi (OfSale, Transactions, TeamSystem) non hanno un singolo modulo
  di dominio proprietario ovvio (toccano Payment/export in modo trasversale) — Infra è il registro per i
  casi che non hanno un dominio specifico che li rivendica, non un'eccezione al principio.
- **Identity resta minimale** per obiettivo dichiarato (§1): possedere anche il ciclo di vita di
  integrazioni partner (onboarding, scoping per stabilimento, rotazione) andrebbe contro quell'obiettivo.
- **Non cambia il flusso di emissione del token**: Identity **continua a emettere** il JWT `ApiBearer`
  (§7) — valida il client tramite un contratto cross-modulo verso Infra (interfaccia pubblica sincrona,
  [linee-guida-implementative-dotnet.md §6](linee-guida-implementative-dotnet.md)), esattamente come
  dipenderebbe da un'interfaccia di qualunque altro modulo. Possedere il registro non è necessario per
  emettere il token.

Lo stesso registro va riusato, non duplicato, se in futuro il backend espone tool **MCP** a client
esterni ([28-mcp.md §5](../28-mcp.md)): stesso meccanismo di autenticazione M2M, un nuovo valore
nell'enum `IntegrationApp`.

---

## 10. Dove sta cosa

- **Configurazione schemi/policy** → **host** (`Program.cs`): infrastruttura trasversale.
- **Store e validazione credenziali utente + emissione token** → modulo **Identity**.
- **Registro dei client M2M** (`client_id`/`client_secret_ref`) → modulo **Infra**; Identity lo consulta
  via contratto cross-modulo per validare e **emettere** comunque il token `ApiBearer` (§7, §9bis).
- **Autorizzazione per singola operazione** → ogni **modulo**, dai claim del token (`role`, `client_id`,
  scope, `property_id`), **senza** chiamare Identity a runtime.

---

## 11. Riepilogo regole

1. **Secure-by-default** via `FallbackPolicy` (token utente); apri in opt-out con `[AllowAnonymous]`.
2. **Ruolo + scoping nel token** → autorizzazione decentralizzata, niente chiamate a Identity per richiesta.
3. **Due schemi JWT** (`UserBearer` / `ApiBearer`) con **audience diversi** per separare utenti e client esterni.
4. **Token esterni da key/secret** (client_credentials): secret **mai a DB** (solo `client_secret_ref` al
   secret manager), registro dei client in **Infra** (§9bis), audience dedicato.
5. **Webhook** autenticati via **firma**, non token.
6. **Identity** possiede accesso, rubrica clienti (`customer`) e abbonamenti (`account`); registro
   client M2M in **Infra** — link per `public_id` (cliente senza account per i walk-in).
7. **Access token a vita breve** + refresh (utenti); niente refresh per M2M.
