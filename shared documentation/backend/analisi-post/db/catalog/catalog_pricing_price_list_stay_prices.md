# Tabella `catalog_pricing_price_list_stay_prices`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_price_list_stay_prices` · **forma canonica**:
> `catalog.pricing_price_list_stay_prices`.
>
> Figlia di [`catalog_pricing_price_list_period_prices`](catalog_pricing_price_list_period_prices.md).
> Rimpiazza la **griglia prezzi per numero di giorni** che nel legacy vive dentro
> `price_lists.detailPrices[].edit_price_data` (`[{day, defaultPrice, price, isPriceChange, item, date}]`).
> Fonti:
> [`analisi-pre/db/price_lists.md`](../../../analisi-pre/db/price_lists.md),
> `front-pre/src/components/Price/EditPriceTableInput.js`, `EditPriceListContent.js`,
> `SecondTableChildren.js` (editor "Second Table"). Verificato — [D-15b](../_domande-committente.md).

---

## Descrizione

Nel legacy il prezzo di una prenotazione di **N giorni** si ottiene in due modi combinati:

1. **calcolato** dal motore prezzo a partire dalle tariffe base
   ([`catalog_pricing_price_list_period_prices`](catalog_pricing_price_list_period_prices.md):
   `daily_price` / `weekend_price`) secondo il metodo di calcolo (`proportional` / `daily` /
   `daily_with_weekend` / `logarithmic`), con i bucket fissi `days_7_price` / `days_14_price` /
   `monthly_price` per quelle durate;
2. **impostato manualmente** dal gestore per una durata specifica, tramite la griglia "giorni 1–90"
   dell'editor prezzi (`EditPriceListContent` mostra la tabella *days | price*; `EditPriceTableInput`
   scrive `edit_price_data[].price` e marca `isPriceChange = 1`; toccare `day = 7` riscrive anche
   `days_7_price`).

Questa tabella persiste **solo gli override manuali** (`isPriceChange = 1`): una riga per (voce di
listino × periodo tariffario × settore × numero di giorni). Le durate non override le ricalcola il
motore prezzo.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_price_list_stay_prices` | `id` | PK | — |
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
- **Legacy**: la riga di `price_lists.detailPrices` (per-periodo) che contiene l'`edit_price_data`
- **Descrizione**: periodo tariffario (e, tramite esso, voce di listino) a cui si applica il prezzo per
  durata.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_price_list_period_prices.id`](catalog_pricing_price_list_period_prices.md)
  `ON DELETE CASCADE`.

### sector_id
- **Legacy**: dedotto dalla "combination" dell'editor (settore × periodo — `combo.name` in
  `SecondTableChildren`)
- **Descrizione**: settore a cui si applica il prezzo per durata.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = override valido per tutti i settori del periodo, se l'editor lo consente;
  da confermare in migrazione).
- **Relazioni**: **FK** → [`catalog_map_sectors.id`](catalog_map_sectors.md) `ON DELETE CASCADE`.

