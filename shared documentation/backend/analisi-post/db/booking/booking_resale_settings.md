# Tabella `booking_resale_settings`

> **Dominio**: C1 — `booking` · **sottoarea**: `hold` (rivendita) · **schema**: `booking` · **nome flat**:
> `booking_resale_settings` · **forma canonica**: `booking.resale_settings`.
>
> Sostituisce i **7 campi "sharing" di
> [`configurations`](../../../analisi-pre/db/configurations.md)** (`sharing_enable`,
> `sharing_seasonal_workers`, `credit_perday_resold`, `credit_value`, `disable_both_options`,
> `enable_sharing_on_all_article`, `plan_absence_as_sharing`) — che nel legacy vivono nella config della
> mappa spiaggia (C2) "solo per accidente": sono parametri della feature rivendita/`hold` di **C1**
> ([`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md), campo cross-dominio). Fonti:
> [`analisi-pre/db/configurations.md`](../../../analisi-pre/db/configurations.md).

---

## Descrizione

**Una riga per stabilimento**: configurazione della feature "sharing / rivendita del posto". Guida il
comportamento di [`booking_resale_offers`](booking_resale_offers.md) e
[`booking_resale_credit_ledger`](booking_resale_credit_ledger.md).

Correzioni rispetto al legacy:

- **7 campi `int`/`boolean`/`varchar` dentro `configurations` (C2)** → tabella dedicata C1, 1 riga per
  property (`UNIQUE`, mancava).
- **`credit_value` testo libero molto sporco** (`'5€'`, `'5 €'`, `'5 Euro'`, `'5%'`,
  `'30% del giornaliero'`, `''`, `'0'`) → `credit_amount numeric(12,2)` + `credit_calc enum`
  (`fixed_amount` / `daily_percentage`).
- **`plan_absence_as_sharing` / `disable_both_options` "sempre 0" nel campione dev** → in produzione 2–3
  righe a `1`: si migrano come sono.
- **`sharing_enable = 0` sul 92%** → default `is_enabled = false`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_resale_settings` | `id` | PK | — |
| `uq_..._property` | `(property_id)` | UNIQUE | 1 riga per stabilimento (mancava nel legacy) |

## Campi

### id
- **Legacy**: `configurations.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `configurations.propertyId`
- **Descrizione**: stabilimento.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); `UNIQUE`.

### is_enabled
- **Legacy**: `configurations.sharing_enable` (`int` 0/1; `1` su ~13 righe)
- **Descrizione**: la feature rivendita/sharing è attiva per lo stabilimento.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: se `false`, non si creano offerte di rivendita.

### mode
- **Legacy**: `plan_absence_as_sharing` (`int` 0/1)
- **Descrizione**: come viene gestita la cessione di un giorno.
- **Tipo**: `enum booking.resale_mode` — valori: `absence` (solo "assenza pianificata", nessun credito —
  `plan_absence_as_sharing = 0`), `sharing` (cessione con credito al titolare —
  `plan_absence_as_sharing = 1`).
- **Default**: `absence`.
- **Nullabile**: No.
- **Relazioni**: guida `booking_resale_offers.origin` e la generazione di righe nel ledger crediti.

### applies_to
- **Legacy**: `configurations.sharing_seasonal_workers` (`int` 0/1; `1` su ~7 righe) +
  `configurations.enable_sharing_on_all_article` (`int` 0/1; `1` su 3 righe)
- **Descrizione**: estensioni della rivendita. Flag **non esclusivi** dello stesso argomento ("a cosa si
  applica lo sharing") → array invece di 2 colonne boolean.
- **Tipo**: `enum booking.resale_scope[]` — valori: `seasonal_workers` (consentita anche per i pass
  "dipendenti/stagionali" — ex `sharing_seasonal_workers`), `all_articles` (la cessione riguarda
  **tutte** le postazioni del pacchetto, non solo la prima — ex `enable_sharing_on_all_article`).
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: regola di esclusività "più flag dello stesso argomento → `enum[]`"
  ([`docs/03-gestione-dati.md` §3.10](../../../progettazione/03-gestione-dati.md)).

### both_options_disabled
- **Legacy**: `configurations.disable_both_options` (`int` 0/1; `1` su 3 righe)
- **Descrizione**: interruttore che disabilita **entrambe** le modalità (semantica esatta da riverificare
  col committente — si migra il valore, [D-14 analogo](../_domande-committente.md)).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: campo di semantica incerta nel legacy; migrato as-is, "da rivedere".

### credit_units_per_day
- **Legacy**: `configurations.credit_perday_resold` (`int`; per lo più `0`/`1`, qualche outlier `30`/`320`)
- **Descrizione**: quanti "crediti" simbolici matura il titolare per ogni giorno ceduto e ripreso.
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: alimenta `booking_resale_credit_ledger.credit_units`; outlier legacy bonificati.

### credit_calc
- **Legacy**: derivato da `credit_value` (`'5€'` → importo; `'5%'` / `'30% del giornaliero'` → percentuale)
- **Descrizione**: come si calcola il **valore** del credito per giorno ceduto.
- **Tipo**: `enum booking.resale_credit_calc` — valori: `fixed_amount` (importo fisso),
  `daily_percentage` (percentuale della tariffa giornaliera della postazione).
- **Default**: `fixed_amount`.
- **Nullabile**: No.
- **Relazioni**: —.

### credit_amount
- **Legacy**: `configurations.credit_value` (`varchar` sporco)
- **Descrizione**: valore del credito: importo € se `credit_calc = fixed_amount`, percentuale (0–100)
  se `daily_percentage`.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: mai `float`; parsing del testo legacy (`€`/`Euro`/spazi/`%`) in migrazione.

### created_at / updated_at / deleted_at
- **Legacy**: `configurations.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica / soft-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON — 7 scalari sparsi in `configurations` (C2) → colonne tipizzate in una tabella C1 dedicata.

## Suggerimenti per le prestazioni

- 1 riga per stabilimento, letta all'avvio del flusso rivendita. Nessun indice oltre a PK e
  `UNIQUE(property_id)`.
- Candidata a entrare nel read-model di configurazione di C1 (se ne emergerà uno — [D-33 analogo](../_domande-committente.md)).

## Implementazione .NET

`ResaleSettings` è un **aggregate root** (lookup di configurazione, 1 riga per stabilimento) dell'area
`Resale` del modulo `Module.Booking`, schema `booking`. Implementa `ISoftDeletable`/`IAuditable`.

```csharp
// Domain/Enums/ResaleMode.cs
public enum ResaleMode { Absence = 0, Sharing = 1 }

// Domain/Enums/ResaleCreditCalc.cs
public enum ResaleCreditCalc { FixedAmount = 0, DailyPercentage = 1 }

// Domain/Enums/ResaleScope.cs — flags non esclusivi ("a cosa si applica lo sharing") → enum array PG
public enum ResaleScope { SeasonalWorkers = 0, AllArticles = 1 }
```

```csharp
// <Modulo>DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ResaleMode>("booking", "resale_mode");
modelBuilder.HasPostgresEnum<ResaleCreditCalc>("booking", "resale_credit_calc");
modelBuilder.HasPostgresEnum<ResaleScope>("booking", "resale_scope");
```

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResaleSettingsConfiguration : IEntityTypeConfiguration<ResaleSettings>
{
	public void Configure(EntityTypeBuilder<ResaleSettings> builder)
	{
		builder.ToTable("booking_resale_settings", "booking", t => t.HasComment(
			"Configurazione per stabilimento della feature rivendita/sharing del posto: guida il " +
			"comportamento di booking_resale_offers e booking_resale_credit_ledger."));

		builder.HasKey(x => x.Id).HasName("pk_booking_resale_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.IsEnabled)
			.HasColumnName("is_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("La feature rivendita/sharing è attiva per lo stabilimento");

		builder.Property(x => x.Mode)
			.HasColumnName("mode")
			.HasColumnType("booking.resale_mode")
			.HasDefaultValue(ResaleMode.Absence)
			.IsRequired()
			.HasComment("Come viene gestita la cessione di un giorno: solo assenza pianificata senza credito, o sharing con credito al titolare");

		builder.Property(x => x.AppliesTo)
			.HasColumnName("applies_to")
			.HasColumnType("booking.resale_scope[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Estensioni della rivendita: consentita per pass stagionali, estesa a tutte le postazioni del pacchetto");

		builder.Property(x => x.BothOptionsDisabled)
			.HasColumnName("both_options_disabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Disabilita entrambe le modalità (semantica esatta da riverificare col committente, migrato as-is)");

		builder.Property(x => x.CreditUnitsPerDay)
			.HasColumnName("credit_units_per_day")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Quanti crediti simbolici matura il titolare per ogni giorno ceduto e ripreso");

		builder.Property(x => x.CreditCalc)
			.HasColumnName("credit_calc")
			.HasColumnType("booking.resale_credit_calc")
			.HasDefaultValue(ResaleCreditCalc.FixedAmount)
			.IsRequired()
			.HasComment("Come si calcola il valore del credito per giorno ceduto: importo fisso o percentuale della tariffa giornaliera");

		builder.Property(x => x.CreditAmount)
			.HasColumnName("credit_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Valore del credito: importo € se credit_calc = fixed_amount, percentuale 0-100 se daily_percentage");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione della configurazione");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica della configurazione");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della configurazione");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_booking_resale_settings_property");
	}
}
```

**Note di migrazione**:

- Estrazione dei 7 campi "sharing" da `configurations` (C2) per `propertyId`, spostati in questa tabella
  dedicata C1: `sharing_enable` → `is_enabled`, `plan_absence_as_sharing` → `mode`,
  `sharing_seasonal_workers`/`enable_sharing_on_all_article` → `applies_to[]`, `disable_both_options` →
  `both_options_disabled`, `credit_perday_resold` → `credit_units_per_day` (outlier `30`/`320` bonificati).
- `credit_value` (testo libero sporco: `'5€'`, `'5 €'`, `'5 Euro'`, `'5%'`, `'30% del giornaliero'`, `''`,
  `'0'`) → parsing in migrazione con tabella di frequenza dei valori reali distinti e mappatura esplicita
  (§2.6 linee guida dati) verso `credit_calc` (`%` ⇒ `DailyPercentage`, altrimenti `FixedAmount`) +
  `credit_amount numeric(12,2)`; valori vuoti/non parsabili → `0` con `FixedAmount` e riga segnalata per
  revisione manuale.
- Righe di `configurations` senza `propertyId` valorizzato → scartate (nessuno stabilimento a cui
  associare la configurazione).
- Vincolo `UNIQUE(property_id)` mancava nel legacy (i 7 campi erano semplici colonne di una riga di
  configurazione più ampia): qui garantito dallo schema, un solo record per property.
