# Tabella `booking_line_additional_items`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_line_additional_items` · **forma canonica**: `booking.line_additional_items`.
>
> Sostituisce i JSON `bookings.additionalList[]`, `bookings.certainAddItemData[]` e la parte
> "articoli aggiuntivi" di `packageSelectedData[].AdditionalItem`. Fonti:
> [`analisi-pre/db/bookings.md`](../../../analisi-pre/db/bookings.md),
> [`analisi-pre/db/additional_items_counts_details.md`](../../../analisi-pre/db/additional_items_counts_details.md),
> [`docs/03-gestione-dati.md` §3.6, §3.7](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga per articolo aggiuntivo acquistato su una riga prenotazione** (lettino extra, cabina, sdraio,
sedia regista, ingressi, tassa di soggiorno…), con quantità e prezzo bloccato. Se l'articolo è scelto
**solo per certe date** (`certainAddItemData` legacy → tabella
[`additional_items_counts_details`](../../../analisi-pre/db/additional_items_counts_details.md)), la
copertura per data è `date_scope = specific_dates` + le righe di
[`booking_resource_consumptions`](booking_resource_consumptions.md) per quei giorni; altrimenti
`date_scope = whole_stay` (tutta la durata della riga).

Correzioni rispetto al legacy:

- **`additionalList[].name` / `it` (nome EN/IT stringa libera, con refusi e `\t`)** → `catalog_item_id`
  FK a [`catalog_inventory_items`](../catalog/catalog_inventory_items.md) **o**
  [`catalog_pricing_price_lists`](../catalog/catalog_pricing_price_lists.md) (`kind = additional`),
  discriminati da `item_source`; + `name_snapshot`.
- **`value` (quantità, `int` con `0` e negativi in produzione)** → `quantity smallint` con
  `CHECK (quantity > 0)`.
- **`price` (numero nel JSON)** → `unit_price numeric(12,2)`.
- **`hotelPrefixedName`** → derivato in presentazione da `booking_reservations.agreement_partner_id`,
  non persistito.
- **`certainAddItemData` vs `additionalList`** (due JSON, due tabelle contatori) → un'unica tabella con
  `date_scope`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_line_additional_items` | `id` | PK | — |
| `ix_..._line` | `(booking_line_id) WHERE deleted_at IS NULL` | parziale | articoli di una riga |
| `ix_..._reservation` | `(reservation_id) WHERE deleted_at IS NULL` | parziale | articoli aggiunti "sulla prenotazione" (non su una postazione) |
| `ix_..._catalog_item` | `(catalog_item_id) WHERE deleted_at IS NULL` | parziale | reportistica per articolo |

## Campi

### id
- **Legacy**: — (elemento di array JSON)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### reservation_id
- **Legacy**: `bookings.bookingNumber`
- **Descrizione**: prenotazione a cui appartiene l'articolo.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### booking_line_id
- **Legacy**: `additionalList[].location` (postazione a cui l'articolo è associato)
- **Descrizione**: riga postazione a cui l'articolo è agganciato. `NULL` = articolo "sulla
  prenotazione" nel complesso (non su una postazione specifica).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`booking_lines.id`](booking_lines.md) `ON DELETE CASCADE`.

### property_id
- **Legacy**: `additional_items_counts_details.property_id`
- **Descrizione**: stabilimento (denormalizzato per indici locali).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dalla testata.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2.

### item_source
- **Legacy**: implicito (l'articolo poteva venire dal listino `additional` o dal magazzino)
- **Descrizione**: da dove proviene l'anagrafica dell'articolo.
- **Tipo**: `enum booking.additional_item_source` — valori: `price_list` (voce
  `catalog_pricing_price_lists` `kind = additional`), `inventory` (voce `catalog_inventory_items`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: discrimina il significato di `catalog_item_id`.

### catalog_item_id
- **Legacy**: `additionalList[].name` (stringa, match per uguaglianza con `warehouses` — fragile)
- **Descrizione**: id dell'articolo di catalogo.
- **Tipo**: `uuid` (= `public_id` di `catalog_pricing_price_lists` **o** `catalog_inventory_items`
  secondo `item_source`).
- **Default**: nessuno.
- **Nullabile**: Sì (articolo personalizzato non a catalogo — il legacy ammetteva nomi arbitrari).
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### name_snapshot
- **Legacy**: `additionalList[].it` / `name`
- **Descrizione**: nome dell'articolo **congelato** al momento della prenotazione (mostrato in
  ricevute/dettaglio).
- **Tipo**: `varchar(160)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot; refusi legacy (`"Lettino Standard\t"`) normalizzati in migrazione.

### date_scope
- **Legacy**: `additionalList` (tutta la durata) vs `certainAddItemData` (date puntuali)
- **Descrizione**: copertura temporale dell'articolo.
- **Tipo**: `enum booking.additional_item_date_scope` — valori: `whole_stay`, `specific_dates`.
- **Default**: `whole_stay`.
- **Nullabile**: No.
- **Relazioni**: se `specific_dates`, le date effettive sono le righe di
  [`booking_resource_consumptions`](booking_resource_consumptions.md) generate per l'articolo
  (`resource_type` dell'articolo, `source` = questo record).

### quantity
- **Legacy**: `additionalList[].value` / `certainAddItemData[].value` (`int`; in produzione `0` e valori negativi — dati sporchi)
- **Descrizione**: unità acquistate (per giorno se `specific_dates`, per l'intera durata altrimenti).
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (quantity > 0)` — le righe legacy a `0`/negative non si migrano.

### unit_price
- **Legacy**: `additionalList[].price` (numero nel JSON)
- **Descrizione**: prezzo unitario **bloccato**.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### line_total
- **Legacy**: `packageSelectedData[].AdditionalItemPrice` (aggregato)
- **Descrizione**: totale bloccato dell'articolo = `quantity × unit_price × (nr. giorni se whole_stay)`.
  Persistito per non ricalcolarlo (parte dell'aggregato-documento).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: concorre a `booking_reservations.subtotal`.

### created_at / updated_at
- **Legacy**: `additional_items_counts_details.createdAt` / `updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` (+ trigger).
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: destroy+recreate (nessun soft-delete)
- **Descrizione**: soft-delete della riga articolo.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF.

## Valutazione JSON

Nessun JSON. `additionalList[]` e `certainAddItemData[]` → righe di questa tabella; il consumo per
giorno (ex `booking_items_counts_details` / `additional_items_counts_details`) →
[`booking_resource_consumptions`](booking_resource_consumptions.md).

## Suggerimenti per le prestazioni

- Poche righe per prenotazione. Gli indici per `booking_line_id` e `reservation_id` coprono il caricamento
  del dettaglio prenotazione.
- Il consumo per data e il confronto con le giacenze non si fa qui ma sull'aggregato
  `booking_resource_availability_daily`.

## Implementazione .NET

`BookingLineAdditionalItem` è **entità figlia** dell'aggregato `Reservation` (modulo `Module.Booking`,
schema `booking`): articolo aggiuntivo agganciato o a una specifica [`booking_lines`](booking_lines.md)
(`booking_line_id` valorizzato) o direttamente alla prenotazione nel suo complesso (`booking_line_id
= NULL`). Sempre creata/modificata tramite l'aggregate root
(`Reservation.AddAdditionalItem(...)`), mai persistita in isolamento.

### Enum propri

```csharp
// Module.Booking.Domain.Enums

/// Da dove proviene l'anagrafica dell'articolo aggiuntivo.
public enum AdditionalItemSource
{
	PriceList = 0,
	Inventory = 1
}

/// Copertura temporale dell'articolo aggiuntivo sulla riga.
public enum AdditionalItemDateScope
{
	WholeStay = 0,
	SpecificDates = 1
}
```

```csharp
// Module.Booking.Infrastructure.DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<AdditionalItemSource>("booking", "additional_item_source");
modelBuilder.HasPostgresEnum<AdditionalItemDateScope>("booking", "additional_item_date_scope");
```

```csharp
// Module.Booking Infrastructure DI — registrazione data source Npgsql
dataSourceBuilder.MapEnum<AdditionalItemSource>("booking.additional_item_source");
dataSourceBuilder.MapEnum<AdditionalItemDateScope>("booking.additional_item_date_scope");
```

### `BookingLineAdditionalItemConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingLineAdditionalItemConfiguration : IEntityTypeConfiguration<BookingLineAdditionalItem>
{
	public void Configure(EntityTypeBuilder<BookingLineAdditionalItem> builder)
	{
		builder.ToTable("booking_line_additional_items", "booking", t =>
		{
			t.HasComment("Articolo aggiuntivo (lettino extra, cabina, tassa di soggiorno, ...) acquistato su una riga o sull'intera prenotazione.");
			t.HasCheckConstraint("ck_booking_line_additional_items_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_line_additional_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui appartiene l'articolo.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga postazione a cui l'articolo è agganciato; NULL = articolo sulla prenotazione nel complesso.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema.");

		builder.Property(x => x.ItemSource)
			.HasColumnName("item_source")
			.HasColumnType("booking.additional_item_source")
			.IsRequired()
			.HasComment("Origine dell'anagrafica dell'articolo: voce di listino o articolo di magazzino.");

		builder.Property(x => x.CatalogItemId)
			.HasColumnName("catalog_item_id")
			.HasComment("public_id di catalog.pricing_price_lists o catalog.inventory_items (secondo item_source), copiato — NULL per articolo personalizzato non a catalogo.");

		builder.Property(x => x.NameSnapshot)
			.HasColumnName("name_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome dell'articolo congelato al momento della prenotazione.");

		builder.Property(x => x.DateScope)
			.HasColumnName("date_scope")
			.HasColumnType("booking.additional_item_date_scope")
			.IsRequired()
			.HasDefaultValueSql("'whole_stay'")
			.HasComment("Copertura temporale: tutta la durata della riga o solo date puntuali.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasColumnType("smallint")
			.IsRequired()
			.HasComment("Unità acquistate (per giorno se specific_dates, per l'intera durata se whole_stay).");

		builder.Property(x => x.UnitPrice)
			.HasColumnName("unit_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Prezzo unitario bloccato dell'articolo.");

		builder.Property(x => x.LineTotal)
			.HasColumnName("line_total")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale bloccato dell'articolo (quantità × prezzo unitario, eventualmente × giorni); concorre al subtotal della testata.");

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
			.HasComment("Soft-delete della riga articolo.");

		builder.HasIndex(x => x.BookingLineId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_line_additional_items_line");

		builder.HasIndex(x => x.ReservationId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_line_additional_items_reservation");

		builder.HasIndex(x => x.CatalogItemId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_line_additional_items_catalog_item");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.AdditionalItems)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_line_additional_items_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany(l => l.AdditionalItems)
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_line_additional_items_line")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`BookingLineAdditionalItem` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable`
(`created_at`/`updated_at`). `property_id` e `catalog_item_id` sono `public_id`
copiati da altri moduli (C2/C4, nessuna FK cross-schema, solo `HasComment`); le FK EF reali sono
`reservation_id` → `booking_reservations` e `booking_line_id` (nullable) → `booking_lines`, entrambe
nello schema `booking`.

### Note di migrazione

- **Merge di due JSON legacy**: `additionalList[]` (copertura `whole_stay`) e `certainAddItemData[]`
  (copertura `specific_dates`, ex tabella `additional_items_counts_details`) confluiscono in un'unica
  tabella distinta da `date_scope`.
- `additionalList[].value` / `certainAddItemData[].value` con `0` o valori **negativi** (dati sporchi
  osservati in produzione) → righe **scartate** (`CHECK (quantity > 0)` non le ammetterebbe).
- `additionalList[].name`/`it` (stringa libera, con refusi e `\t`) → tentativo di risoluzione contro
  `catalog_pricing_price_lists`/`catalog_inventory_items` per nome normalizzato; se non risolvibile,
  `catalog_item_id = NULL` e `name_snapshot` bonificato (trim, refusi corretti) invece di scartare la
  riga.
- **`lineItemId` (uuid sparso in `packageSelectedData`)** → **non migrato, e la colonna che lo
  ospitava è stata rimossa** il 2026-10-01 (M5 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`). Era una pezza del legacy:
  le voci di una prenotazione non erano righe ma elementi di array JSON, che non hanno chiave
  primaria, e il backend se ne inventava una con `uuidv4()` per usarla nel payload dello
  scontrino. Nel nuovo schema quelle voci **sono righe vere**, con `id` e `public_id` propri.
  Il collegamento che la colonna prometteva non è mai esistito nemmeno nel legacy: in
  `fiskaly_receipt.entries` gli elementi hanno sei chiavi (`type`, `description`, `category`,
  `quantity`, `unit_price`, `vat`) e nessun `lineItemId` — verificato, zero occorrenze su 2.609
  righe. Il riferimento corretto esiste già nel verso opposto:
  `fiskaly.receipt_lines.source_line_public_id`.
