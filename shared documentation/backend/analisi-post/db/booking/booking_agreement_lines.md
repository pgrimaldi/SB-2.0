# Tabella `booking_agreement_lines`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo, convenzioni) · **schema**: `booking` · **nome
> flat**: `booking_agreement_lines` · **forma canonica**: `booking.agreement_lines`.
>
> **Fetta C1** di [`hotel_payments`](../../../analisi-pre/db/hotel_payments.md): la riga "questa
> postazione ha consumato *questa* voce di listino/voucher di una convenzione" + lo **snapshot** del
> prezzo — parte legittima dell'aggregato Prenotazione ([`docs/03-gestione-dati.md` §3.6 punto 4,
> §4](../../../progettazione/03-gestione-dati.md)). L'**importo / stato pagamento / metodo** (`amount`,
> `paymentStatus`, `paymentMethod`) vanno in **C3** e **non** sono in questa tabella. Nomenclatura
> allineata a C2 (`catalog_agreement_*`, ex `hotels`/`hotel_price_lists`). Fonti:
> [`analisi-pre/db/hotel_payments.md`](../../../analisi-pre/db/hotel_payments.md),
> [`analisi-post/db/catalog/catalog_agreement_price_lists.md`](../catalog/catalog_agreement_price_lists.md),
> [`analisi-pre/business-logic/stabilimento/06-convenzioni.md`](../../../analisi-pre/business-logic/stabilimento/06-convenzioni.md).

---

## Descrizione

**Una riga per (riga prenotazione × voce di listino/voucher di una convenzione)**, quando
`booking_reservations.agreement_partner_id` è valorizzato. Nel legacy `processHotelBookingPayments`
faceva `deletePaymentsByBookingId` + ricrea ad ogni `updateBooking` (in produzione **71% delle righe è
`isDeleted = 1`**); qui si tengono solo le righe vive e la modifica è un `UPDATE`/soft-delete mirato.

Correzioni rispetto al legacy:

- **`hotel_payments` "di confine"** → questa tabella tiene solo la parte C1 (voce + snapshot prezzo);
  `amount`/`assignedPrice`/`paymentStatus`/`paymentMethod` → **C3**.
- **`hotel*` → `agreement*`**: allineamento alla nomenclatura C2 (`hotels`→`catalog_agreement_partners`,
  `hotel_price_lists`→`catalog_agreement_price_lists`, [D-20]).
- **`mainPrices` / `detailPrices` doppiamente serializzati** (`"\"[]\""`), blob da decine di KB →
  `applied_price` (valore risolto) + FK al listino di catalogo.
- **`priceCalculation` snapshot come stringa libera** (7 varianti) → `enum` allineato a
  `catalog_pricing_calculation_methods` (lookup condivisa).
- **`locationName` (stringa) match con `bookings` per nome postazione** → `booking_line_id` FK.
- **`room` / `voucherNumber` / `date` / `notes` `NULL` su tutte le righe di produzione** → non migrate
  (`date` serviva `checkVoucherLimit`, che di fatto non contava nulla); il limite voucher per stagione
  si ricalcola su `booking_agreement_lines` × `booking_lines.start_date`.
- **`usedAsDiscount` (`tinyint`)** → `is_used_as_discount boolean`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_agreement_lines` | `id` | PK | — |
| `uq_booking_agreement_lines_public_id` | `(public_id)` | UNIQUE | referenziato da C3 (importo/stato pagamento della voce) |
| `ix_..._reservation` | `(reservation_id) WHERE deleted_at IS NULL` | parziale | voci convenzione di una prenotazione |
| `ix_..._line` | `(booking_line_id) WHERE deleted_at IS NULL` | parziale | voci convenzione di una postazione |
| `ix_..._partner` | `(agreement_partner_id) WHERE deleted_at IS NULL` | parziale | consumi per struttura partner (report convenzioni C2) |
| `ix_..._voucher_limit` | `(property_id, agreement_price_list_id, customer_id) WHERE list_type = 'voucher' AND deleted_at IS NULL` | parziale | conteggio utilizzi voucher per cliente/stagione (ex `checkVoucherLimit`) |

## Campi

### id
- **Legacy**: `hotel_payments.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico, **copiato cross-modulo** verso C3 (che tiene importo/stato pagamento
  della voce).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C3.

