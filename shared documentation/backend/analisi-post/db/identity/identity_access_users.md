# Tabella `identity_access_users`

> **Dominio**: C5 — `identity` · **sottoarea**: `access` · **schema PostgreSQL**: `identity` · **nome
> flat**: `identity_access_users` · **forma canonica**: `identity.access_users`.
>
> Rimpiazza la legacy [`users`](../../../analisi-pre/db/users.md) (tabella account **unica** per gestori,
> staff, superadmin e clienti finali "consumer"). Fonti: [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md),
> [`analisi-pre/db/user_roles.md`](../../../analisi-pre/db/user_roles.md),
> [`analisi-pre/db/roles.md`](../../../analisi-pre/db/roles.md),
> [`analisi-pre/azioni/cliente-finale/01-account-e-registrazione.md`](../../../analisi-pre/azioni/cliente-finale/01-account-e-registrazione.md),
> [`docs/03-gestione-dati.md`](../../../progettazione/03-gestione-dati.md) (§2.4 enum, §3.1–§3.5 convenzioni, §3.1ter
> id, §3.2 nomi colonna utente, §6 campi cross-dominio),
> [`docs/01-criticita-e-indicazioni-backend.md` §7](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md),
> [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md),
> [`docs/pre/autenticazione-autorizzazione-dotnet.md`](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md).
>
> **Nota sulla voce "Legacy"**: per ogni campo indica la colonna della tabella legacy da cui deriva,
> nella forma `nome_tabella.campo`. `— (nuovo)` = campo senza corrispondente legacy.

---

## Descrizione

Anagrafica **minimale degli account di login** del sistema: solo ciò che serve ad autenticare e
autorizzare una persona. Una riga per account, indipendentemente dal tipo (titolare/`superadmin`,
staff operativo, cliente finale `consumer`). Il **tipo di account** è dato dal `role_id` (FK verso
[`identity_access_roles`](identity_access_roles.md)) e, per lo staff, dall'appartenenza allo
stabilimento (`property_id`).

Rispetto alla legacy `users`:

- **`email` con unicità reale** (indice unico funzionale su `lower(email)` fra le righe vive), e ogni
  lookup/aggiornamento avviene per `id` — mai più `UPDATE ... WHERE email = ...` (legacy senza `UNIQUE`,
  con rischio di update multi-riga verificato).
- **`role_id` singolo** con FK reale (la relazione N:N `user_roles` non è mai stata usata: nel dump di
  produzione `sb_prod_dump_2026_09_08` solo 2 account su 6.694 hanno un secondo ruolo, entrambi
  `superadmin`+`manager` — vedi "Mapping dal legacy").
- **Anagrafica clienti finali separata**: [`identity_customer_customers`](identity_customer_customers.md)
  (rubrica per-stabilimento, ~224k righe, 97% senza account) referenzia *questa* tabella per `id`
  quando esiste un account collegato.
- **Campi cross-dominio spostati fuori** (vedi "Campi legacy non migrati").
- **Abbonamento/licenza spostati** nella sottoarea `account`
  ([`identity_account_subscriptions`](identity_account_subscriptions.md)).
- **Soft-delete uniforme** `deleted_at` (la legacy usava `userStatus = 'inactive'`).
- **JSON `affiliate_selected_locations` → colonna array `resource_scopes text[]`** (in un primo tempo
  tabella figlia [`identity_access_user_resource_scopes`](_Zidentity_access_user_resource_scopes.md),
  eliminata il 2026-09-30: riquadro sotto).


> ### ✅ Applicato nel backend (2026-09-21) — i quattro flag audio sono diventati un array
>
> Le colonne `mgmt_booking_sound_enabled`, `online_booking_sound_enabled`,
> `mgmt_service_booking_sound_enabled`, `online_service_booking_sound_enabled` **non esistono più**:
> sostituite da **`enabled_notification_sounds identity.notification_sound_scope[]`**
> ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §13.1), migration `CollapseChildRowsIntoArrays`.
>
> Erano quattro booleani paralleli sullo stesso argomento — il pattern che lo schema risolve già con un
> array altrove (`enabled_payment_methods`). Qui non era stato applicato: era un'incoerenza di disegno,
> non una scelta. Aggiungere un quinto ambito ora è un valore di enum, non una quinta colonna.
>
> **I dati sono stati travasati, non reimpostati.** La migration copia i valori esistenti *prima* di
> eliminare le colonne di origine: il default della nuova colonna (tutti e quattro gli ambiti attivi)
> vale per le righe future, non per chi aveva già disattivato un suono. Verificato dopo l'applicazione:
> 6.407 account con l'avviso sul gestionale e 6.422 su quello online, prima e dopo, invariati.

