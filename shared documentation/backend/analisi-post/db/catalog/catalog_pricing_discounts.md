# Tabella `catalog_pricing_discounts`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_discounts` · **forma canonica**: `catalog.pricing_discounts`.
>
> Rimpiazza la legacy [`discounts`](../../../analisi-pre/db/discounts.md) (**50 righe** = 1 riga JSON per
> stabilimento; i singoli voucher erano gli elementi dell'array `items`). Fonti:
> [`analisi-pre/db/discounts.md`](../../../analisi-pre/db/discounts.md),
> [`analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md`](../../../analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md),
> [`docs/03-gestione-dati.md` §2.4 (`DiscountType`)](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

Un **voucher/codice sconto** configurato da uno stabilimento. Nel legacy erano gli elementi dell'array
`discounts.items` (JSON **doppiamente incapsulato**), 1 riga per property. Qui: **una riga per voucher**.

Correzioni rispetto al legacy:

- **`name` era sia nome sia codice** (nessun campo codice separato) → `code` dedicato + `name`
  descrittivo.
- **`totalNoOfUsage`** (contatore utilizzi) era **dentro il JSON**, incrementato **non atomicamente** e a
  partire da un valore **fornito dal client** (manipolabile, race-condition). Qui: `used_count` colonna
  con incremento atomico server-side (`UPDATE ... SET used_count = used_count + 1 WHERE used_count <
  max_uses`), contatore **aggregato per voucher** (nessun legame a `customerId` — confermato dai dati).
- **`type`** (`percentage`/`fixed`) → `enum` `DiscountType` (condiviso con gli snapshot sconto di C1).
- **`voucherType`** (`all`/`custom`/`withoutCustom` — 3 stati, il terzo non documentato nel legacy) →
  `enum scope`.
- **`selectedRow`** (nomi settore per stringa) → [`catalog_pricing_discount_sectors`](catalog_pricing_discount_sectors.md).
- **`voucherDateRange[]`** → [`catalog_pricing_discount_date_ranges`](catalog_pricing_discount_date_ranges.md).
- `checkBarService`/`checkNormalService`/`checkHourlyService`/`checkAdditionalServices` → un solo
  **`applies_to_service_categories`** `enum[]`; `checkManagementFlag`/`checkOnlineFlag` → un solo
  **`applicable_channel`** `enum` (`manager_and_online`/`manager_only`/`online_only`).
- **`currentId`** (numero casuale client-side) → scartato.
- l'endpoint di scrittura di massa senza auth (`updateVoucherDateRange`, `POST /api/sb/update`) → non
  riprodotto.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_discounts` | `id` | PK | — |
| `uq_catalog_pricing_discounts_public_id` | `(public_id)` | UNIQUE | snapshot sconto in C1 |
| `uq_..._property_code` | `(property_id, code_normalized) WHERE deleted_at IS NULL` | UNIQUE parziale | codice univoco per stabilimento, case-insensitive (il legacy non lo garantiva). Sulla colonna generata `code_normalized` = `upper(code)` e non su un indice di espressione: EF lo dichiara e lo migra senza SQL a mano |
| `ix_..._property_active` | `(property_id) WHERE is_active AND deleted_at IS NULL` | parziale | validazione codice al checkout |

## Campi

### id
- **Legacy**: — (nuovo; il voucher era un elemento di array)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata dalle due figlie `catalog_pricing_discount_*`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico, **copiato** negli snapshot sconto di C1 (`bookings.booking_discount`,
  `service_reservation.discountData`).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C1.

### property_id
- **Legacy**: `discounts.propertyId`
- **Descrizione**: stabilimento proprietario del voucher.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.

### code
- **Legacy**: `discounts.items[].name` (usato **sia** come nome **sia** come codice digitato dal cliente)
- **Descrizione**: codice che il cliente digita per riscattare lo sconto.
- **Tipo**: `varchar(60)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` per property (case-insensitive), tramite `code_normalized`.

### code_normalized
- **Legacy**: nessuno
- **Descrizione**: proiezione `upper(code)`, esiste solo per l'unicità case-insensitive del codice per
  stabilimento. Non si scrive mai: è una colonna generata.
- **Tipo**: `varchar(60)`, `GENERATED ALWAYS AS (upper(code)) STORED`.
- **Default**: — (colonna generata).
- **Nullabile**: Sì a livello di catalogo; di fatto mai `NULL`, perché `code` è obbligatorio.
- **Relazioni**: chiave di `uq_catalog_pricing_discounts_property_code`. In EF è una **shadow
  property** (`CodeNormalized`), non esposta sull'entità.

### name
- **Legacy**: `discounts.items[].name` (stesso valore — nel legacy non c'era distinzione)
- **Descrizione**: nome descrittivo del voucher (uso interno / mostrato al gestore).
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: Sì (in migrazione = `code`).
- **Relazioni**: —.

### type
- **Legacy**: `discounts.items[].type` (`"percentage"` / `"fixed"`)
- **Descrizione**: tipo di sconto.
- **Tipo**: `enum catalog.discount_type` — valori: `percentage`, `fixed`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **stesso enum** usato in C1 per gli snapshot (`booking_discount.type`,
  `additionalAutoVoucher[].type`, `service_reservation.discountData.type`).

### value
- **Legacy**: `discounts.items[].value` (numerico; percentuale o importo secondo `type`)
- **Descrizione**: entità dello sconto.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: per `percentage` è 0–100; per `fixed` è euro. `CHECK (value >= 0)`.

### scope
- **Legacy**: `discounts.items[].voucherType` (`"all"` / `"custom"` / `"withoutCustom"`)
- **Descrizione**: ambito di applicabilità. `all` = qualunque prenotazione nel periodo; `custom` = con le
  condizioni aggiuntive sotto; `without_custom` = semantica **da chiarire** nel codice
  `discountCodeSearch` (terzo valore non documentato nel legacy).
- **Tipo**: `enum catalog.discount_scope` — valori: `all`, `custom`, `without_custom`.
- **Default**: `all`.
- **Nullabile**: No.
- **Relazioni**: —.

### applies_to_service_categories
- **Legacy**: `discounts.items[].checkBarService` / `checkNormalService` / `checkHourlyService` / `checkAdditionalServices` (4 booleani indipendenti)
- **Descrizione**: per i voucher `scope = custom`, **insieme** delle categorie di servizio a cui il
  voucher è limitato. Array vuoto = nessuna restrizione di categoria.
- **Tipo**: `catalog.discount_service_category[]` — enum: `bar`, `normal`, `hourly`, `additional`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **`enum[]`** anziché 4 booleane — insieme piccolo, stabile, sempre letto con la riga del
  voucher in `checkdiscount`. In migrazione: ogni `checkXService = true` → il relativo valore nell'array.

### applicable_channel
- **Legacy**: `discounts.items[].checkManagementFlag` / `checkOnlineFlag` (2 booleani)
- **Descrizione**: canale/i di prenotazione su cui il voucher è valido. Nel legacy: entrambi `true` →
  valido ovunque; altrimenti deve corrispondere al canale della richiesta; "entrambi false" era di fatto
  un voucher inutilizzabile → collassati in 3 stati utili.
- **Tipo**: `enum catalog.discount_applicable_channel` — valori: `manager_and_online`, `manager_only`,
  `online_only`.
- **Default**: `manager_and_online`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Migrazione**: `checkManagementFlag & checkOnlineFlag` → `manager_and_online`; solo management →
  `manager_only`; solo online → `online_only`; entrambi false → `manager_and_online` (fallback).

### max_uses
- **Legacy**: `discounts.items[].maximunNoUse` (sic — obbligatorio e `>0` lato form)
- **Descrizione**: numero massimo di utilizzi consentiti (aggregato, non per cliente).
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = illimitato).
- **Relazioni**: —.
- **Note**: `CHECK (max_uses IS NULL OR max_uses > 0)`.

### used_count
- **Legacy**: `discounts.items[].totalNoOfUsage` (dentro il JSON, incrementato non atomicamente dal valore del client)
- **Descrizione**: utilizzi già effettuati.
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: incrementato da C1 alla conferma di una prenotazione con questo voucher — **atomico**
  server-side, con `CHECK (max_uses IS NULL OR used_count <= max_uses)`.
- **Note**: risolve manipolabilità e race-condition del legacy.

### is_active
- **Legacy**: `discounts.items[].status` (`1` = attivo; toggle solo riscrivendo l'intero array)
- **Descrizione**: voucher attivo/riscattabile.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_by_user_id
- **Legacy**: `discounts.createdBy`
- **Descrizione**: operatore che ha creato il voucher.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users`.

### created_at / updated_at
- **Legacy**: `discounts.createdAt` / `discounts.updatedAt` (26/50 righe con `updatedAt` = batch migrazione 2025-01-28)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nel legacy si rimuoveva l'elemento dall'array)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON. `discounts.items` (doppiamente incapsulato) → una riga per voucher; array figli
(`selectedRow`, `voucherDateRange`) → tabelle; `currentId` scartato.

## Suggerimenti per le prestazioni

- Poche righe per property. Validazione codice al checkout: 1 lookup su `(property_id, upper(code))` +
  join alle finestre di validità e ai settori.
- Il decremento della disponibilità (`used_count`) è un `UPDATE ... WHERE used_count < max_uses` che
  ritorna 0 righe se esaurito — nessuna race-condition.
- Full-table-scan multi-tenant del legacy (`getpropertyDiscount` caricava gli sconti di **ogni** property)
  eliminato dal filtro `property_id` obbligatorio + indice.

## Implementazione .NET

**Aggregate root** del cluster voucher, modulo `Module.Catalog`, area `Pricing`, schema `catalog`. Radice
di [`catalog_pricing_discount_date_ranges`](catalog_pricing_discount_date_ranges.md) (FK `discount_id`,
esposta come `Discount.ValidityWindows`) e della figlia `catalog_pricing_discount_sectors` (fuori da
questo batch — stessa relazione: FK reale `discount_id`, non ridefinita qui).

### Enum propri

`DiscountType` è definito **qui** (schema `catalog`, tabella proprietaria); C1 ne riusa lo stesso tipo C#
(shared kernel di dominio) per gli **snapshot** sconto (`booking_discount.type`,
`service_reservation.discountData.type`) — sono copie di valore al momento della prenotazione, non FK.

```csharp
// Module.Catalog/Domain/Enums/DiscountType.cs
public enum DiscountType { Percentage = 0, Fixed = 1 }

// Module.Catalog/Domain/Enums/DiscountScope.cs
public enum DiscountScope { All = 0, Custom = 1, WithoutCustom = 2 }

// Module.Catalog/Domain/Enums/DiscountServiceCategory.cs
public enum DiscountServiceCategory { Bar = 0, Normal = 1, Hourly = 2, Additional = 3 }

// Module.Catalog/Domain/Enums/DiscountApplicableChannel.cs
public enum DiscountApplicableChannel { ManagerAndOnline = 0, ManagerOnly = 1, OnlineOnly = 2 }
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<DiscountType>("catalog", "discount_type");
modelBuilder.HasPostgresEnum<DiscountScope>("catalog", "discount_scope");
modelBuilder.HasPostgresEnum<DiscountServiceCategory>("catalog", "discount_service_category");
modelBuilder.HasPostgresEnum<DiscountApplicableChannel>("catalog", "discount_applicable_channel");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<DiscountType>("catalog.discount_type");
dataSourceBuilder.MapEnum<DiscountScope>("catalog.discount_scope");
dataSourceBuilder.MapEnum<DiscountServiceCategory>("catalog.discount_service_category");
dataSourceBuilder.MapEnum<DiscountApplicableChannel>("catalog.discount_applicable_channel");
```

`applies_to_service_categories` è un **array** dell'enum `DiscountServiceCategory` (regola §3.10bis
del template): `List<DiscountServiceCategory>` mappato su `catalog.discount_service_category[]`, nessun
`HasConversion` manuale necessario una volta registrato `MapEnum` (Npgsql mappa nativamente gli array di
enum PG).

