# Tabella `catalog_agreement_partners`

> **Dominio**: C2 — `catalog` · **sottoarea**: `agreement` · **schema**: `catalog` · **nome flat**:
> `catalog_agreement_partners` · **forma canonica**: `catalog.agreement_partners`.
>
> Rimpiazza la legacy [`hotels`](../../../analisi-pre/db/hotels.md) (**112 righe** in produzione; feature
> "Convenzioni / Hotel" entrata in produzione **aprile 2026**, usata da ~17 stabilimenti). Fonti:
> [`analisi-pre/db/hotels.md`](../../../analisi-pre/db/hotels.md),
> [`analisi-pre/business-logic/stabilimento/06-convenzioni.md`](../../../analisi-pre/business-logic/stabilimento/06-convenzioni.md).
>
> **Nota di nomenclatura**: rinominata `agreement_partners` — non è un modulo di prenotazione alberghiera
> (stanze/check-in). È l'anagrafica delle **strutture partner** ("convenzioni") di uno stabilimento.

> ⛔ **Manca una figlia: `catalog_agreement_reports`** — scheda da scrivere, 🔄 deciso il 2026-10-02.
>
> La legacy `hotel_reports` (2 righe in produzione) **non ha oggi nessun corrispettivo nel nuovo
> schema**: era stata classificata come «artefatto della gestione convenzioni» e lasciata fuori. Il
> titolare ha deciso che la funzione dei **report periodici alle strutture partner** si rifà **e che
> archivia il PDF**, quindi la tabella serve: è figlia di questa, nella stessa sottoarea `agreement`.
>
> - mappatura campo per campo, enum e i **due difetti del legacy da non riprodurre**:
>   [`catalog_agreement_reports.md`](catalog_agreement_reports.md);
> - perché la decisione è cambiata (la funzione nel legacy è completa ma in produzione non ha mai
>   prodotto un PDF, e i 28 file sul bucket sono dell'ambiente di sviluppo):
>   [`docs/83-retention-file.md` §7-G1](../../../progettazione/83-retention-file.md);
> - il **PDF archiviato** non va in `catalog`: va in `notification.media_assets` con
>   `kind = 'rendered_pdf'`, dove la retention a 24 mesi lo raggiunge (policy F5).
>
> **Nessun dato da migrare**: le 2 righe di produzione hanno `path` NULL.

---

## Descrizione

Struttura partner con cui uno stabilimento ha un accordo commerciale: un cliente del beach-club può in
aggiunta consumare un listino/voucher di questa struttura. Figlia di `catalog_properties` (un partner
esiste solo come figlio di una property). Referenziata da C1 (`booking_reservations.agreement_partner_id`
/ `booking_agreement_lines`) per il consumo
transazionale.

A differenza della maggior parte del dominio C2, il modulo legacy Convenzioni è **più recente e meglio
fatto**: Service layer, DTO, FK reali a DB, **ownership verificata** via `req.userId → user.propertyId`
(non IDOR). Correzioni residue:

- **`isDeleted` → `deleted_at`** (il cascade soft-delete sui listini era già corretto).
- **`province`** stringa libera → valutare lookup province italiane (o lasciare `varchar`).
- controllo di ruolo a livello route: `isManager` era **commentato** su ogni rotta hotel → nel target il
  modulo ha una policy di autorizzazione di modulo.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_agreement_partners` | `id` | PK | — |
| `uq_catalog_agreement_partners_public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C1 `booking_reservations.agreement_partner_id`) |
| `ix_..._property` | `(property_id) WHERE deleted_at IS NULL` | parziale | "le convenzioni di questa property" |
| `ix_..._name_trgm` | `lower(name) gin_trgm_ops` | GIN trigram | ricerca per nome (`getHotelList`) |

## Campi

### id
- **Legacy**: `hotels.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `integer`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità (id legacy 1–112 preservati).
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`catalog_agreement_price_lists.partner_id`](catalog_agreement_price_lists.md).

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico, **copiato cross-modulo** — C1 `booking_reservations.agreement_partner_id` (attiva il flusso listino
  hotel quando valorizzato).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C1.

### property_id
- **Legacy**: `hotels.propertyId` (FK reale `hotels_ibfk_1`, `ON UPDATE CASCADE`)
- **Descrizione**: stabilimento che ha registrato la struttura partner.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE RESTRICT`.
- **Note**: risolto **sempre server-side** da `req.userId` (mai dal body) — pattern da mantenere.

### name
- **Legacy**: `hotels.name` (`NOT NULL`; unico campo obbligatorio in creazione)
- **Descrizione**: nome della struttura partner.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `duplicateHotel` genera nomi con suffisso "copia".

### city / postal_code / province / street_name / street_number / phone / email / note
- **Legacy**: `hotels.city` / `postalCode` / `province` / `streetName` / `streetNumber` / `phone` / `email` / `note`
- **Descrizione**: dati anagrafici/di contatto della struttura partner.
- **Tipo**: `varchar` (email `citext`, `note` `text`).
- **Default**: nessuno.
- **Nullabile**: Sì (tutti — in produzione ~96% `NULL`: gli stabilimenti registrano gli hotel solo con
  `name`).
- **Relazioni**: —.
- **Note**: dati sporchi nei pochi valori (`province` "pl", `postalCode` "00000", `phone` "666") — bonifica
  best-effort in migrazione.