> ### ✅ Applicato nel backend (2026-09-30) — anche la tabella figlia degli ambiti è diventata un array
>
> La tabella `identity_access_user_resource_scopes` **non esiste più**: sostituita dalla colonna
> **`resource_scopes text[]`** su questa tabella ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §13.1),
> migration `FixRoleFlagDefaultsAndCollapseResourceScopes` (dal 2026-10-06 consolidata nella
> `InitialCreate` di `Module.Identity` — [`docs/42-migration.md` §10](../../../progettazione/42-migration.md)).
>
> Era una delle due conversioni che il 2026-09-21 il documento lasciava «condizionate», perché si
> perdono `created_at`/`deleted_at` per singola assegnazione. Il travaso ha preso solo le assegnazioni
> attive (`deleted_at IS NULL`), in ordine alfabetico così l'array è stabile e confrontabile. Sul DB di
> sviluppo hanno ambiti 4 account su 6.674; 2 hanno `is_resource_scope_restricted = true`. La scheda della
> tabella eliminata è archiviata in [`_Zidentity_access_user_resource_scopes.md`](_Zidentity_access_user_resource_scopes.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_access_users` | `id` | PK | — |
| `uq_identity_access_users_public_id` | `public_id` | UNIQUE | riferimento pubblico / cross-modulo |
| `uq_identity_access_users_email_alive` | `lower(email)` `WHERE deleted_at IS NULL` | UNIQUE parziale funzionale | login, controllo disponibilità email; unicità solo fra account vivi ⚠️ **Non presente a DB** (verifica del 2026-10-06): progettato, mai creato. Sul DB di sviluppo ci sono **36 email duplicate** fra gli account vivi: oggi l'indice non si potrebbe creare senza bonifica. `email` è `citext`, quindi un indice unico diretto sulla colonna sarebbe già case-insensitive senza `lower()`. |
| `uq_identity_access_users_username_alive` | `lower(username)` `WHERE deleted_at IS NULL AND username IS NOT NULL` | UNIQUE parziale | login staff cassa per username ⚠️ **Non presente a DB** (verifica del 2026-10-06): progettato, mai creato. **4 username duplicati** fra gli account vivi sul DB di sviluppo. |
| `ix_identity_access_users_property` | `(property_id) WHERE deleted_at IS NULL` | parziale | "utenti di questo stabilimento" (lista staff, risoluzione destinatari) |
| `ix_identity_access_users_role` | `(role_id)` | FK | join ruolo, conteggi per ruolo |
| `ix_identity_access_users_google_id` | `(google_id) WHERE google_id IS NOT NULL` | parziale | login social Google |
| `ix_identity_access_users_apple_sub` | `(apple_subject) WHERE apple_subject IS NOT NULL` | parziale | login social Apple |
| `ix_identity_access_users_facebook_id` | `(facebook_id) WHERE facebook_id IS NOT NULL` | parziale | login social Facebook |
| `ix_identity_access_users_last_login` | `(last_login_at DESC) WHERE deleted_at IS NULL` | parziale | retention / audit account inattivi (§3.4 retention) |

## Campi

### id
- **Legacy**: `users.id`
- **Descrizione**: identificativo tecnico interno dell'account; PK.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK. Referenziato **entro lo schema `identity`** da
  [`identity_access_refresh_tokens.user_id`](identity_access_refresh_tokens.md),
  [`identity_access_user_auth_challenges.user_id`](identity_access_user_auth_challenges.md),
  [`identity_account_subscriptions.owner_user_id`](identity_account_subscriptions.md),
  [`identity_account_contract_acceptances.actor_user_id`](identity_account_contract_acceptances.md),
  [`identity_customer_customers.user_id`](identity_customer_customers.md) e da `*_operator_id` di più
  tabelle.
- **Note**: in migrazione si **preserva 1:1** il valore di `users.id` legacy (`bigint`, già così a DB) →
  tutte le FK applicative legacy (`properties.propertyOwner`, `customers.referenceId`,
  `bookings.booking_agent`, …) restano valide senza remap. Non esce mai dallo schema `identity`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: identificativo pubblico stabile, usato in URL/REST, negli endpoint GDPR
  ([`docs/11-modulo-privacy.md` §5](../../../progettazione/11-modulo-privacy.md)) e come **riferimento cross-modulo**
  (Booking/Payment/Notification copiano *questo*, mai `id`).
- **Tipo**: `uuid` (UUIDv7, generato in factory — [`docs/pre/identificatori-e-uuid.md`](../../../progettazione/pre/identificatori-e-uuid.md)).
- **Default**: nessuno (assegnato dall'applicazione alla creazione).
- **Nullabile**: No.
- **Relazioni**: chiave alternativa; è il valore copiato in `Customer.IdentityAccountId` degli altri
  moduli ([`docs/pre/autenticazione-autorizzazione-dotnet.md` §9](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md)).
- **Note**: in migrazione si genera un UUIDv7 nuovo per ogni riga (timestamp ancorato a `created_at`
  legacy dove disponibile).

### username
- **Legacy**: `users.username`
- **Descrizione**: nome utente alternativo per il login, usato **solo dallo staff** (es. `waiter`/
  `cashoperator` che accedono in cassa e per cui digitare un'email è scomodo). Per gli account
  `consumer` non esiste un username separato (nel legacy `username == email`).
- **Tipo**: `varchar(100)`.
- **Default**: nessuno.
- **Nullabile**: Sì (valorizzato solo per lo staff che ne ha bisogno; per i `consumer` resta `NULL`,
  non si duplica l'email).
- **Relazioni**: —.
- **Note**: in migrazione, dove `users.username == users.email` → `NULL`. Unicità case-insensitive fra
  righe vive (indice parziale). La legacy non aveva alcun `UNIQUE`.

### email
- **Legacy**: `users.email`
- **Descrizione**: email di contatto e identificatore primario di login / reset password / controllo
  disponibilità in registrazione.
- **Tipo**: `citext` (o `varchar(255)` + indice funzionale `lower(email)`), lunghezza max 255.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **unicità reale** fra righe vive (la legacy ne era priva, con `UPDATE ... WHERE email`
  multi-riga come rischio verificato). Alla cancellazione GDPR/anonimizzazione l'email viene sostituita
  con un placeholder non-instradabile (`deleted+<id>@invalid.local`), **non** più il rename legacy
  `del_DD/MM/YYYY_<email>` (che lasciava PII leggibile). Masking strutturale nei DTO di lista
  ([`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md)).

### password_hash
- **Legacy**: `users.password`
- **Descrizione**: hash della password in **formato PHC** (stringa auto-descrittiva:
  `$argon2id$v=19$m=…,t=…,p=…$<salt>$<hash>` per Argon2id, `$2b$<cost>$…` per bcrypt).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per gli account creati via **solo** social login — nel dump ~17% delle righe;
  finché è `NULL` il login con password è impossibile).
