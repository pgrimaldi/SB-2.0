# Tabella `booking_service_lines`

> **Dominio**: C1 — `booking` · **sottoarea**: `service` · **schema**: `booking` · **nome flat**:
> `booking_service_lines` · **forma canonica**: `booking.service_lines`.
>
> Sostituisce i JSON `bookings.servicesData` / `service_reservation.servicesData` (bar / normali /
> orari) e la **parte "riga di vendita"** di
> [`services_booking_counts`](../../../analisi-pre/db/services_booking_counts.md). La **parte
> conteggio per data** va in [`booking_resource_consumptions`](booking_resource_consumptions.md). Fonti:
> [`analisi-pre/db/service_reservation.md`](../../../analisi-pre/db/service_reservation.md),
> [`analisi-pre/db/services_booking_counts.md`](../../../analisi-pre/db/services_booking_counts.md),
> [`analisi-pre/db/hourly_service_booking_management.md`](../../../analisi-pre/db/hourly_service_booking_management.md),
> [`analisi-pre/business-logic/stabilimento/03-cassa.md`](../../../analisi-pre/business-logic/stabilimento/03-cassa.md),
> [`docs/03-gestione-dati.md` §2.4bis, §3.6](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga per servizio venduto su una prenotazione** — sia acquistato contestualmente a una postazione
(`bookings.servicesData`) sia come prenotazione di soli servizi (`service_reservation.servicesData`).
`kind` distingue `bar` / `normal` / `hourly`. È la **riga d'ordine** (nome, prezzo unitario, quantità,
variante, IVA, totale, stato evasione, riga fiscale); il **consumo per data** e l'occupazione degli slot
sono righe di [`booking_resource_consumptions`](booking_resource_consumptions.md).

Correzioni rispetto al legacy:

- **`servicesData` blob** con immagini `data:` base64 inline (fino a >200 KB per riga) e array di
  costanti UI (`day_constant`, `timeslot_constants`, `bar_category_constants`) → solo `sale_item_id` +
  quantità + prezzo + variante.
- **chiavi diverse** (`barServices`/`houlryServices` in `bookings` vs `barServiceData`/`hourlyServiceData`
  in `service_reservation`) → un solo schema.
- **`ServiceName` / `service_name` stringa libera** → `sale_item_id` FK a
  [`catalog_service_sale_items`](../catalog/catalog_service_sale_items.md) + `name_snapshot`.
- **`ServicePrice` / `percentIVA` stringa** → `unit_price` / `vat_rate` `numeric`.
- **`service_type` `"Bar Service"` / `"Normal Service"` (drift vs `bar`/`normal`)** → `enum` allineato a
  `catalog.service_sale_item.kind`.
- **`booking_type` (`services_booking_counts`) = `bookingByAdmin` travestito** → `booking_channel`
  condiviso.
- **contatore `services_booking_counts` con 98% righe a `quantity = 0`** → qui solo righe reali
  (`quantity > 0`); il conteggio per data è in `booking_resource_consumptions`.
- **`summaryServicesData` (servizi pagati col borsellino)** → `is_paid_with_wallet` (il **movimento**
  wallet è C3).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_service_lines` | `id` | PK | — |
| `uq_booking_service_lines_public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C4 line item, C3 riga) |
| `ix_..._reservation` | `(reservation_id) WHERE deleted_at IS NULL` | parziale | servizi di una prenotazione |
| `ix_..._property_kind` | `(property_id, kind) WHERE deleted_at IS NULL` | parziale | statistiche di vendita per categoria |
| `ix_..._sale_item` | `(sale_item_id) WHERE deleted_at IS NULL` | parziale | "quante volte è stato venduto il servizio X" |
| `ix_..._order_open` | `(property_id) WHERE is_order_filled = false AND deleted_at IS NULL` | parziale | ordini bar/cucina da evadere |


> ### ✅ Applicato nel backend (2026-09-21)
>
> La colonna **`line_total`** è ora `GENERATED ALWAYS AS ((unit_price + variant_surcharge) * quantity) STORED` ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §5.1),
> migration `AddDbOptimizationsPropertyIdAndGeneratedColumns`.
>
> **È un cambio di contratto, non solo di schema**: PostgreSQL *rifiuta* un `INSERT`/`UPDATE` che
> valorizzi esplicitamente la colonna. Il codice applicativo che oggi la scrive va rimosso, non
> adattato. Il `DEFAULT 0` è sparito: una colonna `GENERATED ALWAYS` non ammette default, il valore è sempre la formula.

