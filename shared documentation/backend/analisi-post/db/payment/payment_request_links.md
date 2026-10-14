# Tabella `payment_request_links`

> **Dominio**: C3 — `payment` · **sottoarea**: `request` · **schema**: `payment` · **nome flat**:
> `payment_request_links` · **forma canonica**: `payment.request_links`.
>
> Sostituisce la **fetta C3** di
> [`bookings_send_payment_summaries`](../../../analisi-pre/db/bookings_send_payment_summaries.md)
> (1.392 righe reali; feature "link di pagamento Stripe" da fine maggio 2026). La fetta **canale/esito
> invio** (`method_flag`, `email_sent`) va in **C6** ([`docs/03-gestione-dati.md` §4](../../../progettazione/03-gestione-dati.md)).
> Fonti:
> [`analisi-pre/db/bookings_send_payment_summaries.md`](../../../analisi-pre/db/bookings_send_payment_summaries.md),
> [`docs/03-gestione-dati.md` §2.4bis, §3.7, §4](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).
>
> *La sottoarea `request` non era in [`docs/03-gestione-dati.md` §3.1bis](../../../progettazione/03-gestione-dati.md)
> (che elenca `gateway` / `transaction` / `ledger` / `wallet` / `pos`): emerge da questa scheda — il
> "fatto" richiesta di pagamento è a sé rispetto alla transazione che ne nasce. Da recepire in §3.1bis.*

---

## Descrizione

**Una riga = un link di pagamento Stripe** ("riepilogo pagamento") inviato al cliente per saldare o far
confermare una prenotazione. Tiene il *fatto* "pagamento richiesto: importo X, scadenza Y, esito Z" e
il collegamento alla prenotazione e (una volta pagato) alla transazione
([`payment_transaction_charges`](payment_transaction_charges.md)). Il *fatto* "il link è stato inviato
via email/WhatsApp" è un log di messaggistica **C6**.

Correzioni rispetto al legacy:

- **`amount` intero (euro)** — nessun centesimo, ×100 su Stripe → `numeric(12,2)`
  ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **Stato spalmato su 3 campi** (`payment_status` `unpaid`/`paid`, `cancel_payment_flag`,
  `end_date_time` scaduto) → un solo `status` enum
  `pending → paid | canceled | expired | failed` ([scheda legacy §payment_status](../../../analisi-pre/db/bookings_send_payment_summaries.md)).
- **`payment_status` stringa libera** con storia di significati (`0/1/2` → `unpaid/paid`) → enum.
- **`method_flag` / `email_sent`** (canale ed esito invio) → **C6** (log messaggio).
- **`booking_cancel_flag`** → `cancel_reservation_on_expiry` (config della richiesta, **non** stato del
  link).
- **`isCallingFromBookingDetail`** (`NULL` sul 50%) → `origin` enum.
- **Riga creata in due passi** (prima `stripe_session_id = ""`, poi update) → `stripe_payment_link_id`
  nullable finché Stripe non risponde, nessun `""`.
- **`booking_id` nullable agganciato a posteriori** → resta nullable (il link può precedere la
  prenotazione), ma con indice per la ricongiunzione.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_request_links` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | usato nell'URL `payment-summary-success/{public_id}` |
| `uq_..._stripe_link` | `(stripe_payment_link_id) WHERE stripe_payment_link_id IS NOT NULL` | UNIQUE parziale | dedup webhook |
| `ix_..._reservation` | `(reservation_public_id)` | btree | link di una prenotazione / ricongiunzione |
| `ix_..._property_status` | `(property_id, status)` | btree | cruscotto "link inviati / pagati / scaduti" |
| `ix_..._expiry` | `(expires_at) WHERE status = 'pending'` | btree parziale | job di scadenza link + (opz.) cancellazione prenotazione |

## Campi

### id
- **Legacy**: `bookings_send_payment_summaries.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: `bookings_send_payment_summaries.id` (usato **in chiaro** nell'URL di redirect)
- **Descrizione**: id pubblico stabile, esposto nell'URL post-pagamento e nel messaggio al cliente.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`; sostituisce l'id sequenziale enumerabile.

### property_id
- **Legacy**: derivato dalla prenotazione
- **Descrizione**: stabilimento.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); [D-01](../_domande-committente.md).