### stay_days
- **Legacy**: `edit_price_data[].day` (intero 1–90; l'editor filtra `x.day <= 90`)
- **Descrizione**: numero di giorni consecutivi di soggiorno a cui si applica `price`.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `CHECK (stay_days BETWEEN 1 AND 90)`; per `stay_days = 7 / 14 / 30` il valore duplica
  (coerentemente col legacy) i bucket `days_7_price` / `days_14_price` / `monthly_price` di
  `catalog_pricing_price_list_period_prices`.

### price
- **Legacy**: `edit_price_data[].price`
- **Descrizione**: prezzo totale (o giornaliero — da confermare col motore prezzo legacy) impostato
  manualmente per quella durata.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### is_manual_override
- **Legacy**: `edit_price_data[].isPriceChange` (`0` = valore = `defaultPrice` calcolato; `1` = modificato dal gestore)
- **Descrizione**: `true` sempre per le righe qui presenti (si persistono **solo** gli override). Campo
  esplicito per chiarezza e per un eventuale futuro "materializza tutta la griglia".
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

Nessun JSON. `edit_price_data[]` (griglia dentro `detailPrices`) → righe. `day` string/number → `smallint`;
`price` string → `numeric`; `defaultPrice` scartato (ricalcolabile); `item`/`date` di contesto scartati.

## Suggerimenti per le prestazioni

- Poche righe per voce di listino (solo le durate effettivamente ritoccate a mano). Il motore prezzo:
  per un soggiorno di N giorni, cerca prima un override `(period_price_id, sector_id, stay_days = N)`;
  se assente, calcola da `daily_price` + metodo di calcolo (con i bucket 7/14/30).
- Materializzare l'intera griglia 1–90 sarebbe ~90 righe × (periodi × settori) per voce: **da evitare**
  (calcolo a runtime + override sparsi).

## Implementazione .NET

**Entità figlia di secondo livello**: appesa a `PriceListPeriodPrice` (a sua volta figlia di `PriceList`),
modulo `Module.Catalog`, schema `catalog`. Esposta come collezione
`PriceListPeriodPrice.StayPriceOverrides`. Nessun enum proprio.

### `PriceListStayPriceConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PriceListStayPriceConfiguration : IEntityTypeConfiguration<PriceListStayPrice>
{
	public void Configure(EntityTypeBuilder<PriceListStayPrice> builder)
	{
		builder.ToTable("pricing_price_list_stay_prices", "catalog", t =>
		{
			t.HasComment("Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario.");
			t.HasCheckConstraint("ck_catalog_pricing_price_list_stay_prices_stay_days", "stay_days BETWEEN 1 AND 90");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_stay_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PeriodPriceId)
			.HasColumnName("period_price_id")
			.IsRequired()
			.HasComment("Periodo tariffario (e, tramite esso, voce di listino) a cui si applica il prezzo per durata.");
		builder.HasOne<PriceListPeriodPrice>()
			.WithMany(p => p.StayPriceOverrides)
			.HasForeignKey(x => x.PeriodPriceId)
			.HasConstraintName("fk_catalog_pricing_price_list_stay_prices_period_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("Settore a cui si applica il prezzo per durata (NULL = valido per tutti i settori del periodo, da confermare in migrazione).");
		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_price_list_stay_prices_sector")
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

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete dell'override.");

		builder.HasIndex(x => new { x.PeriodPriceId, x.SectorId, x.StayDays })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_stay_prices_period_sector_days");

		builder.HasIndex(x => new { x.PeriodPriceId, x.StayDays })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_stay_prices_period_days");
	}
}
```

`PriceListStayPrice` implementa `ISoftDeletable` + `IAuditable`. `StayDays` tipo `short` (`smallint`).

### Relazioni

- `period_price_id` → `PriceListPeriodPrice` (stesso schema): FK reale,
  `WithMany(p => p.StayPriceOverrides)`, cascade.
- `sector_id` → `catalog_map_sectors` (stesso schema, nullable): FK reale, `ON DELETE CASCADE` (coerente
  con la semantica di override sparso — se il settore sparisce, l'override per quel settore non ha più
  senso).

### Note di migrazione

- Motore prezzo: per un soggiorno di N giorni, cerca prima un override
  `(period_price_id, sector_id, stay_days = N)`; se assente, calcola da `daily_price` + metodo di calcolo
  (con i bucket 7/14/30 di `catalog_pricing_price_list_period_prices`). Precedenza rispetto agli override
  per data di calendario ([`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md))
  da confermare col committente/codice legacy — indicativamente `date override > stay override > calcolo`.
- Da `detailPrices[].edit_price_data[]`: tenere solo le righe con `isPriceChange = 1`; `defaultPrice`
  scartato (ricalcolabile dal motore prezzo); `day`/`price` stringa → `smallint`/`numeric`.
- Risoluzione di settore e periodo dalla "combination" dell'editor legacy (`combo.name` in
  `SecondTableChildren.js`).
