# Tabella `payment_ledger_entries`

> **Dominio**: C3 — `payment` · **sottoarea**: `ledger` · **schema**: `payment` · **nome flat**:
> `payment_ledger_entries` · **forma canonica**: `payment.ledger_entries`.
>
> È la tabella `payment_transactions` prevista da [`docs/03-gestione-dati.md` §4](../../../progettazione/03-gestione-dati.md).
> Sostituisce/assorbe: [`transactions_payments`](../../../analisi-pre/db/transactions_payments.md) (2.539
> righe, prototipo del ledger normalizzato), i **singoli movimenti** oggi annidati nei JSON
> [`bookings.booking_payments`](../../../analisi-pre/db/bookings.md) (`paidAmountData[]`,
> `addPaidAmountData[]`, `depositList[]`, `getCustExpenseData[]`) e
> [`service_reservation.payments`](../../../analisi-pre/db/service_reservation.md), e la **fetta
> economica** di [`hotel_payments`](../../../analisi-pre/db/hotel_payments.md)
> (`amount` / `paymentStatus` / `paymentMethod`). Fonti:
> [`analisi-pre/db/transactions_payments.md`](../../../analisi-pre/db/transactions_payments.md),
> [`analisi-pre/db/booking_payments.md`](../../../analisi-pre/db/booking_payments.md),
> [`analisi-pre/db/hotel_payments.md`](../../../analisi-pre/db/hotel_payments.md),
> [`docs/03-gestione-dati.md` §2.4bis, §3.7, §4, §6](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un movimento di incasso** (o acconto, spesa cliente, rimborso, rettifica) relativo a una
prenotazione. **Append-only**: il saldo di una prenotazione è la proiezione di queste righe verso C1
(`paid` / `balance_due`), aggiornata a evento (`PaymentRecorded` / `RefundIssued`). È il "come si è
incassato": ogni movimento porta **imponibile + IVA separati**, la modalità, la data, l'origine
(cassa manuale / gateway online / link / POS / borsellino) e lo snapshot fiscale del cliente. La
scomposizione **per articolo** (per l'export contabile esterno) è nella figlia
[`payment_ledger_entry_lines`](payment_ledger_entry_lines.md).

Correzioni rispetto al legacy:

- **Movimenti dentro i JSON** `paidAmountData`/`depositList`/… → righe tipizzate; in C1 resta solo il
  *riepilogo* ([`docs/03 §4`, §6](../../../progettazione/03-gestione-dati.md)).
- **`transactions_payments` full-replace per prenotazione** (`destroy` + `bulkCreate`, id nuovi ogni
  modifica) → **append-only**, righe di rettifica invece di riscrittura.
- **`booking_payments.total_amount` `float`** con valori impossibili (`~2,1e9`, negativi) →
  `numeric(12,2)` con bonifica ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **IVA cablata 22%** se `percentIVA` assente → `vat_rate` esplicito, obbligatorio.
- **`payment_mode` drift** (`NULL` 67%, `""`, `"Credit card"`, `"Cash"`, `"Bank Transfer"`, `"Other"`) →
  `enum payment.payment_mode` con normalizzazione ([`docs/03 §2.4bis`](../../../progettazione/03-gestione-dati.md)).
- **`type` con 3 codifiche** (`1`/`2` a DB, `1`/`0` in `creditDetails`) e `transactions_payments.type`
  `enum('deposit','payment')` → un solo `entry_type` enum.
- **Popolamento best-effort non bloccante** (`.catch(console.error)`) → scrittura **nella stessa
  transazione** del comando + outbox ([`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- **`hotel_payments.amount`/`assignedPrice` = `0.00` sul 100%** (voucher a copertura totale) → riga
  creata **solo** quando c'è un incasso reale; lo *snapshot listino* resta in C1
  ([`booking_agreement_lines`](../booking/booking_agreement_lines.md)).
- **Snapshot cliente denormalizzato senza FK** (`customerFullName`/`Mobile`/`Email`) → `customer_public_id`
  (FK viva C5) **+** `*_snapshot` congelati sui fatti fiscali ([`docs/03 §3.6`](../../../progettazione/03-gestione-dati.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_ledger_entries` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | riferito da C4 (righe di pagamento dello scontrino) e dall'export contabile |
| `ix_..._reservation` | `(reservation_public_id, occurred_at)` | btree | ricostruzione saldo / estratto della prenotazione |
| `ix_..._property_date` | `(property_id, occurred_at DESC)` | btree | libro incassi per intervallo (ex `creditDetails`) |
| `ix_..._property_mode_date` | `(property_id, payment_mode, occurred_at)` | btree | totali per modalità di pagamento |
| `ix_..._source_ref` | `(source, source_ref_id)` | btree | risalita al gateway/POS/wallet che ha originato il movimento |
| `ix_..._agreement_line` | `(agreement_line_public_id) WHERE agreement_line_public_id IS NOT NULL` | btree parziale | incassi legati a una voce convenzione |


> ### ✅ Applicato nel backend (2026-09-21)
>
> La colonna **`gross_amount`** è ora `GENERATED ALWAYS AS (net_amount + vat_amount) STORED` ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §5.1),
> migration `AddDbOptimizationsPropertyIdAndGeneratedColumns`.
>
> **È un cambio di contratto, non solo di schema**: PostgreSQL *rifiuta* un `INSERT`/`UPDATE` che
> valorizzi esplicitamente la colonna. Il codice applicativo che oggi la scrive va rimosso, non
> adattato. Verificato prima di convertire: le rettifiche contabili sono un **nuovo movimento** con `entry_kind = 'adjustment'`, mai un `UPDATE` dell'importo esistente — quindi non esiste un percorso che debba scrivere questo valore a mano.

## Campi

### id
- **Legacy**: `transactions_payments.id` / elemento del JSON `paidAmountData[]`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `payment_ledger_entry_lines.entry_id`.

### public_id
- **Legacy**: nessuno
- **Descrizione**: id pubblico stabile, **copiato cross-modulo** (C4: righe `payments[]` dello scontrino;
  export contabile esterno).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `transactions_payments.propertyId` / `booking_payments.property_id`
- **Descrizione**: stabilimento che incassa.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); [D-01](../_domande-committente.md).

### reservation_public_id
- **Legacy**: `transactions_payments.bookingId` / `serviceReservationId` (alternativi) / `booking_payments.booking_id`
- **Descrizione**: prenotazione a cui il movimento si riferisce (testata unica C1).
- **Tipo**: `uuid` (= `public_id` di `booking_reservations`).
- **Default**: nessuno.
- **Nullabile**: Sì (movimento non legato a una prenotazione — raro).
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_reservations`](../booking/booking_reservations.md).

### agreement_line_public_id
- **Legacy**: `hotel_payments` (riga)
- **Descrizione**: valorizzato quando il movimento riguarda una **voce di convenzione/voucher hotel**
  con addebito reale.
- **Tipo**: `uuid` (= `public_id` di `booking_agreement_lines`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_agreement_lines`](../booking/booking_agreement_lines.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.540 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### entry_type
- **Legacy**: `transactions_payments.type` (`deposit`/`payment`) + `booking_payments.type` + `getCustExpenseData`
- **Descrizione**: natura del movimento.
- **Tipo**: `enum payment.ledger_entry_type` — valori: `deposit` (acconto — ex `depositList[]`),
  `payment` (incasso — ex `paidAmountData[]`), `expense` (spesa cliente addebitata — ex
  `getCustExpenseData[]`), `refund` (rimborso — collegato a
  [`payment_transaction_refunds`](payment_transaction_refunds.md)), `adjustment` (rettifica manuale al
  posto della riscrittura legacy).
- **Default**: `payment`.
- **Nullabile**: No.
- **Relazioni**: `refund` ha importo negativo o `direction = out` (vedi `direction`).

### direction
- **Legacy**: implicito (segno di `amount`, incoerente)
- **Descrizione**: verso del denaro rispetto allo stabilimento.
- **Tipo**: `enum payment.ledger_direction` — valori: `in` (incasso), `out` (rimborso / storno).
- **Default**: `in`.
- **Nullabile**: No.
- **Relazioni**: `entry_type = refund` ⇒ `out`; tutti gli importi restano **positivi** (`CHECK`), il
  segno lo dà `direction`.

### payment_mode
- **Legacy**: `booking_payments.payment_mode` / `service_reservation_payments.payment_mode` / `hotel_payments.paymentMethod`
- **Descrizione**: modalità con cui è avvenuto il movimento.
- **Tipo**: `enum payment.payment_mode` — valori: `cash`, `credit_card`, `bank_transfer`, `other`.
  Vocabolario **condiviso** con C1 (`booking.payment_mode`).
- **Default**: `other`.
- **Nullabile**: Sì (movimenti legacy con `NULL`/`""` non normalizzabili → `NULL`).
- **Relazioni**: normalizzazione del drift in migrazione (`""` → `NULL`, `"Bank Transfer"` →
  `bank_transfer`).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.540 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### source
- **Legacy**: implicito (tabella/JSON di provenienza)
- **Descrizione**: da dove nasce il movimento.
- **Tipo**: `enum payment.ledger_source` — valori: `manual` (cassa/gestionale), `online_gateway`
  (incasso Stripe — [`payment_transaction_charges`](payment_transaction_charges.md)), `payment_link`
  ([`payment_request_links`](payment_request_links.md)), `pos_ofsale`, `pos_teamsystem`
  ([`payment_pos_charges`](payment_pos_charges.md)), `wallet`
  ([`payment_wallet_transactions`](payment_wallet_transactions.md)), `agreement` (convenzione hotel).
- **Default**: `manual`.
- **Nullabile**: No.
- **Relazioni**: con `source_ref_id` risale alla riga di origine (FK interna, tipo secondo `source`).

### source_ref_id
- **Legacy**: nessuno (nessun collegamento)
- **Descrizione**: id (interno) della riga di origine nello schema `payment` (charge, request link, pos
  charge, wallet transaction, refund).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`source = manual` → `NULL`).
- **Relazioni**: **FK interna** allo schema `payment`, tabella determinata da `source`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.540 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### gross_amount
- **Legacy**: `transactions_payments` (`itemNetAmount` + `itemVatAmount`) / `booking_payments.paid_amount` (`float`)
- **Descrizione**: importo lordo del movimento (IVA inclusa).
- **Tipo**: `numeric(12,2)`, `GENERATED ALWAYS AS (net_amount + vat_amount) STORED`.
- **Default**: — (colonna generata).
- **Nullabile**: No.
- **Relazioni**: `CHECK (gross_amount > 0)` (`ck_payment_ledger_entries_gross_amount`); `= net_amount +
  vat_amount`, calcolata dal DB (riquadro in «Indici»). ⚠️ Il CHECK era andato perso quando la colonna è
  diventata generata (Npgsql la ricrea con `DROP` + `ADD` e il vincolo se ne va con lei, senza errori): è
  tornato con il consolidamento delle migration del 2026-10-06 —
  [`docs/42-migration.md` §10](../../../progettazione/42-migration.md).

### net_amount
- **Legacy**: `transactions_payments.itemNetAmount` (`decimal(10,2)`, `calculateVat(gross, vat)`)
- **Descrizione**: imponibile del movimento (`gross / (1 + vat_rate/100)`).
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### vat_amount
- **Legacy**: `transactions_payments.itemVatAmount` (`gross − net`)
- **Descrizione**: quota IVA del movimento.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### vat_rate
- **Legacy**: `percentIVA` dal JSON servizi / **22 cablato** per i pacchetti
- **Descrizione**: aliquota IVA % applicata al movimento. Quando il movimento copre righe ad aliquota
  mista, è l'aliquota **prevalente** e il dettaglio sta in `payment_ledger_entry_lines`.
- **Tipo**: `numeric(5,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: da riconciliare col vocabolario aliquote di C4 in fase implementativa.

### currency
- **Legacy**: nessuno
- **Descrizione**: valuta ISO 4217.
- **Tipo**: `char(3)`.
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: —.

### customer_public_id
- **Legacy**: nessuno (solo snapshot testuali)
- **Descrizione**: cliente del movimento — **riferimento vivo** a C5.
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_customer_customers`](../identity/identity_customer_customers.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.540 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### customer_name_snapshot / customer_email_snapshot / customer_phone_snapshot
- **Legacy**: `transactions_payments.customerFullName` / `customerEmail` / `customerMobile`
- **Descrizione**: dati cliente **congelati** al momento del movimento (fatto fiscale: devono restare
  anche se l'anagrafica cambia o viene pseudonimizzata).
- **Tipo**: `varchar(160)` / `varchar(180)` / `varchar(40)`.
- **Default**: nessuno.
- **Nullabile**: Sì / Sì / Sì.
- **Relazioni**: snapshot; accanto al riferimento vivo `customer_public_id` ([`docs/03 §3.6`](../../../progettazione/03-gestione-dati.md)).

### recorded_by_user_id
- **Legacy**: nessuno (movimento da JSON, nessun operatore tracciato)
- **Descrizione**: operatore che ha registrato il movimento manuale (audit fiscale).
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì (`source ≠ manual` → `NULL`).
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.540 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### occurred_at
- **Legacy**: `transactions_payments.transactionDate` (`deposit.depositDate` / `payment.date` dal JSON; fallback `new Date()`)
- **Descrizione**: data/ora contabile del movimento.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: decorrenza retention 10 anni (24 mesi online, poi archivio S3 — [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md)).

### stay_from / stay_to
- **Legacy**: `transactions_payments.reservationStartDate` / `reservationEndDate` (`date`)
- **Descrizione**: intervallo della prenotazione/servizio (denormalizzato per l'export contabile e i
  filtri per stagione).
- **Tipo**: `date` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: copiati da C1.

### created_at / updated_at / deleted_at
- **Legacy**: `transactions_payments.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica. `deleted_at` esposto per uniformità: **append-only** (scrittura
  contabile — una correzione è una nuova riga `adjustment`, mai un update/delete).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at` (sempre `NULL`).

## Valutazione JSON

**Molti JSON legacy diventano righe qui.** `booking_payments.paidAmountData[]` / `addPaidAmountData[]` /
`depositList[]` / `getCustExpenseData[]` e `service_reservation.payments.*` → 1 riga per elemento.
`transactions_payments` è già relazionale (enum `type`, `decimal`, net/vat) → mappatura quasi diretta.
Nessun `jsonb` nel nuovo schema: il dettaglio per articolo è la figlia
[`payment_ledger_entry_lines`](payment_ledger_entry_lines.md).

## Suggerimenti per le prestazioni

- Tabella che cresce con ogni incasso: partizionare per anno di `occurred_at`; l'archiviazione a 24 mesi
  ([`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md)) tiene "calda" solo la parte recente.
- Le viste "crediti / incassi / non pagati" (ex `creditDetails` con `JOIN bookings ↔ booking_payments`)
  non toccano più questa tabella per il saldo: leggono la **proiezione** su C1
  (`booking_reservations.paid` / `balance_due`). Qui si legge solo per l'**estratto** di una
  prenotazione o per i totali contabili per intervallo — coperti dagli indici `(reservation_public_id,
  occurred_at)` e `(property_id, occurred_at)`.
- Indice `(property_id, payment_mode, occurred_at)` per i report "incassato per modalità" senza
  `GROUP BY` a runtime sul JSON (era il pattern legacy).
- `net_amount`/`vat_amount` materializzati: nessun ricalcolo `calculateVat` a ogni lettura.

## Implementazione .NET

`LedgerEntry` è l'**aggregate root** dell'area `Ledger` in `Module.Payment`, schema `payment`:
append-only, 1 riga per movimento di incasso/acconto/spesa/rimborso/rettifica. Espone la collezione
owned `Lines` ([`payment_ledger_entry_lines`](payment_ledger_entry_lines.md)). Ogni comando che registra
un incasso scrive **una** `LedgerEntry` nella propria transazione ed emette `PaymentRecorded` (outbox) →
C1 aggiorna `paid`/`balance_due`/`prevalent_payment_mode`, C4 valuta l'emissione dello scontrino.

### Enum propri

`payment.payment_mode` è una **copia per-schema** dell'enum di `booking.payment_mode` (i tipi `ENUM`
PostgreSQL sono per-schema, non condivisibili cross-schema — vedi
[`booking_reservations.md` → Enum propri](../booking/booking_reservations.md)): stesso significato, stesso
set di valori, tipo PG e classe C# distinti.

```csharp
// Module.Payment.Domain.Enums.LedgerEntryType
public enum LedgerEntryType
{
	Deposit = 0,
	Payment = 1,
	Expense = 2,
	Refund = 3,
	Adjustment = 4
}

// Module.Payment.Domain.Enums.LedgerDirection
public enum LedgerDirection
{
	In = 0,
	Out = 1
}

// Module.Payment.Domain.Enums.PaymentMode — copia per-schema di Module.Booking.Domain.Enums.PaymentMode
public enum PaymentMode
{
	Cash = 0,
	CreditCard = 1,
	BankTransfer = 2,
	Other = 3
}

// Module.Payment.Domain.Enums.LedgerSource
public enum LedgerSource
{
	Manual = 0,
	OnlineGateway = 1,
	PaymentLink = 2,
	PosOfsale = 3,
	PosTeamsystem = 4,
	Wallet = 5,
	Agreement = 6
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<LedgerEntryType>("payment", "ledger_entry_type");
modelBuilder.HasPostgresEnum<LedgerDirection>("payment", "ledger_direction");
modelBuilder.HasPostgresEnum<PaymentMode>("payment", "payment_mode");
modelBuilder.HasPostgresEnum<LedgerSource>("payment", "ledger_source");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<LedgerEntryType>("payment.ledger_entry_type");
dataSourceBuilder.MapEnum<LedgerDirection>("payment.ledger_direction");
dataSourceBuilder.MapEnum<PaymentMode>("payment.payment_mode");
dataSourceBuilder.MapEnum<LedgerSource>("payment.ledger_source");
```

### `LedgerEntryConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
	public void Configure(EntityTypeBuilder<LedgerEntry> builder)
	{
		builder.ToTable("ledger_entries", "payment", t =>
		{
			t.HasComment("Movimento di incasso/acconto/spesa/rimborso/rettifica relativo a una prenotazione; append-only.");
			t.HasCheckConstraint("ck_payment_ledger_entries_gross_amount", "gross_amount > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_ledger_entries");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo (C4 righe payments[] dello scontrino; export contabile).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento che incassa.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione del movimento.");

		builder.Property(x => x.AgreementLinePublicId)
			.HasColumnName("agreement_line_public_id")
			.HasComment("public_id di booking.booking_agreement_lines, copiato — nessuna FK cross-schema; valorizzato per addebiti reali su voucher hotel.");

		builder.Property(x => x.EntryType)
			.HasColumnName("entry_type")
			.HasColumnType("payment.ledger_entry_type")
			.HasDefaultValue(LedgerEntryType.Payment)
			.IsRequired()
			.HasComment("Natura del movimento: acconto, incasso, spesa cliente, rimborso o rettifica.");

		builder.Property(x => x.Direction)
			.HasColumnName("direction")
			.HasColumnType("payment.ledger_direction")
			.HasDefaultValue(LedgerDirection.In)
			.IsRequired()
			.HasComment("Verso del denaro rispetto allo stabilimento; gli importi restano sempre positivi.");

		builder.Property(x => x.PaymentMode)
			.HasColumnName("payment_mode")
			.HasColumnType("payment.payment_mode")
			.HasDefaultValue(PaymentMode.Other)
			.HasComment("Modalità con cui è avvenuto il movimento; NULL sui movimenti legacy non normalizzabili.");

		builder.Property(x => x.Source)
			.HasColumnName("source")
			.HasColumnType("payment.ledger_source")
			.HasDefaultValue(LedgerSource.Manual)
			.IsRequired()
			.HasComment("Da dove nasce il movimento (cassa manuale, gateway online, link, POS, borsellino, convenzione).");

		builder.Property(x => x.SourceRefId)
			.HasColumnName("source_ref_id")
			.HasComment("Id interno della riga di origine nello schema payment (charge, request link, pos charge, wallet transaction), tipo determinato da source; NULL se source = manual.");

		builder.Property(x => x.GrossAmount)
			.HasColumnName("gross_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo lordo del movimento, IVA inclusa (= net_amount + vat_amount).");

		builder.Property(x => x.NetAmount)
			.HasColumnName("net_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Imponibile del movimento.");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Quota IVA del movimento.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota IVA % applicata; se il movimento copre aliquote miste è la prevalente, il dettaglio sta nelle Lines.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217.");

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.HasComment("public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente del movimento (riferimento vivo).");

		builder.Property(x => x.CustomerNameSnapshot)
			.HasColumnName("customer_name_snapshot")
			.HasMaxLength(160)
			.HasComment("Nome cliente congelato al momento del movimento (fatto fiscale), accanto al riferimento vivo customer_public_id.");

		builder.Property(x => x.CustomerEmailSnapshot)
			.HasColumnName("customer_email_snapshot")
			.HasMaxLength(180)
			.HasComment("Email cliente congelata al momento del movimento.");

		builder.Property(x => x.CustomerPhoneSnapshot)
			.HasColumnName("customer_phone_snapshot")
			.HasMaxLength(40)
			.HasComment("Telefono cliente congelato al momento del movimento.");

		builder.Property(x => x.RecordedByUserId)
			.HasColumnName("recorded_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha registrato il movimento manuale (audit fiscale).");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora contabile del movimento, decorrenza retention.");

		builder.Property(x => x.StayFrom)
			.HasColumnName("stay_from")
			.HasComment("Inizio del soggiorno/servizio, denormalizzato per export contabile e filtri stagionali.");

		builder.Property(x => x.StayTo)
			.HasColumnName("stay_to")
			.HasComment("Fine del soggiorno/servizio, denormalizzato per export contabile e filtri stagionali.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only (una correzione è una nuova riga adjustment, sempre NULL).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_ledger_entries_public_id");

		builder.HasIndex(x => new { x.ReservationPublicId, x.OccurredAt })
			.HasDatabaseName("ix_payment_ledger_entries_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.OccurredAt })
			.HasDatabaseName("ix_payment_ledger_entries_property_date");

		builder.HasIndex(x => new { x.PropertyId, x.PaymentMode, x.OccurredAt })
			.HasDatabaseName("ix_payment_ledger_entries_property_mode_date");

		builder.HasIndex(x => new { x.Source, x.SourceRefId })
			.HasDatabaseName("ix_payment_ledger_entries_source_ref");

		builder.HasIndex(x => x.AgreementLinePublicId)
			.HasFilter("agreement_line_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_ledger_entries_agreement_line");

		builder.OwnsMany(x => x.Lines, lines => lines.ToTable("ledger_entry_lines", "payment"));
	}
}
```

### Relazioni

`property_id`, `reservation_public_id`, `agreement_line_public_id`, `customer_public_id`,
`recorded_by_user_id` sono `uuid` copiati da C2/C1/C5: solo `.HasComment`, nessuna FK cross-schema.
`source_ref_id` non è una FK EF tipizzata (la tabella di destinazione varia con `source`): solo
`.HasComment`, risoluzione applicativa. Referenziata (FK interna) da
`payment_ledger_entry_lines.entry_id` — vedi [`payment_ledger_entry_lines.md`](payment_ledger_entry_lines.md)
per il dettaglio della collezione owned `Lines`.

`ISoftDeletable` implementato solo per uniformità (sempre `NULL`, append-only); `IAuditable` implementato.

### Note di migrazione

- Esplodere i JSON `bookings.booking_payments` (`paidAmountData[]` → `Payment`, `depositList[]` →
  `Deposit`, `getCustExpenseData[]` → `Expense`) e `service_reservation.payments` in righe distinte.
- Importare `transactions_payments` 1:1 (net/vat già presenti, mappatura quasi diretta).
- Da `hotel_payments` importare **solo** le righe con `amount > 0` (`source = Agreement`); il resto (voucher
  a copertura totale) resta snapshot di listino in C1.
- Bonifica outlier su `booking_payments.total_amount` (`float`, valori impossibili tipo `~2,1e9`,
  negativi) in fase di cast a `numeric`.
- `payment_mode` normalizzato: `NULL`/`""` → `NULL`; `"Bank Transfer"` → `BankTransfer`; casing libero →
  valore enum.
- [D-35](../_domande-committente.md) resta aperta: la ripartizione proporzionale legacy
  (`transactions_payments` spalma ogni movimento su tutte le righe-articolo) — mantenerla come stima
  (`is_allocated = true` sulle Lines) o registrare l'aggregato + Lines solo quando i dati reali lo
  consentono.
