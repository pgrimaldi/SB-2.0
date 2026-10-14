# Tabella `payment_gateway_connected_accounts`

> **Dominio**: C3 — `payment` · **sottoarea**: `gateway` · **schema**: `payment` · **nome flat**:
> `payment_gateway_connected_accounts` · **forma canonica**: `payment.gateway_connected_accounts`.
>
> Sostituisce [`stripe_configrations`](../../../analisi-pre/db/stripe_configrations.md) (44 righe reali,
> ~1 per stabilimento). Fonti:
> [`analisi-pre/db/stripe_configrations.md`](../../../analisi-pre/db/stripe_configrations.md),
> [`docs/03-gestione-dati.md` §2.4ter, §4](../../../progettazione/03-gestione-dati.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §6](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga per stabilimento**: l'account **Stripe Connect (connected account)** del venditore, con cui la
piattaforma incassa per suo conto i pagamenti online (`Stripe-Account` / `destination` nelle chiamate
Stripe). La presenza di una riga attiva è la pre-condizione della prenotazione online con pagamento;
alla cessazione del contratto la riga si soft-elimina e il collegamento Connect si revoca
([`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md)).

Correzioni rispetto al legacy:

- **Nome con refuso** (`stripe_configrations`) + `camelCase` → `snake_case`.
- **Nessun `UNIQUE(propertyId)`** (garantito solo da `findOne` prima della `create`; al login si
  prendeva `stripe_configrations[0]`) → `UNIQUE(property_id)` parziale.
- **Bug `update` con `where: { id: req.body.userId }`** (la ri-connessione non aggiornava nulla) →
  upsert per `property_id`.
- **Accoppiamento implicito** `deleteStripeAccount` → `properties.serviceOnlineBooking = 0` in due
  tabelle → l'effetto sul canale online resta in C2, guidato da un **evento** `ConnectedAccountRevoked`.
- **`sellerStripeConnectId` referenziato per valore** da `online_transaction_details.stripe_connected_id`
  → qui è FK interna allo schema `payment` ([`payment_transaction_charges`](payment_transaction_charges.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_gateway_connected_accounts` | `id` | PK | — |
| `uq_..._property` | `(property_id) WHERE deleted_at IS NULL` | UNIQUE parziale | 1 account attivo per stabilimento (mancava) |
| `uq_..._stripe_account` | `(stripe_account_id) WHERE deleted_at IS NULL` | UNIQUE parziale | un connected account non può servire due property |
| `ix_..._onboarding` | `(onboarding_state) WHERE deleted_at IS NULL` | btree parziale | cruscotto "account incompleti / da riconciliare" |

## Campi

### id
- **Legacy**: `stripe_configrations.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `payment_transaction_charges.connected_account_id`.

### property_id
- **Legacy**: `stripe_configrations.propertyId` (`int`, impostato dal parametro `state` dell'OAuth Connect)
- **Descrizione**: stabilimento proprietario dell'account.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md) — nessuna FK cross-schema; `UNIQUE` parziale. Tipo `uuid`/`bigint` soggetto a [D-01](../_domande-committente.md).

### stripe_account_id
- **Legacy**: `stripe_configrations.sellerStripeConnectId` (`varchar(255)`, indice non-unico)
- **Descrizione**: id del connected account Stripe (`acct_…`), da `response.stripe_user_id` dello scambio
  OAuth `authorization_code`. Identificativo esterno **opaco** ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).
- **Tipo**: `varchar(32)` (formato `acct_` + 16 alfanumerici; margine).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: usato come `Stripe-Account` in tutte le operazioni per conto del venditore; congelato in
  `payment_transaction_charges.stripe_account_id_snapshot` per audit. Colonna **propria** di questa
  entità → nessun suffisso `_snapshot` qui.

### onboarding_state
- **Legacy**: nessuno (il legacy non traccia lo stato di onboarding Connect)
- **Descrizione**: stato di completamento del connected account lato Stripe.
- **Tipo**: `enum payment.connect_onboarding_state` — valori: `pending` (link creato, onboarding non
  concluso), `restricted` (Stripe richiede altri dati / capability non attive), `active`
  (charges + payouts abilitati), `rejected`, `disabled`.
- **Default**: `pending`.
- **Nullabile**: No.
- **Relazioni**: aggiornato dal webhook `account.updated`; `active` è la condizione perché C2 tenga
  acceso il canale "prenotazione online con pagamento".

### charges_enabled / payouts_enabled
- **Legacy**: nessuno
- **Descrizione**: copia degli omonimi flag Stripe `account.charges_enabled` / `payouts_enabled`
  (comodità di lettura senza chiamare l'API).
- **Tipo**: `boolean` ciascuno.
- **Default**: `false` / `false`.
- **Nullabile**: No / No.
- **Relazioni**: mantenuti dal webhook `account.updated`.

### connected_at
- **Legacy**: `stripe_configrations.createdAt` (di fatto = data di connessione)
- **Descrizione**: quando l'account è stato collegato.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### revoked_at
- **Legacy**: nessuno (il legacy fa `destroy` della riga)
- **Descrizione**: quando il collegamento Connect è stato revocato (cessazione contratto / cambio
  account). Distinto da `deleted_at` per conservare la storia della revoca.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: valorizzato insieme all'evento `ConnectedAccountRevoked` verso C2.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 44 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `stripe_configrations.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica / soft-delete (alla cessazione dello stabilimento).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. Il legacy non ne aveva; i pochi campi sono scalari tipizzati.

## Suggerimenti per le prestazioni

- ~50 righe totali: tabella minuscola, letta all'inizio di ogni flusso di pagamento online. Candidata a
  **cache applicativa** (per `property_id`, invalidata dal webhook `account.updated`).
- `UNIQUE(property_id)` parziale rende la `findOne` per property un lookup su indice, non uno scan.
- Nessun indice oltre a PK + i due `UNIQUE` parziali + `onboarding_state`.

## Implementazione .NET

`ConnectedAccount` è l'**aggregate root** dell'area `Gateway` in `Module.Payment`, schema `payment`: 1
riga per stabilimento, solleva gli eventi di dominio `ConnectedAccountActivated` / `ConnectedAccountRevoked`
consumati (via outbox) da C2. Interagisce con Stripe tramite `Stripe.net` dietro `IPaymentGateway`, mai
direttamente dal dominio.

### Enum propri

```csharp
// Module.Payment.Domain.Enums.ConnectOnboardingState
public enum ConnectOnboardingState
{
	Pending = 0,
	Restricted = 1,
	Active = 2,
	Rejected = 3,
	Disabled = 4
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ConnectOnboardingState>("payment", "connect_onboarding_state");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<ConnectOnboardingState>("payment.connect_onboarding_state");
```

### `ConnectedAccountConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class ConnectedAccountConfiguration : IEntityTypeConfiguration<ConnectedAccount>
{
	public void Configure(EntityTypeBuilder<ConnectedAccount> builder)
	{
		builder.ToTable("gateway_connected_accounts", "payment", t => t.HasComment(
			"Account Stripe Connect (connected account) del venditore, uno per stabilimento."));

		builder.HasKey(x => x.Id).HasName("pk_payment_gateway_connected_accounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento proprietario dell'account.");

		builder.Property(x => x.StripeAccountId)
			.HasColumnName("stripe_account_id")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Id del connected account Stripe (acct_…), identificativo esterno opaco.");

		builder.Property(x => x.OnboardingState)
			.HasColumnName("onboarding_state")
			.HasColumnType("payment.connect_onboarding_state")
			.HasDefaultValue(ConnectOnboardingState.Pending)
			.IsRequired()
			.HasComment("Stato di completamento dell'onboarding Connect lato Stripe, aggiornato dal webhook account.updated.");

		builder.Property(x => x.ChargesEnabled)
			.HasColumnName("charges_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Copia del flag Stripe account.charges_enabled.");

		builder.Property(x => x.PayoutsEnabled)
			.HasColumnName("payouts_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Copia del flag Stripe account.payouts_enabled.");

		builder.Property(x => x.ConnectedAt)
			.HasColumnName("connected_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data di collegamento dell'account.");

		builder.Property(x => x.RevokedAt)
			.HasColumnName("revoked_at")
			.HasComment("Data di revoca del collegamento Connect (cessazione contratto o ricollegamento); distinta da deleted_at.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete alla cessazione dello stabilimento.");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_payment_gateway_connected_accounts_property");

		builder.HasIndex(x => x.StripeAccountId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_payment_gateway_connected_accounts_stripe_account");

		builder.HasIndex(x => x.OnboardingState)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_payment_gateway_connected_accounts_onboarding");
	}
}
```

### Relazioni

`property_id` è un `uuid` copiato da `catalog.properties` (C2): solo `.HasComment`, nessuna FK
cross-schema. Referenziata da FK interne allo schema `payment`: `payment_customer_gateway_profiles.connected_account_id`
e `payment_transaction_charges.connected_account_id` (definite sulle rispettive Configuration con
`HasOne(...).WithMany(...)`).

`ISoftDeletable` + `IAuditable`: implementati (soft-delete alla cessazione dello stabilimento).

### Note di migrazione

- 1:1 da `stripe_configrations` per `propertyId` (mappa `legacy_propertyId → public_id` via C2).
- `onboarding_state = Active`, `charges_enabled = true` per le righe esistenti (hanno già incassato in
  produzione); `payouts_enabled` da verificare via API Stripe in fase di import, non assunto.
- Nessun segreto persistito in tabella: l'OAuth Connect non conserva refresh token qui (si usa
  l'access token della piattaforma); un eventuale `stripe_publishable_key` del connected account resta
  derivato/`appsettings`, non colonna.