## Campi

### id
- **Legacy**: elemento di `servicesData` / riga `services_booking_counts`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`booking_service_time_slots`](booking_service_time_slots.md).

### public_id
- **Legacy**: `servicesData[].lineItemId` (uuid nel JSON, non stabile)
- **Descrizione**: id pubblico della riga servizio, **copiato cross-modulo** (C4 line item, C3 riga
  economica).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C3/C4.

### reservation_id
- **Legacy**: `bookings.bookingNumber` / `service_reservation.bookingNumber`
- **Descrizione**: prenotazione a cui il servizio appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### booking_line_id
- **Legacy**: — (nel legacy il servizio non era legato a una postazione specifica)
- **Descrizione**: riga postazione a cui il servizio è agganciato (es. "un lettino extra su A13"). `NULL`
  = servizio "sulla prenotazione" (es. "un caffè").
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`booking_lines.id`](booking_lines.md) `ON DELETE SET NULL`.

### property_id
- **Legacy**: `services_booking_counts.property_id` (`varchar`)
- **Descrizione**: stabilimento (denormalizzato).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dalla testata.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2.

### kind
- **Legacy**: `service_type` (`"Bar Service"` / `"Normal Service"`; gli orari erano altrove)
- **Descrizione**: categoria del servizio venduto.
- **Tipo**: `enum booking.service_category` — valori: `bar`, `normal`, `hourly`, `additional`.
  `additional` esiste solo per essere citato come categoria in `booking_discounts.applies_to_categories`:
  `kind` di una riga servizio non lo assume mai (gli articoli aggiuntivi sono
  [`booking_line_additional_items`](booking_line_additional_items.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: allineato a `catalog.service_sale_item_kind` /
  [`catalog_service_type_icons.type`](../catalog/catalog_service_type_icons.md); drift `"Bar Service"`→`bar`
  in migrazione ([`docs/03-gestione-dati.md` §2.4bis](../../../progettazione/03-gestione-dati.md)).

### sale_item_id
- **Legacy**: `ServiceName` (stringa)
- **Descrizione**: servizio di catalogo venduto.
- **Tipo**: `uuid` (= `public_id` di `catalog_service_sale_items`).
- **Default**: nessuno.
- **Nullabile**: Sì (servizio ad hoc non a catalogo).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_service_sale_items`](../catalog/catalog_service_sale_items.md).

### name_snapshot
- **Legacy**: `ServiceName` / `service_name` / `hourly_service_name`
- **Descrizione**: nome del servizio **congelato** (ricevute/dettaglio).
- **Tipo**: `varchar(160)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot.

### variant_label
- **Legacy**: `servicesData[].selectedVariant` / `VariantList` (`{variantnames, variantvalues}`)
- **Descrizione**: variante scelta del prodotto (es. "Macchiato"). `NULL` = nessuna variante.
- **Tipo**: `varchar(80)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### variant_surcharge
- **Legacy**: `selectedVariant[].variantvalues` (`"0.30"`)
- **Descrizione**: sovrapprezzo della variante (unitario).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### unit_price
- **Legacy**: `ServicePrice` (stringa `"1.20"`)
- **Descrizione**: prezzo unitario **bloccato** (senza variante).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### quantity
- **Legacy**: `servicesData[].count` / `selectedData[].click` sommati; `hourly_service_booking_management.booked_count`
- **Descrizione**: unità vendute (totale della riga). Il dettaglio per giorno è in
  `booking_resource_consumptions`.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (quantity > 0)`.

### vat_rate
- **Legacy**: `percentIVA` (stringa `"22"` / `"10"`)
- **Descrizione**: aliquota IVA % **congelata** (per lo scontrino).
- **Tipo**: `numeric(5,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: snapshot fiscale.

### line_total
- **Legacy**: derivato lato client (`Σ prezzo×qtà + varianti`)
- **Descrizione**: totale bloccato della riga = `(unit_price + variant_surcharge) × quantity`.
- **Tipo**: `numeric(12,2)`, `GENERATED ALWAYS AS ((unit_price + variant_surcharge) * quantity) STORED`
  (riquadro in «Indici»).
- **Default**: — (colonna generata; il `DEFAULT 0` progettato non è ammesso).
- **Nullabile**: No.
- **Relazioni**: concorre a `booking_reservations.subtotal`.

### service_date
- **Legacy**: `service_reservation.startDate` / `selectedData[].date` (per il caso mono-data)
- **Descrizione**: data di riferimento del servizio quando è una sola (comodità di lettura); per servizi
  multi-data le date effettive sono in `booking_resource_consumptions`.
- **Tipo**: `date`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### booking_channel
- **Legacy**: `services_booking_counts.booking_type` (`"0"`/`"1"`)
- **Descrizione**: canale della prenotazione di origine.
- **Tipo**: `enum booking.booking_channel` — valori: `customer`, `manager`.
- **Default**: dalla testata.
- **Nullabile**: No.
- **Relazioni**: vocabolario condiviso.

### is_order_filled
- **Legacy**: `bookings.isOrderFilled` / `service_reservation.isOrderFilled` (a livello prenotazione)
- **Descrizione**: la riga è stata **evasa** (bar/cucina, endpoint `update-order-filled`). Qui è per
  riga; il riepilogo è `booking_reservations.is_order_filled`.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### is_paid_with_wallet
- **Legacy**: presenza in `bookings.summaryServicesData` (`barCardServices` / `normalCardServices`)
- **Descrizione**: la riga è stata pagata col **borsellino/tessera prepagata** del cliente. Il
  **movimento** wallet è C3 (`payment.wallet`).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` / destroy fisico
- **Descrizione**: creazione / modifica / soft-delete (voce servizio rimossa —
  `deleteSingleServiceReservationList`).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. `servicesData` (con immagini base64 e costanti UI) → `sale_item_id` + scalari. Le
**costanti UI** (`day_constant`, `timeslot_constants`, `bar_category_constants`, `maximumReservations`,
`highlightServices`, …) vivono in C2 (anagrafica servizi), non si copiano nella prenotazione.

## Suggerimenti per le prestazioni

- Righe reali soltanto (`quantity > 0`): niente rumore da 98% di righe a zero come in
  `services_booking_counts`.
- La disponibilità dello slot orario (ex `checkHourlyServiceAvailability`) si calcola su
  `booking_resource_availability_daily` / `booking_resource_consumptions`, non qui.
- Indice parziale `WHERE is_order_filled = false` per la coda ordini di cucina/bar.

## Implementazione .NET

`BookingServiceLine` è **entità figlia** dell'aggregato `Reservation` (modulo `Module.Booking`, schema
`booking`, sottoarea `service`): riga d'ordine per un servizio venduto (bar/normale/orario), agganciata
o a una [`booking_lines`](booking_lines.md) (`booking_line_id` valorizzato) o direttamente alla
prenotazione (`booking_line_id = NULL`). Espone la collezione figlia `TimeSlots`
([`booking_service_time_slots`](booking_service_time_slots.md), solo per `kind = Hourly`). Sempre
creata/modificata tramite l'aggregate root (`Reservation.AddServiceLine(...)`), mai persistita in
isolamento; alla conferma genera righe di consumo su `booking_resource_consumptions` (fuori da questo
batch).

`BookingChannel` è **già definito** in [`booking_reservations.md`](booking_reservations.md#enum-propri)
(stesso schema `booking`) — non ridefinito qui, si riusa lo stesso tipo C# e lo stesso `ENUM` PG
`booking.booking_channel`.

### Enum propri

```csharp
// Module.Booking.Domain.Enums

/// Categoria del servizio venduto — allineata (ma non FK) a catalog.service_sale_item_kind, schema diverso.
/// Additional esiste solo per essere referenziato come categoria in booking_discounts.applies_to_categories
/// (vedi booking_discounts.md): BookingServiceLine.Kind non assume mai questo valore (gli articoli
/// aggiuntivi sono booking_line_additional_items, non una booking_service_lines).
public enum ServiceCategory
{
	Bar = 0,
	Normal = 1,
	Hourly = 2,
	Additional = 3
}
```

```csharp
// Module.Booking.Infrastructure.DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ServiceCategory>("booking", "service_category");
// BookingChannel già registrato da booking_reservations.md — non ripetere la HasPostgresEnum qui.
```

```csharp
// Module.Booking Infrastructure DI — registrazione data source Npgsql
dataSourceBuilder.MapEnum<ServiceCategory>("booking.service_category");
```

### `BookingServiceLineConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingServiceLineConfiguration : IEntityTypeConfiguration<BookingServiceLine>
{
	public void Configure(EntityTypeBuilder<BookingServiceLine> builder)
	{
		builder.ToTable("booking_service_lines", "booking", t =>
		{
			t.HasComment("Riga d'ordine per un servizio (bar/normale/orario) venduto su una prenotazione, agganciata o meno a una postazione.");
			t.HasCheckConstraint("ck_booking_service_lines_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_service_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico della riga servizio, copiato cross-modulo da C4 (line item) e C3 (riga economica).");

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui il servizio appartiene.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga postazione a cui il servizio è agganciato; NULL = servizio sulla prenotazione nel complesso.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("booking.service_category")
			.IsRequired()
			.HasComment("Categoria del servizio venduto: bar, normale o orario.");

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.HasComment("public_id di catalog.service_sale_items, copiato — NULL per servizio ad hoc non a catalogo.");

		builder.Property(x => x.NameSnapshot)
			.HasColumnName("name_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome del servizio congelato al momento della vendita.");

		builder.Property(x => x.VariantLabel)
			.HasColumnName("variant_label")
			.HasMaxLength(80)
			.HasComment("Variante scelta del prodotto (es. \"Macchiato\"); NULL = nessuna variante.");

		builder.Property(x => x.VariantSurcharge)
			.HasColumnName("variant_surcharge")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Sovrapprezzo unitario della variante.");

		builder.Property(x => x.UnitPrice)
			.HasColumnName("unit_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Prezzo unitario bloccato, senza variante.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.IsRequired()
			.HasComment("Unità vendute (totale della riga); il dettaglio per giorno è in booking_resource_consumptions.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.HasComment("Aliquota IVA % congelata per lo scontrino.");

		builder.Property(x => x.LineTotal)
			.HasColumnName("line_total")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale bloccato della riga = (unit_price + variant_surcharge) × quantity; concorre al subtotal della testata.");

		builder.Property(x => x.ServiceDate)
			.HasColumnName("service_date")
			.HasColumnType("date")
			.HasComment("Data di riferimento del servizio quando è una sola; per servizi multi-data le date sono in booking_resource_consumptions.");

		builder.Property(x => x.BookingChannel)
			.HasColumnName("booking_channel")
			.HasColumnType("booking.booking_channel")
			.IsRequired()
			.HasComment("Canale della prenotazione di origine, denormalizzato dalla testata.");

		builder.Property(x => x.IsOrderFilled)
			.HasColumnName("is_order_filled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("La riga è stata evasa (bar/cucina); il riepilogo a livello prenotazione è su booking_reservations.");

		builder.Property(x => x.IsPaidWithWallet)
			.HasColumnName("is_paid_with_wallet")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("La riga è stata pagata col borsellino/tessera prepagata; il movimento wallet è in C3.");

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
			.HasComment("Soft-delete della voce servizio (rimossa dalla prenotazione).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_booking_service_lines_public_id");

		builder.HasIndex(x => x.ReservationId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_lines_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.Kind })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_lines_property_kind");

		builder.HasIndex(x => x.SaleItemId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_lines_sale_item");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("is_order_filled = false AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_lines_order_open");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.ServiceLines)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_service_lines_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany(l => l.ServiceLines)
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_service_lines_line")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

`BookingServiceLine` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable`
(`created_at`/`updated_at`). `property_id` e `sale_item_id` sono `public_id`
copiati da `catalog` (C2, solo `HasComment`, nessuna FK cross-schema); le FK EF reali sono
`reservation_id` → `booking_reservations` (`Cascade`) e `booking_line_id` (nullable) →
`booking_lines` (`SetNull`, coerente con "articolo aggiuntivo/servizio senza postazione" quando la
riga postazione viene rimossa).

### Note di migrazione

- `servicesData` bar/normal (`bookings`/`service_reservation`) → 1 riga per servizio con `quantity` =
  somma dei `click`/`count` frammentati nel JSON legacy.
- `hourlyServiceData` → 1 riga per (servizio × slot) con `kind = Hourly`; gli slot/date effettivi vanno
  in [`booking_service_time_slots`](booking_service_time_slots.md), non su questa riga.
- `services_booking_counts` con **98% delle righe a `quantity = 0`** → **non migrate**: qui restano
  solo righe reali (`CHECK (quantity > 0)`); il conteggio per data vive in
  `booking_resource_consumptions`.
- Immagini `data:` base64 inline e costanti UI (`day_constant`, `timeslot_constants`,
  `bar_category_constants`, …) del blob `servicesData` → **scartate**, restano in anagrafica C2.
- `percentIVA` stringa (con `""` e drift) → `vat_rate numeric(5,2)` normalizzato, `NULL` se non
  determinabile.
- `services_booking_counts.booking_type` (`"0"`/`"1"`) → `booking_channel` enum condiviso con
  `booking_reservations`.
