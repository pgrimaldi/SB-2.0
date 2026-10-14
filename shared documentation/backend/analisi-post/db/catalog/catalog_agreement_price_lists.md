# Tabella `catalog_agreement_price_lists`

> **Dominio**: C2 — `catalog` · **sottoarea**: `agreement` · **schema**: `catalog` · **nome flat**:
> `catalog_agreement_price_lists` · **forma canonica**: `catalog.agreement_price_lists`.
>
> Rimpiazza la legacy [`hotel_price_lists`](../../../analisi-pre/db/hotel_price_lists.md) (**388 righe** in
> produzione; `isVoucher = 1` su 310, `= 0` su 78). Struttura **quasi identica a `price_lists`** + 3
> campi voucher. Fonti:
> [`analisi-pre/db/hotel_price_lists.md`](../../../analisi-pre/db/hotel_price_lists.md),
> [`analisi-pre/db/price_lists.md`](../../../analisi-pre/db/price_lists.md),
> [`analisi-pre/business-logic/stabilimento/06-convenzioni.md`](../../../analisi-pre/business-logic/stabilimento/06-convenzioni.md).

---

## Descrizione

Listino ordinario (`list_type = regular`) o **voucher** (`list_type = voucher`) venduto da una struttura
partner. Stesso schema tariffario di [`catalog_pricing_price_lists`](catalog_pricing_price_lists.md): testata
qui + figlie [`catalog_agreement_price_list_items`](catalog_agreement_price_list_items.md),
[`catalog_agreement_price_list_sector_prices`](catalog_agreement_price_list_sector_prices.md),
[`catalog_agreement_price_list_period_prices`](catalog_agreement_price_list_period_prices.md),
[`catalog_agreement_price_list_date_prices`](catalog_agreement_price_list_date_prices.md) (override per
data / festivi — in produzione quasi sempre vuota).

Nel legacy `hotel_price_lists` **reimplementa integralmente** lo schema JSON di `price_lists` (stesso
model shape) — [`docs/03-gestione-dati.md`](../../../progettazione/03-gestione-dati.md) suggerisce un **value object
condiviso** `PriceListDefinition`. A DB restano due tabelle parallele (schemi diversi: `pricing` vs
`agreement`, nessuna FK cross-sottoarea nel senso di modulo — sono la stessa `catalog`).

Differenze da `catalog_pricing_price_lists`:

- `+ list_type` (enum `regular`/`voucher`), `+ min_booking_days`, `+ max_voucher_quantity` (solo voucher; forzati a `NULL` per i
  non-voucher lato service — comportamento corretto da mantenere).
- `packageName` **è realmente popolato** qui (a differenza di `price_lists.packageName`, colonna morta) →
  **migrato** come `name`.
- **nessun collegamento con `seasons_periods`**: i nomi in `detailPrices[].name` sono liberi (nessun
  matching automatico) → `catalog_agreement_price_list_period_prices` usa un `period_label` testuale, non
  una FK a `catalog_pricing_tariff_periods`.
- `priceCalculation` / `monthlySetting` / `weekendSetting` risultano **costanti** in produzione (a
  differenza di `price_lists`) — comunque modellati con lo stesso enum/lookup per uniformità.
- la verifica del limite voucher (`maxVoucherQuantity`) avviene nel dominio transazionale
  (`booking_agreement_lines` → C1), non qui.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_agreement_price_lists` | `id` | PK | — |
| `uq_catalog_agreement_price_lists_public_id` | `(public_id)` | UNIQUE | snapshot in C1 (`booking_agreement_lines`) |
| `ix_..._partner_kind` | `(partner_id, list_type) WHERE deleted_at IS NULL` | parziale | listini/voucher di una struttura |

## Campi

### id
- **Legacy**: `hotel_price_lists.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `integer`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità (id legacy preservati).
- **Nullabile**: No.
- **Relazioni**: PK; referenziata dalle figlie `catalog_agreement_price_list_*`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico, **copiato** da C1 (`booking_agreement_lines` referenzia il listino/voucher hotel
  consumato).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C1.