### reservation_public_id
- **Legacy**: `bookings_send_payment_summaries.booking_id` (`int`, agganciato anche **dopo** dalla `updatePaymentSummary`)
- **Descrizione**: prenotazione da saldare/confermare.
- **Tipo**: `uuid` (= `public_id` di `booking_reservations`).
- **Default**: nessuno.
- **Nullabile**: Sì (il link può essere generato **prima** che la prenotazione esista).
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_reservations`](../booking/booking_reservations.md).

### status
- **Legacy**: `payment_status` (`unpaid`/`paid`) + `cancel_payment_flag` + `end_date_time` (scaduto)
- **Descrizione**: stato del link. Un solo enum, nessuna combinazione contraddittoria.
- **Tipo**: `enum payment.payment_request_status` — valori: `pending` (creato/inviato, non pagato),
  `paid`, `canceled` (revocato), `expired` (scaduto non pagato), `failed` (fallimento Stripe).
- **Default**: `pending`.
- **Nullabile**: No.
- **Relazioni**: `paid` dal webhook `checkout.session.completed` → crea/aggancia
  [`payment_transaction_charges`](payment_transaction_charges.md); `expired` dal job su `expires_at`.

### origin
- **Legacy**: `bookings_send_payment_summaries.isCallingFromBookingDetail` (`1`/`0`/`NULL`)
- **Descrizione**: da dove è stato generato il link (cambia testi/redirect).
- **Tipo**: `enum payment.payment_request_origin` — valori: `booking_detail` (saldo di una prenotazione
  esistente), `booking_creation` (creazione con richiesta di pagamento), `unknown` (righe legacy con
  `NULL`).
- **Default**: `booking_detail`.
- **Nullabile**: No.
- **Relazioni**: —.

### amount
- **Legacy**: `bookings_send_payment_summaries.amount` (`int`, euro interi; ×100 su Stripe)
- **Descrizione**: importo richiesto dal link.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (amount > 0)`.

### currency
- **Legacy**: nessuno
- **Descrizione**: valuta ISO 4217.
- **Tipo**: `char(3)`.
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: —.

### cancel_reservation_on_expiry
- **Legacy**: `bookings_send_payment_summaries.booking_cancel_flag` (`tinyint` 0/1)
- **Descrizione**: alla scadenza/annullamento del link va cancellata **anche** la prenotazione
  associata (usato dal cron). È **configurazione** della richiesta, non stato del link.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: se `true` e `status → expired`, emette `PaymentRequestExpired` con richiesta di
  cancellazione a C1.

### stripe_payment_link_id
- **Legacy**: `bookings_send_payment_summaries.stripe_session_id` (`plink_…` / sessione; `""` prima della creazione)
- **Descrizione**: id del Payment Link / Checkout Session Stripe.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (finché `paymentLinks.create` non risponde).
- **Relazioni**: `UNIQUE` parziale; correla il webhook al link.

### charge_id
- **Legacy**: nessuno (il legacy non collegava il link alla transazione)
- **Descrizione**: transazione generata dal pagamento del link.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (valorizzata a `status = paid`).
- **Relazioni**: **FK interna** → [`payment_transaction_charges.id`](payment_transaction_charges.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1.390 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### expires_at
- **Legacy**: `bookings_send_payment_summaries.end_date_time` (`datetime`)
- **Descrizione**: scadenza del link.
- **Tipo**: `timestamptz`.
- **Default**: nessuno.
- **Nullabile**: Sì (link senza scadenza).
- **Relazioni**: il job confronta `now()` con questo valore per le righe `pending`.

### paid_at / canceled_at
- **Legacy**: desunti da `payment_status` / `cancel_payment_flag`
- **Descrizione**: quando il link è stato pagato / revocato.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: —.

### created_by_user_id
- **Legacy**: nessuno
- **Descrizione**: operatore che ha generato il link (audit).
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1.390 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `bookings_send_payment_summaries.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica. `deleted_at` per uniformità: il *fatto* segue la transazione
  (retention 10 anni), il *payload di invio* → C6 (12 mesi) — [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. Il legacy non ne aveva; i flag multipli si consolidano in `status` + 1 boolean di config.

## Suggerimenti per le prestazioni

- ~1.400 righe, crescita moderata. Indice parziale su `expires_at WHERE status = 'pending'` mantiene
  piccolo l'insieme che il job di scadenza deve scandire.
- Indice `(property_id, status)` per i cruscotti; nessuna aggregazione onerosa.
- Il webhook `checkout.session.completed` deve essere **idempotente** su `event.id` (nel legacy questo
  branch ne era del tutto privo — [`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).

## Implementazione .NET

`PaymentRequestLink` è l'**aggregate root** dell'area `Request` in `Module.Payment`, schema `payment`:
1 riga per link di pagamento Stripe inviato al cliente. Solleva `PaymentRequestPaid` /
`PaymentRequestExpired` (outbox), consumati da C1 (cancellazione condizionata) e C4. Il canale di invio
(email/WhatsApp) resta fuori: delegato a C6 (`INotificationService`), nessuna colonna qui.

### Enum propri

```csharp
// Module.Payment.Domain.Enums.PaymentRequestStatus
public enum PaymentRequestStatus
{
	Pending = 0,
	Paid = 1,
	Canceled = 2,
	Expired = 3,
	Failed = 4
}

// Module.Payment.Domain.Enums.PaymentRequestOrigin
public enum PaymentRequestOrigin
{
	BookingDetail = 0,
	BookingCreation = 1,
	Unknown = 2
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<PaymentRequestStatus>("payment", "payment_request_status");
modelBuilder.HasPostgresEnum<PaymentRequestOrigin>("payment", "payment_request_origin");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<PaymentRequestStatus>("payment.payment_request_status");
dataSourceBuilder.MapEnum<PaymentRequestOrigin>("payment.payment_request_origin");
```

