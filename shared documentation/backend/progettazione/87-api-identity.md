# API del modulo Identity

> Catalogo degli endpoint di `Module.Identity` (scritti il 2026-10-07). Regole comuni in
> [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md) (§5ter sessione,
> §4bis firma) e [pre/standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md) (errori). Il dettaglio
> dei campi è nello Swagger del modulo (`/swagger`, documento *Identity API*). Cose rimandate: [85-promemoria.md](85-promemoria.md).

## Regole comuni

- **Prefisso**: `/api/identity`. Fanno eccezione i due percorsi legacy dei partner (sotto).
- **Autenticazione**: `Authorization: Bearer <accessToken>`. Senza indicazioni un endpoint richiede il token
  utente; quelli marcati *anonimo* no.
- **Stabilimento**: sempre dal claim `property_id` del token, mai dal corpo o dalla querystring.
- **Errori**: Problem Details con `code` (`auth.*`, `account.*`, `customer.*`, `subscription.*`, `contract.*`,
  `validation.*`) e, per la validazione, `errors[]` con `field` + `code` + `args`.
- **Refresh token**: solo nel cookie HttpOnly (`Path=/api/identity`), mai nel corpo.
- **Enum nelle risposte**: codici snake_case stabili (`management_booking`, `active`, `fixed_term`, …).
- **Verbi**: `PUT` sostituisce la risorsa intera (un campo null viene svuotato), `PATCH` cambia solo i campi
  presenti (null = invariato), `POST` crea o esegue un comando non ripetibile (cambio password, nuovo periodo di
  abbonamento, transizioni di stato).
- **Lingue**: `it` (default), `en`, `fr`, `es`, `de`, `zh` (cinese semplificato), `ar` — vedi
  [12-lingue-e-date.md](12-lingue-e-date.md). Un codice diverso nei campi `language` è un errore di validazione.
- **Percorsi dei partner**: gli endpoint chiamati da terze parti (OfSale, Transactions, TeamSystem) mantengono
  l'indirizzo legacy, anche quando appartengono ad altri moduli.

## Permessi

| Permesso | Ruoli |
|---|---|
| `staff.manage`, `security.manage` | superadmin, admin |
| `customers.read` | superadmin, admin, manager, user1, marketing, lifeguard, cashoperator, dailycashoperator |
| `customers.contacts`, `customers.write` | superadmin, admin, manager, user1, marketing |
| `contracts.accept` | superadmin, admin |
| `audit.read` | superadmin, admin (per Module.Audit) |
| `platform.manage` | systemadmin |

La matrice è in `Shared/Security/RolePermissions.cs`; `GET /me` restituisce i permessi dell'utente, così il
frontend costruisce i menu dalla stessa fonte. Ruoli senza permessi Identity (waiter, affiliate,
advancedlifeguard, websiteadmin) accedono solo all'area personale.

## Accesso e sessione (anonimi, rate limit per IP)

| Metodo e percorso | Corpo | Risposta |
|---|---|---|
| `POST /auth/login` | `username` (email o username), `password`, `remember`, `app` (`hub`/`customer`/`admin`) | 200 `{ accessToken, expiresIn, user }` + cookie, oppure 200 `{ twoFactorRequired, mfaToken, expiresIn }` |
| `POST /auth/login/verify-otp` | `mfaToken`, `code` | 200 sessione + cookie |
| `POST /auth/login/google`, `/apple`, `/facebook` | `token` del provider, `app`, `remember`, `termsAccepted` (account nuovo), `email`/`firstName`/`lastName`/`language` facoltativi | come il login |
| `POST /auth/refresh` | solo cookie | 200 sessione + nuovo cookie (rotazione) |
| `POST /auth/logout` | solo cookie | sempre 204 |
| `POST /auth/register` | `email`, `password`, `firstName`, `lastName`, `phoneCountryCode`, `phone`, `language`, `termsAccepted`, `newsletterConsent` | 201 sessione + cookie |
| `GET /auth/email-availability?email=` | — | `{ available }` |
| `POST /auth/password/forgot` | `email`, `app` | sempre 204 |
| `POST /auth/password/reset` | `token` (dal link), `newPassword` | 204; chiude tutte le sessioni |
| `POST /auth/impersonation/exchange` | `code` | 200 `{ accessToken, expiresIn, user, impersonatorId }`, nessun refresh |

`user` = `{ id, email, role, propertyId }`.

**Impersonazione** ("accesso come gestore" del systemadmin), in due token:
1. `POST /admin/properties/{propertyId}/impersonation` restituisce un **codice** (JWT con audience proprio,
   vita di 60 secondi, monouso) che il back-office passa all'hub;
2. l'hub lo scambia con `POST /auth/impersonation/exchange` e riceve un **access token del titolare**
   (60 minuti, nessun refresh) che porta il claim standard `act` (RFC 8693) con il `sub` del systemadmin.
   Le azioni risultano del titolare, con il systemadmin registrato nell'audit; cambio password e chiusura
   dell'account sono vietati.

## Area personale (token utente)

