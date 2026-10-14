# Tabella `catalog_agreement_price_list_stay_prices`

> **Dominio**: C2 — `catalog` · **sottoarea**: `agreement` · **schema**: `catalog` · **nome flat**:
> `catalog_agreement_price_list_stay_prices` · **forma canonica**:
> `catalog.agreement_price_list_stay_prices`.
>
> Figlia di [`catalog_agreement_price_list_period_prices`](catalog_agreement_price_list_period_prices.md).
> Gemella di [`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md) lato
> pricing — rimpiazza la stessa **griglia prezzi per numero di giorni** che nel legacy vive dentro
> `hotel_price_lists.detailPrices[].edit_price_data` (`[{day, defaultPrice, price, isPriceChange, item,
> date}]`), verificata **identica** a quella di `price_lists`. Fonti:
> [`analisi-pre/db/hotel_price_lists.md`](../../../analisi-pre/db/hotel_price_lists.md),
> [`analisi-pre/db/price_lists.md`](../../../analisi-pre/db/price_lists.md). Aggiunta per **D-63**
> ([`_domande-committente.md`](../_domande-committente.md), risolta con opzione (a)): la tabella non
> esisteva nelle schede iniziali — un'omissione di disegno scoperta in migrazione, non una scelta
> deliberata — e senza di essa ~26.000 override reali (`isPriceChange = 1` su 307.517 elementi
> griglia) sarebbero andati persi.

---

## Descrizione

Stessa logica di [`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md#descrizione):
nel legacy il prezzo di un voucher/listino hotel per **N giorni** si ottiene combinando (1) il calcolo
dal motore prezzo a partire dalle tariffe base
([`catalog_agreement_price_list_period_prices`](catalog_agreement_price_list_period_prices.md)) e (2)
un **override manuale** per una durata specifica, tramite la stessa griglia "giorni 1–90" dell'editor
prezzi (`edit_price_data[].price` con `isPriceChange = 1`).

Questa tabella persiste **solo gli override manuali**: una riga per (listino/voucher hotel × periodo
tariffario × settore × numero di giorni). Le durate non override le ricalcola il motore prezzo.

**Verificato empiricamente (2026-09-19, D-63)** su 388 righe `hotel_price_lists`: la griglia
`edit_price_data` usa la stessa forma ISO/`day` 1–90 di `price_lists` (non la forma per-data singola
che [`catalog_agreement_price_list_date_prices`](catalog_agreement_price_list_date_prices.md) accoglie),
con **26.016** righe reali `isPriceChange = 1` distribuite su gran parte dei listini — non un residuo
trascurabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_agreement_price_list_stay_prices` | `id` | PK | — |
| `uq_..._period_sector_days` | `(period_price_id, sector_id, stay_days) WHERE deleted_at IS NULL` | UNIQUE parziale | un override per (periodo, settore, durata) |
| `ix_..._period_days` | `(period_price_id, stay_days) WHERE deleted_at IS NULL` | parziale | lookup del prezzo per durata al momento del `quote` |

## Campi

### id
- **Legacy**: — (nuovo; nel legacy era una posizione nell'array `edit_price_data`)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### period_price_id
- **Legacy**: la riga di `hotel_price_lists.detailPrices` (per-periodo) che contiene l'`edit_price_data`
- **Descrizione**: periodo tariffario (e, tramite esso, listino/voucher hotel) a cui si applica il
  prezzo per durata.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_agreement_price_list_period_prices.id`](catalog_agreement_price_list_period_prices.md)
  `ON DELETE CASCADE`.

### sector_id
- **Legacy**: dedotto dal nome composto di `detailPrices[].name` (es. "Fila 2-3 Bassa stagione") per
  prefisso più lungo contro `catalog_map_sectors` della property (stessa tecnica usata lato pricing per
  `catalog_pricing_price_list_stay_prices.sector_id`).
- **Descrizione**: settore a cui si applica il prezzo per durata.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = override valido per tutti i settori del periodo, quando il nome composto
  non contiene un prefisso di settore riconosciuto).
- **Relazioni**: **FK** → [`catalog_map_sectors.id`](catalog_map_sectors.md) `ON DELETE CASCADE`.

### stay_days
- **Legacy**: `edit_price_data[].day` (intero 1–90)
- **Descrizione**: numero di giorni consecutivi di soggiorno a cui si applica `price`.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `CHECK (stay_days BETWEEN 1 AND 90)`.

### price
- **Legacy**: `edit_price_data[].price`
- **Descrizione**: prezzo impostato manualmente per quella durata.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### is_manual_override
- **Legacy**: `edit_price_data[].isPriceChange` (`0` = valore = `defaultPrice` calcolato; `1` = modificato dal gestore)
- **Descrizione**: `true` sempre per le righe qui presenti (si persistono **solo** gli override).
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `edit_price_data[].defaultPrice` **non** si persiste — è ricalcolabile dal motore prezzo.