### reservation_id
- **Legacy**: `hotel_payments.bookingNumber`
- **Descrizione**: prenotazione a cui la voce appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### booking_line_id
- **Legacy**: `hotel_payments.locationName` / `locationKey` (match per stringa con `bookings`)
- **Descrizione**: riga postazione che ha consumato la voce di convenzione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (voce a livello prenotazione).
- **Relazioni**: **FK** → [`booking_lines.id`](booking_lines.md) `ON DELETE CASCADE`.

### property_id
- **Legacy**: derivato
- **Descrizione**: stabilimento (denormalizzato).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dalla testata.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2.

### agreement_partner_id
- **Legacy**: `hotel_payments.hotelId` (`int`, `NOT NULL`)
- **Descrizione**: struttura partner ("convenzione").
- **Tipo**: `uuid` (= `public_id` di `catalog_agreement_partners`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_agreement_partners`](../catalog/catalog_agreement_partners.md).

### agreement_price_list_id
- **Legacy**: `hotel_payments.hotelPriceListId` (`int`)
- **Descrizione**: voce di listino / voucher di convenzione usata.
- **Tipo**: `uuid` (= `public_id` di `catalog_agreement_price_lists`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_agreement_price_lists`](../catalog/catalog_agreement_price_lists.md).

### customer_id
- **Legacy**: derivato dalla prenotazione (per `checkVoucherLimit`)
- **Descrizione**: cliente intestatario (copiato per il conteggio utilizzi voucher per cliente/stagione
  senza join alla testata).
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: dalla testata.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` copiato** da C5.

### list_type
- **Legacy**: distinzione listino vs voucher su `hotel_price_lists.list_type`
- **Descrizione**: natura della voce.
- **Tipo**: `enum booking.agreement_line_type` — valori: `regular` (voce di listino), `voucher`.
- **Default**: `regular`.
- **Nullabile**: No.
- **Relazioni**: `voucher` ⇒ concorre al limite `max_voucher_quantity` del listino (C2).

### quantity
- **Legacy**: `hotel_payments.quantity` (`int`)
- **Descrizione**: quantità di voci di listino.
- **Tipo**: `smallint`.
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (quantity > 0)`.

### price_calculation
- **Legacy**: `hotel_payments.priceCalculation` (`varchar`; prod: `"proportional"` 3.560 / `"daily"` 5)
- **Descrizione**: metodo di calcolo prezzo del listino di convenzione **congelato** al momento della
  prenotazione.
