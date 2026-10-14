# Tabella `payment_transaction_charges`

> **Dominio**: C3 — `payment` · **sottoarea**: `transaction` · **schema**: `payment` · **nome flat**:
> `payment_transaction_charges` · **forma canonica**: `payment.transaction_charges`.
>
> Sostituisce [`online_transaction_details`](../../../analisi-pre/db/online_transaction_details.md)
> (38.240 righe reali; nome file/ORM con refuso "Taransaction"). Fonti:
> [`analisi-pre/db/online_transaction_details.md`](../../../analisi-pre/db/online_transaction_details.md),
> [`docs/03-gestione-dati.md` §2.4bis, §3.7, §4](../../../progettazione/03-gestione-dati.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §6](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un PaymentIntent Stripe** (o una registrazione "paga in struttura"). Traccia il livello
**gateway** dell'incasso online: importo pagato / trattenuto (hold, cattura manuale) / residuo,
commissione di piattaforma, stato Stripe grezzo + stato interno derivato. È l'ancora dei webhook e dei
rimborsi ([`payment_transaction_refunds`](payment_transaction_refunds.md)); il *movimento contabile*
corrispondente è una riga di [`payment_ledger_entries`](payment_ledger_entries.md) (`source =
online_gateway`).

Correzioni rispetto al legacy:

- **Tutti gli importi come `varchar`** (`paid_payment`, `hold_payment`, `pending_payment`,
  `application_fee_amount`) → `numeric(12,2)` ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **`propertyId` / `booking_id` / `reservation_id` come `varchar`**, `reservation_id = ""` invece di
  `NULL` → `uuid` copiati, `NULL` reale.
- **`status` passthrough Stripe con casing incoerente** (`succeeded` / `Uncaptured`) → `status` enum
  **interno** + `stripe_status` grezzo in colonna dedicata ([`docs/03 §2.4bis`](../../../progettazione/03-gestione-dati.md)).
- **`booking_type` non documentato** (`"1"` / `"2"` / `NULL`) → `subject_kind` enum allineato a
  `reservation_kind` di C1.
- **`payment_type`** (`pay_now` / `advance_balance` / `pay_at_property`) → **enum** (valori esclusivi).
- **`isIframe` int** → `sales_channel` enum (guida la scelta del tasso commissione, [`payment_gateway_commission_settings`](payment_gateway_commission_settings.md)).
- **`sync({ alter: true })` all'avvio** su tabella di produzione → migrazioni versionate.
- **Nessun indice** su `stripe_transaction_id` / `booking_id` / `reservation_id` (letti a ogni
  webhook/refund → full scan) → indici dedicati ([`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- **~10 `create` quasi identiche in `stripe.controller.js`** → un solo percorso di creazione.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_transaction_charges` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | referenziato da C4 (autoreceipt) e in URL gestionale |
| `uq_..._payment_intent` | `(stripe_payment_intent_id) WHERE stripe_payment_intent_id IS NOT NULL` | UNIQUE parziale | idempotenza webhook / lookup rimborso |
| `ix_..._reservation` | `(reservation_public_id)` | btree | pagamenti di una prenotazione |
| `ix_..._property_date` | `(property_id, created_at DESC)` | btree | lista "transazioni online" del gestionale |
| `ix_..._status` | `(property_id, status) WHERE status IN ('pending','authorized')` | btree parziale | riconciliazione / hold da catturare o scadere |
| `ix_..._connected_account` | `(connected_account_id)` | btree | estratto per connected account |


> ### ✅ Applicato nel backend (2026-09-21)
>
> La colonna **`gross_amount`** è ora `GENERATED ALWAYS AS (paid_amount + pending_amount + hold_amount) STORED` ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §5.1),
> migration `AddDbOptimizationsPropertyIdAndGeneratedColumns`.
>
> **È un cambio di contratto, non solo di schema**: PostgreSQL *rifiuta* un `INSERT`/`UPDATE` che
> valorizzi esplicitamente la colonna. Il codice applicativo che oggi la scrive va rimosso, non
> adattato. Verificato: la tabella è append/stato e non si soft-elimina; i rimborsi sono righe separate in [`payment_transaction_refunds`](payment_transaction_refunds.md), non modifiche di questa riga.

## Campi

### id
- **Legacy**: `online_transaction_details.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `payment_transaction_refunds.charge_id` e
  `payment_ledger_entries.gateway_charge_id`.

### public_id
- **Legacy**: nessuno
- **Descrizione**: id pubblico stabile, **copiato cross-modulo** (C4 per l'emissione automatica dello
  scontrino dopo il pagamento Stripe; gestionale in URL).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`; copiato da C4.

### property_id
- **Legacy**: `online_transaction_details.propertyId` (`varchar`)
- **Descrizione**: stabilimento che incassa.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); [D-01](../_domande-committente.md).

### connected_account_id
- **Legacy**: `online_transaction_details.stripe_connected_id` (`varchar`, = `sellerStripeConnectId`)
- **Descrizione**: connected account su cui è avvenuto l'incasso.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per le righe "paga in struttura", che non passano da Stripe).
- **Relazioni**: **FK interna** → [`payment_gateway_connected_accounts.id`](payment_gateway_connected_accounts.md).
- **Stato dopo la migrazione (2026-10-01)**: **non è più vuota** dal 2026-10-01: la popola il passo `payment_transaction_charges_connected_account` agganciando `stripe_account_id_snapshot` all'account Stripe migrato (28.724 righe su 29.387; le altre hanno uno snapshot che non corrisponde a nessun account a catalogo e restano NULL correttamente). Era NULL perché la sorgente legacy è l'id Stripe *testuale* e il lookup standard risolve solo per id legacy numerico. Voce M6 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### stripe_account_id_snapshot
- **Legacy**: `online_transaction_details.stripe_connected_id`
- **Descrizione**: id `acct_…` **congelato** al momento dell'incasso — regola `_snapshot`: identificativo
  copiato da un'entità mutabile ([`payment_gateway_connected_accounts`](payment_gateway_connected_accounts.md),
  la cui riga può essere sostituita con un nuovo `acct_` a un ricollegamento) con la FK viva
  `connected_account_id` accanto ([`_feature-tabelle.md` → C1-7](../_feature-tabelle.md)). Serve a sapere
  **su quale account Stripe** è finito il denaro anche dopo un ricollegamento, e a correlare con la
  dashboard Stripe.
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per "paga in struttura").
- **Relazioni**: snapshot di `payment_gateway_connected_accounts.stripe_account_id`.

