# Tabella `payment_transaction_refunds`

> **Dominio**: C3 — `payment` · **sottoarea**: `transaction` · **schema**: `payment` · **nome flat**:
> `payment_transaction_refunds` · **forma canonica**: `payment.transaction_refunds`.
>
> Sostituisce [`refund_transaction_details`](../../../analisi-pre/db/refund_transaction_details.md)
> (0 righe — i rimborsi oggi **non** vengono tracciati; funzione `refundPayment` duplicata e
> funzionalmente rotta). Fonti:
> [`analisi-pre/db/refund_transaction_details.md`](../../../analisi-pre/db/refund_transaction_details.md),
> [`docs/03-gestione-dati.md` §3.7, §4](../../../progettazione/03-gestione-dati.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §6](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un rimborso Stripe** su una transazione online
([`payment_transaction_charges`](payment_transaction_charges.md)). Append-only. Supporta il **rimborso
parziale** (assente nel legacy: `stripe.refunds.create({ charge })` senza `amount` → sempre totale) per
i casi realistici di cancellazione con penale / rimborso su singola giornata. Il *movimento contabile*
del rimborso è una riga di [`payment_ledger_entries`](payment_ledger_entries.md) (`entry_type = refund`).

Correzioni rispetto al legacy:

- **Funzione `refundPayment` duplicata** (`stripe.controller.js` + `serviceReservation.helper.js`),
  legge `req.body.bookingid` (campo reale `booking_id`, quasi sempre `undefined`), passa un id
  `PaymentIntent` dove serve un `Charge`, referenzia `refundData` non definito → un solo percorso,
  corretto ([`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- **`amount` in centesimi** mentre `online_transaction_details.paid_payment` è euro-stringa → `numeric(12,2)`
  in euro, unità allineate ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **Nessun campo di stato/esito** → `status` enum + `failure_reason`.
- **Array di `booking_id` ma logga solo `[0]`** → FK al `charge` (che porta già la prenotazione).
- **Nessun collegamento a `service_reservation`** anche quando pertinente → `charge_id` risolve entrambi
  i casi (C1 testata unica).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_transaction_refunds` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | riferimento esterno / URL gestionale |
| `uq_..._stripe_refund` | `(stripe_refund_id) WHERE stripe_refund_id IS NOT NULL` | UNIQUE parziale | dedup webhook `charge.refunded` |
| `ix_..._charge` | `(charge_id)` | btree | rimborsi di una transazione |
| `ix_..._property_date` | `(property_id, occurred_at DESC)` | btree | lista rimborsi del gestionale / export contabile |
| `ix_..._pending` | `(status) WHERE status = 'pending'` | btree parziale | rimborsi in corso da riconciliare |

## Campi

### id
- **Legacy**: `refund_transaction_details.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: nessuno
- **Descrizione**: id pubblico stabile (gestionale, eventuale riferimento da C4/C6).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### charge_id
- **Legacy**: `refund_transaction_details.stripe_transaction_id` (correlazione per stringa) + `booking_id`
- **Descrizione**: transazione online rimborsata.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`payment_transaction_charges.id`](payment_transaction_charges.md)
  (stesso schema `payment`).

### property_id
- **Legacy**: derivato dalla prenotazione
- **Descrizione**: stabilimento (denormalizzato per gli indici e l'export).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: copiato da `payment_transaction_charges.property_id`; [D-01](../_domande-committente.md).

### reservation_public_id
- **Legacy**: `refund_transaction_details.booking_id` (`int`, salvava solo `[0]`)
- **Descrizione**: prenotazione a cui il rimborso si riferisce (denormalizzato).
- **Tipo**: `uuid` (= `public_id` di `booking_reservations`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_reservations`](../booking/booking_reservations.md).

### status
- **Legacy**: nessuno (in caso di errore la funzione ritornava `false` senza scrivere)
- **Descrizione**: esito del rimborso.
- **Tipo**: `enum payment.refund_status` — valori: `pending` (inviato a Stripe, non ancora confermato),
  `succeeded`, `failed`, `canceled`.
- **Default**: `pending`.
- **Nullabile**: No.
- **Relazioni**: `succeeded` dal webhook `charge.refund.updated` / `refund.succeeded`; aggiorna
  `payment_transaction_charges.status` a `refunded` / `partially_refunded`.

### reason
- **Legacy**: nessuno
- **Descrizione**: motivo del rimborso (per report e per il parametro `reason` di Stripe).
- **Tipo**: `enum payment.refund_reason` — valori: `requested_by_customer`, `booking_canceled`,
  `duplicate`, `fraudulent`, `other`.
- **Default**: `booking_canceled`.
- **Nullabile**: No.
- **Relazioni**: `duplicate` / `fraudulent` mappano ai `reason` nativi Stripe; gli altri → `other` lato
  API.

### amount
- **Legacy**: `refund_transaction_details.amount` (`int`, **centesimi**; sempre = importo pieno)
- **Descrizione**: importo rimborsato in €. Può essere < `original_amount` (rimborso parziale).
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (amount > 0 AND amount <= original_amount)`.

### original_amount
- **Legacy**: nessuno (era implicito = importo pieno della transazione)
- **Descrizione**: importo pieno della transazione originaria al momento del rimborso (snapshot, per
  report senza chiamare Stripe).
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: copiato da `payment_transaction_charges.paid_amount`.

### currency
- **Legacy**: nessuno
- **Descrizione**: valuta ISO 4217.
- **Tipo**: `char(3)`.
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: = `payment_transaction_charges.currency`.

### stripe_refund_id
- **Legacy**: `refund_transaction_details.refund_id` (`varchar`, `re_…` da `refund.id`)
- **Descrizione**: id dell'oggetto `Refund` Stripe, salvato verbatim; correla al webhook
  `charge.refunded` e alla dashboard.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (valorizzato appena Stripe risponde).
- **Relazioni**: `UNIQUE` parziale.

### failure_reason
- **Legacy**: nessuno
- **Descrizione**: messaggio/codice di fallimento Stripe quando `status = failed`.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### requested_by_user_id
- **Legacy**: nessuno (endpoint `optionalAuth`, nessuna ownership — [`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md))
- **Descrizione**: operatore che ha disposto il rimborso (audit fiscale).
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì (rimborso automatico da cancellazione → `NULL`).
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).

