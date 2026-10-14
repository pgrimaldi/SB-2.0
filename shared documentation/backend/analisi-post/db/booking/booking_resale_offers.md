# Tabella `booking_resale_offers`

> **Dominio**: C1 — `booking` · **sottoarea**: `hold` (rivendita) · **schema**: `booking` · **nome flat**:
> `booking_resale_offers` · **forma canonica**: `booking.resale_offers`.
>
> Unifica [`booking_absence_details`](../../../analisi-pre/db/booking_absence_details.md) ("assenza
> pianificata / rivendita del posto") e la parte "offerta" di
> [`sharing_details`](../../../analisi-pre/db/sharing_details.md) ("sharing" con credito al titolare) —
> due modellazioni dello **stesso concetto** ("(postazione, giorno) → torna disponibile"), collegate nel
> legacy dal ponte fragile `sharing_details.is_sharing_coming_from_absence`. La legacy
> [`booking_absences`](../../../analisi-pre/db/booking_absences.md) (dismessa, ferma al 2024) **non si
> migra**. Fonti:
> [`analisi-pre/db/booking_absence_details.md`](../../../analisi-pre/db/booking_absence_details.md),
> [`analisi-pre/db/sharing_details.md`](../../../analisi-pre/db/sharing_details.md),
> [`docs/03-gestione-dati.md` §2.4bis, §3.8](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga per (riga prenotazione di origine × postazione × giorno ceduto)**. Il titolare di un
abbonamento/prenotazione lunga dichiara che **non userà** la postazione in certi giorni; quel giorno
torna prenotabile. Se lo stabilimento gestisce la feature come "sharing"
(`booking_resale_settings.mode = sharing`), la ripresa di quel giorno da parte di un altro cliente
matura un **credito** per il titolare (→ [`booking_resale_credit_ledger`](booking_resale_credit_ledger.md)).

`offer_state` (`available` / `reassigned`) sostituisce la **doppia rappresentazione** legacy `status`
(2/1) + `isBooked` (0/1) — in produzione **disallineati su 33 righe reali**. La riga di **consumo** del
giorno ripreso è in [`booking_resource_consumptions`](booking_resource_consumptions.md)
(`source_kind = resale`), non qui.

Correzioni rispetto al legacy:

- **due tabelle** (`booking_absence_details`, `sharing_details`) + un JSON (`bookings.booking_absence_dates`)
  → una tabella con `origin` (`absence` / `sharing`).
- **`status` (2 = disponibile — controintuitivo) + `isBooked` (ridondante)** → `offer_state` unico.
- **`location` stringa (match fragile numeri vs codici)** → `resource_id uuid` FK.
- **`book_period_allowed` `varchar` (con logica half-day che azzera a `NULL`)** → `enum day_period` +
  `remaining_period` per il caso "ceduto FullDay, ripreso Morning → resta Afternoon".
- **`booked_by` = `bookings.id` della nuova prenotazione** → `taken_by_reservation_id` FK interna.
- **`credit_value` intero con parsing regex (`replace(/€/g,"")`), `number_of_credit` int** → spostati nel
  ledger crediti (`numeric(12,2)`).
- **`email_sent` (flag invio email "credito guadagnato")** → **C6** (log notifiche); non qui.
- **`createSharing` = `destroy` + bulk `INSERT` non transazionale, endpoint senza auth** → comando di
  dominio transazionale.
- **`sharing_details.sharings` gemella morta** → ignorata.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_resale_offers` | `id` | PK | — |
| `ix_..._availability` | `(property_id, resource_id, offer_date) WHERE offer_state = 'available' AND deleted_at IS NULL` | parziale | "questa postazione è ceduta e libera il giorno D?" |
| `ix_..._origin_line` | `(booking_line_id) WHERE deleted_at IS NULL` | parziale | offerte di una riga prenotazione |
| `ix_..._taken_by` | `(taken_by_reservation_id) WHERE taken_by_reservation_id IS NOT NULL` | parziale | "quali giorni ceduti ha ripreso la prenotazione X" |
| `ix_..._credit_pending` | `(property_id) WHERE offer_state = 'reassigned' AND credit_settled = false AND origin = 'sharing' AND deleted_at IS NULL` | parziale | giorni ripresi da accreditare (ex cron `sharingCreditCron`) |
| `ex_booking_resale_offers_no_overlap` | `(booking_line_id, offer_date, int4range(fascia)) WHERE deleted_at IS NULL AND offer_state <> 'withdrawn' AND booking_line_id IS NOT NULL` | GiST (`EXCLUDE`) | una sola offerta per riga, giorno e fascia (riquadro sotto) |


> ### ✅ Vincolo di non sovrapposizione applicato (2026-09-30) — chiave sulla riga, non sulla risorsa
>
> In vigore sul DB (migration `ExtendPartitionsAndResaleOfferExclude`, oggi consolidata nella
> `InitialCreate` di `Module.Booking` — [`docs/42-migration.md` §10](../../../progettazione/42-migration.md)),
> estensione `btree_gist`:
>
> ```sql
> CONSTRAINT ex_booking_resale_offers_no_overlap
>   EXCLUDE USING gist (
>     booking_line_id WITH =, offer_date WITH =,
>     int4range(CASE offered_period WHEN 'afternoon' THEN 2 ELSE 1 END,
>               CASE offered_period WHEN 'morning'   THEN 2 ELSE 3 END) WITH &&)
>   WHERE (deleted_at IS NULL AND offer_state <> 'withdrawn' AND booking_line_id IS NOT NULL)
> ```
>
> **Perché `booking_line_id` e non `resource_id`** come in
> [`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §8. Con la chiave sulla risorsa
> lo violavano 294 coppie (17 con entrambe le offerte `reassigned`, che sembravano lo stesso posto
> venduto due volte). Nessuna condivide però la `booking_line_id`: una risorsa è l'ombrellone, e sotto
> un ombrellone ci sono più righe di prenotazione (i lettini), ognuna rilasciabile per conto proprio da
> una prenotazione diversa. Il committente ha confermato il 2026-09-30 che è legittimo (mezza giornata o
> sharing). Con la chiave sulla riga le violazioni sono **zero, senza bonificare nulla**: la chiave era
> sulla colonna sbagliata, non i dati sbagliati.
>
> Il resto del disegno resta: fasce della giornata come `int4range` (mattina `[1,2)`, pomeriggio `[2,3)`,
> giornata intera `[1,3)`), offerte `withdrawn` escluse, `reassigned` incluse.
>
> ⚠️ **Copertura parziale, dichiarata**: 926 offerte su 12.900 non hanno `booking_line_id`, e in un
> `EXCLUDE` una colonna `NULL` non conflitta con niente. Un `COALESCE` su un valore fittizio le farebbe
> conflittare **fra loro**, che è peggio; restano quindi fuori dal vincolo, che protegge le 11.974 con la
> riga valorizzata.

## Campi

### id
- **Legacy**: `booking_absence_details.id` / `sharing_details.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `booking_resource_consumptions.source_id` (`source_kind = resale`)
  e da `booking_resale_credit_ledger.resale_offer_id`.

### property_id
- **Legacy**: `booking_absence_details.property_id` / `sharing_details.property_id` (con placeholder `9999`)
- **Descrizione**: stabilimento.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2; placeholder bonificati.

### origin
- **Legacy**: `booking_absence_details` vs `sharing_details` (+ `is_sharing_coming_from_absence` come ponte)
- **Descrizione**: come è nata l'offerta.
- **Tipo**: `enum booking.resale_origin` — valori: `absence` (assenza pianificata), `sharing`
  (condivisione con credito), `absence_as_sharing` (assenza gestita come sharing —
  `plan_absence_as_sharing` legacy).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: guida se si genera credito (`sharing` / `absence_as_sharing` → sì).

### source_reservation_id
- **Legacy**: `booking_absence_details.booking_id` / `sharing_details.booking_id` (prenotazione di origine)
- **Descrizione**: prenotazione del **titolare** che cede il giorno.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### booking_line_id
- **Legacy**: `location` + `bookingNumber` (match applicativo)
- **Descrizione**: riga postazione ceduta.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (offerta a livello prenotazione, raro).
- **Relazioni**: **FK** → [`booking_lines.id`](booking_lines.md) `ON DELETE CASCADE`.

### resource_id
- **Legacy**: `location` (stringa)
- **Descrizione**: postazione ceduta.
- **Tipo**: `uuid` (= `public_id` di `catalog_map_spots`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_map_spots`](../catalog/catalog_map_spots.md).

### offer_date
- **Legacy**: `absence_date` / `sharing_date` (`datetime` a mezzanotte, con offset di fuso)
- **Descrizione**: il giorno ceduto.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### offered_period
- **Legacy**: `book_period_allowed` (in `createSharing` = `booking_period` della prenotazione di origine)
- **Descrizione**: fascia ceduta.
- **Tipo**: `enum booking.day_period` — valori: `full_day`, `morning`, `afternoon`.
- **Default**: `full_day`.
- **Nullabile**: No.
- **Relazioni**: —.

### remaining_period
- **Legacy**: logica in `updateAbsenceDetails` (FullDay ceduto, Morning ripreso → riga passa ad Afternoon)
- **Descrizione**: fascia **ancora disponibile** dopo una ripresa parziale. `NULL` quando l'offerta è
  completamente ripresa o quando `offered_period` non è `full_day`.
- **Tipo**: `enum booking.day_period`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 12.900 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### offer_state
- **Legacy**: `status` (2 = disponibile / 1 = ripreso) **e** `isBooked` (0/1) — disallineati in prod
- **Descrizione**: stato dell'offerta.
- **Tipo**: `enum booking.resale_offer_state` — valori: `available` (giorno ceduto, ancora libero — ex
  `status 2` / `isBooked 0`), `reassigned` (ripreso da un'altra prenotazione — ex `status 1` /
  `isBooked 1`), `withdrawn` (il titolare ha ritirato la cessione prima che venisse ripresa).
- **Default**: `available`.
- **Nullabile**: No.
- **Relazioni**: `reassigned` ⇒ `taken_by_reservation_id` valorizzato.

### taken_by_reservation_id
- **Legacy**: `booked_by` (`bookings.id` della nuova prenotazione)
- **Descrizione**: prenotazione che ha **ripreso** il giorno ceduto.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE SET NULL`.

### taken_at
- **Legacy**: `updatedAt` alla ripresa
- **Descrizione**: quando il giorno è stato ripreso.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### credit_settled
- **Legacy**: `sharing_details.isApplied` + `email_sent` (nel cron)
- **Descrizione**: `true` se il credito per questa ripresa è stato accreditato al titolare
  (→ [`booking_resale_credit_ledger`](booking_resale_credit_ledger.md)). Solo per `origin` di tipo
  sharing.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: sostituisce l'assegnazione (non incremento) legacy su `users.total_number_of_credit`.

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` / destroy fisico
- **Descrizione**: creazione / modifica / soft-delete (offerta rimossa).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. Le due tabelle legacy + il JSON `bookings.booking_absence_dates` → righe di questa tabella;
il consumo del giorno ripreso → [`booking_resource_consumptions`](booking_resource_consumptions.md); i
crediti → [`booking_resale_credit_ledger`](booking_resale_credit_ledger.md); l'invio email "credito
guadagnato" → C6.

## Suggerimenti per le prestazioni

- Un solo `offer_state` (niente `status`+`isBooked` da tenere allineati — nel legacy 33 righe reali
  incoerenti).
- L'indice `(property_id, resource_id, offer_date) WHERE offer_state = 'available'` serve la mappa
  ("cella libera perché ceduta"); l'indice `credit_pending` sostituisce la scansione del cron
  `sharingCreditCron`.
- La disponibilità effettiva del giorno ceduto passa comunque da `booking_resource_availability_daily`
  (una cessione riduce il `booked_qty` del titolare per quel giorno).

## Implementazione .NET

`ResaleOffer` è un **aggregate root** dell'area `Resale` del modulo `Module.Booking`, schema `booking`
(referenziata da [`booking_resource_consumptions`](booking_resource_consumptions.md) con
`source_kind = resale` e da [`booking_resale_credit_ledger`](booking_resale_credit_ledger.md), che restano
aggregate separati collegati per id). Implementa `ISoftDeletable`/`IAuditable`.

```csharp
// Domain/Enums/ResaleOrigin.cs
public enum ResaleOrigin { Absence = 0, Sharing = 1, AbsenceAsSharing = 2 }

// Domain/Enums/ResaleOfferState.cs
public enum ResaleOfferState { Available = 0, Reassigned = 1, Withdrawn = 2 }
```

`DayPeriod` (usato da `offered_period`/`remaining_period`) è un **enum condiviso**, definito in
[`booking_lines.md`](booking_lines.md) — non ridefinito qui.

```csharp
// <Modulo>DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ResaleOrigin>("booking", "resale_origin");
modelBuilder.HasPostgresEnum<ResaleOfferState>("booking", "resale_offer_state");
```

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResaleOfferConfiguration : IEntityTypeConfiguration<ResaleOffer>
{
	public void Configure(EntityTypeBuilder<ResaleOffer> builder)
	{
		builder.ToTable("booking_resale_offers", "booking", t => t.HasComment(
			"Riga (riga prenotazione di origine × postazione × giorno ceduto): il titolare dichiara che " +
			"non userà la postazione quel giorno, che torna prenotabile (assenza pianificata o sharing)."));

		builder.HasKey(x => x.Id).HasName("pk_booking_resale_offers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("booking.resale_origin")
			.IsRequired()
			.HasComment("Come è nata l'offerta: assenza pianificata, sharing con credito, o assenza gestita come sharing");

		builder.Property(x => x.SourceReservationId)
			.HasColumnName("source_reservation_id")
			.IsRequired()
			.HasComment("Prenotazione del titolare che cede il giorno");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga postazione ceduta (nullable: offerta a livello prenotazione, raro)");

		builder.Property(x => x.ResourceId)
			.HasColumnName("resource_id")
			.IsRequired()
			.HasComment("public_id di catalog.map_spots (postazione ceduta), copiato — nessuna FK cross-schema");

		builder.Property(x => x.OfferDate)
			.HasColumnName("offer_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno ceduto");

		builder.Property(x => x.OfferedPeriod)
			.HasColumnName("offered_period")
			.HasColumnType("booking.day_period")
			.HasDefaultValue(DayPeriod.FullDay)
			.IsRequired()
			.HasComment("Fascia ceduta (full day, mattina, pomeriggio)");

		builder.Property(x => x.RemainingPeriod)
			.HasColumnName("remaining_period")
			.HasColumnType("booking.day_period")
			.HasComment("Fascia ancora disponibile dopo una ripresa parziale; NULL se completamente ripresa o se offered_period non è full_day");

		builder.Property(x => x.OfferState)
			.HasColumnName("offer_state")
			.HasColumnType("booking.resale_offer_state")
			.HasDefaultValue(ResaleOfferState.Available)
			.IsRequired()
			.HasComment("Stato dell'offerta: disponibile, ripreso da un'altra prenotazione, ritirato dal titolare");

		builder.Property(x => x.TakenByReservationId)
			.HasColumnName("taken_by_reservation_id")
			.HasComment("Prenotazione che ha ripreso il giorno ceduto (valorizzato quando offer_state = reassigned)");

		builder.Property(x => x.TakenAt)
			.HasColumnName("taken_at")
			.HasComment("Data/ora della ripresa del giorno");

		builder.Property(x => x.CreditSettled)
			.HasColumnName("credit_settled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("True se il credito per questa ripresa è stato accreditato al titolare (solo origin di tipo sharing)");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione dell'offerta");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica dell'offerta");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete dell'offerta");

		builder.HasIndex(x => new { x.PropertyId, x.ResourceId, x.OfferDate })
			.HasFilter("offer_state = 'available' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_resale_offers_availability");

		builder.HasIndex(x => x.BookingLineId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_resale_offers_origin_line");

		builder.HasIndex(x => x.TakenByReservationId)
			.HasFilter("taken_by_reservation_id IS NOT NULL")
			.HasDatabaseName("ix_booking_resale_offers_taken_by");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("offer_state = 'reassigned' AND credit_settled = false AND origin = 'sharing' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_resale_offers_credit_pending");

		builder.HasOne(x => x.SourceReservation)
			.WithMany()
			.HasForeignKey(x => x.SourceReservationId)
			.HasConstraintName("fk_booking_resale_offers_source_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany()
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_resale_offers_booking_line")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.TakenByReservation)
			.WithMany()
			.HasForeignKey(x => x.TakenByReservationId)
			.HasConstraintName("fk_booking_resale_offers_taken_by_reservation")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

**Note di migrazione**:

- Merge di due tabelle legacy + un JSON: `booking_absence_details` (`status 2` → `Available`, `status 1` →
  `Reassigned`) e `sharing_details` (offerta = riga con `booked_by IS NULL`; ripresa = `booked_by`
  valorizzato → `taken_by_reservation_id`); `bookings.booking_absence_dates` JSON esploso in righe.
- `is_sharing_coming_from_absence = true` → `origin = AbsenceAsSharing`; niente più ponte applicativo tra
  le due tabelle.
- `status` + `isBooked` disallineati su 33 righe reali in prod: in migrazione **`status` vince** come fonte
  di verità per `offer_state` (coerente con la semantica "2 = disponibile" già usata dall'app), le righe
  incoerenti loggate per verifica manuale post-migrazione.
- `location` (stringa, match fragile numero/codice) → risolta a `resource_id` (`public_id` di
  `catalog_map_spots`) tramite lo stesso mapping già usato per `booking_lines`.
- `credit_value`/`number_of_credit` (parsing regex su stringa con `€`) non migrano qui: diventano righe di
  [`booking_resale_credit_ledger`](booking_resale_credit_ledger.md) in `numeric(12,2)`.
- Dedup di righe multiple per `(source_reservation_id, offer_date)` prodotte da upsert ripetuti legacy:
  tiene la più recente per `updatedAt`, le altre scartate e loggate.
- `email_sent` non migra come colonna: diventa un evento/notifica in C6.