### reservation_public_id
- **Legacy**: `online_transaction_details.booking_id` **o** `reservation_id` (`varchar`, alternativi)
- **Descrizione**: prenotazione pagata. C1 ha unificato testata postazioni + servizi in
  `booking_reservations` → un solo campo.
- **Tipo**: `uuid` (= `public_id` di `booking_reservations`).
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` finché il link/pagamento non è agganciato a una prenotazione).
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_reservations`](../booking/booking_reservations.md).

### subject_kind
- **Legacy**: `online_transaction_details.booking_type` (`"1"` postazione / `"2"` servizi / `NULL`)
- **Descrizione**: cosa si sta pagando. Allineato a `PaymentSubjectType` / `booking_reservations.reservation_kind`.
- **Tipo**: `enum payment.transaction_subject` — valori: `reservation` (prenotazione con postazioni),
  `service_reservation` (prenotazione di soli servizi).
- **Default**: `reservation`.
- **Nullabile**: Sì (`NULL` sulle righe "paga in struttura" legacy che non lo valorizzano).
- **Relazioni**: —.

### payment_type
- **Legacy**: `online_transaction_details.payment_type` (`pay_now` / `advance_balance` / `pay_at_property`)
- **Descrizione**: modalità di pagamento online scelta. Valori **esclusivi** → enum.
- **Tipo**: `enum payment.online_payment_type` — valori: `pay_now` (tutto subito), `advance_balance`
  (acconto ora + hold/saldo dopo), `pay_at_property` (paga in struttura — nessun incasso online).
