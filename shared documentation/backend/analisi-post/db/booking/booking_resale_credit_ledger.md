# Tabella `booking_resale_credit_ledger`

> **Dominio**: C1 — `booking` · **sottoarea**: `hold` (rivendita) · **schema**: `booking` · **nome flat**:
> `booking_resale_credit_ledger` · **forma canonica**: `booking.resale_credit_ledger`.
>
> Nuova: sostituisce i campi `number_of_credit` / `credit_value` di
> [`sharing_details`](../../../analisi-pre/db/sharing_details.md) e il cron
> `sharingCreditCron` che scriveva **per assegnazione** (non incremento) su
> `users.total_number_of_credit` / `total_credit_value` (ancorato all'**email**). Fonti:
> [`analisi-pre/db/sharing_details.md`](../../../analisi-pre/db/sharing_details.md),
> [`analisi-pre/db/configurations.md` §sharing](../../../analisi-pre/db/configurations.md),
> [`docs/03-gestione-dati.md` §3.7, §6](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Ledger append-only** dei crediti "sharing" maturati dal titolare di una prenotazione quando un suo
giorno ceduto ([`booking_resale_offers`](booking_resale_offers.md)) viene ripreso da un altro cliente.
Una riga per accredito (o storno). Il **saldo** è la somma delle righe per cliente; verso **C3**
(borsellino/wallet) si emette un evento `ResaleCreditEarned` — qui resta la storia di *come* il credito
è maturato (quale prenotazione, quale giorno, quale postazione).

Correzioni rispetto al legacy:

- **`credit_value` intero** ottenuto da `req.body.credit_value.replace(/€/g,"")` (perde i decimali,
  mescola importi e percentuali) → `amount numeric(12,2)` + il **tipo** (importo/percentuale) è nella
  config ([`booking_resale_settings`](booking_resale_settings.md)).
- **accredito che sovrascrive** (`UPDATE users SET total_number_of_credit = :n`) → riga di ledger
  **immutabile**; il saldo si somma.
- **ancoraggio all'email** → `customer_id` (uuid, C5).
- **`totalCreditsEarned` denormalizzato su `users` da un cron** → C3 (proiezione wallet), non qui.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_resale_credit_ledger` | `id` | PK | — |
| `ix_..._customer` | `(property_id, customer_id, occurred_at DESC)` | btree | saldo/estratto conto crediti del cliente |
| `ix_..._offer` | `(resale_offer_id)` | btree | risalita all'offerta che ha generato il credito |
| `uq_..._offer_entry` | `(resale_offer_id, entry_type) WHERE entry_type = 'earned'` | UNIQUE parziale | un solo accredito per ripresa (idempotenza) |

## Campi

### id
- **Legacy**: — (nuova)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `sharing_details.property_id`
- **Descrizione**: stabilimento.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2.

### customer_id
- **Legacy**: `users.email` (il cron accreditava per email)
- **Descrizione**: cliente **titolare** a cui il credito è maturato.
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_customer_customers`](../identity/identity_customer_customers.md).

### resale_offer_id
- **Legacy**: `sharing_details` (la riga di ripresa)
- **Descrizione**: offerta/ripresa che ha generato il credito.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (aggiustamenti manuali senza offerta).
- **Relazioni**: **FK** → [`booking_resale_offers.id`](booking_resale_offers.md) `ON DELETE SET NULL`.

### source_reservation_id
- **Legacy**: `sharing_details.booking_id`
- **Descrizione**: prenotazione del titolare (di cui è stato ceduto il giorno).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE SET NULL`.

### entry_type
- **Legacy**: — (solo accredito)
- **Descrizione**: tipo di movimento.
- **Tipo**: `enum booking.resale_credit_entry` — valori: `earned` (accredito per ripresa),
  `reversed` (storno: la ripresa è stata annullata), `adjustment` (correzione manuale).
- **Default**: `earned`.
- **Nullabile**: No.
- **Relazioni**: —.

### credit_units
- **Legacy**: `sharing_details.number_of_credit` (`int`)
- **Descrizione**: numero di "crediti" (unità simboliche) maturati per il movimento.
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### amount
- **Legacy**: `sharing_details.credit_value` (int da `replace(/€/g,"")`)
- **Descrizione**: valore economico del credito (segno positivo per `earned`, negativo per `reversed`).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: mai `float`; il tipo (importo assoluto vs percentuale del giornaliero) è definito in
  [`booking_resale_settings`](booking_resale_settings.md) e già risolto in importo qui.

### occurred_at
- **Legacy**: `createdAt` della riga di ripresa / esecuzione del cron
- **Descrizione**: quando il movimento è avvenuto.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### wallet_event_emitted
- **Legacy**: — (il legacy scriveva direttamente su `users`)
- **Descrizione**: `true` quando l'evento `ResaleCreditEarned` è stato consegnato a C3 (per la
  riconciliazione col borsellino).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: outbox verso C3.

### note
- **Legacy**: —
- **Descrizione**: nota (per `adjustment`).
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1.547 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: `createdAt` / `updatedAt`
- **Descrizione**: creazione / modifica riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` (+ trigger).
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: —
- **Descrizione**: presente per uniformità; **non usato** (ledger append-only — le correzioni sono
  righe `reversed`/`adjustment`, [`docs/03-gestione-dati.md` §3.5](../../../progettazione/03-gestione-dati.md)).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Append-only: nessun `UPDATE` sul saldo (il legacy sovrascriveva `users` a ogni ripresa, perdendo i
  crediti di prenotazioni multiple). Il saldo è `SUM(amount)` per `customer_id`, servito dall'indice
  `(property_id, customer_id, occurred_at DESC)`.
- `UNIQUE (resale_offer_id) WHERE entry_type = 'earned'` rende idempotente l'accredito (il cron legacy
  deduplicava a mano con un `Set` di chiavi).