### created_at / updated_at
- **Legacy**: della riga contenitore
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nel legacy la modifica riscriveva l'array)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON. `edit_price_data[]` (griglia dentro `detailPrices`) → righe. `day` string/number →
`smallint`; `price` string → `numeric`; `defaultPrice` scartato (ricalcolabile); `item`/`date` di
contesto scartati.

## Suggerimenti per le prestazioni

- Poche righe per listino/voucher (solo le durate effettivamente ritoccate a mano). Il motore prezzo:
  per un soggiorno di N giorni, cerca prima un override `(period_price_id, sector_id, stay_days = N)`;
  se assente, calcola da `daily_price` + metodo di calcolo.
- Materializzare l'intera griglia 1–90 sarebbe ~90 righe × (periodi × settori) per listino: **da
  evitare** (calcolo a runtime + override sparsi), coerente con la scelta gemella di pricing.

## Implementazione .NET

**Entità figlia di secondo livello**: appesa a `AgreementPriceListPeriodPrice` (a sua volta figlia di
`AgreementPriceList`), modulo `Module.Catalog`, schema `catalog`. Esposta come collezione
`AgreementPriceListPeriodPrice.StayPriceOverrides`. Nessun enum proprio. File reali:
`Domain/Entities/AgreementPriceListStayPrice.cs`,
`Infrastructure/Configurations/AgreementPriceListStayPriceConfiguration.cs`, migration
`20260921064813_AddAgreementPriceListStayPricesAndSuggestedPricePeriodLabel`.

### `AgreementPriceListStayPriceConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AgreementPriceListStayPriceConfiguration : IEntityTypeConfiguration<AgreementPriceListStayPrice>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListStayPrice> builder)
	{
		builder.ToTable("agreement_price_list_stay_prices", "catalog", t =>
		{
			t.HasComment("Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario di un listino/voucher hotel.");
			t.HasCheckConstraint("ck_catalog_agreement_price_list_stay_prices_stay_days", "stay_days BETWEEN 1 AND 90");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_stay_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PeriodPriceId)
			.HasColumnName("period_price_id")
			.IsRequired()
			.HasComment("Periodo tariffario (e, tramite esso, listino/voucher hotel) a cui si applica il prezzo per durata.");
		builder.HasOne<AgreementPriceListPeriodPrice>()
			.WithMany(p => p.StayPriceOverrides)
			.HasForeignKey(x => x.PeriodPriceId)
			.HasConstraintName("fk_catalog_agreement_price_list_stay_prices_period_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("Settore a cui si applica il prezzo per durata (NULL = valido per tutti i settori del periodo).");
		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_agreement_price_list_stay_prices_sector")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.StayDays)
			.HasColumnName("stay_days")
			.IsRequired()
			.HasComment("Numero di giorni consecutivi di soggiorno a cui si applica il prezzo (1-90).");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo impostato manualmente dal gestore per la durata indicata.");

		builder.Property(x => x.IsManualOverride)
			.HasColumnName("is_manual_override")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Sempre true per le righe presenti: si persistono solo gli override manuali (isPriceChange legacy).");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete dell'override.");

		builder.HasIndex(x => new { x.PeriodPriceId, x.SectorId, x.StayDays })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_agreement_price_list_stay_prices_period_sector_days");

		builder.HasIndex(x => new { x.PeriodPriceId, x.StayDays })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_stay_prices_period_days");
	}
}
```

`AgreementPriceListStayPrice` implementa `ISoftDeletable` + `IAuditable`. `StayDays` tipo `short`
(`smallint`).

### Relazioni

- `period_price_id` → `AgreementPriceListPeriodPrice` (stesso schema): FK reale,
  `WithMany(p => p.StayPriceOverrides)`, cascade.
- `sector_id` → `catalog_map_sectors` (stesso schema, nullable): FK reale, `ON DELETE CASCADE`.

### Note di migrazione

- Stessa procedura di [`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md#note-di-migrazione):
  da `detailPrices[].edit_price_data[]`, tenere solo le righe con `isPriceChange = 1`; `defaultPrice`
  scartato; `day`/`price` stringa → `smallint`/`numeric`.
- `sector_id` risolto dal nome composto di `detailPrices[].name` per prefisso più lungo contro
  `catalog_map_sectors` della property del partner (via `hotelId → hotels.propertyId`, come già fatto
  per `catalog_agreement_price_list_sector_prices`).
- `period_price_id` risolto tramite la stessa riga `catalog_agreement_price_list_period_prices` già
  creata per quell'elemento di `detailPrices[]` (nessuna collisione di gruppo da gestire: a differenza
  del lato pricing, qui `period_label` è testo libero e ogni elemento genera sempre una riga propria).