- **Default**: `pay_now`.
- **Nullabile**: No.
- **Relazioni**: i rimborsi sono ammessi solo per `pay_now` / `advance_balance` (come nel legacy).

### sales_channel
- **Legacy**: `online_transaction_details.isIframe` (`int` 0/1)
- **Descrizione**: canale da cui è partito il pagamento — determina quale tasso commissione applicare.
- **Tipo**: `enum payment.sales_channel` — valori: `redirect_checkout` (sito pubblico / Checkout),
  `iframe` (widget imbottito su sito terzo).
- **Default**: `redirect_checkout`.
- **Nullabile**: No.
- **Relazioni**: usato con [`payment_gateway_commission_settings`](payment_gateway_commission_settings.md)
  (`online_rate` vs `iframe_rate`, `exemption = iframe_only`).

### status
- **Legacy**: `online_transaction_details.status` (passthrough Stripe, casing misto)
- **Descrizione**: stato **interno** della transazione, derivato dallo stato Stripe.
- **Tipo**: `enum payment.charge_status` — valori: `pending`, `authorized` (hold attivo, non catturato —
  ex `"Uncaptured"`), `succeeded`, `failed`, `canceled`, `refunded`, `partially_refunded`.
- **Default**: `pending`.
- **Nullabile**: No.
- **Relazioni**: `authorized` → cattura porta a `succeeded` spostando `hold_amount` in `paid_amount`;
  scadenza hold → `canceled`.

### stripe_status
- **Legacy**: `online_transaction_details.status` (valore grezzo)
- **Descrizione**: stato **grezzo** del PaymentIntent Stripe conservato verbatim (`requires_capture`,
  `succeeded`, `processing`, …) — snapshot per diagnostica/riconciliazione, mai normalizzato.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: da `status` interno separato ([`docs/03 §2.4bis`](../../../progettazione/03-gestione-dati.md)).

### stripe_payment_intent_id
- **Legacy**: `online_transaction_details.stripe_transaction_id` (`pi_…`; usato come `charge` nel refund — bug)
- **Descrizione**: id del PaymentIntent Stripe.
- **Tipo**: `varchar(255)` (progettato `varchar(40)`, allargato il 2026-09-15 dalla migration
  `WidenStripeIdColumns`: il legacy ha dimostrato che qui può finire anche un id di Checkout Session
  `cs_live_…` di 66+ caratteri invece di un `pi_…` — 190 righe venivano scartate a `NULL` dalla
  migrazione perché il `varchar(40)` le rifiutava).
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per "paga in struttura").
- **Relazioni**: `UNIQUE` parziale; il rimborso usa l'id del **Charge** (`ch_…`, vedi sotto), non
  questo.

### stripe_charge_id
- **Legacy**: nessuno (il legacy passava per errore il `pi_…` dove Stripe voleva un `ch_…`)
- **Descrizione**: id del Charge Stripe (`ch_…`) associato al PaymentIntent — il valore corretto per
  `stripe.refunds.create({ charge })`.
- **Tipo**: `varchar(255)` (allargato insieme a `stripe_payment_intent_id`, stessa famiglia di id Stripe).
- **Default**: `NULL`.
- **Nullabile**: Sì (valorizzato dal webhook `charge.succeeded` / `payment_intent.succeeded`).
- **Relazioni**: usato da `payment_transaction_refunds`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 29.387 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### gross_amount
- **Legacy**: derivato (`paid_payment` + `application_fee_amount` in alcuni rami)
- **Descrizione**: importo lordo dell'operazione (quanto paga il cliente).
- **Tipo**: `numeric(12,2)`, `GENERATED ALWAYS AS (paid_amount + pending_amount + hold_amount) STORED`.
- **Default**: — (colonna generata; il `DEFAULT 0` progettato non è ammesso su una colonna generata).
- **Nullabile**: No.
- **Relazioni**: `gross = paid_amount + pending_amount + hold_amount`, calcolata dal DB (riquadro in
  «Indici»): un `INSERT`/`UPDATE` che la valorizzi viene rifiutato.

