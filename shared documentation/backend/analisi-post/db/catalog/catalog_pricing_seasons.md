# Tabella `catalog_pricing_seasons`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_seasons` · **forma canonica**: `catalog.pricing_seasons`.
>
> Rimpiazza i campi scalari della legacy [`seasons_periods`](../../../analisi-pre/db/seasons_periods.md)
> (**171 righe** = 1 per stabilimento). L'array `periods` va in
> [`catalog_pricing_tariff_periods`](catalog_pricing_tariff_periods.md). Fonti:
> [`analisi-pre/db/seasons_periods.md`](../../../analisi-pre/db/seasons_periods.md),
> [`analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md`](../../../analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md).

---

## Descrizione

Confini della **stagione corrente** e (opzionale) della **stagione successiva** di uno stabilimento — 1
riga per property. I periodi tariffari (alta/media/bassa) che suddividono la stagione vivono in
[`catalog_pricing_tariff_periods`](catalog_pricing_tariff_periods.md).

Nel legacy: `seasonFrom`/`seasonTo` (`NOT NULL` nel model, ma `NULLABLE` nello schema fisico dev — da
confermare), `nextSeasonFrom`/`nextSeasonTo` (opzionali, usati solo dal rinnovo di prenotazioni con pass
stagionale — `updateseasonsperiods` non li usa in logica). `updateseasonsperiods` non attendeva l'`update`
(bug "successo falso") — corretto nel target.

> **Nota — possibile evoluzione futura ([D-17b](../_domande-committente.md)).** L'attuale modello 1:1
> con `catalog_pricing_tariff_periods` (che porta i range dentro `period_group_key`) mantiene la dualità
> legacy "stagione corrente / stagione successiva". Un'alternativa più pulita — **non adottata ora** — è
> una struttura a più tabelle: `catalog_pricing_season_years(property_id, year, starts_on, ends_on)` (1
> riga per anno, "corrente" = quella che contiene oggi, pre-pianificazione pluriennale naturale) +
> `catalog_pricing_tariff_periods` **stabili** (`tier`/`name`, senza anno) + una figlia
> `catalog_pricing_tariff_period_dates(tariff_period_id, year, starts_on, ends_on)` per le date del
> periodo anno per anno, con i prezzi agganciati al periodo **stabile** (il gestore non re-inserisce i
> prezzi ogni anno, come nel legacy). Si valuterà se e quando ci sarà il driver (oggi nessun dato).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_seasons` | `id` | PK | — |
| `uq_catalog_pricing_seasons_property` | `(property_id)` | UNIQUE | 1:1 con la property |

## Campi

### id
- **Legacy**: `seasons_periods.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`catalog_pricing_tariff_periods.season_id`](catalog_pricing_tariff_periods.md).

### property_id
- **Legacy**: `seasons_periods.propertyId`
- **Descrizione**: stabilimento proprietario.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`; `UNIQUE`.

### season_starts_on / season_ends_on
- **Legacy**: `seasons_periods.seasonFrom` / `seasons_periods.seasonTo` (`date`)
- **Descrizione**: primo/ultimo giorno della stagione corrente.
- **Tipo**: `date` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No (nel legacy discrepanza model/schema — qui obbligatori: una stagione senza confini
  non ha senso).
- **Relazioni**: letti anche dalla ricerca pubblica (prezzo minimo del periodo corrente).
- **Note**: `CHECK (season_ends_on >= season_starts_on)`.

### next_season_starts_on / next_season_ends_on
- **Legacy**: `seasons_periods.nextSeasonFrom` / `seasons_periods.nextSeasonTo` (`date`, opzionali; `NULL` ~29–97%)
- **Descrizione**: confini pre-impostati della stagione dell'anno successivo (per il rinnovo dei pass
  stagionali).
- **Tipo**: `date` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: consumati da C1 nel rinnovo delle prenotazioni con `is_seasonal_pass = true`.
- **Note**: `CHECK (next_season_ends_on >= next_season_starts_on)`.

### created_by_user_id
- **Legacy**: `seasons_periods.createdBy`
- **Descrizione**: operatore che ha configurato le stagioni.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users`.

### created_at / updated_at
- **Legacy**: `seasons_periods.createdAt` / `seasons_periods.updatedAt`
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
- **Note**: praticamente non usato (1:1 con property); presente per uniformità.

## Valutazione JSON

