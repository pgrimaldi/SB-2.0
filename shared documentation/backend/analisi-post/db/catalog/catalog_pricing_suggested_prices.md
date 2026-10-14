# Tabella `catalog_pricing_suggested_prices`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_suggested_prices` · **forma canonica**: `catalog.pricing_suggested_prices`.
>
> Rimpiazza il JSON `price_lists.suggestedPriceDetails` — output della feature **Revenue Management**
> (prezzo suggerito dinamico), oggi riscritto in-place con `UPDATE` SQL diretti da
> `revenue.controller.js`, organizzato per anno. Fonti:
> [`analisi-pre/db/price_lists.md` (campo `suggestedPriceDetails`)](../../../analisi-pre/db/price_lists.md),
> [`analisi-pre/db/revenue_notifications.md`](../../../analisi-pre/db/revenue_notifications.md),
> [`analisi-pre/business-logic/stabilimento/10-revenue.md`](../../../analisi-pre/business-logic/stabilimento/10-revenue.md).

---

## Descrizione

Il **prezzo suggerito** calcolato dall'algoritmo Revenue Management per un pacchetto in una data specifica
(combina meteo, stagionalità, weekend, occupazione). Una riga per (pacchetto × data). Include lo stato
"applicato" (il gestore può accettare il prezzo suggerito o lo sconto, e quel prezzo entra nel calcolo
della prenotazione).

Nel legacy: JSON `{ "<anno>": [{ date, packages:[{ id, newUniqueId, name, daily_price, weekend_price,
suggestedPrice, isSuggestedPriceApplied, isOriginalPriceApplied, discountPercentage, isDiscountApplied }] }] }`,
riscritto per intero ad ogni ricalcolo, senza invalidazione della cache. Solo ~13 righe su ~788 valorizzate.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_suggested_prices` | `id` | PK | — |
| `uq_..._package_date_period` | `(price_list_id, price_date, period_label)` | UNIQUE | una riga per (pacchetto, data, variante di periodo) — aggiornato da `(price_list_id, price_date)` per D-64 |
| `ix_..._property_date` | `(property_id, price_date) WHERE is_applied` | parziale | "quali prezzi suggeriti sono applicati oggi" (usato dal motore prenotazione) |

## Campi

### id
- **Legacy**: `suggestedPriceDetails.<anno>[].packages[].newUniqueId` (id locale generato nel JSON — non stabile)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### price_list_id
- **Legacy**: `suggestedPriceDetails.<anno>[].packages[].id`
- **Descrizione**: pacchetto a cui si riferisce il prezzo suggerito.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_price_lists.id`](catalog_pricing_price_lists.md) `ON DELETE CASCADE`.

### property_id
- **Legacy**: `price_lists.propertyId` (della riga contenitore)
- **Descrizione**: stabilimento (denormalizzato per gli indici/filtri).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.

### price_date
- **Legacy**: `suggestedPriceDetails.<anno>[].date` (`"15/08/2024"`)
- **Descrizione**: giorno a cui si riferisce il prezzo suggerito.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: l'"anno" del JSON legacy è la parte anno di questa data.

