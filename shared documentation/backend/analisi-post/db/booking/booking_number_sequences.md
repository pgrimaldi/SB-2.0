# Tabella `booking_number_sequences`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_number_sequences` · **forma canonica**: `booking.number_sequences`.
>
> Sostituisce [`booking_logs`](../../../analisi-pre/db/booking_logs.md) (nome legacy fuorviante: **non**
> è un audit log, è il **contatore progressivo del numero prenotazione per stabilimento**). Fonti:
> [`analisi-pre/db/booking_logs.md`](../../../analisi-pre/db/booking_logs.md),
> [`analisi-pre/db/bookings.md` §`bookingNumber`](../../../analisi-pre/db/bookings.md).

---

## Descrizione

**Una riga per stabilimento**: l'ultimo progressivo assegnato. `booking_reservations.booking_number` si
genera in transazione con `SELECT … FOR UPDATE` su questa riga (lock per-property), incrementando
`last_sequence` e componendo `"{property_legacy_id}-{last_sequence zero-padded 10}"`
(es. `10795-0000012345`).

Correzioni rispetto al legacy (`booking_logs`):

- **~232.526 righe** invece di ~1 per stabilimento: `createNewBooking` eseguiva un
  `BookingLogs.create` **ridondante** a ogni prenotazione (il nuovo `createBookingNumber` non
  restituiva più il campo `response` che il ramo si aspettava). In migrazione si tiene **solo l'ultimo
  `booking_number` per `property_id`** → 1 riga per property.
- **`booking_number` come concatenazione di stringhe** (`String(propertyId) + "10000"` poi
  `parseInt + 1`) → **numero prefissato + progressivo intero zero-padded ampio** con separatore, niente
  collisione fra property adiacenti; `UNIQUE` su `booking_reservations.booking_number`.
- **nessun vincolo di unicità su `property_id`** → `UNIQUE (property_id)`.
- In alternativa a una tabella, una **PostgreSQL `SEQUENCE` per-property** o `IDENTITY` dedicata: la
  tabella è scelta per poter migrare 1:1 il valore corrente e ispezionarlo.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_number_sequences` | `id` | PK | — |
| `uq_..._property` | `(property_id)` | UNIQUE | chiave logica (1 riga per stabilimento) |

## Campi

### id
- **Legacy**: `booking_logs.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `booking_logs.property_id` (`int`, **non unico** in produzione)
- **Descrizione**: stabilimento a cui si riferisce il contatore.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); `UNIQUE`.

### number_prefix
- **Legacy**: `String(propertyId) + "10000"` (la parte fissa)
- **Descrizione**: prefisso stabile del numero prenotazione per la property (di norma il suo id legacy,
  per continuità con lo storico). Immutabile.
- **Tipo**: `varchar(20)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `booking_number = number_prefix || '-' || lpad(last_sequence, 10, '0')`.

### last_sequence
- **Legacy**: `booking_logs.booking_number` (stringa incrementata con `parseInt + 1`)
- **Descrizione**: ultimo progressivo assegnato per la property (il **prossimo** numero è
  `last_sequence + 1`).
- **Tipo**: `bigint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: incrementato sotto lock (`FOR UPDATE`) nella stessa transazione della creazione
  prenotazione.
- **Note**: in migrazione = massimo progressivo storico osservato per la property (ricavato dai
  `booking_number` esistenti).

### created_at / updated_at
- **Legacy**: `booking_logs.createdAt` / `updatedAt`
- **Descrizione**: creazione / ultimo incremento.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` (+ trigger).
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: —
- **Descrizione**: presente per uniformità; **non usato** (tabella di registro/contatore, come
  `booking_logs`/`transactions_payments` — [`docs/03-gestione-dati.md` §3.5](../../../progettazione/03-gestione-dati.md)).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- 1 riga per stabilimento (~200–500 righe totali). Il lock `FOR UPDATE` è per-property → nessuna
  contesa globale; sotto picco stagionale la serializzazione è per singolo stabilimento.
- Nessun indice oltre a PK e `UNIQUE(property_id)`.
- Non è un log: **niente** insert per prenotazione (il bug legacy da ~232k righe non si riproduce).

## Implementazione .NET

`BookingNumberSequence` è un **aggregate root** minimale (contatore, 1 riga per stabilimento) del nucleo
del modulo `Module.Booking`, schema `booking`. Implementa `IAuditable`, **non** `ISoftDeletable`:
`deleted_at` è mappato per uniformità di schema ma senza semantica applicativa (tabella di
registro/contatore, come `transactions_payments` — [`docs/03-gestione-dati.md` §3.5`](../../../progettazione/03-gestione-dati.md)),
niente Global Query Filter né metodo `Delete()` nel dominio. Nessun enum proprio.

