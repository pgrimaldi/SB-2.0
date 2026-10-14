# Tabella `booking_spot_holds`

> **Dominio**: C1 — `booking` · **sottoarea**: `hold` · **schema**: `booking` · **nome flat**:
> `booking_spot_holds` · **forma canonica**: `booking.spot_holds`.
>
> Sostituisce [`location_reservations`](../../../analisi-pre/db/location_reservations.md) (blocco morbido
> "riservata" sulla mappa) e [`hold_locations`](../../../analisi-pre/db/hold_locations.md) (blocco
> temporaneo di checkout — feature incompleta, 0 righe). Fonti:
> [`analisi-pre/db/location_reservations.md`](../../../analisi-pre/db/location_reservations.md),
> [`analisi-pre/db/hold_locations.md`](../../../analisi-pre/db/hold_locations.md),
> [`analisi-pre/business-logic/stabilimento/02-planner.md` §2](../../../analisi-pre/business-logic/stabilimento/02-planner.md),
> [`analisi-pre/business-logic/stabilimento/01-mappa.md` §11](../../../analisi-pre/business-logic/stabilimento/01-mappa.md),
> [`docs/03-gestione-dati.md` §3.9](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Blocco di una postazione senza prenotazione** (nessun cliente, nessun pagamento): manutenzione, VIP,
tenere delle postazioni dal planner, oppure blocco temporaneo durante il checkout online. `hold_kind`
distingue i due usi; `held_until` (solo per il checkout) ne fissa la scadenza server-side. Ogni riga
produce anche righe in [`booking_resource_consumptions`](booking_resource_consumptions.md)
(`source_kind = manual_hold`, `state = held`) — così la disponibilità resta **single-source**.

Correzioni rispetto al legacy:

- **`location_reservations`: 1 riga per giorno** (`getAllDatesInRange`), `endDate` sempre = `startDate`
  → **1 riga per intervallo** (`start_date`/`end_date`), l'espansione per-giorno è nelle consumptions.
- **`freeLocationReservation` = `DELETE` fisico senza filtro `propertyId`** (rischio di cancellare la
  riserva di un'altra property con stessa `locationId`) → soft-delete (`released_at`) scoped per
  `property_id` + FK reale alla risorsa.
- **`locationId` stringa** vs `Location` numero/codice (match fragile) → `resource_id uuid` FK.
- **`locationData` (metadati mappa duplicati, ~35 KB per riga, con array-spazzatura `{"locatin":"a"}`)**
  → eliminato (i metadati mappa sono in C2).
- **`propertyId` sporco fino a `99999`, `createdAt` `0000-00-00`** → bonifica in migrazione.
- **`hold_locations`: `hold_until` e `status` fidati dal client, nessun cron di scadenza** → `held_until`
  calcolato server-side + job di scadenza (condiviso con `booking_resource_consumptions`).
- **nessuno stato/scadenza** → `hold_status` esplicito.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_spot_holds` | `id` | PK | — |
| `ix_..._property_resource_dates` | `(property_id, resource_id, start_date, end_date) WHERE hold_status = 'active'` | parziale | "questa postazione è riservata in queste date?" |
| `ix_..._expiry` | `(held_until) WHERE hold_status = 'active' AND held_until IS NOT NULL` | parziale | job di scadenza dei blocchi di checkout |
| `ix_..._property_active` | `(property_id) WHERE hold_status = 'active' AND deleted_at IS NULL` | parziale | elenco riserve attive di uno stabilimento |

## Campi

### id
- **Legacy**: `location_reservations.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `booking_resource_consumptions.source_id` quando
  `source_kind = manual_hold`.

### property_id
- **Legacy**: `location_reservations.propertyId` (`int`, sporco fino a `99999`)
- **Descrizione**: stabilimento.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; **sempre dal token**.

### resource_id
- **Legacy**: `location_reservations.locationId` (`varchar`, match per stringa con `Location`)
- **Descrizione**: postazione bloccata.
- **Tipo**: `uuid` (= `public_id` di `catalog_map_spots`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_map_spots`](../catalog/catalog_map_spots.md).

### spot_label_snapshot
- **Legacy**: `locationId` (valore mostrato)
- **Descrizione**: etichetta della postazione congelata.
- **Tipo**: `varchar(40)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: snapshot.

### hold_kind
- **Legacy**: `location_reservations` (riserva operativa) vs `hold_locations` (checkout)
- **Descrizione**: motivo del blocco.
- **Tipo**: `enum booking.hold_kind` — valori: `operational` (manutenzione/VIP/planner — ex
  `location_reservations`), `checkout` (blocco temporaneo durante il pagamento online — ex
  `hold_locations`).
- **Default**: `operational`.
- **Nullabile**: No.
- **Relazioni**: `checkout` ⇒ `held_until` valorizzato.

### hold_status
- **Legacy**: — (implicito: la riga esiste o è stata `DELETE`)
- **Descrizione**: stato del blocco.
- **Tipo**: `enum booking.hold_status` — valori: `active`, `released` (liberato manualmente),
  `expired` (scaduto — solo `checkout`), `converted` (il checkout è diventato prenotazione).
- **Default**: `active`.
- **Nullabile**: No.
- **Relazioni**: le viste di disponibilità/mappa considerano solo `active`.

### start_date / end_date
- **Legacy**: `location_reservations.startDate` / `endDate` (1 riga = 1 giorno)
- **Descrizione**: intervallo bloccato (estremi inclusi).
- **Tipo**: `date` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `end_date >= start_date`; l'espansione per-giorno è in `booking_resource_consumptions`.

### booking_period
- **Legacy**: — (`location_reservations` non aveva la fascia)
- **Descrizione**: fascia bloccata.
- **Tipo**: `enum booking.day_period`.
- **Default**: `full_day`.
- **Nullabile**: No.
- **Relazioni**: —.

### held_until
- **Legacy**: `hold_locations.hold_until` (dal client, mai usato)
- **Descrizione**: scadenza del blocco di checkout. `NULL` per `operational`.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `now() + Booking:CheckoutHoldTtl` (configurazione), mai dal body; job di scadenza →
  `hold_status = expired` + consumptions → `released`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 15.311 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### reason_note
- **Legacy**: `reservedObject` (piccolo JSON) — di fatto solo stato
- **Descrizione**: nota libera opzionale (motivo della riserva operativa).
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 15.311 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_by_user_id
- **Legacy**: `location_reservations.agent` (`varchar` id, obbligatorio in create)
- **Descrizione**: operatore che ha creato il blocco.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì (blocco di checkout creato dal flusso online, senza operatore).
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md); dal token.

### released_at / released_by_user_id
- **Legacy**: `freeLocationReservation` = `DELETE` fisico
- **Descrizione**: quando/da chi il blocco è stato liberato (soft-release, per audit).
- **Tipo**: `timestamptz` / `uuid`.
- **Default**: `NULL` / `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: `released_by_user_id` = `public_id` copiato da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 15.311 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### converted_reservation_id
- **Legacy**: — (il legacy non collegava hold e prenotazione)
- **Descrizione**: prenotazione nata da questo blocco di checkout (`hold_status = converted`).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE SET NULL`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 15.311 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `created_at`/`updated_at` + `createdAt`/`updatedAt` (`0000-00-00`) — quadrupli
- **Descrizione**: creazione / modifica / soft-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. `locationData` (metadati mappa duplicati, con array-spazzatura) e `reservedObject` →
eliminati: la postazione è una FK, lo stato è `hold_status`.

## Suggerimenti per le prestazioni

- 1 riga per intervallo invece di N per giorno: l'espansione la fa `booking_resource_consumptions`.
- Indice parziale `WHERE hold_status = 'active'` per la mappa; `WHERE held_until IS NOT NULL` per il
  job di scadenza.
- Per la disponibilità la mappa legge `booking_resource_availability_daily` (che include gli `held`),
  non questa tabella.

## Implementazione .NET

`SpotHold` è un **aggregate root** dell'area `Hold` del modulo `Module.Booking`, schema `booking`.
Implementa `ISoftDeletable`/`IAuditable`. Ogni creazione/rilascio sincronizza righe "gemelle" in
[`booking_resource_consumptions`](booking_resource_consumptions.md) (`source_kind = manual_hold`) —
mantenute allineate dal comando applicativo, non da un trigger DB.

```csharp
// Domain/Enums/HoldKind.cs
public enum HoldKind { Operational = 0, Checkout = 1 }

// Domain/Enums/HoldStatus.cs
public enum HoldStatus { Active = 0, Released = 1, Expired = 2, Converted = 3 }
```

`DayPeriod` (`booking_period`) è un **enum condiviso**, definito in
[`booking_lines.md`](booking_lines.md) — non ridefinito qui.

```csharp
// <Modulo>DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<HoldKind>("booking", "hold_kind");
modelBuilder.HasPostgresEnum<HoldStatus>("booking", "hold_status");
```

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class SpotHoldConfiguration : IEntityTypeConfiguration<SpotHold>
{
	public void Configure(EntityTypeBuilder<SpotHold> builder)
	{
		builder.ToTable("booking_spot_holds", "booking", t => t.HasComment(
			"Blocco di una postazione senza prenotazione (manutenzione, VIP, planner, o blocco " +
			"temporaneo di checkout online); nessun cliente, nessun pagamento."));

		builder.HasKey(x => x.Id).HasName("pk_booking_spot_holds");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dal token — nessuna FK cross-schema");

		builder.Property(x => x.ResourceId)
			.HasColumnName("resource_id")
			.IsRequired()
			.HasComment("public_id di catalog.map_spots (postazione bloccata), copiato — nessuna FK cross-schema");

		builder.Property(x => x.SpotLabelSnapshot)
			.HasColumnName("spot_label_snapshot")
			.HasMaxLength(40)
			.HasComment("Etichetta della postazione congelata al momento del blocco");

		builder.Property(x => x.HoldKind)
			.HasColumnName("hold_kind")
			.HasColumnType("booking.hold_kind")
			.HasDefaultValue(HoldKind.Operational)
			.IsRequired()
			.HasComment("Motivo del blocco: operativo (manutenzione/VIP/planner) o checkout temporaneo durante il pagamento online");

		builder.Property(x => x.HoldStatus)
			.HasColumnName("hold_status")
			.HasColumnType("booking.hold_status")
			.HasDefaultValue(HoldStatus.Active)
			.IsRequired()
			.HasComment("Stato del blocco: attivo, liberato manualmente, scaduto, convertito in prenotazione");

		builder.Property(x => x.StartDate)
			.HasColumnName("start_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Inizio dell'intervallo bloccato (estremi inclusi)");

		builder.Property(x => x.EndDate)
			.HasColumnName("end_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Fine dell'intervallo bloccato (estremi inclusi); l'espansione per-giorno è in booking_resource_consumptions");

		builder.Property(x => x.BookingPeriod)
			.HasColumnName("booking_period")
			.HasColumnType("booking.day_period")
			.HasDefaultValue(DayPeriod.FullDay)
			.IsRequired()
			.HasComment("Fascia bloccata");

		builder.Property(x => x.HeldUntil)
			.HasColumnName("held_until")
			.HasComment("Scadenza del blocco di checkout (now() + Booking:CheckoutHoldTtl, calcolato server-side); NULL per hold operational");

		builder.Property(x => x.ReasonNote)
			.HasColumnName("reason_note")
			.HasMaxLength(255)
			.HasComment("Nota libera opzionale, motivo della riserva operativa");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users (operatore che ha creato il blocco, dal token), copiato — nessuna FK cross-schema; NULL per hold di checkout creati dal flusso online");

		builder.Property(x => x.ReleasedAt)
			.HasColumnName("released_at")
			.HasComment("Data/ora di rilascio del blocco (soft-release, per audit)");

		builder.Property(x => x.ReleasedByUserId)
			.HasColumnName("released_by_user_id")
			.HasComment("public_id di identity.access_users (operatore che ha liberato il blocco), copiato — nessuna FK cross-schema");

		builder.Property(x => x.ConvertedReservationId)
			.HasColumnName("converted_reservation_id")
			.HasComment("Prenotazione nata da questo blocco di checkout (hold_status = converted)");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione del blocco");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica del blocco");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete del blocco");

		builder.HasIndex(x => new { x.PropertyId, x.ResourceId, x.StartDate, x.EndDate })
			.HasFilter("hold_status = 'active'")
			.HasDatabaseName("ix_booking_spot_holds_property_resource_dates");

		builder.HasIndex(x => x.HeldUntil)
			.HasFilter("hold_status = 'active' AND held_until IS NOT NULL")
			.HasDatabaseName("ix_booking_spot_holds_expiry");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("hold_status = 'active' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_spot_holds_property_active");

		builder.HasOne(x => x.ConvertedReservation)
			.WithMany()
			.HasForeignKey(x => x.ConvertedReservationId)
			.HasConstraintName("fk_booking_spot_holds_converted_reservation")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

**Note di migrazione**:

- `location_reservations` (1 riga per giorno, `getAllDatesInRange`) raggruppate per
  `(propertyId, locationId, intervallo contiguo)` → 1 riga `hold_kind = Operational` con
  `start_date`/`end_date` dell'intervallo risultante.
- `hold_locations` (feature checkout mai completata, 0 righe in prod): nulla da migrare, ma lo schema
  (`hold_kind = Checkout`, `held_until`) è pronto per la nuova implementazione server-side.
- `freeLocationReservation` era una `DELETE` fisica **senza filtro `propertyId`** (rischio di colpire la
  riserva di un'altra property con lo stesso `locationId`): non c'è nulla da recuperare per le righe già
  cancellate; da qui in avanti il rilascio è sempre soft (`released_at`) e scoped per `property_id`.
- `locationId` (stringa, match fragile con `Location`) → risolto a `resource_id` (`public_id` di
  `catalog_map_spots`) con lo stesso mapping usato per le altre tabelle C1.
- `locationData` (~35 KB di metadati mappa duplicati per riga, con array-spazzatura tipo
  `{"locatin":"a"}`) e `reservedObject` (JSON che di fatto conteneva solo lo stato) → **eliminati**, non
  migrati: i metadati mappa restano in C2, lo stato è `hold_status`.
- `propertyId` sporchi (fino a `99999`) e `createdAt`/`updatedAt` a `0000-00-00` → righe bonificate o
  scartate secondo le regole generali di bonifica del dominio C1.
- `agent` (varchar id operatore) → risolto a `created_by_user_id` (`public_id` di `identity_access_users`)
  tramite il mapping utenti già usato dalle altre schede C1.