- **Tipo**: `smallint` (FK a lookup) — **stessa lookup** di
  [`catalog_pricing_calculation_methods`](../catalog/catalog_pricing_price_lists.md#lookup-catalog_pricing_calculation_methods)
  (valore copiato per id, non FK cross-schema).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: vocabolario condiviso con `price_lists`/`hotel_price_lists` (7 varianti da consolidare,
  [`docs/03-gestione-dati.md` §2.4bis](../../../progettazione/03-gestione-dati.md)).

### applied_price
- **Legacy**: `hotel_payments.mainPrices` / `detailPrices` (JSON doppiamente serializzati, decine di KB)
- **Descrizione**: prezzo **bloccato** della voce per la riga (prezzo per settore/periodo già risolto dal
  motore prezzo, non l'intero listino). Il listino completo resta su C2
  ([`catalog_agreement_price_lists`](../catalog/catalog_agreement_price_lists.md)) — qui solo il valore
  applicato (dato del documento-prenotazione, §3.6 punto 4).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### is_used_as_discount
- **Legacy**: `hotel_payments.usedAsDiscount` (`tinyint`; prod ~10%)
- **Descrizione**: la voce è applicata come **sconto** sulla prenotazione (non come addebito).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: se `true`, concorre a `booking_reservations.discount` invece che al subtotale.

### created_at / updated_at / deleted_at
- **Legacy**: `hotel_payments.createdAt` / `updatedAt` / `isDeleted`
- **Descrizione**: creazione / modifica / soft-delete (voce rimossa in modifica prenotazione).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF; il pattern legacy `destroy`+`recreate` (71% righe `isDeleted`)
  diventa `UPDATE`/soft-delete mirato.

## Valutazione JSON

Nessun JSON. `mainPrices` / `detailPrices` (doppiamente serializzati) → `applied_price` (valore
applicato) + FK `agreement_price_list_id` verso il listino completo di C2. Colonne legacy `NULL` su
tutte le righe di produzione (`room`, `voucherNumber`, `date`, `notes`, `paymentMethod`) → non migrate.

## Suggerimenti per le prestazioni

- Solo righe vive (il legacy teneva 3× le righe per il destroy+recreate).
- L'indice `voucher_limit` sostituisce `checkVoucherLimit` (che nel legacy contava per `hp.date`, quasi
  sempre `NULL` → non contava nulla): il conteggio per stagione si fa su
  `booking_agreement_lines` × `booking_lines.start_date` contro
  [`catalog_pricing_seasons`](../catalog/catalog_pricing_seasons.md).
- `applied_price` scalare invece di un blob listino da decine di KB per riga.

## Implementazione .NET

`BookingAgreementLine` è **entità figlia** dell'aggregato `Reservation` (modulo `Module.Booking`,
schema `booking`): consumo di una voce di listino/voucher di una convenzione (partner) da parte di una
riga prenotazione (o della prenotazione nel complesso), con lo snapshot del prezzo risolto. Sempre
creata/modificata tramite l'aggregate root (`Reservation.AddAgreementLine(...)`), mai persistita in
isolamento; solo righe vive (il pattern legacy "destroy+recreate a ogni update", che in produzione
lasciava il 71% delle righe `isDeleted = 1`, diventa qui `UPDATE`/soft-delete mirato). Alla creazione
solleva un evento di dominio verso C3 (`AgreementLineRegistered`, con `public_id`) per generare il
movimento di incasso della voce.

Nessun enum condiviso da altre schede di questo batch; `AgreementLineType` è proprio di questa tabella.

### Enum propri

```csharp
// Module.Booking.Domain.Enums

/// Natura della voce di convenzione consumata.
public enum AgreementLineType
{
	Regular = 0,
	Voucher = 1
}
```

```csharp
// Module.Booking.Infrastructure.DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<AgreementLineType>("booking", "agreement_line_type");
```

```csharp
// Module.Booking Infrastructure DI — registrazione data source Npgsql
dataSourceBuilder.MapEnum<AgreementLineType>("booking.agreement_line_type");
```