### `PaymentRequestLinkConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class PaymentRequestLinkConfiguration : IEntityTypeConfiguration<PaymentRequestLink>
{
	public void Configure(EntityTypeBuilder<PaymentRequestLink> builder)
	{
		builder.ToTable("request_links", "payment", t =>
		{
			t.HasComment("Link di pagamento Stripe inviato al cliente per saldare o confermare una prenotazione.");
			t.HasCheckConstraint("ck_payment_request_links_amount", "amount > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_request_links");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, esposto nell'URL post-pagamento e nel messaggio al cliente.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; il link può precedere la prenotazione.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.payment_request_status")
			.HasDefaultValue(PaymentRequestStatus.Pending)
			.IsRequired()
			.HasComment("Stato del link: creato, pagato, revocato, scaduto o fallito.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("payment.payment_request_origin")
			.HasDefaultValue(PaymentRequestOrigin.BookingDetail)
			.IsRequired()
			.HasComment("Da dove è stato generato il link, cambia testi/redirect.");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo richiesto dal link.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217.");

		builder.Property(x => x.CancelReservationOnExpiry)
			.HasColumnName("cancel_reservation_on_expiry")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Se true, alla scadenza/annullamento del link va cancellata anche la prenotazione associata.");

		builder.Property(x => x.StripePaymentLinkId)
			.HasColumnName("stripe_payment_link_id")
			.HasMaxLength(40)
			.HasComment("Id del Payment Link / Checkout Session Stripe, valorizzato appena Stripe risponde.");

		builder.Property(x => x.ChargeId)
			.HasColumnName("charge_id")
			.HasComment("Transazione generata dal pagamento del link, valorizzata a status = paid.");

		builder.Property(x => x.ExpiresAt)
			.HasColumnName("expires_at")
			.HasComment("Scadenza del link; NULL se senza scadenza.");

		builder.Property(x => x.PaidAt)
			.HasColumnName("paid_at")
			.HasComment("Quando il link è stato pagato.");

		builder.Property(x => x.CanceledAt)
			.HasColumnName("canceled_at")
			.HasComment("Quando il link è stato revocato.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha generato il link.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: il fatto segue la transazione (retention 10 anni).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_request_links_public_id");

		builder.HasIndex(x => x.StripePaymentLinkId)
			.IsUnique()
			.HasFilter("stripe_payment_link_id IS NOT NULL")
			.HasDatabaseName("uq_payment_request_links_stripe_link");

		builder.HasIndex(x => x.ReservationPublicId)
			.HasDatabaseName("ix_payment_request_links_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.Status })
			.HasDatabaseName("ix_payment_request_links_property_status");

		builder.HasIndex(x => x.ExpiresAt)
			.HasFilter("status = 'pending'")
			.HasDatabaseName("ix_payment_request_links_expiry");

		builder.HasOne(x => x.Charge)
			.WithMany()
			.HasForeignKey(x => x.ChargeId)
			.HasConstraintName("fk_payment_request_links_charge")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

### Relazioni

`property_id`, `reservation_public_id`, `created_by_user_id` sono `uuid` copiati da C2/C1/C5: solo
`.HasComment`, nessuna FK cross-schema. `charge_id` referenzia
[`payment_transaction_charges`](payment_transaction_charges.md), stesso schema `payment`: FK EF reale,
nullable (`Restrict`, valorizzata solo a pagamento avvenuto).

`ISoftDeletable` + `IAuditable`: implementati.

### Note di migrazione

- `amount` (int euro) → `numeric(12,2)`.
- `payment_status` + `cancel_payment_flag` + `end_date_time` → un solo `status`: `paid` → `Paid`;
  `cancel_payment_flag = 1` → `Canceled`; `unpaid` con `end_date_time < now()` → `Expired`; resto →
  `Pending`.
- `isCallingFromBookingDetail` (`1`/`0`/`NULL`) → `origin` (`NULL` legacy → `Unknown`).
- `method_flag`/`email_sent` (canale ed esito invio) esportati verso il log messaggi C6, non migrati qui.
- Creazione del link: `paymentLinks.create` Stripe → salvataggio `stripe_payment_link_id` nella stessa
  transazione applicativa; se la chiamata fallisce, la riga resta `Pending` senza link (retry).
- Webhook `checkout.session.completed` deve essere idempotente su `event.id` (nel legacy questo branch ne
  era del tutto privo — [`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md)); job
  `ExpirePaymentRequestLinks` porta a `Expired` + eventuale `PaymentRequestExpired`.
