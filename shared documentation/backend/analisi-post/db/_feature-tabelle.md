# Tabelle del nuovo schema — indice per dominio

> Indice unico delle tabelle del **nuovo schema** (post-refactoring) create in `analisi-post/db/`.
> Per il dettaglio completo (ogni campo con legacy/tipo/valori/default/nullabilità/relazioni, indici,
> suggerimenti di prestazioni, sezione .NET) vedere la scheda `<nome_tabella>.md` corrispondente.
>
> **Domande aperte** (per tutti i domini) → [`_domande-committente.md`](_domande-committente.md).
> **Proposta read-model JSON di configurazione** → [`_config-snapshot-proposta.md`](_config-snapshot-proposta.md).
> **Pattern comune della sezione "Implementazione .NET" di ogni scheda** → [`_dotnet-template.md`](_dotnet-template.md).
>
> **Sorgenti**: le schede legacy in [`../../analisi-pre/db/`](../../analisi-pre/db/) (riviste coi dati di
> produzione `sb_prod_dump_2026_09_08` + il DB locale completo `u811491364_summer_booking`), la
> business-logic in [`../../analisi-pre/business-logic/`](../../analisi-pre/business-logic/) e i documenti
> di indirizzo in [`../../docs/`](../../progettazione/) — in particolare
> [`03-gestione-dati.md`](../../progettazione/03-gestione-dati.md),
> [`01-criticita-e-indicazioni-backend.md`](../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`11-modulo-privacy.md`](../../progettazione/11-modulo-privacy.md), [`80-retention.md`](../../progettazione/80-retention.md),
> [`34-multitenant.md`](../../progettazione/34-multitenant.md),
> [`pre/autenticazione-autorizzazione-dotnet.md`](../../progettazione/pre/autenticazione-autorizzazione-dotnet.md),
> [`pre/identificatori-e-uuid.md`](../../progettazione/pre/identificatori-e-uuid.md).
>
> **Allineamento col DB (2026-10-06)**: tutte le schede sono state confrontate con lo schema reale di
> `summer_booking_dev` dopo il consolidamento delle migration in una `InitialCreate` per modulo
> ([`docs/42-migration.md` §10](../../progettazione/42-migration.md)) — colonne, tipi, nullabilità, default, valori
> degli enum, indici e vincoli. Dove il DB diverge dal progetto, la scheda descrive il DB e lo dice. Gli
> indici e i vincoli **progettati ma mai creati** restano nelle schede, marcati «⚠️ Non presente a DB».
> Lo stesso giorno ne sono stati creati 17 (migration `AddMissingDesignedIndexes` in 7 moduli, quasi tutti
> persi perché due `HasIndex` EF sulle stesse colonne collassano in uno) ed è stata corretta la FK duplicata
> di `payment_pos_charge_items` / `payment_wallet_transaction_items` (`FixOwnedItemsForeignKeys`).
> Restano aperti l'`EXCLUDE` anti-doppia-prenotazione di `booking_resource_consumptions` e l'unicità di
> email/username in `identity_access_users`: entrambi richiedono prima una decisione sui dati in conflitto.

---

## Stato per dominio

| Dominio | Modulo | Iterazione | Schede |
|---|---|---|---|
| **C5** | `identity` — autenticazione, autorizzazione, account/abbonamento, rubrica clienti | ✅ completata | `identity_*` (14 tabelle) |
| **C2** | `catalog` — catalogo & configurazione (mappa, listini, servizi, magazzino, convenzioni) + read-model di configurazione | ✅ completata | `catalog_*` (49 schede + lookup/figlie inline) |
| **C1** | `booking` — prenotazioni (testata unica postazioni+servizi, righe, consumi/disponibilità, rivendita/hold, gruppi, convenzioni) | ✅ completata | `booking_*` (19 schede) |
| **C3** | `payment` — Stripe Connect & commissioni, transazioni/rimborsi online, link di pagamento, ledger incassi, borsellino prepagato, addebiti POS (OfSale/TeamSystem) | ✅ completata | `payment_*` (13 schede) |
| **C4** | `fiskaly` — scontrino fiscale **elettronico** SIGN IT (account, casse, ricevute/pre-conto in testata unica, righe & pagamenti fiscali, log, sequenze, lookup IVA, PDF) | ✅ completata | `fiskaly_*` (9 schede) |
| **C4bis** | `fiscalprint` — stampa fiscale su **hardware RT** (config stampanti + reparti IVA, log stampe, sequenza) — *si mantiene come componente separato (decisione di business)* | ✅ completata | `fiscalprint_*` (4 schede) |
| **C6** | `notification` — outbox unico per destinatario, canale email, notifiche in-app, template, marketing (contatti/consenso/liste/campagne/automazioni/eventi/newsletter/lead), preferenze & push dello staff | ✅ completata | `notification_*` (21 schede) |
| **C7** | `switchboard` — centralino Telnyx (config, IVR ad albero, audio, registro chiamate) + WhatsApp Business (template Meta, log webhook) — *0 righe su `local_sb`/dump di produzione, ma **attiva e collaudata** su un terzo ambiente (`summer_booking_dev`, RDS) — vedi nota 2026-09-11; redesign pulito* | ✅ completata | `switchboard_*` (10 schede) |
| **C8** | `chatbot` — assistente AI dell'app gestionale (storico conversazioni, limiti per stabilimento, catalogo provider LLM) — *0 righe su `local_sb`/dump di produzione, **10 righe di conversazione + 1 di config reali** su `summer_booking_dev` (RDS) — verificato 2026-09-11; feature su branch a parte [`front-chatbot/`](../../../../../../back-chatbot/), non ancora in `back-pre`* | ✅ completata | `chatbot_*` (3 schede) |
| **INFRA** | `infra` — registro dei client API partner (credenziali → secret manager, mai a DB); raccoglie anche, come annotazione (non tabelle), tutti i riferimenti "segreto → secret manager" emersi negli altri domini (Fiskaly, SMTP, SIP, provider LLM) | ✅ completata | `infra_*` (1 scheda) |
| **Privacy** | `privacy` — modulo sottile di orchestrazione GDPR (richieste cancellazione/export Art. 15/17/20, retention dichiarativa, legal hold, audit di conformità); **non possiede mai un dato personale** di cliente/utente, orchestra via eventi i moduli proprietari | ✅ completata | `privacy_*` (5 schede) |
| **Audit** | `audit` — registro trasversale di chi ha fatto cosa (azioni degli operatori, eventi di sicurezza, azioni del systemadmin); i log tecnici di dominio restano nei moduli proprietari — *deciso il 2026-10-07, [`docs/86-modulo-audit.md`](../../progettazione/86-modulo-audit.md)* | ✅ schema creato il 2026-10-07 (solo persistenza, come i moduli senza API) | `audit_*` (1 scheda) |

---

## Convenzioni trasversali (tutti i domini)

