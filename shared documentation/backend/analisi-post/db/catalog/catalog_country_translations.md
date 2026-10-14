# Tabella `catalog_country_translations`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (lookup di dominio) · **schema**: `catalog` · **nome
> flat**: `catalog_country_translations` · **forma canonica**: `catalog.country_translations`.
>
> Tabella figlia di [`catalog_countries`](catalog_countries.md). Sostituisce le colonne
> `country.country_english` / `country.country_italian` con traduzioni **per lingua** (N lingue), coerente
> con la regola [`docs/03-gestione-dati.md` §2.2.B](../../../progettazione/03-gestione-dati.md) (vocabolario
> gestito a runtime con testo mostrato all'utente → tabella `*_translations`, mai `label_it`/`label_en` a
> colonne). Fonti: [`analisi-pre/db/country.md`](../../../analisi-pre/db/country.md).

---

## Descrizione

Nome di un paese in una `culture`. Una riga per (paese × lingua). Il seed iniziale ha 2 righe per paese
(`en`, `it`) dai due campi legacy; nuove lingue si aggiungono senza modifiche di schema.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_country_translations` | `id` | PK | — |
| `uq_..._country_culture` | `(country_id, culture) WHERE deleted_at IS NULL` | UNIQUE parziale | una traduzione per (paese, lingua) |
| `ix_..._culture` | `(culture) WHERE deleted_at IS NULL` | parziale | caricamento elenco paesi per una lingua |

## Campi

### id
- **Legacy**: — (nuovo; nel legacy le 2 lingue erano colonne)
- **Descrizione**: PK tecnica.
- **Tipo**: `smallint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### country_id
- **Legacy**: `country.id`
- **Descrizione**: paese a cui si riferisce la traduzione.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_countries.id`](catalog_countries.md) `ON DELETE CASCADE`.

### culture
- **Legacy**: implicita (`country_english` → `en`, `country_italian` → `it`)
- **Descrizione**: codice cultura BCP-47 (es. `it-IT`, `en-GB`).
- **Tipo**: `varchar(10)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### name
- **Legacy**: `country.country_english` / `country.country_italian`
- **Descrizione**: nome del paese nella lingua indicata.
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

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
- **Note**: praticamente immutabile; presente per uniformità.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Poche decine di righe → cache applicativa per `culture`.

## Implementazione .NET

`CountryTranslation` è un'**entità figlia** di `Country` (non un EF owned type: ha propria PK
`bigint`/`smallint` identity, timestamp e soft-delete come le altre tabelle `*_translations` di questo
dominio — coerente con la regola [`docs/03-gestione-dati.md` §2.2.B](../../../progettazione/03-gestione-dati.md)),
esposta come collezione `Country.Translations`. Modulo `Module.Catalog`, schema `catalog`, tabella
`country_translations`. Nessun enum proprio (`culture` resta `varchar` BCP-47 libero).

### `CountryTranslationConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class CountryTranslationConfiguration : IEntityTypeConfiguration<CountryTranslation>
{
	public void Configure(EntityTypeBuilder<CountryTranslation> builder)
	{
		builder.ToTable("country_translations", "catalog", t => t.HasComment(
			"Nome di un paese in una culture. Una riga per (paese × lingua); sostituisce le colonne country_english/country_italian."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_country_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.CountryId)
			.HasColumnName("country_id")
			.IsRequired()
			.HasComment("Paese a cui si riferisce la traduzione.");

		builder.Property(x => x.Culture)
			.HasColumnName("culture")
			.HasMaxLength(10)
			.IsRequired()
			.HasComment("Codice cultura BCP-47 (es. it-IT, en-GB).");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome del paese nella lingua indicata.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete; praticamente immutabile, presente per uniformità.");

		builder.HasIndex(x => new { x.CountryId, x.Culture })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_country_translations_country_culture");

		builder.HasIndex(x => x.Culture)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_country_translations_culture");

		builder.HasOne(x => x.Country)
			.WithMany(c => c.Translations)
			.HasForeignKey(x => x.CountryId)
			.HasConstraintName("fk_catalog_country_translations_country")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`CountryTranslation` implementa `IAuditable`/`ISoftDeletable`.

### Relazioni

- `CountryId` → `Country` (stesso schema, configurazione in [`catalog_countries.md`](catalog_countries.md)):
  FK reale, `ON DELETE CASCADE`.

### Note di migrazione

- Per ogni riga `country` legacy → 2 righe `CountryTranslation` (`en-GB` da `country_english`, `it-IT` da
  `country_italian`); nuove lingue si aggiungono senza modifiche di schema.
- Nessuna migrazione diretta di `id`: la PK è nuova (il legacy non aveva una riga per lingua).