### partner_id
- **Legacy**: `hotel_price_lists.hotelId` (FK reale, `ON UPDATE CASCADE`)
- **Descrizione**: struttura partner a cui appartiene il listino/voucher.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_agreement_partners.id`](catalog_agreement_partners.md)
  `ON DELETE CASCADE`.

### list_type
- **Legacy**: `hotel_price_lists.isVoucher` (`tinyint(1)`; `1` su 310/388)
- **Descrizione**: natura della voce. `regular` = listino prezzi ordinario (stesso schema di
  [`catalog_pricing_price_lists`](catalog_pricing_price_lists.md)); `voucher` = buono/coupon
  riscattabile presso la struttura partner, con vincoli d'uso (`min_booking_days`,
  `max_voucher_quantity`).
- **Tipo**: `enum catalog.agreement_list_type` — valori: `regular`, `voucher` (estendibile senza deploy).
- **Default**: `regular`.
- **Nullabile**: No.
- **Relazioni**: **ortogonale a `kind`** — nel legacy `isVoucher` e `type` variano indipendentemente
  (voucher-package, voucher-additional, regular-package, …): restano due campi distinti.
- **Migrazione**: `isVoucher = 0` → `regular`; `= 1` → `voucher`.

### kind
- **Legacy**: `hotel_price_lists.type` (`package` / `additional`)
- **Descrizione**: tipo di voce (pacchetto o articolo aggiuntivo).
- **Tipo**: `enum catalog.price_list_kind` — valori: `package`, `additional` (stesso enum di
  [`catalog_pricing_price_lists`](catalog_pricing_price_lists.md)).
- **Default**: `package`.
- **Nullabile**: No.
- **Relazioni**: —.

### name
- **Legacy**: `hotel_price_lists.packageName` (**realmente popolato**) + `details.PackageName` / `details.AdditionalItem`
- **Descrizione**: nome del listino/voucher.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **colonna singola** — testo libero digitato dal manager, non bilingue nel legacy (`AddPackage`
  agreements: un solo input; `PackageItemList.jsx:117` fa `details['PackageName'] += ' - copy'`). Qui
  `packageName` **si migra** (27/68 righe DB dev con valore reale) — non è la colonna morta di
  `price_lists`.

### max_people
- **Legacy**: `hotel_price_lists.details.MaxPeopleCount`
- **Descrizione**: persone massime coperte.
- **Tipo**: `smallint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### calculation_method_id
- **Legacy**: `hotel_price_lists.priceCalculation` (in produzione sempre `"proportional"`)
- **Descrizione**: metodo di calcolo prezzo multi-giorno.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → `catalog_pricing_calculation_methods.id` (lookup condiviso — vedi
  [`catalog_pricing_price_lists`](catalog_pricing_price_lists.md#lookup-catalog_pricing_calculation_methods)).

### monthly_setting / weekend_setting
- **Legacy**: `hotel_price_lists.monthlySetting` / `weekendSetting` (costanti in produzione)
- **Descrizione**: impostazioni durata mensile / giorni weekend.
- **Tipo**: `enum catalog.monthly_setting` / `enum catalog.weekend_setting` (stessi di
  [`catalog_pricing_price_lists`](catalog_pricing_price_lists.md)).
- **Default**: `effective_month_duration` / `saturday_sunday`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### allows_public_holidays
- **Legacy**: `hotel_price_lists.allowPublicHolidays` (sempre `0` in produzione)
- **Descrizione**: tariffa festiva dedicata.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: le date festive → figlia analoga a
  [`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md) se mai usata (oggi
  `holidayDates` sempre `[]`).

### min_booking_days
- **Legacy**: `hotel_price_lists.minBookingDays` (`int`; valorizzato solo per `isVoucher = 1`)
- **Descrizione**: durata minima di soggiorno perché il voucher sia applicabile.
- **Tipo**: `smallint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: `CHECK (list_type = 'voucher' OR min_booking_days IS NULL)` — forzato a `NULL` per i non-voucher (come il
  service legacy).

### max_voucher_quantity
- **Legacy**: `hotel_price_lists.maxVoucherQuantity` (`int`; in produzione `NULL` su 77%, altrimenti sempre `1000`)
- **Descrizione**: numero massimo di utilizzi del voucher per cliente per stagione.
- **Tipo**: `smallint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` = nessun limite).
- **Relazioni**: la **verifica** del limite (conteggio utilizzi) è C1 (`booking_agreement_lines`) — qui solo la
  dichiarazione.
- **Note**: `CHECK (list_type = 'voucher' OR max_voucher_quantity IS NULL)`.

### created_by_user_id
- **Legacy**: `hotel_price_lists.createdBy` (server-side da `req.userId`)
- **Descrizione**: operatore che ha creato il listino.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users`.

### created_at / updated_at
- **Legacy**: `hotel_price_lists.createdAt` / `hotel_price_lists.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: `hotel_price_lists.isDeleted`
- **Descrizione**: soft-delete (anche a cascata dalla cancellazione del partner).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON. Stesso trattamento di [`catalog_pricing_price_lists`](catalog_pricing_price_lists.md): scalari +
flag su questa tabella, `details`/`mainPrices`/`detailPrices` nelle 3 figlie. `packageItemString`
ricalcolato in presentazione; `suggestedPriceDetails` (sempre `NULL` qui) non modellato.

## Suggerimenti per le prestazioni

- ~388 righe. Indice `(partner_id, list_type)`. I contatori del pannello Convenzioni → read model.
- Riuso del **motore prezzo** di `catalog_pricing_price_lists` (stesso value object) — un solo algoritmo.

## Implementazione .NET

Entità figlia di [`AgreementPartner`](catalog_agreement_partners.md) (aggregate root dell'area
`Agreement`); testata della gerarchia listino/voucher, radice per le proprie figlie
`AgreementPriceListItem` / `AgreementPriceListSectorPrice` / `AgreementPriceListPeriodPrice` /
`AgreementPriceListDatePrice`. `PublicId` è esposto perché snapshot-referenziato cross-modulo da C1
(`booking_agreement_lines`). Modulo `Module.Catalog`, schema `catalog`.

### Enum proprio

```csharp
// Module.Catalog.Domain.Enums/AgreementListType.cs
public enum AgreementListType { Regular = 0, Voucher = 1 }
```

```csharp
modelBuilder.HasPostgresEnum<AgreementListType>("catalog", "agreement_list_type");
```

```csharp
dataSourceBuilder.MapEnum<AgreementListType>("catalog.agreement_list_type");
```

`kind` (`PriceListKind`), `monthly_setting` (`MonthlySetting`) e `weekend_setting` (`WeekendSetting`) sono
enum **condivisi** con [`catalog_pricing_price_lists`](catalog_pricing_price_lists.md) (stesso schema
`catalog`) — non ridefiniti qui, stesso `HasPostgresEnum`/`MapEnum` registrato una volta sola nel
`DbContext` del modulo.

```csharp
// Module.Catalog.Domain.Entities/AgreementPriceList.cs
namespace Module.Catalog.Domain.Entities;

public sealed class AgreementPriceList : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PartnerId { get; private set; }
	public AgreementPartner Partner { get; private set; } = null!;
	public AgreementListType ListType { get; private set; }
	public PriceListKind Kind { get; private set; }
	public string Name { get; private set; } = null!;
	public decimal? BasePrice { get; private set; }
	public short? MaxPeople { get; private set; }
	public short? CalculationMethodId { get; private set; }
	public MonthlySetting? MonthlySetting { get; private set; }
	public WeekendSetting? WeekendSetting { get; private set; }
	public bool AllowsPublicHolidays { get; private set; }
	public short? MinBookingDays { get; private set; }
	public short? MaxVoucherQuantity { get; private set; }
	public Guid? CreatedByUserId { get; private set; } // public_id di identity_access_users (C5), copiato

	private readonly List<AgreementPriceListItem> _items = new();
	public IReadOnlyCollection<AgreementPriceListItem> Items => _items.AsReadOnly();
	private readonly List<AgreementPriceListSectorPrice> _sectorPrices = new();
	public IReadOnlyCollection<AgreementPriceListSectorPrice> SectorPrices => _sectorPrices.AsReadOnly();
	private readonly List<AgreementPriceListPeriodPrice> _periodPrices = new();
	public IReadOnlyCollection<AgreementPriceListPeriodPrice> PeriodPrices => _periodPrices.AsReadOnly();
	private readonly List<AgreementPriceListDatePrice> _datePrices = new();
	public IReadOnlyCollection<AgreementPriceListDatePrice> DatePrices => _datePrices.AsReadOnly();

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/AgreementPriceListConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AgreementPriceListConfiguration : IEntityTypeConfiguration<AgreementPriceList>
{
	public void Configure(EntityTypeBuilder<AgreementPriceList> builder)
	{
		builder.ToTable("catalog_agreement_price_lists", "catalog", t =>
		{
			t.HasComment("Listino ordinario o voucher venduto da una struttura partner (convenzione).");
			t.HasCheckConstraint("ck_catalog_agreement_price_lists_min_booking_days",
				"list_type = 'voucher' OR min_booking_days IS NULL");
			t.HasCheckConstraint("ck_catalog_agreement_price_lists_max_voucher_quantity",
				"list_type = 'voucher' OR max_voucher_quantity IS NULL");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_lists");
		builder.Property(x => x.Id).HasColumnType("integer").UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7), copiato — C1 booking_agreement_lines referenzia il listino/voucher consumato.");

		builder.Property(x => x.PartnerId)
			.HasColumnName("partner_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Struttura partner a cui appartiene il listino/voucher.");

		builder.Property(x => x.ListType)
			.HasColumnName("list_type")
			.IsRequired()
			.HasDefaultValue(AgreementListType.Regular)
			.HasComment("regular = listino ordinario; voucher = buono riscattabile presso la struttura partner, con vincoli d'uso.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.IsRequired()
			.HasDefaultValue(PriceListKind.Package)
			.HasComment("Tipo di voce: pacchetto o articolo aggiuntivo (enum condiviso con catalog_pricing_price_lists).");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome del listino/voucher (testo libero digitato dal manager).");

		builder.Property(x => x.BasePrice)
			.HasColumnName("base_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo di riferimento del listino/voucher.");

		builder.Property(x => x.MaxPeople)
			.HasColumnName("max_people")
			.HasColumnType("smallint")
			.HasComment("Numero massimo di persone coperte.");

		builder.Property(x => x.CalculationMethodId)
			.HasColumnName("calculation_method_id")
			.HasColumnType("smallint")
			.HasComment("Metodo di calcolo prezzo multi-giorno (lookup condiviso catalog_pricing_calculation_methods).");

		builder.Property(x => x.MonthlySetting)
			.HasColumnName("monthly_setting")
			.HasDefaultValue(Domain.Enums.MonthlySetting.EffectiveMonthDuration)
			.HasComment("Impostazione durata mensile (enum condiviso con catalog_pricing_price_lists).");

		builder.Property(x => x.WeekendSetting)
			.HasColumnName("weekend_setting")
			.HasDefaultValue(Domain.Enums.WeekendSetting.SaturdaySunday)
			.HasComment("Impostazione giorni weekend (enum condiviso con catalog_pricing_price_lists).");

		builder.Property(x => x.AllowsPublicHolidays)
			.HasColumnName("allows_public_holidays")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Indica se il listino/voucher prevede una tariffa festiva dedicata.");

		builder.Property(x => x.MinBookingDays)
			.HasColumnName("min_booking_days")
			.HasColumnType("smallint")
			.HasComment("Durata minima di soggiorno perché il voucher sia applicabile (solo list_type = voucher).");

		builder.Property(x => x.MaxVoucherQuantity)
			.HasColumnName("max_voucher_quantity")
			.HasColumnType("smallint")
			.HasComment("Numero massimo di utilizzi del voucher per cliente per stagione; NULL = nessun limite (solo list_type = voucher).");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access_users (C5), copiato — operatore che ha creato il listino, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete, anche a cascata dalla cancellazione della struttura partner.");

		builder.HasOne(x => x.Partner)
			.WithMany(p => p.PriceLists)
			.HasForeignKey(x => x.PartnerId)
			.HasConstraintName("fk_catalog_agreement_price_lists_partner")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<PricingCalculationMethod>()
			.WithMany()
			.HasForeignKey(x => x.CalculationMethodId)
			.HasConstraintName("fk_catalog_agreement_price_lists_calculation_method")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.Items)
			.WithOne(i => i.PriceList)
			.HasForeignKey(i => i.PriceListId)
			.OnDelete(DeleteBehavior.Cascade);
		builder.HasMany(x => x.SectorPrices)
			.WithOne(s => s.PriceList)
			.HasForeignKey(s => s.PriceListId)
			.OnDelete(DeleteBehavior.Cascade);
		builder.HasMany(x => x.PeriodPrices)
			.WithOne(p => p.PriceList)
			.HasForeignKey(p => p.PriceListId)
			.OnDelete(DeleteBehavior.Cascade);
		builder.HasMany(x => x.DatePrices)
			.WithOne(d => d.PriceList)
			.HasForeignKey(d => d.PriceListId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_agreement_price_lists_public_id");

		builder.HasIndex(x => new { x.PartnerId, x.ListType })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_lists_partner_kind");
	}
}
```

**Note di migrazione**: `id` 1:1 (preservati); `isVoucher` (`0`/`1`) → `list_type` (`regular`/`voucher`);
`packageName` → `name` (qui **realmente popolato**, a differenza della colonna morta omonima di
`price_lists` — si migra il valore, non si scarta); `minBookingDays`/`maxVoucherQuantity` copiati così
come sono (già `NULL` lato service per i non-voucher, il `CHECK` li blinda anche a DB); figlie
(`details`/`mainPrices`/`detailPrices`) esplose nelle 4 tabelle figlie. Il motore prezzo è concettualmente
lo stesso di `catalog_pricing_price_lists` (stesso "shape" `PriceListDefinition` suggerito in
`03-gestione-dati.md`): a livello di dominio conviene estrarre un **servizio di calcolo condiviso** che
opera sulle proprietà comuni delle due entità (via interfaccia, es. `IPriceListPricingFacts`), non un
owned type EF condiviso — le due tabelle restano fisicamente separate (schemi/aggregate diversi) e ognuna
mantiene la propria `IEntityTypeConfiguration`.
