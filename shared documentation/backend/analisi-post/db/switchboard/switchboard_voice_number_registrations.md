# Tabella `switchboard_voice_number_registrations`

> **Dominio**: C7 — `switchboard` · **sottoarea**: `voice` · **schema**: `switchboard` · **nome flat**:
> `switchboard_voice_number_registrations` · **forma canonica**: `switchboard.voice_number_registrations`.
>
> **Nuova** — scorporo dei campi `reg_*` + `telnyx_number_order_*` di
> [`switchboard_config`](../../../analisi-pre/db/switchboard_config.md) (0 righe su `local_sb`/dump
> di produzione — ma **verificato 2026-09-11**: su `summer_booking_dev` **tutti** i campi `reg_*` della
> riga reale sono valorizzati con un KYC vero, non un placeholder: lo scorporo e la cifratura non sono
> precauzioni teoriche, proteggono un caso reale già esistente). Isola i **dati KYC / regolatori**
> dell'ordine numero Telnyx IT, che [`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md)
> classifica come **PII ad alto rischio** (codice fiscale personale, documento d'identità, data/luogo
> di nascita, id documenti caricati) da cifrare a riposo, con accesso ristretto al ruolo `systemadmin`
> e **audit log di ogni lettura**, mai in export/log.
>
> **Fonti**: [`analisi-pre/db/switchboard_config.md`](../../../analisi-pre/db/switchboard_config.md)
> (campi `reg_*`, `telnyx_number_order_id`, `telnyx_phone_number_status`),
> [`docs/11-modulo-privacy.md` §4 / §5 / §"Domande aperte" 1](../../../progettazione/11-modulo-privacy.md)
> (dubbio D.L. 7/2015), [`docs/80-retention.md` §3.7](../../../progettazione/80-retention.md),
> [`docs/17-secrets.md`](../../../progettazione/17-secrets.md),
> [`docs/03-gestione-dati.md` §2.4quinquies](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga per centralino** (`config_id` `UNIQUE`, 1:1 con [`switchboard_configs`](switchboard_configs.md)):
lo stato dell'**ordine del numero PSTN italiano** su Telnyx e il **fascicolo dei requisiti regolatori**
(requirement group Telnyx per l'IT: dati anagrafici del referente, documento d'identità, prova di
indirizzo, visura camerale). Nel legacy erano ~24 colonne `reg_*`/`telnyx_*` mescolate nella tabella
di configurazione, senza cifratura né controllo d'accesso dedicato.

Correzioni rispetto al legacy:

- **Colonne KYC nella tabella di config** → tabella separata, **cifrata a riposo** sui 4 campi 🔒
  (`reg_identity_doc_number`, `reg_date_of_birth`, `reg_place_of_birth`, `reg_personal_tax_code`),
  accesso via policy `systemadmin` + audit di lettura. **Meccanismo scelto**: cifratura **applicativa**
  colonna per colonna via **AWS KMS**, non TDE a livello di volume/tablespace — TDE protegge solo dal
  furto del disco/backup, non da un accesso diretto al DB con permessi di lettura (che è il rischio
  primario per un servizio DB condiviso come questo); la cifratura applicativa protegge anche quel
  caso, perché il DB vede solo `bytea` illeggibile.
  - `IColumnEncryptor.Encrypt/Decrypt` chiama direttamente `KMS.Encrypt`/`KMS.Decrypt` (i valori sono
    corti — codice fiscale, numero documento, date — ben sotto il limite di 4 KB di KMS: **non serve**
    un envelope con data-key locale) con una **chiave gestita (CMK) per ambiente** (da configurazione,
    non per riga) e un **encryption context** `{table, column}` legato al valore, così un ciphertext
    copiato in un'altra colonna/tabella **non si decifra** (KMS rifiuta se il context non combacia).
  - **Ogni chiamata a `Decrypt`** passa per un unico punto (l'`IColumnEncryptor`) che emette l'evento di
    audit `KycFieldAccessed` (chi, quando, quale campo) — [`docs/31`](../../../progettazione/31-observability.md);
    nessuna lettura di questi campi bypassa il wrapper.
  - **Rotazione**: gestita da KMS (rotazione automatica annuale della CMK) in modo trasparente, senza
    ri-cifratura dei dati esistenti; una migrazione a una CMK diversa (es. cambio account) richiede un
    job batch una tantum di decifra/ricifra.
  - **In locale**: `IColumnEncryptor` punta a **LocalStack KMS** (stesso SDK, stesso comportamento) —
    stesso principio del `ISecretResolver` per i segreti dinamici (vedi
    [`switchboard_configs.md`](switchboard_configs.md)`.sip_password_secret_ref`); **mai** una CMK
    reale né dati KYC reali in ambienti di sviluppo/test.
- **Date come stringa** (`reg_identity_doc_expiry` / `reg_date_of_birth` `varchar(10)` `"YYYY-MM-DD"`)
  → `date`.
- **`telnyx_phone_number_status`** (passthrough Telnyx grezzo) → `provisioning_state` **enum interno
  derivato** + `telnyx_status_raw` grezzo conservato (stesso pattern di
  `payment` Stripe / `fiskaly_state_raw`).
- **`reg_it_customer_type` / `reg_identity_doc_type`** (vocabolari chiusi imposti da Telnyx) → `enum`
  passthrough.
- **Id documenti/address Telnyx** (`reg_*_doc_id`, `reg_address_id`, `reg_supplemental_address_id`,
  `reg_requirement_group_id`) → identificatori esterni opachi, conservati per PATCH/DELETE lato Telnyx.
- **Retention**: vivono con lo stabilimento; 🗑️ alla cessazione + eventuale periodo post-cessazione da
  definire ([`docs/11 §"Domande aperte"`](../../../progettazione/11-modulo-privacy.md), dubbio D.L. 7/2015 —
  vedi [D-51](../_domande-committente.md)).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `provisioning_state` | **enum esclusivo** — un solo stato dell'ordine numero alla volta (`not_started` → `documents_pending` → `submitted` → `active` / `rejected` / `released`). |
| `reg_it_customer_type` | **enum esclusivo** (`individual` / `business`) — imposto da Telnyx. |
| `reg_identity_doc_type` | **enum esclusivo** (`passport` / `national_id` / `driving_license`) — imposto da Telnyx. |
| — | nessun gruppo di flag booleani → nessun `enum[]`. |

## Snapshot

Non applicabile: sono dati anagrafici/documentali **correnti** del referente, aggiornati quando
cambiano (rinnovo documento ⇒ nuova versione della riga, `updated_at` registra il "quando"). Nessuna
copia congelata da un'altra entità.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_voice_number_registrations` | `id` | PK | — |
| `uq_..._config` | `(config_id)` | UNIQUE | 1:1 con la configurazione del centralino |
| `ix_..._state` | `(provisioning_state) WHERE provisioning_state IN ('documents_pending','submitted')` | parziale | polling degli ordini numero in corso (`getNumberOrderStatus`) |
| `ix_..._doc_expiry` | `(reg_identity_doc_expiry) WHERE reg_identity_doc_expiry IS NOT NULL` | parziale | promemoria di scadenza documento del referente |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica interna allo schema.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### config_id
- **Legacy**: `switchboard_config.id` (i campi `reg_*` erano colonne della stessa riga)
- **Descrizione**: centralino a cui appartiene il fascicolo. 1:1.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`switchboard_configs`](switchboard_configs.md)`.id`, `UNIQUE`,
  `ON DELETE CASCADE` (fascicolo cancellato con la config).

### property_id
- **Legacy**: `switchboard_config.property_id`
- **Descrizione**: stabilimento (denormalizzato per scoping multi-tenant e policy d'accesso).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo per valore.

### provisioning_state
- **Legacy**: derivato da `switchboard_config.telnyx_phone_number_status` (`"pending"` all'ordine, poi `"active"` / `"provisioning"` / …)
- **Descrizione**: stato interno dell'ordine numero e del suo ciclo di vita.

  | Valore | Significato | Il numero è utilizzabile? |
  |---|---|---|
  | `not_started` | Nessun ordine tentato; nessun dato KYC ancora raccolto. | No |
  | `documents_pending` | KYC in raccolta (form regolatorio compilato/in compilazione); documenti/indirizzi caricati ma requirement group **non ancora inviato** a Telnyx. | No |
  | `submitted` | Requirement group + ordine numero **inviati** a Telnyx; in attesa di provisioning/approvazione (`getNumberOrderStatus` in polling). | No |
  | `active` | Numero **provisionato e assegnato** allo stabilimento **ora**: `switchboard_configs.telnyx_phone_e164` è valorizzato e il centralino può ricevere chiamate su quel numero. | **Sì** |
  | `rejected` | Telnyx ha **rifiutato** l'ordine (KYC/documenti non validi) **prima** di arrivare ad `active` — stato terminale, nessun numero mai assegnato. | No |
  | `released` | Il numero **era** `active` ma è stato **rilasciato** (l'operatore ha disdetto il centralino, o il numero è stato deprovisionato): stato terminale **dopo** `active`, non prima. `switchboard_configs.telnyx_phone_e164` viene azzerato, ma questa riga **resta** come registro storico ("questo stabilimento ha avuto il numero X fino al `released_at`"), utile per audit/fatturazione pregressa. | No |

  **Differenza chiave `active` vs `released`**: entrambi indicano "il numero non è (più) `pending`", ma
  `active` = lo stabilimento **possiede e usa** il numero adesso; `released` = lo **ha posseduto in
  passato** e non lo usa più. Non sono intercambiabili: un ordine può tornare da `active` a
  `released` (dismissione), ma non il contrario — un nuovo utilizzo dello stesso numero (raro, dipende
  da Telnyx) richiederebbe un nuovo ordine, quindi in pratica una nuova riga o un nuovo `not_started`
  esplicito, non un ritorno diretto ad `active`.
- **Tipo**: `enum switchboard.number_provisioning_state` — valori: `not_started`, `documents_pending`,
  `submitted`, `active`, `rejected`, `released`.
- **Default**: `not_started`.
- **Nullabile**: No.
- **Relazioni**: —.

### telnyx_status_raw
- **Legacy**: `switchboard_config.telnyx_phone_number_status` (`varchar(50)`)
- **Descrizione**: stato del numero **così come restituito da Telnyx**, conservato grezzo per
  diagnostica (l'enum interno è derivato da questo).
- **Tipo**: `varchar(50)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### telnyx_number_order_id
- **Legacy**: `switchboard_config.telnyx_number_order_id` (`varchar(100)`)
- **Descrizione**: id del *number order* Telnyx, per il polling dello stato di provisioning.
  Identificatore esterno opaco.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### telnyx_phone_number_id
- **Legacy**: `switchboard_config.telnyx_phone_number_id` (`varchar(50)`)
- **Descrizione**: id dell'oggetto *phone number* Telnyx (`/v2/phone_numbers/{id}`), per PATCH
  (assegnazione Call Control App / messaging profile), GET stato, release. Identificatore esterno
  opaco.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —. Il numero E.164 risultante è salvato in
  [`switchboard_configs`](switchboard_configs.md)`.telnyx_phone_e164` (chiave di routing).

### reg_requirement_group_id
- **Legacy**: `switchboard_config.reg_requirement_group_id` (`varchar(100)`)
- **Descrizione**: id del *requirement group* Telnyx (contenitore dei requisiti regolatori compilati),
  passato all'ordine numero. Identificatore esterno opaco.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_address_id / reg_supplemental_address_id
- **Legacy**: `switchboard_config.reg_address_id` / `.reg_supplemental_address_id` (`varchar(100)`)
- **Descrizione**: id degli *address* Telnyx (principale e supplementare) usati per i requisiti
  regolatori. Identificatori esterni opachi.
- **Tipo**: `varchar(64)` ciascuno.
- **Default**: `NULL` / `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: —.

### reg_proof_of_address_doc_id / reg_id_document_copy_doc_id / reg_company_reg_cert_doc_id
- **Legacy**: `switchboard_config.reg_proof_of_address_doc_id` / `.reg_id_document_copy_doc_id` / `.reg_company_reg_cert_doc_id` (`varchar(100)`)
- **Descrizione**: id dei *document* Telnyx (PDF) caricati: prova di indirizzo, copia documento
  d'identità, certificato camerale/visura. Identificatori esterni opachi (il file vive su Telnyx, non
  nel nostro storage).
- **Tipo**: `varchar(64)` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_it_customer_type
- **Legacy**: `switchboard_config.reg_it_customer_type` (`varchar(50)`)
- **Descrizione**: tipo cliente IT richiesto da Telnyx per l'ordine numero locale.
- **Tipo**: `enum switchboard.it_customer_type` — valori: `individual`, `business`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_end_user_name
- **Legacy**: `switchboard_config.reg_end_user_name` (`varchar(100)`)
- **Descrizione**: nome dell'utilizzatore finale del numero (persona o azienda).
- **Tipo**: `varchar(150)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_identity_doc_type
- **Legacy**: `switchboard_config.reg_identity_doc_type` (`varchar(50)`)
- **Descrizione**: tipo del documento d'identità del referente.
- **Tipo**: `enum switchboard.identity_doc_type` — valori: `passport`, `national_id`, `driving_license`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_identity_doc_number 🔒
- **Legacy**: `switchboard_config.reg_identity_doc_number` (`varchar(40)`)
- **Descrizione**: numero del documento d'identità. **PII ad alto rischio** → cifrato a riposo.
- **Tipo**: `bytea` (valore cifrato) — in chiaro `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_identity_doc_issuer
- **Legacy**: `switchboard_config.reg_identity_doc_issuer` (`varchar(40)`)
- **Descrizione**: ente emittente del documento.
- **Tipo**: `varchar(80)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_identity_doc_expiry
- **Legacy**: `switchboard_config.reg_identity_doc_expiry` (`varchar(10)` `"YYYY-MM-DD"`)
- **Descrizione**: data di scadenza del documento — il **giorno solare stampato sul documento**, la
  stessa cifra che oggi vive nella stringa legacy.
- **Tipo**: `date` (in .NET: `DateOnly`, **non** `DateTime`/`DateTimeOffset`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Fuso orario**: **nessuno — non si applica**. `date` (e `DateOnly`) rappresentano un **giorno di
  calendario puro**, senza componente ora: non c'è "UTC" o "fuso locale" da scegliere, perché non è un
  istante nel tempo (a differenza di `created_at`/`updated_at`, che sono `timestamptz`). Migrazione: la
  stringa legacy `"YYYY-MM-DD"` **si copia 1:1** nel valore `date`, senza alcuna conversione — è già la
  rappresentazione del giorno voluto.

### reg_date_of_birth 🔒
- **Legacy**: `switchboard_config.reg_date_of_birth` (`varchar(10)` `"YYYY-MM-DD"`)
- **Descrizione**: data di nascita del referente — stesso principio di `reg_identity_doc_expiry` sopra
  (giorno di calendario puro, nessun fuso orario). **PII** → cifrata a riposo.
- **Tipo**: `bytea` (cifrato) — in chiaro `date` (`DateOnly`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Nota tecnica**: la cifratura applicativa (vedi §"Correzioni rispetto al legacy") cifra la
  **rappresentazione seriale** del `DateOnly` (es. `"1991-12-10"`), non un `timestamp`: decifrando si
  riottiene lo stesso giorno di calendario, senza ambiguità di fuso.

### reg_place_of_birth 🔒
- **Legacy**: `switchboard_config.reg_place_of_birth` (`varchar(100)`)
- **Descrizione**: luogo di nascita del referente. **PII** → cifrato a riposo.
- **Tipo**: `bytea` (cifrato) — in chiaro `varchar(100)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_personal_tax_code 🔒
- **Legacy**: `switchboard_config.reg_personal_tax_code` (`varchar(20)`)
- **Descrizione**: codice fiscale **personale** del referente. **PII ad alto rischio** → cifrato a
  riposo, mai in export/log.
- **Tipo**: `bytea` (cifrato) — in chiaro `varchar(16)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_company_tax_code
- **Legacy**: `switchboard_config.reg_company_tax_code` (`varchar(20)`)
- **Descrizione**: codice fiscale aziendale.
- **Tipo**: `varchar(16)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reg_vat_number
- **Legacy**: `switchboard_config.reg_vat_number` (`varchar(20)`)
- **Descrizione**: partita IVA.
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### submitted_at / activated_at / released_at
- **Legacy**: — (nuovo; il legacy non tracciava il "quando")
- **Descrizione**: istante di invio del requirement group a Telnyx (→ `submitted`) / di attivazione del
  numero (→ `active`) / di rilascio del numero (→ `released`). Insieme a `provisioning_state`
  permettono di distinguere `active` da `released` anche solo guardando quale timestamp è
  valorizzato: `released_at IS NOT NULL` ⇒ stato attuale `released` (a meno di re-instradamento con
  nuovo ordine, non previsto in questo modello).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `CHECK (released_at IS NULL OR activated_at IS NOT NULL)` — non si rilascia un numero
  mai stato attivo (quel caso è `rejected`, non `released`).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `switchboard_config` — `timestamps: false`
- **Descrizione**: creazione / ultima modifica (anche rinnovo documento) / soft-delete (rilascio
  numero / cessazione). Retention: 🗑️ alla cessazione dello stabilimento
  ([`docs/80 §5`](../../../progettazione/80-retention.md)); eventuale conservazione post-cessazione →
  [D-51](../_domande-committente.md).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. I `reg_*` sono già colonne piatte nel legacy e sono **dati nostri** (fatti anagrafici del
referente, con date interrogabili per i promemoria di scadenza): restano colonne tipizzate, con
cifratura a riposo sui campi ad alto rischio. Non è un documento di terzi (il payload che va a Telnyx
si compone in applicazione dai singoli campi).

## Suggerimenti per le prestazioni

- 1 riga per centralino: tabella minuscola. Gli indici parziali su `provisioning_state` e
  `reg_identity_doc_expiry` servono ai job (polling ordine, promemoria scadenza), non al percorso
  caldo delle chiamate.
- La cifratura colonna-level ha costo solo su lettura/scrittura di questa tabella, che è rara (setup +
  aggiornamenti sporadici) — nessun impatto sul runtime IVR, che non tocca questa tabella.
- Accesso segregato: una connessione/ruolo DB dedicato con permessi solo su questo `config_id` via RLS
  ([`docs/34-multitenant.md`](../../../progettazione/34-multitenant.md)).

## Implementazione .NET

`VoiceNumberRegistration` è un'**entità figlia** 1:1 con [`SwitchboardConfig`](switchboard_configs.md)
(modulo `Module.Switchboard`, area `Voice`): isola il fascicolo KYC/regolatorio dell'ordine numero PSTN
IT, ad accesso ristretto. 0 righe su `local_sb`/dump di produzione, ma su `summer_booking_dev` la riga
reale ha **tutti** i campi `reg_*` valorizzati con un KYC vero — lo scorporo e la cifratura proteggono
un caso già esistente, non solo un rischio teorico.

### Enum

```csharp
// Module.Switchboard.Domain.Enums.Voice/NumberProvisioningState.cs
public enum NumberProvisioningState
{
	NotStarted = 0,
	DocumentsPending = 1,
	Submitted = 2,
	Active = 3,
	Rejected = 4,
	Released = 5
}

// Module.Switchboard.Domain.Enums.Voice/ItCustomerType.cs
public enum ItCustomerType
{
	Individual = 0,
	Business = 1
}

// Module.Switchboard.Domain.Enums.Voice/IdentityDocType.cs
public enum IdentityDocType
{
	Passport = 0,
	NationalId = 1,
	DrivingLicense = 2
}
```

```csharp
// SwitchboardDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<NumberProvisioningState>("switchboard", "number_provisioning_state");
modelBuilder.HasPostgresEnum<ItCustomerType>("switchboard", "it_customer_type");
modelBuilder.HasPostgresEnum<IdentityDocType>("switchboard", "identity_doc_type");
```

```csharp
// Module.Switchboard.Infrastructure DI
dataSourceBuilder.MapEnum<NumberProvisioningState>("switchboard.number_provisioning_state");
dataSourceBuilder.MapEnum<ItCustomerType>("switchboard.it_customer_type");
dataSourceBuilder.MapEnum<IdentityDocType>("switchboard.identity_doc_type");
```

### Campi cifrati 🔒 — non un `ValueConverter` EF

I 4 campi 🔒 (`reg_identity_doc_number`, `reg_date_of_birth`, `reg_place_of_birth`,
`reg_personal_tax_code`) **non** usano un `ValueConverter` EF: `IColumnEncryptor.Encrypt`/`Decrypt`
chiama `KMS.Encrypt`/`Decrypt` in modo **asincrono** (chiamata di rete), incompatibile con la
conversione sincrona di un `ValueConverter`. La cifratura/decifratura avviene esplicitamente
nell'Application layer (comando/query dedicati), **prima** dell'assegnazione all'entità e **dopo** la
lettura da EF; l'entità espone le proprietà come `byte[]` (ciphertext) mappate su `bytea`, mai come
`string`/`DateOnly` in chiaro — evita che un `SaveChanges`/proiezione LINQ generico scriva o legga per
sbaglio testo in chiaro. Ogni chiamata a `Decrypt` passa per l'unico punto `IColumnEncryptor` che emette
l'evento di audit `KycFieldAccessed`.

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceNumberRegistrationConfiguration : IEntityTypeConfiguration<VoiceNumberRegistration>
{
	public void Configure(EntityTypeBuilder<VoiceNumberRegistration> builder)
	{
		builder.ToTable("switchboard_voice_number_registrations", "switchboard", t => t.HasComment(
			"Stato dell'ordine del numero PSTN italiano su Telnyx e fascicolo dei requisiti regolatori (KYC) del centralino, 1:1 con la configurazione."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_number_registrations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino a cui appartiene il fascicolo (1:1).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping e policy d'accesso.");

		builder.Property(x => x.ProvisioningState)
			.HasColumnName("provisioning_state")
			.HasColumnType("switchboard.number_provisioning_state")
			.IsRequired()
			.HasDefaultValue(NumberProvisioningState.NotStarted)
			.HasComment("Stato interno dell'ordine numero e del suo ciclo di vita (not_started → documents_pending → submitted → active/rejected/released).");

		builder.Property(x => x.TelnyxStatusRaw)
			.HasColumnName("telnyx_status_raw")
			.HasMaxLength(50)
			.HasComment("Stato del numero così come restituito da Telnyx, conservato grezzo per diagnostica.");

		builder.Property(x => x.TelnyxNumberOrderId)
			.HasColumnName("telnyx_number_order_id")
			.HasMaxLength(64)
			.HasComment("Id del number order Telnyx, per il polling dello stato di provisioning.");

		builder.Property(x => x.TelnyxPhoneNumberId)
			.HasColumnName("telnyx_phone_number_id")
			.HasMaxLength(64)
			.HasComment("Id dell'oggetto phone number Telnyx, per PATCH/GET stato/release.");

		builder.Property(x => x.RegRequirementGroupId)
			.HasColumnName("reg_requirement_group_id")
			.HasMaxLength(64)
			.HasComment("Id del requirement group Telnyx (contenitore dei requisiti regolatori compilati).");

		builder.Property(x => x.RegAddressId)
			.HasColumnName("reg_address_id")
			.HasMaxLength(64)
			.HasComment("Id dell'address Telnyx principale usato per i requisiti regolatori.");

		builder.Property(x => x.RegSupplementalAddressId)
			.HasColumnName("reg_supplemental_address_id")
			.HasMaxLength(64)
			.HasComment("Id dell'address Telnyx supplementare usato per i requisiti regolatori.");

		builder.Property(x => x.RegProofOfAddressDocId)
			.HasColumnName("reg_proof_of_address_doc_id")
			.HasMaxLength(64)
			.HasComment("Id del document Telnyx caricato come prova di indirizzo.");

		builder.Property(x => x.RegIdDocumentCopyDocId)
			.HasColumnName("reg_id_document_copy_doc_id")
			.HasMaxLength(64)
			.HasComment("Id del document Telnyx caricato come copia del documento d'identità.");

		builder.Property(x => x.RegCompanyRegCertDocId)
			.HasColumnName("reg_company_reg_cert_doc_id")
			.HasMaxLength(64)
			.HasComment("Id del document Telnyx caricato come certificato camerale/visura.");

		builder.Property(x => x.RegItCustomerType)
			.HasColumnName("reg_it_customer_type")
			.HasColumnType("switchboard.it_customer_type")
			.HasComment("Tipo cliente IT richiesto da Telnyx per l'ordine numero locale.");

		builder.Property(x => x.RegEndUserName)
			.HasColumnName("reg_end_user_name")
			.HasMaxLength(150)
			.HasComment("Nome dell'utilizzatore finale del numero (persona o azienda).");

		builder.Property(x => x.RegIdentityDocType)
			.HasColumnName("reg_identity_doc_type")
			.HasColumnType("switchboard.identity_doc_type")
			.HasComment("Tipo del documento d'identità del referente.");

		builder.Property(x => x.RegIdentityDocNumberEncrypted)
			.HasColumnName("reg_identity_doc_number")
			.HasColumnType("bytea")
			.HasComment("Numero del documento d'identità del referente — PII ad alto rischio, cifrato a riposo (KMS).");

		builder.Property(x => x.RegIdentityDocIssuer)
			.HasColumnName("reg_identity_doc_issuer")
			.HasMaxLength(80)
			.HasComment("Ente emittente del documento d'identità.");

		builder.Property(x => x.RegIdentityDocExpiry)
			.HasColumnName("reg_identity_doc_expiry")
			.HasColumnType("date")
			.HasComment("Data di scadenza del documento d'identità (giorno di calendario puro).");

		builder.Property(x => x.RegDateOfBirthEncrypted)
			.HasColumnName("reg_date_of_birth")
			.HasColumnType("bytea")
			.HasComment("Data di nascita del referente — PII, cifrata a riposo (KMS).");

		builder.Property(x => x.RegPlaceOfBirthEncrypted)
			.HasColumnName("reg_place_of_birth")
			.HasColumnType("bytea")
			.HasComment("Luogo di nascita del referente — PII, cifrato a riposo (KMS).");

		builder.Property(x => x.RegPersonalTaxCodeEncrypted)
			.HasColumnName("reg_personal_tax_code")
			.HasColumnType("bytea")
			.HasComment("Codice fiscale personale del referente — PII ad alto rischio, cifrato a riposo, mai in export/log.");

		builder.Property(x => x.RegCompanyTaxCode)
			.HasColumnName("reg_company_tax_code")
			.HasMaxLength(16)
			.HasComment("Codice fiscale aziendale.");

		builder.Property(x => x.RegVatNumber)
			.HasColumnName("reg_vat_number")
			.HasMaxLength(20)
			.HasComment("Partita IVA.");

		builder.Property(x => x.SubmittedAt)
			.HasColumnName("submitted_at")
			.HasComment("Istante di invio del requirement group a Telnyx.");

		builder.Property(x => x.ActivatedAt)
			.HasColumnName("activated_at")
			.HasComment("Istante di attivazione del numero.");

		builder.Property(x => x.ReleasedAt)
			.HasColumnName("released_at")
			.HasComment("Istante di rilascio del numero; la riga resta come registro storico.");

		builder.ToTable(t => t.HasCheckConstraint(
			"ck_switchboard_voice_number_registrations_release_sequence",
			"released_at IS NULL OR activated_at IS NOT NULL"));

		builder.HasIndex(x => x.ConfigId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_voice_number_registrations_config");

		builder.HasIndex(x => x.ProvisioningState)
			.HasFilter("provisioning_state IN ('documents_pending','submitted')")
			.HasDatabaseName("ix_switchboard_voice_number_registrations_state");

		builder.HasIndex(x => x.RegIdentityDocExpiry)
			.HasFilter("reg_identity_doc_expiry IS NOT NULL")
			.HasDatabaseName("ix_switchboard_voice_number_registrations_doc_expiry");

		builder.HasOne(x => x.Config)
			.WithOne(c => c.NumberRegistration)
			.HasForeignKey<VoiceNumberRegistration>(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_number_registrations_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`VoiceNumberRegistration` implementa `ISoftDeletable`/`IAuditable`. Accesso segregato via ruolo/
connessione DB dedicata con RLS su `config_id`
([`docs/34-multitenant.md`](../../../progettazione/34-multitenant.md)); esclusione **strutturale** dai DTO di
export GDPR e dai log (DTO dedicato che non proietta i campi cifrati, non un `if` a valle).

### Provider esterno

`ITelephonyProvider.OrderNumber` / `GetNumberOrderStatus` / `CreateRequirementGroup` /
`UploadDocument` (Infrastructure, wrapper Telnyx) — mai loggare il body della richiesta/risposta.

### Note di migrazione

- 0 righe su `local_sb`/dump di produzione → nessun backfill da quelle copie; su `summer_booking_dev`
  la riga reale ha tutti i `reg_*` valorizzati — se quell'ambiente va portato al nuovo schema, i 4 campi
  🔒 migrano già cifrati (mai in chiaro su disco/log durante la migrazione), le date `varchar(10)`
  `"YYYY-MM-DD"` si copiano 1:1 nel valore `date`/`DateOnly`.
- `telnyx_phone_number_status` grezzo si scinde in `provisioning_state` (enum derivato) +
  `telnyx_status_raw` (grezzo conservato).
