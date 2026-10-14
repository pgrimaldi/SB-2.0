# Tabella `catalog_properties`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio) · **schema**: `catalog` · **nome flat**:
> `catalog_properties` · **forma canonica**: `catalog.properties`.
>
> Rimpiazza la legacy [`properties`](../../../analisi-pre/db/properties.md) (**~10.856 `id` massimo** nel DB
> dev, ma **~175 stabilimenti realmente operativi**; il dump di produzione `sb_prod_dump_2026_09_08`
> **non contiene** questa tabella). Fonti: [`analisi-pre/db/properties.md`](../../../analisi-pre/db/properties.md),
> [`analisi-pre/db/online_reservations.md`](../../../analisi-pre/db/online_reservations.md),
> [`analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md`](../../../analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md),
> [`analisi-pre/business-logic/stabilimento/11-configurazione.md`](../../../analisi-pre/business-logic/stabilimento/11-configurazione.md) e
> [`12-impostazioni.md`](../../../analisi-pre/business-logic/stabilimento/12-impostazioni.md),
> [`docs/03-gestione-dati.md` §3.1ter, §5, §6](../../../progettazione/03-gestione-dati.md),
> [`docs/34-multitenant.md`](../../../progettazione/34-multitenant.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §5](../../../progettazione/01-criticita-e-indicazioni-backend.md).
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine; `— (nuovo)` = campo senza corrispondente
> legacy.
>
> **`catalog_properties` è la radice del tenant.** Il suo `id` (`bigint`) è il **discriminatore
> multi-tenant** copiato in tutti gli altri moduli come `property_id bigint` (mai FK cross-schema, mai
> `uuid` — [`docs/03-gestione-dati.md` §3.1ter](../../../progettazione/03-gestione-dati.md),
> [`docs/34-multitenant.md`](../../../progettazione/34-multitenant.md)).

---

## Descrizione

Anagrafica **gestionale/di piattaforma** dello stabilimento balneare (tenant): stato di approvazione,
contatti amministrativi, titolare, e i pochi flag di configurazione che *appartengono alla property e ne
governano il ciclo di vita*. È distinta dalla "vetrina pubblica"
([`catalog_bathhouses`](catalog_bathhouses.md)): nel legacy il rapporto era 1:1 di fatto, qui è
**1:N** — una `property` può raggruppare più `bathhouse` (decisione committente, recepita anche in C5).

Correzioni strutturali rispetto al legacy:

- **`property_id` come `bigint`** ovunque nei moduli a valle (non `uuid`): l'`id` interno è la chiave
  tenant, copiato per valore. `public_id uuid` presente comunque per URL di back-office/admin.