| Metodo e percorso | Note |
|---|---|
| `GET /me` | profilo, ruolo, permessi, suoni, 2FA dello stabilimento, abbonamento, `allFeaturesUnlocked` |
| `PUT /me` | `firstName`, `lastName`, `phoneCountryCode`, `phone`: il profilo si invia intero, un campo null viene svuotato |
| `POST /me/password` | `currentPassword` (se l'account ne ha una), `newPassword`; chiude le altre sessioni, risponde con una sessione nuova |
| `PATCH /me/preferences` | `language` (vedi Lingue nelle regole comuni), `notificationSounds[]`, `newsSectionEnabled` (null = invariato) |
| `DELETE /me` | solo consumer; `password`, oppure `provider` + `providerToken` per gli account solo social |
| `GET /me/sessions`, `DELETE /me/sessions/{id}` | sessioni attive (una per dispositivo) |
| `GET /me/liked-bathhouses?page&pageSize` | preferiti |
| `PUT` / `DELETE /me/liked-bathhouses/{bathhouseId}` | mettere e togliere sono idempotenti |

Password e chiusura dell'account sono vietate in impersonazione.

## Stabilimento (token utente + permesso)

| Metodo e percorso | Permesso |
|---|---|
| `GET` / `POST /property-users`, `PATCH` / `DELETE /property-users/{id}` | `staff.manage` |
| `GET /roles/assignable` | `staff.manage` |
| `GET` / `PUT /property-security-settings` | `security.manage` |
| `GET /customers?search&blacklistOnly&sortBy&sortDir&page&pageSize` | `customers.read`; contatti in chiaro solo con `customers.contacts` (altrimenti `emailMasked`/`phoneMasked`) |
| `GET /customers/{id}` | `customers.contacts` |
| `POST /customers`, `PUT` / `DELETE /customers/{id}` | `customers.write` |
| `GET /customers/export?format=csv\|xlsx` (+ filtri) | `customers.write` |
| `GET /contract-documents/current` | `contracts.accept` (204 se non c'è un documento in vigore) |
| `PUT /contract-documents/{id}/acceptance` | `contracts.accept`; `{ accepted }` |

## Systemadmin (`platform.manage`)

| Metodo e percorso | Note |
|---|---|
| `GET` / `POST` / `PATCH /admin/properties/{propertyId}/owner` | titolare con password scelta dal systemadmin; email e username non modificabili |
| `POST /admin/properties/{propertyId}/impersonation` | `{ code, expiresIn }` da passare all'hub |
| `GET /admin/properties/{propertyId}/subscription` | stato attuale |
| `GET /admin/properties/{propertyId}/subscription/periods` | storico dei periodi |
| `POST /admin/properties/{propertyId}/subscription/periods` | apre un nuovo periodo (crea l'abbonamento se manca): `licensePlanCode`, `startsAt`, `endsAt`, `amount`, `currency`; `201` |
| `POST .../subscription/suspend`, `/reactivate`, `/cancel` | transizioni di stato |
| `GET` / `POST /admin/license-plans`, `PUT /admin/license-plans/{code}` | piani e traduzioni |
| `GET` / `POST /admin/contract-documents`, `POST .../{id}/publish` | upload PDF (multipart: `file`, `versionCode`, `title`, `notes`) |
| `GET /license-plans` | *anonimo*: piani attivi |

## Partner esterni (anonimi)

| Metodo e percorso | Contratto |
|---|---|
| `POST /api/identity/external/token` | `client_id`, `client_secret`, `grant_type=client_credentials` → `{ access_token, token_type, expires_in }` |
| `POST /api/auth/signinApi` | **legacy, invariato**: `{ apiKey, signature, timestamp }` → `{ token }` (24 h); HMAC-SHA256 di `apiKey:timestamp` (ms Unix) |
| `GET /api/sb/teamsystem/v1/oauth/access_token` | **legacy, invariato**: querystring, risposta ISO-8859-1 `{ access_token }` (7 giorni) |

I token dei partner valgono solo sugli endpoint con policy `ExternalApi` e si possono mandare anche come
header `x-access-token` o, per TeamSystem, come `access_token` in querystring.

## Contratto verso gli altri moduli (in-process)

`ICustomerDirectory` (Shared): `FindOrCreateAsync`, `IsBlacklistedAsync`, `IsRegisteredContactAsync`, per Booking
e Payment. Eventi pubblicati: `AccountRegistered`, `AccountDeleted`, `StaffContactChannelsChanged`,
`PropertyOwnerAssigned`, `PropertyOnlineReservationSet`, `PropertySubscriptionChanged`, `BathhouseLikeChanged`,
`AuditEntryRecorded` (salvato da Module.Audit, [88-api-audit.md](88-api-audit.md)) — gli altri oggi senza gestori (PROMEMORIA-85-5).

## Configurazione da fornire per ambiente

Dal secret manager, mai committati: `Jwt:UserKeys`, `Jwt:ApiKeys` (base64, ≥ 256 bit, con `Kid`),
`Auth:PasswordHash:Pepper` (versione 0, gli hash senza `k=`) e `Auth:PasswordHash:Peppers`
(`[{ "Version": N, "Key": base64 }]`, ≥ 256 bit: gli hash nuovi usano la versione più alta e la scrivono
come `k=N`; le precedenti restano per verificare e vengono sostituite al login), `Auth:Social:*` (client id
Google/Apple, app id e secret Facebook),
`Notification:Smtp:*`, `Secrets:<client_secret_ref>` dei partner. Da `appsettings.{env}.json`:
`Auth:AllowedOrigins`, `Auth:FrontendUrls`, `Identity:Documents:Bucket`, `Storage:PublicBaseUrls`,
`Hosting:TrustForwardedHeaders` dietro il load balancer.
Fuori da Development il backend **non parte senza pepper** (controllo all'avvio, 2026-10-20), salvo
`Auth:PasswordHash:AllowNoPepper=true`, da usare solo in un ambiente di prova che carica dati con hash
scritti senza pepper (es. sb-dev, alimentato dal DB locale).
`Jwt:Issuer` diverso per ogni ambiente (produzione: `summerbooking-identity-prod`, già in `appsettings.json`);
fuori dallo sviluppo l'avvio fallisce se issuer o chiavi sono quelli di sviluppo o se un `kid` inizia con `dev-`.
