# Tabella `booking_discounts`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_discounts` · **forma canonica**: `booking.discounts`.
>
> Sostituisce i JSON `bookings.booking_discount` (voucher digitato), `bookings.additionalAutoVoucher[]`
> (voucher automatici su articoli) e `service_reservation.discountData`. È lo **snapshot** del voucher
> applicato alla prenotazione ([`docs/03-gestione-dati.md` §3.6](../../../progettazione/03-gestione-dati.md)).
> Fonti: [`analisi-pre/db/bookings.md`](../../../analisi-pre/db/bookings.md),
> [`analisi-pre/db/discounts.md`](../../../analisi-pre/db/discounts.md),
> [`analisi-post/db/catalog/catalog_pricing_discounts.md`](../catalog/catalog_pricing_discounts.md),
> [`docs/03-gestione-dati.md` §2.4bis, §3.6](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga per voucher/sconto applicato a una prenotazione** (raramente più d'una: uno digitato + N
automatici). Quando la prenotazione viene confermata, il voucher di C2
([`catalog_pricing_discounts`](../catalog/catalog_pricing_discounts.md)) viene **fotografato** qui
(codice, tipo, valore, canale, categorie ammesse) + l'importo effettivamente scontato: così una modifica
successiva del voucher non altera prenotazioni già chiuse, e il contatore utilizzi si incrementa
**atomicamente server-side** (nel legacy era `JSON.parse(JSON.parse(...))` + `+1` manipolabile e con
race).

Correzioni rispetto al legacy:

- **snapshot completo dell'elemento `discounts.items[]` nel JSON `booking_discount`** → colonne
  tipizzate + `discount_id` (riferimento vivo al voucher di C2).
- **`type` `"percentage"`/`"fixed"`** → `enum booking.discount_type` (condiviso con C2/C3).
- **`value` (stringa)** → `value numeric(12,2)`.
- **`voucherType` `all`/`custom`/`withoutCustom`** → `enum booking.discount_scope` (3 valori mantenuti —
  [D-16], comportamento `discountCodeSearch` da riprodurre 1:1).
- **`checkManagementFlag`/`checkOnlineFlag`** → `applied_channel enum` (snapshot).
- **`checkBar/Normal/Hourly/AdditionalServices`** → `applies_to_categories enum[]` (snapshot).
- **`totalNoOfUsage` incrementato nel JSON** → l'incremento vive su C2
  ([`catalog_pricing_discounts.used_count`](../catalog/catalog_pricing_discounts.md), atomico); qui si
  registra solo che *questa* prenotazione ha consumato un uso (`consumed_usage = true`).
- **`currentId` (`Math.random()`)** → scartato.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_discounts` | `id` | PK | — |
| `ix_..._reservation` | `(reservation_id) WHERE deleted_at IS NULL` | parziale | sconti di una prenotazione |
| `ix_..._discount` | `(discount_id) WHERE deleted_at IS NULL` | parziale | "quali prenotazioni hanno usato il voucher X" (audit del contatore) |
| `ix_..._property_code` | `(property_id, upper(code_snapshot)) WHERE deleted_at IS NULL` | parziale | ricerche per codice |

## Campi

### id
- **Legacy**: — (elemento JSON)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### reservation_id
- **Legacy**: `bookings.bookingNumber`
- **Descrizione**: prenotazione a cui si applica lo sconto.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### booking_line_id
- **Legacy**: `additionalAutoVoucher` agganciato a un articolo/postazione
- **Descrizione**: riga specifica a cui lo sconto è limitato. `NULL` = sconto sull'intera prenotazione.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`booking_lines.id`](booking_lines.md) `ON DELETE CASCADE`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1.795 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### property_id
- **Legacy**: `discounts.propertyId`
- **Descrizione**: stabilimento (denormalizzato).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dalla testata.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2.

### discount_id
- **Legacy**: match per `name` con `discounts.items[]`
- **Descrizione**: voucher di catalogo applicato (riferimento vivo per audit del contatore utilizzi).
- **Tipo**: `uuid` (= `public_id` di `catalog_pricing_discounts`).
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = sconto manuale/ad hoc non a catalogo).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_pricing_discounts`](../catalog/catalog_pricing_discounts.md).

