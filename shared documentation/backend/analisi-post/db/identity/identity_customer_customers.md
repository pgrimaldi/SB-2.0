# Tabella `identity_customer_customers`

> **Dominio**: C5 — `identity` · **sottoarea**: `customer` · **schema**: `identity` · **nome flat**:
> `identity_customer_customers` · **forma canonica**: `identity.customer_customers`.
>
> Rimpiazza la legacy [`customers`](../../../analisi-pre/db/customers.md) (**224.467 righe** in
> produzione: la tabella anagrafica più grande del sistema). Fonti:
> [`analisi-pre/db/customers.md`](../../../analisi-pre/db/customers.md),
> [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md) (relazione con gli account),
> [`docs/03-gestione-dati.md` §3.1–§3.6, §3.1ter, §6](../../../progettazione/03-gestione-dati.md),
> [`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md) (PII, `note` art. 9, `isBlackList`,
> masking strutturale), [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md),
> [`docs/pre/autenticazione-autorizzazione-dotnet.md` §9](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md)
> (Identity minimale; anagrafica cliente separata; link per id nullable).
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine; `— (nuovo)` = campo senza
> corrispondente legacy.

---

## Descrizione

**Rubrica dei clienti finali per singolo stabilimento**: i nominativi che compaiono in una
prenotazione o in una transazione di cassa. Creata automaticamente quando compare un contatto non
ancora noto **per quella property** — indipendentemente dal fatto che quella persona abbia mai un
account di login. Scoped **sempre** per `property_id` (nessuna anagrafica condivisa fra stabilimenti:
lo stesso nome/telefono per due property = due righe).

**È distinta da [`identity_access_users`](identity_access_users.md)** (account di login, ~6.7k righe):
in produzione i `customers` sono ~224k, il 97% senza account. Il collegamento è opzionale:
`user_id` (ex `referenceId`).

Modifiche strutturali rispetto al legacy:

- **3 campi telefono ridondanti consolidati** (`mobile` + `phone` + `otherMobileNumber` → un
  `phone` primario + `phone_secondary`).
- **`referenceId int default 0`** ("0 = nessun account") → **`user_id bigint` nullable** con FK reale.
- **`isDelete`** → `deleted_at`.
- **`is_guest`** *non migrato qui*: la distinzione intestatario/ospite è una proprietà del **legame
  prenotazione↔cliente** (C1), non dell'anagrafica — vedi "Campi legacy non migrati".
- **denormalizzazione JSON eliminata**: `bookings.booking_consumer` / `service_reservation.customerData`
  (copie dei dati cliente in ogni prenotazione) diventano FK verso questa tabella (C1); qui non si
  copia nulla.

> **Scoping per `property_id` con più `bathhouse` per `property`** (indicazione committente). La
> `property` è l'entità gestionale / tenant; può raggruppare più `bathhouse` (vetrine pubbliche).
> La rubrica clienti è **per `property`** (come il legacy `customers.propertyId`, che già puntava a
> `properties`, non a `bathouses`) → è **naturalmente condivisa** fra tutte le strutture della stessa
> `property`: una società multi-lido gestita da un'unica `property` ha un'unica rubrica. Se in futuro
> servisse **segmentare** i contatti per singola struttura, si aggiunge una colonna `bathhouse_id`
> `uuid` nullable (il `public_id` del `bathhouse`; non un livello sopra `property`): struttura invariata. Da confermare col committente che la
> rubrica condivisa a livello `property` è il comportamento voluto — vedi
> [`_feature-tabelle.md` → "Punti ancora aperti"](../_feature-tabelle.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_customer_customers` | `id` | PK | — |
| `uq_identity_customer_customers_public_id` | `public_id` | UNIQUE | referenziato cross-modulo (Booking/Payment/Fiskaly copiano questo) |
| `ix_..._property_alive` | `(property_id) WHERE deleted_at IS NULL` | parziale | scoping di ogni query |
| `ix_..._property_phone` | `(property_id, phone) WHERE deleted_at IS NULL` | parziale composito | find-or-create / blacklist al momento della prenotazione (chiave di matching primaria) |
| `ix_..._property_email` | `(property_id, email) WHERE deleted_at IS NULL AND email IS NOT NULL` | parziale funzionale | idem, per email ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): sulla colonna e non su `lower(email)`, perché `email` è `citext` e l'indice è già case-insensitive. |
| `ix_..._property_name` | `(property_id, full_name) WHERE deleted_at IS NULL` | parziale | lista/ricerca contatti ordinata per nome (ex `idx_propertyId_fullName`) |
| `ix_..._user` | `(user_id) WHERE user_id IS NOT NULL` | parziale | "contatti collegati a questo account" |
| `ix_..._last_activity` | `(property_id, last_activity_at) WHERE deleted_at IS NULL` | parziale | retention: anonimizzazione clienti inattivi (§3.4) |

