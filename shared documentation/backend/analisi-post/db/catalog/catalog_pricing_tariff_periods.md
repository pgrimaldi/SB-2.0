# Tabella `catalog_pricing_tariff_periods`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_tariff_periods` · **forma canonica**: `catalog.pricing_tariff_periods`.
>
> Rimpiazza l'array JSON `seasons_periods.periods` (`[{name, selectedRanges:[{startDate, endDate, key}]}]`).
> Fonti: [`analisi-pre/db/seasons_periods.md`](../../../analisi-pre/db/seasons_periods.md),
> [`analisi-pre/db/price_lists.md` (`detailPrices[].name`)](../../../analisi-pre/db/price_lists.md).

---

## Descrizione

I **periodi tariffari** in cui è suddivisa la stagione di uno stabilimento (es. "Bassa stagione", "Media
stagione", "Alta stagione"), ciascuno con **uno o più intervalli di date** (un periodo può essere **non
contiguo** — es. bassa stagione a inizio *e* fine estate). Nel legacy un periodo aveva `selectedRanges[]`
dentro il JSON; qui **una riga per intervallo**, con `name` (+ `period_group_key`) a raggruppare gli
intervalli dello stesso periodo.

Il `name` era la chiave di correlazione **testuale** (`LIKE`) verso `price_lists.detailPrices[].name` —
correlazione fragile che nel target diventa FK
([`catalog_pricing_price_list_period_prices.tariff_period_id`](catalog_pricing_price_list_period_prices.md)).

> **Nota** — [`catalog_pricing_seasons`](catalog_pricing_seasons.md) tiene una nota di possibile
> evoluzione futura ([D-17b](../_domande-committente.md)): passare a stagioni **per anno** +
> tariff periods **stabili** (`tier`/`name`, senza anno) + una figlia
> `catalog_pricing_tariff_period_dates(tariff_period_id, year, starts_on, ends_on)` per le date anno per
> anno. **Non adottato ora**; l'attuale modello tiene i range in `period_group_key`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_tariff_periods` | `id` | PK | — |
| `ix_..._season` | `(season_id) WHERE deleted_at IS NULL` | parziale | periodi di uno stabilimento |
| `ix_..._season_group` | `(season_id, period_group_key)` | — | intervalli dello stesso periodo |
| `ix_..._range` | `(season_id, starts_on, ends_on)` | — | "che periodo è la data X?" |
| `ex_catalog_pricing_tariff_periods_no_overlap` | `(season_id, COALESCE(period_group_key, -1), daterange(starts_on, ends_on, '[]')) WHERE deleted_at IS NULL` | GiST (`EXCLUDE`) | nessuna sovrapposizione di date dentro lo stesso gruppo (riquadro sotto) |


