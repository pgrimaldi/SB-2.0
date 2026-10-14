# Tabella `catalog_service_sale_items`

> **Dominio**: C2 — `catalog` · **sottoarea**: `service` · **schema**: `catalog` · **nome flat**:
> `catalog_service_sale_items` · **forma canonica**: `catalog.service_sale_items`.
>
> Rimpiazza la legacy [`services_types`](../../../analisi-pre/db/services_types.md) (**88 righe** = 1 riga
> per stabilimento, con 3 array JSON: `barServices` fino a ~4 MB, `normalServices`, `hourlyServices`).
> Fonti: [`analisi-pre/db/services_types.md`](../../../analisi-pre/db/services_types.md),
> [`analisi-pre/business-logic/stabilimento/11-configurazione.md`](../../../analisi-pre/business-logic/stabilimento/11-configurazione.md).

---

## Descrizione

I **servizi venduti** da uno stabilimento a corredo della prenotazione: servizi da **bar** (`kind =
bar`), servizi **normali** (`kind = normal`, es. noleggio attrezzatura a giornata), servizi a **fascia
oraria** (`kind = hourly`, es. campo beach volley, SUP). Una riga per servizio (nel legacy erano elementi
degli array `barServices`/`normalServices`/`hourlyServices` di **una** riga per property).

Per i servizi `hourly`, i giorni della settimana attivi stanno nell'array `active_weekdays` di questa
riga (era la figlia [`catalog_service_sale_item_weekdays`](_Zcatalog_service_sale_item_weekdays.md), ora
eliminata) e le fasce orarie interne (con prezzo dedicato) nella figlia
[`catalog_service_sale_item_time_slots`](catalog_service_sale_item_time_slots.md).

Correzioni rispetto al legacy:

- **1 riga per servizio** (non 1 blob per property) → editing granulare, prezzi tipizzati.
- **`Servicemage`** (refuso storico per "ServiceImage", URL stringa) → FK `icon_id` verso
  [`catalog_service_type_icons`](catalog_service_type_icons.md) (o `icon_asset_id` verso
  [`catalog_media_assets`](catalog_media_assets.md) per icone caricate ad hoc).
- **`ServicePrice`** (stringa numerica) → `numeric(12,2)`.
- **`PurchaseStatus`** (int 0/1) → `is_purchasable boolean`.
- **`currentId`** (numero casuale client-side) → scartato.
- cache Redis per `propertyId` (invalidava tutte e 3 le liste insieme) → invalidazione granulare a
  eventi.


> ### ✅ Applicato nel backend (2026-09-21) — la tabella figlia dei giorni è stata collassata
>
> Aggiunta la colonna **`active_weekdays catalog.weekday[]`** ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §13), migration
> `CollapseChildRowsIntoArrays`. La tabella `catalog_service_sale_item_weekdays` **non esiste più**.
>
> Rappresentava un insieme di al più sette valori con **sette righe figlie** e un flag booleano
> ciascuna, più `id`, `created_at`, `updated_at`, `deleted_at` per riga: molte volte il peso del dato
> utile. Nessuna FK in ingresso, nessun audit per singola assegnazione, e sempre letta e scritta per
> intero insieme al padre — cioè tutte le condizioni che il progetto usa altrove per scegliere un array
> (`enabled_payment_methods`, `amenity_ids`).
>
> Array vuoto = servizio mai prenotabile. La scheda della tabella eliminata è archiviata in
> [`_Zcatalog_service_sale_item_weekdays.md`](_Zcatalog_service_sale_item_weekdays.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_service_sale_items` | `id` | PK | — |
| `uq_catalog_service_sale_items_public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C1) |
| `ix_..._property_kind` | `(property_id, kind) WHERE deleted_at IS NULL` | parziale | listino servizi per tipo ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `ix_..._property_purchasable` | `(property_id, kind) WHERE is_purchasable AND deleted_at IS NULL` | parziale | servizi vendibili al checkout |

## Campi