Nessun JSON su questa tabella (l'array `periods` è estratto in
[`catalog_pricing_tariff_periods`](catalog_pricing_tariff_periods.md)).

## Suggerimenti per le prestazioni

- 1 riga per property (~171). Lookup su `property_id` (UNIQUE).
- Il calcolo "prezzo minimo del periodo corrente" per la ricerca pubblica non deve fare `LIKE` fra nomi
  periodo e voci listino (anti-pattern `getLowestPrice`): usa le FK verso
  `catalog_pricing_tariff_periods` e `catalog_pricing_price_list_period_prices`, materializzando il minimo
  nel read model di ricerca.

## Implementazione .NET

**Aggregate root** a sé (1:1 con lo stabilimento), modulo `Module.Catalog`, schema `catalog`. Radice della
gerarchia periodi tariffari: figlia
[`catalog_pricing_tariff_periods`](catalog_pricing_tariff_periods.md) (FK `season_id`), esposta come
collezione `PricingSeason.TariffPeriods`. Nessun enum proprio su questa tabella (l'enum `TariffTier` vive
sulla figlia).

### `PricingSeasonConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PricingSeasonConfiguration : IEntityTypeConfiguration<PricingSeason>
{
	public void Configure(EntityTypeBuilder<PricingSeason> builder)
	{
		builder.ToTable("pricing_seasons", "catalog", t =>
		{
			t.HasComment("Confini della stagione corrente (e, opzionale, della successiva) di uno stabilimento.");
			t.HasCheckConstraint("ck_catalog_pricing_seasons_season_range", "season_ends_on >= season_starts_on");
			t.HasCheckConstraint("ck_catalog_pricing_seasons_next_season_range",
				"next_season_ends_on IS NULL OR next_season_starts_on IS NULL OR next_season_ends_on >= next_season_starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_seasons");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario (relazione 1:1).");
		builder.HasOne<Property>()
			.WithOne()
			.HasForeignKey<PricingSeason>(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_seasons_property")
			.OnDelete(DeleteBehavior.Cascade);
		builder.HasIndex(x => x.PropertyId).IsUnique().HasDatabaseName("uq_catalog_pricing_seasons_property");

		builder.Property(x => x.SeasonStartsOn)
			.HasColumnName("season_starts_on")
			.IsRequired()
			.HasComment("Primo giorno della stagione corrente.");

		builder.Property(x => x.SeasonEndsOn)
			.HasColumnName("season_ends_on")
			.IsRequired()
			.HasComment("Ultimo giorno della stagione corrente.");

		builder.Property(x => x.NextSeasonStartsOn)
			.HasColumnName("next_season_starts_on")
			.HasComment("Primo giorno pre-impostato della stagione dell'anno successivo (rinnovo pass stagionali).");

		builder.Property(x => x.NextSeasonEndsOn)
			.HasColumnName("next_season_ends_on")
			.HasComment("Ultimo giorno pre-impostato della stagione dell'anno successivo.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access_users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete (praticamente non usato, 1:1 con la property; presente per uniformità).");

		builder.HasMany(x => x.TariffPeriods).WithOne().HasForeignKey(p => p.SeasonId);
	}
}
```

`PricingSeason` implementa `ISoftDeletable` + `IAuditable`. `SeasonStartsOn`/`SeasonEndsOn`/
`NextSeasonStartsOn`/`NextSeasonEndsOn` sono `DateOnly` (colonne "solo giorno").

### Relazioni

- `property_id` → `catalog_properties` (stesso schema): FK reale 1:1 (`HasOne().WithOne()`), cascade.
- `next_season_starts_on`/`next_season_ends_on` consumati da C1 nel rinnovo delle prenotazioni con
  `is_seasonal_pass = true`: nessuna FK (lettura applicativa, non relazione).

### Note di migrazione

- `id` copiato 1:1 dal legacy `seasons_periods.id`.
- Scalari copiati direttamente (`seasonFrom`/`seasonTo`/`nextSeasonFrom`/`nextSeasonTo`); qui **obbligatori**
  i confini della stagione corrente (nel legacy discrepanza model/schema, risolta rendendoli `NOT NULL`).
- L'array `periods[]` esploso nella figlia [`catalog_pricing_tariff_periods`](catalog_pricing_tariff_periods.md).
- Bug legacy `updateseasonsperiods` (non attendeva l'`UPDATE`, "successo falso") non riprodotto: la
  scrittura passa per un comando applicativo sincrono con `SaveChangesAsync` atteso.
