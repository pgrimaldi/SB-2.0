# Tabella `booking_resource_availability_daily`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_resource_availability_daily` · **forma canonica**: `booking.resource_availability_daily`.
>
> **Aggregato di disponibilità per (risorsa/tipo × giorno)**. Nuova: nel legacy la disponibilità si
> ricavava con `GROUP BY` su centinaia di righe di dettaglio (`booking_items_counts_details` +
> `additional_items_counts_details` + …) confrontate con colonne fisse di `warehouses`. Disegno:
> [`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md),
> [`analisi-post/db/_config-snapshot-proposta.md` §6](../_config-snapshot-proposta.md) (overlay
> disponibilità richiesto da C2, disegnato qui).

---

## Descrizione

Proiezione **mantenuta dal DB** (trigger su
[`booking_resource_consumptions`](booking_resource_consumptions.md), oppure vista materializzata con
refresh mirato). Una riga per `(property_id, resource_type, resource_id?, consumption_date,
booking_period)` con `booked_qty` / `total_qty` / `available_qty` (generata). Serve:

- la ricerca pubblica "disponibile il {data}" (badge, filtri) — proiezione **grossolana** per tipo;
- il rendering veloce della mappa spiaggia — proiezione **fine** per postazione;
- la validazione rapida in preventivo (`POST /bookings/quote`) prima della conferma transazionale.

`total_qty` viene da C2: `catalog_inventory_items.total_quantity` per il magazzino, `1` per la singola
postazione, la capienza area per `area_seat`, `maximum_reservations` del servizio per bar/normale/orario
(se impostato). Un cambio di giacenza in C2 emette un evento che riallinea `total_qty` qui.

Correzioni rispetto al legacy:

- **`GROUP BY` a runtime su tabelle di dettaglio + confronto per stringa con `warehouses`** → `SELECT`
  puntuale su questa tabella.
- **giacenza (`warehouses.<item>_count`) letta live e per colonna fissa** → `total_qty` materializzato,
  aggiornato a evento da C2.
- **nessuna riconciliazione** → job periodico che ricalcola da `booking_resource_consumptions` e
  confronta (rileva drift trigger↔dettaglio).


> ### ✅ Applicato nel backend (2026-09-21)
>
> Partizionamento **annuale per anno solare** su `consumption_date`
> ([`docs/05-ottimizzazioni-db.md` §2](../../../progettazione/05-ottimizzazioni-db.md)), migration
> `PartitionResourceTablesByYear` di `Module.Booking`. Partizioni `_y2021`…`_y2035` più una `_default`
> come rete di sicurezza per date fuori intervallo. Fino al 2026-09-30 arrivavano al 2030: le
> 2031–2035 le ha aggiunte `ExtendPartitionsAndResaleOfferExclude`, perché la `_default` non fallisce —
> raccoglie in silenzio le righe di un anno senza partizione, e finché le contiene la partizione di
> quell'anno non è più creabile. Dal 2026-10-06 entrambe le migration sono consolidate nella
> `InitialCreate` di `Module.Booking` ([`docs/42-migration.md` §10](../../../progettazione/42-migration.md));
> manutenzione in [`docs/06-partizioni-manutenzione.md`](../../../progettazione/06-partizioni-manutenzione.md).
>
> **La chiave primaria è ora `(id, consumption_date)`**, non `id` da solo: PostgreSQL impone che la PK
> di una tabella partizionata contenga la chiave di partizione. Non è una scelta di modellazione, è il
> prezzo del partizionamento — `id` resta comunque un'identità globale sul padre.
>
> **Perché anno solare e non stagione**: le stagioni sono definite per stabilimento
> ([`catalog_pricing_seasons`](../catalog/catalog_pricing_seasons.md)), quindi non esiste un confine
> unico su cui tagliare. L'anno solare è l'unico criterio condiviso da tutte le property.
>
> Questa tabella è oggi vuota (è una proiezione ricostruibile), ma segue la stessa partizione della
> sorgente `booking_resource_consumptions`: tenerle allineate evita che il job di ricostruzione debba
> ragionare su due schemi diversi. L'indice unico `uq_..._grain` contiene già `consumption_date` e resta
> valido su tabella partizionata.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_resource_availability_daily` | `id` | PK | — |
| `uq_..._grain` | `(property_id, resource_type, coalesce(resource_id,'00000000-0000-0000-0000-000000000000'::uuid), consumption_date, booking_period)` | UNIQUE | 1 riga per grana (chiave dell'`UPSERT`) |
| `ix_..._search` | `(property_id, consumption_date, resource_type) WHERE available_qty > 0` | parziale | ricerca "disponibile il {data}" per tipo |
| `ix_..._spot_day` | `(property_id, consumption_date) WHERE resource_id IS NOT NULL` | parziale | overlay mappa per giorno |
| `ix_..._sector` | `(sector_id, consumption_date) WHERE sector_id IS NOT NULL` | parziale | disponibilità per settore |

## Campi

### id
- **Legacy**: — (nuova)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: —
- **Descrizione**: stabilimento.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2.

### resource_type
- **Legacy**: —
- **Descrizione**: tipo di risorsa (stesso enum di
  [`booking_resource_consumptions.resource_type`](booking_resource_consumptions.md)).
- **Tipo**: `enum booking.resource_type`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### resource_id
- **Legacy**: —
- **Descrizione**: risorsa specifica (postazione) per la proiezione fine; `NULL` per la proiezione
  grossolana per tipo (es. "quanti lettini liberi il giorno D").
- **Tipo**: `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` copiato** da C2 (`catalog_map_spots` / `catalog_inventory_items`).

### sector_id
- **Legacy**: —
- **Descrizione**: settore della risorsa (per i filtri di disponibilità per fila). Denormalizzato da C2
  al momento del rebuild.
- **Tipo**: `uuid` (= `public_id` di `catalog_map_sectors`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` copiato** da C2 [`catalog_map_sectors`](../catalog/catalog_map_sectors.md).

### consumption_date
- **Legacy**: —
- **Descrizione**: giorno.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### booking_period
- **Legacy**: —
- **Descrizione**: fascia (`full_day` aggrega mattina+pomeriggio in lettura secondo la regola del
  legacy: `FullDay` include `Morning`+`Afternoon`).
- **Tipo**: `enum booking.day_period`.
- **Default**: `full_day`.
- **Nullabile**: No.
- **Relazioni**: —.

### booked_qty
- **Legacy**: `SUM(quantity)` su `*_items_counts_details` con `status = 1`
- **Descrizione**: unità occupate (`state <> 'released'`) per la grana.
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: mantenuto atomico dal trigger (`booked_qty ± NEW.quantity`).

### held_qty
- **Legacy**: — (il legacy non distingueva)
- **Descrizione**: quota di `booked_qty` in stato `held` (blocco temporaneo non ancora confermato) —
  utile per mostrare "in prenotazione" senza contarlo come definitivo.
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### total_qty
- **Legacy**: `warehouses.<item>_count` (letto live, per colonna fissa)
- **Descrizione**: capienza totale della grana. `1` per la postazione puntuale; giacenza per il
  magazzino; capienza area; `maximum_reservations` del servizio (se impostato, altrimenti "illimitato").
- **Tipo**: `integer`.
- **Default**: `NULL` (`NULL` = nessun limite → `available_qty` non vincolante).
- **Nullabile**: Sì.
- **Relazioni**: aggiornato a evento da C2 (`InventoryQuantityChanged`, `ServiceMaxReservationsChanged`).

### available_qty
- **Legacy**: calcolo a runtime
- **Descrizione**: `total_qty - booked_qty` quando `total_qty` è valorizzato; altrimenti `NULL`
  (illimitato). Colonna **generata**.
- **Tipo**: `integer` `GENERATED ALWAYS AS (total_qty - booked_qty) STORED`.
- **Default**: —.
- **Nullabile**: Sì.
- **Relazioni**: indicizzata parziale `WHERE available_qty > 0` per la ricerca.

### rebuilt_at
- **Legacy**: —
- **Descrizione**: ultimo ricalcolo completo (job di riconciliazione).
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: —
- **Descrizione**: creazione / ultimo `UPSERT`.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` (+ trigger).
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: —
- **Descrizione**: presente per uniformità; non usato (proiezione — si ricostruisce, non si
  soft-elimina). Le righe di date passate le rimuove la retention (hard-delete).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- È una **proiezione**: la coerenza con `booking_resource_consumptions` è garantita tenendo
  l'aggiornamento **lato DB** (trigger `UPSERT` atomico `booked_qty ± quantity`) + job di
  riconciliazione periodico che ricalcola e confronta.
- Costo: una scrittura in più per consumo (assorbita dal trigger). Beneficio: la disponibilità è una
  `SELECT` puntuale invece di `GROUP BY` su centinaia di righe di dettaglio.
- Le righe di date passate vanno in retention (hard-delete) per non far crescere la tabella
  indefinitamente ([`docs/80-retention.md`](../../../progettazione/80-retention.md)).
- La proiezione **fine** (per `resource_id`) si genera solo se il rendering mappa a volume lo richiede
  ([`_config-snapshot-proposta.md` §6](../_config-snapshot-proposta.md) opzione (ii)); di base basta la
  grossolana per tipo + calcolo on-the-fly da `booking_resource_consumptions` con cache a breve TTL.

## Implementazione .NET

`ResourceAvailabilityDaily` **non è un aggregate di dominio**: è una **proiezione/read-model** del modulo
`Module.Booking`, schema `booking`, mantenuta lato DB da un trigger su
[`booking_resource_consumptions`](booking_resource_consumptions.md) (`UPSERT` atomico
`booked_qty ± quantity`) e da un handler eventi per `total_qty` (`InventoryQuantityChanged`,
`ServiceMaxReservationsChanged` da C2). L'Application layer la **legge** (`AsNoTracking()`), non la scrive
mai via `SaveChanges`: le uniche scritture applicative sono il job di riconciliazione
`AvailabilityReconciliation`, che esegue un `UPSERT` raw SQL (stessa funzione del trigger), non un
`Update()` tracciato su un'istanza di dominio. Per questo **non** implementa `ISoftDeletable`/`IAuditable`
(nessun ciclo di vita applicativo da tracciare: `deleted_at` è mappato solo per uniformità e non è mai
valorizzato — le righe di date passate le rimuove la retention con hard-delete, non un soft-delete).

`resource_type` è l'**enum condiviso** già definito in
[`booking_resource_consumptions.md`](booking_resource_consumptions.md); `booking_period` (`DayPeriod`) è
l'enum condiviso definito in [`booking_lines.md`](booking_lines.md) — nessuno dei due va ridefinito qui.

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResourceAvailabilityDailyConfiguration : IEntityTypeConfiguration<ResourceAvailabilityDaily>
{
	public void Configure(EntityTypeBuilder<ResourceAvailabilityDaily> builder)
	{
		builder.ToTable("booking_resource_availability_daily", "booking", t => t.HasComment(
			"Proiezione di disponibilità per (risorsa/tipo × giorno), mantenuta dal DB via trigger su " +
			"booking_resource_consumptions: sostituisce il GROUP BY a runtime del legacy."));

		builder.HasKey(x => x.Id).HasName("pk_booking_resource_availability_daily");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.ResourceType)
			.HasColumnName("resource_type")
			.HasColumnType("booking.resource_type")
			.IsRequired()
			.HasComment("Tipo di risorsa (stesso enum di booking_resource_consumptions.resource_type)");

		builder.Property(x => x.ResourceId)
			.HasColumnName("resource_id")
			.HasComment("public_id C2 della risorsa (proiezione fine); NULL per la proiezione grossolana per tipo");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("public_id di catalog.map_sectors, denormalizzato al momento del rebuild — nessuna FK cross-schema");

		builder.Property(x => x.ConsumptionDate)
			.HasColumnName("consumption_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno della grana di disponibilità");

		builder.Property(x => x.BookingPeriod)
			.HasColumnName("booking_period")
			.HasColumnType("booking.day_period")
			.HasDefaultValue(DayPeriod.FullDay)
			.IsRequired()
			.HasComment("Fascia della grana (full_day aggrega mattina+pomeriggio in lettura)");

		builder.Property(x => x.BookedQty)
			.HasColumnName("booked_qty")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Unità occupate (state <> released) per la grana, mantenuto atomico dal trigger");

		builder.Property(x => x.HeldQty)
			.HasColumnName("held_qty")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Quota di booked_qty in stato held (blocco temporaneo non ancora confermato)");

		builder.Property(x => x.TotalQty)
			.HasColumnName("total_qty")
			.HasComment("Capienza totale della grana (1 per postazione, giacenza magazzino, capienza area, maximum_reservations servizio); NULL = nessun limite");

		builder.Property(x => x.AvailableQty)
			.HasColumnName("available_qty")
			.HasComputedColumnSql("total_qty - booked_qty", stored: true)
			.HasComment("total_qty - booked_qty quando total_qty è valorizzato, altrimenti NULL (illimitato) — colonna generata");

		builder.Property(x => x.RebuiltAt)
			.HasColumnName("rebuilt_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Ultimo ricalcolo completo dal job di riconciliazione");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione della riga di proiezione");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultimo UPSERT della riga di proiezione");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità, non usato: la proiezione si ricostruisce, non si soft-elimina; le righe di date passate le rimuove la retention (hard-delete)");

		builder.HasIndex(x => new { x.PropertyId, x.ConsumptionDate, x.ResourceType })
			.HasFilter("available_qty > 0")
			.HasDatabaseName("ix_booking_resource_availability_daily_search");

		builder.HasIndex(x => new { x.PropertyId, x.ConsumptionDate })
			.HasFilter("resource_id IS NOT NULL")
			.HasDatabaseName("ix_booking_resource_availability_daily_spot_day");

		builder.HasIndex(x => new { x.SectorId, x.ConsumptionDate })
			.HasFilter("sector_id IS NOT NULL")
			.HasDatabaseName("ix_booking_resource_availability_daily_sector");

		// indice unique di grana — vedi nota sotto: usa coalesce(resource_id, ...), non esprimibile in
		// Fluent API pura, rifinito con SQL raw in migration
		builder.HasIndex(x => new { x.PropertyId, x.ResourceType, x.ResourceId, x.ConsumptionDate, x.BookingPeriod })
			.IsUnique()
			.HasDatabaseName("uq_booking_resource_availability_daily_grain");
	}
}
```

**Trigger DB e indice di grana** — non esprimibili in Fluent API pura, aggiunti con SQL raw nella
migration generata:

```csharp
migrationBuilder.Sql("""
	-- l'indice unique generato da EF su (property_id, resource_type, resource_id, consumption_date,
	-- booking_period) va sostituito: resource_id NULL deve contare come chiave (coalesce), altrimenti
	-- Postgres tratta NULL come "diverso da ogni altro NULL" e l'UPSERT sulla grana grossolana duplica.
	DROP INDEX IF EXISTS booking.uq_booking_resource_availability_daily_grain;
	CREATE UNIQUE INDEX uq_booking_resource_availability_daily_grain
		ON booking.booking_resource_availability_daily (
			property_id, resource_type,
			coalesce(resource_id, '00000000-0000-0000-0000-000000000000'::uuid),
			consumption_date, booking_period);

	CREATE OR REPLACE FUNCTION booking.fn_resource_availability_daily_upsert() RETURNS trigger AS $$
	BEGIN
		-- UPSERT booked_qty/held_qty ± quantity sulla grana coinvolta (fine per resource_id, grossolana
		-- per tipo), sia su INSERT/UPDATE (nuovo stato) sia su DELETE/vecchio stato in caso di UPDATE.
		-- Dettaglio della funzione definito in fase di prima migration del dominio.
		RETURN NULL;
	END;
	$$ LANGUAGE plpgsql;

	CREATE TRIGGER trg_resource_consumptions_availability
		AFTER INSERT OR UPDATE OR DELETE ON booking.booking_resource_consumptions
		FOR EACH ROW EXECUTE FUNCTION booking.fn_resource_availability_daily_upsert();
	""");
```

**Note di migrazione**:

- Tabella **nuova**, nessuna colonna legacy diretta: al primo deploy si popola con un rebuild completo da
  `booking_resource_consumptions` (già migrata) invece che da un trigger a freddo.
- `total_qty` iniziale letto da C2 (`catalog_inventory_items.total_quantity`, `1` per postazione, capienza
  area, `maximum_reservations` servizio) al momento del rebuild; da lì in poi mantenuto dagli eventi C2.
- Il job `AvailabilityReconciliation` (giornaliero) ricalcola da `booking_resource_consumptions` e
  confronta con i valori mantenuti dal trigger, loggando i drift — non c'era nulla di equivalente nel
  legacy (il `GROUP BY` a runtime non poteva "driftare").
- La proiezione **fine** (per `resource_id`) va popolata solo se il rendering mappa a volume la richiede
  (opzione (ii) di [`_config-snapshot-proposta.md` §6](../_config-snapshot-proposta.md)); di base basta la
  grossolana per tipo.