### `BookingAgreementLineConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingAgreementLineConfiguration : IEntityTypeConfiguration<BookingAgreementLine>
{
	public void Configure(EntityTypeBuilder<BookingAgreementLine> builder)
	{
		builder.ToTable("booking_agreement_lines", "booking", t =>
		{
			t.HasComment("Voce di listino/voucher di una convenzione consumata da una riga prenotazione, con snapshot del prezzo applicato.");
			t.HasCheckConstraint("ck_booking_agreement_lines_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_agreement_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico, copiato cross-modulo verso C3 che tiene importo/stato pagamento della voce.");

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui la voce appartiene.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga postazione che ha consumato la voce di convenzione; NULL = voce a livello prenotazione.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.AgreementPartnerId)
			.HasColumnName("agreement_partner_id")
			.IsRequired()
			.HasComment("public_id di catalog.agreement_partners, copiato — struttura partner della convenzione.");

		builder.Property(x => x.AgreementPriceListId)
			.HasColumnName("agreement_price_list_id")
			.HasComment("public_id di catalog.agreement_price_lists, copiato — voce di listino/voucher di convenzione usata.");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.HasComment("public_id di identity.customer_customers, copiato dalla testata — per il conteggio utilizzi voucher per cliente/stagione senza join.");

		builder.Property(x => x.ListType)
			.HasColumnName("list_type")
			.HasColumnType("booking.agreement_line_type")
			.IsRequired()
			.HasDefaultValueSql("'regular'")
			.HasComment("Natura della voce: voce di listino ordinaria o voucher.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Quantità di voci di listino consumate.");

		builder.Property(x => x.PriceCalculation)
			.HasColumnName("price_calculation")
			.HasColumnType("smallint")
			.HasComment("Id copiato della lookup catalog.pricing_calculation_methods (metodo di calcolo prezzo congelato) — nessuna FK cross-schema.");

		builder.Property(x => x.AppliedPrice)
			.HasColumnName("applied_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Prezzo bloccato della voce per la riga, già risolto dal motore prezzo (non l'intero listino).");

		builder.Property(x => x.IsUsedAsDiscount)
			.HasColumnName("is_used_as_discount")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("La voce è applicata come sconto sulla prenotazione invece che come addebito.");

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
			.HasComment("Soft-delete (voce rimossa in modifica prenotazione).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_booking_agreement_lines_public_id");

		builder.HasIndex(x => x.ReservationId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_agreement_lines_reservation");

		builder.HasIndex(x => x.BookingLineId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_agreement_lines_line");

		builder.HasIndex(x => x.AgreementPartnerId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_agreement_lines_partner");

		builder.HasIndex(x => new { x.PropertyId, x.AgreementPriceListId, x.CustomerId })
			.HasFilter("list_type = 'voucher' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_agreement_lines_voucher_limit");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.AgreementLines)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_agreement_lines_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany()
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_agreement_lines_line")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`BookingAgreementLine` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable`
(`created_at`/`updated_at`). `property_id`, `agreement_partner_id`, `agreement_price_list_id`,
`customer_id` è un `public_id` copiato da altri moduli (C2/C5/C4, solo
`HasComment`, nessuna FK cross-schema — `price_calculation` in particolare è un `smallint` copiato per
id dalla lookup condivisa `catalog_pricing_calculation_methods`, non un `uuid`, coerente con quanto
indicato nella scheda); le FK EF reali sono `reservation_id` → `booking_reservations` e
`booking_line_id` (nullable) → `booking_lines`, entrambe `Cascade` (stesso schema `booking`).

### Note di migrazione

- Migrare **solo le righe vive** (`hotel_payments.isDeleted = 0`): il pattern legacy
  `deletePaymentsByBookingId` + ricrea a ogni `updateBooking` lasciava in produzione il 71% delle
  righe con `isDeleted = 1` — non hanno valore storico da preservare.
- `mainPrices`/`detailPrices` (JSON **doppiamente serializzati**, `"\"[]\""`, decine di KB) → parse a
  doppio `JSON.parse` per estrarre il valore risolto in `applied_price`; il blob non si persiste.
- `hotelId`/`hotelPriceListId` (`int`) → `agreement_partner_id`/`agreement_price_list_id` (`uuid`,
  `public_id` di C2).
- `locationName`/`locationKey` (match per stringa con `bookings`) → `booking_line_id` risolto per FK.
- `priceCalculation` (`varchar`, 7 varianti da consolidare) → id copiato verso la lookup
  `catalog_pricing_calculation_methods`.
- `room`, `voucherNumber`, `date`, `notes`, `paymentMethod` — **tutti `NULL` su ogni riga di
  produzione** (verificato) → **non migrati**; in particolare `date` alimentava `checkVoucherLimit`,
  che di fatto non contava mai nulla: il nuovo conteggio si fa server-side su
  `booking_agreement_lines` (`list_type = voucher`) incrociato con `booking_lines.start_date` contro
  `catalog_pricing_seasons`.