## Campi

### id
- **Legacy**: `customers.id`
- **Descrizione**: identificativo tecnico interno; PK.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziato **entro lo schema** da eventuali tabelle figlie (contatti
  secondari, se estratti).
- **Note**: in migrazione si **preserva 1:1** `customers.id` (`bigint`) → `bookings.customerId`,
  `service_reservation.customerId`, `card_transactions.user_id` legacy restano validi.

### public_id
- **Legacy**: — (nuovo; nel legacy gli altri moduli/JSON referenziavano `customers.id` intero)
- **Descrizione**: id pubblico stabile, usato nelle API e come **riferimento cross-modulo** (Booking,
  Payment, Fiskaly copiano *questo* per legare una prenotazione/scontrino a un cliente).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: nessuno (assegnato alla creazione).
- **Nullabile**: No.
- **Relazioni**: chiave alternativa; è l'id del *contatto* che gli altri moduli referenziano al posto
  della copia JSON legacy (`booking_consumer` / `customerData`).
- **Note**: non enumerabile (rilevante: la legacy espone `customers` con id sequenziali).

### property_id
- **Legacy**: `customers.propertyId`
- **Descrizione**: `property` (entità gestionale) a cui il contatto appartiene (scope di isolamento).
- **Tipo**: `uuid` — il **`public_id`** della `property` (C2), non il suo `bigint` interno.
- **Default**: nessuno.
- **Nullabile**: No (la legacy ne aveva 1 riga a `NULL` e alcune a `0` — dati sporchi da risolvere in
  migrazione).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 (`catalog`) — nessuna FK cross-modulo.
- **Note**: discriminatore tenant; prima colonna di quasi tutti gli indici. In migrazione:
  `customers.propertyId` (bigint legacy) → risolto via mappa `properties.id → property.public_id`.

### user_id
- **Legacy**: `customers.referenceId` (`int` default `0` = "nessun account")
- **Descrizione**: account di login (ruolo `consumer`) collegato a questo contatto, **se esiste**.
- **Tipo**: `bigint`.
- **Default**: `NULL` (nel legacy era `0` = "nessun account").
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`identity_access_users.id`](identity_access_users.md) `ON DELETE SET NULL`.
  (Concettualmente è il `Customer.IdentityAccountId` di
  [`docs/pre/autenticazione-autorizzazione-dotnet.md` §9](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md);
  poiché qui `customers` è una sottoarea dello **stesso** schema `identity`, il legame è una FK reale,
  non un id copiato cross-schema.)
