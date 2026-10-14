# Tabella `payment_customer_gateway_profiles`

> **Dominio**: C3 — `payment` · **sottoarea**: `gateway` · **schema**: `payment` · **nome flat**:
> `payment_customer_gateway_profiles` · **forma canonica**: `payment.customer_gateway_profiles`.
>
> Ricostruisce da zero [`user_stripe_card_details`](../../../analisi-pre/db/user_stripe_card_details.md)
> (0 righe, feature non operativa end-to-end) e assorbe [`users.stripeCustomerId`](../../../analisi-pre/db/users.md)
> (C5 → C3, [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)). Fonti:
> [`analisi-pre/db/user_stripe_card_details.md`](../../../analisi-pre/db/user_stripe_card_details.md),
> [`analisi-pre/db/users.md` §stripeCustomerId](../../../analisi-pre/db/users.md),
> [`docs/03-gestione-dati.md` §2.4ter, §6](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga per (cliente × connected account)**: la mappatura `cliente finale → Stripe Customer`
(`cus_…`), che permette di riusare le carte salvate del cliente nei pagamenti online **verso quello
stabilimento** (il Customer Stripe è creato sul *connected account*, non sulla piattaforma). Contiene
**solo** id/oggetti Stripe — **mai** PAN/CVC/scadenza ([`docs/11-modulo-privacy.md`](../../../progettazione/11-modulo-privacy.md)).

Correzioni rispetto al legacy:

- **Model disallineato dallo schema** (`propertyId` nel model, assente a DB) → colonna reale
  `connected_account_id` (FK interna).
- **Nome fuorviante** `stripeCustomerCardId` (è un *Customer*, non una carta) → `stripe_customer_id`.
- **`token` colonna morta** (solo `...2` senza chiamanti) → non migrata.
- **`users.stripeCustomerId`** (che per errore conteneva anche id `acct_` Connect) → qui, solo `cus_…`
  validati.
- **Nessun `UNIQUE`** → `UNIQUE(customer_public_id, connected_account_id)`.
- **Alternativa**: se si delega interamente la gestione carte a Stripe (Customer creato al volo dal
  PaymentElement), questa tabella si può **eliminare** — vedi [D-34](../_domande-committente.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_customer_gateway_profiles` | `id` | PK | — |
| `uq_..._customer_account` | `(customer_public_id, connected_account_id) WHERE deleted_at IS NULL` | UNIQUE parziale | un solo Customer Stripe per cliente e connected account |
| `uq_..._stripe_customer` | `(stripe_customer_id)` | UNIQUE | id Stripe non riusabile |
| `ix_..._account` | `(connected_account_id)` | btree | elenco Customer per stabilimento (retention / chiusura account) |

## Campi

### id
- **Legacy**: `user_stripe_card_details.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### customer_public_id
- **Legacy**: `user_stripe_card_details.userId` (`int`, id `users`) / `users.id`
- **Descrizione**: cliente finale titolare del Customer Stripe. **Riferimento vivo** a C5.
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_customer_customers`](../identity/identity_customer_customers.md) — nessuna FK cross-schema.

### connected_account_id
- **Legacy**: `user_stripe_card_details.propertyId` (nel model, **assente** a schema → tutti i percorsi rompevano)
- **Descrizione**: connected account su cui vive il Customer Stripe (il cliente ha un `cus_…` **per
  stabilimento**).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`payment_gateway_connected_accounts.id`](payment_gateway_connected_accounts.md) (stesso schema `payment`).

### stripe_customer_id
- **Legacy**: `user_stripe_card_details.stripeCustomerCardId` (`varchar(255)`)
- **Descrizione**: id del Customer Stripe (`cus_…`) creato con `stripe.customers.create` sul connected
  account. Identificativo esterno opaco.
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`; passato a `paymentMethods.list({ customer })` / `attach`.

### default_payment_method_id
- **Legacy**: nessuno (il legacy elencava sempre tutte le carte, nessun default)
- **Descrizione**: id del PaymentMethod Stripe (`pm_…`) marcato come predefinito dal cliente.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: pre-selezione al checkout; il dettaglio carta (brand, last4, exp) **non** si persiste,
  si legge da Stripe on-demand.

### last_used_at
- **Legacy**: nessuno
- **Descrizione**: ultimo utilizzo di una carta salvata di questo profilo — guida la retention.
  **Deciso dal titolare (2026-09-17, D-34 in [`_domande-committente.md`](../_domande-committente.md),
  risolta)**: eliminazione **solo** dopo 24 mesi di inattività — la chiusura dell'account **non**
  anticipa la cancellazione, il profilo permane fino allo scadere del timer anche se l'account chiude
  prima ([`docs/05-ottimizzazioni-db.md` §1](../../../progettazione/05-ottimizzazioni-db.md),
  [`docs/82-retention-data.md` §3.2](../../../progettazione/82-retention-data.md)). Il job di cleanup deve
  quindi scattare solo sul timer di inattività, mai sull'evento "chiusura account".
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: aggiornato dopo un pagamento andato a buon fine con carta salvata.

### created_at / updated_at / deleted_at
- **Legacy**: `user_stripe_card_details.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica / soft-delete (retention per inattività — non la chiusura
  account, vedi `last_used_at` sopra).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`; hard-delete a valle su richiesta GDPR (nessun
  vincolo fiscale su questo dato).

## Valutazione JSON

Nessun JSON. Il dettaglio carta (PAN/brand/last4/exp) **non** si conserva: vive su Stripe, letto
on-demand.

## Suggerimenti per le prestazioni

- Volume basso (≈ clienti ricorrenti con carta salvata per stabilimento). Lettura puntuale per
  `(customer_public_id, connected_account_id)` su indice unico.
- Nessuna aggregazione. `last_used_at` serve solo al job di retention.

## Implementazione .NET

`CustomerGatewayProfile` è un'**entità semplice** dell'area `Gateway` in `Module.Payment`, schema
`payment`: 1 riga per (cliente × connected account), nessun evento di dominio. Contiene solo id/oggetti
Stripe — mai PAN/CVC/scadenza. Letta/scritta da `IStripeCustomerService.GetOrCreate(customerPublicId,
connectedAccountId)` (idempotente); la tokenizzazione carte avviene **solo client-side** (PaymentElement),
il backend riceve al più `pm_…`.

Nessun enum proprio (tutti i campi sono id/scalari).

### `CustomerGatewayProfileConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class CustomerGatewayProfileConfiguration : IEntityTypeConfiguration<CustomerGatewayProfile>
{
	public void Configure(EntityTypeBuilder<CustomerGatewayProfile> builder)
	{
		builder.ToTable("customer_gateway_profiles", "payment", t => t.HasComment(
			"Mappatura cliente finale → Stripe Customer, per connected account: consente di riusare le carte salvate presso quello stabilimento."));

		builder.HasKey(x => x.Id).HasName("pk_payment_customer_gateway_profiles");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.HasComment("public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente titolare del Customer Stripe.");

		builder.Property(x => x.ConnectedAccountId)
			.HasColumnName("connected_account_id")
			.IsRequired()
			.HasComment("Connected account su cui vive il Customer Stripe (il cliente ha un cus_… per stabilimento).");

		builder.Property(x => x.StripeCustomerId)
			.HasColumnName("stripe_customer_id")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Id del Customer Stripe (cus_…) creato sul connected account, identificativo esterno opaco.");

		builder.Property(x => x.DefaultPaymentMethodId)
			.HasColumnName("default_payment_method_id")
			.HasMaxLength(40)
			.HasComment("Id del PaymentMethod Stripe (pm_…) marcato come predefinito dal cliente, per la pre-selezione al checkout.");

		builder.Property(x => x.LastUsedAt)
			.HasColumnName("last_used_at")
			.HasComment("Ultimo utilizzo di una carta salvata di questo profilo, guida la retention per inattività.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete alla retention per inattività (24 mesi) — non alla sola chiusura account.");

		builder.HasIndex(x => new { x.CustomerPublicId, x.ConnectedAccountId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_payment_customer_gateway_profiles_customer_account");

		builder.HasIndex(x => x.StripeCustomerId)
			.IsUnique()
			.HasDatabaseName("uq_payment_customer_gateway_profiles_stripe_customer");

		builder.HasIndex(x => x.ConnectedAccountId)
			.HasDatabaseName("ix_payment_customer_gateway_profiles_account");

		builder.HasOne(x => x.ConnectedAccount)
			.WithMany()
			.HasForeignKey(x => x.ConnectedAccountId)
			.HasConstraintName("fk_payment_customer_gateway_profiles_connected_account")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

### Relazioni

`customer_public_id` è un `uuid` copiato da `identity.customer_customers` (C5): solo `.HasComment`,
nessuna FK — riferimento vivo ma cross-modulo. `connected_account_id` referenzia
[`payment_gateway_connected_accounts`](payment_gateway_connected_accounts.md), stesso schema `payment`:
FK EF reale (`HasOne`/`WithMany`, `Restrict` — non si cancella un connected account con profili cliente
agganciati senza gestirli esplicitamente).

`ISoftDeletable` + `IAuditable`: implementati (soft-delete solo alla retention per inattività, 24 mesi
— D-34, non alla sola chiusura account).

### Note di migrazione

- `user_stripe_card_details` è vuota (0 righe) → nessun dato da importare da lì.
- `users.stripeCustomerId`: importare solo i valori `cus_…` sintatticamente validi, scartando gli
  `acct_…` finiti lì per errore; ogni riga va abbinata al `connected_account_id` della property
  dell'utente (join con `payment_gateway_connected_accounts`).
- Job di retention `PurgeInactiveGatewayProfiles` (24 mesi `last_used_at` senza uso / chiusura account)
  → detach dei PaymentMethod su Stripe seguito da hard-delete della riga (nessun vincolo fiscale su
  questo dato, GDPR-friendly).
- [D-34](../_domande-committente.md) resta aperta: valutare se eliminare del tutto la tabella e delegare
  interamente a Stripe (Customer creato al volo dal PaymentElement).