### origin
- **Legacy**: `booking_discount` (digitato) vs `additionalAutoVoucher[]` (automatico)
- **Descrizione**: come lo sconto è entrato nella prenotazione.
- **Tipo**: `enum booking.discount_origin` — valori: `code_entered` (codice digitato dal cliente/operatore),
  `auto_applied` (regola di listino/promo).
- **Default**: `code_entered`.
- **Nullabile**: No.
- **Relazioni**: —.

### code_snapshot
- **Legacy**: `booking_discount.name` (nel legacy il "codice" **è** il nome del voucher)
- **Descrizione**: codice/nome del voucher **congelato**.
- **Tipo**: `varchar(80)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot.

### discount_type
- **Legacy**: `booking_discount.type` (`"fixed"` / `"percentage"`)
- **Descrizione**: natura del valore.
- **Tipo**: `enum booking.discount_type` — valori: `fixed`, `percentage`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: vocabolario condiviso con C2 (`catalog_pricing_discounts`) e C3.

### value
- **Legacy**: `booking_discount.value` (stringa: `"57"`, `"10"`)
- **Descrizione**: valore dello sconto **congelato** (importo € se `fixed`, percentuale se
  `percentage`).
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### scope
- **Legacy**: `booking_discount.voucherType` (`all` / `custom` / `withoutCustom`)
- **Descrizione**: ambito di validità del voucher (snapshot).
- **Tipo**: `enum booking.discount_scope` — valori: `all`, `custom`, `without_custom`.
- **Default**: `all`.
- **Nullabile**: No.
- **Relazioni**: comportamento `without_custom` = quello di `globalConstants.discountCodeSearch` (da
  riprodurre 1:1, [D-16]).

### applied_channel
- **Legacy**: `checkManagementFlag` / `checkOnlineFlag`
- **Descrizione**: canale su cui il voucher era ammesso (snapshot).
- **Tipo**: `enum booking.discount_channel` — valori: `any`, `manager_only`, `online_only`.
- **Default**: `any`.
- **Nullabile**: No.
- **Relazioni**: —.

### applies_to_categories
- **Legacy**: `checkBarService` / `checkNormalService` / `checkHourlyService` / `checkAdditionalServices`
- **Descrizione**: categorie di servizio a cui il voucher è limitato (snapshot); vuoto = nessun limite.
- **Tipo**: `enum booking.service_category[]` — valori: `bar`, `normal`, `hourly`, `additional`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: flag multipli dello stesso argomento (possono essere attivi insieme) → `enum[]`
  ([`docs/03-gestione-dati.md` §3.10](../../../progettazione/03-gestione-dati.md), regola di esclusività).

### is_roundup
- **Legacy**: `booking_discount.checkRoundup`
- **Descrizione**: arrotondamento del prezzo dopo lo sconto (snapshot della regola).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### applied_amount
- **Legacy**: derivato lato client (`voucherServices.js`) e sommato nel totale, non salvato esplicito
- **Descrizione**: importo effettivamente scontato su questa prenotazione (**valore assoluto ≥ 0**;
  concorre a `booking_reservations.discount` come negativo).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: calcolato dal motore prezzo server-side.

### consumed_usage
- **Legacy**: `discounts.items[].totalNoOfUsage += 1` (nel JSON, manipolabile, con race)
- **Descrizione**: `true` se questa prenotazione ha **consumato un utilizzo** del voucher (per audit /
  storno del contatore in caso di annullamento).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: l'incremento/decremento del contatore è su C2
  ([`catalog_pricing_discounts.used_count`](../catalog/catalog_pricing_discounts.md)), atomico
  server-side; qui si tiene traccia per riconciliazione.

### created_at / updated_at / deleted_at
- **Legacy**: — (elemento JSON)
- **Descrizione**: creazione / modifica / soft-delete (rimozione dello sconto dalla prenotazione).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. L'elemento `discounts.items[]` copiato per intero nel JSON `booking_discount` (con doppia
serializzazione lato C2) diventa colonne tipizzate. Campi legacy non migrati: `currentId`
(`Math.random()`), `maximunNoUse` / `voucherDateRange` / `status` (sono **regole del voucher**, vivono su
C2 `catalog_pricing_discounts` e figlie — non si fotografano nella prenotazione se non per il valore già
applicato).

## Suggerimenti per le prestazioni

- Tabella piccola (0–2 righe per prenotazione). Gli indici servono soprattutto l'audit del contatore
  utilizzi (`discount_id`) e la ricerca prenotazioni per codice.
- L'incremento atomico del contatore vive su C2: qui nessuna scrittura calda.

## Implementazione .NET

`BookingDiscount` è **entità figlia** dell'aggregato `Reservation` (modulo `Module.Booking`, schema
`booking`): snapshot del voucher applicato alla prenotazione (o a una sua riga), congelato al momento
dell'applicazione così che una modifica successiva del voucher in C2 non alteri prenotazioni già
chiuse. Sempre creata/modificata tramite l'aggregate root (`Reservation.ApplyDiscount(...)`,
`Reservation.RemoveDiscount(...)`), mai persistita in isolamento; alla rimozione/annullamento solleva
l'evento di dominio `DiscountUsageReleased` per decrementare `catalog_pricing_discounts.used_count`
in C2.

`ServiceCategory` è **già definito** in
[`booking_service_lines.md`](booking_service_lines.md#enum-propri) (stesso schema `booking`, valori
`Bar`/`Normal`/`Hourly`/`Additional`) — non ridefinito qui.

### Enum propri

```csharp
// Module.Booking.Domain.Enums