### period_label
- **Legacy**: `packages[].name` (es. `"Fila 1 Alta stagione"`)
- **Descrizione**: etichetta libera della variante di periodo/settore. **Aggiunta 2026-09-21 per
  D-64** ([`_domande-committente.md`](../_domande-committente.md), risolta con opzione (a)):
  verificato empiricamente che `packages[]` non enumera pacchetti distinti (`packages[].id` è
  **sempre** l'id della price_list contenitore, 0 eccezioni su 63.383 elementi) ma varianti di
  periodo tariffario per la stessa (pacchetto, data) — il 99%+ delle date ne ha più di una. Senza
  questa colonna la `UNIQUE(price_list_id, price_date)` originaria avrebbe fatto scartare la
  maggioranza dei suggerimenti calcolati dall'algoritmo.
- **Tipo**: `varchar(120)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: etichetta libera, non una FK a un gruppo tariffario risolto — stessa scelta di
  [`catalog_agreement_price_list_period_prices.period_label`](catalog_agreement_price_list_period_prices.md)
  (i nomi legacy sono composti e non sempre classificabili in modo affidabile contro le stagioni).

### list_daily_price / list_weekend_price
- **Legacy**: `packages[].daily_price` / `.weekend_price` (snapshot del listino al momento del calcolo)
- **Descrizione**: prezzo di listino di riferimento al momento del calcolo (per confronto).
- **Tipo**: `numeric(12,2)` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### suggested_price
- **Legacy**: `packages[].suggestedPrice`
- **Descrizione**: prezzo suggerito dall'algoritmo per quella data.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### discount_percentage
- **Legacy**: `packages[].discountPercentage`
- **Descrizione**: sconto percentuale suggerito (alternativa al prezzo assoluto).
- **Tipo**: `numeric(5,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### applied_mode
- **Legacy**: `packages[].isSuggestedPriceApplied` / `.isOriginalPriceApplied` / `.isDiscountApplied` (3 booleani)
- **Descrizione**: cosa ha deciso il gestore per quella data.
- **Tipo**: `enum catalog.suggested_price_mode` — valori: `none`, `suggested`, `original`, `discount`.
- **Default**: `none`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: i 3 booleani mutuamente esclusivi del legacy → un enum. `is_applied` (indice parziale) =
  `applied_mode <> 'none'`.

### computed_at
- **Legacy**: — (nel legacy implicito nell'ultimo `UPDATE`)
- **Descrizione**: quando l'algoritmo ha prodotto questa riga.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### applied_at / applied_by_user_id
- **Legacy**: — (nel legacy non tracciato)
- **Descrizione**: quando e da chi è stato applicato il prezzo/sconto.
- **Tipo**: `timestamptz` / `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `applied_by_user_id` = `public_id` copiato da C5 `identity_access_users`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 49.366 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: tabella di puro output algoritmico: le righe vecchie (date passate) si possono
  hard-deletare con un job di retention; `deleted_at` presente per uniformità.

## Valutazione JSON

Nessun JSON. Il JSON per-anno riscritto in-place → righe per (pacchetto, data). Elimina anche il bug
legacy della cache Redis non invalidata dagli `UPDATE` diretti.

## Suggerimenti per le prestazioni

- Il ricalcolo (job) fa `INSERT ... ON CONFLICT (price_list_id, price_date) DO UPDATE` — non riscrive un
  blob.
- Retention: eliminare le righe con `price_date` nel passato (job periodico).
- Il motore prenotazione legge solo `WHERE property_id = ? AND price_date = ? AND is_applied` — indice
  parziale dedicato.

## Implementazione .NET

**Entità indipendente** (non esposta come owned collection di `PriceList`: è output di un job di calcolo,
non una componente editoriale del listino), modulo `Module.Catalog`, area `Pricing`, schema `catalog`.
Riferisce `PriceList` e `Property` per FK reale (stesso schema).

### Enum proprio

```csharp
// Module.Catalog/Domain/Enums/SuggestedPriceMode.cs
public enum SuggestedPriceMode { None = 0, Suggested = 1, Original = 2, Discount = 3 }
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<SuggestedPriceMode>("catalog", "suggested_price_mode");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<SuggestedPriceMode>("catalog.suggested_price_mode");
```

### `SuggestedPriceConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class SuggestedPriceConfiguration : IEntityTypeConfiguration<SuggestedPrice>
{
	public void Configure(EntityTypeBuilder<SuggestedPrice> builder)
	{
		builder.ToTable("pricing_suggested_prices", "catalog", t => t.HasComment(
			"Prezzo suggerito dall'algoritmo di Revenue Management per un pacchetto in una data specifica."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_suggested_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si riferisce il prezzo suggerito.");
		builder.HasOne<PriceList>()
			.WithMany()
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_suggested_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento, denormalizzato per filtri/indici.");
		builder.HasOne<Property>()
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_suggested_prices_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.PriceDate)
			.HasColumnName("price_date")
			.IsRequired()
			.HasComment("Giorno a cui si riferisce il prezzo suggerito.");

		builder.Property(x => x.PeriodLabel)
			.HasColumnName("period_label")
			.HasMaxLength(120)
			.HasComment("Etichetta libera del periodo/settore legacy (es. \"Fila 1 Alta stagione\"); NULL quando il calcolo non distingue varianti per la data. Aggiunta per D-64.");

		builder.Property(x => x.ListDailyPrice)
			.HasColumnName("list_daily_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo di listino giornaliero di riferimento al momento del calcolo (snapshot).");

		builder.Property(x => x.ListWeekendPrice)
			.HasColumnName("list_weekend_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo di listino weekend di riferimento al momento del calcolo (snapshot).");

		builder.Property(x => x.SuggestedPriceValue)
			.HasColumnName("suggested_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo suggerito dall'algoritmo per la data.");

		builder.Property(x => x.DiscountPercentage)
			.HasColumnName("discount_percentage")
			.HasColumnType("numeric(5,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Sconto percentuale suggerito, alternativa al prezzo assoluto.");

		builder.Property(x => x.AppliedMode)
			.HasColumnName("applied_mode")
			.HasColumnType("catalog.suggested_price_mode")
			.HasDefaultValue(SuggestedPriceMode.None)
			.IsRequired()
			.HasComment("Cosa ha deciso il gestore per la data: nulla, prezzo suggerito, prezzo originale o sconto.");

		builder.Property(x => x.ComputedAt)
			.HasColumnName("computed_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Momento in cui l'algoritmo ha prodotto la riga.");

		builder.Property(x => x.AppliedAt)
			.HasColumnName("applied_at")
			.HasComment("Momento in cui il gestore ha applicato il prezzo/sconto suggerito.");

		builder.Property(x => x.AppliedByUserId)
			.HasColumnName("applied_by_user_id")
			.HasComment("public_id di identity_access_users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete (tabella di output algoritmico; le righe con data passata sono candidate a retention hard-delete via job).");

		// UNIQUE su (price_list_id, price_date, period_label) invece di (price_list_id, price_date):
		// D-64, i dati reali sono multi-variante per data (99%+ delle date con più di una riga
		// packages[] per lo stesso pacchetto/data, distinte per period_label).
		builder.HasIndex(x => new { x.PriceListId, x.PriceDate, x.PeriodLabel })
			.IsUnique()
			.HasDatabaseName("uq_catalog_pricing_suggested_prices_package_date_period");

		builder.HasIndex(x => new { x.PropertyId, x.PriceDate })
			.HasFilter("applied_mode <> 'none'")
			.HasDatabaseName("ix_catalog_pricing_suggested_prices_property_date");
	}
}
```

`SuggestedPrice` implementa `ISoftDeletable` + `IAuditable`. `PriceDate` è `DateOnly`.

### Relazioni

- `price_list_id` → `PriceList` (stesso schema): FK reale, cascade.
- `property_id` → `catalog_properties` (stesso schema, denormalizzato): FK reale, cascade.
- `applied_by_user_id` → `identity_access_users.public_id` (C5): id copiato, nessuna FK cross-schema.

### Note di migrazione

- Job di ricalcolo (ex `revenue.controller.js`, portato server-side — meteo/occupazione via servizi di
  dominio dedicati) esegue `INSERT ... ON CONFLICT (price_list_id, price_date) DO UPDATE`, non riscrive un
  blob JSON: elimina anche il bug della cache Redis non invalidata dagli `UPDATE` diretti legacy.
- Migrazione: esplosione del JSON `suggestedPriceDetails.<anno>[].packages[]` in righe (pacchetto, data);
  l'"anno" del JSON diventa la parte anno di `price_date`; i 3 booleani mutuamente esclusivi
  (`isSuggestedPriceApplied`/`isOriginalPriceApplied`/`isDiscountApplied`) → `applied_mode`.
- Retention: job periodico che rimuove (hard-delete) le righe con `price_date` nel passato.