### id
- **Legacy**: — (nuovo; il servizio era un elemento di array)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata dalle figlie `catalog_service_sale_item_*`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico, **copiato cross-modulo** — C1 lo referenzia per i contatori/consumi
  servizi (`services_booking_counts`, `hourly_service_booking_management` →
  `resource_consumptions.resource_id`).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C1.

### property_id
- **Legacy**: `services_types.propertyId`
- **Descrizione**: stabilimento che vende il servizio.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.

### kind
- **Legacy**: array di provenienza (`barServices` → `bar`; `normalServices` → `normal`; `hourlyServices` → `hourly`)
- **Descrizione**: tipo di servizio.
- **Tipo**: `enum catalog.service_sale_kind` — valori: `bar`, `normal`, `hourly`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: per `kind <> hourly` `active_weekdays` è vuoto, la figlia time_slots non ha righe e `hour_from`/`hour_to` sono `NULL`.

### name
- **Legacy**: `barServices[].ServiceName` / `normalServices[].ServiceName` / `hourlyServices[].ServiceName`
- **Descrizione**: nome del servizio mostrato al cliente.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### price
- **Legacy**: `*Services[].ServicePrice` (stringa numerica, es. `"3.50"`)
- **Descrizione**: prezzo unitario del servizio.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: per `hourly`, può essere sovrascritto dalla fascia oraria
  ([`catalog_service_sale_item_time_slots.price`](catalog_service_sale_item_time_slots.md)).

### icon_id
- **Legacy**: `*Services[].Servicemage` (sic — URL icona)
- **Descrizione**: icona del servizio, scelta dal catalogo master.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_service_type_icons.id`](catalog_service_type_icons.md)
  `ON DELETE SET NULL`.
- **Note**: se il gestore carica un'immagine non presente nel master, usare invece `icon_asset_id` verso
  [`catalog_media_assets`](catalog_media_assets.md) — una delle due colonne valorizzata.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 616 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### icon_asset_id
- **Legacy**: `*Services[].Servicemage` quando è un upload custom (URL S3 diretto)
- **Descrizione**: icona caricata ad hoc (alternativa a `icon_id`).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'icon'`).

### is_purchasable
- **Legacy**: `*Services[].PurchaseStatus` (int 0/1 usato come booleano)
- **Descrizione**: il servizio è acquistabile (vs. solo elencato/disattivato).
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### active_weekdays
- **Legacy**: `hourlyServices[].day_constant[]` (solo `kind = hourly`)
- **Descrizione**: giorni della settimana in cui il servizio è prenotabile. Array vuoto = mai
  prenotabile.
- **Tipo**: `catalog.weekday[]` — enum `catalog.weekday`: `monday`, `tuesday`, `wednesday`, `thursday`,
  `friday`, `saturday`, `sunday`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: —. Sostituisce la tabella figlia `catalog_service_sale_item_weekdays` (riquadro sopra).
- **Dati**: valorizzato su 163 servizi su 616 (DB di sviluppo, 2026-10-06) — sono gli `hourly`.

### hour_from / hour_to
- **Legacy**: `hourlyServices[].HourFrom` / `hourlyServices[].HourTo` (solo `kind = hourly`)
- **Descrizione**: fascia oraria complessiva in cui il servizio è disponibile.
- **Tipo**: `time` ciascuno.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per `kind <> hourly`).
- **Relazioni**: —.

### created_by_user_id
- **Legacy**: `services_types.createdBy`
- **Descrizione**: operatore che ha configurato i servizi.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 616 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: `services_types.createdAt` / `services_types.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nel legacy si rimuoveva l'elemento dall'array)
- **Descrizione**: soft-delete del servizio.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: un servizio con consumi storici (C1) non si cancella fisicamente.

## Campi legacy **non** migrati

| Campo legacy | Motivo |
|---|---|
| `*Services[].currentId` | pseudo-id casuale client-side |
| `hourlyServices[].day_constant[]` | → colonna `active_weekdays` (in origine la figlia [`catalog_service_sale_item_weekdays`](_Zcatalog_service_sale_item_weekdays.md), eliminata) |
| `hourlyServices[].timeslot_constants[]` | → [`catalog_service_sale_item_time_slots`](catalog_service_sale_item_time_slots.md) |

## Valutazione JSON

Nessun JSON. I 3 array JSON (`barServices` fino a 4 MB) → una tabella con `kind` discriminatore, un array
di enum per i giorni e una figlia per le fasce orarie.

## Suggerimenti per le prestazioni

- ~centinaia di righe per il sistema (88 property × pochi servizi). Indici parziali per `(property_id,
  kind)`.
- Il consumo servizi (C1: `services_booking_counts`, `hourly_service_booking_management`) confluisce in
  `resource_consumptions` con `resource_id` → `public_id` di questa tabella
  ([`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md)).