### occurred_at
- **Legacy**: `refund_transaction_details.createdAt`
- **Descrizione**: data/ora del rimborso (decorrenza retention).
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: retention 10 anni (24 mesi online, poi archivio — [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md)).

### created_at / updated_at / deleted_at
- **Legacy**: `refund_transaction_details.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica. `deleted_at` esposto per uniformità: append-only (scrittura
  contabile).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at` (sempre `NULL`).

## Valutazione JSON

Nessun JSON. `amount` da centesimi a `numeric(12,2)` in euro.

## Suggerimenti per le prestazioni

- Volume basso (i rimborsi sono minoritari). Indici su `charge_id` e `stripe_refund_id` per la
  riconciliazione dei webhook.
- Nessuna aggregazione onerosa: il totale rimborsato di una transazione = `SUM(amount) WHERE charge_id`
  con pochissime righe per charge.
- Stesso partizionamento per anno del `payment_transaction_charges` se/quando lo storico cresce.

## Implementazione .NET

`Refund` è un'**entità figlia** di `Charge` (aggregate root `payment_transaction_charges`), area
`Transaction` in `Module.Payment`, schema `payment`. Append-only: nessuna riscrittura, una correzione è
un nuovo rimborso. `IPaymentGateway.RefundAsync(chargeId, amount?, reason)` usa `stripe_charge_id`
(`ch_…`), mai il `pi_…` — bug ricorrente nel legacy.

### Enum propri

```csharp
// Module.Payment.Domain.Enums.RefundStatus
public enum RefundStatus
{
	Pending = 0,
	Succeeded = 1,
	Failed = 2,
	Canceled = 3
}

// Module.Payment.Domain.Enums.RefundReason
public enum RefundReason
{
	RequestedByCustomer = 0,
	BookingCanceled = 1,
	Duplicate = 2,
	Fraudulent = 3,
	Other = 4
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<RefundStatus>("payment", "refund_status");
modelBuilder.HasPostgresEnum<RefundReason>("payment", "refund_reason");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<RefundStatus>("payment.refund_status");
dataSourceBuilder.MapEnum<RefundReason>("payment.refund_reason");
```