### `DiscountConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
	public void Configure(EntityTypeBuilder<Discount> builder)
	{
		builder.ToTable("pricing_discounts", "catalog", t =>
		{
			t.HasComment("Voucher/codice sconto configurato da uno stabilimento.");
			t.HasCheckConstraint("ck_catalog_pricing_discounts_value", "value >= 0");
			t.HasCheckConstraint("ck_catalog_pricing_discounts_max_uses", "max_uses IS NULL OR max_uses > 0");
			t.HasCheckConstraint("ck_catalog_pricing_discounts_used_count", "max_uses IS NULL OR used_count <= max_uses");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_discounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico, copiato negli snapshot sconto di C1 (booking_discount, discountData).");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_pricing_discounts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario del voucher.");
		builder.HasOne<Property>()
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_discounts_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(60)
			.IsRequired()
			.HasComment("Codice che il cliente digita per riscattare lo sconto.");

		// Colonna generata per l'unicità case-insensitive per property (upper(code)),
		// il legacy non la garantiva.
		builder.Property<string>("CodeNormalized")
			.HasColumnName("code_normalized")
			.HasMaxLength(60)
			.HasComputedColumnSql("upper(code)", stored: true)
			.HasComment("Proiezione upper(code) per l'indice univoco case-insensitive.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.HasComment("Nome descrittivo del voucher, uso interno/gestore.");

		builder.Property(x => x.Type)
			.HasColumnName("type")
			.HasColumnType("catalog.discount_type")
			.IsRequired()
			.HasComment("Tipo di sconto: percentuale o importo fisso.");

		builder.Property(x => x.Value)
			.HasColumnName("value")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Entità dello sconto: 0-100 per percentage, euro per fixed.");

		builder.Property(x => x.Scope)
			.HasColumnName("scope")
			.HasColumnType("catalog.discount_scope")
			.HasDefaultValue(DiscountScope.All)
			.IsRequired()
			.HasComment("Ambito di applicabilità del voucher (all / custom / without_custom).");

		builder.Property(x => x.AppliesToServiceCategories)
			.HasColumnName("applies_to_service_categories")
			.HasColumnType("catalog.discount_service_category[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Categorie di servizio a cui è limitato il voucher (scope = custom); vuoto = nessuna restrizione.");

		builder.Property(x => x.ApplicableChannel)
			.HasColumnName("applicable_channel")
			.HasColumnType("catalog.discount_applicable_channel")
			.HasDefaultValue(DiscountApplicableChannel.ManagerAndOnline)
			.IsRequired()
			.HasComment("Canale/i di prenotazione su cui il voucher è valido.");

		builder.Property(x => x.MaxUses)
			.HasColumnName("max_uses")
			.HasComment("Numero massimo di utilizzi consentiti, aggregato (NULL = illimitato).");

		builder.Property(x => x.UsedCount)
			.HasColumnName("used_count")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Utilizzi già effettuati, incrementato atomicamente da C1 alla conferma prenotazione.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Voucher attivo/riscattabile.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access_users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete del voucher.");

		builder.HasIndex("PropertyId", "CodeNormalized")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_discounts_property_code");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_discounts_property_active");

		builder.HasMany(x => x.ValidityWindows).WithOne().HasForeignKey(w => w.DiscountId);
	}
}
```

`Discount` implementa `ISoftDeletable` + `IAuditable`.

### Relazioni

- `property_id` → `catalog_properties` (stesso schema): FK reale, cascade.
- `public_id` copiato negli snapshot sconto di C1: nessuna FK (cross-modulo).
- `created_by_user_id` → `identity_access_users.public_id` (C5): id copiato, nessuna FK.
- Figlie: [`catalog_pricing_discount_date_ranges`](catalog_pricing_discount_date_ranges.md) (`discount_id`,
  FK reale, cascade) e `catalog_pricing_discount_sectors` (fuori batch, stesso pattern).

### Note di migrazione

- Doppio `JSON.parse` di `discounts.items` (incapsulamento doppio legacy); ogni elemento dell'array →
  una riga.
- `name` legacy era sia nome sia codice → `code` (obbligatorio) + `name` (opzionale, default = `code` se
  assente).
- `totalNoOfUsage` (contatore nel JSON, incrementato non atomicamente da un valore fornito dal client) →
  `used_count`, incrementato server-side con `UPDATE ... SET used_count = used_count + 1 WHERE used_count
  < max_uses` (comando `RedeemDiscount` idempotente, evento di dominio verso C1 alla conferma).
- `checkBarService`/`checkNormalService`/`checkHourlyService`/`checkAdditionalServices` (4 booleani) →
  `applies_to_service_categories` (`enum[]`); `checkManagementFlag`/`checkOnlineFlag` (2 booleani) →
  `applicable_channel` (mapping: entrambi true → `manager_and_online`; solo management →
  `manager_only`; solo online → `online_only`; entrambi false → `manager_and_online`, fallback).
- `currentId` (id casuale client-side) scartato.
- Semantica di `scope = without_custom` **da chiarire col committente** (terzo valore non documentato nel
  legacy `discountCodeSearch`).
- L'endpoint di scrittura massiva senza autenticazione (`POST /api/sb/update`) **non** riprodotto.
