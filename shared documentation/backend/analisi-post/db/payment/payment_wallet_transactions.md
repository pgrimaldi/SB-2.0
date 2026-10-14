# Tabella `payment_wallet_transactions`

> **Dominio**: C3 — `payment` · **sottoarea**: `wallet` · **schema**: `payment` · **nome flat**:
> `payment_wallet_transactions` · **forma canonica**: `payment.wallet_transactions`.
>
> Sostituisce [`card_transactions`](../../../analisi-pre/db/card_transactions.md) (708 righe reali;
> `transaction_type` `1` credito × 637 / `2` debito × 70). Fonti:
> [`analisi-pre/db/card_transactions.md`](../../../analisi-pre/db/card_transactions.md),
> [`docs/03-gestione-dati.md` §2.4bis, §3.7, §4](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un movimento del borsellino** ([`payment_wallet_accounts`](payment_wallet_accounts.md)):
ricarica (credito) o consumo (debito). **Append-only**: il saldo del conto è `Σ` di queste righe. Un
consumo legato a servizi bar porta il dettaglio nella figlia
[`payment_wallet_transaction_items`](payment_wallet_transaction_items.md); quando il movimento salda
(anche in parte) una prenotazione, si emette un evento che genera la riga contabile in
[`payment_ledger_entries`](payment_ledger_entries.md) (`source = wallet`).

Correzioni rispetto al legacy:

- **`transaction_type` `1`/`2` con debiti registrati come `type=1` + `amount<0`** → `movement_type` enum
  `credit`/`debit` con `amount` **sempre positivo**; normalizzazione delle righe sporche
  ([`docs/03 §2.4bis`](../../../progettazione/03-gestione-dati.md)).
- **`amount` `decimal(10,2)` nullable** (`NULL` su 21 righe) → `numeric(12,2)` `NOT NULL`, `CHECK > 0`.
- **`debit_details` JSON** (dati sporchi: prezzi a molte cifre, `serviceCount: 0` con totali `null`) →
  figlia tipizzata.
- **`booking_id` `0` = "nessuna prenotazione"** → `reservation_public_id` `NULL`.
- **`identifier` sul movimento** → il legame è il `wallet_id` (FK), non una stringa ripetuta.
- **Nessuna FK** → `wallet_id` FK interna; `customer`/`property` ereditati dal conto (denormalizzati per
  gli indici).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_wallet_transactions` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | referenziato da C1 (riga servizio pagata col borsellino) e da C4 (pagamento `VOUCHER`/`OUTSTANDING` dello scontrino) |
| `ix_..._wallet_date` | `(wallet_id, occurred_at DESC)` | btree | estratto conto del borsellino |
| `ix_..._reservation` | `(reservation_public_id) WHERE reservation_public_id IS NOT NULL` | btree parziale | consumi di una prenotazione |
| `ix_..._property_date` | `(property_id, occurred_at DESC)` | btree | report movimenti tessera per stabilimento |

## Campi

### id
- **Legacy**: `card_transactions.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `payment_wallet_transaction_items.transaction_id`.

### public_id
- **Legacy**: nessuno
- **Descrizione**: id pubblico stabile, copiato da C1/C4.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### wallet_id
- **Legacy**: `card_transactions.identifier` (chiave logica per stringa)
- **Descrizione**: conto borsellino movimentato.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`payment_wallet_accounts.id`](payment_wallet_accounts.md).

### property_id
- **Legacy**: `card_transactions.property_id` (`int`, `NULL` su 2 righe)
- **Descrizione**: stabilimento (denormalizzato dal conto per gli indici).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: = `payment_wallet_accounts.property_id`; [D-01](../_domande-committente.md).

### customer_id
- **Legacy**: `card_transactions.user_id`
- **Descrizione**: cliente (denormalizzato dal conto).
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: = `payment_wallet_accounts.customer_id`.

