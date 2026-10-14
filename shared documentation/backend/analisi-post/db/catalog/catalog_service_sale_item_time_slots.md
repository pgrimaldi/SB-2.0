# Tabella `catalog_service_sale_item_time_slots`

> **Dominio**: C2 — `catalog` · **sottoarea**: `service` · **schema**: `catalog` · **nome flat**:
> `catalog_service_sale_item_time_slots` · **forma canonica**: `catalog.service_sale_item_time_slots`.
>
> Rimpiazza `services_types.hourlyServices[].timeslot_constants` (array `{id, name, status, price}`).
> Fonti: [`analisi-pre/db/services_types.md` (campo `hourlyServices`)](../../../analisi-pre/db/services_types.md),
> [`analisi-pre/db/hourly_service_booking_management.md`](../../../analisi-pre/db/hourly_service_booking_management.md).

---

## Descrizione

Per un servizio a fascia oraria (`kind = hourly`), le **fasce orarie interne** prenotabili, con eventuale
prezzo dedicato (es. slot 9–11 a tariffa diversa da 15–17). Una riga per fascia. Referenziata da C1
(`hourly_service_booking_management` → `resource_consumptions` con `slot_start`/`slot_end`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_service_sale_item_time_slots` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C1: `booking_service_time_slots.time_slot_id`) |
| `ix_..._item` | `(sale_item_id, sort_order) WHERE deleted_at IS NULL` | parziale | elenco fasce di un servizio |


> ### ✅ Applicato nel backend (2026-09-21)
>
> Vincolo **`EXCLUDE USING gist`** di non sovrapposizione ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §8), migration
> `AddNoOverlapExcludeConstraints` + estensione `btree_gist`:
>
> ```sql
> EXCLUDE USING gist (sale_item_id WITH =,
>                     tsrange(DATE '2000-01-01' + slot_start,
>                             DATE '2000-01-01' + slot_end) WITH &&)
> WHERE (deleted_at IS NULL AND slot_start IS NOT NULL AND slot_end IS NOT NULL
>        AND slot_end > slot_start)
> ```
>
> Un `CHECK` garantisce solo che la **singola riga** sia coerente (`ends >= starts`); l'`EXCLUDE` è
> l'unico strumento che vincola **due righe fra loro** — ed è quello che serve qui, perché il problema
> non è una riga sbagliata ma una data che cade in due periodi diversi senza che nessuno sappia quale
> vince. Parziale su `deleted_at IS NULL`: una riga soft-eliminata non deve bloccare quella che la
> sostituisce.
>
> PostgreSQL non ha un range nativo per `time`, quindi gli orari sono ancorati a una data convenzionale
> per ottenere un `tsrange`. Il `WHERE` esclude anche gli slot che attraversano la mezzanotte
> (`slot_end <= slot_start`): per quelli il range sarebbe invalido e l'`INSERT` fallirebbe con un
> errore oscuro invece di restare semplicemente fuori dal vincolo.

## Campi

### id
- **Legacy**: `timeslot_constants[].id` (id locale)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile, **copiato cross-modulo**. C1 lo usa come `time_slot_id` in
  [`booking_service_time_slots`](../booking/booking_service_time_slots.md) per legare lo slot prenotato
  alla fascia di catalogo (prezzo dedicato, capienza) senza affidarsi al match per etichetta oraria.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C1 (`booking_service_time_slots.time_slot_id`).

### sale_item_id
- **Legacy**: elemento di `hourlyServices` (contenitore di `timeslot_constants`)
- **Descrizione**: servizio orario a cui appartiene la fascia.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_service_sale_items.id`](catalog_service_sale_items.md)
  `ON DELETE CASCADE`.

### label
- **Legacy**: `timeslot_constants[].name` (atteso `"9-11"`; nei dati reali è **sempre una durata**:
  `"30 min"`, `"1 hr 30 min"`)
- **Descrizione**: etichetta della fascia mostrata al cliente. Nei dati legacy non è mai un intervallo
  orario: vedi `duration_minutes`.
- **Tipo**: `varchar(40)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### slot_start / slot_end
- **Legacy**: derivati da `timeslot_constants[].name` (nel legacy solo l'etichetta stringa `"9-11"`)
- **Descrizione**: orari di inizio/fine della fascia.
- **Tipo**: `time` ciascuno.
- **Default**: nessuno.
- **Nullabile**: Sì (se non parsabile dall'etichetta legacy → da compilare a mano in bonifica).
- **Relazioni**: usati da C1 per `resource_consumptions.slot_start`/`slot_end`
  ([`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md)).
- **Note**: in migrazione parsing best-effort di `label` (`"9-11"` → `09:00`/`11:00`).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1.661 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### duration_minutes
- **Legacy**: derivato da `timeslot_constants[].name` (`"30 min"` → `30`, `"1 hr 30 min"` → `90`)
- **Descrizione**: durata della fascia in minuti, quando il servizio è venduto **a durata** invece che a
  orario fisso. I dati legacy sono tutti di questa forma (11 valori: 10/15/30/45/60/90/120/180/240/300/360
  minuti), quindi sullo storico migrato è questa la colonna valorizzata e `slot_start`/`slot_end` restano
  `NULL`. Per una fascia a orario assoluto vale il contrario: la durata si calcola e questa resta `NULL`.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-02)**: valorizzata su **tutte le 1.661 righe** (migration
  `AddTimeSlotDurationMinutes`, oggi consolidata nella `InitialCreate` di `Module.Catalog` —
  [`docs/42-migration.md` §10](../../../progettazione/42-migration.md)). È la colonna che ha sciolto il
  «interamente NULL» di `slot_start`/`slot_end` qui sopra: quel parsing non era rinviato, era
  impossibile, perché l'etichetta legacy non contiene orari.