### paid_amount
- **Legacy**: `online_transaction_details.paid_payment` (`varchar`)
- **Descrizione**: importo effettivamente **incassato** (catturato).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: alla cattura, `hold_amount` → `paid_amount`.

### hold_amount
- **Legacy**: `online_transaction_details.hold_payment` (`varchar`)
- **Descrizione**: importo **autorizzato con cattura manuale** (fondi bloccati sulla carta, non ancora
  incassati). Casi: acconto+saldo, garanzia.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: se l'hold scade/annulla, i fondi tornano al cliente senza movimento contabile.

### pending_amount
- **Legacy**: `online_transaction_details.pending_payment` (`varchar`)
- **Descrizione**: residuo ancora **da pagare** (senza fondi bloccati). Distinto da `hold_amount`.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: per "paga in struttura" contiene `req.body.pending_amount`.

### application_fee_amount
- **Legacy**: `online_transaction_details.application_fee_amount` (`varchar`)
- **Descrizione**: commissione di piattaforma trattenuta (Stripe Connect). Ora **calcolata server-side**
  da [`payment_gateway_commission_settings`](payment_gateway_commission_settings.md).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: `= gross * rate/100 + fixed_fee_amount` (per acconto su `gross/2`); `0` se `exemption`
  copre il canale.