### movement_type
- **Legacy**: `card_transactions.transaction_type` (`1` credito / `2` debito; `NULL` su 1 riga)
- **Descrizione**: verso del movimento. **Esclusivo** → enum.
- **Tipo**: `enum payment.wallet_movement_type` — valori: `credit` (entra credito nel conto), `debit`
  (esce credito dal conto).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `credit` incrementa `balance` e `lifetime_credited`; `debit` lo decrementa.

### source
- **Legacy**: implicito (`debit_details` valorizzato ⇒ consumo servizio; altrimenti ricarica)
- **Descrizione**: causale del movimento.
- **Tipo**: `enum payment.wallet_movement_source` — valori: `top_up` (ricarica alla cassa),
  `service_consumption` (consumo bar/servizi), `resale_credit_earned` (accredito crediti sharing da C1),
  `reservation_settlement` (saldo di una prenotazione col borsellino), `reversal` (storno di un
  movimento precedente), `adjustment` (rettifica manuale), `expiry` (scadenza crediti).
- **Default**: `top_up`.
- **Nullabile**: No.
- **Relazioni**: `resale_credit_earned` ⇒ `movement_type = credit`, `reserved_offer_ref` valorizzato.

### amount
- **Legacy**: `card_transactions.amount` (`decimal(10,2)`, nullable, può essere negativo con `type=1`)
- **Descrizione**: importo del movimento, **sempre positivo**; il verso lo dà `movement_type`.
- **Tipo**: `numeric(12,2)`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (amount > 0)`.

### currency
- **Legacy**: nessuno
- **Descrizione**: valuta ISO 4217.
- **Tipo**: `char(3)`.
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: = conto.

### balance_after
- **Legacy**: nessuno (il legacy non registrava il saldo progressivo)
- **Descrizione**: saldo del conto **dopo** questo movimento (running balance, per l'estratto conto e la
  verifica di integrità).
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: calcolato in transazione; un job verifica la catena.

### reservation_public_id
- **Legacy**: `card_transactions.booking_id` (`int`; `0`/`NULL` su ~23%)
- **Descrizione**: prenotazione a cui il movimento è collegato (consumo o saldo).
- **Tipo**: `uuid` (= `public_id` di `booking_reservations`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_reservations`](../booking/booking_reservations.md).