- **Relazioni**: —.
- **Note**:
  - **Mai** restituito da alcun endpoint; sempre confrontato a tempo costante.
  - **Come si distingue un hash vecchio da uno nuovo**: non serve una colonna "versione" — il **formato
    PHC contiene algoritmo e parametri** (`$argon2id$…m=…,t=…,p=…` o `$2b$08$…`). Al login riuscito si
    fa il *parse* dell'hash salvato: se algoritmo o parametri sono **sotto la policy corrente** →
    **re-hash trasparente** del testo appena verificato con i parametri correnti e `UPDATE` della
    colonna (pattern `PasswordVerificationResult.SuccessRehashNeeded` di ASP.NET Core Identity). Così i
    ~tutti gli hash legacy bcrypt cost 8 vengono aggiornati in modo incrementale, senza forzare un
    reset di massa.
  - **"È abbastanza robusto per durare?"**: nessun costo fisso dura per sempre — per questo lo schema è
    **parametrico e versionato tramite configurazione**, non "un valore appena sufficiente". Parametri
    di partenza consigliati: **Argon2id** `m ≥ 19456 KiB (19 MiB)`, `t ≥ 2`, `p = 1`, tarati a
    **~250–500 ms** sull'hardware di produzione (fallback: **bcrypt cost ≥ 12**). I valori vivono in
    configurazione (`Auth:PasswordHash:*` / variabili d'ambiente — vedi
    [`docs/pre/autenticazione-autorizzazione-dotnet.md` §5bis](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md))
    e si **rivedono periodicamente** (es. annualmente) alzando `m`/`t`/`cost` con l'evoluzione
    dell'hardware; l'aumento si propaga da solo al login successivo (re-hash sopra).
  - **Pepper server-side** (dal secret manager, [`docs/17-secrets.md`](../../../progettazione/17-secrets.md))
    applicato prima dell'hashing: un leak del **solo** DB non è attaccabile offline. Dal 2026-10-20 il pepper
    ha una **versione**, scritta nell'hash come parametro `k=N` (assente = versione 0): si può sostituire
    aggiungendone uno nuovo, gli hash vecchi si verificano col loro e vengono riscritti al login. Un pepper
    che manca rende inutilizzabili gli hash che lo usano: va custodito come la chiave delle copie
    ([`92-copie-sicurezza.md` §8.3](../../../progettazione/92-copie-sicurezza.md)).
  - Il **formato PHC** consente in futuro di **migrare algoritmo** (Argon2id → successore) con lo stesso
    meccanismo di re-hash al login, senza migrazione di schema.
  - Il flusso di reset **non** scrive più qui via endpoint non autenticato: passa da
    [`identity_access_user_auth_challenges`](identity_access_user_auth_challenges.md); a ogni scrittura
    si aggiorna `password_changed_at` e si **revocano tutti i refresh token** dell'account.

### password_changed_at
- **Legacy**: — (nuovo)
- **Descrizione**: data/ora dell'ultima modifica di `password_hash` (set password, reset, cambio,
  re-hash di upgrade).
- **Tipo**: `timestamptz`.
- **Default**: `NULL` (account senza password / social-only).
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: usato per **invalidare le sessioni** al cambio password (revoca di tutti i
  [`identity_access_refresh_tokens`](identity_access_refresh_tokens.md) con `revoked_reason = password_changed`
  e, se si adotta, rifiuto degli access token emessi prima di questo istante) e per policy di scadenza
  password se mai richieste. Il re-hash silenzioso di upgrade **non** invalida le sessioni (la password
  non è cambiata) — distinguere i due casi nel servizio.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 6.660 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### first_name / last_name
- **Legacy**: `users.firstname` / `users.lastname`
- **Descrizione**: nome e cognome dell'account.
- **Tipo**: `varchar(100)` ciascuno.
- **Default**: nessuno.
- **Nullabile**: Sì (i flussi di creazione staff non li rendono obbligatori; validazione applicativa
  per il flusso `consumer`).
- **Relazioni**: —.
- **Note**: in migrazione normalizzare i valori sporchi osservati (`"undefined"` letterale, stringa
  vuota) → `NULL`. Masking strutturale nei DTO di lista ([`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md)).

### phone_country_code
- **Legacy**: `users.countryCode`
- **Descrizione**: prefisso telefonico internazionale (E.164) del numero `phone`.
- **Tipo**: `varchar(8)` — valori tipo `+39`, `+33`, `+1`.
- **Default**: `'+39'`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: erede del **solo** `users.countryCode` (verificato vivo e affidabile: `+39` dominante).
  `users.mobileCountryCode` (verificato morto: sempre `NULL`/`''`) **non** viene migrato.

### phone
- **Legacy**: `users.mobile`
- **Descrizione**: numero di cellulare dell'account, **senza** prefisso (che sta in `phone_country_code`).
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: erede di `users.mobile`. Normalizzare (solo cifre) in migrazione.

### profile_picture_origin / profile_picture_path
- **Legacy**: `users.profilePic` (URL unico → scomposto in `origin` + `path`, [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md))
- **Descrizione**: riferimento scomposto alla foto profilo (avatar caricato o URL del provider social).
- **Tipo**: `profile_picture_origin` → enum PG `storage_origin` (`s3_us_east_2` | `s3_us_east_1` |
  `cloudfront` | `external`); `profile_picture_path` → `varchar(500)` (percorso relativo, o URL intero
  se `origin = external`).
- **Default**: nessuno.
- **Nullabile**: Sì (entrambi; valorizzati insieme o entrambi `NULL`).
- **Relazioni**: —.
- **Note**: applica il pattern di [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md) (mai
  URL completo a DB). Gli avatar Google/Facebook sono `origin = external`. Un solo campo-file → due
  colonne, non una tabella media dedicata (§1.2/§2.2: la sola necessità non la giustifica).

### login_method
- **Legacy**: `users.loginMethod`
- **Descrizione**: metodo di autenticazione usato l'**ultima volta**.
- **Tipo**: enum PG `identity.login_method` — valori: `normal`, `google`, `facebook`, `apple`.
- **Default**: `'normal'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `enum` C# (ogni provider richiede comunque codice dedicato — [`docs/03-gestione-dati.md`
  §2.4](../../../progettazione/03-gestione-dati.md)). Sul dump: `normal` ~93%, `facebook`/`google`/`apple` il resto.