### created_at / updated_at
- **Legacy**: `hotels.createdAt` / `hotels.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: nessuna riga anteriore ad aprile 2026.

### deleted_at
- **Legacy**: `hotels.isDeleted` (`tinyint(1)`; soft-delete con cascade applicativo sui listini)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: la cancellazione propaga `deleted_at` a
  [`catalog_agreement_price_lists`](catalog_agreement_price_lists.md) collegati (cascade applicativo, come
  nel legacy). `includeDeleted` per risolvere partner referenziati da prenotazioni storiche.

## Valutazione JSON

Nessun JSON su questa tabella.

## Suggerimenti per le prestazioni

- Piccola (~112 righe, fortemente concentrata: 1 property con 47 hotel). Indice trigram per `getHotelList`
  (ricerca per nome). I contatori `totalVouchers`/`usedVouchers`/`usedPriceLists` del legacy (subquery
  correlate) → materializzati nel read model del pannello Convenzioni, aggiornati a eventi da C1.

## Implementazione .NET

Aggregate root dell'area `Agreement` (bounded context "Convenzioni/Hotel Configuration"), modulo
`Module.Catalog`, schema `catalog`. Radice della gerarchia listini/voucher partner
(`catalog_agreement_price_lists` e figlie). `PublicId` è esposto perché referenziato cross-modulo da C1
(`booking_reservations.agreement_partner_id`, `booking_agreement_lines`). Nessun enum proprio.

```csharp
// Module.Catalog.Domain.Entities/AgreementPartner.cs
namespace Module.Catalog.Domain.Entities;

public sealed class AgreementPartner : AggregateRoot, IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property Property { get; private set; } = null!;
	public string Name { get; private set; } = null!;
	public string? City { get; private set; }
	public string? PostalCode { get; private set; }
	public string? Province { get; private set; }
	public string? StreetName { get; private set; }
	public string? StreetNumber { get; private set; }
	public string? Phone { get; private set; }
	public string? Email { get; private set; }
	public string? Note { get; private set; }

	private readonly List<AgreementPriceList> _priceLists = new();
	public IReadOnlyCollection<AgreementPriceList> PriceLists => _priceLists.AsReadOnly();

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void Delete(DateTime utcNow)
	{
		DeletedAt = utcNow;
		foreach (var priceList in _priceLists) priceList.Delete(utcNow); // cascade applicativo, come nel legacy
	}
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/AgreementPartnerConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AgreementPartnerConfiguration : IEntityTypeConfiguration<AgreementPartner>
{
	public void Configure(EntityTypeBuilder<AgreementPartner> builder)
	{
		builder.ToTable("catalog_agreement_partners", "catalog", t => t.HasComment(
			"Struttura partner (\"convenzione\"/hotel) con cui uno stabilimento ha un accordo commerciale."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_partners");
		builder.Property(x => x.Id).HasColumnType("integer").UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7), copiato cross-modulo da C1 (booking_reservations.agreement_partner_id).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento che ha registrato la struttura partner (risolto sempre server-side, mai dal body).");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome della struttura partner.");

		builder.Property(x => x.City).HasColumnName("city").HasMaxLength(255)
			.HasComment("Città della struttura partner.");
		builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20)
			.HasComment("CAP della struttura partner.");
		builder.Property(x => x.Province).HasColumnName("province").HasMaxLength(255)
			.HasComment("Provincia della struttura partner (testo libero, dati storicamente sporchi).");
		builder.Property(x => x.StreetName).HasColumnName("street_name").HasMaxLength(255)
			.HasComment("Via della struttura partner.");
		builder.Property(x => x.StreetNumber).HasColumnName("street_number").HasMaxLength(20)
			.HasComment("Numero civico della struttura partner.");
		builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50)
			.HasComment("Telefono di contatto della struttura partner.");
		builder.Property(x => x.Email).HasColumnName("email").HasColumnType("citext")
			.HasComment("Email di contatto della struttura partner.");
		builder.Property(x => x.Note).HasColumnName("note").HasColumnType("text")
			.HasComment("Annotazioni libere sulla struttura partner.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete; propaga a cascata (applicativa) ai listini/voucher figli.");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_agreement_partners_property")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.PriceLists)
			.WithOne(pl => pl.Partner)
			.HasForeignKey(pl => pl.PartnerId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_agreement_partners_public_id");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_partners_property");

		// Indice GIN trigram su lower(name) per ricerca per nome (getHotelList): EF Core non genera
		// espressioni funzionali nell'IndexBuilder fluente — HasMethod/HasOperators fissano motore e
		// classe operatore, l'espressione lower(name) va rifinita a mano nella migration generata.
		builder.HasIndex(x => x.Name)
			.HasMethod("gin")
			.HasOperators("gin_trgm_ops")
			.HasDatabaseName("ix_catalog_agreement_partners_name_trgm");
	}
}
```

**Note di migrazione**: `id` 1–112 preservati (`IDENTITY` riparte da `MAX(id)+1`); `isDeleted` →
`deleted_at`; bonifica best-effort dei pochi valori anagrafici sporchi (`province`, `postalCode`,
`phone`) senza bloccare la migrazione. `public_id` generato ex-novo per ogni riga (UUIDv7). La policy di
autorizzazione di modulo sostituisce l'ex controllo `isManager` (commentato nel legacy su ogni rotta
hotel) — da implementare come requirement/handler in Application, non a livello di entità.
