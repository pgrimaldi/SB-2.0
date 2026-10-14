# Tabella `catalog_pricing_price_list_date_prices`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_price_list_date_prices` · **forma canonica**: `catalog.pricing_price_list_date_prices`.
>
> Rimpiazza `price_lists.holidayDates` (`["DD/MM/YYYY", …]`, date festive con tariffa dedicata, **senza
> prezzo** nel JSON) **e** le eventuali righe **per-data** di `price_lists.detailPrices[].edit_price_data`
> nella forma legacy `{date:"15/08/2024", day:"Thursday", price}`. La forma **attiva** di `edit_price_data`
> (griglia per **numero di giorni** 1–90) va invece in
> [`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md) — vedi
> [D-15b](../_domande-committente.md). Fonti:
> [`analisi-pre/db/price_lists.md` (campi `detailPrices` / `holidayDates` / `allowPublicHolidays`)](../../../analisi-pre/db/price_lists.md).

---

## Descrizione

Deroghe di prezzo per **data di calendario** di una voce di listino: date festive con tariffa dedicata
(`kind = holiday`) e override puntuali su una data specifica (`kind = override`, es. Ferragosto). Una
riga per (voce × data × tipo). **Distinta** dalla griglia prezzi per *numero di giorni*
([`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md)).

`price` valorizzato per gli override; `NULL` per le date festive "solo marcate" (il motore prezzo applica
la logica festiva della voce). In produzione, sui listini hotel `holidayDates` è sempre `[]`; sui
`price_lists` è raro.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_price_list_date_prices` | `id` | PK | — |
| `uq_..._package_date_kind` | `(price_list_id, price_date, kind) WHERE deleted_at IS NULL` | UNIQUE parziale | una deroga per (pacchetto, data, tipo) |
| `ix_..._package_date` | `(price_list_id, price_date) WHERE deleted_at IS NULL` | parziale | "c'è una deroga per il pacchetto P il giorno D?" |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### price_list_id
- **Legacy**: `price_lists.id`
- **Descrizione**: pacchetto a cui si applica la deroga.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_price_lists.id`](catalog_pricing_price_lists.md) `ON DELETE CASCADE`.

### period_price_id
- **Legacy**: la riga di `detailPrices` che conteneva l'`edit_price_data`
- **Descrizione**: periodo tariffario a cui la deroga è agganciata (per gli override; `NULL` per le date
  festive globali del pacchetto).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_pricing_price_list_period_prices.id`](catalog_pricing_price_list_period_prices.md)
  `ON DELETE CASCADE`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1.414 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### kind
- **Legacy**: struttura di provenienza (`edit_price_data` → `override`; `holidayDates` → `holiday`)
- **Descrizione**: natura della deroga.
- **Tipo**: `enum catalog.date_price_kind` — valori: `override`, `holiday`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### price_date
- **Legacy**: `edit_price_data[].date` (`"15/08/2024"`) / `holidayDates[]` (`"DD/MM/YYYY"`)
- **Descrizione**: la data a cui si applica il prezzo/la deroga.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: in migrazione parsing `DD/MM/YYYY` → `date`.

### price
- **Legacy**: `edit_price_data[].price` (stringa numerica) — per `holiday` non presente nel JSON
- **Descrizione**: prezzo giornaliero per quella data. Per `kind = override` è valorizzato; per `kind =
  holiday` può essere `NULL` (il motore prezzo applica la regola festiva del pacchetto).
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: della riga contenitore
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (modifica = riscrittura array)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON. `holidayDates[]` (lista piatta) + eventuali `edit_price_data` per-data legacy → una tabella date-keyed con `kind`. La griglia per numero di giorni di `edit_price_data` va in `catalog_pricing_price_list_stay_prices`.

## Suggerimenti per le prestazioni

- Poche righe per pacchetto (le deroghe sono eccezioni). Il motore prezzo controlla l'esistenza di una
  riga per `(price_list_id, price_date)` prima di applicare la tariffa di periodo.
- Indicizzabile per data → query "quali pacchetti hanno una tariffa speciale il 15 agosto" (impossibile
  nel legacy col JSON).

## Implementazione .NET

**Entità figlia** dell'aggregate `PriceList` (modulo `Module.Catalog`, schema `catalog`), esposta come
collezione `PriceList.DatePrices`. FK reale (non `OwnsMany`) perché referenzia opzionalmente
`PriceListPeriodPrice`, un'altra entità figlia della stessa radice.

### Enum proprio

```csharp
// Module.Catalog/Domain/Enums/DatePriceKind.cs
public enum DatePriceKind { Override = 0, Holiday = 1 }
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<DatePriceKind>("catalog", "date_price_kind");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<DatePriceKind>("catalog.date_price_kind");
```

### `PriceListDatePriceConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PriceListDatePriceConfiguration : IEntityTypeConfiguration<PriceListDatePrice>
{
	public void Configure(EntityTypeBuilder<PriceListDatePrice> builder)
	{
		builder.ToTable("pricing_price_list_date_prices", "catalog", t => t.HasComment(
			"Deroga di prezzo per data di calendario: data festiva con tariffa dedicata o override puntuale."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_date_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si applica la deroga.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.DatePrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_date_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.PeriodPriceId)
			.HasColumnName("period_price_id")
			.HasComment("Periodo tariffario a cui la deroga è agganciata (NULL per le date festive globali del pacchetto).");
		builder.HasOne<PriceListPeriodPrice>()
			.WithMany()
			.HasForeignKey(x => x.PeriodPriceId)
			.HasConstraintName("fk_catalog_pricing_price_list_date_prices_period_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("catalog.date_price_kind")
			.IsRequired()
			.HasComment("Natura della deroga: override puntuale o data festiva.");

		builder.Property(x => x.PriceDate)
			.HasColumnName("price_date")
			.IsRequired()
			.HasComment("Data a cui si applica il prezzo/la deroga.");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo giornaliero per la data (NULL per holiday quando si applica la regola festiva del pacchetto).");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della deroga.");

		builder.HasIndex(x => new { x.PriceListId, x.PriceDate, x.Kind })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_date_prices_package_date_kind");

		builder.HasIndex(x => new { x.PriceListId, x.PriceDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_date_prices_package_date");
	}
}
```

`PriceDate` è `DateOnly` (colonna "solo giorno" `date`, §2 del template). `PriceListDatePrice` implementa
`ISoftDeletable` + `IAuditable`; nessun evento proprio.

### Relazioni

- `price_list_id` → `PriceList` (stesso schema): FK reale, `WithMany(p => p.DatePrices)`, cascade.
- `period_price_id` → `PriceListPeriodPrice` (stesso schema, altra figlia della stessa radice): FK reale
  nullable, cascade (se il periodo viene rimosso, cadono anche i suoi override per data).

### Note di migrazione

- Motore prezzo: precedenza `date override > holiday > period price > sector base price` (da confermare
  col committente, coerente con la logica legacy che controlla prima le deroghe per data).
- Parsing `DD/MM/YYYY` → `DateOnly` per `holidayDates[]` e per gli eventuali `edit_price_data` per-data
  legacy.
- `holidayDates[]` → righe `kind = holiday` con `price = NULL` quando `price_lists.allowPublicHolidays`.
- Deduplica su `(price_list_id, price_date, kind)` in caso di doppioni nel JSON di origine.