### google_id
- **Legacy**: `users.googleId`
- **Descrizione**: `sub` dell'account Google collegato (login social).
- **Tipo**: `varchar(64)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: indice parziale per il lookup al login. Un utente può avere più provider collegati.

### facebook_id
- **Legacy**: `users.facebookId`
- **Descrizione**: id dell'account Facebook collegato.
- **Tipo**: `varchar(64)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: nel dump ~13 valori reali (il login Facebook **è** usato, contrariamente a quanto sembrava
  dal campione dev).

### apple_subject
- **Legacy**: `users.appleId` (nel legacy conteneva l'intero JWT Apple → si migra solo il claim `sub`)
- **Descrizione**: `sub` (identificatore stabile) restituito da Apple per l'account collegato.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: la legacy salvava in `appleId` (`text`) **l'intero JWT** Apple — errore: si migra solo il
  claim `sub`. Il JWT non va conservato.

### language
- **Legacy**: `users.languageCode` (`int` `1`=EN / `2`=IT)
- **Descrizione**: lingua preferita dell'account, per le email transazionali e la UI.
- **Tipo**: enum PG `identity.ui_language` — valori: `it`, `en`.
- **Default**: `'it'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: erede di `users.languageCode` (`int` `1`=EN / `2`=IT, significato solo da commento). Sul
  dump solo 2 valori. Se in futuro servono altre lingue → aggiungere valori all'enum (append-only).

### role_id
- **Legacy**: `user_roles.roleId` (tabella di giunzione N:N, senza FK reale)
- **Descrizione**: ruolo applicativo dell'account (determina tipo di account e permessi).
- **Tipo**: `bigint`.
- **Default**: nessuno (obbligatorio — **niente fallback privilegiato** come il legacy `setRoles([1])`
  = `systemadmin`).
- **Nullabile**: No.
- **Relazioni**: **FK reale** → [`identity_access_roles.id`](identity_access_roles.md) `ON DELETE RESTRICT`.
- **Note**: relazione **1:N** (un ruolo per account). Il cambio ruolo diventa un'operazione esplicita e
  tracciata (nel legacy era impossibile via API). Per i 2 soli account multi-ruolo del dump vedi
  "Mapping dal legacy".

### property_id
- **Legacy**: `users.propertyId`
- **Descrizione**: **entità gestionale / tenant** di appartenenza per lo staff (incluso il `superadmin`
  titolare); `NULL` per i `consumer`. Una `property` può raggruppare più `bathhouse` (vetrine
  pubbliche) — indicazione committente: lo staff appartiene alla `property`, non a un singolo
  `bathhouse`.
- **Tipo**: `uuid` — il **`public_id`** della `property` (C2), **non** il suo `bigint` interno.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** dalla `property` di C2 (`catalog`) — **nessuna FK
  cross-modulo** ([`docs/03-gestione-dati.md` §3.1ter](../../../progettazione/03-gestione-dati.md),
  [`docs/34-multitenant.md`](../../../progettazione/34-multitenant.md)). È il discriminatore usato dalle policy di
  isolamento tenant e dal Global Query Filter per lo staff.
- **Note**: la legacy aveva qui una delle rare FK InnoDB reali (`users_ibfk_1`); nel nuovo modello il
  vincolo di esistenza è garantito dal flusso applicativo + eventi, non da FK cross-schema.

### is_resource_scope_restricted
- **Legacy**: `users.isAffiliateFlag`
- **Descrizione**: se `true`, l'account (tipicamente ruolo `affiliate`) può operare **solo** sulle
  risorse elencate in `resource_scopes`; se `false`, nessuna restrizione di risorsa.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: erede di `users.isAffiliateFlag` (nome fuorviante nel legacy: non "è un affiliato" ma "ha un
  filtro di location attivo"). **L'enforcement diventa server-side** (nel legacy era solo lato client).

### news_section_enabled
- **Legacy**: `users.isNewsSectionEnable`
- **Descrizione**: mostra/nasconde la sezione "novità" nel pannello personale.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: erede di `users.isNewsSectionEnable`.

### resource_scopes
- **Legacy**: `users.affiliate_selected_locations` (JSON, array di codici location)
- **Descrizione**: codici delle postazioni su cui l'account può operare quando la restrizione è attiva
  (`is_resource_scope_restricted = true`).
- **Tipo**: `text[]`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: nessuna FK (codici testuali di risorse di C2, come nel legacy). Sostituisce la tabella
  figlia [`identity_access_user_resource_scopes`](_Zidentity_access_user_resource_scopes.md) (riquadro sopra).
- **Note**: si perde lo storico per singola assegnazione (`created_at`/`deleted_at` della riga figlia):
  è il prezzo accettato della conversione.

### enabled_notification_sounds
- **Legacy**: `users.enableManagementBookingSound` / `users.enableOnlineBookingSound` / `users.enableManagementServiceBookingSound` / `users.enableOnlineServiceBookingSound`
- **Descrizione**: ambiti per cui l'utente vuole l'allerta sonora del gestionale all'arrivo di una nuova
  prenotazione, differenziati per **tipo** (postazione / servizi) e **origine** (creata dallo staff /
  online dal cliente). Array vuoto = nessun suono.
- **Tipo**: `identity.notification_sound_scope[]` — enum `identity.notification_sound_scope`:
  `management_booking`, `online_booking`, `management_service_booking`, `online_service_booking`.
- **Default**: tutti e quattro gli ambiti (`'{management_booking,online_booking,management_service_booking,online_service_booking}'`).
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: sostituisce le 4 colonne booleane `mgmt_booking_sound_enabled`, `online_booking_sound_enabled`,
  `mgmt_service_booking_sound_enabled`, `online_service_booking_sound_enabled` (riquadro in
  «Descrizione»). Preferenza personale dello staff; per i `consumer` resta al default.

### last_login_at
- **Legacy**: `users.lastLogin`
- **Descrizione**: data/ora dell'ultimo login **realmente autenticato**.
- **Tipo**: `timestamptz`.
- **Default**: nessuno.
- **Nullabile**: Sì (mai loggato).
- **Relazioni**: —.
- **Note**: erede di `users.lastLogin`, ma scritto **solo** da un login autenticato (nel legacy anche
  `sendToken` — endpoint senza verifica password — lo aggiornava). Usato dai batch di retention
  ([`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)).

  > **Aggiornamento 2026-09-21 — retention validata dal titolare.** L'azione a scadenza è
  > **cancellazione a 12 mesi dalla revoca**, non pseudonimizzazione a 24 mesi come indicato in
  > precedenza: per l'audit fiscale/contabile basta il riferimento allo **stabilimento**, non serve
  > preservare l'identità dell'**utente** (chiarimento Q5,
  > [`docs/82-retention-data.md` §3.4](../../../progettazione/82-retention-data.md)).
  >
  > Conseguenza sulla **decorrenza**: il clock non è `last_login_at` (inattività) ma la **revoca
  > dell'accesso**, che si materializza come `deleted_at` (soft-delete).
  >
  > ✅ **Confermato dal titolare il 2026-10-02 (D-65 punto 2): revoca ≡ cancellazione.** Nessuna colonna
  > `access_revoked_at` da aggiungere: la decorrenza è `deleted_at`, ed è già così nel seed (verificato
  > a database: `identity.access_users`, `deleted_at`, 12 mesi, `delete_row`).
  >
  > ⚠️ **Condizione sotto cui la decisione va riaperta**, da leggere prima di introdurre nuovi stati
  > dell'account: se nascerà uno stato "sospeso" distinto dal soft-delete, un account sospeso ma non
  > cancellato **non scadrebbe mai**, e allora servirà la colonna dedicata. La decisione di oggi vale
  > perché oggi revocare un accesso *significa* cancellarlo. Vedi *Limiti noti* nel
  > [§ Seed di `privacy_retention_policies`](../privacy/privacy_retention_policies.md).