### resale_offer_ref
- **Legacy**: nessuno (l'accredito sharing avveniva su `users` per email)
- **Descrizione**: `public_id` dell'offerta di rivendita che ha generato il credito (`source =
  resale_credit_earned`).
- **Tipo**: `uuid` (= `public_id` di `booking_resale_offers`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_resale_offers`](../booking/booking_resale_offers.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 610 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### reverses_transaction_id
- **Legacy**: nessuno
- **Descrizione**: per `source = reversal`, il movimento stornato.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK interna** (auto-riferimento) → `payment_wallet_transactions.id`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 610 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### ledger_entry_ref
- **Legacy**: nessuno
- **Descrizione**: `public_id` della riga contabile generata quando il movimento salda una prenotazione.
- **Tipo**: `uuid` (= `public_id` di `payment_ledger_entries`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: correlazione ledger ↔ wallet.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 610 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### operated_by_user_id
- **Legacy**: `card_transactions.operated_by` (`int`, `NULL` su 51 righe; un operatore domina)
- **Descrizione**: operatore che ha registrato il movimento (audit).
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).

### occurred_at
- **Legacy**: `card_transactions.createdAt`
- **Descrizione**: data/ora del movimento.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: retention 10 anni (scrittura contabile — [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md)).

### created_at / updated_at / deleted_at
- **Legacy**: `card_transactions.createdAt` / `updatedAt` / `IsDeleted`
- **Descrizione**: creazione / modifica. `deleted_at` esposto per uniformità: **append-only** (una
  correzione è un movimento `reversal`, non un update/delete).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at` (sempre `NULL`).

## Valutazione JSON

`card_transactions.debit_details` (oggetto JSON, dati sporchi) → figlia tipizzata
[`payment_wallet_transaction_items`](payment_wallet_transaction_items.md). Nessun JSON qui.

## Suggerimenti per le prestazioni

- Volume storicamente basso; l'indice `(wallet_id, occurred_at DESC)` copre l'estratto conto senza
  `SUM()` (il saldo è su `payment_wallet_accounts.balance`, verificabile con `balance_after`).
- `balance_after` rende l'audit del saldo O(1) per riga invece di ricalcolare tutta la catena.
- Nessun partizionamento previsto (crescita lenta).

## Implementazione .NET

`WalletTransaction` è un'**entità figlia** di `WalletAccount` ([`payment_wallet_accounts`](payment_wallet_accounts.md)),
area `Wallet` in `Module.Payment`, schema `payment`. Append-only, con `public_id` proprio (referenziata
da C1/C4) e collezione owned `Items` ([`payment_wallet_transaction_items`](payment_wallet_transaction_items.md)):
non è un owned type di `WalletAccount` (ha identità pubblica indipendente), ma è comunque scritta solo
tramite l'aggregate root `WalletAccount` per garantire l'invariante `Balance = Σ transactions`.

### Enum propri

```csharp
// Module.Payment.Domain.Enums.WalletMovementType
public enum WalletMovementType
{
	Credit = 0,
	Debit = 1
}

// Module.Payment.Domain.Enums.WalletMovementSource
public enum WalletMovementSource
{
	TopUp = 0,
	ServiceConsumption = 1,
	ResaleCreditEarned = 2,
	ReservationSettlement = 3,
	Reversal = 4,
	Adjustment = 5,
	Expiry = 6
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<WalletMovementType>("payment", "wallet_movement_type");
modelBuilder.HasPostgresEnum<WalletMovementSource>("payment", "wallet_movement_source");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<WalletMovementType>("payment.wallet_movement_type");
dataSourceBuilder.MapEnum<WalletMovementSource>("payment.wallet_movement_source");
```

### `WalletTransactionConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
	public void Configure(EntityTypeBuilder<WalletTransaction> builder)
	{
		builder.ToTable("wallet_transactions", "payment", t =>
		{
			t.HasComment("Movimento del borsellino (ricarica o consumo); append-only, il saldo del conto è la somma di queste righe.");
			t.HasCheckConstraint("ck_payment_wallet_transactions_amount", "amount > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_wallet_transactions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato da C1 (riga servizio pagata col borsellino) e da C4 (pagamento VOUCHER/OUTSTANDING dello scontrino).");

		builder.Property(x => x.WalletId)
			.HasColumnName("wallet_id")
			.IsRequired()
			.HasComment("Conto borsellino movimentato.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dal conto — nessuna FK cross-schema; denormalizzato per gli indici.");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.IsRequired()
			.HasComment("public_id di identity.customer_customers, copiato dal conto — nessuna FK cross-schema; denormalizzato per gli indici.");

		builder.Property(x => x.MovementType)
			.HasColumnName("movement_type")
			.HasColumnType("payment.wallet_movement_type")
			.IsRequired()
			.HasComment("Verso del movimento: credit incrementa balance/lifetime_credited, debit li decrementa.");

		builder.Property(x => x.Source)
			.HasColumnName("source")
			.HasColumnType("payment.wallet_movement_source")
			.HasDefaultValue(WalletMovementSource.TopUp)
			.IsRequired()
			.HasComment("Causale del movimento (ricarica, consumo, accredito sharing, saldo prenotazione, storno, rettifica, scadenza).");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo del movimento, sempre positivo; il verso lo dà movement_type.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217, allineata al conto.");

		builder.Property(x => x.BalanceAfter)
			.HasColumnName("balance_after")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Saldo del conto dopo questo movimento (running balance), per l'estratto conto e la verifica di integrità.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione a cui il movimento è collegato.");

		builder.Property(x => x.ResaleOfferRef)
			.HasColumnName("resale_offer_ref")
			.HasComment("public_id di booking.booking_resale_offers, copiato — nessuna FK cross-schema; valorizzato per source = ResaleCreditEarned.");

		builder.Property(x => x.ReversesTransactionId)
			.HasColumnName("reverses_transaction_id")
			.HasComment("Movimento stornato, per source = Reversal.");

		builder.Property(x => x.LedgerEntryRef)
			.HasColumnName("ledger_entry_ref")
			.HasComment("public_id di payment.ledger_entries — correlazione debole (non FK EF) verso la riga contabile generata quando il movimento salda una prenotazione: valorizzata a posteriori dall'handler di PaymentRecorded, non in un'unica transazione con l'inserimento del movimento.");

		builder.Property(x => x.OperatedByUserId)
			.HasColumnName("operated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha registrato il movimento (audit).");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora del movimento, decorrenza retention.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only (una correzione è un movimento reversal, sempre NULL).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_wallet_transactions_public_id");

		builder.HasIndex(x => new { x.WalletId, x.OccurredAt })
			.HasDatabaseName("ix_payment_wallet_transactions_wallet_date");

		builder.HasIndex(x => x.ReservationPublicId)
			.HasFilter("reservation_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_wallet_transactions_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.OccurredAt })
			.HasDatabaseName("ix_payment_wallet_transactions_property_date");

		builder.HasOne(x => x.Wallet)
			.WithMany(w => w.Transactions)
			.HasForeignKey(x => x.WalletId)
			.HasConstraintName("fk_payment_wallet_transactions_wallet")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.ReversesTransaction)
			.WithMany()
			.HasForeignKey(x => x.ReversesTransactionId)
			.HasConstraintName("fk_payment_wallet_transactions_reverses_transaction")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

### Relazioni

`wallet_id` referenzia [`payment_wallet_accounts`](payment_wallet_accounts.md), stesso schema `payment`:
FK EF reale (`HasOne`/`WithMany(w => w.Transactions)`, `Restrict`, append-only). `reverses_transaction_id`
è un **auto-riferimento** FK EF reale sulla stessa tabella. `property_id`, `customer_id`,
`reservation_public_id`, `resale_offer_ref`, `operated_by_user_id` sono id/uuid copiati da altri
moduli (C2/C5/C1): solo `.HasComment`, nessuna FK. `ledger_entry_ref`, pur puntando a un'altra tabella
dello **stesso** schema `payment`, resta `.HasComment` senza FK EF: è una correlazione a posteriori
(scritta dopo che l'handler dell'evento ha creato la riga di ledger), non un riferimento presente al
momento dell'insert — introdurre una FK NOT DEFERRABLE bloccherebbe l'ordine di scrittura.

`ISoftDeletable` implementato solo per uniformità (sempre `NULL`, append-only); `IAuditable` implementato.

### Note di migrazione

- `card_transactions.transaction_type = 1` con `amount < 0` → normalizzato in `MovementType.Debit` con
  `amount > 0` (segno spostato nel tipo, come da regola dominio).
- `transaction_type` `NULL` (1 riga) da classificare a mano in migrazione (nessuna euristica affidabile).
- `booking_id = 0` → `reservation_public_id = NULL`.
- `balance_after` ricostruito applicando i movimenti normalizzati in ordine cronologico per `wallet_id`
  (il legacy non lo registrava).
- Ogni movimento aggiorna `WalletAccount.Balance` nella stessa transazione applicativa (concorrenza
  ottimistica sull'account); `source = ReservationSettlement` emette `PaymentRecorded` (outbox) → crea la
  riga in `payment_ledger_entries` e valorizza `ledger_entry_ref`.
- Consumer dell'evento `ResaleCreditEarned` (da C1) → movimento `Credit` / `source =
  ResaleCreditEarned`.