- Tabella piccola (~9 stabilimenti usano lo sharing).

## Implementazione .NET

`ResaleCreditLedgerEntry` è un **aggregate root append-only** dell'area `Resale` del modulo
`Module.Booking`, schema `booking`. Implementa `IAuditable` (`created_at`/`updated_at` reali), **non**
`ISoftDeletable`: `deleted_at` è mappato per uniformità di schema ma non ha semantica applicativa qui (le
correzioni sono righe `reversed`/`adjustment`, mai una `DELETE`/soft-delete di un movimento) — niente
Global Query Filter su questa entità, e nessun metodo `Delete()` nel dominio.

```csharp
// Domain/Enums/ResaleCreditEntryType.cs
public enum ResaleCreditEntryType { Earned = 0, Reversed = 1, Adjustment = 2 }
```

```csharp
// <Modulo>DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ResaleCreditEntryType>("booking", "resale_credit_entry");
```

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResaleCreditLedgerEntryConfiguration : IEntityTypeConfiguration<ResaleCreditLedgerEntry>
{
	public void Configure(EntityTypeBuilder<ResaleCreditLedgerEntry> builder)
	{
		builder.ToTable("booking_resale_credit_ledger", "booking", t => t.HasComment(
			"Ledger append-only dei crediti sharing maturati dal titolare quando un giorno ceduto viene " +
			"ripreso da un altro cliente; il saldo è la somma delle righe per cliente."));

		builder.HasKey(x => x.Id).HasName("pk_booking_resale_credit_ledger");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.IsRequired()
			.HasComment("public_id di identity.customer_customers (cliente titolare a cui il credito è maturato), copiato — nessuna FK cross-schema");

		builder.Property(x => x.ResaleOfferId)
			.HasColumnName("resale_offer_id")
			.HasComment("Offerta/ripresa che ha generato il credito (nullable: aggiustamenti manuali senza offerta)");

		builder.Property(x => x.SourceReservationId)
			.HasColumnName("source_reservation_id")
			.HasComment("Prenotazione del titolare di cui è stato ceduto il giorno");

		builder.Property(x => x.EntryType)
			.HasColumnName("entry_type")
			.HasColumnType("booking.resale_credit_entry")
			.HasDefaultValue(ResaleCreditEntryType.Earned)
			.IsRequired()
			.HasComment("Tipo di movimento: accredito per ripresa, storno di una ripresa annullata, correzione manuale");

		builder.Property(x => x.CreditUnits)
			.HasColumnName("credit_units")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Numero di crediti (unità simboliche) maturati per il movimento");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Valore economico del credito: segno positivo per earned, negativo per reversed");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora in cui il movimento è avvenuto");

		builder.Property(x => x.WalletEventEmitted)
			.HasColumnName("wallet_event_emitted")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("True quando l'evento ResaleCreditEarned è stato consegnato a Payment (C3) per la riconciliazione col borsellino");

		builder.Property(x => x.Note)
			.HasColumnName("note")
			.HasMaxLength(255)
			.HasComment("Nota libera, valorizzata per i movimenti di tipo adjustment");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione della riga di ledger");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica della riga di ledger");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato: il ledger è append-only, le correzioni sono righe reversed/adjustment");

		builder.HasIndex(x => new { x.PropertyId, x.CustomerId, x.OccurredAt })
			.HasDatabaseName("ix_booking_resale_credit_ledger_customer")
			.IsDescending(false, false, true);

		builder.HasIndex(x => x.ResaleOfferId)
			.HasDatabaseName("ix_booking_resale_credit_ledger_offer");

		builder.HasIndex(x => x.ResaleOfferId)
			.IsUnique()
			.HasFilter("entry_type = 'earned'")
			.HasDatabaseName("uq_booking_resale_credit_ledger_offer_entry");

		builder.HasOne(x => x.ResaleOffer)
			.WithMany()
			.HasForeignKey(x => x.ResaleOfferId)
			.HasConstraintName("fk_booking_resale_credit_ledger_resale_offer")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.SourceReservation)
			.WithMany()
			.HasForeignKey(x => x.SourceReservationId)
			.HasConstraintName("fk_booking_resale_credit_ledger_source_reservation")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

**Note di migrazione**:

- Una riga `entry_type = Earned` per ogni riga `sharing_details` con `isBooked = 1` / `booked_by` non
  nullo (ripresa avvenuta); `number_of_credit` → `credit_units`, `credit_value` (int da
  `replace(/€/g,"")`) → `amount numeric(12,2)` risolvendo secondo il `credit_calc` già determinato in
  [`booking_resale_settings`](booking_resale_settings.md) per lo stabilimento.
- `customer_id` risolto dall'**email** del titolare (unico ancoraggio disponibile nel legacy, il cron
  scriveva su `users` per email) verso il `public_id` in `identity_customer_customers`; email non
  risolvibili (utente cancellato/duplicato) → riga scartata e loggata.
- Nessuno storno (`reversed`) migra dal legacy: il cron sovrascriveva `total_number_of_credit` senza
  tracciare le correzioni; si parte con solo righe `earned` storiche.
- `wallet_event_emitted` parte `false` per tutte le righe migrate: la riconciliazione col borsellino C3
  (evento `ResaleCreditEarned`) è un processo **nuovo**, non esisteva nel legacy (scriveva direttamente
  su `users`).
