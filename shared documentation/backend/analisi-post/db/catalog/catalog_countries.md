# Tabella `catalog_countries`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (lookup di dominio) · **schema**: `catalog` · **nome
> flat**: `catalog_countries` · **forma canonica**: `catalog.countries`.
>
> Rimpiazza la legacy [`country`](../../../analisi-pre/db/country.md) (**11 righe** in produzione: nazioni
> affacciate su Adriatico/Mediterraneo). Fonti:
> [`analisi-pre/db/country.md`](../../../analisi-pre/db/country.md),
> [`analisi-pre/business-logic/systemadmin/03-dati-di-piattaforma-e-lookup.md`](../../../analisi-pre/business-logic/systemadmin/03-dati-di-piattaforma-e-lookup.md).
>
> **Nota di collocazione**: nel legacy è una lookup di piattaforma senza `propertyId`. È referenziata
> dai form di C2 ([`catalog_properties.country_id`](catalog_properties.md),
> [`catalog_bathhouses.country_id`](catalog_bathhouses.md)) e la usa anche C5 (indirizzo cliente). Qui
> vive nello schema `catalog` come lookup di dominio; i moduli che ne hanno bisogno copiano il `code`
> ISO (stabile) — non una FK cross-schema.

---

## Descrizione

Elenco (non esaustivo) delle nazioni selezionabili nei form. Nel legacy solo 11 righe, gestibili solo
via DB (l'endpoint `createCountry` non ha consumer UI, nessun controllo di ruolo). Correzioni:

- **`+ code` ISO 3166-1 alpha-2** (il legacy non ne aveva) — chiave stabile per i riferimenti cross-modulo.
- **etichette tradotte → tabella figlia** `catalog_country_translations` (`country_id`, `culture`,
  `name`), N lingue, invece di `country_english`/`country_italian` a colonne
  ([`docs/03-gestione-dati.md` §2.2.B](../../../progettazione/03-gestione-dati.md)).
- **`status int` → `is_active boolean`**.
- **doppia coppia di timestamp** (`created_at`/`updated_at` snake_case realmente popolati +
  `createdAt`/`updatedAt` sempre `0000-00-00`) → una sola coppia.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_countries` | `id` | PK | — |
| `uq_catalog_countries_code` | `(code)` | UNIQUE | riferimento stabile (ISO), copiato cross-modulo |

## Campi

### id
- **Legacy**: `country.id` (con buchi: id 4, 10, 12 cancellati fisicamente)
- **Descrizione**: PK tecnica.
- **Tipo**: `smallint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `catalog_country_translations.country_id`.

### code
- **Legacy**: — (nuovo; da ricavare dal nome in migrazione)
- **Descrizione**: codice ISO 3166-1 alpha-2 (`IT`, `ES`, `HR`, …).
- **Tipo**: `char(2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`; è il valore **copiato** dai moduli che referenziano un paese (mai FK
  cross-schema).
- **Note**: in migrazione mappatura dai nomi legacy (`"Italy"/"Italia"` → `IT`, ecc.); le varianti
  sporche di `properties.country` (`"india"`, `"Italy"` vs `"Italia"`) si normalizzano tutte al `code`.

### is_active
- **Legacy**: `country.status` (`int`, sempre `1`)
- **Descrizione**: paese selezionabile nei form (`countryList` filtrava `status = 1`).
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### sort_order
- **Legacy**: implicito (ordine di `id`)
- **Descrizione**: ordine nelle tendine (es. Italia in cima).
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: `country.created_at` / `country.updated_at` (snake_case — gli unici realmente popolati)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: la coppia camelCase `country.createdAt`/`updatedAt` (sempre `0000-00-00 00:00:00`) **non**
  è migrata.

### deleted_at
- **Legacy**: — (il legacy faceva cancellazioni fisiche — id mancanti 4/10/12)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: un paese referenziato da property/bathhouse/cliente non si cancella fisicamente.

## Tabella figlia `catalog_country_translations`

| Colonna | Legacy | Tipo | Note |
|---|---|---|---|
| `id` | — | `smallint` IDENTITY | PK |
| `country_id` | — | `smallint` | **FK** → `catalog_countries.id` `ON DELETE CASCADE` |
| `culture` | — | `varchar(10)` | es. `it-IT`, `en-GB` |
| `name` | `country.country_english` / `country.country_italian` | `varchar(120)` | nome tradotto |
| `created_at`/`updated_at`/`deleted_at` | — | `timestamptz` | standard |

`UNIQUE (country_id, culture) WHERE deleted_at IS NULL`. Seed: 2 righe per paese (`en`, `it`) dai due
campi legacy.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- ~11 righe → cache applicativa. Le traduzioni si caricano una volta per `culture`.

## Implementazione .NET

`Country` è un'**entità lookup** di dominio (nessun `public_id`: referenziata solo internamente allo
schema `catalog` e da altri moduli tramite il `code` ISO copiato, mai una FK cross-schema — §3 del
template). `CountryTranslation` è un'**entità figlia** (non owned type: ha propria PK, timestamp e
soft-delete come le altre tabelle `*_translations` di questo dominio — configurazione descritta a sé in
[`catalog_country_translations.md`](catalog_country_translations.md), qui solo la relazione
`Country.Translations`). Modulo `Module.Catalog`, schema `catalog`, tabella `countries`. Nessun enum
proprio (`is_active` resta `boolean`).

### `CountryConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
	public void Configure(EntityTypeBuilder<Country> builder)
	{
		builder.ToTable("countries", "catalog", t => t.HasComment(
			"Elenco (non esaustivo) delle nazioni selezionabili nei form; lookup di dominio referenziata dai moduli tramite il code ISO copiato."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_countries");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasColumnType("char(2)")
			.IsRequired()
			.HasComment("Codice ISO 3166-1 alpha-2 (IT, ES, HR, ...), valore copiato dai moduli che referenziano un paese — mai FK cross-schema.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Paese selezionabile nei form.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordine nelle tendine (es. Italia in cima).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: un paese referenziato da property/bathhouse/cliente non si cancella fisicamente.");

		builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_catalog_countries_code");
	}
}
```

`Country` implementa `IAuditable`/`ISoftDeletable`.

### Relazioni

Nessuna FK in uscita. Referenziata (stesso schema) da `Property.CountryId`, `Bathhouse.CountryId` (FK
reali, definite nelle rispettive Configuration) e da `CountryTranslation.CountryId` (FK reale, definita in
[`catalog_country_translations.md`](catalog_country_translations.md)). Il `code` ISO è il valore copiato
dai moduli esterni allo schema `catalog` (es. C5 indirizzo cliente).

### Note di migrazione

- `country.country` legacy (nome libero) → derivare il `code` ISO in migrazione
  (`"Italy"`/`"Italia"` → `IT`, ecc.); le varianti sporche consumate da `properties.country`
  (`"india"`, mismatch di capitalizzazione) si normalizzano tutte al `code` risolto.
- `status` → `is_active`; la coppia camelCase `createdAt`/`updatedAt` (sempre `0000-00-00`) **non** è
  migrata, solo `created_at`/`updated_at` snake_case.
- Valutare col committente il seed completo ISO 3166-1 invece delle sole 11 righe legacy (nazioni
  Adriatico/Mediterraneo).