> ### ✅ Vincolo di non sovrapposizione applicato (2026-09-30) — con una chiave diversa da quella progettata
>
> In vigore sul DB (migration `AddSectorNameUniqueAndPeriodExcludes`, oggi consolidata nella
> `InitialCreate` di `Module.Catalog` — [`docs/42-migration.md` §10](../../../progettazione/42-migration.md)),
> estensione `btree_gist`:
>
> ```sql
> CONSTRAINT ex_catalog_pricing_tariff_periods_no_overlap
>   EXCLUDE USING gist (season_id WITH =,
>                       COALESCE(period_group_key, -1) WITH =,
>                       daterange(starts_on, ends_on, '[]') WITH &&)
>   WHERE (deleted_at IS NULL)
> ```
>
> **Perché non è quello di [`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §8.**
> Il vincolo progettato era su `(season_id, daterange)` e sui dati reali lo violavano 41 coppie. Di
> queste, 27 hanno gruppo diverso: sono un giorno di alta stagione dentro un periodo di bassa, cioè un
> giorno di festa prezzato a parte. Il committente ha confermato il 2026-09-30 che è **voluto** e che
> **prevale il prezzo del periodo più breve** — supera la nota del 2026-09-21 secondo cui gruppi diversi
> non dovevano mai sovrapporsi. Il vincolo originale avrebbe vietato una funzione, non un errore.
>
> - **La chiave è (stagione, gruppo)**, la fascia (`tier`) non entra. Di sostanza: il prezzo è agganciato
>   al gruppo (`pricing_price_list_period_prices.tariff_period_group`), quindi due periodi dello stesso
>   gruppo hanno per costruzione lo stesso prezzo. Di forma: `tier::text` in un'espressione di indice non
>   è ammesso (il cast di un enum a testo è `STABLE`, non `IMMUTABLE`: PostgreSQL rifiuta con `42P17`).
> - **`COALESCE(period_group_key, -1)`**: in un `EXCLUDE` una colonna `NULL` non conflitta con niente;
>   senza `COALESCE` le righe senza gruppo resterebbero scoperte.
> - **Le 14 coppie residue** (stesso gruppo, stessa fascia, tutte per contenimento) sono ridondanza a
>   prezzo invariato: **l'ETL scarta il periodo contenuto**.
>
> ⚠️ Conseguenza per il motore di prezzo: una data può cadere in **più periodi di gruppi diversi**, e la
> regola di risoluzione è «vince il periodo più breve». Non è il vincolo a garantirlo: va implementata.
> Vedi anche `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`, voci B7 e B11.

## Campi

### id
- **Legacy**: — (nuovo; il legacy identificava per posizione)
- **Descrizione**: PK tecnica di un singolo intervallo tariffario.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da
  [`catalog_pricing_price_list_period_prices.tariff_period_id`](catalog_pricing_price_list_period_prices.md).

### season_id
- **Legacy**: `seasons_periods.id` (contenitore di `periods`)
- **Descrizione**: stagione a cui appartiene il periodo.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_seasons.id`](catalog_pricing_seasons.md) `ON DELETE CASCADE`.