- **Campi che escono da C2** ([`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)) — **non**
  riprodotti qui:
  - `fiskalyFlag`, `fiskalyEnabled`, `fiskalyAutoreceipt` → **C4** (`fiskaly`).
  - `commissionAmount` (`onlineCommission`, `iframeCommission`) → **C3** (`payment`) — con le eccezioni
    oggi hardcoded nel frontend (`COMMISSION_FREE_PROPERTY_IDS`, `CONNECTED_AND_PLATFORM_ACCOUNT_PROPERTIES`)
    da portare come dato di dominio.
  - `stripeCustomerId`, saldi crediti "sharing" → **C3**.
  - preferenze notifiche staff (`notifyEmail`, `notifyWhatsapp`, `deviceTokens`, `disableNotification`) →
    **C6** (stavano su `users`, non su `properties`, ma stesso principio).
- **Campi che entrano in C2 come stato della property**: `contractDocumentsId` (le tabelle
  `contract_documents`/`contract_acceptation` sono **C5**, ma il campo che *attiva* la feature per lo
  stabilimento resta qui — [`docs/03-gestione-dati.md` §6 nota](../../../progettazione/03-gestione-dati.md)).
- **`isTwoFactorEnabled`** (legacy su `bathouses`) → **C5** (`identity_access_property_security_settings`),
  aggregato per property. Non qui.
- **`onlineReservation` / `onlineBookingOptions` / `paymentOptions` / `superAdminOnlineFlag`** → estratti
  in [`catalog_property_online_booking_settings`](catalog_property_online_booking_settings.md) e figlie
  (Option A, [`docs/03-gestione-dati.md` §5](../../../progettazione/03-gestione-dati.md)). `superAdminOnlineFlag`
  resta un flag della property (kill-switch di piattaforma) e vive qui come `is_online_sales_suspended`.
- **`status`** intero arbitrario → `enum`; il valore `3` (commento model, mai usato) scartato, `4`
  (`csvUpload`) mappato a `imported`.
- **`photos`** (array URL su property, quasi sempre `[]` e concettualmente duplicato di
  `bathouses.photos`) → **non migrato**: le foto vivono sulla vetrina
  ([`catalog_bathhouse_photos`](catalog_bathhouse_photos.md)).
- **`country`** stringa libera quasi-fissa → FK verso [`catalog_countries`](catalog_countries.md).
- **`mobileCountryCode`** (mai valorizzato: mismatch di chiave col frontend) → **mantenuto** come
  `contact_phone_country_code`; il nuovo form lo raccoglie (decisione committente [D-06](../_domande-committente.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_properties` | `id` | PK | discriminatore tenant (copiato nei moduli a valle) |
| `uq_catalog_properties_public_id` | `(public_id)` | UNIQUE | risoluzione da URL admin/API |
| `ix_catalog_properties_status` | `(status) WHERE deleted_at IS NULL` | parziale | code registrazioni per stato (systemadmin) |
| `ix_catalog_properties_owner` | `(owner_user_public_id) WHERE deleted_at IS NULL` | parziale | "le mie property" |
| `ix_catalog_properties_country` | `(country_id)` | — | filtri per paese |
| `ix_catalog_properties_name_trgm` | `lower(name) gin_trgm_ops` | GIN trigram | ricerca amministrativa per nome (sostituisce i `LIKE` full-scan del legacy) ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): indice su espressione, scritto a mano nella migration (da riportare in un eventuale nuovo consolidamento). |

## Campi

### id
- **Legacy**: `properties.id`
- **Descrizione**: PK tecnica **e** discriminatore multi-tenant copiato come `property_id bigint` in C1,
  C3, C4, C6, C7.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità. In migrazione gli `id` legacy sono preservati 1:1.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da quasi ogni tabella `catalog_*` con `property_id`; copiata (per valore,
  no FK) dai moduli a valle.
- **Note**: **niente `uuid` come chiave tenant** — [`docs/34-multitenant.md`](../../../progettazione/34-multitenant.md).

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile per URL di back-office / endpoint admin.
- **Tipo**: `uuid` (UUIDv7), `DEFAULT` generato in factory.
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **non** è la chiave tenant cross-modulo (quella è `id`). Usato per non esporre l'intero
  sequenziale negli URL amministrativi ed eventuali endpoint GDPR/DPO.

### name
- **Legacy**: `properties.propertyName`
- **Descrizione**: nome amministrativo dello stabilimento (il nome commerciale pubblico vive su
  [`catalog_bathhouses.name`](catalog_bathhouses.md)).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No (il legacy ammetteva `NULL`; qui obbligatorio — raccolto dal form di registrazione).
- **Relazioni**: —.
- **Note**: nel legacy non era più modificabile dopo la registrazione se non da systemadmin; nel target
  editabile dal titolare.

### status
- **Legacy**: `properties.status` (`int`: 0 pending, 1 approved, 2 rejected, 4 imported-CSV; `3` mai usato)
- **Descrizione**: stato del ciclo di vita amministrativo dello stabilimento.
- **Tipo**: `enum catalog.property_status` — valori: `pending`, `approved`, `rejected`, `imported`.
- **Default**: `pending`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `imported` = ex `status = 4` (`user.controller.js:csvUpload`); il valore `3` del commento
  model non è mai stato scritto → scartato. La ricerca pubblica considera solo `approved` (+ `imported`
  se il committente conferma che devono essere visibili — punto aperto in
  [`systemadmin/01-gestione-stabilimenti.md`](../../../analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md)).

  > ### Manca lo stato "cessato" — emerso applicando la retention (2026-09-21)
  >
  > Il titolare ha deciso che uno stabilimento **cessato** non sparisce dal portale: la scheda **resta
  > pubblicata**, **ridotta alle sole informazioni essenziali** (nome, foto, posizione, descrizione base)
  > e **con la prenotazione online disattivata**, perché serve a popolare il portale pubblico
  > (chiarimento Q3, [`docs/82-retention-data.md` §5](../../../progettazione/82-retention-data.md);
  > [`docs/80-retention.md` §5](../../../progettazione/80-retention.md)). Corregge la proposta precedente, che
  > prevedeva la rimozione dalla ricerca pubblica.
  >
  > Questo enum **non sa esprimerlo**. Oggi `rejected` è un giudizio di ammissione, non una cessazione, e
  > gli altri tre valori descrivono l'ingresso in piattaforma, non l'uscita. Senza un valore dedicato,
  > "cessato ma pubblicato in forma ridotta" resterebbe implicito in una combinazione di flag su tabelle
  > diverse — esattamente il genere di stato non nominato che il legacy pagava caro.
  >
  > ## ✅ Deciso dal titolare il 2026-10-02: **la cessazione si deriva, non si aggiunge** (D-65 punto 1)
  >
  > ~~**Proposta**: valore `terminated` nell'enum + colonna `terminated_at timestamptz NULL`.~~
  > **Non si fa.** L'enum `catalog.property_status` resta `pending | approved | rejected | imported`, e
  > la cessazione si ricava da `identity`.
  >
  > **Come si deriva** (definizione da scrivere **una volta** e condividere, non da ricalcolare in ogni
  > job — vedi [`docs/80-retention.md` §5](../../../progettazione/80-retention.md)):
  >
  > | Serve | Dove sta |
  > |---|---|
  > | cessato ≠ sospeso | `identity.account_subscriptions.status` → `active \| suspended \| expired \| cancelled` |
  > | perché il periodo è finito | `identity.account_subscription_periods.close_reason` → `renewed \| expired \| cancelled \| superseded` |
  > | **la data della cascata** | `closed_at` (o `ends_at`) dell'ultimo periodo |
  >
  > `cancelled` è la cessazione esplicita; `expired` senza rinnovo successivo è la cessazione di fatto;
  > `suspended` non è né l'una né l'altra. Quindi l'obiezione che motivava la proposta — «sospeso ≠
  > cessato, e confonderli significa riattivare per sbaglio uno stabilimento chiuso al primo rinnovo» —
  > **non richiede una colonna qui**: è già distinguibile. Quello che resta di quell'obiezione è un
  > problema di **irreversibilità**: se il deprovisioning cancella i file e poi il cliente rinnova, non
  > si torna indietro. Si risolve facendo passare le cancellazioni dalla coda con periodo di grazia
  > ([`privacy_file_deletions`](../privacy/privacy_file_deletions.md)), non con uno stato in più.
  >
  > **Il costo accettato**: `DeprovisionClosedProperty` interroga Identity invece di leggere una colonna
  > locale. È la ragione per cui la proposta esisteva, ed è stata preferita **una sola verità** a una
  > copia che qualcuno deve ricordarsi di aggiornare.
  >
  > ⚠️ **Nota tecnica, da correggere dove compare**: la data di cessazione **non** vive solo in
  > `identity_account_subscriptions.ends_at`, come diceva la proposta: esiste
  > `identity.account_subscription_periods` con `closed_at`, `ends_at` **e** `close_reason`.
  >
  > Resta vero che la cascata del §5 **non è esprimibile** come singola riga di
  > [`privacy_retention_policies`](../privacy/privacy_retention_policies.md): la decorrenza è un evento
  > derivato, non una colonna data della riga stessa. È il motivo per cui serve `reference_event`
  > ([`privacy_retention_policies`](../privacy/privacy_retention_policies.md), campo `reference_event`).

### admin_comment
- **Legacy**: `properties.adminComment`
- **Descrizione**: nota del systemadmin all'approvazione/rifiuto/cancellazione; inserita nel corpo delle
  email automatiche al titolare.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: testo mostrato al cliente (es. motivazione rifiuto) — non solo interno.

### owner_user_public_id
- **Legacy**: `properties.propertyOwner`
- **Descrizione**: utente titolare (ruolo `superadmin`) collegato alla registrazione.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì (valorizzato solo quando l'account titolare viene creato; ~96% `NULL` nel DB dev per
  l'alta quota di registrazioni non completate).
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users`, nessuna FK cross-schema
  (contratto cross-modulo: i riferimenti a entità C5 usano il `public_id`, non il `bigint` —
  [`docs/03-gestione-dati.md` §3.1ter](../../../progettazione/03-gestione-dati.md)). Il nome porta il suffisso
  `_public_id` per non confonderlo con un `bigint` interno.
- **Note**: il legacy stesso lo trattava come riferimento potenzialmente *stale* e preferiva
  `users.propertyId` + ruolo per individuare il gestore da notificare. Nel target il legame "staff ↔
  property" è in C5 (`identity_access_users.property_id`); qui resta solo il "titolare storico della
  registrazione". Da confermare col committente se mantenere il concetto separato.

### contact_full_name
- **Legacy**: `properties.fullName`
- **Descrizione**: nome e cognome della persona di riferimento (contatto interno).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì (~92% `NULL` nel DB dev).
- **Relazioni**: —.

### contact_email
- **Legacy**: `properties.email`
- **Descrizione**: email di contatto amministrativo; destinataria delle comunicazioni di
  benvenuto/approvazione/rifiuto/cancellazione.
- **Tipo**: `citext` (o `varchar(320)` + confronto `lower()`).
- **Default**: nessuno.
- **Nullabile**: Sì (ma di fatto necessaria: è l'identificativo di comunicazione).
- **Relazioni**: —.
- **Note**: il legacy alla registrazione la copiava anche in `subscribes` (newsletter) — nel target
  quell'iscrizione implicita è una decisione esplicita di C6, non un effetto collaterale.

### contact_phone
- **Legacy**: `properties.mobile`
- **Descrizione**: telefono di contatto amministrativo.
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: nel legacy conteneva spesso il placeholder `333333333`; in migrazione i placeholder noti →
  `NULL`.

### contact_phone_country_code
- **Legacy**: `properties.mobileCountryCode` (mai valorizzato — il frontend inviava la chiave `countryCode`, il backend leggeva `mobileCountryCode`)
- **Descrizione**: prefisso internazionale del telefono di contatto.
- **Tipo**: `varchar(8)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: **si mantiene** (decisione committente [D-06](../_domande-committente.md)). Nel legacy non era
  mai valorizzato per un mismatch di chiave (frontend inviava `countryCode`, backend leggeva
  `mobileCountryCode`) — nel target il nuovo form lo raccoglie e la colonna viene popolata; le righe
  migrate restano `NULL`.

### city
- **Legacy**: `properties.city`
- **Descrizione**: città indicata in registrazione (dato amministrativo; la città pubblica è su
  `catalog_bathhouses`).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: nel legacy conteneva a volte CAP + città concatenati (testo libero non normalizzato).

### country_id
- **Legacy**: `properties.country` (stringa libera: `""`/`"Italia"`/`"Italy"`/`"india"`)
- **Descrizione**: paese dello stabilimento.
- **Tipo**: `smallint` (FK).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_countries.id`](catalog_countries.md) `ON DELETE RESTRICT`.
- **Note**: sostituisce la stringa libera; in migrazione `"Italia"`/`"Italy"` → stessa riga
  `catalog_countries`, `""`/`"india"` (test) → `NULL`.

### is_online_sales_suspended
- **Legacy**: `properties.superAdminOnlineFlag` (`1` = online consentito, `0` = sospeso) — **invertito** per coerenza `is_*`
- **Descrizione**: kill-switch **di piattaforma** che sospende le vendite online dello stabilimento
  (es. per abbonamento scaduto), a prescindere dalle impostazioni del gestore in
  [`catalog_property_online_booking_settings`](catalog_property_online_booking_settings.md).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: guidato dallo stato abbonamento in C5
  ([`identity_account_subscriptions`](../identity/identity_account_subscriptions.md)): il job notturno che nel legacy
  faceva `superAdminOnlineFlag = 0` su abbonamento scaduto nel target è un evento C5 → proiezione su
  questa colonna (o valutazione a runtime via policy).
- **Note**: il legacy lo scriveva anche `updatesuperadmin` (systemadmin, con la chiave di body fuorviante
  `onlineReservation`). Semanticamente distinto dai flag del gestore — va tenuto separato.

### contract_document_id
- **Legacy**: `properties.contractDocumentsId`
- **Descrizione**: quando valorizzato, identifica il documento contrattuale B2B (addendum) che *questo*
  stabilimento deve accettare — **attiva** la feature "contract condition" per la property.
- **Tipo**: `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì (quasi sempre `NULL`: feature opt-in poco diffusa).
- **Relazioni**: **`public_id` (uuid) copiato** da C5
  [`identity_account_contract_documents`](../identity/identity_account_contract_documents.md) — nessuna FK
  cross-schema. Il registro delle accettazioni è C5
  ([`identity_account_contract_acceptances`](../identity/identity_account_contract_acceptances.md)).
- **Note**: `NULL` ⇒ feature "spenta" per la property (lettura risponde `{ enabled: false }`). Il campo
  *possiede* lo stato di attivazione lato property, pur consumando entità C5 — criterio "chi governa il
  ciclo di vita" ([`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)).

### created_at / updated_at
- **Legacy**: `properties.createdAt` / `properties.updatedAt` (gestiti da Sequelize; `NULL` su righe importate)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4.

### deleted_at
- **Legacy**: `properties.isDelete` (`tinyint`, soft-delete)
- **Descrizione**: soft-delete della property (ex `deleteproperty` del systemadmin).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: Global Query Filter EF. La cancellazione di una property è un evento raro e ad alto impatto
  (trascina vetrine, listini, mappe): nel target va gestita con una procedura esplicita, non un semplice
  update di flag.

## Campi legacy **non** migrati

| Campo legacy | Motivo |
|---|---|
| ~~`properties.photos`~~ | ⛔ **DA MIGRARE — decisione del titolare del 2026-10-01.** La motivazione qui sotto è smentita dalla misura, vedi il riquadro subito dopo la tabella. |
| `properties.photos` *(motivazione originale, non più valida)* | ~~array URL a livello property, quasi sempre `[]`, duplicato concettuale di `bathouses.photos` → le foto vivono sulla vetrina~~ |
| `properties.onlineReservation` | → [`catalog_property_online_booking_settings.is_enabled`](catalog_property_online_booking_settings.md) |
| `properties.paymentOptions` / `servicebookingPaymentOptions` | → colonne booleane su `catalog_property_online_booking_settings` / `catalog_property_service_booking_settings` |
| `properties.onlineBookingOptions` / `serviceBookingOptions` | → `catalog_property_*_booking_settings` + figlie |
| `properties.serviceOnlineBooking` | → [`catalog_property_service_booking_settings.is_enabled`](catalog_property_service_booking_settings.md) |
| `properties.commissionAmount` | → **C3** |
| `properties.fiskalyFlag` / `fiskalyEnabled` / `fiskalyAutoreceipt` | → **C4** |

> ## ⛔ `properties.photos` va migrata — modifica da fare, non ancora applicata
>
> **Decisione del titolare del 2026-10-01**: quelle foto si recuperano. Le due premesse per cui
> erano state escluse sono smentite dalla misura sul dump di produzione:
>
> | Premessa scritta sopra | Misura del 2026-10-01 |
> |---|---|
> | «quasi sempre `[]`» | valorizzata su **10.510 righe su 10.852**, il **96,8%** |
> | «duplicato concettuale di `bathouses.photos`» | **zero chiavi in comune** fra i due insiemi: sono foto diverse |
>
> Sono **10.097 file distinti** (9.687 sotto `images/bathouse/`, 410 sotto `images/property/`),
> tutti esistenti su S3, e il 91% appartiene a stabilimenti che compaiono nella ricerca pubblica.
>
> ### Cosa va modificato
>
> 1. **`utility/01-migrazione-test/custom_transforms/catalog_media_assets.py`** — oggi legge da
>    `properties` solo `onlineBookingOptions` (per le icone di legenda). Va aggiunta la lettura di
>    `properties.photos`, con lo stesso trattamento di `bathouses.photos`: una riga
>    `catalog.media_assets` per chiave (dedup su `(origin, path)`, molte sono già presenti) e una
>    riga `catalog.bathhouse_photos` per l'aggancio.
> 2. **L'aggancio alla vetrina.** Ogni property ha **esattamente una** vetrina (misurato: 10.428
>    con una, 2 con due), quindi `bathouses.propertyId` dà la corrispondenza senza ambiguità.
>    **9.801 chiavi su 10.097 sono agganciabili**; le **296** restanti appartengono a property
>    **senza vetrina**. 🔄 **Deciso dal titolare il 2026-10-02** (opzione (b) di
>    [`docs/83-retention-file.md`](../../../progettazione/83-retention-file.md) §7-G3): non si scartano tutte.
>
>    | `properties.status` | Significato | Foto | Esito |
>    |---:|---|---:|---|
>    | 0 | registrato, in attesa di approvazione | 175 | ✅ **si migra** senza aggancio |
>    | 1 | approvato, senza vetrina aperta | 97 | ✅ **si migra** senza aggancio |
>    | 2 | **rifiutato** dal system admin | 24 | 🗑️ **si scarta**, con motivo `property rifiutata (status=2), nessuna vetrina` |
>
>    Le **272** conservate entrano in `catalog.media_assets` **senza** la riga `catalog.bathhouse_photos`
>    di aggancio: sono stabilimenti che potrebbero aprire la vetrina domani, e allora le foto devono
>    esserci già. ⚠️ **Hanno però la stessa firma di un file orfano**, quindi vanno marcate in modo che
>    le policy di retention non le cancellino 48 ore dopo la migrazione. ✅ **Deciso il 2026-10-02: si
>    valorizza `property_id` sull'asset** — colonna da aggiungere a `catalog.media_assets`, vedi
>    [`catalog_media_assets.md`](catalog_media_assets.md), campo `property_id`. ⚠️ Va aggiunta **prima** di questo recupero,
>    non dopo: dopo, i file sono già migrati e il primo giro di policy li ha già cancellati.
>
>    *(Non si usa un'etichetta `media_origin` dedicata: quell'enum descrive **l'host dello storage* —
>    `s3us_east2`, `s3us_east1`, `cloudfront`, `external` — *e non la provenienza logica.)*
> 3. **`sort_order`.** Le foto di `properties.photos` vanno **dopo** quelle di `bathouses.photos`,
>    perché per la retention le prime 3 per `sort_order` più la copertina sono «foto della scheda
>    pubblica» e restano per sempre, mentre le altre seguono la cessazione dello stabilimento
>    (vedi [`docs/83-retention-file.md`](../../../progettazione/83-retention-file.md) §3.2). Mettere le foto
>    recuperate in testa cambierebbe quali sono le foto pubbliche.
> 4. **Nessuna modifica di schema**: `catalog.bathhouse_photos` basta così com'è. Non serve una
>    `catalog.property_photos`, perché la decisione di progetto — «le foto vivono sulla vetrina» —
>    resta valida: era sbagliato solo il presupposto che quelle di `properties` fossero duplicati.
>
> Impatto sul travaso dei file: queste 10.097 chiavi oggi sono classificate come **orfani** dalla
> lista di lavoro costruita dal PostgreSQL, quindi `utility/02-migrazione-file` non le porterebbe
> via. Dopo questa modifica entrano nella lista normale.

## Valutazione JSON

Nessun JSON. Tutti i JSON legacy della tabella (`paymentOptions`, `onlineBookingOptions`,
`serviceBookingOptions`, `commissionAmount`, `photos`) diventano colonne tipizzate, righe di tabelle
figlie, o campi di altri domini.

## Suggerimenti per le prestazioni

- Tabella **hot**: caricata quasi ad ogni richiesta per il contesto tenant. Tenerla **stretta** (le
  colonne qui elencate, niente blob di configurazione): righe corte, più righe/pagina, cache efficiente.
- `id` come `bigint` (8 byte) mantiene compatti gli indici e le FK dei moduli a valle.
- L'indice trigram su `lower(name)` sostituisce i `findAll` + `LIKE` senza indice del legacy
  (`serchProperty` — [`docs/01-criticita-e-indicazioni-backend.md` §5](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- La ricerca pubblica di stabilimenti (con prezzo minimo, servizi, geo) **non** interroga questa tabella
  direttamente: si costruisce un **read model / proiezione** dedicato (denormalizzato da
  `catalog_bathhouses` + `catalog_pricing_*` + `catalog_service_*`), aggiornato a eventi — elimina il
  pattern legacy "N query per risultato".
- In migrazione: **non** portare le ~10.000 righe segnaposto (property registrate e mai completate,
  `status = pending`, nessun dato collegato) come tenant attivi — perimetro reale ~175.

## Implementazione .NET

`Property` è l'**aggregate root** del dominio `catalog` (radice del tenant): `sealed class Property :
AggregateRoot` — solleva eventi (`PropertyApproved`, `SubscriptionSuspended`, …) consumati da altri moduli.
Modulo `Module.Catalog`, schema `catalog`, tabella `properties`. `Id` (`long`) è il **discriminatore
tenant** copiato per valore in tutti i moduli a valle; `PublicId` serve solo URL/API di back-office.

### Enum proprio

```csharp
// Module.Catalog/Domain/Enums/PropertyStatus.cs
public enum PropertyStatus
{
	Pending = 0,
	Approved = 1,
	Rejected = 2,
	Imported = 3
}
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<PropertyStatus>("catalog", "property_status");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<PropertyStatus>("catalog.property_status");
```

### `PropertyConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
	public void Configure(EntityTypeBuilder<Property> builder)
	{
		builder.ToTable("properties", "catalog", t => t.HasComment(
			"Anagrafica gestionale/di piattaforma dello stabilimento (tenant): stato di approvazione, contatti amministrativi, titolare."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_properties");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7) per URL di back-office / endpoint admin; non è il discriminatore tenant.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome amministrativo dello stabilimento (il nome commerciale pubblico vive su catalog.bathhouses).");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("catalog.property_status")
			.IsRequired()
			.HasDefaultValue(PropertyStatus.Pending)
			.HasComment("Stato del ciclo di vita amministrativo dello stabilimento.");

		builder.Property(x => x.AdminComment)
			.HasColumnName("admin_comment")
			.HasColumnType("text")
			.HasComment("Nota del systemadmin all'approvazione/rifiuto/cancellazione, mostrata anche al titolare nelle email automatiche.");

		builder.Property(x => x.OwnerUserPublicId)
			.HasColumnName("owner_user_public_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Titolare storico della registrazione.");

		builder.Property(x => x.ContactFullName)
			.HasColumnName("contact_full_name")
			.HasMaxLength(255)
			.HasComment("Nome e cognome della persona di riferimento (contatto interno).");

		builder.Property(x => x.ContactEmail)
			.HasColumnName("contact_email")
			.HasColumnType("citext")
			.HasComment("Email di contatto amministrativo; destinataria delle comunicazioni di benvenuto/approvazione/rifiuto/cancellazione.");

		builder.Property(x => x.ContactPhone)
			.HasColumnName("contact_phone")
			.HasMaxLength(32)
			.HasComment("Telefono di contatto amministrativo.");

		builder.Property(x => x.ContactPhoneCountryCode)
			.HasColumnName("contact_phone_country_code")
			.HasMaxLength(8)
			.HasComment("Prefisso internazionale del telefono di contatto.");

		builder.Property(x => x.City)
			.HasColumnName("city")
			.HasMaxLength(255)
			.HasComment("Città indicata in registrazione (dato amministrativo; la città pubblica è su catalog.bathhouses).");

		builder.Property(x => x.CountryId)
			.HasColumnName("country_id")
			.HasComment("Paese dello stabilimento.");

		builder.Property(x => x.IsOnlineSalesSuspended)
			.HasColumnName("is_online_sales_suspended")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Kill-switch di piattaforma che sospende le vendite online dello stabilimento (es. abbonamento scaduto), a prescindere dalle impostazioni del gestore.");

		builder.Property(x => x.ContractDocumentId)
			.HasColumnName("contract_document_id")
			.HasComment("public_id di identity.account_contract_documents, copiato — nessuna FK cross-schema. NULL = feature contratto B2B disattivata per la property.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete della property (evento raro e ad alto impatto: trascina vetrine, listini, mappe).");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_properties_public_id");

		builder.HasIndex(x => x.Status)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_properties_status");

		builder.HasIndex(x => x.OwnerUserPublicId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_properties_owner");

		builder.HasIndex(x => x.CountryId)
			.HasDatabaseName("ix_catalog_properties_country");

		// Indice trigram su lower(name): Fluent API non esprime lower(name) come colonna indicizzata,
		// la migration generata va corretta a mano (vedi Note di migrazione).
		builder.HasIndex(x => x.Name)
			.HasMethod("gin")
			.HasOperators("gin_trgm_ops")
			.HasDatabaseName("ix_catalog_properties_name_trgm");

		builder.HasOne(x => x.Country)
			.WithMany()
			.HasForeignKey(x => x.CountryId)
			.HasConstraintName("fk_catalog_properties_country")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

`Property` implementa `IAuditable` e `ISoftDeletable` (plumbing comune, §1/§2 del template).

### Relazioni

- `CountryId` → `Country` (stesso schema `catalog`): FK reale, `ON DELETE RESTRICT`.
- `OwnerUserPublicId`, `ContractDocumentId`: id copiati da C5 (`identity.access_users`,
  `identity.account_contract_documents`) — solo `.HasComment`, nessuna FK.
- Tutte le tabelle `catalog_*` che referenziano la property lo fanno con `property_id bigint` copiato
  (non FK EF dichiarata qui, per evitare un grafo di navigazione enorme sull'aggregate root; ogni
  scheda figlia dichiara la propria `HasOne(x => x.Property)` verso questa tabella).

### Note di migrazione

- `id` legacy preservato 1:1; **non** migrare le ~10.000 righe segnaposto (`status = pending`, nessun
  dato collegato) — perimetro reale ~175 property.
- `status`: `4` (`csvUpload`) → `Imported`; il valore `3` (mai scritto, solo nel commento del model
  legacy) → scartato, nessuna riga lo usa.
- `country` stringa libera → risoluzione a `country_id` (`"Italia"`/`"Italy"` → stessa riga,
  `""`/`"india"` → `NULL`).
- Placeholder di contatto noti (es. `contact_phone = "333333333"`) → `NULL`.
- `owner_user_id`/`updated_by` legacy (bigint) → risolti a `public_id` uuid di C5 tramite la mappa
  `legacy_id → public_id` prodotta dalla migrazione di C5, valorizzando `owner_user_public_id`.
- Evento in ingresso da C5 (`SubscriptionExpired` / `SubscriptionReactivated`) aggiorna
  `is_online_sales_suspended` via un consumer applicativo, non un trigger DB.