## Implementazione .NET

`ServiceSaleItem` è l'**aggregate root** del cluster "servizi venduti" (modulo `Module.Catalog`, area
`Service`, schema `catalog`): possiede la collezione figlia
[`ServiceSaleItemTimeSlot`](catalog_service_sale_item_time_slots.md) (entità con proprio ciclo di vita e
propria PK, non owned type in senso stretto — ha un `public_id` referenziato da C1) e la proprietà
`ActiveWeekdays` (`List<Weekday>` mappata sull'array `catalog.weekday[]`; la vecchia figlia
`ServiceSaleItemWeekday` non esiste più). Implementa
`IAuditable`/`ISoftDeletable` (§1 del template).

### Enum proprio

```csharp
// Domain/Enums/ServiceSaleKind.cs
public enum ServiceSaleKind { Bar = 0, Normal = 1, Hourly = 2 }
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ServiceSaleKind>("catalog", "service_sale_kind");
```

```csharp
// Registrazione data source
dataSourceBuilder.MapEnum<ServiceSaleKind>("catalog.service_sale_kind");
```

Condiviso con [`catalog_service_type_icons.type`](catalog_service_type_icons.md) — stesso enum, non
ridefinito lì.

### Entità

```csharp
// Domain/Entities/ServiceSaleItem.cs
namespace Module.Catalog.Domain.Entities;

public sealed class ServiceSaleItem : IAuditable, ISoftDeletable
{
    public long Id { get; private set; }
    public Guid PublicId { get; private set; }
    public long PropertyId { get; private set; }
    public ServiceSaleKind Kind { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int? IconId { get; private set; }
    public long? IconAssetId { get; private set; }
    public bool IsPurchasable { get; private set; }
    public List<Weekday> ActiveWeekdays { get; private set; } = new();
    public TimeOnly? HourFrom { get; private set; }
    public TimeOnly? HourTo { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private readonly List<ServiceSaleItemTimeSlot> _timeSlots = new();
    public IReadOnlyCollection<ServiceSaleItemTimeSlot> TimeSlots => _timeSlots.AsReadOnly();

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<ServiceSaleItem>`