### name
- **Legacy**: `seasons_periods.periods[].name`
- **Descrizione**: nome del periodo tariffario (es. "Alta stagione").
- **Tipo**: `varchar(80)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: nel legacy usato per `LIKE` contro i listini; qui è solo etichetta, le correlazioni sono per
  `id`. Righe con lo stesso `name` nella stessa stagione = intervalli dello stesso periodo.

### period_group_key
- **Legacy**: implicito (elemento dell'array `periods` che raggruppava più `selectedRanges`)
- **Descrizione**: chiave che raggruppa gli intervalli non contigui di uno stesso periodo tariffario.
- **Tipo**: `smallint` (o `uuid`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: assegnata in migrazione (una per elemento di `periods[]`); i listini si agganciano al
  **gruppo** (tutte le righe con stesso `season_id` + `period_group_key`) tramite la FK verso una di esse
  o meglio verso `period_group_key` — vedi `catalog_pricing_price_list_period_prices`.

### tier
- **Legacy**: dedotto dal `name` (`Bassa`/`Media`/`Alta` stagione)
- **Descrizione**: livello tariffario normalizzato, per confronti indipendenti dalla lingua/nome.
- **Tipo**: `enum catalog.tariff_tier` — valori: `low`, `mid`, `high`, `peak`, `custom`.
- **Default**: `custom`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: in migrazione mappatura best-effort dal nome; `custom` per nomi non riconosciuti. Utile per
  il motore prezzo e la ricerca ("prezzo bassa stagione") senza dipendere dalla stringa.

### starts_on / ends_on
- **Legacy**: `seasons_periods.periods[].selectedRanges[].startDate` / `.endDate`
- **Descrizione**: intervallo di date (incluso) coperto da questa riga.
- **Tipo**: `date` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `CHECK (ends_on >= starts_on)`. `key` di `react-date-range` (`selection1`/`selection2`)
  scartato.

### created_at / updated_at
- **Legacy**: `seasons_periods.createdAt` / `updatedAt` (della riga contenitore)
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

Nessun JSON — elimina `periods[]` e `selectedRanges[]`. Il periodo non contiguo = N righe con stesso
`period_group_key`.

## Suggerimenti per le prestazioni

- Poche righe per stabilimento (unità × pochi intervalli). "Che periodo è la data X?" =
  `WHERE season_id = ? AND ? BETWEEN starts_on AND ends_on`.
- Il motore prezzo (C1) risolve una volta il `tariff_period_id`/`period_group_key` per la data richiesta
  e poi cerca i prezzi in `catalog_pricing_price_list_period_prices` per `id`.

## Implementazione .NET

**Entità figlia** dell'aggregate `PricingSeason` (modulo `Module.Catalog`, schema `catalog`), esposta come
collezione `PricingSeason.TariffPeriods`. Una riga per intervallo di date; più righe con lo stesso
`(season_id, period_group_key)` formano un periodo tariffario non contiguo.

### Enum proprio

```csharp
// Module.Catalog/Domain/Enums/TariffTier.cs
public enum TariffTier { Low = 0, Mid = 1, High = 2, Peak = 3, Custom = 4 }
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<TariffTier>("catalog", "tariff_tier");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<TariffTier>("catalog.tariff_tier");
```

### `TariffPeriodConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class TariffPeriodConfiguration : IEntityTypeConfiguration<TariffPeriod>
{
	public void Configure(EntityTypeBuilder<TariffPeriod> builder)
	{
		builder.ToTable("pricing_tariff_periods", "catalog", t =>
		{
			t.HasComment("Intervallo di date di un periodo tariffario (bassa/media/alta stagione), eventualmente non contiguo.");
			t.HasCheckConstraint("ck_catalog_pricing_tariff_periods_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_tariff_periods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SeasonId)
			.HasColumnName("season_id")
			.IsRequired()
			.HasComment("Stagione a cui appartiene il periodo.");
		builder.HasOne<PricingSeason>()
			.WithMany(s => s.TariffPeriods)
			.HasForeignKey(x => x.SeasonId)
			.HasConstraintName("fk_catalog_pricing_tariff_periods_season")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(80)
			.IsRequired()
			.HasComment("Nome del periodo tariffario (es. Alta stagione); solo etichetta, le correlazioni sono per id.");

		builder.Property(x => x.PeriodGroupKey)
			.HasColumnName("period_group_key")
			.IsRequired()
			.HasComment("Chiave che raggruppa gli intervalli non contigui dello stesso periodo tariffario.");

		builder.Property(x => x.Tier)
			.HasColumnName("tier")
			.HasColumnType("catalog.tariff_tier")
			.HasDefaultValue(TariffTier.Custom)
			.IsRequired()
			.HasComment("Livello tariffario normalizzato, indipendente dal nome/lingua.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Inizio dell'intervallo di date coperto dalla riga.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Fine dell'intervallo di date coperto dalla riga.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete dell'intervallo.");

		builder.HasIndex(x => x.SeasonId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_tariff_periods_season");

		builder.HasIndex(x => new { x.SeasonId, x.PeriodGroupKey })
			.HasDatabaseName("ix_catalog_pricing_tariff_periods_season_group");

		builder.HasIndex(x => new { x.SeasonId, x.StartsOn, x.EndsOn })
			.HasDatabaseName("ix_catalog_pricing_tariff_periods_range");
	}
}
```

`TariffPeriod` implementa `ISoftDeletable` + `IAuditable`. `StartsOn`/`EndsOn` sono `DateOnly`.

### Relazioni

- `season_id` → `PricingSeason` (stesso schema): FK reale, `WithMany(s => s.TariffPeriods)`, cascade.
- `period_group_key` **non** è una FK: raggruppa righe sorelle della stessa `season_id` (stesso pattern di
  [`catalog_pricing_price_list_period_prices.tariff_period_group`](catalog_pricing_price_list_period_prices.md),
  che referenzia questo gruppo senza vincolo DB — vedi nota in quella scheda).
- `id` referenziato da
  [`catalog_pricing_price_list_period_prices.tariff_period_id`](catalog_pricing_price_list_period_prices.md)
  solo nel senso di "una riga del gruppo"; la correlazione operativa passa per `(season_id,
  period_group_key)`, non per singolo `id`.

### Note di migrazione

- `seasons_periods.periods[]` → gruppi (`period_group_key` assegnato una volta per elemento
  dell'array, in migrazione); `selectedRanges[]` di ciascun periodo → una riga per intervallo.
- `tier` assegnato **best-effort** dal `name` (`"Bassa"`/`"Media"`/`"Alta"` stagione → `low`/`mid`/`high`);
  nomi non riconosciuti → `custom`.
- `key` di `react-date-range` (`selection1`/`selection2`) scartato: non ha significato di dominio.
