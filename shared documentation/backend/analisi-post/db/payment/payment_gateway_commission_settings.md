# Tabella `payment_gateway_commission_settings`

> **Dominio**: C3 — `payment` · **sottoarea**: `gateway` · **schema**: `payment` · **nome flat**:
> `payment_gateway_commission_settings` · **forma canonica**: `payment.gateway_commission_settings`.
>
> Sostituisce il JSON [`properties.commissionAmount`](../../../analisi-pre/db/properties.md)
> (`{onlineCommission, iframeCommission}`, default `{6,6}`) **e** le due liste hardcoded nel frontend
> `COMMISSION_FREE_PROPERTY_IDS` / `CONNECTED_AND_PLATFORM_ACCOUNT_PROPERTIES`
> ([`properties.md` §commissionAmount](../../../analisi-pre/db/properties.md)). Campo cross-dominio
> C2 → C3 ([`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md),
> [`_feature-tabelle.md` → "Campi che escono da C2"](../_feature-tabelle.md)). Fonti:
> [`analisi-pre/db/properties.md`](../../../analisi-pre/db/properties.md),
> [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §6](../../../progettazione/01-criticita-e-indicazioni-backend.md).

---

## Descrizione

**Una riga per stabilimento**: il **tasso di commissione** che la piattaforma trattiene sugli incassi
online (`application_fee_amount` di Stripe Connect), differenziato per canale (sito pubblico vs widget
iframe su sito terzo) più la fee fissa per transazione. Oggi il calcolo è **tutto lato client**
(`ArithmaticService.calculateBookingTotal`) e il backend non ricalcola: nella riscrittura la commissione
si calcola **server-side** in C3 al momento della creazione del PaymentIntent
([`payment_transaction_charges`](payment_transaction_charges.md)).

Correzioni rispetto al legacy:

- **JSON `{onlineCommission, iframeCommission}`** (a volte una stringa `"2"`, a volte JSON malformato) →
  due colonne `numeric(5,2)`.
- **Fee fissa `+ 0.5`** cablata nella formula frontend → colonna `fixed_fee_amount` (default `0.50`).
- **Esenzioni via liste di `propertyId` hardcoded nel bundle** (5 "commission free via iframe" + 1
  "connected and platform account") → `exemption` **enum** (verifica di esclusività: un solo motivo di
  esenzione per stabilimento, [`docs/03 §3.10`](../../../progettazione/03-gestione-dati.md)).
- **Nessun `UNIQUE`** → `UNIQUE(property_id)`.
- **`onlineCommission`/`iframeCommission` non esclusivi** (canali diversi, entrambi applicabili) →
  restano **due colonne**, non un enum.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_gateway_commission_settings` | `id` | PK | — |
| `uq_..._property` | `(property_id)` | UNIQUE | 1 riga per stabilimento |

## Campi

### id
- **Legacy**: nessuno (era un campo su `properties`)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `properties.id` (la riga possiede `commissionAmount`)
- **Descrizione**: stabilimento.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); `UNIQUE`. Tipo soggetto a [D-01](../_domande-committente.md).

### online_rate
- **Legacy**: `properties.commissionAmount.onlineCommission` (default `6`)
- **Descrizione**: percentuale trattenuta sulle prenotazioni concluse sul sito pubblico di prenotazione
  (`front-customer-pre`), quando **non** in iframe.
- **Tipo**: `numeric(5,2)` (percentuale 0–100).
- **Default**: `6.00`.
- **Nullabile**: No.
- **Relazioni**: usata come base di `application_fee_amount` in `payment_transaction_charges` quando
  `sales_channel = redirect_checkout`.

### iframe_rate
- **Legacy**: `properties.commissionAmount.iframeCommission` (default `6`; in alcune righe `0`)
- **Descrizione**: percentuale trattenuta quando la prenotazione avviene dentro il widget iframe
  imbottito su un sito terzo (`isIframe === "iframe"`).
- **Tipo**: `numeric(5,2)`.
- **Default**: `6.00`.
- **Nullabile**: No.
- **Relazioni**: usata quando `payment_transaction_charges.sales_channel = iframe`.

### fixed_fee_amount
- **Legacy**: costante `+ 0.5` nella formula `calculateBookingTotal` (frontend)
- **Descrizione**: fee fissa in € aggiunta alla commissione percentuale su ogni transazione online.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0.50`.
- **Nullabile**: No.
- **Relazioni**: `application_fee = gross * rate/100 + fixed_fee_amount` (per acconto: `gross/2`).

### exemption
- **Legacy**: liste hardcoded `COMMISSION_FREE_PROPERTY_IDS` (5 id) / `CONNECTED_AND_PLATFORM_ACCOUNT_PROPERTIES` (1 id)
- **Descrizione**: motivo di esenzione dalla commissione. **Esclusivo** (una property ha al più un
  motivo).
- **Tipo**: `enum payment.commission_exemption` — valori: `none` (commissione applicata), `iframe_only`
  (nessuna commissione sulle prenotazioni via iframe — ex `COMMISSION_FREE_PROPERTY_IDS`), `always`
  (nessuna commissione su nessun canale — ex `CONNECTED_AND_PLATFORM_ACCOUNT_PROPERTIES`, che incassa su
  connected **e** platform account).
- **Default**: `none`.
- **Nullabile**: No.
- **Relazioni**: se `iframe_only` e `sales_channel = iframe` → `application_fee = 0`; se `always` →
  sempre `0`.

### created_at / updated_at / deleted_at
- **Legacy**: `properties.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica / soft-delete (cessazione stabilimento).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Il JSON legacy `commissionAmount` (2 chiavi) → 2 colonne `numeric`. Nessun JSON nel nuovo schema.

## Suggerimenti per le prestazioni

- ~1 riga per stabilimento; letta al calcolo del totale di ogni checkout online. Candidata a **cache**
  per `property_id`.
- Nessun indice oltre a PK + `UNIQUE(property_id)`.
- La sostituzione delle liste hardcoded con `exemption` elimina un deploy del frontend per ogni nuova
  property esente.

## Implementazione .NET

`GatewayCommissionSetting` è un'**entità semplice** (config, 1 riga per stabilimento) dell'area `Gateway`
in `Module.Payment`, schema `payment`: nessun evento di dominio, `sealed class`, non eredita
`AggregateRoot`. Letta da `ICommissionCalculator` (unico punto di calcolo, usato sia da `POST
/payments/quote` sia dalla creazione del PaymentIntent) — sostituisce il calcolo lato client legacy.

### Enum propri

```csharp
// Module.Payment.Domain.Enums.CommissionExemption
public enum CommissionExemption
{
	None = 0,
	IframeOnly = 1,
	Always = 2
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<CommissionExemption>("payment", "commission_exemption");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<CommissionExemption>("payment.commission_exemption");
```

### `GatewayCommissionSettingConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class GatewayCommissionSettingConfiguration : IEntityTypeConfiguration<GatewayCommissionSetting>
{
	public void Configure(EntityTypeBuilder<GatewayCommissionSetting> builder)
	{
		builder.ToTable("gateway_commission_settings", "payment", t => t.HasComment(
			"Tasso di commissione trattenuto dalla piattaforma sugli incassi online, per stabilimento e canale."));

		builder.HasKey(x => x.Id).HasName("pk_payment_gateway_commission_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento a cui si applica la commissione.");

		builder.Property(x => x.OnlineRate)
			.HasColumnName("online_rate")
			.HasColumnType("numeric(5,2)")
			.HasDefaultValue(6.00m)
			.IsRequired()
			.HasComment("Percentuale trattenuta sulle prenotazioni concluse sul sito pubblico (fuori iframe).");

		builder.Property(x => x.IframeRate)
			.HasColumnName("iframe_rate")
			.HasColumnType("numeric(5,2)")
			.HasDefaultValue(6.00m)
			.IsRequired()
			.HasComment("Percentuale trattenuta sulle prenotazioni concluse nel widget iframe su sito terzo.");

		builder.Property(x => x.FixedFeeAmount)
			.HasColumnName("fixed_fee_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0.50m)
			.IsRequired()
			.HasComment("Fee fissa in euro aggiunta alla commissione percentuale su ogni transazione online.");

		builder.Property(x => x.Exemption)
			.HasColumnName("exemption")
			.HasColumnType("payment.commission_exemption")
			.HasDefaultValue(CommissionExemption.None)
			.IsRequired()
			.HasComment("Motivo di esenzione dalla commissione, esclusivo per stabilimento.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete alla cessazione dello stabilimento.");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_payment_gateway_commission_settings_property");
	}
}
```

### Relazioni

`property_id` è un `uuid` copiato da `catalog.properties` (C2), campo cross-dominio C2 → C3: solo
`.HasComment`, nessuna FK. Nessuna FK interna in ingresso/uscita: letta per valore da `Charge`
(`payment_transaction_charges`) al calcolo di `application_fee_amount`, non referenziata a schema.

`ISoftDeletable` + `IAuditable`: implementati (soft-delete alla cessazione dello stabilimento).

### Note di migrazione

- Per ogni property con `commissionAmount` → parse tollerante: JSON valido `{onlineCommission,
  iframeCommission}` → le 2 rate; stringa numerica libera (`"2"`, `"4"`) → `online_rate = iframe_rate =
  valore`; JSON malformato o assente → default `6.00`/`6.00`.
- `exemption` derivata dalle due liste hardcoded del bundle `front-customer-pre`
  (`COMMISSION_FREE_PROPERTY_IDS` → `IframeOnly`; `CONNECTED_AND_PLATFORM_ACCOUNT_PROPERTIES` → `Always`).
- `fixed_fee_amount = 0.50` per tutte le righe (era una costante cablata nel frontend, non persistita).