- **Note**: ex `referenceId`. Il collegamento automatico "questo contatto è la stessa persona di questo
  account" (match per email/mobile) resta **best-effort** e va reso **visibile all'operatore**, non
  silenzioso come nel legacy (che sovrascriveva i dati del form con quelli dell'account trovato).

### full_name
- **Legacy**: `customers.fullName`
- **Descrizione**: nome del cliente/contatto.
- **Tipo**: `varchar(200)`.
- **Default**: nessuno.
- **Nullabile**: Sì (i flussi da cassa/carta possono crearlo vuoto).
- **Relazioni**: —.
- **Note**: normalizzare in migrazione i placeholder (`"Unknown"`, `""`). Masking strutturale nei DTO
  di lista pubblica ([`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md)).

### phone_country_code
- **Legacy**: `customers.countryCode`
- **Descrizione**: prefisso telefonico internazionale del `phone`.
- **Tipo**: `varchar(8)`.
- **Default**: `'+39'`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: ex `customers.countryCode` (verificato **vivo** e affidabile, default `+39`). In migrazione
  i clienti creati da `cardTransaction` con `""` hardcoded → `+39` o `NULL`.

### phone
- **Legacy**: `customers.mobile`
- **Descrizione**: telefono primario del contatto — **la** chiave di matching/deduplica per property
  (insieme a `email`).
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: Sì (nel dump `NULL` sul 10% — il telefono è comunque l'identificatore di contatto più
  presente: `email` è `NULL`/vuota su oltre metà).
- **Relazioni**: —.
- **Note**: consolida `customers.mobile`. **Validare il formato** prima di usarlo come chiave di
  matching: la legacy fonde clienti diversi che hanno digitato lo stesso segnaposto (`"99999999"`,
  `"3333333333"`, `"00000"`) — [`analisi-pre/db/customers.md`](../../../analisi-pre/db/customers.md).

### phone_secondary
- **Legacy**: `customers.phone` + `customers.otherMobileNumber` (due colonne consolidate in una)
- **Descrizione**: recapito telefonico alternativo.
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: consolida `customers.phone` **e** `customers.otherMobileNumber` (tre colonne telefono
  legacy quasi sempre vuote, mai usate come chiave). Se serve più di un secondario in futuro → tabella
  figlia `identity_customer_customer_contacts`; oggi non giustificata (99% `NULL`).

### email
- **Legacy**: `customers.email` (nel legacy `varchar(50)` — troppo stretto)
- **Descrizione**: email di contatto, seconda chiave di matching.
- **Tipo**: `citext` / `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì (nel dump `NULL`/vuota su oltre metà delle righe).
- **Relazioni**: —.
- **Note**: la legacy la limitava a **50 caratteri** (troppo stretto) → 255. Masking strutturale nei
  DTO di lista.

### fiscal_code
- **Legacy**: `customers.fiscalCode` (`varchar(255)`, spesso sporco)
- **Descrizione**: codice fiscale (clienti che richiedono fattura/scontrino intestato).
- **Tipo**: `varchar(16)`.
- **Default**: nessuno.
- **Nullabile**: Sì (nel dump valorizzato < 4%).
- **Relazioni**: —.
- **Note**: ex `customers.fiscalCode`. Consumato in **snapshot** dallo scontrino Fiskaly (C4) —
  riferimento legittimo, non campo cross-dominio ([`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)).

### vat_number
- **Legacy**: `customers.vat`
- **Descrizione**: partita IVA (clienti business).
- **Tipo**: `varchar(20)`.
- **Default**: nessuno.
- **Nullabile**: Sì (nel dump valorizzato < 4%, quasi tutto sporco).
- **Relazioni**: —.
- **Note**: ex `customers.vat`.

### address / city / postal_code
- **Legacy**: `customers.address` / `customers.city` / `customers.cap`
- **Descrizione**: indirizzo di residenza/fatturazione.
- **Tipo**: `address` → `varchar(255)`; `city` → `varchar(120)`; `postal_code` → `varchar(16)`.
- **Default**: nessuno.
- **Nullabile**: Sì (nel dump `NULL` sul ~96%).
- **Relazioni**: —.
- **Note**: dati "estesi" raccolti solo dal form "Contatti" completo. In UI vanno resi
  opzionali/collassabili ([`analisi-pre/db/customers.md`](../../../analisi-pre/db/customers.md)).

### date_of_birth
- **Legacy**: `customers.dateOfbirth` (naming legacy incoerente: "b" minuscola)
- **Descrizione**: data di nascita.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: Sì (nel dump `NULL` sul 99%; molti valori sono la data odierna precompilata o
  `1970-01-01`).
- **Relazioni**: —.
- **Note**: ex `customers.dateOfbirth`. In migrazione scartare i valori epoch/odierni/futuri. Valutare
  col committente se serve davvero raccoglierla (minimizzazione).

### other_names
- **Legacy**: `customers.otherNames`
- **Descrizione**: nominativi aggiuntivi/alias del contatto (testo libero).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: ex `customers.otherNames`.

### discount_note
- **Legacy**: `customers.anyDiscounts` (nel form React chiamato `discount`)
- **Descrizione**: nota testuale libera su sconti/condizioni particolari riservate al cliente.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: ex `customers.anyDiscounts` — **non** collegato al motore sconti strutturato
  (`discounts`/`price_lists`): è un promemoria per l'operatore. Nome chiarito.

### note
- **Legacy**: `customers.note`
- **Descrizione**: nota libera sul cliente.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: **rischio art. 9 GDPR** (dati sulla salute infilati in campo libero — es. allergie —
  [`docs/11-modulo-privacy.md` §3/§4](../../../progettazione/11-modulo-privacy.md)). Validare/limitare il
  contenuto. Se emerge un'esigenza operativa reale (sicurezza alimentare), trattarlo
  come categoria particolare con base giuridica dedicata — **decisione di business/legale**.

  > **Retention validata (2026-09-21)**: `note` e `discount_note` si azzerano a **N−1** (una stagione),
  > **prima** del resto dell'anagrafica che vive 48 mesi
  > ([`docs/82-retention-data.md` §3.4](../../../progettazione/82-retention-data.md)). È una riga di policy
  > distinta (`field_names = {note, discount_note}`, `action = null_fields`) nel
  > [§ Seed di `privacy_retention_policies`](../privacy/privacy_retention_policies.md), non un effetto
  > collaterale della policy sull'anagrafica: un job con una soglia unica terrebbe il campo più
  > rischioso della tabella tre stagioni di troppo.

### is_blacklisted
- **Legacy**: `customers.isBlackList`
- **Descrizione**: flag di blacklist **per singolo stabilimento** (la riga è già scoped per
  `property_id`).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: ex `customers.isBlackList` (nel dump `1` su 40 righe, ~0,018%). Applicato in creazione
  prenotazione (rifiuto). L'override legacy `allowBookingForBlacklistedCustomer` (booleano dal client)
  va sostituito da una **decisione autorizzata server-side** (ruolo/permesso) —
  [`analisi-pre/db/customers.md`](../../../analisi-pre/db/customers.md). Dato valutativo su persona:
  documentare la base giuridica (legittimo interesse), prevedere rettifica/contestazione, revisione
  periodica ([`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)).

  > **Aggiornamento 2026-09-21 — il titolare ha deciso *chi* rivede il flag**: **revisione annuale a
  > carico del gestore dello stabilimento**, non del sistema
  > ([`docs/82-retention-data.md` §3.4](../../../progettazione/82-retention-data.md)). Non è quindi un job di
  > retention (il flag non scade da solo) ma un **promemoria/pannello nel gestionale**: "questi N
  > clienti sono in blacklist da oltre 12 mesi, confermi?".
  >
  > ~~**Gap di disegno**: servirebbe un `blacklist_reviewed_at` (+ `blacklist_reviewed_by_user_id`).~~
  >
  > ## ✅ **Deciso il 2026-10-02 (D-65 punto 3): si applica la logica del legacy.** La colonna **non**
  > si aggiunge.
  >
  > Nel legacy `customers.isBlackList` è un `tinyint` scritto alla creazione e all'aggiornamento del
  > cliente (`back-pre/app/controllers/customers.controller.js`) e letto da `isUserBlacklisted`, e
  > **niente altro**: nessuna data di revisione, nessun promemoria, nessuna scadenza. In produzione 40
  > righe su 224.526 ce l'hanno a `1`. Qui `is_blacklisted` resta un flag che il gestore mette e toglie
  > a mano, e `blacklist_reason` è l'unica aggiunta rispetto al legacy.
  >
  > **Conseguenza da conoscere** (non un'obiezione alla decisione, una sua implicazione): la revisione
  > annuale resta un compito umano **senza traccia a database**. Un eventuale promemoria può basarsi
  > solo su `updated_at`, che cambia per qualsiasi modifica del cliente: non distinguerà «ho
  > riconfermato la blacklist» da «ho corretto il telefono», e ripeterà la domanda a chi ha appena
  > risposto.
  >
  > ⚠️ **E per i dati legacy non è documentata nemmeno la base giuridica**: `blacklist_reason` è
  > **interamente NULL su tutte le 224.524 righe**, incluse le 40 con il flag a `1` — il legacy aveva
  > solo il flag, senza motivo, e l'ETL lascia la colonna vuota di proposito
  > ([`analisi-pre/db-no-copied/_colonne-vuote.md`](../../../analisi-pre/db-no-copied/_colonne-vuote.md), categoria B). Quindi per quelle 40 righe, oggi, non
  > esiste né il motivo né la data di revisione. La colonna si riempirà solo per le blacklist messe dal
  > nuovo gestionale.
  >
  > Nessuna policy di `privacy_retention_policies` è interessata: verificato a database, per questo flag
  > non esiste nessuna riga — la revisione è umana, non un job.

### blacklist_reason
- **Legacy**: — (nuovo; il legacy aveva solo il flag `isBlackList`, senza motivo)
- **Descrizione**: motivo della blacklist (per la revisione periodica e il diritto di contestazione).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: nuovo (il legacy aveva solo il flag).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 224.524 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1; il perche' e il verdetto nella Parte 2).

  > ## 🔄 **Deciso dal titolare il 2026-10-02: non resta NULL, va popolata dall'ETL.**
  >
  > Il nodo era che questa colonna è la **decorrenza di tre policy già seminate** — anonimizza a 48
  > mesi, azzera `note`/`discount_note` a 1 stagione, cancella a 10 anni — e con la colonna vuota
  > **nessuna delle tre si applica ai 224.524 clienti legacy**, cioè proprio ai dati vecchi che la
  > retention dovrebbe ripulire.
  >
  > | Clienti | Quanti | `last_activity_at` |
  > |---|---:|---|
  > | con almeno una prenotazione | **106.910** | `MAX` sulle prenotazioni del cliente |
  > | senza nessuna prenotazione | **117.614** | **la data della migrazione** |
  >
  > La seconda riga è una scelta dichiarata, non un dato sul cliente: *«di attività prima di questo
  > momento non abbiamo prova, quindi si comincia a contare da qui»*. Conseguenza da conoscere: quei
  > 117.614 maturano **tutti nello stesso giorno**, 48 mesi dopo la migrazione — sarà
  > un'anonimizzazione di massa, non un flusso continuo, e il job deve poterla reggere.
  >
  > ⚠️ **La data della migrazione deve essere una costante della corsa, non `now()`.** Il committente
  > ha deciso che l'ETL si riesegue da zero ([`analisi-pre/db-no-copied/_colonne-vuote.md` Q7](../../../analisi-pre/db-no-copied/_colonne-vuote.md)): con
  > `now()` ogni rerun **azzera l'orologio** di 117.614 schede, e la retention non scatterebbe mai.
  > Va presa dalla data di cutover, passata come parametro e registrata nel report della corsa.
  >
  > Resta vero che **in avanti** la colonna la mantengono gli eventi di dominio Booking/Payment: l'ETL
  > la inizializza, non la gestisce.

### source_channel
- **Legacy**: — (nuovo; nel legacy si deduceva da quali campi erano valorizzati — form completo vs creazione automatica da booking/service/card)
- **Descrizione**: come è nata la riga.
- **Tipo**: enum PG `identity.customer_source` — valori: `contact_form`, `booking`,
  `service_reservation`, `card_transaction`, `import`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: nel legacy si deduceva da quali campi erano valorizzati (form completo vs creazione
  automatica). Esplicitarlo aiuta deduplica e qualità dati.

### created_by_user_id
- **Legacy**: `customers.createdBy` (nel legacy `int` dichiarato dal client, senza FK)
- **Descrizione**: operatore che ha creato/modificato il contatto dal form "Contatti".
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per le creazioni automatiche — nel dump 96%).
- **Relazioni**: **FK** → [`identity_access_users.id`](identity_access_users.md) `ON DELETE SET NULL`.
- **Note**: ex `customers.createdBy` (nel legacy `int` dichiarato dal client). Ora dedotto dal
  principal autenticato ([`docs/03-gestione-dati.md` §3.2](../../../progettazione/03-gestione-dati.md)).

### last_activity_at
- **Legacy**: — (nuovo; mantenuto dagli eventi di dominio Booking/Payment — nel legacy non esiste)
- **Descrizione**: data dell'ultima prenotazione/transazione che coinvolge questo cliente.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: **nuovo** — mantenuto dagli eventi di dominio (Booking/Payment). Serve alla retention:
  **"anonimizza dopo 48 mesi senza prenotazioni"** ([`docs/11-modulo-privacy.md` §6](../../../progettazione/11-modulo-privacy.md),
  [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)) — la decorrenza non è `created_at`.

  > **Aggiornamento 2026-09-21 — retention validata dal titolare**: **48 mesi** (non più "24–36"),
  > poi pseudonimizzazione; cancellazione completa a 10 anni se il cliente è legato a documenti fiscali
  > ([`docs/82-retention-data.md` §3.4](../../../progettazione/82-retention-data.md)). I campi `note` e
  > `discount_note` scadono **prima**, a N−1 — vedi la nota sul campo `note`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 224.524 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: `customers.createdAt` / `customers.updatedAt` (nel legacy colonna fisica `DEFAULT NULL`)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4. In migrazione porta `customers.createdAt`/`updatedAt` (nel dump `NULL` su ~0,5–1%
  → data di go-live).

### deleted_at
- **Legacy**: `customers.isDelete` (`tinyint(1)`)
- **Descrizione**: soft-delete (`NULL` ⇔ contatto attivo).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: sostituisce `customers.isDelete` (nel dump `1` su ~2,2%). Il "ripristino" (nel legacy
  `deletecustomer` con `isDelete: false`) diventa un'operazione esplicita. La cancellazione GDPR
  effettiva (anonimizzazione nome/contatti mantenendo id + minimi fiscali) è un secondo stadio guidato
  da `Module.Privacy` — [`docs/11-modulo-privacy.md` §5](../../../progettazione/11-modulo-privacy.md).

## Campi legacy non migrati

| Campo legacy | Motivo | Destinazione |
|---|---|---|
| `customers.is_guest` | distinzione intestatario/ospite = proprietà del **legame prenotazione↔cliente** | **C1** (Booking): il legame `booking ↔ customer` porta un ruolo (`holder` / `guest`); una persona può essere intestataria in una prenotazione e ospite in un'altra |
| `customers.mobile` + `customers.phone` + `customers.otherMobileNumber` (3 colonne) | ridondanti | consolidati in `phone` + `phone_secondary` |
| `customers.referenceId` (`int` default `0`) | "0 = nessun account" | `user_id` `bigint` NULL + FK |
| `customers.createdAt`/`customers.updatedAt` come `DEFAULT NULL` | incoerenza | `NOT NULL DEFAULT now()` |

## Valutazione JSON

Nessun `jsonb`. Anzi: questa tabella **elimina** JSON altrui — le copie denormalizzate
`bookings.booking_consumer` / `service_reservation.customerData` (dati cliente duplicati in ogni
prenotazione, tenuti in sync solo da `updatecustomer`) diventano FK verso `public_id` di questa tabella
lato C1. Coerente con [`docs/03-gestione-dati.md` §3.6 / §6](../../../progettazione/03-gestione-dati.md) e
[`docs/note.md`](../../../progettazione/note.md).

## Suggerimenti per le prestazioni

- **Tabella grande (~224k righe, in crescita con le prenotazioni)** e sotto due percorsi caldi:
  1. **find-or-create al momento della prenotazione**: `WHERE property_id = $1 AND (phone = $2 OR
     lower(email) = $3) AND deleted_at IS NULL` → serviti dagli indici parziali compositi
     `(property_id, phone)` e `(property_id, lower(email))`.
  2. **lista/ricerca contatti nel gestionale**: `WHERE property_id = $1 AND deleted_at IS NULL ORDER BY
     full_name` → `(property_id, full_name)` parziale (ex `idx_propertyId_fullName`).
- `property_id` **sempre** prima colonna degli indici compositi (uguaglianza prima del range, §3.11).
- Indici **parziali** `WHERE deleted_at IS NULL`: la tabella accumula contatti soft-deleted (~2%) e di
  test — non devono appesantire le scansioni vive.
- Tenere i campi "estesi" (indirizzo, P.IVA, CF, date, note) **fuori** dai DTO di lista: proiezioni
  esplicite → meno I/O e niente PII di troppo nelle risposte di lista.
- **Non** unificare con [`identity_access_users`](identity_access_users.md): metterebbe una tabella da
  ~230k righe sotto **anche** il percorso caldo del login (`WHERE lower(email) = ?`) e bloaterebbe ogni
  index scan con colonne auth inutilizzate.
- Retention: `ix_..._last_activity` serve il batch "anonimizza clienti inattivi da **48 mesi**" senza
  full scan (valore validato dal titolare, [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)).
- Partizionamento per `property_id` (hash/list) o per range temporale: **solo** con un driver concreto
  di volume/prestazioni ([`docs/34-multitenant.md` §2](../../../progettazione/34-multitenant.md),
  [`docs/53-prestazioni.md`](../../../progettazione/53-prestazioni.md)) — non al day one.

## Implementazione .NET

Aggregate root del modulo `Module.Identity`, sottoarea `Customer` (il prefisso fisico resta
`identity_customer_*`; il 2026-10-07 è stato deciso che la rubrica **resta in Identity**, non in un
modulo `Customers/CRM` separato — vedi
[`docs/pre/autenticazione-autorizzazione-dotnet.md` §9](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md)),
schema `identity`. Factory `Customer.Create(...)` assegna `PublicId = Guid.CreateVersion7()`. Implementa
`ISoftDeletable`/`IAuditable`.

### Enum proprio

```csharp
// Domain/Enums/CustomerSource.cs
public enum CustomerSource { ContactForm = 0, Booking = 1, ServiceReservation = 2, CardTransaction = 3, Import = 4 }
```

```csharp
modelBuilder.HasPostgresEnum<CustomerSource>("identity", "customer_source");
dataSourceBuilder.MapEnum<CustomerSource>("identity.customer_source");
```

### `CustomerConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
	public void Configure(EntityTypeBuilder<Customer> builder)
	{
		builder.ToTable("customer_customers", "identity", t => t.HasComment(
			"Rubrica dei clienti finali per singolo stabilimento (contatti da prenotazioni/transazioni di cassa)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_customer_customers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, riferimento cross-modulo (Booking/Payment/Fiskaly).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog) a cui il contatto appartiene, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.HasComment("Account di login (ruolo consumer) collegato a questo contatto, se esiste.");

		builder.Property(x => x.FullName)
			.HasColumnName("full_name")
			.HasMaxLength(200)
			.HasComment("Nome del cliente/contatto.");

		builder.Property(x => x.PhoneCountryCode)
			.HasColumnName("phone_country_code")
			.HasMaxLength(8)
			.HasDefaultValue("+39")
			.HasComment("Prefisso telefonico internazionale del telefono primario.");

		builder.Property(x => x.Phone)
			.HasColumnName("phone")
			.HasMaxLength(32)
			.HasComment("Telefono primario, chiave di matching/deduplica per stabilimento.");

		builder.Property(x => x.PhoneSecondary)
			.HasColumnName("phone_secondary")
			.HasMaxLength(32)
			.HasComment("Recapito telefonico alternativo.");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasColumnType("citext")
			.HasMaxLength(255)
			.HasComment("Email di contatto, seconda chiave di matching.");

		builder.Property(x => x.FiscalCode)
			.HasColumnName("fiscal_code")
			.HasMaxLength(16)
			.HasComment("Codice fiscale, per clienti che richiedono fattura/scontrino intestato.");

		builder.Property(x => x.VatNumber)
			.HasColumnName("vat_number")
			.HasMaxLength(20)
			.HasComment("Partita IVA per clienti business.");

		builder.Property(x => x.Address)
			.HasColumnName("address")
			.HasMaxLength(255)
			.HasComment("Indirizzo di residenza/fatturazione.");

		builder.Property(x => x.City)
			.HasColumnName("city")
			.HasMaxLength(120)
			.HasComment("Città di residenza/fatturazione.");

		builder.Property(x => x.PostalCode)
			.HasColumnName("postal_code")
			.HasMaxLength(16)
			.HasComment("CAP di residenza/fatturazione.");

		builder.Property(x => x.DateOfBirth)
			.HasColumnName("date_of_birth")
			.HasComment("Data di nascita.");

		builder.Property(x => x.OtherNames)
			.HasColumnName("other_names")
			.HasMaxLength(255)
			.HasComment("Nominativi aggiuntivi/alias del contatto.");

		builder.Property(x => x.DiscountNote)
			.HasColumnName("discount_note")
			.HasMaxLength(255)
			.HasComment("Nota testuale libera su sconti/condizioni particolari riservate al cliente.");

		builder.Property(x => x.Note)
			.HasColumnName("note")
			.HasMaxLength(255)
			.HasComment("Nota libera sul cliente.");

		builder.Property(x => x.IsBlacklisted)
			.HasColumnName("is_blacklisted")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Flag di blacklist per questo stabilimento.");

		builder.Property(x => x.BlacklistReason)
			.HasColumnName("blacklist_reason")
			.HasMaxLength(255)
			.HasComment("Motivo della blacklist.");

		builder.Property(x => x.SourceChannel)
			.HasColumnName("source_channel")
			.IsRequired()
			.HasComment("Come è nata la riga (form contatti, prenotazione, cassa, import).");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("Operatore che ha creato/modificato il contatto dal form Contatti; NULL per le creazioni automatiche.");

		builder.Property(x => x.LastActivityAt)
			.HasColumnName("last_activity_at")
			.HasComment("Data dell'ultima prenotazione/transazione che coinvolge questo cliente.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_identity_customer_customers_public_id");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_customers_property_alive");

		builder.HasIndex(x => new { x.PropertyId, x.Phone })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_customers_property_phone");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_customers_property_name");
		// (property_id, full_name): la parte "full_name" resta parte dell'indice composito — vedi
		// migrationBuilder.Sql sotto per l'indice funzionale (property_id, lower(email)).

		builder.HasIndex(x => x.UserId)
			.HasFilter("user_id IS NOT NULL")
			.HasDatabaseName("ix_identity_customer_customers_user");

		builder.HasIndex(x => new { x.PropertyId, x.LastActivityAt })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_customers_last_activity");

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.HasConstraintName("fk_identity_customer_customers_user")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.CreatedByUser)
			.WithMany()
			.HasForeignKey(x => x.CreatedByUserId)
			.HasConstraintName("fk_identity_customer_customers_created_by")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Indice funzionale (`lower(email)`)

`ix_identity_customer_customers_property_email` è un indice parziale **su espressione**
(`(property_id, lower(email))`), non rappresentabile con `HasIndex` fluent (stesso limite di
[`identity_access_users`](identity_access_users.md)). Aggiunto in coda alla migration generata:

```csharp
migrationBuilder.Sql(@"
	CREATE INDEX ix_identity_customer_customers_property_email
		ON identity.customer_customers (property_id, lower(email))
		WHERE deleted_at IS NULL AND email IS NOT NULL;
");
```

### Servizi applicativi

- Servizio di **find-or-create con matching esplicito**: valida il formato di telefono/email prima di
  usarli come chiave; nessun merge silenzioso su segnaposto (`"99999999"`, `"3333333333"`, `"00000"`
  osservati nel legacy).
- DTO: `CustomerListItemDto` (email/telefono mascherati) vs `CustomerDetailDto` con autorizzazione
  per-risorsa ([`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md)).
- Handler `CustomerErasureRequested`: anonimizzazione (`"Cliente cancellato #<id>"`), mantenendo `Id` +
  minimi fiscali entro i 10 anni ([`docs/11-modulo-privacy.md` §5](../../../progettazione/11-modulo-privacy.md)).
- Consumo eventi Booking/Payment per aggiornare `LastActivityAt`.
- Override autorizzato server-side del blocco blacklist (ruolo/permesso), mai un flag nel payload come
  il legacy `allowBookingForBlacklistedCustomer`.

### Note di migrazione

- Preservare `id` 1:1; generare `public_id` nuovo.
- Consolidare `mobile`+`phone`+`otherMobileNumber` → `phone`+`phone_secondary`; `referenceId 0` →
  `user_id NULL`; `isDelete` → `deleted_at`.
- Risolvere `property_id` `0`/`NULL` (dati sporchi legacy) verso una property valida o scartare la riga.
- Normalizzare placeholder (`"Unknown"`, `""`) e scartare `date_of_birth` epoch/odierne/future.
- `customers.is_guest` **non** migrato qui: diventa un attributo del legame `booking ↔ customer` in C1.
