# Tabella `catalog_map_spots`

> **Dominio**: C2 — `catalog` · **sottoarea**: `map` · **schema**: `catalog` · **nome flat**:
> `catalog_map_spots` · **forma canonica**: `catalog.map_spots`.
>
> Rimpiazza il contenuto dell'array JSON `configurations.beachMapData` (**173 righe** = 1 blob per
> stabilimento, fino a ~670 KB per riga, 21 forme JSON distinte). Fonti:
> [`analisi-pre/db/configurations.md`](../../../analisi-pre/db/configurations.md),
> [`analisi-pre/db/configure_maps.md`](../../../analisi-pre/db/configure_maps.md),
> [`docs/03-gestione-dati.md` §3.8 (`resource_consumptions.resource_id` → anagrafica postazioni del modulo Catalogo)](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Anagrafica delle postazioni** (ombrelloni, lettini, cabine, parcheggi, tende, biglietti, aree…) di uno
stabilimento: **una riga per postazione**. È il registro delle *risorse prenotabili* che C1 referenzia
come `resource_id` in `resource_consumptions` e nelle righe prenotazione (`booking_lines.resource_id`).

Nel legacy la griglia era un unico JSON `beachMapData` (`[{row, rec:[{cell, id, items:{name, it, itemIcon,
…}}]}]`): posizione, tipo elemento e stato locale erano tutti dentro il blob, con `items.name` = stringa
letterale da un elenco chiuso hardcoded nel backend (`itmNames`: `Umbrella`, `Sunbed`, `Cabin`, `Parking`,
`Tent`, `SpecialCabin`, `SpecialUmbrella1..5`, `XLSunbed`, `UmbrellaArea`, `CabinArea`, `SundbedArea`
[sic], `ParkingArea`, `Ticket`).

Correzioni rispetto al legacy:

- **1 riga per postazione** (non 1 blob per stabilimento) → query dirette, indici, integrità.
- **`items.name` stringa hardcoded → `enum` `catalog.spot_type`** (canonicalizzato, refuso `Sundbed`
  corretto).
- **posizione** (`R{riga}C{colonna}`) → colonne `grid_row` / `grid_column` numeriche.
- **etichetta** della postazione (il "numero" mostrato al cliente, es. `A12`) → colonna `label`.
- **icone locali** nel blob → riferimento a [`catalog_map_layouts`](catalog_map_layouts.md) per tipo
  (nessuna icona ridondante per cella).
- lo **stato di occupazione** (`isReserved`, `isSelected`) **non** vive qui: è un dato transazionale per
  data (C1, `resource_consumptions` / `location_reservations`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_map_spots` | `id` | PK | — |
| `uq_catalog_map_spots_public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C1 `resource_id`) |
| `uq_..._property_label` | `(property_id, lower(label)) WHERE deleted_at IS NULL` | UNIQUE parziale | numero postazione univoco per stabilimento |
| `ix_..._property_type` | `(property_id, spot_type) WHERE deleted_at IS NULL` | parziale | "tutti gli ombrelloni di X" |
| `ix_..._sector` | `(sector_id) WHERE deleted_at IS NULL` | parziale | postazioni di un settore (prezzi/limiti/sconti) |
| `ix_..._grid` | `(property_id, grid_row, grid_column)` | — | rendering griglia |

## Campi

### id
- **Legacy**: `beachMapData[].rec[].items.id` (id numerico dell'elemento dentro il blob — non stabile fra le forme JSON)
- **Descrizione**: PK tecnica, id **interno** della postazione.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (stesso schema) da
  [`catalog_property_online_allowed_locations.spot_id`](catalog_property_online_allowed_locations.md).

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile, **copiato cross-modulo** (C1 `resource_id`, snapshot mappa).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C1 (`booking_lines.resource_id`, `resource_consumptions.resource_id`).

### property_id
- **Legacy**: `configurations.propertyId`
- **Descrizione**: stabilimento proprietario della postazione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.

### sector_id
- **Legacy**: dedotto (nel legacy la riga della griglia → settore per nome, correlazione fragile)
- **Descrizione**: settore a cui appartiene la postazione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (postazioni fuori settore / non assegnate).
- **Relazioni**: **FK** → [`catalog_map_sectors.id`](catalog_map_sectors.md) `ON DELETE RESTRICT`.

### label
- **Legacy**: `beachMapData[].rec[].name` / il "numero" mostrato in mappa (es. `A12`, `U4`, `12`)
- **Descrizione**: etichetta/numero della postazione visibile al cliente e all'operatore.
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `UNIQUE` per property (case-insensitive); i riferimenti legacy per etichetta
  (`onlineBookingOptions.locations`) si risolvono su questa colonna in migrazione.

### spot_type
- **Legacy**: `beachMapData[].rec[].items.name` (elenco chiuso `itmNames` hardcoded nel backend)
- **Descrizione**: tipo di elemento posizionato nella cella.
- **Tipo**: `enum catalog.spot_type` — valori: `umbrella`, `sunbed`, `sunbed_xl`, `cabin`, `special_cabin`,
  `special_umbrella1`, `special_umbrella2`, `special_umbrella3`, `special_umbrella4`,
  `special_umbrella5`, `tent`, `parking`, `ticket`, `umbrella_area`, `cabin_area`, `sunbed_area`,
  `parking_area`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: allineato all'enum `resource_type` di
  [`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md) usato da C1 (mappatura al bordo del
  modulo).
- **Note**: `SundbedArea` (refuso legacy) → `sunbed_area`; `XLSunbed` → `sunbed_xl`.

### is_area
- **Legacy**: `items.name` che termina in `Area` (`UmbrellaArea`, `CabinArea`, …) + `areaItemCount` nel blob
- **Descrizione**: `true` se la postazione rappresenta un'**area** con più posti (non una singola
  postazione).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### area_seat_count
- **Legacy**: `beachMapData[].rec[].areaItemCount` (presente solo in alcune forme JSON)
- **Descrizione**: numero di posti dell'area (rilevante solo se `is_area = true`).
- **Tipo**: `smallint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### grid_row / grid_column
- **Legacy**: `beachMapData[].row` e la parte `C{n}` di `cell` / `id` (`"R{riga}C{colonna}"`)
- **Descrizione**: posizione della postazione nella griglia del layout.
- **Tipo**: `smallint` ciascuna.
- **Default**: nessuno.
- **Nullabile**: Sì (postazioni non posizionate su griglia).
- **Relazioni**: coerenti con `grid_rows` / `grid_columns` di
  [`catalog_map_layouts`](catalog_map_layouts.md) (validazione applicativa; il resize non le tronca in
  silenzio).

### is_bookable
- **Legacy**: — (nel legacy la "prenotabilità" dipendeva da `isReserved` locale e da altre tabelle)
- **Descrizione**: la postazione è offerta alla prenotazione (esclude celle "vuote"/decorative della
  griglia).
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: le celle del blob legacy senza `items` (spazio vuoto) **non** diventano righe qui — solo le
  celle con un elemento reale.

### created_by_user_id
- **Legacy**: `configurations.createdBy`
- **Descrizione**: operatore che ha configurato la mappa.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 26.027 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: `configurations.createdAt` / `configurations.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: rimozione della cella dal blob (spesso per troncamento di resize)
- **Descrizione**: soft-delete della postazione.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: una postazione con prenotazioni storiche (C1) non si cancella fisicamente — `deleted_at` +
  `is_bookable = false` la ritira dall'offerta mantenendo la risolubilità di `resource_id` storici.

## Campi legacy **non** migrati / spostati

| Campo legacy | Destinazione |
|---|---|
| `beachMapData[].rec[].isReserved` / `isSelected` / `currentRow` | stato transazionale per data → **C1** (`resource_consumptions` / `location_reservations`) |
| `items.itemIcon` / `emptyItemImg` / `reservedItemImg` / `bookedItemImg` | icone per tipo → [`catalog_map_layouts`](catalog_map_layouts.md) (non per cella) |
| `items.it` | etichetta tradotta del tipo → risorse di localizzazione (chiave = `spot_type`) |
| `configurations.sharing_enable` / `sharing_seasonal_workers` / `credit_perday_resold` / `credit_value` / `disable_both_options` / `enable_sharing_on_all_article` / `plan_absence_as_sharing` | parametri feature "sharing/rivendita" → **C1** (sottoarea rivendita/`hold`), [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md) |
| celle vuote/decorative del blob | non diventano righe |

## Valutazione JSON

Nessun JSON. Il blob `beachMapData` (fino a 670 KB, 21 forme) è interamente normalizzato: 1 riga per
postazione, tipo → enum, posizione → colonne, stato → C1, icone → layout.

## Suggerimenti per le prestazioni

- Tabella potenzialmente la più grande di C2: ~175 stabilimenti × ~50–300 postazioni ≈ 20–50k righe —
  comunque piccola. Indici su `(property_id, spot_type)` e `(sector_id)` coprono i lookup di
  configurazione/prezzo.
- Il **rendering mappa lato cliente** (griglia + stato occupazione del giorno X) è servito da un read
  model per (property, data): `catalog_map_spots` (statico) + occupazione da C1 — mai ricomponendo il
  blob.
- La migrazione del blob è l'operazione più delicata di C2: parser tollerante alle 21 forme, mappatura
  `itmNames` → enum, ricostruzione `label` (il "numero" postazione), assegnazione `sector_id` per nome.

## Implementazione .NET

`MapSpot` è un'**entità figlia** di `Property` con `public_id` esposto: è l'anagrafica delle risorse
prenotabili che C1 referenzia come `resource_id`. Modulo `Module.Catalog`, area `Map`, schema `catalog`,
tabella `map_spots`.

### Enum proprio

```csharp
// Module.Catalog/Domain/Enums/SpotType.cs
public enum SpotType
{
	Umbrella = 0,
	Sunbed = 1,
	SunbedXl = 2,
	Cabin = 3,
	SpecialCabin = 4,
	SpecialUmbrella1 = 5,
	SpecialUmbrella2 = 6,
	SpecialUmbrella3 = 7,
	SpecialUmbrella4 = 8,
	SpecialUmbrella5 = 9,
	Tent = 10,
	Parking = 11,
	Ticket = 12,
	UmbrellaArea = 13,
	CabinArea = 14,
	SunbedArea = 15,
	ParkingArea = 16
}
```

```csharp
modelBuilder.HasPostgresEnum<SpotType>("catalog", "spot_type");
dataSourceBuilder.MapEnum<SpotType>("catalog.spot_type");
```

`SpotType` è concettualmente allineato a `resource_type` di C1 (`docs/03-gestione-dati.md` §3.8), ma è un
enum **proprio** di questo schema: la mappatura verso `resource_type` è una conversione esplicita al
bordo del modulo (nessun enum condiviso cross-schema).

### `MapSpotConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class MapSpotConfiguration : IEntityTypeConfiguration<MapSpot>
{
	public void Configure(EntityTypeBuilder<MapSpot> builder)
	{
		builder.ToTable("map_spots", "catalog", t => t.HasComment(
			"Anagrafica delle postazioni (ombrelloni, lettini, cabine, parcheggi, ...) di uno stabilimento: una riga per postazione. Sostituisce il blob JSON configurations.beachMapData."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_spots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo (C1 resource_id, snapshot mappa).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario della postazione.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("Settore a cui appartiene la postazione.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Etichetta/numero della postazione visibile al cliente e all'operatore (es. A12); univoca per stabilimento.");

		builder.Property(x => x.SpotType)
			.HasColumnName("spot_type")
			.HasColumnType("catalog.spot_type")
			.IsRequired()
			.HasComment("Tipo di elemento posizionato nella cella.");

		builder.Property(x => x.IsArea)
			.HasColumnName("is_area")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True se la postazione rappresenta un'area con più posti, non una singola postazione.");

		builder.Property(x => x.AreaSeatCount)
			.HasColumnName("area_seat_count")
			.HasComment("Numero di posti dell'area, rilevante solo se is_area = true.");

		builder.Property(x => x.GridRow)
			.HasColumnName("grid_row")
			.HasComment("Riga della postazione nella griglia del layout.");
		builder.Property(x => x.GridColumn)
			.HasColumnName("grid_column")
			.HasComment("Colonna della postazione nella griglia del layout.");

		builder.Property(x => x.IsBookable)
			.HasColumnName("is_bookable")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("La postazione è offerta alla prenotazione (esclude celle vuote/decorative della griglia).");

		builder.Property(x => x.CreatedByUserPublicId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha configurato la mappa.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: la postazione non si cancella fisicamente se ha prenotazioni storiche in C1.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_map_spots_public_id");

		builder.HasIndex(x => new { x.PropertyId, x.Label })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_map_spots_property_label");

		builder.HasIndex(x => new { x.PropertyId, x.SpotType })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_spots_property_type");

		builder.HasIndex(x => x.SectorId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_spots_sector");

		builder.HasIndex(x => new { x.PropertyId, x.GridRow, x.GridColumn })
			.HasDatabaseName("ix_catalog_map_spots_grid");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_map_spots_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Sector)
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_map_spots_sector")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

`MapSpot` implementa `IAuditable`/`ISoftDeletable`.

### Relazioni

- `PropertyId` → `Property` (stesso schema): FK reale, `ON DELETE CASCADE`.
- `SectorId` → `MapSector` (stesso schema): FK reale, `ON DELETE RESTRICT`.
- `CreatedByUserPublicId`: id copiato da C5 — solo `.HasComment`, nessuna FK.
- `PublicId` copiato in uscita da C1 (`booking_lines.resource_id`, `resource_consumptions.resource_id`):
  nessuna FK, contratto cross-modulo. Il modulo Catalog non conosce C1: la risoluzione `public_id ↔ id`
  avviene al bordo del modulo chiamante.

### Note di migrazione

- Parser tollerante alle 21 forme JSON di `beachMapData`; solo le celle con un `items` reale diventano
  righe (celle vuote/decorative scartate).
- `items.name` (elenco chiuso `itmNames`) → `SpotType`; refuso `Sundbed` → `SunbedArea`, `XLSunbed` →
  `SunbedXl`.
- Posizione (`R{riga}C{colonna}`) → `GridRow`/`GridColumn` numerici; etichetta (`items.name`/il "numero"
  mostrato) → `Label`.
- `sector_id` risolto dal nome riga → settore (correlazione fragile nel legacy, qui deterministica sulla
  `map_sectors` già migrata).
- `isReserved`/`isSelected`/`currentRow` (stato transazionale) **non** migrati: vivono in C1.