### price
- **Legacy**: `timeslot_constants[].price` (stringa numerica)
- **Descrizione**: prezzo dedicato della fascia; se `NULL`, si usa
  [`catalog_service_sale_items.price`](catalog_service_sale_items.md).
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### is_active
- **Legacy**: `timeslot_constants[].status` (int 0/1)
- **Descrizione**: fascia prenotabile.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### sort_order
- **Legacy**: posizione nell'array `timeslot_constants`
- **Descrizione**: ordine di visualizzazione.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: della riga contenitore
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (modifica = riscrittura array)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON — elimina `timeslot_constants[]`. `name` etichetta → `label` + `slot_start`/`slot_end`
tipizzati; `price` stringa → `numeric`.

## Suggerimenti per le prestazioni

- Poche righe per servizio. La disponibilità residua di uno slot (nel legacy
  `checkHourlyServiceAvailability`) si calcola contro `resource_consumptions` (C1) per (servizio, data,
  slot).

## Implementazione .NET

`ServiceSaleItemTimeSlot` è **entità figlia** di
[`ServiceSaleItem`](catalog_service_sale_items.md) (modulo `Module.Catalog`, area `Service`, schema
`catalog`): non è modellata come owned type puro perché possiede un proprio `public_id` referenziato da
C1 (`booking_service_time_slots.time_slot_id`) — serve identità stabile indipendente dalla riga padre.
Implementa `IAuditable`/`ISoftDeletable`.

### Entità

```csharp
// Domain/Entities/ServiceSaleItemTimeSlot.cs
namespace Module.Catalog.Domain.Entities;

public sealed class ServiceSaleItemTimeSlot : IAuditable, ISoftDeletable
{
    public long Id { get; private set; }
    public Guid PublicId { get; private set; }
    public long SaleItemId { get; private set; }
    public string Label { get; private set; } = null!;
    public TimeOnly? SlotStart { get; private set; }
    public TimeOnly? SlotEnd { get; private set; }
    public short? DurationMinutes { get; private set; }
    public decimal? Price { get; private set; }
    public bool IsActive { get; private set; }
    public short SortOrder { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<ServiceSaleItemTimeSlot>`

```csharp
// Infrastructure/Configurations/ServiceSaleItemTimeSlotConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class ServiceSaleItemTimeSlotConfiguration : IEntityTypeConfiguration<ServiceSaleItemTimeSlot>
{
    public void Configure(EntityTypeBuilder<ServiceSaleItemTimeSlot> builder)
    {
        builder.ToTable("service_sale_item_time_slots", "catalog", t => t.HasComment(
            "Fascia oraria interna prenotabile di un servizio a fascia oraria (kind = hourly), con eventuale prezzo dedicato."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_service_sale_item_time_slots");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.PublicId)
            .HasColumnName("public_id")
            .ValueGeneratedNever()
            .HasComment("Id pubblico, copiato cross-modulo: C1 lo usa come time_slot_id in booking_service_time_slots.");

        builder.Property(x => x.SaleItemId)
            .HasColumnName("sale_item_id")
            .IsRequired()
            .HasComment("Servizio a fascia oraria a cui appartiene la fascia.");

        builder.Property(x => x.Label)
            .HasColumnName("label")
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("Etichetta della fascia mostrata al cliente. Nei dati legacy è sempre una durata (\"30 min\", \"1 hr 30 min\"), mai un intervallo: vedi duration_minutes.");

        builder.Property(x => x.SlotStart)
            .HasColumnName("slot_start")
            .HasComment("Orario di inizio della fascia.");

        builder.Property(x => x.SlotEnd)
            .HasColumnName("slot_end")
            .HasComment("Orario di fine della fascia.");

        builder.Property(x => x.DurationMinutes)
            .HasColumnName("duration_minutes")
            .HasComment("Durata della fascia in minuti, quando il servizio è venduto a durata invece che a orario fisso.");

        builder.Property(x => x.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(12,2)")
            .HasComment("Prezzo dedicato della fascia; se NULL si usa il prezzo del servizio padre.");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired()
            .HasComment("La fascia è prenotabile.");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasComment("Ordine di visualizzazione delle fasce del servizio.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della fascia.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della fascia.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => x.PublicId)
            .IsUnique()
            .HasDatabaseName("uq_catalog_service_sale_item_time_slots_public_id");

        builder.HasIndex(x => new { x.SaleItemId, x.SortOrder })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_catalog_service_sale_item_time_slots_item");

        builder.HasOne<ServiceSaleItem>()
            .WithMany(x => x.TimeSlots)
            .HasForeignKey(x => x.SaleItemId)
            .HasConstraintName("fk_catalog_service_sale_item_time_slots_item")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

`sale_item_id` → FK reale a `catalog_service_sale_items.id` (stesso schema), `ON DELETE CASCADE`;
navigazione inversa `ServiceSaleItem.TimeSlots` configurata lato aggregate (vedi
[`catalog_service_sale_items.md`](catalog_service_sale_items.md)).

### Note di migrazione

- `timeslot_constants[]` (array JSON dentro `hourlyServices[]`) esploso in una riga per elemento.
- `name` → `label`; dall'etichetta si ricava `duration_minutes` (`"1 hr 30 min"` → `90`). Il parsing a
  `slot_start`/`slot_end` (`"9-11"` → `09:00`/`11:00`) previsto in origine non trova casi: nessuna
  etichetta legacy è un intervallo orario, quindi le due colonne restano `NULL` sullo storico.
- `price` (stringa numerica) → `numeric(12,2)`; `status` (0/1) → `IsActive` bool; posizione nell'array →
  `sort_order`; `id` locale scartato (nuova identity).
