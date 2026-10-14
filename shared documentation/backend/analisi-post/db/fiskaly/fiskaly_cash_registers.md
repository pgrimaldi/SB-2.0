# Tabella `fiskaly_cash_registers`

> **Dominio**: C4 — `fiskaly` · **sottoarea**: — · **schema**: `fiskaly` · **nome flat**:
> `fiskaly_cash_registers` · **forma canonica**: `fiskaly.cash_registers`.
>
> Sostituisce [`fiskaly_cash_register`](../../../analisi-pre/db/fiskaly_cash_register.md) (5 righe
> reali). Fonti:
> [`analisi-pre/db/fiskaly_cash_register.md`](../../../analisi-pre/db/fiskaly_cash_register.md),
> [`docs/03-gestione-dati.md` §2.4ter, §3.5](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga = un registratore di cassa / *System* `FISCAL_DEVICE` registrato su Fiskaly SIGN IT**, con
cui si firmano i documenti commerciali elettronici. **N righe per
[`fiskaly_accounts`](fiskaly_accounts.md)**. Almeno una riga "software" è auto-creata dal backend
(`is_software = true`, producer/software da variabili d'ambiente); il gestore può aggiungere casse
"hardware" definite da lui (`is_software = false`).

Correzioni rispetto al legacy:

- **`system_state` / `system_mode` / `system_type` come `varchar` liberi** (passthrough SIGN IT) →
  `enum` PG ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).
- **`is_default` senza vincolo "una sola default per account"** a schema → `UNIQUE` parziale.
- **Match "è la cassa software?" per stringa esatta** su producer name + software name (fragile: nei
  dati `"Qbitsoft"` / `"SBH Printer Cloud"` / `"Electronic receipt"`) → colonna `is_software`
  autorevole; producer/software solo descrittivi.
- **`createdBy int` senza FK** → `created_by_user_id` / `updated_by_user_id` `uuid`
  ([`docs/03 §3.2`](../../../progettazione/03-gestione-dati.md)).
- **`timestamps: false` + `updatedAt` a mano** → automatici.
- **Nessun `property_id`** sulla riga (scoping via `fiskaly_account_id`) → `property_id` copiato
  (prima colonna degli indici multi-tenant, [`docs/03 §3.1bis`](../../../progettazione/03-gestione-dati.md)).

## Verifica di esclusività dei flag

| Gruppo | Campi | Esito |
|---|---|---|
| Natura della cassa | `is_software`, `is_default` | **due booleani indipendenti** — una cassa può essere software **e** default, software **e** non-default, ecc. Non esclusivi, non enum. |
| Ciclo di vita | `system_state` / `system_mode` | **due enum ortogonali** (come su [`fiskaly_accounts`](fiskaly_accounts.md)): `getCashRegister` per emettere richiede `state = commissioned` **e** `mode = operative`. |

## Snapshot

Nessuno. Anagrafica viva della cassa. Il valore congelato accanto alla FK viva `cash_register_id` su
[`fiskaly_receipts`](fiskaly_receipts.md) è `fiskaly_system_id_snapshot` (l'id `System` opaco, regola
`_snapshot` per identificativi esterni — [`_feature-tabelle.md` → C3-8](../_feature-tabelle.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiskaly_cash_registers` | `id` | PK | — |
| `uq_fiskaly_cash_registers_system` | `(fiskaly_system_id) WHERE fiskaly_system_id IS NOT NULL` | UNIQUE parziale | id `System` Fiskaly univoco; è la chiave con cui scontrini e sequenze si legano alla cassa |
| `uq_fiskaly_cash_registers_default` | `(account_id) WHERE is_default AND deleted_at IS NULL` | UNIQUE parziale | **una sola cassa di default per account** (mancava nel legacy) |
| `ix_fiskaly_cash_registers_account` | `(account_id) WHERE deleted_at IS NULL` | btree parziale | elenco casse di uno stabilimento (`is_default DESC, id ASC`) ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `ix_fiskaly_cash_registers_property_active` | `(property_id) WHERE deleted_at IS NULL AND system_state = 'commissioned' AND system_mode = 'operative'` | btree parziale | selezione cassa per emettere (`getCashRegister`) |

## Campi

### id
- **Legacy**: `fiskaly_cash_register.id`
- **Descrizione**: PK tecnica. Id legacy preservati 1:1.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `fiskaly_receipts.cash_register_id`,
  `fiskaly_receipt_sequences.cash_register_id`.
- **Note**: **solo `bigint`** — referenziata unicamente entro lo schema `fiskaly`, nessun altro
  modulo la referenzia ([`docs/03 §3.1ter`](../../../progettazione/03-gestione-dati.md)). Nessun `public_id`.

### account_id
- **Legacy**: `fiskaly_cash_register.fiskaly_account_id` (`int`, FK `fiskaly_account.id`)
- **Descrizione**: account Fiskaly (quindi stabilimento) a cui la cassa appartiene.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No (nel legacy `SET NULL`; qui la cassa non sopravvive senza account).
- **Relazioni**: **FK interna** → [`fiskaly_accounts.id`](fiskaly_accounts.md); cascade soft-delete.

### property_id
- **Legacy**: — (nuovo; da `fiskaly_account.property_id` in migrazione)
- **Descrizione**: stabilimento proprietario (denormalizzato per lo scoping/indici senza join).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md);
  [D-01](../_domande-committente.md).

### fiskaly_system_id
- **Legacy**: `fiskaly_cash_register.system_id` (`varchar(36)`, UUID v7 Fiskaly, `UNIQUE`)
- **Descrizione**: id del *System* su Fiskaly (`systemResponse.content.id`). Chiave con cui scontrini
  e sequenze giornaliere si legano alla cassa. Azzerato da un decommission che ne forza la ricreazione.
- **Tipo**: `varchar(36)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (tra la creazione del record DB e quella del System su Fiskaly).
- **Relazioni**: identificativo esterno opaco (non enum). `UNIQUE` parziale.

### system_type
- **Legacy**: `fiskaly_cash_register.system_type` (`varchar(50)`, default applicativo `"FISCAL_DEVICE"`)
- **Descrizione**: tipo di System su Fiskaly. Di fatto costante.
- **Tipo**: `enum fiskaly.system_type` — valori: `fiscal_device`.
- **Default**: `fiscal_device`.
- **Nullabile**: No.
- **Relazioni**: —.

### producer_number
- **Legacy**: `fiskaly_cash_register.mpn_number` (`varchar(128)`, *Manufacturer Part Number*)
- **Descrizione**: numero produttore del dispositivo (`producer.number` verso Fiskaly). Cassa
  software: `FISKALY_SW_PRODUCER_NUMBER` (def. `"1"`). **Non modificabile** dopo la creazione
  (regola applicativa).
- **Tipo**: `varchar(128)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (obbligatorio in input per le casse utente).
- **Relazioni**: —.

### producer_name
- **Legacy**: `fiskaly_cash_register.mpn_name` (`varchar(256)`, `producer.details.name`)
- **Descrizione**: nome/descrizione del produttore. Esposto in `getReceipts` come `mpn_name`.
- **Tipo**: `varchar(256)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **descrittivo** — non più usato come chiave di match "cassa software" (vedi
  `is_software`).

### software_name / software_version
- **Legacy**: `fiskaly_cash_register.software_name` / `software_version` (`varchar(128)` ciascuno)
- **Descrizione**: nome / versione del software gestionale (`software.name` / `.version` verso
  Fiskaly). Cassa software: `FISKALY_SW_NAME` (def. `"SummerBooking"`) / `FISKALY_SW_VERSION`
  (def. `"1.0"`). Un cambio forza decommission + ricreazione del System.
- **Tipo**: `varchar(128)` / `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (obbligatori in input per le casse utente).
- **Relazioni**: —.

### system_state
- **Legacy**: `fiskaly_cash_register.system_state` (`varchar(50)`)
- **Descrizione**: stato del System su Fiskaly. `commissioned` è l'unico stato da cui è possibile il
  decommission.
- **Tipo**: `enum fiskaly.entity_state` — valori: `acquired`, `commissioned`, `decommissioned`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `getCashRegister` (emissione) richiede `commissioned`.

### system_mode
- **Legacy**: `fiskaly_cash_register.system_mode` (`varchar(50)`)
- **Descrizione**: modalità operativa del System su Fiskaly.
- **Tipo**: `enum fiskaly.entity_mode` — valori: `inactive`, `operative`, `suspended`, `degraded`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `getCashRegister` (emissione) richiede `operative`.

### is_default
- **Legacy**: `fiskaly_cash_register.is_default` (`tinyint` `NOT NULL` `DEFAULT 0`)
- **Descrizione**: `true` = cassa di default dello stabilimento (usata da `getCashRegister` quando
  non si specifica un `fiskaly_system_id`). Se
  [`fiskaly_accounts.software_cash_register_is_default = true`](fiskaly_accounts.md), il default è la
  cassa software e tutte le casse utente sono forzate a `false`.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` parziale `(account_id) WHERE is_default AND deleted_at IS NULL`.

### is_software
- **Legacy**: `fiskaly_cash_register.is_software` (`tinyint` `NOT NULL` `DEFAULT 0`)
- **Descrizione**: `true` = registratore di cassa **software** auto-creato dal backend; `false` =
  cassa definita dall'utente. `update-account` vieta di modificare via API la cassa software.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: colonna **autorevole** (sostituisce il match per stringa su producer/software).

### created_by_user_id / updated_by_user_id
- **Legacy**: `fiskaly_cash_register.createdBy` (`int`, `users.id`, nessuna FK) / — (nuovo)
- **Descrizione**: operatore che ha creato / modificato la cassa.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`) ciascuno.
- **Default**: nessuno / `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 5 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: `fiskaly_cash_register.createdAt` / `updatedAt` (`datetime`, `timestamps: false`)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: automatici ([`docs/03 §3.4`](../../../progettazione/03-gestione-dati.md)).

### deleted_at
- **Legacy**: `fiskaly_cash_register.deleted_at` (`datetime` `DEFAULT NULL`, **già un vero
  soft-delete** nel legacy)
- **Descrizione**: soft-delete. Valorizzato da `deleteEntities`, `closeAccount`, dalle cascate di
  decommission (cambio dati fiscali/indirizzo/software).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF; cascade dal soft-delete dell'account.

## Valutazione JSON

Nessun JSON (il legacy non ne aveva). Tutti i campi sono scalari tipizzati.

## Suggerimenti per le prestazioni

- Tabella minuscola (≤ pochi registratori per stabilimento). L'unico accesso "caldo" è la selezione
  cassa per emettere lo scontrino → indice parziale su `(property_id)` con i predicati di stato/modo.
- L'unicità del default è garantita a DB, non più solo dal codice.
- Nessun partizionamento / archiviazione necessari.

## Implementazione .NET

`CashRegister` è **owned entity** dell'aggregato `FiskalyAccount` ([`fiskaly_accounts`](fiskaly_accounts.md)):
un registratore di cassa / *System* `FISCAL_DEVICE` su Fiskaly SIGN IT, modulo `Module.Fiskaly`, schema
`fiskaly`. È referenziata via FK interna da `fiskaly_receipts.cash_register_id` e
`fiskaly_receipt_sequences.cash_register_id` — solo `bigint`, nessun `public_id` (mai esposta fuori
schema).

### Enum propri

```csharp
// Domain/Enums/FiskalySystemType.cs
public enum FiskalySystemType { FiscalDevice = 0 }
```

```csharp
// FiskalyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<FiskalySystemType>("fiskaly", "system_type");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<FiskalySystemType>("fiskaly.system_type");
```

`FiskalyEntityState` (`fiskaly.entity_state`) e `FiskalyEntityMode` (`fiskaly.entity_mode`) sono già
definiti e registrati su [`fiskaly_accounts`](fiskaly_accounts.md) — riusati qui senza ridefinizione.

### Configuration

```csharp
namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class FiskalyCashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
	public void Configure(EntityTypeBuilder<CashRegister> builder)
	{
		builder.ToTable("fiskaly_cash_registers", "fiskaly", t => t.HasComment(
			"Registratore di cassa / System FISCAL_DEVICE registrato su Fiskaly SIGN IT, con cui si firmano i documenti commerciali."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_cash_registers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AccountId)
			.HasColumnName("account_id")
			.IsRequired()
			.HasComment("Account Fiskaly (stabilimento) a cui la cassa appartiene.");
		builder.HasIndex(x => x.AccountId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_fiskaly_cash_registers_account");
		builder.HasIndex(x => x.AccountId)
			.IsUnique()
			.HasFilter("is_default AND deleted_at IS NULL")
			.HasDatabaseName("uq_fiskaly_cash_registers_default");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per lo scoping/indici senza join.");
		builder.HasIndex(x => new { x.PropertyId, x.SystemState, x.SystemMode })
			.HasFilter("deleted_at IS NULL AND system_state = 'commissioned' AND system_mode = 'operative'")
			.HasDatabaseName("ix_fiskaly_cash_registers_property_active");

		builder.Property(x => x.FiskalySystemId)
			.HasColumnName("fiskaly_system_id")
			.HasMaxLength(36)
			.HasComment("Id del System su Fiskaly; chiave con cui scontrini e sequenze giornaliere si legano alla cassa.");
		builder.HasIndex(x => x.FiskalySystemId)
			.IsUnique()
			.HasFilter("fiskaly_system_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_cash_registers_system");

		builder.Property(x => x.SystemType)
			.HasColumnName("system_type")
			.HasColumnType("fiskaly.system_type")
			.IsRequired()
			.HasDefaultValue(FiskalySystemType.FiscalDevice)
			.HasComment("Tipo di System su Fiskaly (di fatto costante: FISCAL_DEVICE).");

		builder.Property(x => x.ProducerNumber)
			.HasColumnName("producer_number")
			.HasMaxLength(128)
			.HasComment("Numero produttore del dispositivo (producer.number); non modificabile dopo la creazione.");

		builder.Property(x => x.ProducerName)
			.HasColumnName("producer_name")
			.HasMaxLength(256)
			.HasComment("Nome/descrizione del produttore — campo descrittivo, non chiave di riconoscimento della cassa software.");

		builder.Property(x => x.SoftwareName)
			.HasColumnName("software_name")
			.HasMaxLength(128)
			.HasComment("Nome del software gestionale (software.name verso Fiskaly).");

		builder.Property(x => x.SoftwareVersion)
			.HasColumnName("software_version")
			.HasMaxLength(64)
			.HasComment("Versione del software gestionale (software.version verso Fiskaly); un cambio forza decommission e ricreazione del System.");

		builder.Property(x => x.SystemState)
			.HasColumnName("system_state")
			.HasColumnType("fiskaly.entity_state")
			.HasComment("Stato del System su Fiskaly (acquired/commissioned/decommissioned).");

		builder.Property(x => x.SystemMode)
			.HasColumnName("system_mode")
			.HasColumnType("fiskaly.entity_mode")
			.HasComment("Modalità operativa del System su Fiskaly (inactive/operative/suspended/degraded).");

		builder.Property(x => x.IsDefault)
			.HasColumnName("is_default")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True = cassa di default dello stabilimento, usata quando l'emissione non specifica un system id.");

		builder.Property(x => x.IsSoftware)
			.HasColumnName("is_software")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True = registratore software auto-creato dal backend; colonna autorevole, non modificabile via API.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha creato la cassa.");

		builder.Property(x => x.UpdatedByUserId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Ultimo operatore che ha modificato la cassa.");

		// created_at/updated_at/deleted_at (ISoftDeletable/IAuditable) — plumbing comune, vedi _dotnet-template.md §1-2
	}
}
```

`CashRegister` implementa `IAuditable` e `ISoftDeletable` (soft-delete da `deleteEntities`,
`closeAccount`, cascate di decommission). L'unicità del default (`uq_fiskaly_cash_registers_default`) è
garantita a DB, non più solo dal codice applicativo.

### Note di migrazione

- `fiskaly_cash_register` 1:1; `system_state` / `system_mode` / `system_type` → enum (riuso di
  `entity_state`/`entity_mode` già registrati su `fiskaly_accounts`); `deleted_at` invariato (era già
  un vero soft-delete nel legacy).
- Producer/software di default della cassa software (`FISKALY_SW_PRODUCER_NUMBER`, `FISKALY_SW_NAME`,
  `FISKALY_SW_VERSION`) letti da `IOptions<FiskalySoftwareCashRegisterOptions>` (`appsettings`), non
  cablati in due punti come nel legacy.
