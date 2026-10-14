# Tabella `booking_service_time_slots`

> **Dominio**: C1 — `booking` · **sottoarea**: `service` · **schema**: `booking` · **nome flat**:
> `booking_service_time_slots` · **forma canonica**: `booking.service_time_slots`.
>
> Sostituisce [`hourly_service_booking_management`](../../../analisi-pre/db/hourly_service_booking_management.md)
> (parte "quale slot, quale giorno"). Fonti:
> [`analisi-pre/db/hourly_service_booking_management.md`](../../../analisi-pre/db/hourly_service_booking_management.md),
> [`analisi-pre/db/service_reservation.md` §`servicesData`](../../../analisi-pre/db/service_reservation.md),
> [`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga per (riga servizio orario × slot × giorno)** prenotato. È il dettaglio "d'ordine" degli slot
di un servizio `kind = hourly` (per il riepilogo prenotazione, lo scontrino e la resa in UI);
l'**occupazione** ai fini della disponibilità è comunque una riga di
[`booking_resource_consumptions`](booking_resource_consumptions.md) (`resource_type = hourly_service`,
`slot_*` valorizzati). Le due tabelle sono coerenti (stesso comando le scrive insieme), ma la query di
disponibilità legge sempre `booking_resource_consumptions` / `_availability_daily`.

Correzioni rispetto al legacy:

- **`booking_slot_start_time` / `_end_time` `varchar` `HH:MM`, con `:`/`.` misti (`"07.20"`)** →
  `slot_start`/`slot_end` `time`.
- **`booking_date` con ora fittizia `10:00`** → `service_date date`.
- **`hourly_service_name` stringa libera, nessuna FK** → `time_slot_id uuid` (C2) + il servizio è sulla
  `booking_service_lines`.
- **`user_id` = operatore (nome fuorviante)** → non ripetuto qui (è sulla testata,
  `booking_reservations.operator_user_id`).
- **update = destroy + recreate per `bookingNumber`** → soft-delete + insert nel comando di modifica.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_service_time_slots` | `id` | PK | — |
| `ix_..._service_line` | `(service_line_id) WHERE deleted_at IS NULL` | parziale | slot di una riga servizio |
| `ix_..._property_slot_date` | `(property_id, time_slot_id, service_date) WHERE deleted_at IS NULL` | parziale | occupazione di uno slot in una data |

## Campi

### id
- **Legacy**: `hourly_service_booking_management.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### service_line_id
- **Legacy**: legame implicito via `bookingNumber` + `hourly_service_name`
- **Descrizione**: riga servizio orario a cui lo slot appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_service_lines.id`](booking_service_lines.md) `ON DELETE CASCADE`.

### reservation_id
- **Legacy**: `hourly_service_booking_management.bookingNumber` / `booking_id`
- **Descrizione**: prenotazione (denormalizzato per query dirette).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### property_id
- **Legacy**: `hourly_service_booking_management.property_id`
- **Descrizione**: stabilimento (denormalizzato).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dalla testata.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2.

### time_slot_id
- **Legacy**: — (il legacy non legava lo slot al catalogo)
- **Descrizione**: fascia di catalogo prenotata (prezzo dedicato / capienza).
- **Tipo**: `uuid` (= `public_id` di `catalog_service_sale_item_time_slots`).
- **Default**: nessuno.
- **Nullabile**: Sì (slot manuale non a catalogo — `manual_timeslot` legacy).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_service_sale_item_time_slots`](../catalog/catalog_service_sale_item_time_slots.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 559 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### slot_start / slot_end
- **Legacy**: `booking_slot_start_time` / `booking_slot_end_time` (`varchar` `HH:MM`, formati misti)
- **Descrizione**: orari di inizio/fine dello slot **congelati**.
- **Tipo**: `time` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `slot_end > slot_start` (`CHECK`).

### slot_label_snapshot
- **Legacy**: `timeslot_constants[].name` (`"9-11"`)
- **Descrizione**: etichetta dello slot congelata.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: snapshot.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 559 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### service_date
- **Legacy**: `booking_date` (`datetime` con ora fittizia `10:00`)
- **Descrizione**: giorno dello slot.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### seats
- **Legacy**: `booked_count` (`int`, default `1`; in prod 96% = `1`)
- **Descrizione**: posti/unità prenotati per lo slot.
- **Tipo**: `smallint`.
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (seats > 0)`; alimenta `booking_resource_consumptions.quantity`.