/// Come lo sconto è entrato nella prenotazione.
public enum DiscountOrigin
{
	CodeEntered = 0,
	AutoApplied = 1
}

/// Natura del valore dello sconto — vocabolario condiviso con C2 (catalog_pricing_discounts) e C3, ma
/// ogni schema Postgres registra la propria copia dell'ENUM (i tipi ENUM non sono condivisibili cross-schema).
public enum DiscountType
{
	Fixed = 0,
	Percentage = 1
}

/// Ambito di validità del voucher, congelato dalla regola di C2 al momento dell'applicazione.
public enum DiscountScope
{
	All = 0,
	Custom = 1,
	WithoutCustom = 2
}

/// Canale su cui il voucher era ammesso, congelato dalla regola di C2.
public enum DiscountChannel
{
	Any = 0,
	ManagerOnly = 1,
	OnlineOnly = 2
}
```

```csharp
// Module.Booking.Infrastructure.DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<DiscountOrigin>("booking", "discount_origin");
modelBuilder.HasPostgresEnum<DiscountType>("booking", "discount_type");
modelBuilder.HasPostgresEnum<DiscountScope>("booking", "discount_scope");
modelBuilder.HasPostgresEnum<DiscountChannel>("booking", "discount_channel");
// ServiceCategory già registrato da booking_service_lines.md — non ripetere la HasPostgresEnum qui.
```

```csharp
// Module.Booking Infrastructure DI — registrazione data source Npgsql
dataSourceBuilder.MapEnum<DiscountOrigin>("booking.discount_origin");
dataSourceBuilder.MapEnum<DiscountType>("booking.discount_type");
dataSourceBuilder.MapEnum<DiscountScope>("booking.discount_scope");
dataSourceBuilder.MapEnum<DiscountChannel>("booking.discount_channel");
```

### `BookingDiscountConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingDiscountConfiguration : IEntityTypeConfiguration<BookingDiscount>
{
	public void Configure(EntityTypeBuilder<BookingDiscount> builder)
	{
		builder.ToTable("booking_discounts", "booking", t => t.HasComment(
			"Snapshot di un voucher/sconto applicato a una prenotazione o a una sua riga."));

		builder.HasKey(x => x.Id).HasName("pk_booking_discounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui si applica lo sconto.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga specifica a cui lo sconto è limitato; NULL = sconto sull'intera prenotazione.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.DiscountId)
			.HasColumnName("discount_id")
			.HasComment("public_id di catalog.pricing_discounts, copiato — riferimento vivo per l'audit del contatore utilizzi; NULL per sconto manuale non a catalogo.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("booking.discount_origin")
			.IsRequired()
			.HasDefaultValueSql("'code_entered'")
			.HasComment("Come lo sconto è entrato nella prenotazione: codice digitato o regola automatica.");

		builder.Property(x => x.CodeSnapshot)
			.HasColumnName("code_snapshot")
			.HasMaxLength(80)
			.IsRequired()
			.HasComment("Codice/nome del voucher congelato al momento dell'applicazione.");

		builder.Property(x => x.DiscountType)
			.HasColumnName("discount_type")
			.HasColumnType("booking.discount_type")
			.IsRequired()
			.HasComment("Natura del valore: importo fisso o percentuale.");

		builder.Property(x => x.Value)
			.HasColumnName("value")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Valore dello sconto congelato (importo € se fixed, percentuale se percentage).");

		builder.Property(x => x.Scope)
			.HasColumnName("scope")
			.HasColumnType("booking.discount_scope")
			.IsRequired()
			.HasDefaultValueSql("'all'")
			.HasComment("Ambito di validità del voucher, congelato dalla regola di C2.");

		builder.Property(x => x.AppliedChannel)
			.HasColumnName("applied_channel")
			.HasColumnType("booking.discount_channel")
			.IsRequired()
			.HasDefaultValueSql("'any'")
			.HasComment("Canale su cui il voucher era ammesso, congelato dalla regola di C2.");

		builder.Property(x => x.AppliesToCategories)
			.HasColumnName("applies_to_categories")
			.HasColumnType("booking.service_category[]")
			.IsRequired()
			.HasDefaultValueSql("'{}'")
			.HasComment("Categorie di servizio a cui il voucher è limitato, congelate; vuoto = nessun limite.");

		builder.Property(x => x.IsRoundup)
			.HasColumnName("is_roundup")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Arrotondamento del prezzo dopo lo sconto, congelato dalla regola.");

		builder.Property(x => x.AppliedAmount)
			.HasColumnName("applied_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Importo effettivamente scontato su questa prenotazione, calcolato dal motore prezzo server-side.");

		builder.Property(x => x.ConsumedUsage)
			.HasColumnName("consumed_usage")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True se questa prenotazione ha consumato un utilizzo del voucher; l'incremento/decremento del contatore è atomico su C2.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete (rimozione dello sconto dalla prenotazione).");

		builder.HasIndex(x => x.ReservationId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_discounts_reservation");

		builder.HasIndex(x => x.DiscountId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_discounts_discount");

		builder.HasIndex(x => new { x.PropertyId, x.CodeSnapshot })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_discounts_property_code");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.Discounts)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_discounts_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany()
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_discounts_line")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`BookingDiscount` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable`
(`created_at`/`updated_at`). `property_id` e `discount_id` sono `public_id` copiati da C2 (solo
`HasComment`, nessuna FK cross-schema); le FK EF reali sono `reservation_id` → `booking_reservations`
e `booking_line_id` (nullable) → `booking_lines`, entrambe `Cascade` (stesso schema `booking`). L'indice
`ix_booking_discounts_property_code` usa `upper(code_snapshot)` nella scheda Indici: mapparlo con
`HasIndex(...).HasMethod("btree")` + `HasExpression`, o più semplicemente con
`HasIndex(x => new { x.PropertyId, x.CodeSnapshot }).HasFilter(...)` e una migration manuale che
aggiunge l'espressione `upper(...)` se il case-insensitive è richiesto anche a indice (EF Core non
genera indici su espressioni arbitrarie senza `.HasAnnotation`/migration custom).

### Note di migrazione

- `booking_discount` (voucher digitato) + ogni elemento di `additionalAutoVoucher[]` (voucher
  automatici) → **1 riga ciascuno** in questa tabella, distinti da `origin`.
- `service_reservation.discountData` confluisce con lo stesso schema (colonna `origin` e `scope`
  determinati dal contenuto).
- Match per `discounts.items[].name` (stringa) → risolto contro `catalog_pricing_discounts` per
  popolare `discount_id`; se non risolvibile, `discount_id = NULL` (sconto manuale) ma
  `code_snapshot`/`value`/`discount_type` restano valorizzati dal JSON.
- `voucherType` `withoutCustom` → mantenuto **1:1** come `without_custom` (comportamento
  `discountCodeSearch` da riprodurre esattamente, [D-16], non da reinterpretare).
- `currentId` (`Math.random()`) → **scartato**, nessun equivalente.
- `totalNoOfUsage += 1` nel JSON (manipolabile, con race) → sostituito dall'incremento atomico
  server-side su `catalog_pricing_discounts.used_count`; questa riga registra solo `consumed_usage`
  per riconciliazione/audit.
