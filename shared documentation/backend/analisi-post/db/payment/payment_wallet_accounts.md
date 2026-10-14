# Tabella `payment_wallet_accounts`

> **Dominio**: C3 — `payment` · **sottoarea**: `wallet` · **schema**: `payment` · **nome flat**:
> `payment_wallet_accounts` · **forma canonica**: `payment.wallet_accounts`.
>
> Nuova: dà un'identità propria al "conto tessera" oggi implicito in
> [`card_transactions`](../../../analisi-pre/db/card_transactions.md) (raggruppamento per la stringa
> `identifier`) e assorbe i saldi crediti denormalizzati su
> [`users`](../../../analisi-pre/db/users.md) (`total_number_of_credit`, `total_credit_value`,
> `totalCreditsEarned`, scritti da un cron **per assegnazione**). Fonti:
> [`analisi-pre/db/card_transactions.md`](../../../analisi-pre/db/card_transactions.md),
> [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md),
> [`analisi-pre/db/sharing_details.md`](../../../analisi-pre/db/sharing_details.md),
> [`docs/03-gestione-dati.md` §3.7, §4, §6](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga per (stabilimento × cliente × tipo di borsellino)**: il conto prepagato del cliente presso
uno stabilimento. Due tipi:

- **`prepaid_card`** — tessera/borsellino fisico ricaricabile (ex `card_transactions`), con un
  `card_number` stampato.
- **`resale_credit`** — crediti "sharing" maturati cedendo giorni di postazione (ex
  `users.total_*_credit`, alimentati da eventi `ResaleCreditEarned` di C1
  [`booking_resale_credit_ledger`](../booking/booking_resale_credit_ledger.md)).

Il **saldo** è materializzato (`balance`) e mantenuto dai movimenti
([`payment_wallet_transactions`](payment_wallet_transactions.md)); un job di riconciliazione lo
riallinea a `Σ movimenti`.

Correzioni rispetto al legacy:

- **`identifier` stringa libera dal client** (RNG non sicuro `<propertyId>+6cifre`, valori sporchi `""`,
  `"test"`, `"<n>-<timestamp>"`, nessun `UNIQUE`, nessun controllo di collisione) → `card_number`
  separato con `UNIQUE`, generato **server-side**; il legame `wallet ↔ (customer, property)` è una FK,
  non una convenzione.
- **Saldo ricalcolato con `SUM()` su tutto lo storico ad ogni lettura** (nessun indice `(user_id,
  property_id, isDeleted)`) → `balance` materializzato.
- **`total_number_of_credit`/`total_credit_value` su `users` per `email`, per assegnazione** → riga
  wallet `resale_credit` per `(property, customer)`, saldo **incrementale**.
- **`createCardTransaction` crea `customers` al volo con `fullName: ""`** → il wallet richiede un
  `customer_public_id` valido.
- **`IsDeleted` vs `isDeleted`** (colonna `IsDeleted`, query grezze `isDeleted`) → `deleted_at`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_wallet_accounts` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | esposto al cliente / referenziato da C1 (riga servizio pagata col borsellino) |
| `uq_..._scope` | `(property_id, customer_id, wallet_kind) WHERE deleted_at IS NULL` | UNIQUE parziale | un solo conto per tipo, cliente e stabilimento |
| `uq_..._card_number` | `(property_id, card_number) WHERE card_number IS NOT NULL AND deleted_at IS NULL` | UNIQUE parziale | numero tessera univoco per stabilimento (mancava) |
| `ix_..._customer` | `(customer_id)` | btree | tutti i borsellini di un cliente |
| `ix_..._nonzero` | `(property_id) WHERE balance <> 0 AND deleted_at IS NULL` | btree parziale | conti con saldo aperto (report / retention) |

## Campi

### id
- **Legacy**: nessuno (il conto era implicito)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `payment_wallet_transactions.wallet_id`.

### public_id
- **Legacy**: nessuno
- **Descrizione**: id pubblico stabile — esposto al cliente (estratto tessera) e **copiato da C1** sulla
  riga servizio pagata col borsellino (`is_paid_with_wallet` +
  riferimento, [`booking_service_lines`](../booking/booking_service_lines.md)).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `card_transactions.property_id` (`int`; feature concentrata su ~14 property)
- **Descrizione**: stabilimento presso cui vale il borsellino.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); [D-01](../_domande-committente.md).

### customer_id
- **Legacy**: `card_transactions.user_id` (id di un record `customers`, creato al volo)
- **Descrizione**: cliente titolare del conto — **riferimento vivo** a C5.
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_customer_customers`](../identity/identity_customer_customers.md).