| Aspetto | Regola | Rif. |
|---|---|---|
| **Nome tabella** | `snake_case`, plurale, forma flat `<dominio>_<sottoarea>_<nome>`; forma canonica `<dominio>.<sottoarea>_<nome>` (schema PostgreSQL per modulo) | [`03-gestione-dati.md` §3.1 / §3.1bis](../../progettazione/03-gestione-dati.md), [`note.md`](../../progettazione/note.md) |
| **PK interna** | `id bigint GENERATED ALWAYS AS IDENTITY` (o `integer`/`smallint` per lookup) su **ogni** tabella; id legacy preservati 1:1 in migrazione | [`03-gestione-dati.md` §3.1ter](../../progettazione/03-gestione-dati.md), [`pre/identificatori-e-uuid.md`](../../progettazione/pre/identificatori-e-uuid.md) |
| **Id pubblico** | `public_id uuid` (UUIDv7) UNIQUE **solo** sulle entità esposte in API/URL/GDPR **o** referenziate fuori dal proprio schema di modulo. Le FK **interne** allo schema restano `bigint`/`integer` | [`03-gestione-dati.md` §3.1ter](../../progettazione/03-gestione-dati.md) |
| **Riferimenti cross-modulo** | id **copiato per valore**, **mai** FK cross-schema. Un modulo che referenzia un'entità di un altro modulo copia il `public_id` di quella (mai il `bigint` interno) | [`03-gestione-dati.md` §3.1ter](../../progettazione/03-gestione-dati.md), [`34-multitenant.md`](../../progettazione/34-multitenant.md) |
| **Discriminatore tenant** | colonna `property_id` su ogni tabella multi-tenant, prima colonna degli indici compositi; il tipo è **oggetto di una contraddizione tra i documenti** (`docs/03 §3.1ter` → `bigint`; `docs/34-multitenant.md` → `uuid`). Le schede **C5** usano oggi `uuid`, le schede **C2** usano `bigint`. → [`_domande-committente.md` D-01](_domande-committente.md) | [`34-multitenant.md`](../../progettazione/34-multitenant.md), [`03-gestione-dati.md` §3.1ter](../../progettazione/03-gestione-dati.md) |
| **Timestamp** | `created_at`, `updated_at` `timestamptz NOT NULL DEFAULT now()` (+ trigger/interceptor); date business esplicite (`date` quando l'ora non ha significato) | [`03-gestione-dati.md` §3.3–§3.4](../../progettazione/03-gestione-dati.md) |
| **Soft-delete** | `deleted_at timestamptz NULL` su ogni tabella di dominio + Global Query Filter EF; tabelle di puro log/registro lo espongono per uniformità ma non lo usano | [`03-gestione-dati.md` §3.5](../../progettazione/03-gestione-dati.md) |
| **Enum** | `enum` PG nativo per vocabolari chiusi/stabili; **lookup** dove il vocabolario cresce senza deploy o serve mappare varianti storiche (`identity_access_roles`, `identity_account_license_plans`, `catalog_pricing_calculation_methods`) | [`03-gestione-dati.md` §2.2–§2.4, §3.10](../../progettazione/03-gestione-dati.md) |
| **JSON** | **evitato come modello di dominio**: i JSON legacy diventano colonne o righe. Ammesso un `jsonb` **derivato** (read-model di configurazione, ricostruibile dalle tabelle) — proposta in [`_config-snapshot-proposta.md`](_config-snapshot-proposta.md) | [`03-gestione-dati.md` §2.5](../../progettazione/03-gestione-dati.md), [`note.md`](../../progettazione/note.md) |
| **Prezzi/importi** | `numeric(12,2)` sempre (mai `float`/`varchar`); il legacy li salvava come stringa dentro JSON | [`03-gestione-dati.md` §3.7](../../progettazione/03-gestione-dati.md) |
| **Link/URL a file** | mai URL completo: FK verso la tabella di supporto del modulo (`<dominio>_media_assets`: `origin` + `path`); array URL → tabelle di giunzione ordinate | [`03-gestione-dati.md` §1, §3.1quater.A](../../progettazione/03-gestione-dati.md) |
| **Booleani** | prefisso `is_` / `has_` / `allows_`; interi 0/1 legacy → `boolean` reale | [`03-gestione-dati.md` §3.1](../../progettazione/03-gestione-dati.md) |
| **Colonna utente** | `<ruolo>_user_id` (`created_by_user_id`, `owner_user_id`, `actor_user_id`). Interno a C5: FK reale `bigint`. Da altri moduli verso un utente C5: `uuid` = `public_id` copiato | [`03-gestione-dati.md` §3.2, §3.1ter](../../progettazione/03-gestione-dati.md) |
| **Etichette tradotte** | **mai** `label_it`/`label_en`/… a colonne. Vocabolario code-bound (ruoli, categorie, metodi) → **risorse di localizzazione** (chiave = `code`). Vocabolario gestito a runtime con testo commerciale (piani, paesi) → **tabella figlia `<padre_singolare>_translations`** (`<fk>`, `culture`, `label`), N lingue | [`03-gestione-dati.md` §2.2 / §3.1quater.C](../../progettazione/03-gestione-dati.md) |
| **Enforcement multi-tenant** | `property_id`/`tenant_id` **sempre** dal claim del token (mai dal body) via policy di autorizzazione di modulo; Global Query Filter + (opz.) RLS | [`34-multitenant.md` §1](../../progettazione/34-multitenant.md), [`01-criticita-e-indicazioni-backend.md` §5/§7](../../progettazione/01-criticita-e-indicazioni-backend.md) |
| **Durate token / parametri hash** | valori di **configurazione** (`appsettings` + env); chiavi/pepper dal secret manager; non hardcoded | [`pre/autenticazione-autorizzazione-dotnet.md` §5bis](../../progettazione/pre/autenticazione-autorizzazione-dotnet.md) |

---
---

# Dominio C5 — `identity`

Modulo **`identity`**, tre sottoaree:

- **`access`** — autenticazione, autorizzazione, account, ruoli, sessioni.
- **`account`** — relazione contrattuale/abbonamento **piattaforma ↔ stabilimento**.
- **`customer`** — anagrafica dei clienti finali (rubrica per-stabilimento) e loro interazioni leggere
  con la vetrina (preferiti).

*(La sottoarea `account` non era in [`03-gestione-dati.md` §3.1bis](../../progettazione/03-gestione-dati.md):
emerge dalle schede `contract_documents` / `contract_acceptation` e da
[`11-modulo-privacy.md` §1](../../progettazione/11-modulo-privacy.md). Da recepire in §3.1bis.)*

## Sottoarea `access`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[identity_access_users](identity/identity_access_users.md)** | [`users`](../../analisi-pre/db/users.md) | account di login (titolare/staff/consumer); anagrafica **minimale** per autenticare/autorizzare | **sì** | `lower(email)` e `username` unici parziali (⚠️ progettati, **non presenti a DB** al 2026-10-06), `property_id` parziale, `role_id`, id social | `role_id` singolo (FK); email con unicità reale (⚠️ non ancora a DB: 36 email duplicate da bonificare); campi morti eliminati; campi cross-dominio spostati; abbonamento → sottoarea `account`; `affiliate_selected_locations` JSON → tabella figlia |
| **[identity_access_roles](identity/identity_access_roles.md)** | [`roles`](../../analisi-pre/db/roles.md) | catalogo dei 13 ruoli applicativi | no | `code` UNIQUE | resta **lookup**; seed 13 righe (id 1–13), **tutti mantenuti** (committente); `code` nel claim del token; **etichette da risorse `.resx`**; permessi `lifeguard`/`advancedlifeguard` definiti |
| ~~identity_access_user_resource_scopes~~ ❌ **eliminata 2026-09-30** | `users.affiliate_selected_locations` (JSON) | — | — | — | collassata in `identity_access_users.resource_scopes text[]` ([`docs/05-ottimizzazioni-db.md` §13.1](../../progettazione/05-ottimizzazioni-db.md)); scheda archiviata in [`_Zidentity_access_user_resource_scopes.md`](identity/_Zidentity_access_user_resource_scopes.md) |
| **[identity_access_property_security_settings](identity/identity_access_property_security_settings.md)** | `bathouses.isTwoFactorEnabled` (C2 → C5 per §6) | impostazioni di sicurezza dello **staff** per stabilimento (2FA) — 1 riga per property | no | `property_id` UNIQUE | 2FA per-stabilimento vs per-account = **da chiarire** ([D-23](_domande-committente.md)) |
| **[identity_access_refresh_tokens](identity/identity_access_refresh_tokens.md)** | *(nuova)* — il legacy emette JWT 365 gg senza revoca | refresh token revocabili + rotazione (famiglia) | no | `token_hash` UNIQUE, `(user_id)` attivi parziale, `expires_at` | append/stato, **no `deleted_at`**; retention hard-delete scaduti |
| **[identity_access_user_auth_challenges](identity/identity_access_user_auth_challenges.md)** | `users.two_factor_otp`/`expiry`/`loginOtp` + reset password `Cryptr` statico | sfide a tempo: OTP 2FA, verifica email, token reset password, magic link | no | `secret_hash` UNIQUE, `(user_id, purpose)` attive parziale, `expires_at` | segreto **hashed**, uso singolo, rate limit, no user enumeration; retention aggressiva |

## Sottoarea `account`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[identity_account_license_plans](identity/identity_account_license_plans.md)** | [`licenses`](../../analisi-pre/db/licenses.md) | catalogo dei piani/tier di abbonamento | no | `code` UNIQUE | resta **lookup**; seed 2 righe (`basic`, `advanced`); `duration_months` = solo default; **nessuna colonna label** |
| **[identity_account_license_plan_translations](identity/identity_account_license_plan_translations.md)** | *(nessuna)* | etichetta + descrizione del piano **per lingua** — 1 riga per (piano × `culture`) | no | `(license_plan_id, culture)` UNIQUE parziale | tabella di traduzioni (piani = dato runtime) |
| **[identity_account_subscriptions](identity/identity_account_subscriptions.md)** | campi `users.license`/`startDate`/`endDate`/`subscriptionStatus`/… | abbonamento **dello stabilimento** — periodo corrente — 1 riga per property | no | `property_id` UNIQUE, `license_plan_id`, `ends_at` parziale su `active` | guida `catalog_properties.is_online_sales_suspended`; `term_type` `fixed_term` / `legacy_day_bank`; `remaining_days_balance` non decrementa in `suspended` |
| **[identity_account_subscription_periods](identity/identity_account_subscription_periods.md)** | *(nessuna)* | storico **append-only**: 1 riga per periodo/rinnovo (piano, date, importo) | no | `(subscription_id, starts_at DESC)`, `(subscription_id) WHERE closed_at IS NULL` UNIQUE, `(property_id, starts_at)` | **richiesta committente**; `amount numeric(12,2)`; `period_kind` |
| **[identity_account_contract_documents](identity/identity_account_contract_documents.md)** | [`contract_documents`](../../analisi-pre/db/contract_documents.md) (0 righe) | catalogo dei documenti contrattuali B2B (addendum) da accettare | **sì** (ref. da C2 `catalog_properties`) | `public_id` UNIQUE, `version_code` unico parziale, `(status) WHERE superseded_at IS NULL` parziale + `UNIQUE` su espressione costante per "al più un documento corrente pubblicato" | versionamento **immutabile** (`supersedes_id`); URL PDF scomposto `origin`+`path` |
| **[identity_account_contract_acceptances](identity/identity_account_contract_acceptances.md)** | [`contract_acceptation`](../../analisi-pre/db/contract_acceptation.md) (0 righe) | **registro append-only** delle accettazioni/revoche (chi/quando/IP/hash) | no | `(property_id, contract_document_id, occurred_at DESC)` | il legacy aveva `propertyId` UNIQUE → ora una riga per **evento** |

## Sottoarea `customer`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[identity_customer_customers](identity/identity_customer_customers.md)** | [`customers`](../../analisi-pre/db/customers.md) (~224k righe) | rubrica clienti finali **per stabilimento** (spesso senza account) | **sì** (ref. da C1/C3/C4) | `(property_id)` parz., `(property_id, phone)` / `(property_id, lower(email))` parz., `(property_id, full_name)`, `(user_id)`, `last_activity_at` | 3 telefoni → `phone`+`phone_secondary`; `referenceId 0` → `user_id` NULL+FK; JSON `booking_consumer`/`customerData` eliminati; `note` art. 9 GDPR; `last_activity_at` per retention |
| **[identity_customer_bathhouse_likes](identity/identity_customer_bathhouse_likes.md)** | [`property_like_logs`](../../analisi-pre/db/property_like_logs.md) (799 righe) | "mi piace" di un utente consumer a una **struttura** (`bathhouse`) | no | `(user_id, bathhouse_id)` UNIQUE parziale, `(user_id)`, `(bathhouse_id)` | unicità; "non mi piace" = `deleted_at`; `catalog_bathhouses.likeCount` **per bathhouse** ricalcolato/evento; `bathhouse_id` = `public_id` (uuid) copiato da C2 |

### Campi legacy che **escono** da C5

| Campo legacy (`users`) | Va in | Perché |
|---|---|---|
| `notifyEmail`, `notifyWhatsapp` | **C6** `notification` | preferenze canale notifiche staff |
| `disableNotification` | **C6** | opt-out notifiche in-app |
| `deviceTokens` | **C6** | registrazione canale push (FCM) |
| `stripeCustomerId` | **C3** `payment` | id cliente Stripe carte salvate |
| `total_number_of_credit`, `total_credit_value`, `totalCreditsEarned` | **C3** | saldo crediti "sharing" |

Campi legacy **eliminati** (morti / mai raccolti): `users.birthDate`, `users.gender`, `users.loginOtp`,
`users.country`, `users.mobileCountryCode`, `roles`-permessi hardcoded, `property_like_logs.likedStatus`,
`contract_acceptation.accepted`. **Entra** in C5: `bathouses.isTwoFactorEnabled` (C2 → C5, aggregato per
`property` con `bool_or` in migrazione).

---
---

# Dominio C2 — `catalog`

Modulo **`catalog`**, 5 sottoaree + radice ([`03-gestione-dati.md` §3.1bis](../../progettazione/03-gestione-dati.md)):

- **radice** — anagrafica gestionale (`catalog_properties`, **tenant root**) e vetrina pubblica
  (`catalog_bathhouses`); supporto file (`catalog_media_assets`); config canale "prenotazione online"
  (postazioni + servizi).
- **`map`** — settori, layout griglia, catalogo icone (master SuperAdmin), **anagrafica postazioni**
  (`catalog_map_spots`, il `resource_id` che C1 referenzia), legenda.
- **`pricing`** — stagioni e periodi tariffari, listini (pacchetti + articoli aggiuntivi) con prezzi per
  settore/periodo/data, sconti/voucher, Revenue Management (config + prezzo suggerito).
- **`service`** — dotazioni master (ricerca pubblica) + servizi bar/normali/orari venduti; cataloghi icone.
- **`inventory`** — quantità di magazzino degli articoli fisici.
- **`agreement`** — strutture partner ("convenzioni"/hotel) e i loro listini/voucher.
- **lookup** — paesi (`catalog_countries`, + traduzioni). *(`property_search_regions` verificata non usata → non migrata, [D-21].)*

## Radice del dominio (nessuna sottoarea)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[catalog_media_assets](catalog/catalog_media_assets.md)** | *(nuova)* — `varchar`/JSON con URL completo | riferimento a file scomposto in `origin` + `path` | no | `kind`, `origin` | mappa `origin → base URL` in configurazione; una tabella **per modulo** |
| **[catalog_properties](catalog/catalog_properties.md)** | [`properties`](../../analisi-pre/db/properties.md) | anagrafica gestionale / **tenant root** | sì (URL admin) | `public_id`, `status` parz., `owner_user_public_id` parz., `country_id`, `lower(name)` GIN trgm | `id` = discriminatore tenant; `fiskaly*`→C4, `commissionAmount`→C3, `isTwoFactorEnabled`→C5, `photos` non migrato; N:1 con bathhouse |
| **[catalog_bathhouses](catalog/catalog_bathhouses.md)** | [`bathouses`](../../analisi-pre/db/bathouses.md) (10.432 righe, ~180 reali) | vetrina pubblica dello stabilimento | **sì** (C5 likes) | `public_id`, `slug` unico **parz.** (legacy: 60 indici), `property_id`, `published_at` parz., geo, `region`/`city` | `status` vestigiale → `published_at`; `likeCount`/2FA→C5; `services`/`photos`→giunzioni; geo `varchar`→`numeric`; `pets`→enum, `cancellation`→campo strutturato ([D-10]) |
| **[catalog_bathhouse_photos](catalog/catalog_bathhouse_photos.md)** | `bathouses.photos` (array JSON) | 1 foto galleria in posizione `sort_order` | no | `(bathhouse_id, sort_order)` unico parz. | add/remove = `INSERT`/`DELETE` (risolve la condizione legacy sempre falsa) |
| **[catalog_bathhouse_amenities](catalog/catalog_bathhouse_amenities.md)** | `bathouses.services` (copia integrale JSON del master) | (vetrina × dotazione offerta) | no | `(bathhouse_id, amenity_id)` unico parz., `(amenity_id)` parz. | giunzione N:N |
| **[catalog_property_online_booking_settings](catalog/catalog_property_online_booking_settings.md)** | [`online_reservations`](../../analisi-pre/db/online_reservations.md) (relitto, 16 righe) + `properties.onlineReservation`/`onlineBookingOptions`/`paymentOptions`/`superAdminOnlineFlag` | config canale online **postazioni** (1 per property) | no | `property_id` UNIQUE | **Option A** (committente); 3 boolean d'asse indipendenti ([D-02]); 5 flag `paymentOptions` → **`enabled_payment_methods enum[]`**; `allows_current_day`→`allows_same_day_booking`; colonne carta eliminate (PCI); `superAdminOnlineFlag`→`catalog_properties.is_online_sales_suspended` |
| **[catalog_property_online_allowed_locations](catalog/catalog_property_online_allowed_locations.md)** | `onlineBookingOptions.locations` (array JSON tipi misti) | postazione ammessa online | no | `(settings_id, spot_id)` unico parz. | FK reale → `catalog_map_spots` |
| **[catalog_property_online_allowed_periods](catalog/catalog_property_online_allowed_periods.md)** | `onlineBookingOptions.period` (array/oggetto JSON) | finestra di date consentita online | no | `(settings_id, starts_on, ends_on)` | forma normalizzata; `key` react-date-range scartato |
| **[catalog_property_online_stay_limits](catalog/catalog_property_online_stay_limits.md)** | `onlineBookingOptions.locationOptionObj.bookingLimit` (tabella-nel-JSON) | min/max giorni per (settore, finestra) | no | `(sector_id, starts_on, ends_on)` | `sectorName`→FK, stringhe→`smallint`; **enforcement server-side** (era solo UI) |
| **[catalog_property_service_booking_settings](catalog/catalog_property_service_booking_settings.md)** | `properties.serviceOnlineBooking` + `serviceBookingOptions` + `servicebookingPaymentOptions` | config canale online **servizi** (1 per property) | no | `property_id` UNIQUE | canale separato (committente); 5 flag pagamento → **`enabled_payment_methods enum[]`**; `is_enabled` non più toccato di nascosto da `deleteStripeAccount` |

## Sottoarea `map`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[catalog_map_icon_categories](catalog/catalog_map_icon_categories.md)** | [`configure_categories`](../../analisi-pre/db/configure_categories.md) (5 righe) | categoria di icona mappa | no | `code` UNIQUE | lookup/enum; FK reale da icone (era match per stringa) |
| **[catalog_map_icons](catalog/catalog_map_icons.md)** | [`configure_images`](../../analisi-pre/db/configure_images.md) (89 righe) | icona master della mappa (SuperAdmin) | no | `(category_id, type)` parz., `(media_asset_id)` | `category`/`type` stringa→FK/enum; `image` URL→`media_asset`; delete = soft + GC |
| **[catalog_map_sectors](catalog/catalog_map_sectors.md)** | [`sectors_rows`](../../analisi-pre/db/sectors_rows.md) (175 righe = 1 blob) | settore/fila della spiaggia | no | `(property_id, lower(name))` unico parz., `(property_id, sort_order)` parz. | id **stabile** (legacy `currentId` = `Math.random()`); i riferimenti "per nome" del dominio → FK |
| **[catalog_map_layouts](catalog/catalog_map_layouts.md)** | [`configure_maps`](../../analisi-pre/db/configure_maps.md) (175 righe) | layout griglia (1 per property) + figlia `*_special_icons` | no | `property_id` UNIQUE | `items`→`grid_columns`; 22 colonne icona→FK+figlia; `specialCabin` (sempre NULL) scartato; resize non più troncamento silenzioso ([D-11](_domande-committente.md)) |
| **[catalog_map_spots](catalog/catalog_map_spots.md)** | contenuto di `configurations.beachMapData` (173 blob, ≤670 KB, 21 forme) | **una postazione** (ombrellone/lettino/cabina/…) | **sì** (C1 `resource_id`) | `public_id`, `(property_id, lower(label))` unico parz., `(property_id, spot_type)` parz., `(sector_id)` parz., `(property_id, grid_row, grid_column)` | `items.name` hardcoded→enum `spot_type` (allineato a `resource_type` §3.8); stato occupazione→C1; parametri sharing di `configurations`→C1 |
| **[catalog_map_legend_items](catalog/catalog_map_legend_items.md)** | `onlineBookingOptions.locationOptionObj.legendList` (array JSON) | voce di legenda della mappa online | no | `(property_id, sort_order)` parz. | `icon` URL→`media_asset` |

## Sottoarea `pricing`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[catalog_pricing_seasons](catalog/catalog_pricing_seasons.md)** | scalari di [`seasons_periods`](../../analisi-pre/db/seasons_periods.md) (171 righe) | confini stagione corrente + successiva (1 per property) | no | `property_id` UNIQUE | `next_season_*` per rinnovo pass stagionali (C1); bug `await` mancante corretto |
| **[catalog_pricing_tariff_periods](catalog/catalog_pricing_tariff_periods.md)** | `seasons_periods.periods` (+ `selectedRanges`) | un intervallo di un periodo tariffario | no | `(season_id, period_group_key)`, `(season_id, starts_on, ends_on)` | periodo non contiguo = N righe con stesso `period_group_key`; `tier` enum; il `LIKE` legacy verso i listini → FK |
| **[catalog_pricing_price_lists](catalog/catalog_pricing_price_lists.md)** | testata di [`price_lists`](../../analisi-pre/db/price_lists.md) (~2.920 righe; ex `catalog_pricing_packages`) + lookup `catalog_pricing_calculation_methods` | una **voce di listino** (pacchetto **o** articolo aggiuntivo, discriminati da `kind`) | **sì** (C1 riga prenotazione) | `public_id`, `(property_id, kind)` parz., `WHERE is_default` parz., `WHERE sales_channel <> 'manager_only'` parz. | 6 JSON→flag/scalari + **7 figlie**; `type`/`priceCalculation`/`monthly`/`weekend`→enum/lookup ([D-13]); `enableOnlineReservation`+`enableOnlineOnly`→**`enum sales_channel`**; `name_it` rimosso; `packageName` colonna morta **non** migrata |
| **[catalog_pricing_price_list_items](catalog/catalog_pricing_price_list_items.md)** | `price_lists.details.{PackageItemSelected, selectedAreaItems, IncludedServices, selectedPackages}` | una componente del pacchetto | no | `(price_list_id)` parz., `(inventory_item_id)` parz., `(linked_price_list_id)` parz. | `item_kind` discriminatore + FK reali |
| **[catalog_pricing_price_list_sector_prices](catalog/catalog_pricing_price_list_sector_prices.md)** | `price_lists.mainPrices` + `details.sectorRowsPkg`/`selectedRowsData` | prezzo base per (pacchetto, settore) | no | `(price_list_id, sector_id)` unico parz., `(sector_id)` parz. | l'esistenza della riga = pacchetto disponibile nel settore; stringhe→`numeric` |
| **[catalog_pricing_price_list_period_prices](catalog/catalog_pricing_price_list_period_prices.md)** | `price_lists.detailPrices` (tariffe base) | tariffe base per (voce, periodo tariffario) | no | `(price_list_id, tariff_period_group)` unico parz. | 7 colonne prezzo tipizzate; `name` `LIKE`→FK al gruppo di periodo |
| **[catalog_pricing_price_list_stay_prices](catalog/catalog_pricing_price_list_stay_prices.md)** | `price_lists.detailPrices[].edit_price_data` (griglia giorni 1–90) | prezzo **per numero di giorni** (voce × periodo × settore × N giorni) — calcolato + **override manuale** del gestore | no | `(period_price_id, sector_id, stay_days)` unico parz. | verificato ([D-15b]): `edit_price_data[]` è keyata su `day` (1–90) con `defaultPrice` calcolato + `price`/`isPriceChange`; si persistono solo gli override |
| **[catalog_pricing_price_list_date_prices](catalog/catalog_pricing_price_list_date_prices.md)** | `price_lists.holidayDates` (+ eventuali `edit_price_data` legacy per-data) | override per **data di calendario** / festivo (voce, data) | no | `(price_list_id, price_date, kind)` unico parz. | `kind` `override`/`holiday`; la griglia prezzi **per numero di giorni** è in `..._stay_prices` |
| **[catalog_pricing_price_list_reduced_windows](catalog/catalog_pricing_price_list_reduced_windows.md)** | `price_lists.details.setReducedPrice` (+ figlia `catalog_pricing_price_list_reduced_prices`) | intervallo di date con prezzi ridotti (match **esatto** con le date della prenotazione) | no | `(price_list_id, starts_on, ends_on)` UNIQUE parz. | verificato ([D-15](_domande-committente.md)): `additionalData[].name` = **settore** → FK; `.value` = **prezzo assoluto** per settore → figlia |
| **[catalog_pricing_suggested_prices](catalog/catalog_pricing_suggested_prices.md)** | `price_lists.suggestedPriceDetails` (JSON per-anno riscritto in-place) | prezzo suggerito Revenue Mgmt per (pacchetto, data) | no | `(price_list_id, price_date)` UNIQUE, `(property_id, price_date) WHERE is_applied` parz. | upsert per riga (no blob); 3 booleani→`applied_mode` enum; fix cache non invalidata |
| **[catalog_pricing_discounts](catalog/catalog_pricing_discounts.md)** | [`discounts`](../../analisi-pre/db/discounts.md) (50 righe = 1 blob, doppio-incapsulato) | un voucher/codice sconto | **sì** (snapshot C1) | `public_id`, `(property_id, upper(code))` unico parz., `WHERE is_active` parz. | `name` era anche codice→`code`+`name`; `used_count` con incremento **atomico** server-side (era manipolabile + race); `type`→enum condiviso C1; `voucherType`→`scope` ([D-16]); `checkManagementFlag`/`checkOnlineFlag`→**`enum applicable_channel`**; `checkBar/Normal/Hourly/AdditionalService`→**`applies_to_service_categories enum[]`**; endpoint di scrittura di massa senza auth non riprodotto |
| **[catalog_pricing_discount_date_ranges](catalog/catalog_pricing_discount_date_ranges.md)** | `discounts.items[].voucherDateRange` | finestra di validità di un voucher | no | `(discount_id, starts_on, ends_on)` parz. | — |
| **[catalog_pricing_discount_sectors](catalog/catalog_pricing_discount_sectors.md)** | `discounts.items[].selectedRow` (nomi settore) | (voucher × settore ammesso) | no | `(discount_id, sector_id)` unico parz., `(sector_id)` | FK reale → `catalog_map_sectors` |
| **[catalog_pricing_revenue_settings](catalog/catalog_pricing_revenue_settings.md)** | [`revenue_notifications`](../../analisi-pre/db/revenue_notifications.md) (11 righe) | config Revenue Mgmt (1 per property) | no | `property_id` UNIQUE (mancava nel legacy) | **non C6** (nome fuorviante): config `pricing`; `discounts` (colonna morta) non migrata; `daily_banner_shown_count` incrementato **server-side**; logica finestra oraria da portare server-side |
| **[catalog_pricing_revenue_discounted_spots](catalog/catalog_pricing_revenue_discounted_spots.md)** | `revenue_notifications.enableDiscountsForSelectedLocations` (array codici cella) | (config × postazione scontata) | no | `(settings_id, spot_id)` unico parz., `(spot_id)` | codice cella `R{r}C{c}`→FK `catalog_map_spots` |

*Lookup inline: `catalog_pricing_calculation_methods` (`id`, `code` UNIQUE `proportional`/`daily`/`daily_with_weekend`/`logarithmic`, `is_active`) — mappa esplicita delle 8 varianti sporche di `priceCalculation`.*

## Sottoarea `service`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[catalog_service_amenity_categories](catalog/catalog_service_amenity_categories.md)** | [`service_type_filters`](../../analisi-pre/db/service_type_filters.md) (6 righe + 1 test; ex `_amenity_filters`) | categoria di dotazione (raggruppa nella ricerca pubblica) | no | `code` UNIQUE | rinominata `_categories`; `filter_id`→`category_id`; refuso `"Restuarant"`→`restaurant`; etichette da risorse; test scartato |
| **[catalog_service_amenities](catalog/catalog_service_amenities.md)** | [`services`](../../analisi-pre/db/services.md) (48 righe master) | una dotazione/servizio dichiarabile (Wi-Fi, bar, …) | no | `(category_id) WHERE is_active` parz., `(icon_asset_id)` | rinominata `amenities`; `serviceFilterType` int→`category_id` FK (orfano `0`→NULL); `serviceName`/`serviceItalian`→`name` + figlia `..._translations` (N lingue); `isChecked` scartato |
| **[catalog_service_amenity_translations](catalog/catalog_service_amenity_translations.md)** | `services.serviceItalian` (+ N lingue nuove) | nome dotazione per lingua — 1 riga per (amenity × `culture`) | no | `(amenity_id, culture)` UNIQUE parz. | master consumer-facing → traduzioni in figlia, non colonne `name_it` |
| **[catalog_service_type_icons](catalog/catalog_service_type_icons.md)** | [`servicetypes_icons`](../../analisi-pre/db/servicetypes_icons.md) (61 righe master) | icona per i 3 tipi di servizio venduto | no | `(type) WHERE is_active` parz., `(icon_asset_id)` | `type`→enum `bar`/`normal`/`hourly`; `icon` URL→FK; `category` (colonna morta 100%) non migrata |
| **[catalog_service_sale_items](catalog/catalog_service_sale_items.md)** | [`services_types`](../../analisi-pre/db/services_types.md) (88 righe = 1 blob, `barServices` ≤4 MB) | un servizio venduto (bar/normale/orario) | **sì** (C1 consumi) | `public_id`, `(property_id, kind)` parz., `WHERE is_purchasable` parz. | 3 array JSON→1 tabella con `kind`; `Servicemage` (refuso URL)→FK; `ServicePrice` stringa→`numeric`; `PurchaseStatus`→`is_purchasable`; `currentId` scartato |
| ~~catalog_service_sale_item_weekdays~~ ❌ **eliminata 2026-09-21** | `hourlyServices[].day_constant` (array di 7) | — | — | — | collassata in `catalog_service_sale_items.active_weekdays catalog.weekday[]` ([`docs/05-ottimizzazioni-db.md` §13](../../progettazione/05-ottimizzazioni-db.md)); scheda archiviata in [`_Zcatalog_service_sale_item_weekdays.md`](catalog/_Zcatalog_service_sale_item_weekdays.md) |
| **[catalog_service_sale_item_time_slots](catalog/catalog_service_sale_item_time_slots.md)** | `hourlyServices[].timeslot_constants` (array) | fascia oraria interna con prezzo dedicato | no | `(sale_item_id, sort_order)` parz. | `name` `"9-11"`→`label` + `slot_start`/`slot_end` `time`; `price` stringa→`numeric` (usato da C1 `resource_consumptions.slot_*`) |

## Sottoarea `inventory`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[catalog_inventory_items](catalog/catalog_inventory_items.md)** | [`warehouses`](../../analisi-pre/db/warehouses.md) (172 righe = 1 blob) | un articolo di magazzino + capienza | **sì** (C1 consumi) | `public_id`, `(property_id, lower(name))` unico parz., `(property_id) WHERE is_active` parz. | `items` array→righe; `quantity`/`threshold` stringa→`integer`; `isEnable`→`is_active`; `currentId` scartato; il **consumo** per data è C1 (`resource_consumptions`, §3.8) |

## Sottoarea `agreement` (Convenzioni / Hotel)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[catalog_agreement_partners](catalog/catalog_agreement_partners.md)** | [`hotels`](../../analisi-pre/db/hotels.md) (112 righe, ~apr 2026) | struttura partner ("convenzione") di uno stabilimento | **sì** (C1 `bookings.hotelId`) | `public_id`, `(property_id)` parz., `lower(name)` GIN trgm | rinominata `agreement_partners`; `isDeleted`→`deleted_at` + cascade sui listini; ownership già server-side (no IDOR); `isManager` era commentato → policy di modulo |
| ✅ **[catalog_agreement_reports](catalog/catalog_agreement_reports.md)** 🆕 | [`hotel_reports`](../../analisi-pre/db/hotel_reports.md) (2 righe in produzione, entrambe senza PDF) | report periodico (arrivi/partenze/ricavi) inviato a una struttura partner | **sì** | `public_id`, `(partner_id)` parz., `(period_start, period_end)` | 🔄 **Aggiunta il 2026-10-02**: il titolare ha deciso che la funzione si rifà **e archivia** ([`docs/83-retention-file.md` §7-G1](../../progettazione/83-retention-file.md)), e nel nuovo schema non esisteva nessuna tabella per i report. Mappatura campo per campo e i due difetti del legacy da non riprodurre in [`catalog_agreement_reports.md`](catalog/catalog_agreement_reports.md). Il **PDF archiviato** va in `notification.media_assets` con `kind = 'rendered_pdf'` (C6), non qui. `reportData` → `report_data jsonb`, che è una **cache**: la fonte resta `booking.*`. **Nessun dato da migrare**. ✅ **Applicata il 2026-10-03** (`20261003080145_AddAgreementReports`), scheda scritta |
| **[catalog_agreement_price_lists](catalog/catalog_agreement_price_lists.md)** | [`hotel_price_lists`](../../analisi-pre/db/hotel_price_lists.md) (388 righe; 310 voucher) | listino/voucher venduto da una struttura partner | **sì** (C1 `hotel_payments`) | `public_id`, `(partner_id, list_type)` parz. | stessa struttura di `catalog_pricing_price_lists` + `list_type` (enum `regular`/`voucher`)/`min_booking_days`/`max_voucher_quantity`; `packageName` **qui è reale** → migrato; **nessuna FK a stagioni**; value object `PriceListDefinition` condiviso |
| **[catalog_agreement_price_list_items](catalog/catalog_agreement_price_list_items.md)** | `hotel_price_lists.details.{PackageItemSelected, …}` | componente di un listino hotel | no | `(price_list_id)` parz., `(inventory_item_id)` parz. | analoga a `catalog_pricing_price_list_items` |
| **[catalog_agreement_price_list_sector_prices](catalog/catalog_agreement_price_list_sector_prices.md)** | `hotel_price_lists.mainPrices` (`value`/`halfday`/`seasonal`) | prezzo per (listino hotel, settore) | no | `(price_list_id, sector_id)` unico parz., `(sector_id)` parz. | stringhe→`numeric`; `name`→FK settore |
| **[catalog_agreement_price_list_period_prices](catalog/catalog_agreement_price_list_period_prices.md)** | `hotel_price_lists.detailPrices` (+ figlia date-prices se usata) | prezzi per (listino hotel, periodo `period_label`) | no | `(price_list_id)` parz. | `period_label` testo libero (nessuna FK stagioni, coerente col legacy) |
| **[catalog_agreement_price_list_date_prices](catalog/catalog_agreement_price_list_date_prices.md)** | `hotel_price_lists.detailPrices[].edit_price_data` / `holidayDates` | override per data / festivo di un listino hotel | no | `(price_list_id, price_date, kind)` unico parz. | in produzione quasi sempre vuota (`holidayDates` = `[]`) |

## Lookup / vetrina

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[catalog_countries](catalog/catalog_countries.md)** | [`country`](../../analisi-pre/db/country.md) (11 righe) + figlia `catalog_country_translations` | un paese selezionabile | no | `code` UNIQUE (ISO alpha-2) | `+ code` ISO (mancava); `country_english`/`country_italian`→tabella traduzioni; `status`→`is_active`; doppia coppia timestamp ripulita; `code` copiato dai moduli |
| **[catalog_country_translations](catalog/catalog_country_translations.md)** | `country.country_english` / `country.country_italian` | nome paese per lingua — 1 riga per (paese × `culture`) | no | `(country_id, culture)` UNIQUE parz. | seed 2 righe/paese (`en`, `it`); estendibile a N lingue |

## Read-model di configurazione (JSON derivato)

> Fonte di verità = le tabelle normalizzate sopra; queste due sono **proiezioni ricostruibili** in
> `jsonb`, servite wholesale al frontend. Disegno: [`_config-snapshot-proposta.md`](_config-snapshot-proposta.md).

| Tabella | Cosa contiene | Chiave | Aggiornamento | Note |
|---|---|---|---|---|
| **[catalog_config_snapshots](catalog/catalog_config_snapshots.md)** | 1 riga per `(property_id, snapshot_kind)`; `payload jsonb` pronto per il client. `snapshot_kind`: `property_management_config`, `beach_map`, `public_showcase`, `price_catalog` (listino proprio), `services_catalog`, `agreement_catalog` (convenzioni — solo se la property ha partner) | `(property_id, snapshot_kind)` UNIQUE | rebuild **sincrono** su evento di dominio + job **notturno** totale + utility manuale | `payload_schema` (versione forma), `content_hash` → `ETag`, `source_hash` (skip no-op); URL come `assetRef` `{origin,path}`, mai completi; unica **eccezione consentita** al "niente JSON" (dato derivato) |
| **[catalog_search_bathhouses](catalog/catalog_search_bathhouses.md)** | 1 riga per vetrina **pubblicata**; colonne indicizzate per filtrare/ordinare (`min_price`, `amenity_ids` GIN, `name` trigram; posizione in `latitude`/`longitude` senza indice spaziale — progettata `location` GiST, scartata per non dipendere da PostGIS) + `card jsonb` per la card di risultato | `bathhouse_public_id` (= `catalog_bathhouses.public_id`) | come sopra (+ evento `BathhouseLikeCountChanged` da C5) | sostituisce (come comportamento) il flusso legacy `serchProperty` (`findAll` + N×`getLowestPrice` + filtro JS); `min_price` **materializzato** qui |

### Campi legacy che **escono** da C2

| Campo legacy | Va in | Perché |
|---|---|---|
| `properties.fiskalyFlag` / `fiskalyEnabled` / `fiskalyAutoreceipt` | **C4** `fiskaly` | step/interruttori attivazione fiscalità elettronica |
| `properties.commissionAmount` (`onlineCommission`, `iframeCommission`) | **C3** `payment` | tasso commissione incassi online (da portare server-side; eccezioni hardcoded `COMMISSION_FREE_PROPERTY_IDS` → dato di dominio) |
| `bathouses.isTwoFactorEnabled` | **C5** `identity` | interruttore 2FA per tutto lo staff |
| `bathouses.likeCount` | **C5** (proiezione verso C2) | contatore denormalizzato "mi piace" — ricalcolato da `COUNT(*)` |
| `configurations.sharing_enable` / `sharing_seasonal_workers` / `credit_perday_resold` / `credit_value` / `disable_both_options` / `enable_sharing_on_all_article` / `plan_absence_as_sharing` | **C1** `booking` (sottoarea rivendita/`hold`) | parametri feature "sharing / rivendita del posto" — vivono nella config mappa solo per accidente |
| `online_reservations.cvc` / `cardHodler` / `cardNumber` / `expiry` | **eliminati** | PAN/CVC in chiaro — violazione PCI-DSS, mai a DB |
| `properties.photos` | **eliminato** | array quasi sempre `[]`, duplicato di `bathouses.photos` |

`hotel_message_templates` **non è C2**: va in **C6** (`notification.templates`, unificata con
`custom_templates`) — decisione committente.

---
---

# Dominio C1 — `booking`

Modulo **`booking`**, testata + righe **senza sottoarea** (nucleo del dominio) + tre sottoaree:

- **nucleo** — `booking_reservations` (testata **unica** postazioni+servizi), `booking_lines` (righe
  postazione), articoli aggiuntivi, sconti, ospiti, partecipanti, generatore numero, date "azioni
  rapide", **consumi/disponibilità** (`booking_resource_consumptions` + aggregato), voci convenzioni.
- **`service`** — servizi bar/normali/orari venduti (`booking_service_lines` + slot).
- **`hold`** (blocco/riserva/rivendita) — blocco morbido postazione, offerte di rivendita/sharing,
  ledger crediti, impostazioni sharing.
- **`group`** — raggruppamento manuale di prenotazioni.

*(Il perimetro esclude ciò che nel legacy stava "sotto Prenotazioni" ma appartiene ad altri moduli —
[`docs/03-gestione-dati.md` §4](../../progettazione/03-gestione-dati.md): i **movimenti** di incasso, i link
Stripe, le transazioni/rimborsi online, il borsellino → **C3** `payment`; lo scontrino elettronico e le
righe fiscali → **C4** `fiskaly`; i flag "canale + inviato" e l'attribuzione campagna → **C6**
`notification`; la config del canale online → **C2** `catalog`.)*

## Nucleo (nessuna sottoarea)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[booking_reservations](booking/booking_reservations.md)** | [`bookings`](../../analisi-pre/db/bookings.md) (testata) + [`service_reservation`](../../analisi-pre/db/service_reservation.md) (testata) + [`booking_payments`](../../analisi-pre/db/booking_payments.md) / [`service_reservation_payments`](../../analisi-pre/db/service_reservation_payments.md) (riepilogo) | **1 riga per prenotazione logica** (un solo `booking_number`), postazioni **e/o** servizi; discriminatore `reservation_kind` | **sì** (C3/C4/C6) | `booking_number` unico parz., `reservation_code` unico parz., `(property_id, stay_from)`, `(property_id, reservation_status)`, `(customer_id)`, `WHERE balance_due > 0`, `WHERE online_confirmation_state = 'pending'`, `(agreement_partner_id)` | N righe `bookings` con lo stesso numero → **1**; JSON `booking_payments` → **colonne** `subtotal`/`discount`/`surcharge`/`total`/`deposit`/`paid`/`balance_due` (senza suffisso `_amount`, allineate a `docs/03 §3.7`), `appliedPrice` → `price_override`; movimenti → C3; `bookingByAdmin`→`booking_channel`; `status` 0/1/2/3 → `reservation_status` enum separato da `deleted_at`; `is_booking_confirmed` (bug, mai avanzato) → `online_confirmation_state` che **avanza**; `bookingType` → `listing_type` (`standard`/`agreement_price_list`/`agreement_voucher`), `hotelId` → `agreement_partner_id`; `isBookingDoneBySuggestedPrice`/`isRecalculate`/`isEditedFinalPrice` → **`price_flags enum[]`**; `booking_agent`/`reservation_code`/`bookingNumber` generati **server-side**; `booking_consumer`/`guest_data` → FK C5 + figlia; colonne morte (`booking_map`, `sharing_dates`, `mainPhoto`, `ofsale_payments`, backfill `consumer_*`) non migrate; `isMarketingCampaign`/`summaryServicesData` → C6/C3 |
| **[booking_lines](booking/booking_lines.md)** | [`bookings`](../../analisi-pre/db/bookings.md) (singola riga) + JSON `booking_locations` / `packageSelectedData` | 1 riga per **postazione × intervallo contiguo × fascia** (= la vecchia riga `bookings`) | **sì** (C3 righe, C4 line item) | `booking_line_number` unico parz., `(property_id, resource_id, start_date, end_date)`, `(property_id, start_date)`, `(price_list_id)`, `WHERE checked_in_at IS NOT NULL AND checked_out_at IS NULL`, `(booking_number)` | `Location` stringa → `resource_id uuid` FK `catalog_map_spots` + `spot_label_snapshot`; `packageId` → `price_list_id uuid`; `currentRow` → `sector_id uuid` + `sector_label_snapshot`; `edit_price_data` (griglia 1–90 gg) **non** persistita (ricalcolo motore prezzo); `checkIn/Out` → `checked_in_at`/`checked_out_at` **per riga**; `lineItemId` → `fiskaly_line_item_id` |
| **[booking_line_additional_items](booking/booking_line_additional_items.md)** | `bookings.additionalList[]` / `certainAddItemData[]` (+ [`additional_items_counts_details`](../../analisi-pre/db/additional_items_counts_details.md)) | 1 articolo aggiuntivo su una riga (o sulla prenotazione), con `date_scope` | no | `(booking_line_id)`, `(reservation_id)`, `(catalog_item_id)` | `name`/`it` stringa (refusi, `\t`) → FK `catalog_inventory_items`/`catalog_pricing_price_lists` + `name_snapshot`; `value` con `0`/negativi → `CHECK (quantity > 0)`; consumo per data → `booking_resource_consumptions` |
| **[booking_discounts](booking/booking_discounts.md)** | `bookings.booking_discount` / `additionalAutoVoucher[]` / `service_reservation.discountData` (JSON) | **snapshot** del voucher applicato + importo scontato | no | `(reservation_id)`, `(discount_id)`, `(property_id, upper(code_snapshot))` | elemento `discounts.items[]` copiato nel JSON → colonne tipizzate + `discount_id uuid` (C2); `type`→enum condiviso; `voucherType` 3 valori mantenuti (`without_custom`, [D-16]); `check*Flag` → `applied_channel` enum + `applies_to_categories enum[]`; contatore utilizzi **atomico su C2** (`consumed_usage` qui per audit); `currentId` scartato |
| **[booking_guests](booking/booking_guests.md)** | `bookings.guest_data[]` / `guest_ids[]` (JSON) | 1 ospite (non intestatario) di una prenotazione | no | `(reservation_id, customer_id)` unico parz., `(customer_id)` | 2 array JSON → 1 tabella; FK viva `customer_id` (C5) + `full_name_snapshot`; ricerca `JSON_CONTAINS` → indice |
| **[booking_participants](booking/booking_participants.md)** | [`bookings_requested_details`](../../analisi-pre/db/bookings_requested_details.md) | (prenotazione × account consumer che l'ha aperta col `reservation_code`) → visibilità in area personale | no | `(reservation_id, user_id)` unico parz., `(user_id)` | nome legacy fuorviante ("richieste"); `findOrCreate` senza vincolo → `UNIQUE`; `booking_request_by` → `user_id uuid` (C5); `role` esplicito (`viewer`) |
| **[booking_number_sequences](booking/booking_number_sequences.md)** | [`booking_logs`](../../analisi-pre/db/booking_logs.md) | contatore progressivo numero prenotazione — **1 riga per stabilimento** | no | `(property_id)` UNIQUE | nome legacy fuorviante (**non** un audit log); ~232k righe (bug `create` ridondante) → 1 per property; concatenazione stringhe + `parseInt+1` (collisione garantita) → `number_prefix` + `last_sequence bigint` zero-padded, `SELECT … FOR UPDATE` |
| **[booking_quick_action_dates](booking/booking_quick_action_dates.md)** | `booking_number_informations.quick_actions_data` (array JSON `DD-MM-YYYY`) | 1 data "azione rapida" del planner per prenotazione | no | `(reservation_id, action_date)` unico parz., `(property_id, action_date)` | **unico pezzo di C1** di `booking_number_informations` (il resto → C1 testata / C3 / C4); array → righe `date` |
| **[booking_resource_consumptions](booking/booking_resource_consumptions.md)** | [`booking_items_counts_details`](../../analisi-pre/db/booking_items_counts_details.md) + [`additional_items_counts_details`](../../analisi-pre/db/additional_items_counts_details.md) + [`services_booking_counts`](../../analisi-pre/db/services_booking_counts.md) + [`hourly_service_booking_management`](../../analisi-pre/db/hourly_service_booking_management.md) (conteggio) + [`location_reservations`](../../analisi-pre/db/location_reservations.md) + [`booking_areas`](../../analisi-pre/db/booking_areas.md) + parte "consumo" di [`booking_absence_details`](../../analisi-pre/db/booking_absence_details.md)/[`sharing_details`](../../analisi-pre/db/sharing_details.md) | **1 riga per (risorsa × giorno occupato × sorgente)** — la disponibilità si legge da qui | no | `(property_id, resource_id, consumption_date) WHERE state <> 'released'`, `(property_id, consumption_date, state)`, `(property_id, resource_type, consumption_date)`, `(source_kind, source_id)`, `(held_until) WHERE state='held'`, **EXCLUDE USING gist** (no-overlap postazione/giorno/fascia) — ⚠️ progettato, **non presente a DB** (744 coppie lo violano; vedi scheda) | [`docs/03 §3.8/§3.9`](../../progettazione/03-gestione-dati.md); `booking_element` stringa → `resource_type enum` + `resource_id uuid`; `status` (= `payment_status`) → `state` dedicato (`held`/`confirmed`/`released`); `booking_date` (un solo giorno, `+1` fuso) → 1 riga **per giorno**; `quantity` `0`/negativi → `CHECK > 0`; `location_reservations` senza scadenza → `held_until` + job |
| **[booking_resource_availability_daily](booking/booking_resource_availability_daily.md)** | *(nuova)* — nel legacy `GROUP BY` a runtime + confronto per stringa con `warehouses` | aggregato `booked_qty`/`total_qty`/`available_qty` per (risorsa/tipo × giorno) | no | `(property_id, resource_type, resource_id, consumption_date, booking_period)` UNIQUE, `WHERE available_qty > 0`, `(sector_id, consumption_date)` | proiezione mantenuta **dal DB** (trigger `UPSERT` su consumptions) + job di riconciliazione; `total_qty` da C2 a evento; overlay disponibilità richiesto da C2 ([`_config-snapshot-proposta.md` §6](_config-snapshot-proposta.md)) |
| **[booking_agreement_lines](booking/booking_agreement_lines.md)** | [`hotel_payments`](../../analisi-pre/db/hotel_payments.md) (fetta C1) | (riga prenotazione × voce listino/voucher di una convenzione) + snapshot prezzo | **sì** (C3 importo/stato) | `public_id` unico, `(reservation_id)`, `(booking_line_id)`, `(agreement_partner_id)`, `(property_id, agreement_price_list_id, customer_id) WHERE list_type='voucher'` | nomenclatura allineata a C2 (`hotel*`→`agreement*`); `amount`/`paymentStatus`/`paymentMethod` → **C3**; `mainPrices`/`detailPrices` (doppio-encoded, decine di KB) → `applied_price` + FK listino C2; `priceCalculation` → lookup condivisa; `locationName` stringa → `booking_line_id`; `hotelId`/`hotelPriceListId` → `agreement_partner_id`/`agreement_price_list_id`; `room`/`voucherNumber`/`date`/`notes` (`NULL` in prod) non migrate; 71% righe `isDeleted` (destroy+recreate) → solo righe vive |

## Sottoarea `service`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[booking_service_lines](booking/booking_service_lines.md)** | `bookings.servicesData` / `service_reservation.servicesData` (JSON) + [`services_booking_counts`](../../analisi-pre/db/services_booking_counts.md) (riga vendita) | 1 servizio venduto (bar/normale/orario) su una prenotazione | **sì** (C3/C4) | `public_id` unico, `(reservation_id)`, `(property_id, kind)`, `(sale_item_id)`, `WHERE is_order_filled = false` | blob con immagini `data:` base64 e costanti UI → `sale_item_id uuid` + scalari; `service_type` `"Bar Service"` (drift) → enum `bar`/`normal`/`hourly`; `count`/`click` → `quantity` (`CHECK > 0`, niente 98% righe a 0); `summaryServicesData` → `is_paid_with_wallet` (movimento wallet = C3); conteggio per data → `booking_resource_consumptions` |
| **[booking_service_time_slots](booking/booking_service_time_slots.md)** | [`hourly_service_booking_management`](../../analisi-pre/db/hourly_service_booking_management.md) (parte "slot × giorno") | 1 slot orario prenotato di una riga servizio `hourly` | no | `(service_line_id)`, `(property_id, time_slot_id, service_date)` | `HH:MM` stringa (`:`/`.` misti) → `time`; `booking_date` con ora fittizia → `date`; `hourly_service_name` stringa → `time_slot_id uuid` (C2); update destroy+recreate → soft-delete; occupazione ai fini disponibilità = `booking_resource_consumptions` |

## Sottoarea `hold` (blocco / riserva / rivendita)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[booking_spot_holds](booking/booking_spot_holds.md)** | [`location_reservations`](../../analisi-pre/db/location_reservations.md) + [`hold_locations`](../../analisi-pre/db/hold_locations.md) | blocco di una postazione **senza prenotazione** (operativo o di checkout), per un intervallo | no | `(property_id, resource_id, start_date, end_date) WHERE hold_status='active'`, `(held_until) WHERE hold_status='active'`, `(property_id) WHERE hold_status='active'` | 1 riga per giorno → **1 per intervallo**; `freeLocationReservation` = `DELETE` senza filtro property → soft-release scoped; `locationId` stringa + `locationData` (35 KB, array-spazzatura) → `resource_id uuid`, metadati eliminati; `hold_locations` (0 righe) → nulla da migrare; `held_until` calcolato **server-side** + job scadenza; `propertyId` `99999` / `0000-00-00` bonificati |
| **[booking_resale_offers](booking/booking_resale_offers.md)** | [`booking_absence_details`](../../analisi-pre/db/booking_absence_details.md) + parte "offerta" di [`sharing_details`](../../analisi-pre/db/sharing_details.md) (+ JSON `bookings.booking_absence_dates`) | (riga prenotazione × postazione × giorno ceduto) → torna disponibile | no | `(property_id, resource_id, offer_date) WHERE offer_state='available'`, `(booking_line_id)`, `(taken_by_reservation_id)`, `WHERE offer_state='reassigned' AND credit_settled=false AND origin='sharing'`, **EXCLUDE** `(booking_line_id, offer_date, fascia)` (dal 2026-09-30) | due tabelle + 1 JSON per lo stesso concetto → una, con `origin` (`absence`/`sharing`/`absence_as_sharing`); `status` (2=disponibile, controintuitivo) + `isBooked` (33 righe reali disallineate) → `offer_state` unico; `location` stringa → `resource_id uuid`; `booked_by` → `taken_by_reservation_id`; `email_sent` → **C6**; `credit_value`/`number_of_credit` → ledger; `booking_absences` (dismessa 2024) e `sharings` (morta) **non migrate** |
| **[booking_resale_credit_ledger](booking/booking_resale_credit_ledger.md)** | campi `number_of_credit`/`credit_value` di [`sharing_details`](../../analisi-pre/db/sharing_details.md) + cron `sharingCreditCron` (scriveva su `users`) | 1 movimento di credito "sharing" del titolare (append-only) | no | `(property_id, customer_id, occurred_at DESC)`, `(resale_offer_id)`, `(resale_offer_id) WHERE entry_type='earned'` UNIQUE | ledger **append-only**; accredito **incrementale** (il legacy sovrascriveva `users.total_number_of_credit`, ancorato all'**email**); `credit_value` int da `replace(/€/g,"")` → `amount numeric(12,2)`; verso C3 (borsellino) evento `ResaleCreditEarned`; `totalCreditsEarned` su `users` → C3 |
| **[booking_resale_settings](booking/booking_resale_settings.md)** | 7 campi "sharing" di [`configurations`](../../analisi-pre/db/configurations.md) (`sharing_enable`, `sharing_seasonal_workers`, `credit_perday_resold`, `credit_value`, `disable_both_options`, `enable_sharing_on_all_article`, `plan_absence_as_sharing`) | config feature rivendita/sharing — **1 riga per stabilimento** | no | `(property_id)` UNIQUE (mancava) | campi cross-dominio: vivono in `configurations` (C2) "per accidente" → C1 ([`docs/03 §6`](../../progettazione/03-gestione-dati.md)); `credit_value` testo sporco (`'5€'`/`'5%'`/`'30% del giornaliero'`) → `credit_calc` enum + `credit_amount numeric`; `plan_absence_as_sharing` → `mode` (`absence`/`sharing`); `sharing_seasonal_workers` + `enable_sharing_on_all_article` → `applies_to enum[]` (`seasonal_workers`/`all_articles`); `disable_both_options` migrato as-is ("da rivedere") |

## Sottoarea `group`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[booking_groups](booking/booking_groups.md)** | [`booking_groups`](../../analisi-pre/db/booking_groups.md) (model `booking_group`) | raggruppamento manuale di prenotazioni distinte sotto un'etichetta | no | `(property_id, lower(name))` unico parz., `(property_id)` | `name` unico **globale** → unico **per stabilimento** (bug multi-tenant); `status` int (mai usato) → `deleted_at`; `groupBookings` array → giunzione |
| **[booking_group_members](booking/booking_group_members.md)** | array `booking_groups.groupBookings[]` | (gruppo × prenotazione) | no | `(reservation_id)` unico parz., `(group_id)` | `UNIQUE(reservation_id)` sostituisce la validazione O(N·M) in memoria su tutti i gruppi; `bookings.id` (riga) → `reservation_id` (prenotazione); id orfani scartati |

### Tabelle legacy C1 **non migrate**

| Legacy | Perché |
|---|---|
| [`booking_absences`](../../analisi-pre/db/booking_absences.md) | versione minimale precedente di "assenza pianificata", dismessa (nessun dato dopo il 2024-07-30); solo codice commentato |
| `bookin-absences` (con trattino), `sharings`, `location_reservations_old`, `booking_items_counts`, `booking_items_counts_details_news`, gli `*_archive_03072026`, `bookings_old`, `booking` (singolare) | tabelle legacy/archivio/scratch — vedi [nota in `../../analisi-pre/db/_feature-tabelle.md`](../../analisi-pre/db/_feature-tabelle.md) |
| [`parent_bookings`](../../analisi-pre/db/parent_bookings.md) | dead code (0 righe, import errato `db.parentbooking`); il "pagamento multi-prenotazione" va progettato ex novo se serve (C3) |
| [`booking_areas`](../../analisi-pre/db/booking_areas.md) | writer `createBookingArea` mai invocato (0 righe); la feature "ad area" vive nel JSON `bookings.areaDetails` → assorbita da `booking_lines` (`is_area`) + `booking_resource_consumptions` (`resource_type = area_seat`) |
| [`hold_locations`](../../analisi-pre/db/hold_locations.md) | feature incompleta (0 righe, nessun consumer); il concetto → `booking_spot_holds` (`hold_kind = checkout`) + `booking_resource_consumptions` (`state = held`) |

### Campi legacy che **escono** da C1

| Campo legacy | Va in | Perché |
|---|---|---|
| `bookings.booking_payments.{paidAmountData, addPaidAmountData, depositList, getCustExpenseData}` | **C3** `payment` | singoli movimenti di incasso/acconto/spesa cliente (in C1 resta solo il riepilogo in colonna) |
| `bookings.summaryServicesData` | **C3** (wallet) | consumazioni pagate col borsellino (in C1 solo `is_paid_with_wallet` sulla riga servizio) |
| `bookings.isMarketingCampaign` | **C6** | attribuzione campagna + auto-iscrizione contact list |
| `sharing_details.email_sent` | **C6** | esito invio email "credito guadagnato" |
| `sharing_details.{number_of_credit, credit_value}` (come **valore spendibile**) | **C3** | il credito maturato è un movimento wallet (in C1 resta il ledger di *come* è maturato, evento verso C3) |
| `booking_number_informations.{details_payments_new, details_payments}` | **C4** `fiskaly` | righe fiscali (input scontrino elettronico) |
| `booking_number_informations.ofsale_payments` / `teamsystem_payment` | **C3** | addebiti POS OfSale / TeamSystem |
| `online_transaction_details`, `refund_transaction_details`, `transactions_payments`, `card_transactions`, `bookings_send_payment_summaries` | **C3** (+ C6 per il canale di invio del link) | gateway/incassi/rimborsi/borsellino/link Stripe |
| `booking_receipts`, `receipt_serial_numbers`, `printer_configs` | **C4bis** | stampa fiscale su hardware RT |
| `online_reservations` | **C2** | config del canale "prenotazione online" (già in `catalog_property_online_booking_settings`) |
| `hotel_reports` | **C2** (+ C6 per l'invio PDF) | artefatto della gestione convenzioni. 🔄 **2026-10-02**: diventa la tabella nuova `catalog_agreement_reports` (sottoarea `agreement`), con il PDF archiviato in `notification.media_assets` `kind = 'rendered_pdf'`. Vedi [`catalog_agreement_reports.md`](catalog/catalog_agreement_reports.md) |

---
---

# Dominio C3 — `payment`

Modulo **`payment`** ([`docs/03-gestione-dati.md` §3.1bis / §2.4ter / §4](../../progettazione/03-gestione-dati.md),
[`docs/01-criticita-e-indicazioni-backend.md` §6](../../progettazione/01-criticita-e-indicazioni-backend.md)),
sei sottoaree:

- **`gateway`** — configurazione del gateway per stabilimento: connected account Stripe, tasso di
  commissione; profili Stripe Customer del cliente (carte salvate).
- **`transaction`** — transazioni di pagamento **online** (PaymentIntent Stripe, hold, application fee)
  e **rimborsi**.
- **`request`** — link di pagamento Stripe ("riepilogo pagamento") inviati al cliente. *(sottoarea non
  in §3.1bis: da recepire — vedi nota nella scheda.)*
- **`ledger`** — libro incassi: 1 riga per movimento (acconto/pagamento/spesa/rimborso/rettifica) con
  imponibile + IVA; scomposizione per articolo per l'export contabile.
- **`wallet`** — borsellino/tessera prepagata del cliente (ricariche/consumi) e crediti "sharing" da
  cessione posto.
- **`pos`** — addebiti importati dai POS esterni (OfSale, TeamSystem / Cassa in Cloud).

*(Il perimetro **esclude** ciò che nel legacy stava "sotto pagamenti/fiscale" ma è di un altro modulo —
[`docs/03 §2.4ter / §4`](../../progettazione/03-gestione-dati.md): lo **scontrino elettronico** e le righe fiscali
(`fiskaly_*`, `booking_number_informations.details_payments*`) → **C4** `fiskaly`; la **stampa su RT
hardware** (`printer_configs`, `booking_receipts`, `receipt_serial_numbers`) → **C4bis**; il **canale ed
esito di invio** del link/report (`method_flag`, `email_sent`) → **C6** `notification`; le **credenziali
API partner** (`keysecret`) → **INFRA** (secret manager + [`infra_integration_api_clients`](infra/infra_integration_api_clients.md)), pur servendo i POS di C3; il
**riepilogo economico** della prenotazione (totale/pagato/residuo) resta **C1** come colonne di testata,
qui vivono solo i **movimenti**.)*

## Sottoarea `gateway`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[payment_gateway_connected_accounts](payment/payment_gateway_connected_accounts.md)** | [`stripe_configrations`](../../analisi-pre/db/stripe_configrations.md) (44 righe) | connected account Stripe del venditore — **1 per stabilimento** | no | `(property_id)` unico parz., `(stripe_account_id)` unico parz., `(onboarding_state)` parz. | refuso `configrations`+`camelCase` → `snake_case`; `UNIQUE(property_id)` (mancava); bug `update where id=userId` → upsert per property; `+ onboarding_state`/`charges_enabled`/`payouts_enabled` da webhook `account.updated`; `destroy` → soft-delete + evento verso C2 |
| **[payment_gateway_commission_settings](payment/payment_gateway_commission_settings.md)** | JSON [`properties.commissionAmount`](../../analisi-pre/db/properties.md) (`{onlineCommission, iframeCommission}`) + liste hardcoded `COMMISSION_FREE_PROPERTY_IDS` / `CONNECTED_AND_PLATFORM_ACCOUNT_PROPERTIES` | tasso di commissione sugli incassi online — **1 per stabilimento** | no | `(property_id)` UNIQUE | campo cross-dominio **C2 → C3** ([`docs/03 §6`](../../progettazione/03-gestione-dati.md)); JSON → `online_rate`/`iframe_rate numeric`; `+ 0.5` cablato → `fixed_fee_amount`; esenzioni hardcoded → `exemption` **enum** (`none`/`iframe_only`/`always`); calcolo **server-side** (era solo client) |
| **[payment_customer_gateway_profiles](payment/payment_customer_gateway_profiles.md)** | [`user_stripe_card_details`](../../analisi-pre/db/user_stripe_card_details.md) (0 righe, **da rifare**) + [`users.stripeCustomerId`](../../analisi-pre/db/users.md) | mappatura `cliente → Stripe Customer` per connected account | no | `(customer_public_id, connected_account_id)` unico parz., `(stripe_customer_id)` UNIQUE, `(connected_account_id)` | model≠schema (`propertyId` inesistente) → colonna reale; nome fuorviante `stripeCustomerCardId` (è un *Customer*) → `stripe_customer_id`; `token` colonna morta non migrata; `users.stripeCustomerId` (conteneva anche `acct_`) → solo `cus_…`; mai PAN/CVC; **eliminabile** se si delega tutto a Stripe ([D-34](_domande-committente.md)) |

## Sottoarea `transaction`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[payment_transaction_charges](payment/payment_transaction_charges.md)** | [`online_transaction_details`](../../analisi-pre/db/online_transaction_details.md) (38.240 righe; refuso "Taransaction") | 1 PaymentIntent Stripe (o "paga in struttura") | **sì** (C4 autoreceipt, URL gestionale) | `public_id` UNIQUE, `(stripe_payment_intent_id)` unico parz., `(reservation_public_id)`, `(property_id, created_at DESC)`, `WHERE status IN ('pending','authorized')`, `(connected_account_id)` | importi `varchar` → `numeric(12,2)` ([`docs/03 §3.7`](../../progettazione/03-gestione-dati.md)); `status` passthrough casing misto → enum interno **+** `stripe_status` grezzo; `booking_type` → `subject_kind` (allineato `reservation_kind` C1); `payment_type` → **enum** `pay_now`/`advance_balance`/`pay_at_property`; `isIframe` → `sales_channel` enum; `pi_`/`ch_` distinti (era bug refund); `stripe_connected_id` → `stripe_account_id_snapshot` (id `acct_` congelato accanto alla FK viva `connected_account_id`, regola C1-7); `sync({alter})` all'avvio → migrazioni; indici su `stripe_transaction_id`/`booking_id` (erano full scan, [`docs/01 §6`](../../progettazione/01-criticita-e-indicazioni-backend.md)); idempotenza webhook su `event.id` |
| **[payment_transaction_refunds](payment/payment_transaction_refunds.md)** | [`refund_transaction_details`](../../analisi-pre/db/refund_transaction_details.md) (0 righe — rimborsi non tracciati; `refundPayment` duplicata e rotta) | 1 rimborso Stripe su una transazione | **sì** | `public_id` UNIQUE, `(stripe_refund_id)` unico parz., `(charge_id)`, `(property_id, occurred_at DESC)`, `WHERE status='pending'` | funzione duplicata+rotta (`req.body.bookingid`, `pi_` dove serve `ch_`, `refundData` undefined) → un solo percorso; `amount` centesimi → `numeric` euro; **+ rimborso parziale** (`amount`/`original_amount`/`reason`); `+ status` enum + `failure_reason`; `charge_id` FK interna risolve booking **e** service reservation (testata unica C1) |

## Sottoarea `request`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[payment_request_links](payment/payment_request_links.md)** | fetta **C3** di [`bookings_send_payment_summaries`](../../analisi-pre/db/bookings_send_payment_summaries.md) (1.392 righe) | 1 link di pagamento Stripe inviato al cliente | **sì** (usato nell'URL `payment-summary-success/{id}`) | `public_id` UNIQUE, `(stripe_payment_link_id)` unico parz., `(reservation_public_id)`, `(property_id, status)`, `WHERE status='pending'` su `expires_at` | `amount` euro interi → `numeric`; stato spalmato su 3 campi (`payment_status`/`cancel_payment_flag`/`end_date_time`) → un solo `status` enum `pending→paid│canceled│expired│failed`; `method_flag`/`email_sent` → **C6**; `booking_cancel_flag` → `cancel_reservation_on_expiry` (config, non stato); `isCallingFromBookingDetail` → `origin` enum; `+ charge_id` (il legacy non collegava il link alla transazione) |

## Sottoarea `ledger`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[payment_ledger_entries](payment/payment_ledger_entries.md)** | [`transactions_payments`](../../analisi-pre/db/transactions_payments.md) (2.539 righe, prototipo) + movimenti dentro i JSON [`bookings.booking_payments`](../../analisi-pre/db/bookings.md) (`paidAmountData`/`addPaidAmountData`/`depositList`/`getCustExpenseData`) e [`service_reservation.payments`](../../analisi-pre/db/service_reservation.md) + fetta economica di [`hotel_payments`](../../analisi-pre/db/hotel_payments.md) (`amount`/`paymentStatus`/`paymentMethod`) | 1 movimento di incasso/acconto/spesa/rimborso/rettifica di una prenotazione | **sì** (C4 righe `payments[]` scontrino; export contabile) | `public_id` UNIQUE, `(reservation_public_id, occurred_at)`, `(property_id, occurred_at DESC)`, `(property_id, payment_mode, occurred_at)`, `(source, source_ref_id)`, `WHERE agreement_line_public_id IS NOT NULL` | è `payment_transactions` di [`docs/03 §4`](../../progettazione/03-gestione-dati.md); **append-only** (era `destroy`+`bulkCreate`); `float` con outlier (`~2,1e9`) → `numeric` bonificato; IVA 22% cablata → `vat_rate` esplicito; `payment_mode` drift (`NULL` 67%/`""`/`"Bank Transfer"`) → **enum** condiviso C1; `type` con 3 codifiche → `entry_type` enum unico; scrittura **in transazione** + outbox (era best-effort `.catch`); snapshot cliente + FK viva C5; `hotel_payments` solo righe con `amount>0` |
| **[payment_ledger_entry_lines](payment/payment_ledger_entry_lines.md)** | campi per-articolo di [`transactions_payments`](../../analisi-pre/db/transactions_payments.md) (`itemName`/`itemQuantity`/`itemNetAmount`/`itemVatAmount`) | quota di un movimento attribuita a un articolo | no | `(entry_id)`, `WHERE reservation_line_public_id IS NOT NULL`, `(vat_rate)` | per l'**export contabile** verso gestionali terzi; popolata solo per property in allow-list (era env `API_TRANSACTIONS_PROPERTYID_CSV`); segue l'append-only della testata; `itemName` → `description_snapshot` (nome congelato accanto alla FK viva `reservation_line_public_id`, regola C1-7); `is_allocated` esplicita la ripartizione proporzionale come stima ([D-35](_domande-committente.md)) |

## Sottoarea `wallet`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[payment_wallet_accounts](payment/payment_wallet_accounts.md)** | *(nuova)* — il "conto tessera" era implicito in [`card_transactions`](../../analisi-pre/db/card_transactions.md) (`GROUP BY identifier`) + saldi denormalizzati su [`users`](../../analisi-pre/db/users.md) (`total_number_of_credit`/`total_credit_value`/`totalCreditsEarned`) | conto prepagato di un cliente presso uno stabilimento — **1 per (property × customer × tipo)** | **sì** (ref. da C1: riga servizio pagata col borsellino) | `public_id` UNIQUE, `(property_id, customer_id, wallet_kind)` unico parz., `(property_id, card_number)` unico parz., `(customer_id)`, `WHERE balance <> 0` | `identifier` stringa libera dal client (RNG insicuro, `""`/`"test"`, nessun `UNIQUE`) → `card_number` server-side con `UNIQUE`; saldo `SUM()` a ogni lettura → `balance` materializzato + job ([`docs/01 §6`](../../progettazione/01-criticita-e-indicazioni-backend.md)); `wallet_kind` **enum** (`prepaid_card`/`resale_credit`); crediti sharing da `users` per email/assegnazione → riga `resale_credit` per `(property, customer)` incrementale; `IsDeleted`/`isDeleted` → `deleted_at` |
| **[payment_wallet_transactions](payment/payment_wallet_transactions.md)** | [`card_transactions`](../../analisi-pre/db/card_transactions.md) (708 righe) | 1 movimento del borsellino (ricarica/consumo) | **sì** (C1 riga servizio; C4 pagamento `VOUCHER`/`OUTSTANDING`) | `public_id` UNIQUE, `(wallet_id, occurred_at DESC)`, `WHERE reservation_public_id IS NOT NULL`, `(property_id, occurred_at DESC)` | `transaction_type` `1`/`2` con debiti come `type=1 amount<0` → `movement_type` enum + `amount` sempre positivo ([`docs/03 §2.4bis`](../../progettazione/03-gestione-dati.md)); `debit_details` JSON → figlia; `booking_id 0` → `NULL`; **append-only** (correzione = `reversal`); `+ balance_after` (running balance); `+ source` enum (`top_up`/`service_consumption`/`resale_credit_earned`/`reservation_settlement`/…) |
| **[payment_wallet_transaction_items](payment/payment_wallet_transaction_items.md)** | JSON [`card_transactions.debit_details`](../../analisi-pre/db/card_transactions.md) | 1 articolo consumato in un movimento di debito | no | `(transaction_id)`, `WHERE sale_item_public_id IS NOT NULL` | blob con dati sporchi (`price "55.656595"`, `serviceCount 0`/`22`) → colonne tipizzate + bonifica; `debit_details.name` → `description_snapshot` (accanto alla FK viva `sale_item_public_id`, regola C1-7); `selectedVariantData` array → `variants_description` (testo congelato, **senza** suffisso `_snapshot`: nessuna entità "variante" né FK viva) + `variants_surcharge numeric` |
| **[payment_webhook_events](payment/payment_webhook_events.md)** 🆕 | — (nessuna legacy) | registro degli eventi webhook dei gateway, con chiave di idempotenza | no | `(provider, external_event_id)` UNIQUE, `(received_at)` parz. sui pendenti, `(property_id, received_at)` parz., BRIN `(created_at)` | **nuova** ([`docs/05-ottimizzazioni-db.md` §6](../../progettazione/05-ottimizzazioni-db.md)): nel legacy l'idempotenza era un `SELECT`+`INSERT` applicativo, con una finestra di corsa fra due webhook concorrenti — Stripe ritenta, quindi non era teorica. Ora è un `UNIQUE` a schema + `ON CONFLICT DO NOTHING`. Registra anche gli eventi non gestiti (`ignored`), che nel legacy sparivano con un 200 |

## Sottoarea `pos`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[payment_pos_charges](payment/payment_pos_charges.md)** | [`teamsystem_expenses`](../../analisi-pre/db/teamsystem_expenses.md) (0 righe) + fetta **C3** di [`booking_number_informations.ofsale_payments`](../../analisi-pre/db/booking_number_informations.md) / `teamsystem_payment` (`NULL` in prod) | 1 scontrino/addebito da un POS esterno associato a una prenotazione | **sì** (id restituito al POS per l'idempotenza) | `public_id` UNIQUE, `(pos_system, property_id, external_ref)` UNIQUE, `(reservation_public_id)`, `(property_id, charged_at DESC)`, `(booking_number)` | `idRef bigint` vs UUID atteso (causa tabella vuota) → `external_ref varchar` + `UNIQUE`; `payload` JSON → colonne + figlia; `amount` triplo (colonna + `payload.amount` + `Σ data`) → `total_amount` con vincolo su `Σ righe`; `fiscalized` colonna morta non migrata (stato fiscale = evento/consultazione C4); `payload.app` fisso → `pos_system` enum; `registrationId` "rappresentativa" del gruppo → `reservation_public_id` diretto |
| **[payment_pos_charge_items](payment/payment_pos_charge_items.md)** | array [`teamsystem_expenses.payload.data[]`](../../analisi-pre/db/teamsystem_expenses.md) + `saleItems[]` dei documenti OfSale | 1 riga prodotto di uno scontrino POS | no | `(charge_id)`, `WHERE sale_item_public_id IS NOT NULL`, `(vat_rate)` | array JSON → righe; `vat` intero (TeamSystem) vs `vatRate` stringa (OfSale) → `numeric(5,2)` uniforme; `price` totale-riga vs unitario normalizzati in `unit_price` + `line_total`; `+ net_amount`/`vat_amount` scorporati in acquisizione; nome prodotto → `description_snapshot` + `sale_item_public_id` nullable (mappatura SKU POS ↔ catalogo, assente nel legacy) |

### Tabelle/campi legacy che **non entrano** in C3

| Legacy | Dove va | Perché |
|---|---|---|
| [`keysecret`](../../analisi-pre/db/keysecret.md) | **INFRA / Integrazioni** | credenziali API partner (`apiKey`/`secret` in chiaro) → secret manager + tabella leggera [`infra_integration_api_clients`](infra/infra_integration_api_clients.md) (senza segreto); i 3 consumatori (OfSale/TeamSystem/Transactions) gravitano su C3 ma la tabella non è di dominio C3 ([`docs/03 §2.4ter`](../../progettazione/03-gestione-dati.md), [`docs/80-retention.md` §"Segreti"](../../progettazione/80-retention.md)) |
| `fiskaly_*`, `booking_number_informations.details_payments*` | **C4** `fiskaly` | scontrino elettronico SIGN IT + righe fiscali |
| `printer_configs`, `booking_receipts`, `receipt_serial_numbers` | **C4bis** | stampa fiscale su hardware RT (adapter Epson/RCH lato client) — si mantiene come componente separato (decisione di business) |
| [`parent_bookings`](../../analisi-pre/db/parent_bookings.md) | *(non migrata)* | dead code (0 righe); il "pagamento multi-prenotazione", se servirà, si progetta ex novo in C3 ([D-37](_domande-committente.md)) |
| `bookings_send_payment_summaries.{method_flag, email_sent}` | **C6** `notification` | canale ed esito di invio del link (log messaggio) |
| `online_reservations.{cvc, cardHodler, cardNumber, expiry}` | **eliminati** | PAN/CVC in chiaro — violazione PCI-DSS, mai a DB ([`docs/11-modulo-privacy.md`](../../progettazione/11-modulo-privacy.md)) |
| `revenue_notifications` | **C2** `catalog` (`pricing`) | nonostante il nome, è configurazione Revenue Management, non pagamenti ([`catalog_pricing_revenue_settings`](catalog/catalog_pricing_revenue_settings.md)) |

### Campi legacy che **escono** da C3

| Campo legacy | Va in | Perché |
|---|---|---|
| `bookings_send_payment_summaries.method_flag` / `email_sent` | **C6** | canale (email/WhatsApp) ed esito dell'invio del link di pagamento |
| `card_transactions.sendCardDetails` (→ `email_logs`) | **C6** | invio dell'estratto tessera al cliente |
| `stripe_configrations` — nessun segreto in tabella | **INFRA** | l'OAuth Connect non conserva refresh token qui (si usa la piattaforma) |

---
---
---

# Dominio C4 — `fiskaly`

Modulo **`fiskaly`** ([`docs/03-gestione-dati.md` §2.4ter, §3.1bis, §4](../../progettazione/03-gestione-dati.md)),
**senza sottoaree** (dominio piccolo — schema `fiskaly`, forma flat `fiskaly_*`). Scontrino
**fiscale elettronico** italiano via API Fiskaly **SIGN IT** (documento commerciale online):
account fiscale dello stabilimento presso Fiskaly + entità AdE, registratori di cassa, documenti
emessi (fiscali **e** pre-conto in **testata unica**), righe e pagamenti fiscali, log di dialogo con
SIGN IT, sequenze numeriche giornaliere, lookup dei codici IVA, PDF.

*(Perimetro: **non** C3 `payment` — Stripe/incassi; **non** C4bis `fiscalprint` — stampa su hardware
RT. Verso C1/C3/C5 si copiano solo `public_id` / `booking_number` / importi, **mai** FK cross-modulo:
uno scontrino è un **fatto fiscale storico** — snapshot immutabile, [`docs/03 §3.6 punto 2`](../../progettazione/03-gestione-dati.md).)*

## Nucleo (nessuna sottoarea)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[fiskaly_accounts](fiskaly/fiskaly_accounts.md)** | [`fiskaly_account`](../../analisi-pre/db/fiskaly_account.md) (7 righe) **+** i 3 flag `properties.fiskaly*` | account fiscale dello stabilimento presso Fiskaly SIGN IT + stato di configurazione fiscale — **1 per property** | **sì** (GDPR: CF persona fisica Fisconline + URL pannello) | `public_id`, `(property_id)` unico parz., `(organization_id)`/`(subject_id)`/`(taxpayer_id)`/`(location_id)` unici parz., `(fiscal_setup_level)` parz. | **segreti fuori dalla tabella** → `*_secret_ref` (ARN secret manager), zero materiale (C4-2); `accept_terms` + `fiskalyEnabled`/`fiskalyAutoreceipt`/`fiskalyFlag` (già incoerenti nei dati) → consolidati qui (C4-3): `terms_accepted_at`+`is_enabled`+`autoreceipt_enabled` (catena `CHECK`) + `fiscal_setup_level` enum (`fiskaly_active` **derivato**); `*_state`/`*_mode` `varchar`→enum; `decommissioned_at`→`deleted_at`; `createdBy`→`created_by_user_id uuid` |
| **[fiskaly_cash_registers](fiskaly/fiskaly_cash_registers.md)** | [`fiskaly_cash_register`](../../analisi-pre/db/fiskaly_cash_register.md) (5 righe) | registratore di cassa / *System* `FISCAL_DEVICE` su Fiskaly — **N per account** | no (interno allo schema) | `(fiskaly_system_id)` unico parz., `(account_id) WHERE is_default` unico parz., `(property_id) WHERE commissioned+operative` parz. | `system_state`/`system_mode`/`system_type` `varchar`→enum; `is_software` colonna **autorevole** (era match per stringa su producer/software, fragile); `is_default`/`is_software` booleani **indipendenti**; `+ property_id` (mancava); `deleted_at` già nel legacy |
| **[fiskaly_receipts](fiskaly/fiskaly_receipts.md)** | [`fiskaly_receipt`](../../analisi-pre/db/fiskaly_receipt.md) (2.596) **+** [`fiskaly_non_fiscal_receipt`](../../analisi-pre/db/fiskaly_non_fiscal_receipt.md) (17) | **1 documento commerciale emesso** per una prenotazione — fiscale (firmato) **o** non fiscale (pre-conto); discriminatore `document_kind` | **sì** (C6 invio, C3 auto-scontrino, URL gestionale) | `public_id`, `(record_id)` unico parz., `(e_receipt_uuid)` unico parz., `(property_id, created_at DESC)`, `(reservation_public_id)`, `(booking_number)`, `(property_id, document_kind, record_state)`, `WHERE record_state='completed' AND record_mode='finished'` | **testata unica** (C4-1, come C1-2): `createNonFiscalReceipt` riusa lo stesso builder; figlie condivise. Importi `varchar`→`numeric` (3 valori con semantiche divergenti riconciliati); `record_state` (enum interno) **separato** da `fiskaly_state_raw` (grezzo SIGN IT); `entries`/`payments` JSON→figlie; `paid_deltas` JSON→`paid_allocation` per riga + storno C3; `e_receipt_path` URL→`fiskaly_media_assets`; `+ property_id`, `+ reservation_public_id`; `emailSent`/`whatsappSent`→**C6**; `fiskaly_version` (morta) non migrata; `languageCode` doppia convenzione (`"it"`/`"en"`/`1`/`2`)→`enum fiskaly.language_code` (SharedKernel); snapshot venditore `seller_*_snapshot` + `fiskaly_system_id_snapshot` (§3.6/C1-7); `fiskaly_response`→`jsonb` grezzo (§3.6 punto 5); `deleted_at` esposto ma **non usato** (fatto fiscale, retention 10 anni) |
| **[fiskaly_receipt_lines](fiskaly/fiskaly_receipt_lines.md)** | `fiskaly_receipt.entries` / `fiskaly_non_fiscal_receipt.entries` (array JSON) + parte `paid_deltas.items[]` | 1 voce del documento (pacchetto/servizio/articolo/sovrapprezzo/voucher) + IVA — **congelati** | no | `(receipt_id, line_no)`, `(source_line_public_id)` parz., `(property_id, vat_code_id)` | vocabolari stringa (`SALE`/`RETURN`, `SERVICE`/`GOODS`, `VAT_RATE`/`VAT_EXEMPTION`)→enum; `vat.code`→FK `fiskaly_vat_codes` (lookup); numeri stringa→`numeric`; `entries[].id` (=`lineItemId`)→`source_line_public_id` (C1, `NULL` per voci sintetiche; la correlazione `entries`↔`paid_deltas` è in-memory in migrazione); `description_snapshot` (C1-7); `paid_allocation` ← `paid_deltas.items[].lastPaid` |
| **[fiskaly_receipt_payments](fiskaly/fiskaly_receipt_payments.md)** | `fiskaly_receipt.payments` / `fiskaly_non_fiscal_receipt.payments` (array JSON, **2 strutture diverse**) | 1 forma di pagamento con cui è stato incassato l'importo del documento | no | `(receipt_id, payment_no)`, `(ledger_entry_public_id)` parz., `(property_id, payment_type)` | 2 strutture (fiscale con `details:{}`, non-fiscale piatta) + drift (`"BANK TRANSFER"`)→1 tabella, `payment_type` enum + `label`; `kind`/`concept`→enum condizionati (`CHECK`); `+ ledger_entry_public_id` (C3, mancava); split payment = **più righe** (non `enum[]`) |
| **[fiskaly_receipt_logs](fiskaly/fiskaly_receipt_logs.md)** | [`fiskaly_receipt_log`](../../analisi-pre/db/fiskaly_receipt_log.md) (8.063 righe) | 1 transizione di stato / errore del flusso SIGN IT per uno scontrino — **puro log append-only** | no | `(receipt_id, created_at)`, `(property_id, created_at DESC)`, `WHERE result='ko'` parz. | `state`/`result`/`fiskaly_record_state` `varchar`→enum; `fiskaly_error_message varchar(50)` (troncato)→`text`; `fiskaly_log_messages`→`jsonb` (§3.6 punto 5); `createdBy`→`actor_user_id uuid`; `updatedAt` (ridondante) scartato; **no `deleted_at`** ([§3.5](../../progettazione/03-gestione-dati.md)); retention/partizionamento (crescita illimitata nel legacy) |
| **[fiskaly_receipt_sequences](fiskaly/fiskaly_receipt_sequences.md)** | [`fiskaly_receipt_sequence`](../../analisi-pre/db/fiskaly_receipt_sequence.md) (234 righe) | contatore progressivo giornaliero del numero documento **per cassa** — 1 per `(cash_register, fiscal_date)` | no | `(cash_register_id, fiscal_date)` UNIQUE, `(property_id, fiscal_date DESC)` | **2 implementazioni** con **fusi diversi** (Europe/Rome vs UTC)→**una**, upsert atomico `ON CONFLICT … GREATEST(...)`, giorno **sempre Europe/Rome**; `system_id varchar` FK senza `ON DELETE`→FK interna `cash_register_id`; `+ created_at`/`updated_at` (mancavano); `+ property_id` |
| **[fiskaly_vat_codes](fiskaly/fiskaly_vat_codes.md)** | *(nuova)* — costanti `fiskaly_consts.js` (`FiskalyVatRate`/`FiskalyVatExemption`), mai usate come tipo | 1 codice IVA / esenzione ammesso da SIGN IT (`STANDARD`/`REDUCED_1..3`/`NOT_SUBJECT`/`CAUSE_1..4`) | no | `code` UNIQUE, `(kind, sort_order) WHERE is_active` parz. | **lookup** (cresce senza deploy, porta aliquota + Natura N1-N6); FK reale da `fiskaly_receipt_lines`; etichette da `.resx` (code-bound); seed 10 righe; aliquota **congelata** comunque sulla riga scontrino |
| **[fiskaly_media_assets](fiskaly/fiskaly_media_assets.md)** | *(nuova)* — `fiskaly_receipt.e_receipt_path` (`varchar(500)` URL S3 completo) | riferimento a file (PDF scontrini) scomposto in `origin` + `path` | no | `kind` parz., `origin` | una tabella **per modulo** ([§1.4/§3.1quater.A](../../progettazione/03-gestione-dati.md), citata come futura); FK interna da `fiskaly_receipts.pdf_asset_id` |

*Vista: `fiskaly.v_daily_cash_closure` — chiusura cassa giornaliera (per forma di pagamento e per aliquota, `UNION ALL`, data fiscale in `Europe/Rome`); documentata in [`fiskaly_receipts`](fiskaly/fiskaly_receipts.md#vista-derivata-fiskalyv_daily_cash_closure).*

### Campi/tabelle legacy che **escono** da C4

| Legacy | Va in | Perché |
|---|---|---|
| `fiskaly_receipt.emailSent` / `whatsappSent` | **C6** `notification` | canale ed esito invio dello scontrino (il "sent" WhatsApp era la sola *generazione* del link `wa.me`) → righe di `notification.deliveries` |
| `fiskaly_account.subject_credential_key` / `subject_credential_secret` / `taxpayer_fisconline_pin` / `taxpayer_fisconline_password` | **secret manager** (AWS Secrets Manager / SSM) | credenziali che vanno **rimandate in chiaro** a Fiskaly a ogni chiamata → protezione reversibile, non hash; in tabella solo `*_secret_ref` (C4-2) |
| `fiskaly_receipt.fiskaly_version` | **eliminato** | colonna dichiarata nel model ma **mai scritta né letta** |
| `properties.fiskalyEnabled` / `fiskalyAutoreceipt` / `fiskalyFlag` | **C4** `fiskaly_accounts` (da C2) | stato/integrazione fiscale sullo stabilimento — non appartiene a `properties` ([`docs/03 §6`](../../progettazione/03-gestione-dati.md)) |
| `booking_number_informations.details_payments*` | **C4** (input `fiskaly_receipt_lines`) | righe fiscali calcolate dal pre-conto: sono l'**input** delle righe scontrino, non una tabella propria — la testata "sacco" è spezzata per dominio (vedi C1/C3) |

---
---

# Dominio C4bis — `fiscalprint`

Feature a sé **`fiscalprint`** ([`docs/03-gestione-dati.md` §2.4ter, §3.1bis, §4](../../progettazione/03-gestione-dati.md)),
**senza sottoaree** (schema `fiscalprint`, forma flat `fiscalprint_*`). **Stampa fiscale su hardware
RT**: meccanismo legacy **alternativo** — più vecchio — allo scontrino elettronico (C4), cioè la
stampa su registratore di cassa RT fisico via adapter Epson TM/FP, RCH **lato client**
(`front-pre/src/services/printers/`). Solo 3 tabelle legacy. **Si mantiene come componente separato**
(decisione di business del committente, indipendente dall'uso attuale); qui modellata come **target
pulito ridisegnato** (decisione **C4bis-1**), non migrazione 1:1.

*(Perimetro: **non** C3 `payment`, **non** C4 `fiskaly`/SIGN IT. Uso reale concentrato su **3-4
stabilimenti**; nel legacy in gran parte **rotto** — `receipt_number` fermo a `"001"`, `bookingId`
sempre `NULL`, colonne `printer_*_object` mai popolate.)*

## Nucleo (nessuna sottoarea)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[fiscalprint_printer_configs](fiscalprint/fiscalprint_printer_configs.md)** | [`printer_configs`](../../analisi-pre/db/printer_configs.md) (58 righe, ~17 property) | 1 stampante (termica / RT) configurata da uno stabilimento — **N per property** | no (interno allo schema) | `(property_id) WHERE deleted_at IS NULL` parz., `(property_id, mode) WHERE is_active` unico parz. *(candidato)* | `isActive`+`isdeleted` scollegati (37/56 righe incoerenti)→`is_active` + `deleted_at`; `model` `varchar` con refusi→**enum** (adapter dedicato, non lookup); `mode`→enum; `port varchar` con IP/nome USB→`connection_kind` enum + `port int`/`usb_device`; `SSL`→`use_tls`; `department_1/2/3` `"22%"` posizionali→figlia `fiscalprint_printer_departments`; `userId`→`created_by_user_id uuid`; FK cross-modulo per valore |
| **[fiscalprint_printer_departments](fiscalprint/fiscalprint_printer_departments.md)** | `printer_configs.department_1` / `_2` / `_3` (3 colonne `varchar` `"22%"`) | 1 reparto fisico del registratore RT mappato su un'aliquota IVA | no | `(printer_id, dept_number)` unico parz., `(printer_id, vat_rate)` unico parz. *(candidato)* + btree per il resolver | 3 reparti fissi→**N righe**; `"22%"` numero-con-simbolo→`vat_rate numeric`; legame reparto↔aliquota **posizionale**→`dept_number` esplicito; resolver = `WHERE printer_id=? AND vat_rate=?` |
| **[fiscalprint_receipts](fiscalprint/fiscalprint_receipts.md)** | [`booking_receipts`](../../analisi-pre/db/booking_receipts.md) (4.476 righe) | 1 scontrino/ricevuta stampato su registratore RT fisico — di fatto un **log di stampe** | **sì** (dettaglio gestionale) | `public_id`, `(property_id, printed_at DESC)`, `(reservation_public_id)` parz., `(printer_id)` | `receipt_number`/`document_no` sempre `"001"` (bug)→`integer` da sequenza ridisegnata; date stringa (`"31/5/2023"`, `"20260727T101700"`)→`printed_at timestamptz`; `receipt_object`→`device_response jsonb` (passthrough dispositivo, §3.6 punto 5) + colonne `rt_*` derivate read-only; `printer_request_object`/`printer_response_object` (sempre `NULL`) **non migrati**; `bookingId` (sempre `NULL`)→`reservation_public_id` nullable (C1); associazioni ORM (tutte commentate)→FK interne reali; `deleted_at` esposto ma **non usato** |
| **[fiscalprint_serial_numbers](fiscalprint/fiscalprint_serial_numbers.md)** | [`receipt_serial_numbers`](../../analisi-pre/db/receipt_serial_numbers.md) (4.476 righe) | contatore progressivo scontrino RT **per stampante (dispositivo fiscale) e giorno** — 1 per `(printer_id, fiscal_date)` | no | `(printer_id, fiscal_date)` UNIQUE, `(property_id, fiscal_date DESC)` | il progressivo RT è **per matricola/stampante** (uno stabilimento con 2 RT ha 2 serie); **non incrementa mai** nel legacy (2 bug: `ReferenceError` + match stringa fragile)→upsert atomico `ON CONFLICT … +1`; `findAll` senza filtro `propertyId`/`printerId` (contatore globale per data)→chiave `(printer_id, fiscal_date)` + `property_id` denorm.; `varchar(255)`→`date`/`integer`; `+ UNIQUE` (mancava); la tabella legacy **non aveva `printerId`** → storico non migrato |

### Tabelle legacy C4bis — nota

Tutte e 3 le tabelle legacy (`printer_configs`, `booking_receipts`, `receipt_serial_numbers`) sono
migrate/ridisegnate. Nessun campo esce verso altri domini. La feature **si mantiene** (decisione di
business del committente): le 4 tabelle `fiscalprint_*` si creano regolarmente.

---
---
---

# Dominio C6 — `notification`

Modulo **`notification`** ([`docs/03-gestione-dati.md` §2.4quater, §2.7, §3.1bis, §6](../../progettazione/03-gestione-dati.md),
[`docs/01-criticita-e-indicazioni-backend.md` §8](../../progettazione/01-criticita-e-indicazioni-backend.md),
[`docs/20-jobs.md`](../../progettazione/20-jobs.md), [`docs/80-retention.md` §3.6](../../progettazione/80-retention.md)),
sottoaree:

- **nucleo (senza sottoarea)** — outbox unico per destinatario; catalogo file; catalogo template
  (trasversale ai canali, [`docs/03 §3.1bis`](../../progettazione/03-gestione-dati.md)).
- **`email`** — configurazione del canale email in uscita per stabilimento.
- **`inapp`** — feed notifiche "campanella" del gestionale + stato-lettura per utente; preferenze di
  canale e token push dello **staff** (campi ex `users.*` che [`docs/03 §6`](../../progettazione/03-gestione-dati.md)
  sposta in C6).
- **`marketing`** — rubrica contatti + consenso, liste, lista di soppressione (opt-out), campagne
  email, automazioni, eventi promozionali, iscrizioni newsletter, lead "richiedi la tua pagina".

*(Perimetro: il **canale WhatsApp** non ha una tabella di config nel legacy (invio via Twilio, provider
in **INFRA**) — WhatsApp è un valore di `channel` nell'outbox e un `template_channel`. Il **centralino**
Telnyx/WhatsApp Business è **C7** `switchboard`. Verso C1/C2/C3/C4/C5 si copiano solo `public_id` /
`booking_number`, **mai** FK cross-modulo: la comunicazione parte da un **evento di dominio** dell'altro
modulo consumato qui, o da un comando che scrive la `Delivery` `queued` in **outbox** nella propria
transazione, [`docs/01 §6/§8`](../../progettazione/01-criticita-e-indicazioni-backend.md).)*

## Nucleo (nessuna sottoarea)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[notification_deliveries](notification/notification_deliveries.md)** | [`email_logs`](../../analisi-pre/db/email_logs.md) (~335 k) + flag `*Sent` sparsi: [`bookings_send_payment_summaries.method_flag`/`email_sent`](../../analisi-pre/db/bookings_send_payment_summaries.md) (C3), [`fiskaly_receipt.emailSent`/`whatsappSent`](../../analisi-pre/db/fiskaly_receipt.md) + `fiskaly_non_fiscal_receipt.*` (C4), [`sharing_details.email_sent`](../../analisi-pre/db/sharing_details.md) (C1), invio di [`hotel_reports`](../../analisi-pre/db/hotel_reports.md) (C2), [`card_transactions.sendCardDetails`](../../analisi-pre/db/card_transactions.md) (C3) + invii per destinatario dei cron marketing | 1 messaggio verso 1 destinatario su 1 canale | **sì** (gestionale, export GDPR) | `public_id` UNIQUE, `(next_attempt_at) WHERE status IN ('queued','sending')`, `(property_id, created_at DESC)`, `(subject_type, subject_public_id)`, `(recipient_customer_public_id, created_at DESC)` parz., `(subject_type, subject_public_id, status) WHERE marketing`, `(idempotency_key)` UNIQUE parz. | **outbox + audit unico** ([`docs/03 §3.1bis`](../../progettazione/03-gestione-dati.md), riga "(nuova)"); `status` `0/1` che mescola coda e log → enum `queued→sending→sent→delivered│failed│bounced│skipped`; **nessun fallimento tracciato** (era tutto in `if(!error)`) → stato terminale + `attempt_count`/`next_attempt_at`/`max_attempts` (Polly); `cronStatus` colonna morta non migrata; `emailType` sovraccarico → `kind` enum + `subject_snapshot`; `propertyId` `NULL` in prod → obbligatorio; flag `*Sent` booleani → riga qui (`channel`/`status`/`sent_at`), sull'origine non resta nulla ([`docs/03 §6`](../../progettazione/03-gestione-dati.md)); `attachments` JSON → `has_attachments` + allegati dell'origine; `body` → `rendered_body` (troncato a 90 gg, [`docs/80 §3.6`](../../progettazione/80-retention.md)); alimentata da coda/worker ([`docs/20-jobs.md`](../../progettazione/20-jobs.md)) |
| **[notification_media_assets](notification/notification_media_assets.md)** | URL S3 in colonna / array URL: [`custom_templates.attachment`](../../analisi-pre/db/custom_templates.md), `marketing_email_campaign`/`marketing_automation.attachments`, [`marketing_event.coverImage`](../../analisi-pre/db/marketing_event.md), [`hotel_reports.path`](../../analisi-pre/db/hotel_reports.md) | riferimento a file scomposto in `origin` + `path` | no | `(property_id, kind) WHERE deleted_at IS NULL`, `(checksum)` parz., `(origin)` | una tabella **per modulo** ([`docs/03 §1.4/§3.1quater.A`](../../progettazione/03-gestione-dati.md), come `catalog_media_assets`/`fiskaly_media_assets`); URL completo → `origin` (enum, mappa in config) + `path`; `+ checksum`/`byte_size`/`content_type`; bonifica `files%5B%5D`→`files` |
| **[notification_templates](notification/notification_templates.md)** | [`custom_templates`](../../analisi-pre/db/custom_templates.md) (103) **+** [`hotel_message_templates`](../../analisi-pre/db/hotel_message_templates.md) (0 in prod) **+** corpo di `marketing_automation.textFields` / `marketing_email_campaign.message` | 1 template (oggetto + corpo con segnaposto) per scopo/canale/lingua | **sì** (riferito da `notification_deliveries`, API/URL) | `public_id` UNIQUE, `(property_id, template_kind, channel, culture) WHERE is_active` parz., `(…) WHERE is_default` UNIQUE parz. | **3 sintassi di segnaposto** (`@token`×3 motori, `{token}`) → **una** `{{ mustache }}` ([`docs/03 §2.7`](../../progettazione/03-gestione-dati.md)); rendering **nel browser** → **un renderer server** Razor auto-escaping; token fuori vocabolario **canonico** rifiutato alla `create` (vocabolario = enum/`.resx`, non tabella — [D-48](_domande-committente.md)); `type` `1/2` → `channel` enum (+ `any` per i template report); `status` `0/1` → `deleted_at`+`is_active`; `isDefault` senza transazione → UNIQUE parz. + gestione atomica (da `hotel_message_templates`); `subject 'Oggetto da cambiare'` → `NULL` |
| **[notification_template_attachments](notification/notification_template_attachments.md)** | array [`custom_templates.attachment`](../../analisi-pre/db/custom_templates.md) / `marketing_*.attachments` | 1 file allegato a un template, in posizione N | no | `(template_id, media_asset_id)` UNIQUE parz., `(template_id, sort_order)` parz. | "array URL → giunzione ordinata" ([`docs/03 §3.1quater.A`](../../progettazione/03-gestione-dati.md)); campagne/automazioni referenziano un `template_id` → gli allegati sono **sempre** del template; `filename_snapshot` |

## Sottoarea `email`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[notification_email_channel_settings](notification/notification_email_channel_settings.md)** | [`bathhouse_email_configrations`](../../analisi-pre/db/bathhouse_email_configrations.md) (22 righe) | config SMTP in uscita — **1 per stabilimento** | no | `(property_id)` UNIQUE parz. | `smtpPassword varchar` in chiaro (13 righe condividono quella di piattaforma, restituita da API) → `smtp_password_secret_ref` (secret manager, [`docs/17-secrets.md`](../../progettazione/17-secrets.md), [`docs/03 §6`](../../progettazione/03-gestione-dati.md)); `secure:true` cablato → `security` enum usato davvero; `status` → `is_active`+`deleted_at`; `+ UNIQUE(property_id)` (mancava); `fromName`/`testEmailRecipients` ora applicati; `+ is_verified`/`last_verification_error`; tabella sorella `email_configrations` (0 righe) **non** migrata |

## Sottoarea `inapp`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[notification_inapp_messages](notification/notification_inapp_messages.md)** | [`notification_details`](../../analisi-pre/db/notification_details.md) (**317 300 righe**) | 1 evento operativo di uno stabilimento (feed "campanella") | **sì** (deep-link UI) | `public_id` UNIQUE, `(property_id, created_at DESC) WHERE visibility='visible'` parz., `(subject_kind, subject_public_id)` | **non si salva il testo, si salva l'evento** (`notification_details.md` §"Ottimizzazione", [`docs/03 §2.4quater`](../../progettazione/03-gestione-dati.md)): `kind` enum + `payload jsonb` (variabili template, **snapshot**) + rendering a template localizzato; `notification_text`+`_it` (concatenati in 6+ punti, IT copia, `NULL` senza fallback, `amount`/`undefined` letterali, troncamento 255) eliminati; `type 1/2` → `subject_kind` enum; `status 1/2` (visibilità mascherata) → `visibility` enum; `read` **per property** → stato-lettura **per utente** (figlia); `user_id` (un `customers.id` mai riletto), `bookingNumber '0'` non migrati; `reference_id` (spazi id sovrapposti) → `subject_public_id` uuid tipizzato; retention 6–12 mesi (era crescita illimitata) |
| **[notification_inapp_message_reads](notification/notification_inapp_message_reads.md)** | `notification_details.read` (`tinyint` **per property**) | stato di lettura per (messaggio × utente) | no | `(message_id, user_id)` UNIQUE, `(user_id, read_at DESC)` | l'assenza di riga = non letta; "segna tutte" = `INSERT … ON CONFLICT DO NOTHING` (non `UPDATE` globale per property); `+ dismissed_at` |
| **[notification_user_channel_preferences](notification/notification_user_channel_preferences.md)** | [`users.notifyEmail`/`notifyWhatsapp`/`disableNotification`](../../analisi-pre/db/users.md) (C5 → C6, [`docs/03 §6`](../../progettazione/03-gestione-dati.md)) | preferenze di notifica dello staff — 1 per utente | no | `(user_id)` UNIQUE parz. | 3 booleane su `users` → `enabled_channels` **`enum[]`** (canali combinabili, stesso criterio di `catalog_property_online_booking_settings.enabled_payment_methods`) + `is_muted`; `disableNotification` (mai letto dal backend legacy) → **rispettato**; `+ muted_kinds`/`quiet_hours`; riga creata solo se ≠ default |
| **[notification_push_device_tokens](notification/notification_push_device_tokens.md)** | array [`users.deviceTokens`](../../analisi-pre/db/users.md) (C5 → C6, [`docs/03 §6`](../../progettazione/03-gestione-dati.md)) | 1 token push (FCM/APNs) di un dispositivo | no | `(token_hash)` UNIQUE, `(user_id) WHERE is_active` parz., `(last_seen_at) WHERE is_active` parz. | array senza data/scadenza/piattaforma → 1 riga per token con `platform` enum, `last_seen_at`, `is_active` (disattivato su `UNREGISTERED`); purge dei token stantii |

## Sottoarea `marketing`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[notification_marketing_contacts](notification/notification_marketing_contacts.md)** | *(nuova, [D-46](_domande-committente.md))* — `contact_list_member.contactId` (`customers.id`, 13% `NULL`), array `recipientList`/`recipients`, `addCustomer` da `bookings.isMarketingCampaign` | contatto marketing di uno stabilimento, chiave `(property_id, email)` | **sì** (GDPR, API) | `public_id` UNIQUE, `(property_id, lower(email)) WHERE deleted_at IS NULL` UNIQUE parz., `(unsubscribe_token)` UNIQUE, `(customer_public_id)` parz., `(property_id, consent_status)` | **non è una copia della rubrica C5**: solo gli indirizzi entrati in un pubblico marketing della property (auto-iscrizione da `isMarketingCampaign`, aggiunta/import manuale, lista grezza di campagna); `customer_public_id` FK viva opzionale, `full_name`/`phone`/`email` denorm.; **consenso** (`consent_status`/`_source`/`_at`/`_notice_version` = i 4 elementi della prova art. 7 GDPR) — oggi non tracciato ([`docs/11 §"marketing"`](../../progettazione/11-modulo-privacy.md)); verifica consenso **all'invio** (non alla creazione lista); `+ unsubscribe_token`; email normalizzata + `UNIQUE`; `subscribes` (newsletter globale) **non** confluisce qui; **base giuridica/collocazione da validare** ([D-46](_domande-committente.md)) |
| **[notification_marketing_contact_lists](notification/notification_marketing_contact_lists.md)** | [`contact_list`](../../analisi-pre/db/contact_list.md) (125) | segmento di clientela per campagne/automazioni | **sì** (API/URL) | `public_id` UNIQUE, `(property_id) WHERE deleted_at IS NULL` parz., `(property_id) WHERE is_default` UNIQUE parz. | `status` filtrato ma mai scritto (delete = `destroy` fisico) → `deleted_at`; churn id (destroy+recreate) → soft-delete; `updated_at` mai aggiornato → gestito; `+ UNIQUE(property_id) WHERE is_default`; `+ origin` enum; `+ member_count` denorm. |
| **[notification_marketing_contact_list_members](notification/notification_marketing_contact_list_members.md)** | [`contact_list_member`](../../analisi-pre/db/contact_list_member.md) (29 488 vive, `id`→63 649, 13% `contactId NULL`) | 1 (lista × contatto) | no | `(list_id, contact_id) WHERE deleted_at IS NULL` UNIQUE parz., `(list_id)` parz., `(contact_id)` parz. | `contactId` (`customers.id`) → `contact_id` FK a `notification_marketing_contacts`; `+ UNIQUE(list_id,contact_id)` (mancava); FK reali; add/remove **puntuali** (non destroy+recreate); `status` colonna morta eliminata; `+ added_by` enum, `+ added_at` (storico) |
| **[notification_marketing_suppressions](notification/notification_marketing_suppressions.md)** | *(nuova)* — `subscribes` con `status=0` (434) senza semantica di prova | 1 indirizzo/telefono da non contattare più | no | `(coalesce(property_id,…), lower(address), channel)` UNIQUE, `(lower(address))` | **lista di soppressione (opt-out)** — l'unico dato marketing conservato **a vita** (prova della revoca, [`docs/80 §3.6/§7`](../../progettazione/80-retention.md), [`docs/11`](../../progettazione/11-modulo-privacy.md)); `scope` property o **globale** (`property_id NULL`); `reason` enum (`unsubscribe_link`/`spam_complaint`/`hard_bounce`/…); alimentata da click unsubscribe + webhook bounce/complaint; **nessuna retention** |
| **[notification_marketing_newsletter_subscriptions](notification/notification_marketing_newsletter_subscriptions.md)** | [`subscribes`](../../analisi-pre/db/subscribes.md) (6 548) | 1 iscrizione alla newsletter **globale** (no `property_id`) | **sì** (GDPR, pannello admin) | `public_id` UNIQUE, `(lower(email)) WHERE deleted_at IS NULL` UNIQUE parz., `(confirmation_token)` UNIQUE parz., `(unsubscribe_token)` UNIQUE, `(status, created_at DESC)` | `status 1/0` → `status` enum `pending_confirmation│subscribed│unsubscribed` + **double opt-in** (`confirmation_token`/`confirmed_at`) + `unsubscribe_token` ([`docs/11 §"subscribes"`](../../progettazione/11-modulo-privacy.md)); `+ UNIQUE(email)` (mancava, duplicati garantiti); `+ source` enum; `unsubscribed` → riga di soppressione globale; rotte sotto `systemadmin` (era `optionalAuth`) |
| **[notification_marketing_email_campaigns](notification/notification_marketing_email_campaigns.md)** | [`marketing_email_campaign`](../../analisi-pre/db/marketing_email_campaign.md) (25) | 1 campagna email one-shot verso un segmento | **sì** (API/URL) | `public_id` UNIQUE, `(scheduled_at) WHERE state='scheduled'` parz., `(property_id, state, created_at DESC)` | `state ENUM` → enum **unificato** con automazioni + `deleted_at`; `recipientList` JSON snapshot (destinatari veri, `contactListId` decorativo) → risoluzione **dalla lista all'invio** + figlia `campaign_recipients` (retention 6 mesi); `message` inline + EJS da disco → `template_id`; `name NOT NULL` sempre `""` → nullable; `scheduledDate`(UTC)+`scheduledTime`(stringa) doppio shift → `scheduled_at timestamptz` + `timezone`; `enablePushNotifications`/`sendCopy` → `also_send_push`/`send_copy_to_manager`; nessun esito per destinatario → outbox; indice su `state` (era scan al minuto) |
| **[notification_marketing_campaign_recipients](notification/notification_marketing_campaign_recipients.md)** | array `recipientList` / `recipients` (destinatari **congelati**) | 1 destinatario di un invio (campagna o automazione) + esito | no | `(parent_kind, parent_id, status)`, `(contact_id)` parz., `(parent_kind, parent_id, lower(email_snapshot))` UNIQUE | de-jsonizzazione degli array email; `email_snapshot`/`full_name_snapshot`; `+ status` per destinatario (era solo `console.error`) + link a `notification_deliveries`; retention **6 mesi** dall'invio ([`docs/80 §3.6`](../../progettazione/80-retention.md), verifica consenso al momento dell'invio) |
| **[notification_marketing_automations](notification/notification_marketing_automations.md)** | [`marketing_automation`](../../analisi-pre/db/marketing_automation.md) (221) | 1 automazione (email + push opz.) legata alle prenotazioni | **sì** (API/URL) | `public_id` UNIQUE, `(property_id, schedule_type) WHERE state='active'` parz. | `name` in `switch` per stringa → `automation_kind` enum (Welcome/Goodbye oggi **inerti** — [D-47](_domande-committente.md)); `state ENUM` (mai portato a `sent`) → enum unificato `draft│active│paused│canceled`; `scheduleValue` JSON polimorfo (`{days}` vs array `{type,time}`) → `schedule_type`+`days_offset`+`send_at_time` + figlia trigger; `textFields`/`message` → `template_id`; `recipients` snapshot → risoluzione dalla lista + figlia recipients; **N query SQL per destinatario ogni 5 min** (SQL injection) → **eventi di dominio** C1, no query raw; `enableAppNotifications` → `also_send_push` |
| **[notification_marketing_automation_triggers](notification/notification_marketing_automation_triggers.md)** | array `scheduleValue` per `scheduleType='custom'` (`[{type,time,days}]`) | 1 trigger custom di un'automazione | no | `(automation_id, trigger_kind) WHERE deleted_at IS NULL` UNIQUE parz., `(trigger_kind)` parz. | de-jsonizzazione; `type` (`ofbooking`/`beforebooking`/`afterbooking`/`pendingpayments`) → `trigger_kind` enum; **più righe** per automazione (non `enum[]`, [`docs/03 §2.4quater`](../../progettazione/03-gestione-dati.md)); `+ CHECK(days_offset>=0)` (era `0` → loop infinito) |
| **[notification_marketing_events](notification/notification_marketing_events.md)** | [`marketing_event`](../../analisi-pre/db/marketing_event.md) (39) — *feature marginale, [D-47](_domande-committente.md)* | 1 evento/iniziativa dello stabilimento | **sì** (API/URL + proiezione vetrina C2) | `public_id` UNIQUE, `(property_id, starts_on) WHERE is_active` parz., `(property_id, starts_on) WHERE is_public AND is_active` parz. | **ricorrenza materializzata in N righe** (`AUTO_INCREMENT` 4066/13; modifica su una sola) → **regola** su 1 riga (`repeat_type`/`repeat_interval`/`repeat_unit`/`repeat_until`), occorrenze calcolate a runtime; `repeatType`/`repeatUnit` `ENUM` → enum PG; `repeatInterval` sempre `0` → `CHECK(>=1)`; `startHour/endHour varchar "HH:mm"/""` → `time` + `is_full_day`; `showOnEstablishmentPage` senza effetto → `is_public` **usato davvero** (proiezione C2); `coverImage` URL → `cover_image_asset_id` FK; `notificationsEnabled`+reminder = catena senza mittente → `reminders_enabled` + [D-47](_domande-committente.md); `status` → `deleted_at`+`is_active` |
| **[notification_marketing_event_reminders](notification/notification_marketing_event_reminders.md)** | [`marketing_event_notification`](../../analisi-pre/db/marketing_event_notification.md) (**0 righe**) — *⚠️ feature incompleta, candidata a dismissione, [D-47](_domande-committente.md)* | 1 reminder pianificato di un evento | no | `(scheduled_at) WHERE status='pending'` parz., `(event_id)` parz. | **nessun mittente nel legacy** (`status` mai ≠ `pending`); `notificationType` `ENUM` → `offset` enum; `notificationDate`+`notificationTime varchar` → `custom_date`/`custom_time` + `scheduled_at` calcolato; `+ FK` con `ON DELETE CASCADE` (era pulizia a mano); `status` `ENUM` → `+ canceled`; **da completare con scheduler o dismettere** ([D-47](_domande-committente.md)) |
| **[notification_establishment_claim_requests](notification/notification_establishment_claim_requests.md)** | [`csv_manager_details`](../../analisi-pre/db/csv_manager_details.md) (13) — *feature marginale, candidata a dismissione, [D-47](_domande-committente.md)* | 1 lead "Richiedi la tua pagina" dal sito pubblico | **sì** (pannello admin) | `public_id` UNIQUE, `(status, created_at DESC)`, `(bathhouse_public_id)` parz. | nome legacy (`csv_owner`) senza relazione con la funzione → rinominata; `bathhouseId` **letto ma non persistito** → `bathhouse_public_id` (id copiato C2) + `bathhouse_name_snapshot`; `+ status` enum di lavorazione; `message varchar(255)` troncato → `text`; l'invio all'admin è una riga `notification_deliveries` (`kind=establishment_claim`), non email hardcoded a `info@summerbooking.it`; `+ captcha`/rate-limit (annotato) |

### Tabelle/campi legacy che **non entrano** in C6

| Legacy | Dove va | Perché |
|---|---|---|
| `email_configrations` (0 righe, nessun model) | *(non migrata)* | predecessore di `bathhouse_email_configrations` |
| `email_logs.cronStatus` | **eliminato** | colonna morta (scritta ovunque, mai letta) |
| `notification_details.user_id` | **eliminato** | contiene un `customers.id` mai riletto; nel target il destinatario in-app è l'operatore |
| `custom_templates.createdBy` (sempre `NULL` in prod) | `notification_templates.created_by_user_id` | audit assente, valorizzato best-effort |
| `revenue_notifications` | **C2** `catalog` (`pricing`) | nome fuorviante (`*_notifications`) ma è **configurazione Revenue Management**, nessuna messaggistica ([`catalog_pricing_revenue_settings`](catalog/catalog_pricing_revenue_settings.md)) |
| `switchboard_wa_msg_log` e `switchboard_*` | **C7** `switchboard` | log/entità del centralino Telnyx + WhatsApp Business |
| `bathhouse_email_configrations.smtpPassword` | **INFRA** (secret manager) | password SMTP in chiaro → `smtp_password_secret_ref` |
| segnaposto canonici dei template (`establishment_name`, `customer_full_name`, …) | **codice** (enum + `.resx`) | vocabolario chiuso e versionato col codice, non una tabella ([`docs/03 §2.5/§2.7`](../../progettazione/03-gestione-dati.md), [D-48](_domande-committente.md)) |

### Campi legacy di altri domini che **entrano** in C6

| Campo legacy | Da | Dove entra in C6 |
|---|---|---|
| `users.notifyEmail` / `notifyWhatsapp` | C5 | `notification_user_channel_preferences.enabled_channels` |
| `users.disableNotification` | C5 | `notification_user_channel_preferences.is_muted` |
| `users.deviceTokens` (array) | C5 | `notification_push_device_tokens` (1 riga/token) |
| `bookings_send_payment_summaries.method_flag` / `email_sent` | C3 | riga `notification_deliveries` (`channel`, `status`) |
| `fiskaly_receipt.emailSent` / `whatsappSent` · `fiskaly_non_fiscal_receipt.*` | C4 | riga `notification_deliveries` |
| `sharing_details.email_sent` | C1 | riga `notification_deliveries` (`kind = resale_credit_earned`) |
| `hotel_reports.path` + logica di invio | C2 | `notification_media_assets` (`rendered_pdf`) + riga `notification_deliveries` (`kind = agreement_report`; C2 ha rinominato "hotel" in `agreement`) |
| `card_transactions.sendCardDetails` (→ `email_logs`) | C3 | riga `notification_deliveries` (`kind = wallet_statement`) |

### Domande aperte C6 (in [`_domande-committente.md`](_domande-committente.md))

- **[D-46]** 🟥 Consenso marketing: base giuridica e collocazione (`notification_marketing_contacts` vs
  C5); se `consent_status = unknown` blocca l'invio? — validazione DPO/legale.
- **[D-47]** 🟨 Feature marginali/incomplete C6: `notification_marketing_events` (tenere?
  ricorrenza-regola + proiezione vetrina), `notification_marketing_event_reminders` (completare con
  scheduler o dismettere), `notification_establishment_claim_requests` (tabella o solo invio email).
- **[D-48]** 🟨 Vocabolario canonico dei segnaposto template: enum/costante + `.resx` (proposto) o
  lookup a DB.
- **[D-49]** 🟨 `notification_inapp_messages.payload jsonb` per le variabili di rendering (snapshot):
  confermato per analogia con [D-28] (read-model, approccio A)?

---
---

# Dominio C7 — `switchboard`

Modulo **`switchboard`** — **centralino virtuale Telnyx** (IVR ad albero + inoltro SIP) + **WhatsApp
Business** del centralino ([`docs/03-gestione-dati.md` §2.4quinquies / §3.1bis](../../progettazione/03-gestione-dati.md),
[`docs/01-criticita-e-indicazioni-backend.md` §9](../../progettazione/01-criticita-e-indicazioni-backend.md),
[`docs/35-integrazioni-esterne.md` §Telnyx/Meta](../../progettazione/35-integrazioni-esterne.md),
[`docs/11-modulo-privacy.md` §4/§5](../../progettazione/11-modulo-privacy.md),
[`docs/17-secrets.md`](../../progettazione/17-secrets.md), [`docs/80-retention.md` §3.7](../../progettazione/80-retention.md)),
sottoaree ([`docs/03 §3.1bis`](../../progettazione/03-gestione-dati.md)):

- **nucleo (senza sottoarea)** — hub di configurazione del centralino; catalogo dei prompt audio.
- **`voice`** — ordine numero + KYC regolatorio, IVR ad albero (voci + azioni), dispositivi softphone,
  registro chiamate (dominio) + log diagnostico flusso chiamata.
- **`messaging`** — template WhatsApp registrati su Meta; log eventi webhook WhatsApp (stati consegna +
  messaggi in arrivo).

*(Perimetro: tutte le 8 tabelle `switchboard_*` legacy hanno **0 righe** in `local_sb` e nel dump di
produzione `sb_prod_dump_2026_09_08` ([`docs/03 §2.4quinquies`](../../progettazione/03-gestione-dati.md)) — ma
**⚠️ verificato 2026-09-11**: su un **terzo ambiente**, `summer_booking_dev` (RDS
`database-1.cmmdmd7jtypl.eu-south-1.rds.amazonaws.com`, credenziali in
[`back-pre/app/config/db.config.js`](../../../../../../back-pre/app/config/db.config.js)), la feature è **attiva e
collaudata** con dati reali: 1 config attiva (property di test interno "Qbitsoft s.r.l."), un albero
IVR reale, 5 audio, 3 template WhatsApp approvati su Meta, **214** righe di registro chiamate e **7 355**
righe di log diagnostico su ~2,5 mesi. Resta però vero che **nessuno dei 5 frontend del repo ha una
schermata per il modulo** ([`analisi-pre/business-logic/stabilimento/13`](../../analisi-pre/business-logic/stabilimento/13-assistente-notifiche-account.md),
[`da-chiarire.md` #36](../../analisi-pre/business-logic/da-chiarire.md)) — la configurazione osservata è
stata fatta via API direttamente (o dal "pannello di back-office esterno" citato nel codice), non da
un'interfaccia del repo. Vedi il dettaglio completo, tabella per tabella, nelle note "Aggiornamento
2026-09-11" delle singole schede `analisi-pre/db/switchboard_*.md`. Scelta committente (2026-09-10, **prima**
di questa verifica): **redesign pulito** — target ridisegnato, non migrazione 1:1, come C4bis — con
dominio marcato "candidata a dismissione / interfaccia da reperire" ([D-51](_domande-committente.md),
**aggiornata** alla luce dell'uso reale). Verso C2/C5 si copia solo il `public_id`, **mai** FK
cross-modulo; il runtime SIP/real-time è **isolato** ([`docs/35`](../../progettazione/35-integrazioni-esterne.md)).
I segreti (`sip_password`) e le credenziali partner escono in **INFRA** (secret manager,
[`docs/17`](../../progettazione/17-secrets.md)); i `raw_payload` Telnyx/Meta sono `jsonb` passthrough di sola
diagnostica, con retention breve e pseudonimizzazione.)*

## Nucleo (nessuna sottoarea)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[switchboard_configs](switchboard/switchboard_configs.md)** | [`switchboard_config`](../../analisi-pre/db/switchboard_config.md) (0 righe su `local_sb`/dump prod; **1 riga reale** su `summer_booking_dev`) meno `reg_*` / `sip_password` / `device_fcm_token`·`device_voip_token` / `whatsapp_business_account_id` | hub del centralino di uno stabilimento — 1 per property | **sì** (pannello back-office, GDPR) | `public_id` UNIQUE, `(property_id)` UNIQUE parz., `(telnyx_phone_e164) WHERE is_active` UNIQUE parz., `(sip_username)` UNIQUE parz. | `sip_password varchar` in chiaro (chiave di lookup, restituita da API/email) → `sip_password_secret_ref` (secret manager, come C4-2); `reg_*` KYC → figlia [`switchboard_voice_number_registrations`](switchboard/switchboard_voice_number_registrations.md) (PII cifrata, accesso `systemadmin` + audit — **confermato reale**: la riga osservata ha un KYC vero); `device_fcm_token`/`device_voip_token` (2 colonne, 1 device) → [`switchboard_voice_softphone_devices`](switchboard/switchboard_voice_softphone_devices.md) (1 riga/device, come C6 push tokens); `whatsapp_business_account_id` (mai riletto) / `telnyx_webhook_url` (solo specchio) / `sip_domain` (= env) **non migrati**; `is_active smallint` → `boolean`; modalità IVR implicita (`telnyx_call_control_app_id IS NOT NULL`) → `routing_mode` enum `ivr`/`direct_sip` (**confermato**: la config reale gira in `ivr`); timeout cablati (`SIP_FORWARD_TIMEOUT_SECS=90`, gather) → colonne; `forward_caller_id_mode` (già stringa a 3 valori) → enum; indici unici ridondanti su `property_id` → uno solo (**tre** a schema su `summer_booking_dev`) |
| **[switchboard_media_assets](switchboard/switchboard_media_assets.md)** | [`switchboard_audio_file`](../../analisi-pre/db/switchboard_audio_file.md) (0 righe) | 1 prompt audio (benvenuto / voce di menù / fallback) | no (id pubblico solo per anteprima, non su URL Telnyx) | `public_id` UNIQUE, `(config_id) WHERE deleted_at IS NULL` parz., `(deleted_at) WHERE deleted_at IS NOT NULL` parz., `(config_id, checksum)` UNIQUE parz. opz. | tabella **per modulo**, stessa forma di `catalog_media_assets` ([`docs/03 §3.1quater.A`](../../progettazione/03-gestione-dati.md) la nomina); `s3_key` URL/chiave assoluta → `origin` + `path`; `s3_bucket` (mai riletto) **non migrato**; `mime_type` costante (`audio/mpeg`) → `content_type` con default; `+ checksum`/`byte_size`/`duration_ms`; nessuna deduplica → `UNIQUE` parz. opz.; endpoint proxy pubblico non protetto (`id` numerico indovinabile) → URL firmato a TTL breve; cancellazione parziale su errore S3 → `deleted_at` + GC disaccoppiato |

## Sottoarea `voice`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[switchboard_voice_number_registrations](switchboard/switchboard_voice_number_registrations.md)** | fetta `reg_*` + `telnyx_number_order_*` di [`switchboard_config`](../../analisi-pre/db/switchboard_config.md) | ordine numero Telnyx IT + fascicolo KYC regolatorio — 1:1 con la config | no (accesso via config) | `(config_id)` UNIQUE, `(provisioning_state) WHERE in ('documents_pending','submitted')` parz., `(reg_identity_doc_expiry)` parz. | scorporo per **PII ad alto rischio** ([`docs/11 §4`](../../progettazione/11-modulo-privacy.md): CF personale, documento identità, data/luogo nascita → cifrare a riposo, accesso `systemadmin` + audit di lettura, mai in export/log); date `varchar(10)` → `date`; `telnyx_phone_number_status` grezzo → `provisioning_state` enum interno + `telnyx_status_raw`; `reg_it_customer_type`/`reg_identity_doc_type` (vocabolari Telnyx) → enum passthrough; retention post-cessazione + dubbio D.L. 7/2015 → [D-51](_domande-committente.md) |
| **[switchboard_voice_menu_items](switchboard/switchboard_voice_menu_items.md)** | [`switchboard_menu_item`](../../analisi-pre/db/switchboard_menu_item.md) (0 righe) | 1 voce dell'IVR ad albero (nel contesto di un genitore) | **sì** (API pannello) | `public_id` UNIQUE, `(config_id, parent_id, dtmf_key) WHERE deleted_at IS NULL` UNIQUE parz., `(config_id, parent_id, dtmf_key) WHERE is_active` parz., `(config_id, parent_id, sort_order)` parz. | nessuna unicità `(config, parent, dtmf_key)` (IVR prendeva una riga arbitraria via `findOne`) → `UNIQUE` parz.; `is_active smallint` → `boolean` (filtrato in modo coerente pannello/runtime); `dtmf_key varchar(2)` → `varchar(1)` + `CHECK ([0-9*#])`; profondità implicita (3 livelli) → `depth` + `CHECK`; `position` (solo-UI) → `sort_order`; tipo nodo (submenu/foglia/terminale) **derivato**, non colonna (eventuale `node_kind` enum) |
| **[switchboard_voice_menu_actions](switchboard/switchboard_voice_menu_actions.md)** | [`switchboard_menu_action`](../../analisi-pre/db/switchboard_menu_action.md) (0 righe) | 1 azione di una voce di menù (eseguita in sequenza) | **sì** (API pannello) | `public_id` UNIQUE, `(menu_item_id, execution_order) WHERE deleted_at IS NULL` parz., `(wa_template_id)` parz. | `action_config json` polimorfo → **colonne tipizzate**: `hangup`/`forward_sip` non ne avevano bisogno (contratto morto — `forward_sip` legge tutto da `switchboard_configs`); `send_whatsapp` → `wa_template_id` (FK) + `wa_template_name_snapshot` + `wa_language` + `wa_recipient_mode` enum + `wa_recipient_phone_e164` + `wa_body_params`/`wa_header_params`/`wa_button_params jsonb` (liste di argomenti API Meta, passthrough — [D-52](_domande-committente.md)); `action_type` (già `ENUM`, whitelist duplicata nel controller) → enum PG; `CHECK` su `wa_template_id` per `send_whatsapp` |
| **[switchboard_voice_softphone_devices](switchboard/switchboard_voice_softphone_devices.md)** | colonne `device_fcm_token` / `device_voip_token` di [`switchboard_config`](../../analisi-pre/db/switchboard_config.md) (**1 token FCM reale** osservato su `summer_booking_dev`) | 1 dispositivo softphone che riceve il push "wake" prima dell'inoltro SIP | no | `(token_hash)` UNIQUE, `(config_id) WHERE is_active` parz., `(last_seen_at) WHERE is_active` parz. | 2 colonne / 1 device → N righe (come C6 `notification_push_device_tokens`); piattaforma non tipizzata (dedotta dal nome colonna) → `platform` enum (`fcm_android`/`apns_voip`/`web`); nessuna scadenza/invalidazione → `last_seen_at` + `is_active` (disattivato su `UNREGISTERED`); registrazione autenticata via credenziali SIP in chiaro → segreto risolto + rate limiting; retention: device inattivo N mesi → 🗑️ |
| **[switchboard_voice_call_records](switchboard/switchboard_voice_call_records.md)** | [`switchboard_call_register`](../../analisi-pre/db/switchboard_call_register.md) (0 righe) | 1 chiamata gestita dal centralino (tabella eventi di **dominio**) | **sì** (GDPR, storico) | `public_id` UNIQUE, `(config_id, started_at DESC)`, `(config_id, started_at DESC) WHERE direction='outbound'` parz., `(property_id, caller_number, started_at DESC)`, `(telnyx_call_session_id)`, `(started_at) WHERE deleted_at IS NULL` parz. | [`docs/03 §2.4quinquies`](../../progettazione/03-gestione-dati.md): *"vera tabella eventi con config_id, esito e durata"*; `from`/`to`/`datetime` soltanto → `+ config_id`/`direction`/`outcome` enum/`forward_result` enum/`started_at`·`answered_at`·`ended_at`/`duration_seconds` (generata)/`ivr_path jsonb`; `to` = stringa `sip:` con match esatto → FK `config_id` + `sip_destination` (congelato, **senza** suffisso `_snapshot` — fatto proprio dell'evento, C1-7); retention **12 mesi** poi 🗑️/🔒 numero ([`docs/80 §3.7`](../../progettazione/80-retention.md)); endpoint softphone senza rate limiting → segreto risolto + rate limiting; scritture idempotenti su `telnyx_call_session_id` |
| **[switchboard_voice_call_diagnostics](switchboard/switchboard_voice_call_diagnostics.md)** | [`switchboard_call_log`](../../analisi-pre/db/switchboard_call_log.md) (0 righe, `SB_CALL_LOGGING_ENABLED`, **nessun lettore**) | 1 evento del flusso chiamata (webhook Telnyx o passo IVR) | no (log) | `(telnyx_call_session_id, created_at)`, `(call_record_id)` parz., `(config_id, created_at DESC)`, `(created_at)` | **tabella di puro log** — [`docs/03 §2.4quinquies`](../../progettazione/03-gestione-dati.md) suggeriva di **non** migrarla (→ tracing); **creata come tabella C7 su decisione committente** (2026-09-10) con retention **30–90 gg** ([`docs/80 §3.7`](../../progettazione/80-retention.md)), opt-in per property; `event_type` a doppia natura (`call.*` + `step.*`) → `event_source` enum + `event_name` stringa; `raw_payload` senza redazione → `jsonb` + numeri pseudonimizzati + omissibile; scritture fire-and-forget → buffer best-effort + metrica di scarto; `deleted_at` esposto ma non usato; `+ call_record_id` |

## Sottoarea `messaging`

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[switchboard_messaging_wa_templates](switchboard/switchboard_messaging_wa_templates.md)** | [`switchboard_wa_template`](../../analisi-pre/db/switchboard_wa_template.md) (0 righe) | 1 template WhatsApp registrato su Meta per uno stabilimento | **sì** (API pannello) | `public_id` UNIQUE, `(property_id, lower(template_name), language) WHERE deleted_at IS NULL` UNIQUE parz., `(config_id) WHERE deleted_at IS NULL` parz., `(meta_template_id)` parz., `(property_id, meta_status)` | **distinta da C6 `notification_templates`** (definizioni Meta, non renderer interno); `meta_status ENUM(3)` troppo stretto (Meta espone `PAUSED`/`DISABLED`/`IN_APPEAL`/`PENDING_DELETION` → troncamento) → enum passthrough esteso + `meta_status_raw`; nessuna unicità `(property, name, language)` → `UNIQUE` parz.; `components json` **resta `jsonb`** (documento di sistema esterno, [`docs/03 §3.6 punto 5`](../../progettazione/03-gestione-dati.md)) + validazione al write (forma Meta + segnaposto canonici) + colonne di riepilogo derivate (`header_format`/`has_buttons`/`buttons_count`/`body_param_count`/`body_preview`); `category` (già `ENUM`) → enum PG; `deleteConfig` disallineava Meta → reconcile job; `language` = codice "di libreria" (non enum) |
| **[switchboard_messaging_wa_event_logs](switchboard/switchboard_messaging_wa_event_logs.md)** | [`switchboard_wa_msg_log`](../../analisi-pre/db/switchboard_wa_msg_log.md) (0 righe, **nessun lettore**) | 1 evento webhook WhatsApp (stato consegna o messaggio in arrivo) sul numero del centralino | no (log) | `(wamid, event_type, coalesce(status,'')) WHERE wamid IS NOT NULL` UNIQUE parz., `(config_id, received_at DESC)`, `(wamid)`, `(config_id, event_type, status) WHERE event_type='status'` parz., `(received_at)` | **tabella di puro log** — [`docs/03 §2.4quinquies`](../../progettazione/03-gestione-dati.md) indicava l'outbox C6; **creata come tabella C7 su decisione committente** (2026-09-10) come inbox forense del centralino, retention **6–12 mesi** poi 🔒/🗑️; webhook POST senza verifica firma → `X-Hub-Signature-256` obbligatoria ([`docs/01 §9`](../../progettazione/01-criticita-e-indicazioni-backend.md)); `+ config_id` risolto da `phone_number_id`; `message_body` (testo + payload bottone + dump JSON media) → `body_text` + `body_payload jsonb`; `event_type`/`status`/`conversation_origin`/`pricing_category`/`message_type` stringhe → enum (con `unknown` di fallback); numeri pseudonimizzati; idempotenza su `(wamid, event_type, status)`; correlazione `wamid` → `notification_deliveries` (C6) per lo stato d'invio reale |

### Tabelle/campi legacy che **non entrano** in C7

| Legacy | Dove va | Perché |
|---|---|---|
| `switchboard_config.sip_password` | **INFRA** (secret manager) | password SIP in chiaro → `sip_password_secret_ref` ([`docs/03 §6`](../../progettazione/03-gestione-dati.md), [`docs/17`](../../progettazione/17-secrets.md)) |
| `switchboard_config.whatsapp_business_account_id` | **eliminato** | scritto e restituito, **mai riletto** dalla logica |
| `switchboard_config.telnyx_webhook_url` | **eliminato** (→ configurazione) | solo specchio a DB, mai riletto; l'URL webhook vive in `appsettings` ([`docs/03 §1`](../../progettazione/03-gestione-dati.md)) |
| `switchboard_config.sip_domain` | **eliminato** (→ configurazione) | sempre = `env.TELNYX_SIP_DOMAIN` (`sip.telnyx.com`) |
| `switchboard_audio_file.s3_bucket` | **eliminato** (→ `origin` + config) | scritto ma mai riletto (le operazioni S3 usano una costante) |
| `switchboard_audio_file.mime_type` | **degradato a default** | costante `audio/mpeg` (tutto convertito in MP3) |
| stati di consegna WhatsApp dei **nostri invii** | **C6** `notification_deliveries` (correlazione via `wamid`) | l'outbox è la verità dello stato d'invio; `switchboard_messaging_wa_event_logs` è solo forense |
| `switchboard_config.device_fcm_token` / `device_voip_token` — drift di naming camelCase | risolto in `switchboard_voice_softphone_devices` | **verificato 2026-09-11**: drift isolato a `local_sb` (colonna scritta `deviceFcmToken`); su `summer_booking_dev` le colonne sono correttamente `snake_case` — nessun rischio reale, solo la de-colonnizzazione resta da fare |

### Domande aperte C7 (in [`_domande-committente.md`](_domande-committente.md))

- **[D-50]** 🟨 Finestre di retention esatte per `switchboard_voice_call_records` (12 mesi propone
  `docs/80`), `switchboard_voice_call_diagnostics` (30–90 gg), `switchboard_messaging_wa_event_logs`
  (6–12 mesi) — da confermare + azione a scadenza (🗑️ vs 🔒). **Rilevanza confermata**: su
  `summer_booking_dev` le tre tabelle hanno già 214/7 355/11 righe reali che aspettano una policy.
- **[D-51]** 🟧 C7 mantenere/dismettere: **aggiornata 2026-09-11** — non più "feature mai attivata":
  è attiva e collaudata su `summer_booking_dev`, ma resta senza interfaccia nei 5 frontend del repo
  (`front-admin/` esclusa). Reperire il pannello (verosimilmente esterno, usato per configurare la
  riga reale osservata) o definire i requisiti da zero
  ([`da-chiarire.md` #36](../../analisi-pre/business-logic/da-chiarire.md)); + dubbio legale D.L.
  7/2015 sulla retention del traffico telefonico ([`docs/11 §"Domande aperte" 1`](../../progettazione/11-modulo-privacy.md)),
  ora **non ipotetico** vista la mole reale di dati di traffico già accumulata.
- **[D-52]** 🟨 `jsonb` in C7: `switchboard_messaging_wa_templates.components` (documento Meta),
  `switchboard_voice_menu_actions.wa_*_params` (argomenti API Meta),
  `switchboard_voice_call_records.ivr_path` (traccia di flusso) — confermati come eccezioni
  giustificate (analogia [D-49] / [`docs/03 §3.6 punto 5`](../../progettazione/03-gestione-dati.md)).
- **[D-53]** 🟨 **Nuova, emersa dalla verifica 2026-09-11**: `switchboard_messaging_wa_event_logs.config_id`
  risolto da `phone_number_id` **non basta** quando lo stabilimento usa il numero WhatsApp **generico**
  di piattaforma (osservato realmente: tutte le 11 righe reali sono sul numero condiviso, non su un
  numero custom) — più stabilimenti senza numero proprio condividerebbero lo stesso `phone_number_id`,
  rendendo `config_id` sistematicamente `NULL` per loro. Serve una correlazione aggiuntiva (es. via
  `wamid` verso l'invio in outbox) o accettare l'attribuzione parziale.

---
---

# Dominio C8 — `chatbot`

Modulo **`chatbot`**, nessuna sottoarea — assistente AI dell'app gestionale
([`docs/26-chatbot.md`](../../progettazione/26-chatbot.md), [`docs/35-integrazioni-esterne.md` §1](../../progettazione/35-integrazioni-esterne.md),
[`docs/11-modulo-privacy.md`](../../progettazione/11-modulo-privacy.md), [`docs/80-retention.md`](../../progettazione/80-retention.md),
[`docs/16-redis.md`](../../progettazione/16-redis.md), [`docs/34-multitenant.md`](../../progettazione/34-multitenant.md)):

- storico delle conversazioni (`chatbot_conversation_turns`);
- limiti/preferenze per stabilimento, incluso l'override del provider LLM (`chatbot_manager_configs`);
- catalogo dei modelli/provider LLM selezionabili (`chatbot_llm_providers`, **nuova**).

*(Perimetro: le 2 tabelle `chatbot_*` legacy hanno **0 righe** in `local_sb` e nel dump di produzione
`sb_prod_dump_2026_09_08` — ma, come per C7, un **terzo ambiente**, `summer_booking_dev` (RDS
`database-1.cmmdmd7jtypl.eu-south-1.rds.amazonaws.com`, credenziali in
[`front-chatbot/app/config/db.config.js`](../../../../../../back-pre/app/config/db.config.js)), ha la feature
**attiva e collaudata**: 1 configurazione (property di test interno, la stessa "96" osservata in C7) e
**10 righe reali** di conversazione su ~2 mesi (2026-02-05 → 2026-02-06), verificato 2026-09-11 via
`SHOW CREATE TABLE` + lettura righe. Il codice sorgente vive su un **branch separato**,
[`front-chatbot/`](../../../../../../back-chatbot/), non ancora integrato in `back-pre`/`backend/` — solo
[`chatbotManager.controller.js`](../../../../../../back-chatbot/app/controllers/chatbotManager.controller.js) e i
file da esso raggiunti sono rilevanti, il resto della cartella è superato. Il modulo include, nel
legacy, anche uno strumento che interrogava un archivio esterno di prenotazioni di una stagione passata:
**non fa parte di questo redesign** ed è stato rimosso, non migrato —
[`docs/26-chatbot.md` §8](../../progettazione/26-chatbot.md) motiva la rimozione.)*

## Nucleo (nessuna sottoarea)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[chatbot_conversation_turns](chatbot/chatbot_conversation_turns.md)** | [`chatbot_history`](../../analisi-pre/db/chatbot_history.md) (0 righe su `local_sb`/dump prod; **10 righe reali** su `summer_booking_dev`) | uno scambio completo domanda/risposta tra un operatore (o, in futuro, un cliente anonimo) e l'assistente | **sì** (sostituisce l'`id` sequenziale oggi esposto come `historyId`) | `public_id` UNIQUE, `(property_id, created_at DESC)` parz., `(property_id, channel, created_at)`, `(actor_user_id, created_at DESC)` parz., `(actor_session_id, created_at DESC)` parz., `(llm_provider_id)` parz. | `app`→`channel` enum (`manager`/`customer`, confermato 10/10 righe reali `manag`); `created_by` nullabile→`actor_user_id` **resta nullabile** (oggi sempre valorizzato, ma predisposto per un futuro `customer` non autenticato, senza migrazione — `+ actor_session_id` per correlare i turni anonimi); esito dedotto da `output_text=''` (osservato in 4/10 righe reali)→`status` enum esplicito; `+ llm_provider_id`/`llm_model_snapshot` (chi ha risposto); `+ response_language` (non persistita nel legacy); rate limit su `updated_at`→corretto su `created_at`; dati personali di clienti osservati nel testo libero → [D-54](_domande-committente.md) |
| **[chatbot_manager_configs](chatbot/chatbot_manager_configs.md)** | [`chatbot_manager_config`](../../analisi-pre/db/chatbot_manager_config.md) (0 righe su `local_sb`/dump prod; **1 riga reale** su `summer_booking_dev`) | limiti di utilizzo + preferenza provider LLM di uno stabilimento — 1 per property | no | `(property_id)` UNIQUE parz., `(llm_provider_id)` parz. | `max_call_per_day`→`daily_request_limit`; `max_returned_history`→`history_page_size_limit`; `+ llm_provider_id` (override per stabilimento, nuovo); provisioning implicito su path di lettura (`getChatbotConfig` crea la riga se assente) → da rendere esplicito lato .NET |
| **[chatbot_llm_providers](chatbot/chatbot_llm_providers.md)** | *(nuova)* — sostituisce la mappa cablata `LLM_IMPLEMENTATIONS` di [`llmFactory.js`](../../../../../../back-chatbot/app/utils/llmFactory.js) | un modello/provider LLM selezionabile dal modulo | no | `code` UNIQUE, `(vendor) WHERE is_selectable` parz. | catalogo gestibile senza deploy; `retires_on`/`is_selectable` applicano la deprecazione mai applicata nel legacy (`gemini-2.5-flash`, scadenza dichiarata 2026-03-31, già passata alla data di questa analisi) |

### Tabelle/campi legacy che **non entrano** in C8

| Legacy | Dove va | Perché |
|---|---|---|
| strumento di interrogazione di un archivio esterno di prenotazioni (function calling `queryDatabase`, connessione DB dedicata read-only, sanitizzazione a lista nera della clausola `WHERE`) | **eliminato** | dati di un vecchio sistema esterno non più in uso; pattern "filtro libero generato da LLM + sanitizzazione a lista nera" fragile indipendentemente dall'accuratezza della sanitizzazione — [`docs/26-chatbot.md` §8](../../progettazione/26-chatbot.md) |
| `CHATBOT_MANAGER_FILE_SEARCH_STORE` (nome dello store RAG esterno) | **INFRA/configurazione** | valore di configurazione del provider, non un dato di riga |
| `CHATBOT_MANAGER_APIKEY` / `CHATBOT_MANAGER_SECRET` | **INFRA** (secret manager) | segreti del provider LLM → `docs/17-secrets.md` (da censire) |

### Domande aperte C8 (in [`_domande-committente.md`](_domande-committente.md))

- **[D-54]** 🟧 Dati personali di clienti nel testo libero di `chatbot_conversation_turns` — osservato
  realmente su `summer_booking_dev` (nome e cognome di clienti in una risposta); nessuna retention nel
  legacy. Retention/accesso da confermare.
- **[D-55]** 🟨 Seminare già l'enum `channel_kind` con `customer` (mai raggiunto da alcun endpoint) o
  solo `manager`.

---
---

# Dominio INFRA — `infra`

Modulo **`infra`**, sottoarea `integration` ([`docs/03-gestione-dati.md` §3.1bis](../../progettazione/03-gestione-dati.md)) —
**non un dominio applicativo** come C1…C8 (nessuna logica di prenotazione/pagamento/etc.), ma il
raccoglitore dei due temi cross-cutting emersi dalla revisione campo-per-campo di *tutti* i domini
([`docs/03-gestione-dati.md` §6](../../progettazione/03-gestione-dati.md), [`docs/17-secrets.md`](../../progettazione/17-secrets.md),
[`docs/80-retention.md` §3.10](../../progettazione/80-retention.md)):

1. **credenziali/segreti** che nel legacy vivevano come colonna a DB (password SMTP, password SIP, API
   key/secret Fiskaly e Fisconline, API key/secret del provider LLM, secret partner) → **escono dal
   database**: vanno in AWS Secrets Manager / SSM Parameter Store, e sulla tabella del dominio
   proprietario resta **solo** un riferimento `*_secret_ref` (già applicato in C4 `fiskaly_accounts`
   [C4-2], C6 `notification_email_channel_settings` [C6-5], C7 `switchboard_configs` [C7-3]). **Nessuna
   tabella INFRA per questo tema**: è pura configurazione/infrastruttura, non un dato applicativo.
2. **catalogo dei client API partner** (`keysecret`): a differenza del punto 1, qui **una riga per
   applicazione partner integrata** è un dato applicativo reale (chi può chiamare l'API, con quale
   scoping) — merita una tabella, ma **senza** la colonna segreto (che segue comunque la regola del
   punto 1).

## Nucleo (sottoarea `integration`)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[infra_integration_api_clients](infra/infra_integration_api_clients.md)** | [`keysecret`](../../analisi-pre/db/keysecret.md) (2 righe reali: `OfSale`, `Transactions`) | 1 applicazione partner autorizzata a chiamare l'API | **sì** (gestione da pannello systemadmin) | `public_id` UNIQUE, `(client_id) WHERE deleted_at IS NULL` UNIQUE parz., `(property_id) WHERE property_id IS NOT NULL`, `(app_name) WHERE is_active AND deleted_at IS NULL` | `secret` in chiaro → **eliminato**, resta `client_secret_ref` (secret manager); `appName` varchar → enum `integration_app`; `apiKey` senza indice → `client_id` `UNIQUE`; `params.property_id` JSON → colonna `property_id uuid`; `expiresAt` colonna morta → resa reale; `+ rotated_at` (nuovo); nome corretto da `integration_credentials` (illustrativo in `docs/03 §3.1bis`) a `integration_api_clients` (non contiene credenziali) |

### Tabelle/campi legacy che confluiscono in INFRA (come annotazione, non come tabella)

| Legacy | Campo | Perché non è una tabella INFRA |
|---|---|---|
| `fiskaly_account` | `subject_credential_key`/`_secret`, `taxpayer_fisconline_pin`/`_password` | segreti Fiskaly/Fisconline: solo `*_secret_ref` **sulla tabella C4** (`fiskaly_accounts`, C4-2) — il valore non transita mai per una tabella INFRA |
| `bathhouse_email_configrations` | `smtpPassword` | password SMTP: solo `smtp_password_secret_ref` **sulla tabella C6** (`notification_email_channel_settings`, C6-5) |
| `switchboard_config` | `sip_password` | password SIP: solo `sip_password_secret_ref` **sulla tabella C7** (`switchboard_configs`, C7-3) |
| `CHATBOT_MANAGER_APIKEY` / `_SECRET` (env var, non a DB) | — | segreti del provider LLM: mai stati a DB, restano in `appsettings`/secret manager; nessuna tabella C8 li porta |
| `stripe_configrations` | — (nessun segreto in tabella già nel legacy) | l'OAuth Connect Stripe non conserva un refresh token qui; la tabella (senza segreti) è comunque di dominio **C3** → [`payment_gateway_connected_accounts`](payment/payment_gateway_connected_accounts.md), non INFRA |

### Domande aperte INFRA (in [`_domande-committente.md`](_domande-committente.md))

- **[D-56]** 🟨 Scope granulari per client API oltre `app_name`/`property_id`: non modellare ora
  (nessun consumo reale nel codice), tabella figlia solo se un partner futuro lo richiede.
- **[D-57]** 🟨 Seminare l'enum `integration_app` con `teamsystem` (mai attivato, 0 righe) fin da ora,
  come [D-55] per il chatbot.
- **[D-58]** 🟨 `expires_at` finalmente reale: resta opzionale (default proposto) o diventa obbligatoria
  per i nuovi client?

---
---

# Dominio Privacy — `privacy`

Modulo **`privacy`**, nessuna sottoarea — modulo **sottile** di orchestrazione GDPR
([`docs/11-modulo-privacy.md`](../../progettazione/11-modulo-privacy.md)), non un dominio applicativo come C1…C8:
**non possiede mai un dato personale** di cliente/utente (quelli restano nello schema del modulo
proprietario, che ne governa il ciclo di vita per la propria logica di business). Orchestra via eventi
(process manager/saga in-process, non transazione atomica) i workflow **cross-modulo** che per costruzione
non possono stare in un solo schema: cancellazione (Art. 17), accesso/portabilità (Art. 15/20), retention
programmata, sospensione per contenzioso, audit di conformità (Art. 5.2).

Nessuna tabella esistente vi si sposta: `identity_account_contract_documents`/`_contract_acceptances`
(C5) restano in C5 — registro legale B2B con propria logica di dominio, non parte del ciclo GDPR-cliente
(doc §2). Il fatto che una tabella contenga dati personali non la rende un candidato allo spostamento: la
rende una **riga** nella policy dichiarativa di retention (`privacy_retention_policies`), eseguita dal job
del modulo che già la possiede.

*(Il modulo `privacy` non è elencato in [`docs/03-gestione-dati.md` §3.1bis](../../progettazione/03-gestione-dati.md)
— stessa situazione già annotata per la sottoarea `account` di C5. Da recepire in §3.1bis.)*

## Nucleo (nessuna sottoarea)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[privacy_data_subject_requests](privacy/privacy_data_subject_requests.md)** | *(nuova)* — il legacy cancellava con un soft-delete anonimizzante silenzioso, senza richiesta tracciata | testata di una richiesta Art. 15/17/20 (cancellazione o export, discriminate da `request_kind`) | **sì** (endpoint GDPR) | `public_id`, `(subject_kind, subject_id)`, `(status)` parz. attivi, `(due_at)` parz. attivi | testata **unica** invece dei due aggregati `ErasureRequest`/`ExportRequest` del documento (stesso meccanismo, §5) — [D-59]; mai un dato personale del soggetto, solo `subject_kind`+`subject_id` per valore |
| **[privacy_data_subject_request_module_outcomes](privacy/privacy_data_subject_request_module_outcomes.md)** | *(nuova)* | esito per **modulo coinvolto** in una richiesta (`erased`/`anonymized`/`retained_*`/`exported`/…) | no | `(request_id, module_code)` UNIQUE, `(outcome)` parz. `pending` | 1 riga per (richiesta × modulo); consolida lo `status` della testata |
| **[privacy_retention_policies](privacy/privacy_retention_policies.md)** | *(nuova)* — il legacy non ha mai avuto retention dichiarativa | 1 regola: `schema.tabella`, campo/i, data di riferimento, durata, azione, base giuridica | **sì** (pannello revisione DPO) | `public_id`, `(schema_name, table_name, field_names)` unico parz., `(schema_name, table_name)` parz. attive | **letta**, mai eseguita da qui (ogni modulo esegue sul proprio `DbContext`); `is_dry_run_only` obbligatorio prima dell'esecuzione reale; ambito **piattaforma**, non per-property — [D-62] |
| ✅ **[privacy_retention_runs](privacy/privacy_retention_runs.md)** 🆕 | *(nuova)* — il legacy non ha mai cancellato ne' archiviato nulla | 1 policy valutata in 1 corsa: righe trovate, righe toccate, dove ha archiviato, errore | **si** (pannello SystemAdmin/DPO) | `public_id`, `(run_id, schema_name)`, `(finished_at) WHERE status = 'failed'`, `(policy_id, finished_at)` | il verbale dei job di retention. Serve a tre cose che nessun'altra tabella copre: ritrovare le righe archiviate su storage, raccogliere gli errori senza fermare il motore, e leggere il report di dry-run prima di autorizzare l'esecuzione reale. Da non confondere con `privacy_compliance_audit_logs`, che e' per riga. ✅ **Applicata il 2026-10-03** (`20261003101352_AddPrivacyRetentionRuns`) |
| ✅ **[privacy_file_deletions](privacy/privacy_file_deletions.md)** 🆕 | *(nuova)* — il legacy non ha nessuna coda di cancellazione, ed e' il motivo per cui sul bucket ci sono ~12.000 orfani | 1 oggetto di storage da rimuovere: cosa, perche', quando deciso, quando eseguito, quanti tentativi | **si** (pannello SystemAdmin/DPO) | `public_id`, `(decided_at) WHERE executed_at IS NULL` (la coda), `(storage_origin, storage_path)` unico parz. (niente doppio accodamento) | indispensabile prima di cancellare qualunque file: una DELETE su S3 **non partecipa** alla transazione del database. Agisce solo sulla copia AWS. ✅ **Applicata il 2026-10-03** (`20261003075631_AddPrivacyFileDeletionsQueue`), nasce vuota: i job non sono scritti |
| **[privacy_legal_holds](privacy/privacy_legal_holds.md)** | *(nuova)* | flag che sospende una cancellazione automatica per contenzioso in corso | **sì** (pannello legale) | `public_id`, `(subject_kind, subject_id)` parz. attivi, `(module_code)` parz. attivi | impostato **solo** da ufficio legale/amministrativo, mai da un job; interrogato da ogni job di retention prima di agire |
| **[privacy_compliance_audit_logs](privacy/privacy_compliance_audit_logs.md)** | *(nuova)* | **registro append-only** di ogni azione di cancellazione/anonimizzazione/export eseguita, riportata dai moduli proprietari | no | `(module_code, occurred_at DESC)`, `(schema_name, table_name, record_id)` | prova di conformità (Art. 5.2); retention propria **1-2 anni** (più corta della testata richieste, ~10 anni) |

### Domande aperte Privacy (in [`_domande-committente.md`](_domande-committente.md))

- **[D-59]** 🟨 Testata unica `privacy_data_subject_requests.request_kind` (`erasure`/`export`) invece dei
  due aggregati distinti `ErasureRequest`/`ExportRequest` del documento di indirizzo.
- **[D-60]** 🟨 Seminare `subject_kind` con `user` oltre `customer` fin da ora (GDPR self-service per
  staff/titolare, non ancora un requisito verificato).
- **[D-61]** 🟨 Bundle di export come file su object storage (`export_artifact_origin`/`_path`) invece di
  `payload jsonb` inline.
- **[D-62]** 🟨 `privacy_retention_policies` come configurazione **globale di piattaforma** (nessun
  `property_id`) invece che personalizzabile per stabilimento.

**In fase implementativa** (non bloccante): determinazione dei "moduli effettivamente coinvolti" per una
richiesta (query di scoping vs broadcast a tutti i moduli con risposta `not_applicable`); seed iniziale di
`privacy_retention_policies` dalla "tabella delle durate" di
[`docs/11-modulo-privacy.md` §6](../../progettazione/11-modulo-privacy.md) — molte righe `recommended_policy`, la
retention telefonica (D.L. 7/2015) resta `disputed`/`is_dry_run_only = true` finché non arriva un parere
legale specialistico (stesso dubbio già annotato per [D-51] su C7).

---
---

# Dominio Audit — `audit`

Modulo **`audit`**, nessuna sottoarea — registro **trasversale** "chi ha fatto cosa"
([`docs/86-modulo-audit.md`](../../progettazione/86-modulo-audit.md)). Come Privacy non è un dominio applicativo:
non possiede dati di business e non conosce il dominio degli altri moduli. Riceve solo integration event
`AuditEntryRecorded` (via outbox) e serve la pagina «Tutti i log» di Impostazioni, i registri di sicurezza
e quelli degli amministratori di sistema.

Nessuna tabella esistente vi si sposta: i log che servono a un modulo per funzionare
(`fiskaly_receipt_logs`, `payment_webhook_events`, `notification_deliveries`, `switchboard_*`) e le prove
legali di dominio (`identity_account_contract_acceptances`, `privacy_compliance_audit_logs`) restano dove
sono — regola e valutazione in [`docs/86-modulo-audit.md` §1-§3](../../progettazione/86-modulo-audit.md).

## Nucleo (nessuna sottoarea)

| Tabella | Sostituisce (legacy) | In una riga | `public_id` | Indici chiave | Note |
|---|---|---|---|---|---|
| **[audit_trail_entries](audit/audit_trail_entries.md)** 🆕 | *(nuova)* — il legacy non ha alcun registro delle azioni (`booking_logs` è un contatore) | 1 azione: chi, quando, cosa (`action_code`), su quale oggetto, in quale stabilimento, con etichette fotografate | no | PK `(id, occurred_at)`, `(source_event_id, occurred_at)` UNIQUE, `(property_id, occurred_at DESC, id DESC)` parz., `(property_id, actor_user_id, occurred_at DESC)` parz., BRIN `occurred_at` | partizionata per mese; append-only, utente applicativo con solo `INSERT`/`SELECT`; retention per categoria (`security` 6 mesi, `platform` 12 mesi, `operator` 12 mesi, D-66 chiusa) |

### Domande aperte Audit (in [`_domande-committente.md`](_domande-committente.md))

- **[D-66]** ✅ Retention delle azioni degli operatori: 12 mesi (2026-10-13).
- **[D-67]** 🟧 Informativa al personale (art. 4 Statuto dei Lavoratori) per la pagina «Tutti i log».

---

## Mapping tabelle: legacy → nuovo (colpo d'occhio)

### C5

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `users` | `identity_access_users` (+ `identity_account_subscriptions`, `identity_access_property_security_settings`; `affiliate_selected_locations` → `resource_scopes text[]`) | split per responsabilità; campi morti/cross-dominio rimossi; FK reali |
| `roles` | `identity_access_roles` | + `UNIQUE(code)`; seed in migration; etichette da risorse `.resx` |
| `user_roles` | *(assorbita)* → `identity_access_users.role_id` | N:N → 1:N; account multi-ruolo → `superadmin` (**confermato committente**) |
| `licenses` | `identity_account_license_plans` (+ `_translations`) | + `UNIQUE(code)`; etichette multilingua in figlia |
| `contract_documents` | `identity_account_contract_documents` | versionamento immutabile; URL scomposto |
| `contract_acceptation` | `identity_account_contract_acceptances` | 1 riga sovrascritta → registro append-only |
| `customers` | `identity_customer_customers` | telefoni consolidati; `referenceId`→FK; JSON eliminati |
| `property_like_logs` | `identity_customer_bathhouse_likes` | + unicità; soft-delete; `bathHouseId`→`bathhouse_id` (uuid, diretto) |
| *(nessuna)* | `identity_access_refresh_tokens`, `identity_access_user_auth_challenges`, `identity_account_subscription_periods`, `identity_account_license_plan_translations` | nuove |

### C2

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `properties` | `catalog_properties` (+ `catalog_property_online_booking_settings` & figlie, `catalog_property_service_booking_settings`) | split; campi cross-dominio rimossi (`fiskaly*`→C4, `commissionAmount`→C3, 2FA→C5); JSON→colonne/tabelle |
| `bathouses` | `catalog_bathhouses` (+ `catalog_bathhouse_photos`, `catalog_bathhouse_amenities`) | N:1 con property; `slug` 1 indice (era 60); `services`/`photos` de-JSON; geo→`numeric`; stub (~98%) non migrati |
| `online_reservations` | *(non migrata)* → confluisce in `catalog_property_online_booking_settings` | relitto (16 righe); colonne carta eliminate (PCI) |
| `configure_categories` | `catalog_map_icon_categories` | + FK reale da icone |
| `configure_images` | `catalog_map_icons` | `category`/`type` stringa→FK/enum; `image` URL→`media_asset` |
| `sectors_rows` | `catalog_map_sectors` | 1 blob JSON → N righe con **id stabile**; riferimenti "per nome" → FK |
| `configure_maps` | `catalog_map_layouts` (+ `_special_icons`) | 22 colonne icona→FK+figlia; `items`→`grid_columns`; `specialCabin` scartato |
| `configurations` | `catalog_map_spots` (parametri sharing → **C1**) | blob `beachMapData` (670 KB, 21 forme) → 1 riga per postazione; `items.name`→enum |
| `seasons_periods` | `catalog_pricing_seasons` + `catalog_pricing_tariff_periods` | scalari + array `periods`/`selectedRanges` → tabelle |
| `price_lists` | `catalog_pricing_price_lists` (ex `_packages`) + **7 figlie** + lookup metodi | 6 JSON → relazionale; `priceCalculation` 8 varianti→lookup; `enableOnline*`→`sales_channel`; `edit_price_data` (giorni 1–90)→`_stay_prices`; `packageName` morto scartato |
| `discounts` | `catalog_pricing_discounts` + `_date_ranges` + `_sectors` | 1 blob doppio-incapsulato → 1 riga per voucher; contatore atomico server-side |
| `revenue_notifications` | `catalog_pricing_revenue_settings` + `_discounted_spots` | **rimane C2** (non C6); `discounts` (colonna morta) non migrata; `+ UNIQUE(property_id)` |
| `services` | `catalog_service_amenities` (+ `_translations`) | rinominata `amenities`; `serviceFilterType`→`category_id` FK; `serviceIcon`→`media_asset`; `serviceName`/`serviceItalian`→`name` + figlia traduzioni; `isChecked` scartato |
| `service_type_filters` | `catalog_service_amenity_categories` (ex `_amenity_filters`) | + `code`; `filter_id`→`category_id`; refuso corretto; + traduzioni |
| `servicetypes_icons` | `catalog_service_type_icons` | `type`→enum; `icon`→`media_asset`; `category` morta scartata |
| `services_types` | `catalog_service_sale_items` (+ `_weekdays`, `_time_slots`) | 3 array JSON (≤4 MB) → 1 tabella con `kind` + 2 figlie |
| `warehouses` | `catalog_inventory_items` | `items` array → righe; stringhe→interi; consumi per data → **C1** |
| `hotels` | `catalog_agreement_partners` | `isDeleted`→`deleted_at`; ownership già ok |
| `hotel_price_lists` | `catalog_agreement_price_lists` + 3 figlie | come `price_lists`; `packageName` **qui migrato**; voucher fields |
| `country` | `catalog_countries` (+ `_translations`) | + ISO `code`; etichette→figlia; timestamp doppioni ripuliti |
| `property_search_regions` | *(non migrata)* | verificata non usata (0 consumer FE); feature "in evidenza" resta hardcoded ([D-21]) |
| *(nuove)* | `catalog_media_assets`, `catalog_map_legend_items` | supporto file per il modulo; ex `legendList[]` |

### C1

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `bookings` (testata) + `service_reservation` (testata) + `booking_payments` / `service_reservation_payments` (riepilogo) | `booking_reservations` | **testata unica** (discriminatore `reservation_kind`); N righe stesso numero → 1; JSON `booking_payments` → colonne `subtotal`/`discount`/`surcharge`/`total`/`deposit`/`paid`/`balance_due` + `price_override`; enum per `status`/`bookingByAdmin`/`is_booking_confirmed`/`bookingType` (→ `listing_type` `agreement_*`); `hotelId` → `agreement_partner_id`; 3 flag prezzo → `price_flags enum[]`; numeri generati server-side; movimenti/commissione/`isMarketingCampaign` → C3/C6 |
| `bookings` (singola riga) + JSON `booking_locations` / `packageSelectedData` | `booking_lines` (+ `booking_line_additional_items`) | 1 riga per postazione×tratta×fascia; `Location`/`packageId`/`currentRow` stringa → FK uuid (`catalog_map_spots`/`_pricing_price_lists`/`_map_sectors`) + `*_snapshot` (etichette congelate); griglia prezzi 1–90 gg non persistita; check-in/out per riga |
| `bookings.booking_discount` / `additionalAutoVoucher` / `service_reservation.discountData` (JSON) | `booking_discounts` | snapshot voucher → colonne + `discount_id uuid` (C2); contatore utilizzi atomico su C2 |
| `bookings.guest_data[]` / `guest_ids[]` (JSON) | `booking_guests` | 2 array → 1 tabella; FK viva C5 + `full_name_snapshot` |
| `bookings_requested_details` | `booking_participants` | rinominata (non "richieste"); `UNIQUE(reservation_id, user_id)`; `user_id uuid` (C5) |
| `booking_logs` | `booking_number_sequences` | rinominata (non un log); ~232k righe → 1 per property; contatore `bigint` zero-padded, `FOR UPDATE`; `UNIQUE(property_id)` |
| `booking_number_informations` | *(spezzata)* → `booking_quick_action_dates` (C1) + testata C1 (`payments*`) + **C4** (`details_payments*`) + **C3** (`ofsale_payments`/`teamsystem_payment`) | tabella "sacco" multi-sorgente disaggregata per dominio |
| `booking_items_counts_details` + `additional_items_counts_details` + `services_booking_counts` + `hourly_service_booking_management` (conteggio) + `location_reservations` + `booking_areas` + parte "consumo" di `booking_absence_details`/`sharing_details` | `booking_resource_consumptions` (+ `booking_resource_availability_daily`) | 6 tabelle → 1 (`docs/03 §3.8`); `booking_element` stringa → `resource_type enum` + `resource_id uuid`; `status`(=`payment_status`) → `state` dedicato; 1 riga **per giorno**; `quantity>0`; exclusion constraint GiST per la concorrenza (`§3.9`); aggregato mantenuto dal DB |
| `bookings.servicesData` / `service_reservation.servicesData` (JSON) + `services_booking_counts` (riga vendita) | `booking_service_lines` (+ `booking_service_time_slots`) | blob con base64/costanti UI → `sale_item_id uuid` + scalari; `service_type` drift → enum `bar`/`normal`/`hourly`; niente 98% righe a `quantity 0`; slot `HH:MM` stringa → `time` + `time_slot_id uuid` |
| `location_reservations` + `hold_locations` | `booking_spot_holds` | 1 riga per intervallo (non per giorno); `locationId`/`locationData` → `resource_id uuid`; `held_until` server-side + job; `freeLocationReservation` `DELETE` non-scoped → soft-release scoped; `hold_locations` (0 righe) nulla da migrare |
| `booking_absence_details` + parte "offerta" di `sharing_details` (+ JSON `booking_absence_dates`) | `booking_resale_offers` | due tabelle + 1 JSON → una (`origin` `absence`/`sharing`/`absence_as_sharing`); `status`+`isBooked` (33 righe reali disallineate) → `offer_state`; `location` stringa → `resource_id uuid`; `email_sent` → C6 |
| campi `number_of_credit`/`credit_value` di `sharing_details` + cron `sharingCreditCron` | `booking_resale_credit_ledger` | ledger append-only; accredito **incrementale** (era assegnazione su `users` per email); `credit_value` int da regex → `numeric(12,2)`; evento verso C3 (borsellino) |
| 7 campi "sharing" di `configurations` | `booking_resale_settings` | campi cross-dominio C2→C1 (`docs/03 §6`); `credit_value` testo sporco → `credit_calc` enum + `credit_amount numeric`; `+ UNIQUE(property_id)` |
| `booking_groups` (model `booking_group`) | `booking_groups` + `booking_group_members` | `name` unico globale → per stabilimento; `status` → `deleted_at`; `groupBookings` array → giunzione con `UNIQUE(reservation_id)` |
| `hotel_payments` | *(di confine)* → `booking_agreement_lines` (C1: voce + snapshot prezzo) + **C3** (`amount`/`paymentStatus`/`paymentMethod`) | nomenclatura `hotel*`→`agreement*` (allinea a C2); `mainPrices`/`detailPrices` doppio-encoded → `applied_price` + FK listino C2; `priceCalculation` → lookup; solo righe vive (71% erano `isDeleted`); `checkVoucherLimit` reale |
| `booking_absences`, `parent_bookings`, `booking_areas`, `hold_locations`, `bookin-absences`, `sharings`, `*_archive_03072026`, `booking_items_counts*`, `bookings_old`, `booking` | *(non migrate)* | dismesse / dead code / archivio / scratch — assorbite dove serve (`booking_areas` → `is_area` + consumptions; `hold_locations` → `booking_spot_holds`) |
| *(nuove)* | `booking_resource_availability_daily`, `booking_resale_credit_ledger`, `booking_resale_settings`, `booking_number_sequences`, `booking_quick_action_dates` | proiezione disponibilità; ledger crediti; config sharing; contatore numero; date planner |

### C3

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `stripe_configrations` | `payment_gateway_connected_accounts` | refuso nome + `camelCase`→`snake_case`; `UNIQUE(property_id)`; upsert per property (bug `where id=userId`); `+ onboarding_state`/capability da webhook; `destroy`→soft-delete + evento a C2 |
| `properties.commissionAmount` (JSON) + liste hardcoded nel bundle FE | `payment_gateway_commission_settings` | campo cross-dominio **C2→C3**; JSON→2 `numeric`; fee `+0.5` cablata→colonna; esenzioni hardcoded→`exemption` enum; calcolo commissione **server-side** |
| `user_stripe_card_details` (0 righe) + `users.stripeCustomerId` | `payment_customer_gateway_profiles` | **da rifare da zero**; model≠schema corretto; nome `stripeCustomerCardId` (è un *Customer*)→`stripe_customer_id`; `token` morto scartato; solo `cus_…` (non `acct_`); mai PAN |
| `online_transaction_details` | `payment_transaction_charges` | refuso "Taransaction"; importi `varchar`→`numeric`; `status` casing→enum interno + `stripe_status` grezzo; `booking_type`→`subject_kind`; `payment_type`/`isIframe`→enum; `pi_`/`ch_` distinti; `sync({alter})`→migrazioni; + indici (erano full scan); idempotenza webhook su `event.id` |
| `refund_transaction_details` (0 righe, `refundPayment` duplicata e rotta) | `payment_transaction_refunds` | un solo percorso corretto; `amount` centesimi→euro `numeric`; **+ rimborso parziale** (`original_amount`/`reason`); `+ status`/`failure_reason`; `charge_id` FK risolve booking **e** service reservation |
| `bookings_send_payment_summaries` (fetta C3) | `payment_request_links` | `amount` euro int→`numeric`; 3 flag stato→`status` enum unico; `method_flag`/`email_sent`→**C6**; `booking_cancel_flag`→`cancel_reservation_on_expiry`; `isCallingFromBookingDetail`→`origin`; `+ charge_id` |
| `transactions_payments` + movimenti nei JSON `bookings.booking_payments`/`service_reservation.payments` + fetta economica `hotel_payments` | `payment_ledger_entries` (+ `payment_ledger_entry_lines`) | è `payment_transactions` di `docs/03 §4`; **append-only** (era destroy+recreate); `float` outlier→`numeric` bonificato; IVA 22% cablata→`vat_rate`; `payment_mode` drift→enum condiviso C1; `type` 3 codifiche→`entry_type` enum; scrittura in transazione + outbox; righe per articolo per l'export contabile |
| `card_transactions` | `payment_wallet_accounts` + `payment_wallet_transactions` (+ `payment_wallet_transaction_items`) | conto tessera esplicito (era `GROUP BY identifier`); `identifier` client→`card_number` server-side `UNIQUE`; saldo `SUM()`→`balance` materializzato; `type=1 amount<0`→`movement_type` enum + amount positivo; `debit_details` JSON→figlia; crediti sharing da `users` (per email, assegnazione)→wallet `resale_credit` incrementale |
| `teamsystem_expenses` (0 righe) + `booking_number_informations.ofsale_payments`/`teamsystem_payment` (`NULL`) | `payment_pos_charges` + `payment_pos_charge_items` | `idRef bigint` vs UUID (causa tabella vuota)→`external_ref` + `UNIQUE`; `payload` JSON→colonne + righe; `amount` triplo→`total_amount` con vincolo; `fiscalized` morta scartata; `payload.app` fisso→`pos_system` enum; `registrationId` "rappresentativa"→`reservation_public_id` diretto |
| `keysecret` | *(non migrata → INFRA)* | credenziali partner → secret manager + [`infra_integration_api_clients`](infra/infra_integration_api_clients.md) senza segreto; serve C3 ma non è tabella di dominio C3 |
| `parent_bookings` | *(non migrata)* | dead code; "pagamento multi-prenotazione" da progettare ex novo se servirà ([D-37]) |
| *(nuove)* | `payment_gateway_commission_settings`, `payment_customer_gateway_profiles`, `payment_request_links`, `payment_ledger_entries`, `payment_ledger_entry_lines`, `payment_wallet_accounts`, `payment_wallet_transactions`, `payment_wallet_transaction_items`, `payment_pos_charge_items` | gateway commissioni; carte salvate; link di pagamento; libro incassi (testata + righe); borsellino (conto + movimenti + articoli); righe scontrino POS |

### C4

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `fiskaly_account` + `properties.fiskalyEnabled`/`fiskalyAutoreceipt`/`fiskalyFlag` | `fiskaly_accounts` | consolidamento (C4-3): 4 flag in 2 tabelle, già incoerenti nei dati → `terms_accepted_at`+`is_enabled`+`autoreceipt_enabled` (catena) + `fiscal_setup_level` enum (`fiskaly_active` derivato); segreti → `*_secret_ref` secret manager (C4-2); `*_state`/`*_mode`→enum; `decommissioned_at`→`deleted_at`; `+ UNIQUE(property_id)` |
| `fiskaly_cash_register` | `fiskaly_cash_registers` | `system_state`/`mode`/`type` `varchar`→enum; `is_software` autorevole (era match per stringa); `+ property_id`; `+ UNIQUE` default per account; `deleted_at` già ok |
| `fiskaly_receipt` + `fiskaly_non_fiscal_receipt` | `fiskaly_receipts` (+ `fiskaly_receipt_lines`, `fiskaly_receipt_payments`) | **testata unica** `document_kind` (C4-1); importi `varchar`→`numeric` (3 valori divergenti riconciliati); `record_state` enum interno + `fiskaly_state_raw` grezzo; `entries`/`payments`/`paid_deltas` JSON→figlie + `paid_allocation`; `e_receipt_path` URL→`fiskaly_media_assets`; `emailSent`/`whatsappSent`→C6; `fiskaly_version` (morta) scartata; `+ property_id`/`reservation_public_id`; snapshot venditore `seller_*_snapshot`; `fiskaly_response`→`jsonb` grezzo |
| `fiskaly_receipt_log` | `fiskaly_receipt_logs` | vocabolari `varchar`→enum; `fiskaly_error_message varchar(50)`→`text`; `fiskaly_log_messages`→`jsonb`; `createdBy`→`actor_user_id uuid`; `updatedAt` scartato; **no `deleted_at`**; retention |
| `fiskaly_receipt_sequence` | `fiskaly_receipt_sequences` | 2 implementazioni con fusi diversi→1 upsert atomico, giorno Europe/Rome; `system_id`→FK interna `cash_register_id`; `+ property_id`, `+ timestamp` |
| `booking_number_informations.details_payments*` | *(input di `fiskaly_receipt_lines`)* | non una tabella: righe fiscali calcolate dal pre-conto → alimentano le righe scontrino; la tabella "sacco" è spezzata per dominio (C1/C3/C4) |
| *(nuove)* | `fiskaly_vat_codes` (lookup), `fiskaly_media_assets` | codici IVA/Natura da costanti JS → lookup; supporto file PDF per il modulo |
| `fiskaly_account.subject_credential_*` / `taxpayer_fisconline_pin` / `taxpayer_fisconline_password` | *(non migrate → secret manager)* | credenziali reversibili (rimandate in chiaro a Fiskaly) → AWS Secrets Manager / SSM; in tabella solo `*_secret_ref` |

### C4bis

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `printer_configs` | `fiscalprint_printer_configs` (+ `fiscalprint_printer_departments`) | target **ridisegnato** (C4bis-1): `isActive`+`isdeleted` scollegati→`is_active` + `deleted_at`; `model`/`mode` `varchar`+refusi→enum; `port` con IP/USB→`connection_kind` + `port int`/`usb_device`; `SSL`→`use_tls`; `department_1/2/3` `"22%"` posizionali→figlia (`dept_number`, `vat_rate numeric`, N reparti); `userId`→`created_by_user_id uuid` |
| `booking_receipts` | `fiscalprint_receipts` | `receipt_number`/`document_no` (`"001"` bug)→`integer` da sequenza; date stringa→`printed_at timestamptz`; `receipt_object`→`device_response jsonb` (passthrough) + `rt_*` derivate; `printer_request/response_object` (sempre `NULL`) scartati; `bookingId` (sempre `NULL`)→`reservation_public_id` nullable; `+ public_id` |
| `receipt_serial_numbers` | `fiscalprint_serial_numbers` | non incrementa mai (2 bug)→upsert atomico; contatore globale-per-data→**`(printer_id, fiscal_date)` UNIQUE** (serie per dispositivo RT; la tabella legacy non aveva `printerId` → storico non migrato); `varchar(255)`→`date`/`integer` |

### C6

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `email_logs` (+ flag `*Sent` di C1/C3/C4, invio `hotel_reports`) | `notification_deliveries` | outbox+audit unico per destinatario; `status 0/1`→enum con stato reale + retry/backoff; fallimenti tracciati; `emailType`→`kind` enum + `subject_snapshot`; `propertyId` obbligatorio; `attachments` JSON→`has_attachments`; `body`→`rendered_body` (troncato 90 gg); `cronStatus` non migrata |
| `custom_templates` + `hotel_message_templates` *(unificate)* | `notification_templates` (+ `notification_template_attachments`) | 3 sintassi segnaposto→`{{ mustache }}`; render browser→render server Razor; `type 1/2`→`channel` enum; `status 0/1`→`deleted_at`+`is_active`; `isDefault` atomico + UNIQUE parz.; `attachment` array→giunzione ordinata; vocabolario segnaposto **chiuso** |
| `bathhouse_email_configrations` | `notification_email_channel_settings` | `smtpPassword` in chiaro→`smtp_password_secret_ref` (secret manager); `secure:true` cablato→`security` enum; `status`→`is_active`+`deleted_at`; `+ UNIQUE(property_id)`; `+ is_verified`; `email_configrations` (0 righe) non migrata |
| `notification_details` | `notification_inapp_messages` (+ `notification_inapp_message_reads`) | testo concatenato in 6+ punti→`kind` enum + `payload jsonb` (snapshot) + render a template; `type 1/2`→`subject_kind`; `status 1/2`→`visibility`; `read` per property→stato-lettura **per utente**; `user_id`/`bookingNumber '0'` non migrati; `reference_id`→`subject_public_id` uuid; retention 6–12 mesi |
| `contact_list` / `contact_list_member` | `notification_marketing_contact_lists` / `_contact_list_members` (+ nuova `notification_marketing_contacts`) | `contactId` (`customers.id`, 13% `NULL`)→FK a `notification_marketing_contacts` (chiave `(property_id,email)` + **consenso**); `status` colonna morta→`deleted_at`; churn destroy+recreate→add/remove puntuali; `+ UNIQUE(list_id,contact_id)` |
| `subscribes` | `notification_marketing_newsletter_subscriptions` (+ `notification_marketing_suppressions`) | `status 1/0`→enum + **double opt-in** + `unsubscribe_token`; `+ UNIQUE(email)`; `+ source` enum; opt-out→lista di soppressione **a vita**; rotte sotto `systemadmin` |
| `marketing_email_campaign` | `notification_marketing_email_campaigns` (+ `notification_marketing_campaign_recipients`) | `state ENUM`→enum unificato + `deleted_at`; `recipientList` JSON→risoluzione dalla lista all'invio + figlia recipients (retention 6 mesi); `message` inline→`template_id`; `scheduledDate`+`scheduledTime` doppio shift→`scheduled_at`+`timezone`; esito per destinatario; indice su `state` |
| `marketing_automation` | `notification_marketing_automations` (+ `notification_marketing_automation_triggers`) | `name` in `switch`→`automation_kind` enum; `state ENUM` (mai `sent`)→enum unificato `active/paused`; `scheduleValue` JSON polimorfo→`schedule_type`+`days_offset`+`send_at_time` + figlia trigger; `textFields`→`template_id`; N query/destinatario ogni 5 min→eventi di dominio C1 |
| `marketing_event` | `notification_marketing_events` | ricorrenza materializzata in N righe→**regola** su 1 riga; `repeatType`/`repeatUnit` `ENUM`→enum PG; `repeatInterval 0`→`CHECK(>=1)`; `startHour/endHour varchar`→`time` + `is_full_day`; `showOnEstablishmentPage`→`is_public` **usato davvero**; `coverImage` URL→FK asset; `status`→`deleted_at` |
| `marketing_event_notification` (0 righe) | `notification_marketing_event_reminders` *(se non dismessa)* | nessun mittente nel legacy; `notificationType`/`status` `ENUM`→enum; `notificationDate`+`notificationTime`→`scheduled_at` calcolato; `+ FK ON DELETE CASCADE`; **da completare o dismettere** [D-47] |
| `csv_manager_details` | `notification_establishment_claim_requests` *(candidata a dismissione)* | nome legacy (`csv_owner`) rinominato; `bathhouseId` letto ma non salvato→`bathhouse_public_id` + snapshot nome; `+ status` enum lavorazione; `message varchar(255)`→`text`; invio all'admin = riga `notification_deliveries` (non email hardcoded) |
| `users.notifyEmail`/`notifyWhatsapp`/`disableNotification` | `notification_user_channel_preferences` | 3 booleane→`enabled_channels` **`enum[]`** + `is_muted`; `disableNotification` ora **rispettato** dal modulo |
| `users.deviceTokens` (array) | `notification_push_device_tokens` | array senza metadati→1 riga/token con `platform` enum, `last_seen_at`, `is_active`; purge token stantii; disattivazione su `UNREGISTERED` |
| — *(nuova)* | `notification_media_assets` | catalogo file **per modulo** (`origin`+`path`); assorbe URL S3 in colonna di `custom_templates.attachment`, `marketing_*.attachments`, `marketing_event.coverImage`, `hotel_reports.path` |

### C7

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `switchboard_config` | `switchboard_configs` (+ figlie `switchboard_voice_number_registrations`, `switchboard_voice_softphone_devices`) | hub 1/property; `sip_password` in chiaro→`sip_password_secret_ref` (secret manager); `reg_*` KYC→figlia cifrata con accesso `systemadmin`+audit; `device_fcm_token`/`device_voip_token` (2 colonne, 1 device)→figlia 1 riga/device; `whatsapp_business_account_id`/`telnyx_webhook_url`/`sip_domain` non migrati; `is_active smallint`→`boolean`; modalità IVR implicita→`routing_mode` enum; timeout cablati→colonne; indici unici ridondanti→uno |
| `switchboard_audio_file` | `switchboard_media_assets` | tabella media **per modulo** (come `catalog_media_assets`); `s3_key`→`origin`+`path`; `s3_bucket` (mai riletto) non migrato; `mime_type` costante→default; `+ checksum`/`byte_size`/`duration_ms`; endpoint proxy pubblico (`id` numerico)→URL firmato TTL breve; `deleted_at` + GC disaccoppiato |
| `switchboard_menu_item` | `switchboard_voice_menu_items` | `+ UNIQUE(config_id,parent_id,dtmf_key)` (mancava, IVR sceglieva arbitrario); `is_active smallint`→`boolean`; `dtmf_key varchar(2)`→`varchar(1)`+`CHECK([0-9*#])`; profondità implicita→`depth`+`CHECK`; `position`→`sort_order` (solo-UI) |
| `switchboard_menu_action` | `switchboard_voice_menu_actions` | `action_config json` polimorfo→colonne tipizzate (`hangup`/`forward_sip` = contratto morto; `send_whatsapp`→`wa_template_id` FK + `wa_template_name_snapshot` + `wa_language` + `wa_recipient_mode` enum + `wa_*_params jsonb`); `action_type` (già `ENUM`, whitelist duplicata)→enum PG; `CHECK` su `wa_template_id` |
| `switchboard_call_register` | `switchboard_voice_call_records` | [`docs/03 §2.4quinquies`]: da "registro povero" (`from`/`to`/`datetime`) a **tabella eventi di dominio** — `+ config_id`/`direction`/`outcome` enum/`forward_result` enum/`started_at`·`answered_at`·`ended_at`/`duration_seconds` (generata)/`ivr_path jsonb`; `to` stringa `sip:` (match esatto)→FK + `sip_destination` congelato; `+ public_id`; retention 12 mesi→🗑️/🔒 numero; endpoint softphone→rate limiting; idempotenza su `telnyx_call_session_id` |
| `switchboard_call_log` (0 righe, `SB_CALL_LOGGING_ENABLED`, nessun lettore) | `switchboard_voice_call_diagnostics` *(tabella di log — [`docs/03 §2.4quinquies`] suggeriva tracing; creata su decisione committente)* | `event_type` a doppia natura (`call.*`+`step.*`)→`event_source` enum + `event_name` stringa; `raw_payload` senza redazione→`jsonb` + numeri pseudonimizzati + omissibile; fire-and-forget→buffer + metrica scarto; retention 30–90 gg (opt-in per property); `+ call_record_id`; `deleted_at` esposto non usato |
| `switchboard_wa_template` | `switchboard_messaging_wa_templates` | distinta da C6 `notification_templates` (definizioni Meta); `meta_status ENUM(3)`→enum passthrough esteso + `meta_status_raw`; `+ UNIQUE(property_id,name,language)` (Meta lo impone); `components json`**resta `jsonb`** (documento sistema esterno, [`docs/03 §3.6 p.5`]) + validazione al write + colonne di riepilogo derivate; `category` (già `ENUM`)→enum PG; reconcile job per allineare la cancellazione su Meta |
| `switchboard_wa_msg_log` (0 righe, nessun lettore) | `switchboard_messaging_wa_event_logs` *(tabella di log — [`docs/03 §2.4quinquies`] indicava outbox C6; creata su decisione committente)* | webhook POST senza firma→`X-Hub-Signature-256` obbligatoria; `+ config_id` da `phone_number_id`; `message_body` (testo+bottone+dump media)→`body_text` + `body_payload jsonb`; `event_type`/`status`/`*_category`/`message_type` stringhe→enum (+ `unknown`); numeri pseudonimizzati; idempotenza `(wamid,event_type,status)`; retention 6–12 mesi; stati d'invio veri→correlazione con `notification_deliveries` (C6) |

### C8

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `chatbot_history` | `chatbot_conversation_turns` | `app`→`channel` enum; `created_by` nullabile→`actor_user_id` NOT NULL; esito dedotto da `output_text=''`→`status` enum; `id` esposto→`public_id`; rate limit su `updated_at`→`created_at`; `+ llm_provider_id`/`llm_model_snapshot`/`response_language` (nuovi) |
| `chatbot_manager_config` | `chatbot_manager_configs` | `max_call_per_day`→`daily_request_limit`; `max_returned_history`→`history_page_size_limit`; `+ llm_provider_id` (override per stabilimento); provisioning implicito→da rendere esplicito |
| strumento `queryDatabase` (archivio esterno prenotazioni) | *(eliminato)* | non migrato — vedi [`docs/26-chatbot.md` §8`](../../progettazione/26-chatbot.md) |
| *(nuova)* | `chatbot_llm_providers` | catalogo provider LLM selezionabili, sostituisce la mappa cablata `LLM_IMPLEMENTATIONS` |

### INFRA

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| `keysecret` | `infra_integration_api_clients` | `secret` in chiaro → **eliminato**, resta `client_secret_ref` (secret manager); `appName` varchar → enum `integration_app`; `apiKey` senza indice → `client_id` `UNIQUE`; `params.property_id` JSON → colonna tipizzata; `expiresAt` colonna morta → resa reale; `isEnabled`→`is_active`; `+ public_id`, `+ rotated_at` (nuovi) |
| `fiskaly_account.subject_credential_*` / `taxpayer_fisconline_*`, `bathhouse_email_configrations.smtpPassword`, `switchboard_config.sip_password`, `CHATBOT_MANAGER_APIKEY`/`_SECRET` | *(non migrati in una tabella INFRA)* | restano `*_secret_ref` sulla tabella del dominio proprietario (C4/C6/C7) o env var/secret manager puro (C8) — vedi tabella "Tabelle/campi legacy che confluiscono in INFRA" sopra |

### Privacy

| Legacy | Nuovo | Tipo di cambiamento |
|---|---|---|
| soft-delete anonimizzante silenzioso su `customers` (nessuna richiesta tracciata, nessuna email di conferma — [`da-chiarire.md` #47`](../../analisi-pre/business-logic/da-chiarire.md)) | `privacy_data_subject_requests` + `privacy_data_subject_request_module_outcomes` | comportamento implicito e non tracciato → workflow esplicito e auditabile (Art. 17), con esito per modulo coinvolto |
| *(nessuna — non esisteva)* | `privacy_retention_policies` | prima tabella di retention dichiarativa del progetto: "durate consigliate" da documentazione a configurazione interrogabile dai job |
| *(nessuna — non esisteva)* | `privacy_legal_holds` | prima tabella di sospensione cancellazione per contenzioso |
| *(nessuna — non esisteva)* | `privacy_compliance_audit_logs` | prima traccia di audit delle azioni di conformità dati |

---

## Decisioni del committente (recepite)

### C5

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| C5-1 | Ruoli senza frontend all'analisi iniziale (`user1`, `websiteadmin`, `systemadmin`) | Mantenerli tutti | seed 13 righe invariato. **Nota 2026-09-09**: `systemadmin` **non è obsoleto** — è stato aggiunto al repo il suo pannello `front-admin/` (React/Metronic, WIP) e **il nuovo back-office verrà riscritto da zero** (`front-admin/` = solo riferimento requisiti, non target). Restano candidati "superati" solo `user1` e `websiteadmin` |
| C5-2 | `advancedlifeguard` vs `lifeguard` | Sono diversi (`advancedlifeguard` = `lifeguard` + check-in/out) | sezione "Permessi per ruolo"; policy .NET con ereditarietà |
| C5-3 | Account con doppio ruolo (userId 199, 2885) | Rimuovere `manager`, tenere `superadmin` | mapping migrazione |
| C5-4 | Congelamento giorni abbonamento | Vecchio 365 gg "a consumo" dismesso; nuovi contratti a termine fisso; residui in migrazione | enum `term_type`; `remaining_days_balance` non decrementa in `suspended` |
| C5-5 | `bathHouse ↔ property` | **N:1 fin da ora** | tenant = `property_id`; dati consumer-granulari → `bathhouse_id` |
| C5-6 | Storico rinnovi abbonamento | Serve | nuova `identity_account_subscription_periods` (append-only) |

### C2

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| C2-1 | Perimetro iterazione | Tutto C2 in una volta | 44 schede (5 sottoaree + radice) |
| C2-2 | `properties` / `bathouses` | Radice del dominio C2 | `catalog_properties` (tenant root) + `catalog_bathhouses` (N:1); split verso C3/C4/C5 |
| C2-3 | `online_reservations` | **Option A** — tabella 1:1 dedicata + figlie; non migrata come tabella; canale servizi separato; 3 boolean d'asse; correzione a `docs/03 §5` | 5 tabelle radice per config online; colonne carta eliminate |
| C2-4 | `hotel_message_templates` | Escludi da C2 (va in **C6** `notification.templates`) | nessuna scheda C2 |
| C2-5 | `country` | Includi come lookup C2 (+ ISO `code` + traduzioni) | `catalog_countries` + figlia |
| C2-6 | `property_search_regions` | ~~Includi~~ → **verificata non usata, tolta** | nessuna tabella ([D-21]) |
| C2-7 | `revenue_notifications` | Includi in `pricing` | `catalog_pricing_revenue_settings` + figlia |

### C1

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| C1-1 | Riferimenti cross-modulo | Quando si referenzia un id di un altro modulo, usare il **`public_id` (uuid)**. Se il modulo già scritto non ha `public_id`, aggiungerlo | `property_id`/`customer_id`/`resource_id`/`price_list_id`/`discount_id`/`hotel_id`/… in C1 sono `uuid`. Aggiunto `public_id` a `catalog_map_sectors` e `catalog_service_sale_item_time_slots` (erano interni allo schema `catalog`) |
| C1-2 | Testata prenotazione | **Testata unica** `booking_reservations` per prenotazione-postazioni **e** prenotazione-solo-servizi, con discriminatore `reservation_kind` | evita di duplicare ~25 colonne comuni e le FK polimorfe sui satelliti; ~6 flag "spot-only" inerti per le righe `service`. Coerente con `docs/03 §4` |
| C1-3 | Riepilogo economico | `booking_payments` / `service_reservation_payments` (riepilogo) → **colonne sulla testata** (`subtotal`/`discount`/`surcharge`/`total`/`deposit`/`paid`/`balance_due`/`prevalent_payment_mode`) | niente tabella 1:1; i **movimenti** (`paidAmountData`/`depositList`/…) → C3. `paid`/`balance_due` mantenuti in C1 come proiezione da eventi C3 |
| C1-4 | Perimetro iterazione | Tutto C1 in una volta: nucleo prenotazioni, servizi in prenotazione, consumi & disponibilità (`§3.8`/`§3.9`), rivendita/hold + gruppi + convenzioni | 19 schede `booking_*` |
| C1-5 | Nomenclatura convenzioni | `hotel*` → `agreement*` in C1, ad allineare a C2 (`catalog_agreement_partners`/`catalog_agreement_price_lists`, [D-20]) | `booking_hotel_lines` → **`booking_agreement_lines`**; `hotel_id` → `agreement_partner_id`; enum `listing_type` → `agreement_price_list`/`agreement_voucher`; enum `agreement_line_type`. La prosa può continuare a dire "convenzione/hotel" |
| C1-6 | Flag di provenienza prezzo | `isBookingDoneBySuggestedPrice` + `isRecalculate` + `isEditedFinalPrice` (non esclusivi) → **`price_flags enum[]`** `{suggested_price_accepted, recalculated, manually_edited}`. Stesso approccio per `booking_resale_settings`: `sharing_seasonal_workers` + `enable_sharing_on_all_article` → `applies_to enum[]` `{seasonal_workers, all_articles}` | regola di esclusività `docs/03 §3.10`; `price_override IS NOT NULL` resta il segnale primario dell'override |
| C1-7 | Nomi importi + suffisso `_snapshot` | Importi/prezzi sulla testata **senza suffisso `_amount`** (`subtotal`/`discount`/`surcharge`/`total`/`deposit`/`paid`/`balance_due`, `appliedPrice`→`price_override`) — allineati a `docs/03 §3.7`. `_snapshot` **solo** sulle etichette/nomi congelati copiati da un'entità mutabile con FK viva accanto (`spot_label_snapshot`, `sector_label_snapshot`, `package_name_snapshot`, `name_snapshot`, `resource_name_snapshot`, `slot_label_snapshot`, `code_snapshot`); **niente suffisso** sui numeri/enum che sono dato proprio del documento-prenotazione (`line_price`, `unit_price`, `vat_rate`, `slot_price`, `applied_price`, `price_calculation`, discount `value`/`type`/`scope`) — la loro natura "congelata" è nella descrizione del campo | uniformato in tutte le 19 schede |

### C3

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| C3-1 | Perimetro iterazione | Tutto C3 in una volta: gateway (account + commissioni + carte salvate), transazioni + rimborsi online, link di pagamento, ledger incassi, borsellino, addebiti POS | 13 schede `payment_*` (6 sottoaree) |
| C3-2 | Confine con C1 | In C1 resta il **riepilogo economico** (colonne di testata `total`/`paid`/`balance_due`/`prevalent_payment_mode`, proiezione da eventi); in C3 i **movimenti** (`payment_ledger_entries`). Coerente con C1-3 e `docs/03 §4` | nessuna duplicazione; `PaymentRecorded`/`RefundIssued` (outbox) → C1 |
| C3-3 | Riferimenti cross-modulo | Come C1-1: id di altri moduli come **`public_id` (uuid)** copiato, mai FK cross-schema (`property_id`, `reservation_public_id`, `customer_public_id`, `agreement_line_public_id`, `*_user_id`). FK reali **solo** interne allo schema `payment` | tipo `property_id` `uuid`/`bigint` segue [D-01] |
| C3-4 | `keysecret` | Fuori da C3 → **INFRA** (secret manager + [`infra_integration_api_clients`](infra/infra_integration_api_clients.md) senza segreto). I POS di C3 lo consumano ma la tabella non è di dominio | nessuna scheda C3; annotato in "non entrano in C3" |
| C3-5 | Borsellino + crediti sharing | **Un'unica tabella** `payment_wallet_accounts` con `wallet_kind` enum (`prepaid_card` / `resale_credit`): saldi separati, `card_number` solo per la tessera fisica | `users.total_*_credit` (denormalizzati, per email) → wallet `resale_credit` incrementale via evento `ResaleCreditEarned` da C1 |
| C3-6 | Sottoarea `request` | I link di pagamento Stripe stanno in una sottoarea `request` **non prevista** da `docs/03 §3.1bis` (che elenca `gateway`/`transaction`/`ledger`/`wallet`/`pos`) — da recepire in §3.1bis | `payment_request_links` |
| C3-7 | Rimborso parziale | Introdotto (assente nel legacy: sempre full-refund) — `payment_transaction_refunds` con `amount`/`original_amount`/`reason` | abilita cancellazioni con penale / rimborso su singola giornata |
| C3-8 | Suffisso `_snapshot` (regola C1-7) | Applicato ai **nomi/etichette congelati copiati da un'entità mutabile con FK viva accanto**: `payment_ledger_entry_lines.description_snapshot` (FK `reservation_line_public_id`), `payment_wallet_transaction_items.description_snapshot` (FK `sale_item_public_id`), `payment_pos_charge_items.description_snapshot` (FK `sale_item_public_id`, nuova), `payment_transaction_charges.stripe_account_id_snapshot` (FK `connected_account_id`), `payment_ledger_entries.customer_{name,email,phone}_snapshot` (FK `customer_public_id`) | **Niente** suffisso su: numeri/enum propri del documento (`vat_rate`, `original_amount`, `stripe_status` grezzo, `balance_after`, importi), identificatori propri (`card_number`, `stripe_payment_intent_id`, `external_ref`), business key denormalizzate (`booking_number`, come C1), testo congelato **senza** entità/FK (`variants_description`, `payment_pos_charges.description`) |

### C4 — `fiskaly`

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| C4-1 | Testata scontrino fiscale + pre-conto | **Testata unica** `fiskaly_receipts` con discriminatore `document_kind` (`fiscal` / `non_fiscal`) | `createNonFiscalReceipt` riusa lo stesso builder → figlie condivise ([`fiskaly_receipt_lines`](fiskaly/fiskaly_receipt_lines.md), [`fiskaly_receipt_payments`](fiskaly/fiskaly_receipt_payments.md)), nessuna figlia polimorfa; ~8 colonne solo-fiscali `NULL` sulle righe `non_fiscal`, con `CHECK`. Coerente con C1-2 |
| C4-2 | Segreti Fiskaly/Fisconline | **Solo riferimenti** al secret manager: `fiskaly_accounts.subject_credentials_secret_ref` / `fisconline_credentials_secret_ref` (ARN/nome SSM), **zero materiale segreto** in tabella | i valori vanno **rimandati in chiaro** a Fiskaly a ogni chiamata → serve protezione *reversibile* (Secrets Manager / KMS), **non** un hash; la rotazione ("l'utente cambia PIN/password") = nuova versione del segreto, `fisconline_updated_at` registra il "quando" |
| C4-3 | Flag fiscali su `properties` | **Consolidati su `fiskaly_accounts`**: `terms_accepted_at` (data, non flag) + `is_enabled` + `autoreceipt_enabled` (invariante di catena `CHECK`) + `fiscal_setup_level` enum (`not_configured`/`vat_only`/`fiskaly_active`, con `fiskaly_active` **derivato** dal check `isSetupComplete`, non scrivibile) | `properties` non porta più campi di un dominio non suo ([`docs/03 §6`](../../progettazione/03-gestione-dati.md)); niente flag scrivibile che possa divergere dallo stato reale delle entità Fiskaly (nel legacy i 4 flag erano già incoerenti nei dati) |
| C4-4 | `entries` / `payments` / `paid_deltas` | JSON array → tabelle figlie tipizzate; `fiskaly_response` **resta `jsonb`** grezzo (passthrough SIGN IT, §3.6 punto 5) | unica eccezione "niente JSON" per il modulo; `paid_deltas` → `paid_allocation` per riga + storno append-only in **C3** |
| C4-5 | `property_id` cross-modulo | `uuid` (= `public_id` di `catalog_properties`) copiato per valore, come C1-1 / C3-3 | segue [D-01](_domande-committente.md); FK reali **solo** interne allo schema `fiskaly` |

### C4bis — `fiscalprint`

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| C4bis-1 | Profondità della modellazione | **Target pulito ridisegnato** (non migrazione 1:1) | sequenza reale `(property_id, fiscal_date)`→`last_number` atomica; `department_1/2/3`→figlia `fiscalprint_printer_departments`; `model`/`mode`/`connection_kind` enum; `port int`; `use_tls`; `deleted_at`; colonne morte (`printer_request/response_object`) non migrate |
| C4bis-2 | Mantenimento | **Si mantiene come componente separato** — decisione di business del committente, indipendente dalla copertura di Fiskaly (C4) o dal numero di stabilimenti che la usano | le 4 tabelle `fiscalprint_*` si creano regolarmente |

### C6 — `notification`

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| C6-1 | In-app "campanella" | **Redesign completo** (non migrazione del testo): evento (`kind`) + `payload jsonb` (variabili di rendering, snapshot) + render a template localizzato; stato-lettura **per utente** in figlia | `notification_details.notification_text`+`_it` eliminati; feed servito da indice parziale `(property_id, created_at DESC) WHERE visibility='visible'`; retention 6–12 mesi. `payload jsonb` = eccezione giustificata (snapshot chiuso per `kind`, analogo a [D-28] approccio A — [D-49]) |
| C6-2 | Outbox / log invii | **Tabella unica `notification_deliveries`** per messaggio×destinatario: `channel` enum, `status` reale (`queued→…→failed/bounced/skipped`), retry+backoff con `attempt_count`/`next_attempt_at` nella stessa riga (Polly). Assorbe `email_logs` e **tutti** i flag `*Sent` di C1/C3/C4/C2 | sull'entità di origine **non resta nulla** ([`docs/03 §6`](../../progettazione/03-gestione-dati.md)); nessuna tabella figlia "tentativi" (osservabilità via tracing); alimentata da coda/worker + pattern outbox nella transazione del comando |
| C6-3 | Contatti marketing + consenso | **Introdotta `notification_marketing_contacts`** (rubrica per-stabilimento, chiave `(property_id, email)`, FK opzionale a C5) con stato consenso + fonte + timestamp + `unsubscribe_token`; membri lista e destinatari campagna puntano al **contatto**; **lista di soppressione** (opt-out) a vita | risolve `contactId` = `customers.id` con 13% `NULL`; verifica consenso **al momento dell'invio** ([`docs/11`](../../progettazione/11-modulo-privacy.md)); **base giuridica/collocazione ancora da validare** ([D-46]) |
| C6-4 | Template unificati | **Un catalogo `notification_templates`** (assorbe `custom_templates` + `hotel_message_templates` + i corpi marketing), una sola sintassi `{{ mustache }}`, un solo renderer server (Razor), vocabolario segnaposto **chiuso** (enum + `.resx`, non tabella) | campagne/automazioni referenziano un `template_id` → gli allegati sono sempre del template ([`notification_template_attachments`](notification/notification_template_attachments.md)) |
| C6-5 | Segreti canale email | `bathhouse_email_configrations.smtpPassword` (in chiaro, condivisa, restituita da API) → **`smtp_password_secret_ref`** (secret manager, come C4-2); nessun materiale in tabella | `notification_email_channel_settings`; `+ UNIQUE(property_id)`, `security` enum usato davvero |
| C6-6 | Ricorrenza eventi marketing | La ricorrenza è una **regola** su 1 riga (`repeat_type`/`interval`/`unit`/`until`), **non** N righe materializzate | `notification_marketing_events`; occorrenze calcolate a runtime; `is_public` ora filtra davvero la proiezione verso la vetrina C2 |
| C6-7 | Feature marginali/incomplete | Create con annotazione **"candidata a dismissione"**: `notification_marketing_event_reminders` (0 righe, nessun mittente legacy — completare con scheduler o dismettere), `notification_establishment_claim_requests` (13 righe/3 anni — o solo invio email) | [D-47]; se dismesse, le tabelle non si creano e `notification_marketing_events.reminders_enabled` decade |
| C6-8 | `property_id` cross-modulo | `uuid` (= `public_id` di `catalog_properties`) copiato per valore, come C1-1 / C3-3 / C4-5; FK reali **solo** interne allo schema `notification` | segue [D-01](_domande-committente.md) |
| C6-9 | Campi ex `users.*` | `notifyEmail`/`notifyWhatsapp` → `notification_user_channel_preferences.enabled_channels` (`enum[]`); `disableNotification` → `is_muted` (ora **rispettato**); `deviceTokens` (array) → `notification_push_device_tokens` (1 riga/token) | [`docs/03 §6`](../../progettazione/03-gestione-dati.md); niente riga se le preferenze = default |

### C7 — `switchboard`

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| C7-1 | `property_id` cross-modulo | `uuid` (= `public_id` di `catalog_properties`) copiato per valore, come C6-8 / C4-5 / C3-3 / C1-1; FK reali **solo** interne allo schema `switchboard` | segue [D-01](_domande-committente.md) |
| C7-2 | Livello di ambizione | **Redesign pulito** (target ridisegnato, non migrazione 1:1), come C4bis — decisione presa quando si credeva la feature a **0 righe ovunque**; **verificato dopo** (2026-09-11) che è invece attiva e collaudata su `summer_booking_dev` con dati reali, il che **rinforza** la scelta del redesign pulito | 10 schede `switchboard_*` (hub + 5 `voice` + 2 `messaging` + audio + figlia KYC); dominio marcato "candidata a dismissione" solo per l'assenza di UI nei 5 frontend, non per mancanza d'uso ([D-51](_domande-committente.md)) |
| C7-3 | Segreti SIP + KYC | `sip_password` (in chiaro, chiave di lookup, restituita da API/email) → `sip_password_secret_ref` (secret manager, come C4-2); `reg_*` (CF personale, documento identità, data/luogo nascita) → **figlia `switchboard_voice_number_registrations`** cifrata, accesso `systemadmin` + audit di lettura, esclusa da export/log ([`docs/11 §4`](../../progettazione/11-modulo-privacy.md)) | `switchboard_configs` non porta più segreti né PII ad alto rischio |
| C7-4 | Log diagnostici (`switchboard_call_log`, `switchboard_wa_msg_log`) | [`docs/03 §2.4quinquies`](../../progettazione/03-gestione-dati.md) ne suggeriva la **non-migrazione** (→ tracing/outbox C6). Decisione committente (2026-09-10): **creati come tabelle C7** (`switchboard_voice_call_diagnostics`, `switchboard_messaging_wa_event_logs`) con retention breve, pseudonimizzazione, `deleted_at` esposto non usato | inbox forense del centralino; **non** dipendenza di runtime; gli stati d'invio veri restano correlati a `notification_deliveries` (C6) via `wamid` |
| C7-5 | Registro chiamate | `switchboard_call_register` (`from`/`to`/`datetime`) → **tabella eventi di dominio** `switchboard_voice_call_records` con `config_id`, `direction`/`outcome`/`forward_result` enum, ciclo di vita e `duration_seconds` ([`docs/03 §2.4quinquies`](../../progettazione/03-gestione-dati.md)) | storico softphone via FK stabile (non più match di stringa su `sip:`); retention 12 mesi → 🗑️/🔒 numero |
| C7-6 | `jsonb` in C7 | `components` (documento Meta), `wa_*_params` (argomenti API Meta), `ivr_path` (traccia flusso), `raw_payload`/`handler_result` (webhook di terzi) → **restano `jsonb`** ([`docs/03 §3.6 punto 5`](../../progettazione/03-gestione-dati.md); analogia [D-49]) + validazione al write e colonne di riepilogo derivate per `components` | nessuna decomposizione in tabelle plain di schemi di terzi; `action_config` polimorfo invece **de-jsonizzato** in colonne tipizzate |
| C7-7 | Dispositivi softphone | `device_fcm_token` / `device_voip_token` (2 colonne, 1 device) → `switchboard_voice_softphone_devices` (1 riga/device, `platform` enum, `last_seen_at`, `is_active`), stesso criterio di C6 `notification_push_device_tokens` | supporto multi-dispositivo; disattivazione su `UNREGISTERED`; purge dei device stantii |

### C8 — `chatbot`

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| C8-1 | `property_id` cross-modulo | `uuid` (= `public_id` di `catalog_properties`) copiato per valore, come C1-1/C3-3/C4-5/C6-8/C7-1; FK reali **solo** interne allo schema `chatbot` | segue [D-01](_domande-committente.md) |
| C8-2 | Provider LLM intercambiabile | Introdotto un **catalogo** (`chatbot_llm_providers`) al posto della mappa cablata `LLM_IMPLEMENTATIONS`; override selezionabile **per stabilimento** su `chatbot_manager_configs.llm_provider_id` (`NULL` = default di piattaforma); ogni turno registra chi ha effettivamente risposto (`llm_provider_id` + `llm_model_snapshot`, congelato) | requisito esplicito di poter cambiare il modello LLM senza deploy ([`docs/26-chatbot.md` §2`](../../progettazione/26-chatbot.md)); contratto astratto/implementazione concreta come [`docs/35-integrazioni-esterne.md` §1`](../../progettazione/35-integrazioni-esterne.md) |
| C8-3 | Funzionalità di interrogazione dati storici | **Non migrata**: lo strumento che interrogava un archivio esterno di prenotazioni di una stagione passata (function calling con clausola `WHERE` generata dal modello e sanitizzata a lista nera) è **rimosso**, non ridisegnato | [`docs/26-chatbot.md` §8`](../../progettazione/26-chatbot.md); nessuna tabella/colonna C8 lo riguarda |
| C8-4 | Esito del turno | Sostituita la deduzione legacy "risposta vuota = fallimento" (osservata **non sempre applicata** nei dati reali: 4/10 righe con `output_text=''`) con una colonna esplicita `status` (`answered`/`failed`); su `failed`, `output_text` è sempre `NULL` | `chatbot_conversation_turns.status` + `CHECK` |
| C8-5 | Cliente non autenticato (predisposizione) | `actor_user_id` resta **nullabile** (oggi sempre valorizzato, unico canale attivo è `manager`) invece di `NOT NULL`, con `CHECK` che lo impone solo per `channel='manager'`; `+ actor_session_id` (correlatore opaco lato client) per raggruppare i turni di un futuro visitatore anonimo del canale `customer` | se/quando il canale `customer` ammetterà l'uso senza login, basta smettere di richiedere il JWT su quell'endpoint e popolare `actor_session_id` — **nessuna migrazione**, richiesta esplicita del committente (2026-09-11) |

### INFRA

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| INFRA-1 | Perimetro del dominio | **Non** un dominio applicativo come C1…C8: raccoglie (1) il registro dei client API partner (`keysecret` → 1 tabella) e (2) l'annotazione "segreto → secret manager" per i campi cross-dominio già spostati fuori DB nelle schede C4/C6/C7 | 1 sola scheda `infra_*`; nessuna tabella per i segreti Fiskaly/SMTP/SIP/LLM (restano `*_secret_ref` sulla tabella del dominio proprietario) |
| INFRA-2 | Segreto del client API | **Mai a DB**, nemmeno come hash (la verifica HMAC/OAuth *client credentials* richiede il valore in chiaro) — solo `client_secret_ref` al secret manager, stesso pattern di C4-2/C6-5/C7-3 | `infra_integration_api_clients` senza colonna segreto; migrazione: i 2 segreti legacy (in chiaro nel DB) sono da considerare **compromessi** e ruotati subito dopo il provisioning nel secret manager |
| INFRA-3 | Nome tabella | Corretto da `integration_credentials` (illustrativo, [`docs/03 §3.1bis`](../../progettazione/03-gestione-dati.md)) a **`integration_api_clients`**: la tabella non contiene credenziali | correzione da riportare in `docs/03 §3.1bis` |

### Privacy

| # | Tema | Decisione | Effetto |
|---|---|---|---|
| Privacy-1 | Perimetro del dominio | Modulo **sottile** di orchestrazione: 5 tabelle di workflow/configurazione, **zero** tabelle di dato personale (mai spostato da chi già lo possiede) — [`docs/11-modulo-privacy.md` §1-2](../../progettazione/11-modulo-privacy.md) | schema `privacy` con solo metadati (id, stato, motivazioni), nessun nome/email/telefono |
| Privacy-2 | `ErasureRequest`/`ExportRequest` | Unificati in **una testata** (`privacy_data_subject_requests.request_kind`), non due aggregati distinti come nel documento — stesso precedente di C1 (`reservation_kind`) e C4 (`document_kind`, D-38) | 1 tabella + figlia `_module_outcomes` invece di 2 tabelle quasi-gemelle — [D-59] |
| Privacy-3 | Ambito di `privacy_retention_policies` | Configurazione **di piattaforma**, non per-stabilimento (le regole sono normative/di policy aziendale uniformi) | nessuna colonna `property_id` sulla tabella — [D-62] |

---

## Punti ancora aperti

Tutte le domande da porre al committente/architetto (C5 residue + C2 + cross-cutting + read-model JSON)
sono raccolte in **[`_domande-committente.md`](_domande-committente.md)**.

**C1 — da chiarire in fase implementativa / migrazione** (annotati nelle singole schede, non bloccanti):
semantica esatta di `reservation_status` (`bookings.status` 0/1/2/3 da normalizzare col team prodotto);
mappa varianti `priceCalculation` per `booking_agreement_lines.price_calculation` (segue [D-13]);
`booking_resale_settings.both_options_disabled` (semantica `disable_both_options` incerta nel legacy —
migrato as-is); recuperabilità in migrazione degli importi storici dei crediti sharing
(`booking_resale_credit_ledger.amount`); allegati prenotazione (`bookings.AttachedFiles`) → `booking_media_assets`
non modellata in questa iterazione (feature poco usata).

**C3 — da chiarire** (in `_domande-committente.md`): [D-34] eliminare `payment_customer_gateway_profiles`
delegando le carte interamente a Stripe? [D-35] `payment_ledger_entry_lines` — mantenere la ripartizione
proporzionale legacy come stima o registrare solo il movimento aggregato? [D-36] i crediti "sharing"
storici su `users` (per **email**, senza `property_id`) sono ricostruibili come wallet `resale_credit`
per `(property, customer)`? [D-37] serve il "pagamento multi-prenotazione" (ex `parent_bookings`, dead
code) e con quale modello? In fase implementativa: riconciliazione del vocabolario aliquote IVA di
`payment_ledger_*` / `payment_pos_charge_items` con la lookup di **C4**; idempotenza webhook Stripe su
`event.id` (tabella inbox del modulo `payment`).

**C4 — da chiarire in fase implementativa / migrazione** (annotati nelle singole schede, non
bloccanti): mappatura `fiskaly_vat_codes.code → Natura N1–N6` da validare col commercialista;
mappa delle 8+ varianti storiche di `payments[].type` (drift `"BANK TRANSFER"` ecc.) → `payment_type`
enum via `mapToFiskalyPaymentType`; recuperabilità in migrazione della coppia `booking_number →
reservation_public_id` per gli scontrini legacy (23 righe `intention_created` appese, 53
`transaction_failed`); `fiskaly_receipt_sequences` — numerazione **con o senza buchi** (peek+commit vs
consumo atomico) da confermare col committente. **C4bis** deciso: `fiscalprint_*` si mantiene come
componente separato (C4bis-2).

**C6 — da chiarire** (in `_domande-committente.md`): [D-46] 🟥 consenso marketing — base giuridica,
collocazione (`notification_marketing_contacts` vs C5), se `consent_status = unknown` blocca l'invio
(validazione DPO/legale); [D-47] feature marginali/incomplete — `notification_marketing_events`
(tenere?), `notification_marketing_event_reminders` (completare con scheduler o dismettere),
`notification_establishment_claim_requests` (tabella o solo invio email); [D-48] vocabolario canonico
dei segnaposto template (enum + `.resx` proposto, vs lookup a DB); [D-49] conferma di `payload jsonb`
su `notification_inapp_messages` (snapshot variabili, per analogia [D-28]). In fase implementativa:
mappa segnaposto `@token`/`{token}` legacy → canonici ([`docs/03 §2.7`](../../progettazione/03-gestione-dati.md));
strategia di parsing best-effort di `notification_text` → `kind`+`payload`; migrazione dello stato
`read` per-property (nessun equivalente per-utente).

**C7 — da chiarire** (in `_domande-committente.md`): **⚠️ verificato 2026-09-11**: la feature **non** è
"mai attivata" in assoluto — è attiva e collaudata su un terzo ambiente (`summer_booking_dev`, RDS) con
dati reali (config, IVR, audio, template WhatsApp, 214 righe di registro chiamate, 7 355 di log
diagnostico); resta **0 righe** solo su `local_sb` e sul dump di produzione, e **senza interfaccia** nei
5 frontend del repo. [D-50] 🟨 finestre di retention esatte + azione a scadenza per
`switchboard_voice_call_records` / `_call_diagnostics` / `switchboard_messaging_wa_event_logs` (range
proposti da [`docs/80 §3.7`](../../progettazione/80-retention.md)) — ora con dati reali già in attesa di una
policy; [D-51] 🟧 C7 mantenere/dismettere — reperire il pannello (verosimilmente esterno, quello che ha
configurato la riga reale osservata) o definire i requisiti da zero
([`da-chiarire.md` #36](../../analisi-pre/business-logic/da-chiarire.md)) + dubbio legale D.L. 7/2015
sulla retention del traffico telefonico ([`docs/11 §"Domande aperte" 1`](../../progettazione/11-modulo-privacy.md)),
non più ipotetico; [D-52] 🟨 conferma delle eccezioni `jsonb` di C7 (`components` Meta, `wa_*_params`,
`ivr_path`) per analogia [D-49]; [D-53] 🟨 (nuova) `config_id` non risolvibile da `phone_number_id`
quando il numero WhatsApp è quello generico condiviso (caso reale osservato). In fase implementativa:
0 righe su `local_sb`/dump di produzione ⇒ nessun backfill da lì, ma **1 riga di config reale** (+
satelliti) da migrare se `summer_booking_dev` va portato al nuovo schema; drift di naming
`switchboard_config.deviceFcmToken`/`deviceVoipToken` **verificato isolato a `local_sb`** (risolto,
non un problema reale); mapping `phone_number_id` Meta → `config_id` per il webhook WhatsApp (vedi
D-53).

**C8 — da chiarire** (in `_domande-committente.md`): stesso quadro di C7 — **0 righe** su `local_sb`/dump
di produzione, ma **10 righe di conversazione + 1 di configurazione reali** su `summer_booking_dev`
(verificato 2026-09-11), stessa property di test "96". [D-54] 🟧 dati personali di clienti osservati nel
testo libero di `chatbot_conversation_turns` (nome e cognome in una risposta reale) senza retention nel
legacy — finestra e accesso da confermare con DPO/legale; [D-55] 🟨 seminare già l'enum `channel_kind`
con `customer` (dichiarato nel legacy ma mai raggiunto da alcun endpoint) o solo `manager`. In fase
implementativa: 0 righe su `local_sb`/dump di produzione ⇒ nessun backfill da lì, ma **10 righe di
conversazione + 1 di config reali** da migrare se `summer_booking_dev` va portato al nuovo schema
(mapping dettagliato nella sezione "Implementazione .NET" di ciascuna scheda `chatbot_*`); nessun dato
storico per popolare `llm_provider_id`/`llm_model_snapshot`/`response_language` (introdotti solo nel
target, `NULL` per le righe migrate).

**INFRA — da chiarire** (in `_domande-committente.md`): [D-56] 🟨 scope granulari per client API oltre
`app_name`/`property_id` — non modellati ora (nessun consumo reale), tabella figlia se servirà; [D-57]
🟨 seminare l'enum `integration_app` con `teamsystem` (mai attivato) fin da ora, come [D-55]; [D-58] 🟨
`expires_at` opzionale (default proposto) o obbligatoria sui nuovi client. In fase implementativa: i 2
segreti legacy (`OfSale`, `Transactions`), essendo stati in chiaro nel DB, vanno considerati
**compromessi** e ruotati subito dopo il provisioning nel secret manager — non una semplice copia
1:1 del valore esistente ([`docs/17-secrets.md` §4.1](../../progettazione/17-secrets.md)).

**Privacy — da chiarire** (in `_domande-committente.md`): [D-59] 🟨 testata unica invece dei due aggregati
`ErasureRequest`/`ExportRequest` del documento di indirizzo (già applicato, da recepire in
`docs/11-modulo-privacy.md` §2); [D-60] 🟨 seminare `subject_kind` con `user` oltre `customer` (GDPR
self-service per staff/titolare, non ancora un requisito verificato); [D-61] 🟨 bundle di export come file
su object storage vs `payload jsonb` inline; [D-62] 🟨 `privacy_retention_policies` come configurazione
globale di piattaforma vs personalizzabile per stabilimento. In fase implementativa: determinazione dei
"moduli effettivamente coinvolti" per una richiesta (query di scoping vs broadcast con `not_applicable`);
seed iniziale delle policy dalla "tabella delle durate" di
[`docs/11-modulo-privacy.md` §6](../../progettazione/11-modulo-privacy.md) — quasi tutte `recommended_policy`,
retention telefonica (D.L. 7/2015) `disputed`/`is_dry_run_only = true` finché non arriva un parere legale
specialistico (stesso dubbio di [D-51] su C7); `docs/03 §3.1bis` non elenca ancora il modulo `privacy` fra
i domini (stessa nota già fatta per la sottoarea `account` di C5).