L'incremento **non** passa dal tracking EF ordinario (`Update` + `SaveChanges` rischierebbe una race fra
transazioni concorrenti sulla stessa property): il servizio infrastrutturale `BookingNumberGenerator`
esegue un `SELECT … FOR UPDATE` esplicito sulla riga, nella stessa transazione della creazione
prenotazione, poi incrementa e compone la stringa:

```csharp
namespace Module.Booking.Infrastructure.Services;

public sealed class BookingNumberGenerator(BookingDbContext db)
{
	public async Task<string> NextAsync(Guid propertyId, CancellationToken ct)
	{
		// FOR UPDATE: lock per-property, non globale — nessuna contesa fra stabilimenti diversi.
		var sequence = await db.Database
			.SqlQuery<BookingNumberSequenceRow>(
				$"SELECT id, number_prefix, last_sequence FROM booking.booking_number_sequences " +
				$"WHERE property_id = {propertyId} FOR UPDATE")
			.SingleAsync(ct);

		var next = sequence.LastSequence + 1;
		await db.Database.ExecuteSqlAsync(
			$"UPDATE booking.booking_number_sequences SET last_sequence = {next}, updated_at = now() " +
			$"WHERE id = {sequence.Id}", ct);

		return $"{sequence.NumberPrefix}-{next.ToString("D10", CultureInfo.InvariantCulture)}";
	}
}
```

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingNumberSequenceConfiguration : IEntityTypeConfiguration<BookingNumberSequence>
{
	public void Configure(EntityTypeBuilder<BookingNumberSequence> builder)
	{
		builder.ToTable("booking_number_sequences", "booking", t => t.HasComment(
			"Contatore progressivo del numero prenotazione per stabilimento (1 riga per property); " +
			"booking_number si compone come number_prefix || '-' || lpad(last_sequence, 10, '0')."));

		builder.HasKey(x => x.Id).HasName("pk_booking_number_sequences");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.NumberPrefix)
			.HasColumnName("number_prefix")
			.HasMaxLength(20)
			.IsRequired()
			.HasComment("Prefisso stabile e immutabile del numero prenotazione per la property (di norma il suo id legacy)");

		builder.Property(x => x.LastSequence)
			.HasColumnName("last_sequence")
			.HasDefaultValue(0L)
			.IsRequired()
			.HasComment("Ultimo progressivo assegnato per la property; il prossimo numero è last_sequence + 1, incrementato sotto lock FOR UPDATE");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione del contatore");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora dell'ultimo incremento del contatore");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato: tabella di registro/contatore, non soggetta a soft-delete");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_booking_number_sequences_property");
	}
}
```

**Note di migrazione**:

- `booking_logs` aveva ~232.526 righe invece di ~1 per stabilimento (`createNewBooking` eseguiva un
  `BookingLogs.create` ridondante a ogni prenotazione — bug introdotto quando il nuovo
  `createBookingNumber` smise di restituire il campo `response` atteso dal ramo che invocava anche il
  vecchio insert): in migrazione si esegue `GROUP BY property_id` e si tiene solo `MAX` del progressivo
  storico osservato → 1 riga per property, le altre scartate.
- `number_prefix` = id legacy della property (continuità con lo storico dei `booking_number` già
  assegnati), non il nuovo `public_id` uuid.
- `last_sequence` iniziale = massimo progressivo realmente osservato tra i `booking_number` esistenti per
  quella property (non il valore di `booking_logs`, che poteva essere disallineato dai duplicati) — evita
  di riassegnare un numero già usato.
- Alternativa valutata e scartata: una `SEQUENCE` per-property nativa Postgres (più semplice, nessun lock
  applicativo) — si preferisce la tabella per poter migrare 1:1 il valore corrente, ispezionarlo/
  correggerlo da pannello e mantenere `number_prefix` accanto al contatore.