### wallet_kind
- **Legacy**: implicito (`card_transactions` = tessera; `users.total_*_credit` = crediti sharing)
- **Descrizione**: tipo di borsellino. Valori **esclusivi**.
- **Tipo**: `enum payment.wallet_kind` — valori: `prepaid_card` (tessera fisica ricaricabile),
  `resale_credit` (crediti da cessione posto).
- **Default**: `prepaid_card`.
- **Nullabile**: No.
- **Relazioni**: `card_number` valorizzato **solo** per `prepaid_card`.

### card_number
- **Legacy**: `card_transactions.identifier` (`varchar(255)`, dal client, sporco, non unico)
- **Descrizione**: numero stampato sulla tessera fisica consegnata al cliente.
- **Tipo**: `varchar(24)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` per `wallet_kind = resale_credit`).
- **Relazioni**: `UNIQUE` parziale per stabilimento; generato server-side (o digitato dall'operatore per
  tessere preesistenti) con controllo di collisione.

### balance
- **Legacy**: calcolato a runtime `Σ(credito) − Σ(debito)` per `identifier`
- **Descrizione**: saldo corrente del conto (materializzato).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: mantenuto dai movimenti; `CHECK` no (può andare negativo per storni/rettifiche, come
  nel legacy); riconciliato da job.

### lifetime_credited
- **Legacy**: `users.totalCreditsEarned` (per i crediti sharing)
- **Descrizione**: totale storico accreditato (mai decrementato) — utile per soglie/gamification e per
  distinguere "quanto maturato" da "quanto residuo".
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: incrementato a ogni movimento `credit`.

### currency
- **Legacy**: nessuno
- **Descrizione**: valuta ISO 4217 del saldo.
- **Tipo**: `char(3)`.
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: —.

### status
- **Legacy**: `card_transactions.IsDeleted` (per il conto: `deleteUserCard` marcava tutte le righe)
- **Descrizione**: stato operativo del conto.
- **Tipo**: `enum payment.wallet_status` — valori: `active`, `blocked` (tessera smarrita/sospesa),
  `closed` (chiuso, saldo azzerato/rimborsato).
- **Default**: `active`.
- **Nullabile**: No.
- **Relazioni**: `blocked`/`closed` impediscono nuovi debiti.

### opened_at / last_movement_at
- **Legacy**: `card_transactions.createdAt` (min) / (max)
- **Descrizione**: apertura del conto / ultimo movimento (guida la retention — 24 mesi di inattività,
  [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md)).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `NULL`.
- **Nullabile**: No / Sì.
- **Relazioni**: `last_movement_at` aggiornato a ogni transazione.

### created_at / updated_at / deleted_at
- **Legacy**: `card_transactions.createdAt` / `updatedAt` / `IsDeleted`
- **Descrizione**: creazione / modifica / soft-delete (chiusura conto).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. Il saldo (prima un `SUM()` a runtime) è una colonna; i dettagli di consumo stanno nei
movimenti e nella loro figlia, non in un blob.

## Suggerimenti per le prestazioni

- **`balance` materializzato** elimina il `SUM()` su tutto lo storico a ogni lettura
  ([`docs/01 §6`](../../../progettazione/01-criticita-e-indicazioni-backend.md)): aggiornato in transazione col
  movimento, job notturno di riconciliazione con `Σ payment_wallet_transactions`.
- Volume basso (~700 movimenti in 4 anni, poche property). Nessun partizionamento necessario.
- Indice parziale `WHERE balance <> 0` per il report "tessere con credito residuo".

## Implementazione .NET

`WalletAccount` è l'**aggregate root** dell'area `Wallet` in `Module.Payment`, schema `payment`: 1 riga
per (stabilimento × cliente × tipo di borsellino), con invariante `Balance = Σ payment_wallet_transactions`
mantenuto in transazione a ogni movimento e concorrenza ottimistica (`xmin`) sull'aggiornamento del saldo.
Espone la collezione dei movimenti ([`payment_wallet_transactions`](payment_wallet_transactions.md)) come
aggregato figlio con FK propria (non owned, ha identità e `public_id` propri).

### Enum propri

