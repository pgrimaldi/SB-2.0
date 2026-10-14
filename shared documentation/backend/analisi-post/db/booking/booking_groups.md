# Tabella `booking_groups`

> **Dominio**: C1 — `booking` · **sottoarea**: `group` · **schema**: `booking` · **nome flat**:
> `booking_groups` · **forma canonica**: `booking.groups`.
>
> Sostituisce la legacy [`booking_groups`](../../../analisi-pre/db/booking_groups.md) (model
> `booking_group`; array JSON `groupBookings` → giunzione
> [`booking_group_members`](booking_group_members.md)). Fonti:
> [`analisi-pre/db/booking_groups.md`](../../../analisi-pre/db/booking_groups.md),
> [`analisi-pre/business-logic/stabilimento/02-planner.md` §4](../../../analisi-pre/business-logic/stabilimento/02-planner.md).

---

## Descrizione

**Un raggruppamento manuale di prenotazioni distinte** (comitiva, famiglia su più ombrelloni) sotto
un'etichetta scelta dall'operatore, per vederle/gestirle/spostarle insieme e mostrare `groupInfo`
accanto a ciascuna nel dettaglio/agenda. Non è la prenotazione multipla (quella condivide un
`booking_number`): i membri restano prenotazioni indipendenti.

Correzioni rispetto al legacy:

- **`name` unico globale** (`findOne({ where: { name } })` senza `property_id`) → `UNIQUE (property_id,
  lower(name))` — due stabilimenti possono avere un gruppo con lo stesso nome.
- **`groupBookings` array JSON di `bookings.id`** → giunzione `booking_group_members`.
- **`status` `int` (soft-delete implicito, mai esercitato)** → `deleted_at`.
- **validazione "una prenotazione in un solo gruppo" O(N·M) in memoria su tutti i gruppi** → `UNIQUE`
  su `booking_group_members.reservation_id`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_groups` | `id` | PK | — |
| `uq_..._property_name` | `(property_id, lower(name)) WHERE deleted_at IS NULL` | UNIQUE parziale | nome gruppo univoco **per stabilimento** (era globale) |
| `ix_..._property` | `(property_id) WHERE deleted_at IS NULL` | parziale | elenco gruppi di uno stabilimento |

## Campi

### id
- **Legacy**: `booking_groups.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`booking_group_members.group_id`](booking_group_members.md).

### property_id
- **Legacy**: `booking_groups.property_id` (`int`, `NOT NULL`)
- **Descrizione**: stabilimento proprietario del gruppo.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md).

### name
- **Legacy**: `booking_groups.name` (`varchar`, unicità **globale**)
- **Descrizione**: nome del gruppo mostrato in UI.
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE (property_id, lower(name))` parziale.

### created_by_user_id
- **Legacy**: — (non tracciato)
- **Descrizione**: operatore che ha creato il gruppo.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md); dal token.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 69 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: `booking_groups.createdAt` / `updatedAt`
- **Descrizione**: creazione / modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` (+ trigger).
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: `booking_groups.status` (`!= 1` = cancellato — mai esercitato in prod)
- **Descrizione**: soft-delete del gruppo.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF; cancellando il gruppo si soft-eliminano anche i membri.

## Valutazione JSON

Nessun JSON. `groupBookings` (array di `bookings.id`) → [`booking_group_members`](booking_group_members.md).

## Suggerimenti per le prestazioni

- Tabella minuscola (~70 righe storiche, pochi stabilimenti). Nessun problema di scala; l'importante è
  la giunzione con `UNIQUE` che elimina la validazione O(N·M) in memoria del legacy.

## Implementazione .NET

`BookingGroup` è l'**aggregate root** dell'area `Group` del modulo `Module.Booking`, schema `booking`.
Espone la collezione `Members` (entità figlia [`booking_group_members`](booking_group_members.md)) e
implementa `ISoftDeletable`/`IAuditable`. Nessun enum proprio.

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingGroupConfiguration : IEntityTypeConfiguration<BookingGroup>
{
	public void Configure(EntityTypeBuilder<BookingGroup> builder)
	{
		builder.ToTable("booking_groups", "booking", t => t.HasComment(
			"Raggruppamento manuale di prenotazioni distinte sotto un'etichetta scelta dall'operatore " +
			"(comitiva, famiglia su più ombrelloni); non è la prenotazione multipla."));

		builder.HasKey(x => x.Id).HasName("pk_booking_groups");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento proprietario del gruppo), copiato — nessuna FK cross-schema");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome del gruppo mostrato in UI");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users (operatore che ha creato il gruppo, dal token), copiato — nessuna FK cross-schema");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione del gruppo");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica del gruppo");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete del gruppo (cancellando il gruppo si soft-eliminano anche i membri)");

		// indice unique effettivo su (property_id, lower(name)) — espressione non esprimibile in Fluent
		// API pura, rifinita in migration con SQL raw (vedi Note di migrazione)
		builder.HasIndex(x => new { x.PropertyId, x.Name })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_groups_property_name");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_groups_property");

		builder.HasMany(x => x.Members)
			.WithOne(m => m.Group)
			.HasForeignKey(m => m.GroupId)
			.HasConstraintName("fk_booking_group_members_group")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

**Note di migrazione**:

- L'indice unique `(property_id, lower(name))` usa una funzione (`lower()`) non esprimibile in Fluent API
  pura: dopo `Add-Migration`, l'indice generato su `(property_id, name)` va sostituito a mano nella
  migration con `migrationBuilder.Sql("CREATE UNIQUE INDEX uq_booking_groups_property_name ON " +
  "booking.booking_groups (property_id, lower(name)) WHERE deleted_at IS NULL;")` (drop del `CreateIndex`
  generato automaticamente).
- Migrazione dati: righe legacy con `status = 1` (attivo) → riga `booking_groups`; le altre (`status != 1`,
  mai esercitato in prod) → scartate o valorizzate con `deleted_at = now()` se si vuole preservare lo storico.
- `groupBookings` (array JSON di `bookings.id`) → una riga `booking_group_members` per elemento, con
  `reservation_id` risolto verso il nuovo id di `booking_reservations`; id orfani (prenotazione non trovata)
  scartati e loggati.
- `created_by_user_id` non era tracciato in legacy: valorizzato `NULL` per le righe migrate.