### currency
- **Legacy**: nessuno (implicito `EUR`)
- **Descrizione**: valuta ISO 4217 dell'operazione.
- **Tipo**: `char(3)`.
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: insieme chiuso condiviso (`SharedKernel`, [`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).

### customer_public_id
- **Legacy**: nessuno (il legacy non lega la transazione al cliente)
- **Descrizione**: cliente pagante (per estratti conto e antifrode).
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_customer_customers`](../identity/identity_customer_customers.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 29.387 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### occurred_at
- **Legacy**: `online_transaction_details.transaction_date` (`new Date()` alla creazione)
- **Descrizione**: data/ora dell'evento gateway (creazione PaymentIntent).
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: decorrenza per la retention (24 mesi online → archivio, [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md)).

### captured_at / canceled_at
- **Legacy**: nessuno (si desumeva da `status`)
- **Descrizione**: quando l'hold è stato catturato / la transazione annullata.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 29.387 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `online_transaction_details.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica riga. `deleted_at` esposto per uniformità: la tabella è
  **append/stato**, non si soft-elimina (scrittura contabile, retention 10 anni).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at` (di fatto sempre `NULL`).

## Valutazione JSON

Nessun JSON. Gli importi-stringa legacy diventano `numeric`; lo stato Stripe grezzo è una colonna
dedicata, non un blob.

## Suggerimenti per le prestazioni

- ~38k righe e in crescita: la tabella "calda" del dominio. Gli indici su `stripe_payment_intent_id`
  (idempotenza webhook) e `reservation_public_id` (refund/conferma) eliminano i full scan del legacy
  ([`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- **Idempotenza webhook** basata su `event.id` Stripe persistito in una tabella `payment` di supporto
  (outbox/inbox), **non** sullo stato derivato dell'entità (`status`): un redelivery dopo crash deve
  poter rieseguire ([`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- Partizionamento per anno di `occurred_at` quando lo storico supera qualche milione di righe (retention
  24 mesi online + archivio S3).
- Indice parziale su `status IN ('pending','authorized')` mantiene piccolo l'insieme "hold da gestire".

## Implementazione .NET

`Charge` è l'**aggregate root** dell'area `Transaction` in `Module.Payment`, schema `payment`: 1 riga per
PaymentIntent Stripe (o registrazione "paga in struttura"), ancora dei webhook e dei rimborsi
([`payment_transaction_refunds`](payment_transaction_refunds.md)). Interagisce con Stripe tramite
`Stripe.net` dietro `IPaymentGateway`, mai direttamente dal dominio. Solleva eventi (`ChargeSucceeded`,
`ChargeCaptured`, …) consumati via outbox da C1/C4.

### Enum propri

```csharp
// Module.Payment.Domain.Enums.TransactionSubject
public enum TransactionSubject
{
	Reservation = 0,
	ServiceReservation = 1
}

// Module.Payment.Domain.Enums.OnlinePaymentType
public enum OnlinePaymentType
{
	PayNow = 0,
	AdvanceBalance = 1,
	PayAtProperty = 2
}

// Module.Payment.Domain.Enums.SalesChannel
public enum SalesChannel
{
	RedirectCheckout = 0,
	Iframe = 1
}

// Module.Payment.Domain.Enums.ChargeStatus
public enum ChargeStatus
{
	Pending = 0,
	Authorized = 1,
	Succeeded = 2,
	Failed = 3,
	Canceled = 4,
	Refunded = 5,
	PartiallyRefunded = 6
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<TransactionSubject>("payment", "transaction_subject");
modelBuilder.HasPostgresEnum<OnlinePaymentType>("payment", "online_payment_type");
modelBuilder.HasPostgresEnum<SalesChannel>("payment", "sales_channel");
modelBuilder.HasPostgresEnum<ChargeStatus>("payment", "charge_status");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<TransactionSubject>("payment.transaction_subject");
dataSourceBuilder.MapEnum<OnlinePaymentType>("payment.online_payment_type");
dataSourceBuilder.MapEnum<SalesChannel>("payment.sales_channel");
dataSourceBuilder.MapEnum<ChargeStatus>("payment.charge_status");
```

### `ChargeConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
	public void Configure(EntityTypeBuilder<Charge> builder)
	{
		builder.ToTable("transaction_charges", "payment", t => t.HasComment(
			"Livello gateway dell'incasso online: un PaymentIntent Stripe (o una registrazione paga-in-struttura)."));

		builder.HasKey(x => x.Id).HasName("pk_payment_transaction_charges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo (C4 autoreceipt; URL gestionale).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento che incassa.");

		builder.Property(x => x.ConnectedAccountId)
			.HasColumnName("connected_account_id")
			.HasComment("Connected account su cui è avvenuto l'incasso; NULL per le righe paga-in-struttura.");

		builder.Property(x => x.StripeAccountIdSnapshot)
			.HasColumnName("stripe_account_id_snapshot")
			.HasMaxLength(32)
			.HasComment("Id acct_… congelato al momento dell'incasso, accanto alla FK viva connected_account_id (snapshot su entità mutabile).");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione pagata.");

		builder.Property(x => x.SubjectKind)
			.HasColumnName("subject_kind")
			.HasColumnType("payment.transaction_subject")
			.HasDefaultValue(TransactionSubject.Reservation)
			.HasComment("Cosa si sta pagando: prenotazione con postazioni o di soli servizi.");

		builder.Property(x => x.PaymentType)
			.HasColumnName("payment_type")
			.HasColumnType("payment.online_payment_type")
			.HasDefaultValue(OnlinePaymentType.PayNow)
			.IsRequired()
			.HasComment("Modalità di pagamento online scelta: tutto subito, acconto+saldo, o paga in struttura.");

		builder.Property(x => x.SalesChannel)
			.HasColumnName("sales_channel")
			.HasColumnType("payment.sales_channel")
			.HasDefaultValue(Domain.Enums.SalesChannel.RedirectCheckout)
			.IsRequired()
			.HasComment("Canale di partenza del pagamento, determina il tasso di commissione applicato.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.charge_status")
			.HasDefaultValue(ChargeStatus.Pending)
			.IsRequired()
			.HasComment("Stato interno della transazione, derivato dallo stato Stripe.");

		builder.Property(x => x.StripeStatus)
			.HasColumnName("stripe_status")
			.HasMaxLength(40)
			.HasComment("Stato grezzo del PaymentIntent Stripe conservato verbatim, snapshot per diagnostica/riconciliazione.");

		builder.Property(x => x.StripePaymentIntentId)
			.HasColumnName("stripe_payment_intent_id")
			.HasMaxLength(255)
			.HasComment("Id del PaymentIntent Stripe; NULL per paga-in-struttura.");

		builder.Property(x => x.StripeChargeId)
			.HasColumnName("stripe_charge_id")
			.HasMaxLength(255)
			.HasComment("Id del Charge Stripe (ch_…) associato al PaymentIntent, usato per i rimborsi.");

		builder.Property(x => x.GrossAmount)
			.HasColumnName("gross_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Importo lordo dell'operazione (quanto paga il cliente).");

		builder.Property(x => x.PaidAmount)
			.HasColumnName("paid_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Importo effettivamente incassato (catturato).");

		builder.Property(x => x.HoldAmount)
			.HasColumnName("hold_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Importo autorizzato con cattura manuale, fondi bloccati non ancora incassati.");

		builder.Property(x => x.PendingAmount)
			.HasColumnName("pending_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Residuo ancora da pagare, senza fondi bloccati.");

		builder.Property(x => x.ApplicationFeeAmount)
			.HasColumnName("application_fee_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Commissione di piattaforma trattenuta (Stripe Connect), calcolata server-side.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217 dell'operazione.");

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.HasComment("public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente pagante.");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora dell'evento gateway (creazione PaymentIntent); decorrenza retention.");

		builder.Property(x => x.CapturedAt)
			.HasColumnName("captured_at")
			.HasComment("Quando l'hold è stato catturato.");

		builder.Property(x => x.CanceledAt)
			.HasColumnName("canceled_at")
			.HasComment("Quando la transazione è stata annullata.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: la tabella è append/stato, non si soft-elimina (scrittura contabile, retention 10 anni).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_transaction_charges_public_id");

		builder.HasIndex(x => x.StripePaymentIntentId)
			.IsUnique()
			.HasFilter("stripe_payment_intent_id IS NOT NULL")
			.HasDatabaseName("uq_payment_transaction_charges_payment_intent");

		builder.HasIndex(x => x.ReservationPublicId)
			.HasDatabaseName("ix_payment_transaction_charges_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasDatabaseName("ix_payment_transaction_charges_property_date");

		builder.HasIndex(x => new { x.PropertyId, x.Status })
			.HasFilter("status IN ('pending','authorized')")
			.HasDatabaseName("ix_payment_transaction_charges_status");

		builder.HasIndex(x => x.ConnectedAccountId)
			.HasDatabaseName("ix_payment_transaction_charges_connected_account");

		builder.HasOne(x => x.ConnectedAccount)
			.WithMany()
			.HasForeignKey(x => x.ConnectedAccountId)
			.HasConstraintName("fk_payment_transaction_charges_connected_account")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

### Relazioni

`property_id`, `reservation_public_id`, `customer_public_id` sono `uuid` copiati da C2/C1/C5: solo
`.HasComment`, nessuna FK cross-schema. `connected_account_id` referenzia
[`payment_gateway_connected_accounts`](payment_gateway_connected_accounts.md), stesso schema `payment`:
FK EF reale, nullable (`Restrict`). Referenziata (FK interna, definite sulle rispettive Configuration) da
`payment_transaction_refunds.charge_id` e `payment_ledger_entries.source_ref_id` (quando `source =
online_gateway`).

`ISoftDeletable` è implementato solo per uniformità (`deleted_at` sempre `NULL` in pratica, mai valorizzato
da comandi applicativi — append-only); `IAuditable` implementato.

### Note di migrazione

- `online_transaction_details` → cast `varchar` → `numeric` con bonifica sui 4 importi
  (`paid_payment`/`hold_payment`/`pending_payment`/`application_fee_amount`).
- `status` (casing Stripe misto) → `ChargeStatus` normalizzato (`"Uncaptured"` → `Authorized`) +
  `stripe_status` grezzo conservato verbatim nella colonna dedicata.
- `booking_id`/`reservation_id` (`int`/`""`/`varchar`, alternativi) → `reservation_public_id` unico via
  mappa C1; stringa vuota → `NULL` reale.
- `isIframe` (int 0/1) → `sales_channel` (`RedirectCheckout`/`Iframe`).
- `connected_account_id` ricostruito da `stripe_connected_id` via lookup su
  `payment_gateway_connected_accounts.stripe_account_id`.
- Idempotenza webhook basata su `event.id` Stripe persistito in una tabella di supporto inbox/outbox del
  modulo, non sullo stato derivato `status` ([`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