### slot_price
- **Legacy**: `HourServiceData[].price` / `timeslot_constants[].price`
- **Descrizione**: prezzo dello slot **congelato** alla conferma (se lo slot ha tariffa dedicata;
  altrimenti quello del servizio). Numero del documento-prenotazione → nessun suffisso `_snapshot`
  (a differenza di `slot_label_snapshot`, che è l'etichetta copiata dal catalogo).
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 559 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` / destroy fisico
- **Descrizione**: creazione / modifica / soft-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Tabella di nicchia (~2.400 righe storiche, pochi stabilimenti). Indici essenziali.
- La verifica di sovrapposizione/capienza slot (nel legacy solo lato client) diventa server-side su
  `booking_resource_consumptions` (con `slot_*`) / `booking_resource_availability_daily`.

## Implementazione .NET

`BookingServiceTimeSlot` è **entità figlia** dell'aggregato `Reservation` (modulo `Module.Booking`,
schema `booking`, sottoarea `service`): dettaglio "d'ordine" di uno slot orario prenotato, sempre
sotto una [`booking_service_lines`](booking_service_lines.md) con `kind = Hourly`. Sempre
creata/modificata tramite l'aggregate root (`Reservation.AddServiceTimeSlot(...)` su una
`BookingServiceLine` esistente), mai persistita in isolamento. L'**occupazione** ai fini della
disponibilità resta una riga separata di `booking_resource_consumptions` (fuori da questo batch),
scritta dallo stesso comando applicativo.

Nessun enum proprio introdotto da questa tabella.

### `BookingServiceTimeSlotConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingServiceTimeSlotConfiguration : IEntityTypeConfiguration<BookingServiceTimeSlot>
{
	public void Configure(EntityTypeBuilder<BookingServiceTimeSlot> builder)
	{
		builder.ToTable("booking_service_time_slots", "booking", t =>
		{
			t.HasComment("Slot orario prenotato per una riga servizio hourly: giorno, fascia e posti.");
			t.HasCheckConstraint("ck_booking_service_time_slots_range", "slot_end > slot_start");
			t.HasCheckConstraint("ck_booking_service_time_slots_seats", "seats > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_service_time_slots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ServiceLineId)
			.HasColumnName("service_line_id")
			.IsRequired()
			.HasComment("Riga servizio orario a cui lo slot appartiene.");

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione, denormalizzata dalla riga servizio per query dirette.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.TimeSlotId)
			.HasColumnName("time_slot_id")
			.HasComment("public_id di catalog.service_sale_item_time_slots, copiato — NULL per slot manuale non a catalogo.");

		builder.Property(x => x.SlotStart)
			.HasColumnName("slot_start")
			.HasColumnType("time")
			.IsRequired()
			.HasComment("Orario di inizio dello slot, congelato al momento della prenotazione.");

		builder.Property(x => x.SlotEnd)
			.HasColumnName("slot_end")
			.HasColumnType("time")
			.IsRequired()
			.HasComment("Orario di fine dello slot, congelato al momento della prenotazione.");

		builder.Property(x => x.SlotLabelSnapshot)
			.HasColumnName("slot_label_snapshot")
			.HasMaxLength(40)
			.HasComment("Etichetta dello slot congelata (es. \"9-11\").");

		builder.Property(x => x.ServiceDate)
			.HasColumnName("service_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno dello slot.");

		builder.Property(x => x.Seats)
			.HasColumnName("seats")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Posti/unità prenotati per lo slot; alimenta il consumo di booking_resource_consumptions.");

		builder.Property(x => x.SlotPrice)
			.HasColumnName("slot_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo dello slot congelato alla conferma, se ha tariffa dedicata; altrimenti quello del servizio.");

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
			.HasComment("Soft-delete dello slot.");

		builder.HasIndex(x => x.ServiceLineId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_time_slots_service_line");

		builder.HasIndex(x => new { x.PropertyId, x.TimeSlotId, x.ServiceDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_time_slots_property_slot_date");

		builder.HasOne(x => x.ServiceLine)
			.WithMany(l => l.TimeSlots)
			.HasForeignKey(x => x.ServiceLineId)
			.HasConstraintName("fk_booking_service_time_slots_service_line")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Reservation)
			.WithMany()
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_service_time_slots_reservation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`BookingServiceTimeSlot` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable`
(`created_at`/`updated_at`). `property_id` e `time_slot_id` sono `public_id` copiati da C2 (solo
`HasComment`, nessuna FK cross-schema); le FK EF reali sono `service_line_id` →
`booking_service_lines` e `reservation_id` → `booking_reservations`, entrambe `Cascade` (stesso schema
`booking`).

### Note di migrazione

- `hourly_service_booking_management` → una riga per (riga servizio orario × slot × giorno); il legame
  legacy era implicito via `bookingNumber` + `hourly_service_name` (stringa), qui diventa FK esplicita
  `service_line_id` verso la `booking_service_lines` corrispondente.
- `booking_slot_start_time`/`_end_time` (`varchar` `HH:MM`, formati misti `:`/`.`) → parse tollerante
  (`.` → `:`) in `slot_start`/`slot_end` `time`.
- `booking_date` (`datetime` con ora fittizia `10:00`) → `service_date date` puro.
- `hourly_service_name` (stringa libera, nessuna FK) → risolto verso
  `catalog_service_sale_item_time_slots` per popolare `time_slot_id`, con `slot_label_snapshot`
  congelato dal valore legacy.
- **Update legacy = destroy + recreate** per lo stesso `bookingNumber` → nel nuovo comando di modifica
  diventa soft-delete della riga esistente + insert della nuova, coerente con l'audit trail.