### `RefundConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
	public void Configure(EntityTypeBuilder<Refund> builder)
	{
		builder.ToTable("transaction_refunds", "payment", t =>
		{
			t.HasComment("Un rimborso Stripe su una transazione online, con supporto al rimborso parziale.");
			t.HasCheckConstraint("ck_payment_transaction_refunds_amount", "amount > 0 AND amount <= original_amount");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_transaction_refunds");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, per il gestionale e per eventuale riferimento da C4/C6.");

		builder.Property(x => x.ChargeId)
			.HasColumnName("charge_id")
			.IsRequired()
			.HasComment("Transazione online rimborsata.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla transazione — nessuna FK cross-schema; denormalizzato per indici ed export.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione a cui il rimborso si riferisce.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.refund_status")
			.HasDefaultValue(RefundStatus.Pending)
			.IsRequired()
			.HasComment("Esito del rimborso.");

		builder.Property(x => x.Reason)
			.HasColumnName("reason")
			.HasColumnType("payment.refund_reason")
			.HasDefaultValue(RefundReason.BookingCanceled)
			.IsRequired()
			.HasComment("Motivo del rimborso, per report e per il parametro reason di Stripe.");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo rimborsato in euro; può essere inferiore a original_amount (rimborso parziale).");

		builder.Property(x => x.OriginalAmount)
			.HasColumnName("original_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo pieno della transazione originaria al momento del rimborso (snapshot).");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217, allineata alla transazione.");

		builder.Property(x => x.StripeRefundId)
			.HasColumnName("stripe_refund_id")
			.HasMaxLength(40)
			.HasComment("Id dell'oggetto Refund Stripe (re_…), verbatim; correla al webhook charge.refunded.");

		builder.Property(x => x.FailureReason)
			.HasColumnName("failure_reason")
			.HasMaxLength(255)
			.HasComment("Messaggio/codice di fallimento Stripe quando status = failed.");

		builder.Property(x => x.RequestedByUserId)
			.HasColumnName("requested_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha disposto il rimborso (audit fiscale); NULL se automatico.");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora del rimborso, decorrenza retention.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only (scrittura contabile), sempre NULL.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_transaction_refunds_public_id");

		builder.HasIndex(x => x.StripeRefundId)
			.IsUnique()
			.HasFilter("stripe_refund_id IS NOT NULL")
			.HasDatabaseName("uq_payment_transaction_refunds_stripe_refund");

		builder.HasIndex(x => x.ChargeId)
			.HasDatabaseName("ix_payment_transaction_refunds_charge");

		builder.HasIndex(x => new { x.PropertyId, x.OccurredAt })
			.HasDatabaseName("ix_payment_transaction_refunds_property_date");

		builder.HasIndex(x => x.Status)
			.HasFilter("status = 'pending'")
			.HasDatabaseName("ix_payment_transaction_refunds_pending");

		builder.HasOne(x => x.Charge)
			.WithMany(c => c.Refunds)
			.HasForeignKey(x => x.ChargeId)
			.HasConstraintName("fk_payment_transaction_refunds_charge")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

### Relazioni

`charge_id` referenzia [`payment_transaction_charges`](payment_transaction_charges.md), stesso schema
`payment`: FK EF reale (`HasOne`/`WithMany(c => c.Refunds)`, `Restrict` — append-only, mai cascata di
cancellazione). `property_id`, `reservation_public_id`, `requested_by_user_id` sono `uuid` copiati da
C2/C1/C5: solo `.HasComment`, nessuna FK cross-schema.

`ISoftDeletable` implementato solo per uniformità (sempre `NULL`, append-only); `IAuditable` implementato.

### Note di migrazione

- `refund_transaction_details` è vuota (0 righe, i rimborsi oggi non vengono tracciati) → nessun dato
  storico da migrare, tabella creata vuota.
- `IPaymentGateway.RefundAsync` unifica le due implementazioni legacy duplicate e rotte
  (`stripe.controller.js` + `serviceReservation.helper.js`).
- Webhook `charge.refunded` / `refund.updated` (idempotente su `event.id`) → aggiorna `status`; emette
  `RefundIssued` (outbox) → C1 aggiorna `paid`/`balance_due`, C4 valuta nota di credito/annullo scontrino.