```csharp
// Infrastructure/Configurations/ServiceSaleItemConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class ServiceSaleItemConfiguration : IEntityTypeConfiguration<ServiceSaleItem>
{
    public void Configure(EntityTypeBuilder<ServiceSaleItem> builder)
    {
        builder.ToTable("service_sale_items", "catalog", t => t.HasComment(
            "Servizio (bar/normale/a fascia oraria) venduto da uno stabilimento a corredo della prenotazione."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_service_sale_items");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.PublicId)
            .HasColumnName("public_id")
            .ValueGeneratedNever()
            .HasComment("Id pubblico, copiato cross-modulo: C1 lo usa per contatori/consumi del servizio.");

        builder.Property(x => x.PropertyId)
            .HasColumnName("property_id")
            .IsRequired()
            .HasComment("Stabilimento che vende il servizio.");

        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasColumnType("catalog.service_sale_kind")
            .IsRequired()
            .HasComment("Tipo di servizio: bar, normale, a fascia oraria.");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired()
            .HasComment("Nome del servizio mostrato al cliente.");

        builder.Property(x => x.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(12,2)")
            .IsRequired()
            .HasComment("Prezzo unitario del servizio (sovrascrivibile dalla fascia oraria per kind = hourly).");

        builder.Property(x => x.IconId)
            .HasColumnName("icon_id")
            .HasComment("Icona scelta dal catalogo master; alternativa a icon_asset_id.");

        builder.Property(x => x.IconAssetId)
            .HasColumnName("icon_asset_id")
            .HasComment("Icona caricata ad hoc; alternativa a icon_id.");

        builder.Property(x => x.IsPurchasable)
            .HasColumnName("is_purchasable")
            .HasDefaultValue(true)
            .IsRequired()
            .HasComment("Il servizio è acquistabile (vs. solo elencato/disattivato).");

        builder.Property(x => x.ActiveWeekdays)
            .HasColumnName("active_weekdays")
            .HasColumnType("catalog.weekday[]")
            .HasDefaultValueSql("'{}'")
            .IsRequired()
            .HasComment("Giorni della settimana in cui il servizio e' attivo. Sostituisce la tabella figlia a sette righe con flag.");

        builder.Property(x => x.HourFrom)
            .HasColumnName("hour_from")
            .HasComment("Inizio della fascia oraria complessiva di disponibilità (solo kind = hourly).");

        builder.Property(x => x.HourTo)
            .HasColumnName("hour_to")
            .HasComment("Fine della fascia oraria complessiva di disponibilità (solo kind = hourly).");

        builder.Property(x => x.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .HasComment("public_id di identity_access_users (C5), copiato — nessuna FK cross-schema.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione del servizio.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica del servizio.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
            .HasComment("Soft-delete; un servizio con consumi storici (C1) non si cancella fisicamente.");

        builder.HasIndex(x => x.PublicId)
            .IsUnique()
            .HasDatabaseName("uq_catalog_service_sale_items_public_id");

        builder.HasIndex(x => new { x.PropertyId, x.Kind })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_catalog_service_sale_items_property_kind");

        builder.HasIndex(x => new { x.PropertyId, x.Kind })
            .HasFilter("is_purchasable AND deleted_at IS NULL")
            .HasDatabaseName("ix_catalog_service_sale_items_property_purchasable");

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .HasConstraintName("fk_catalog_service_sale_items_property")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ServiceTypeIcon>()
            .WithMany()
            .HasForeignKey(x => x.IconId)
            .HasConstraintName("fk_catalog_service_sale_items_icon")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => x.IconAssetId)
            .HasConstraintName("fk_catalog_service_sale_items_icon_asset")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.TimeSlots)
            .WithOne()
            .HasForeignKey(x => x.SaleItemId)
            .HasConstraintName("fk_catalog_service_sale_item_time_slots_item")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

- `property_id` → FK reale a `catalog_properties.id` (stesso schema), `ON DELETE CASCADE`.
- `icon_id` → FK reale a `catalog_service_type_icons.id` (stesso schema), `ON DELETE SET NULL`.
- `icon_asset_id` → FK reale a `catalog_media_assets.id` (stesso schema), `ON DELETE RESTRICT` (un asset
  in uso da un'icona di servizio non va cancellato senza prima scollegarlo).
- `created_by_user_id` → id copiato da `identity_access_users` (C5), nessuna FK cross-schema.
- `TimeSlots` → FK reale interna (§3), `ON DELETE CASCADE` sulla figlia.

### Note di migrazione

- Esplosione dei 3 array JSON (`barServices`/`normalServices`/`hourlyServices`) in una riga per elemento,
  con `kind` impostato dall'array di provenienza.
- `ServicePrice`/`price` stringa → `numeric(12,2)`; `PurchaseStatus` (0/1) → `IsPurchasable` bool.
- `Servicemage` (URL) → `icon_id` se risolvibile contro il master `catalog_service_type_icons`, altrimenti
  `icon_asset_id` (upload S3 diretto tramite `catalog_media_assets`).
- `currentId` (id client-side casuale) scartato: non è un identificatore stabile.
- `day_constant[]`/`timeslot_constants[]` migrano nelle due tabelle figlie (vedi le rispettive schede).