```csharp
// Module.Payment.Domain.Enums.WalletKind
public enum WalletKind
{
	PrepaidCard = 0,
	ResaleCredit = 1
}

// Module.Payment.Domain.Enums.WalletStatus
public enum WalletStatus
{
	Active = 0,
	Blocked = 1,
	Closed = 2
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<WalletKind>("payment", "wallet_kind");
modelBuilder.HasPostgresEnum<WalletStatus>("payment", "wallet_status");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<WalletKind>("payment.wallet_kind");
dataSourceBuilder.MapEnum<WalletStatus>("payment.wallet_status");
```

### `WalletAccountConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class WalletAccountConfiguration : IEntityTypeConfiguration<WalletAccount>
{
	public void Configure(EntityTypeBuilder<WalletAccount> builder)
	{
		builder.ToTable("wallet_accounts", "payment", t => t.HasComment(
			"Conto prepagato del cliente presso uno stabilimento: tessera fisica ricaricabile o crediti da cessione posto."));

		builder.HasKey(x => x.Id).HasName("pk_payment_wallet_accounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, esposto al cliente e copiato da C1 sulla riga servizio pagata col borsellino.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento presso cui vale il borsellino.");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.IsRequired()
			.HasComment("public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente titolare del conto.");

		builder.Property(x => x.WalletKind)
			.HasColumnName("wallet_kind")
			.HasColumnType("payment.wallet_kind")
			.HasDefaultValue(WalletKind.PrepaidCard)
			.IsRequired()
			.HasComment("Tipo di borsellino: tessera fisica ricaricabile o crediti da cessione posto.");

		builder.Property(x => x.CardNumber)
			.HasColumnName("card_number")
			.HasMaxLength(24)
			.HasComment("Numero stampato sulla tessera fisica, generato server-side; NULL per wallet_kind = ResaleCredit.");

		builder.Property(x => x.Balance)
			.HasColumnName("balance")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Saldo corrente del conto, materializzato e mantenuto dai movimenti; può andare negativo per storni/rettifiche.");

		builder.Property(x => x.LifetimeCredited)
			.HasColumnName("lifetime_credited")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Totale storico accreditato, mai decrementato; distingue quanto maturato da quanto residuo.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217 del saldo.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.wallet_status")
			.HasDefaultValue(WalletStatus.Active)
			.IsRequired()
			.HasComment("Stato operativo del conto; blocked/closed impediscono nuovi debiti.");

		builder.Property(x => x.OpenedAt)
			.HasColumnName("opened_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Apertura del conto.");

		builder.Property(x => x.LastMovementAt)
			.HasColumnName("last_movement_at")
			.HasComment("Ultimo movimento registrato, guida la retention per inattività (24 mesi).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete alla chiusura del conto.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_wallet_accounts_public_id");

		builder.HasIndex(x => new { x.PropertyId, x.CustomerId, x.WalletKind })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_payment_wallet_accounts_scope");

		builder.HasIndex(x => new { x.PropertyId, x.CardNumber })
			.IsUnique()
			.HasFilter("card_number IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("uq_payment_wallet_accounts_card_number");

		builder.HasIndex(x => x.CustomerId)
			.HasDatabaseName("ix_payment_wallet_accounts_customer");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("balance <> 0 AND deleted_at IS NULL")
			.HasDatabaseName("ix_payment_wallet_accounts_nonzero");
	}
}
```

### Relazioni

`property_id` e `customer_id` sono `uuid` copiati da C2/C5: solo `.HasComment`, nessuna FK cross-schema.
Referenziata (FK interna) da `payment_wallet_transactions.wallet_id` (definita sulla Configuration del
figlio).

`ISoftDeletable` + `IAuditable`: implementati (soft-delete alla chiusura conto).

### Note di migrazione

- 1 wallet `PrepaidCard` per ogni `(property_id, identifier)` distinto e "pulito" di `card_transactions`:
  scartare `identifier` `""`/`"test"`, ricondurre i pattern `"<n>-<timestamp>"` a un nuovo `card_number`
  generato server-side (`CardNumberGenerator`, RNG sicuro con retry su unique violation).
- `balance` non copiato dal legacy: ricalcolato dai movimenti normalizzati (era un `SUM()` a runtime).
- 1 wallet `ResaleCredit` per ogni utente con `total_credit_value > 0`; la property di riferimento non è
  sempre ricostruibile dai dati legacy — [D-36](../_domande-committente.md) resta aperta.
- Consumer dell'evento `ResaleCreditEarned` (da C1) → `GetOrCreate(walletKind: ResaleCredit)` + movimento
  `credit` nella stessa transazione.