### terms_accepted_at / terms_version
- **Legacy**: — (nuovo; il legacy riceveva `privacyAccepted` dal form di registrazione senza salvarlo)
- **Descrizione**: quando il cliente ha accettato privacy e termini d'uso alla registrazione, e quale
  versione del testo. È la prova del consenso.
- **Tipo**: `timestamptz` / `varchar(50)`.
- **Default**: nessuno.
- **Nullabile**: Sì (entrambi; `NULL` per gli account storici e per lo staff, che non passa dalla
  registrazione pubblica).
- **Relazioni**: —.
- **Note**: aggiunte il 2026-10-07; **migration da creare** insieme alle API di registrazione. Il consenso
  alla newsletter è un'altra cosa e sta in Notification (`notification_marketing_newsletter_subscriptions`).

### created_at
- **Legacy**: `users.createdAt`
- **Descrizione**: creazione della riga.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4. In migrazione si porta `users.createdAt` (dove valido); per le righe legacy con
  timestamp sporco (`0000-00-00`) → data di go-live migrazione ([`docs/11-modulo-privacy.md` §6](../../../progettazione/11-modulo-privacy.md)).

### updated_at
- **Legacy**: `users.updatedAt`
- **Descrizione**: ultima modifica della riga.
- **Tipo**: `timestamptz`.
- **Default**: `now()` (trigger `set_updated_at` + interceptor EF).
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: a differenza del legacy **non** viene toccato dal login (per quello c'è `last_login_at`).

### deleted_at
- **Legacy**: `users.userStatus` (valore `inactive` = soft-delete)
- **Descrizione**: soft-delete (`NULL` ⇔ account attivo).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: sostituisce `users.userStatus = 'inactive'`. Il soft-delete **non** soddisfa da solo
  l'Art. 17 GDPR: la cancellazione/anonimizzazione effettiva dei campi PII è un secondo stadio guidato
  da `Module.Privacy` ([`docs/11-modulo-privacy.md` §5](../../../progettazione/11-modulo-privacy.md)). Un account
  può restare soft-deleted mentre i dati collegati a documenti fiscali sopravvivono per obbligo di legge.

### created_by_user_id
- **Legacy**: `users.createdBy` (nel legacy `int` dichiarato dal client, senza FK)
- **Descrizione**: account che ha creato *questo* account (per gli account staff creati da un
  admin/superadmin).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (i `consumer` si auto-registrano → `NULL`).
- **Relazioni**: FK → `identity_access_users.id` (auto-referenza) `ON DELETE SET NULL`.
- **Note**: erede di `users.createdBy` (nel legacy `int` dichiarato dal **client** — falsificabile).
  Ora **sempre** ricavato dal principal autenticato ([`docs/03-gestione-dati.md` §3.2](../../../progettazione/03-gestione-dati.md)).

## Enumerazioni introdotte

| Enum PG | Valori | Note |
|---|---|---|
| `identity.login_method` | `normal`, `google`, `facebook`, `apple` | ex `users.loginMethod` (`varchar` libero) |
| `identity.ui_language` | `it`, `en` | ex `users.languageCode` `int` 1/2 |
| `storage_origin` *(SharedKernel)* | `s3_us_east_2`, `s3_us_east_1`, `cloudfront`, `external` | condiviso, [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md) |

## Mapping dal legacy (`users`)

| Legacy | Nuovo | Note |
|---|---|---|
| `users.id` (`bigint`) | `id` | preservato 1:1 |
| — | `public_id` | nuovo, UUIDv7 |
| `users.username` | `username` | `NULL` se `== email` |
| `users.email` | `email` | unicità reale; rename-alla-cancellazione sostituito da placeholder `@invalid.local` |
| `users.password` | `password_hash` | formato PHC (algo+parametri nell'hash); re-hash trasparente al login se sotto policy; Argon2id + pepper; parametri da configurazione |
| — | `password_changed_at` | nuovo — revoca sessioni al cambio password |
| `users.firstname` / `users.lastname` | `first_name` / `last_name` | pulizia `"undefined"` / `''` → `NULL` |
| `users.countryCode` | `phone_country_code` | `users.mobileCountryCode` **non** migrato (morto) |
| `users.mobile` | `phone` | — |
| `users.profilePic` | `profile_picture_origin` + `profile_picture_path` | scomposizione URL (§1) |
| `users.loginMethod` | `login_method` (enum) | — |
| `users.googleId` | `google_id` | — |
| `users.facebookId` | `facebook_id` | — |
| `users.appleId` (JWT intero) | `apple_subject` | solo il claim `sub` |
| `users.languageCode` (1/2) | `language` (enum `it`/`en`) | — |
| `users.userStatus` (`active`/`inactive`) | `deleted_at` | `inactive` → `deleted_at = updatedAt` |
| `user_roles.roleId` (N:N, no FK) | `role_id` (FK, 1:N) | **userId 199** (*fierarimini*) e **2885** (*Spiaggia 46-47*) — unici multi-ruolo nel dump, `superadmin`+`manager` → `role_id = superadmin`, riga `manager` **rimossa** (confermato dal committente; `superadmin ⊇ manager` verificato lato server e client) |
| `users.propertyId` | `property_id` (`uuid`) | `public_id` della property copiato da C2, no FK cross-modulo |
| `users.isAffiliateFlag` | `is_resource_scope_restricted` | enforcement ora server-side |
| `users.affiliate_selected_locations` (JSON) | `resource_scopes` (`text[]`) | un elemento per risorsa (in origine 1 riga per risorsa nella figlia, eliminata) |
| `users.isNewsSectionEnable` | `news_section_enabled` | — |
| `users.enableManagementBookingSound` / `users.enableOnlineBookingSound` / `users.enableManagementServiceBookingSound` / `users.enableOnlineServiceBookingSound` | `enabled_notification_sounds` (un ambito per ogni flag a `true`) | travasati, non reimpostati |
| `users.lastLogin` | `last_login_at` | solo login autenticato |
| `users.createdBy` | `created_by_user_id` | FK reale, dedotto server-side dal principal (mai dal body) |
| `users.createdAt` / `users.updatedAt` | `created_at` / `updated_at` | — |

## Campi legacy non migrati

| Campo legacy | Motivo | Destinazione |
|---|---|---|
| `users.birthDate`, `users.gender` | campi morti (nessun form li raccoglie; valori sentinella) — [`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md) | **eliminati** |
| `users.loginOtp` | campo morto (mai un vero OTP) | **eliminato** (l'OTP vive in [`identity_access_user_auth_challenges`](identity_access_user_auth_challenges.md)) |
| `users.totalCreditsEarned` | campo morto (sempre `0`, mai scritto) | **eliminato** |
| `users.country` | morto (sempre `NULL`/`''`, un cluster di test `india`) | **eliminato** |
| `users.mobileCountryCode` | morto (sempre `NULL`/`''`) | **eliminato** (resta `phone_country_code` ex `users.countryCode`) |
| `users.two_factor_otp`, `users.expiry` | OTP 2FA transiente | → [`identity_access_user_auth_challenges`](identity_access_user_auth_challenges.md) |
| `users.notifyEmail`, `users.notifyWhatsapp` | preferenze canale notifiche staff | **C6** (`notification`) — [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md) |
| `users.disableNotification` | opt-out notifiche in-app | **C6** |
| `users.deviceTokens` | token push FCM (registrazione canale) | **C6** |
| `users.stripeCustomerId` | id cliente Stripe carte salvate (nel legacy conteneva per errore anche `acct_` Connect) | **C3** (`payment`) |
| `users.total_number_of_credit`, `users.total_credit_value` | saldo crediti "sharing" (oggi denormalizzato da un cron) | **C3** (ledger/wallet) |
| `users.license`, `users.startDate`, `users.endDate`, `users.subscriptionStatus`, `users.subscriptionDisableDate`, `users.subscriptionEnableDate` | abbonamento/licenza **dello stabilimento** | → sottoarea `account`: [`identity_account_subscriptions`](identity_account_subscriptions.md) |

## Valutazione JSON

Nessuna colonna `jsonb` in questa tabella. L'unico JSON legacy (`users.affiliate_selected_locations`,
array di codici location) diventa la colonna tipizzata **`resource_scopes text[]`** (in un primo tempo righe
relazionali nella figlia, poi collassate) — coerente con
[`note.md`](../../../progettazione/note.md) ("molti dei JSON del vecchio schema devono diventare valori nel nuovo
schema") e con [`docs/03-gestione-dati.md` §2.5](../../../progettazione/03-gestione-dati.md).

## Suggerimenti per le prestazioni

- Tabella **piccola** (~6.7k righe in produzione) e a **bassa scrittura**: il set di indici sopra è
  sufficiente; non aggiungerne "per ogni evenienza" (§3.11).
- Il percorso caldo è il **login**: `SELECT ... WHERE lower(email) = $1 AND deleted_at IS NULL` →
  coperto dall'indice unico funzionale parziale. Nessun `JOIN` a `identity_access_roles` al login: il
  `role` finisce nel token (claim), letto poi senza ri-query ([`docs/pre/autenticazione-autorizzazione-dotnet.md`](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md)).
- `property_id` come **prima colonna** degli indici compositi eventualmente aggiunti per le liste staff
  filtrate/ordinate (§3.11: uguaglianza prima del range).
- Tenere `password_hash` e i campi social fuori dai DTO di lista (proiezioni esplicite) — riduce I/O e
  rischio di leak.
- Indici **parziali** `WHERE deleted_at IS NULL` ovunque: la tabella accumula account disabilitati che
  non devono appesantire le scansioni vive.
- Retention ([`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)): l'indice
  `ix_identity_access_users_last_login` serve le query "account inattivi" senza full scan. **Il batch di
  retention validato non usa questo indice**: cancella a 12 mesi dalla **revoca** (`deleted_at`), non
  dall'ultimo login — e `deleted_at` è già il predicato parziale di tutti gli altri indici. L'indice
  resta utile per audit/report sugli account dormienti, non per il job.

## Implementazione .NET

Aggregate root del modulo `Module.Identity` (sottoarea `Access`), schema `identity`. Rappresenta
l'account di login (`Account`); la factory `Account.Register(...)` assegna
`PublicId = Guid.CreateVersion7()` e richiede `RoleId` esplicito (nessun fallback implicito come il
legacy `setRoles([1])`). Implementa `ISoftDeletable` e `IAuditable` (plumbing comune, §1 del template).

### Enum propri

```csharp
// Domain/Enums/LoginMethod.cs
public enum LoginMethod { Normal = 0, Google = 1, Facebook = 2, Apple = 3 }

// Domain/Enums/UiLanguage.cs
public enum UiLanguage { It = 0, En = 1 }
```

```csharp
// IdentityDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<LoginMethod>("identity", "login_method");
modelBuilder.HasPostgresEnum<UiLanguage>("identity", "ui_language");
// storage_origin è l'enum condiviso di SharedKernel (docs/03-gestione-dati.md §1), registrato una
// volta a livello di DataSourceBuilder comune — non ripetuto per ogni scheda che lo usa.
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<LoginMethod>("identity.login_method");
dataSourceBuilder.MapEnum<UiLanguage>("identity.ui_language");
```

### `AccountConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
	public void Configure(EntityTypeBuilder<Account> builder)
	{
		builder.ToTable("access_users", "identity", t => t.HasComment(
			"Account di login del sistema (titolare, staff, superadmin, cliente finale consumer)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_users");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Identificativo pubblico stabile (UUIDv7), usato in URL/API e come riferimento cross-modulo.");

		builder.Property(x => x.Username)
			.HasColumnName("username")
			.HasMaxLength(100)
			.HasComment("Username alternativo per il login da cassa (solo staff); NULL per i consumer.");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasColumnType("citext")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Email di contatto e identificatore primario di login.");

		builder.Property(x => x.PasswordHash)
			.HasColumnName("password_hash")
			.HasMaxLength(255)
			.HasComment("Hash della password; NULL per gli account creati solo via social login.");

		builder.Property(x => x.PasswordChangedAt)
			.HasColumnName("password_changed_at")
			.HasComment("Ultima modifica della password; usata per invalidare le sessioni al cambio.");

		builder.Property(x => x.FirstName)
			.HasColumnName("first_name")
			.HasMaxLength(100)
			.HasComment("Nome dell'account.");

		builder.Property(x => x.LastName)
			.HasColumnName("last_name")
			.HasMaxLength(100)
			.HasComment("Cognome dell'account.");

		builder.Property(x => x.PhoneCountryCode)
			.HasColumnName("phone_country_code")
			.HasMaxLength(8)
			.HasDefaultValue("+39")
			.HasComment("Prefisso telefonico internazionale (E.164) del numero di cellulare.");

		builder.Property(x => x.Phone)
			.HasColumnName("phone")
			.HasMaxLength(32)
			.HasComment("Numero di cellulare, senza prefisso.");

		builder.Property(x => x.ProfilePictureOrigin)
			.HasColumnName("profile_picture_origin")
			.HasComment("Provenienza della foto profilo (storage interno o provider social esterno).");

		builder.Property(x => x.ProfilePicturePath)
			.HasColumnName("profile_picture_path")
			.HasMaxLength(500)
			.HasComment("Percorso relativo della foto profilo, o URL intero se origin = external.");

		builder.Property(x => x.LoginMethod)
			.HasColumnName("login_method")
			.IsRequired()
			.HasDefaultValue(LoginMethod.Normal)
			.HasComment("Metodo di autenticazione usato l'ultima volta.");

		builder.Property(x => x.GoogleId)
			.HasColumnName("google_id")
			.HasMaxLength(64)
			.HasComment("Sub dell'account Google collegato (login social).");

		builder.Property(x => x.FacebookId)
			.HasColumnName("facebook_id")
			.HasMaxLength(64)
			.HasComment("Id dell'account Facebook collegato.");

		builder.Property(x => x.AppleSubject)
			.HasColumnName("apple_subject")
			.HasMaxLength(255)
			.HasComment("Sub Apple dell'account collegato (solo il claim, mai l'intero JWT).");

		builder.Property(x => x.Language)
			.HasColumnName("language")
			.IsRequired()
			.HasDefaultValue(UiLanguage.It)
			.HasComment("Lingua preferita per email transazionali e UI.");

		builder.Property(x => x.RoleId)
			.HasColumnName("role_id")
			.IsRequired()
			.HasComment("Ruolo applicativo dell'account (determina tipo account e permessi).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id della property (catalog) di appartenenza dello staff, copiato — nessuna FK cross-schema. NULL per i consumer.");

		builder.Property(x => x.IsResourceScopeRestricted)
			.HasColumnName("is_resource_scope_restricted")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se true, l'account opera solo sulle risorse elencate in resource_scopes.");

		builder.Property(x => x.ResourceScopes)
			.HasColumnName("resource_scopes")
			.HasColumnType("text[]")
			.IsRequired()
			.HasDefaultValueSql("'{}'::text[]")
			.HasComment("Codici delle postazioni su cui l'account puo' operare quando la restrizione e' attiva.");

		builder.Property(x => x.NewsSectionEnabled)
			.HasColumnName("news_section_enabled")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Mostra/nasconde la sezione novità nel pannello personale.");

		builder.Property(x => x.EnabledNotificationSounds)
			.HasColumnName("enabled_notification_sounds")
			.HasColumnType("identity.notification_sound_scope[]")
			.HasDefaultValueSql("'{management_booking,online_booking,management_service_booking,online_service_booking}'")
			.IsRequired()
			.HasComment("Ambiti per cui l'utente vuole l'allerta sonora. Array vuoto = nessun suono.");

		builder.Property(x => x.LastLoginAt)
			.HasColumnName("last_login_at")
			.HasComment("Data/ora dell'ultimo login realmente autenticato.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("Account che ha creato questo account (staff creato da admin/superadmin); NULL per i consumer auto-registrati.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_users_public_id");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_access_users_property");

		builder.HasIndex(x => x.RoleId)
			.HasDatabaseName("ix_identity_access_users_role");

		builder.HasIndex(x => x.GoogleId)
			.HasFilter("google_id IS NOT NULL")
			.HasDatabaseName("ix_identity_access_users_google_id");

		builder.HasIndex(x => x.FacebookId)
			.HasFilter("facebook_id IS NOT NULL")
			.HasDatabaseName("ix_identity_access_users_facebook_id");

		builder.HasIndex(x => x.AppleSubject)
			.HasFilter("apple_subject IS NOT NULL")
			.HasDatabaseName("ix_identity_access_users_apple_sub");

		builder.HasIndex(x => x.LastLoginAt)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_access_users_last_login");

		builder.HasOne(x => x.Role)
			.WithMany()
			.HasForeignKey(x => x.RoleId)
			.HasConstraintName("fk_identity_access_users_role")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.CreatedByUser)
			.WithMany()
			.HasForeignKey(x => x.CreatedByUserId)
			.HasConstraintName("fk_identity_access_users_created_by")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Indici funzionali (`lower(email)` / `lower(username)`)

`uq_identity_access_users_email_alive` e `uq_identity_access_users_username_alive` sono indici unique
parziali **su espressione**, non rappresentabili con `HasIndex(x => x.Email)` (che indicizza solo
colonne mappate). Si aggiungono in coda alla migration generata:

```csharp
migrationBuilder.Sql(@"
	CREATE UNIQUE INDEX uq_identity_access_users_email_alive
		ON identity.access_users (lower(email)) WHERE deleted_at IS NULL;
	CREATE UNIQUE INDEX uq_identity_access_users_username_alive
		ON identity.access_users (lower(username)) WHERE deleted_at IS NULL AND username IS NOT NULL;
");
```

con `Down()` speculare (`DROP INDEX`). Lo stesso pattern vale per ogni indice funzionale citato nelle
altre schede di questo batch (es. `identity_customer_customers`).

> ⚠️ **Mai fatto** (verifica del 2026-10-06): nessuno dei due indici esiste a DB, né c'era in nessuna
> delle 69 migration consolidate il 2026-10-06. Oggi l'unicità di email e username **non è garantita
> dal database**, e sul DB di sviluppo ci sono già 36 email e 4 username duplicati fra gli account vivi:
> prima di aggiungere l'indice serve una bonifica (nell'ETL o con una regola di fusione degli account).

### Sicurezza password

- `IPasswordHasher` (Argon2id, parametri `Auth:PasswordHash:*` da configurazione, ~250–500 ms; fallback
  bcrypt cost ≥ 12) + pepper da secret manager; re-hash trasparente al login
  (`PasswordVerificationResult.SuccessRehashNeeded`) —
  [`docs/pre/autenticazione-autorizzazione-dotnet.md` §5bis](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md).
- Ogni scrittura di `PasswordHash` aggiorna `PasswordChangedAt` e pubblica un evento di dominio che
  revoca tutti i [`identity_access_refresh_tokens`](identity_access_refresh_tokens.md) dell'account
  (`revoked_reason = password_changed`); il re-hash silenzioso di upgrade **non** lo fa.
- Handler `CustomerErasureRequested` (GDPR): anonimizza `Email` (placeholder
  `deleted+<id>@invalid.local`), `FirstName`/`LastName`, i campi `Phone*` e social; non tocca
  `Id`/`PublicId`.

### DTO e relazioni cross-modulo

- `AccountListItemDto` (email/telefono mascherati) vs `AccountDetailDto` — proiezioni esplicite, mai
  `PasswordHash` o i campi social nei DTO di lista ([`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md)).
- `PropertyId` è l'unico campo copiato da C2 (`catalog`): solo `.HasComment`, nessuna FK.

### Note di migrazione

- Preservare `id` 1:1 dal legacy `users.id`; generare `public_id` nuovo (UUIDv7, ancorato a
  `created_at` dove disponibile).
- `username = email` → `NULL`; normalizzare `first_name`/`last_name` sporchi (`"undefined"`, `''`) →
  `NULL`.
- Collassare i 2 account multi-ruolo (`userId 199`, `2885`) su `role_id = superadmin`; riga `manager`
  rimossa.
- `apple_subject`: migrare **solo** il claim `sub` dal JWT legacy salvato in `users.appleId`, mai
  l'intero token.
- `affiliate_selected_locations` (JSON) → array `resource_scopes` (codici ordinati, solo quelli
  attivi).
