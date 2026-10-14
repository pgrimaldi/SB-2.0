# Tabella `catalog_bathhouses`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio) · **schema**: `catalog` · **nome flat**:
> `catalog_bathhouses` · **forma canonica**: `catalog.bathhouses`.
>
> Rimpiazza la legacy [`bathouses`](../../../analisi-pre/db/bathouses.md) (**10.432 righe** in produzione, di
> cui **~10.250 segnaposto vuoti**; ~180 vetrine realmente compilate). Fonti:
> [`analisi-pre/db/bathouses.md`](../../../analisi-pre/db/bathouses.md),
> [`analisi-pre/db/country.md`](../../../analisi-pre/db/country.md),
> [`analisi-pre/business-logic/stabilimento/11-configurazione.md`](../../../analisi-pre/business-logic/stabilimento/11-configurazione.md),
> [`analisi-pre/business-logic/cliente-finale/03-scheda-stabilimento.md`](../../../analisi-pre/business-logic/cliente-finale/03-scheda-stabilimento.md),
> [`docs/03-gestione-dati.md` §1, §6](../../../progettazione/03-gestione-dati.md).
>
> **Nota su `bathhouse` vs `property`** — decisione committente: **una `property` può raggruppare più
> `bathhouse`** (relazione **N:1**). I dati consumer-facing granulari (foto, "mi piace", geo, orari)
> stanno sulla singola vetrina; staff/abbonamento/rubrica/2FA restano per `property_id`.

---

## Descrizione

Vetrina **pubblica** dello stabilimento: nome commerciale, indirizzo, contatti, descrizione, foto,
geolocalizzazione, orari, politiche (cancellazione, animali), slug d'URL. Consumata dal sito pubblico di
ricerca/prenotazione e dall'app cliente.

Correzioni rispetto al legacy:

- **N:1 con `catalog_properties`**: `property_id` FK reale.
- **`public_id uuid`**: referenziato **cross-modulo** — C5
  [`identity_customer_bathhouse_likes.bathhouse_id`](../identity/identity_customer_bathhouse_likes.md) copia questo
  `public_id`.
- **`slug`**: **un solo** indice `UNIQUE` (il legacy ne aveva **60 ridondanti** per esecuzioni ripetute
  di `sync({alter:true})`); generato **una volta** alla prima pubblicazione (niente rigenerazione ad ogni
  `save()` con timestamp, che nel legacy poteva cambiare l'URL pubblico e rompere SEO/link).
- **`likeCount` → non colonna**: contatore denormalizzato **per `bathhouse`**, ricalcolato da
  `COUNT(*)` su C5 `identity_customer_bathhouse_likes` via proiezione/evento
  ([`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)) — non mantenuto qui con `± 1` non
  transazionale (nel legacy produceva anche valori negativi). Se serve materializzarlo per la ricerca,
  vive nel **read model** di ricerca, non su questa tabella.
- **`isTwoFactorEnabled`** → **C5** (`identity_access_property_security_settings`, per `property`).
- **`isTwoFactorAuthentication`** (doppione morto, sempre `0`) → **non migrato**.
- **`status`** (vestigiale, sempre `0`, mai letto per decisioni; la visibilità dipende da
  `properties.status`) → sostituito da `published_at` (la vetrina è "pubblica" solo quando compilata e
  pubblicata; le ~10.250 righe stub **non** si migrano come vetrine attive).
- **`services`** (copia integrale JSON del master) → tabella di giunzione
  [`catalog_bathhouse_amenities`](catalog_bathhouse_amenities.md).
- **`photos`** (array URL JSON) → [`catalog_bathhouse_photos`](catalog_bathhouse_photos.md) (giunzione
  ordinata verso [`catalog_media_assets`](catalog_media_assets.md)).
- **`mainPhoto` / `priceListPDF` / `menuPDF`** (URL `varchar`) → FK verso
  [`catalog_media_assets`](catalog_media_assets.md).
- **`linkTourVirtual`** (URL `varchar`, `NULL` su **tutte** le 10.432 righe di produzione) → **non
  migrato** (decisione committente [D-09](../_domande-committente.md)).
- **`latitude` / `longitude`** `varchar` → `numeric(9,6)` (o tipo `point`).
- **`country`** stringa libera → FK verso [`catalog_countries`](catalog_countries.md).
- **`pets`** stringa libera → `enum`; **`cancellation`** stringa libera → **campo strutturato**
  `is_free_cancellation` + `cancellation_notice_hours` (decisione committente [D-10](../_domande-committente.md), provvisoria).
- **`userId`** → verificato nel codice ([D-08](../_domande-committente.md)): è l'account che ha salvato per
  ultimo la vetrina → migrato come **`updated_by_user_id`** (uuid da C5), non come "titolare".
- **`whatsApp`**: nel legacy **mai salvato alla creazione** per il typo `whatsAPP` → nel target è un
  campo normale.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_bathhouses` | `id` | PK | — |
| `uq_catalog_bathhouses_public_id` | `(public_id)` | UNIQUE | riferimento cross-modulo (C5 likes) |
| `uq_catalog_bathhouses_slug` | `(slug) WHERE slug IS NOT NULL` | UNIQUE parziale | **1 solo** indice (legacy: 60) |
| `ix_catalog_bathhouses_property` | `(property_id)` | — | "le vetrine di questa property" |
| `ix_catalog_bathhouses_published` | `(published_at) WHERE published_at IS NOT NULL AND deleted_at IS NULL` | parziale | solo le vetrine reali (~180 su ~10.000) |
| `ix_catalog_bathhouses_geo` | `(latitude, longitude)` o GiST su `point` | — | ricerca "vicino a me" |
| `ix_catalog_bathhouses_region` | `(region)`, `(city)` | — | ricerca pubblica per area |

## Campi

### id
- **Legacy**: `bathouses.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità (id legacy preservati 1:1).
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`catalog_bathhouse_photos`](catalog_bathhouse_photos.md),
  [`catalog_bathhouse_amenities`](catalog_bathhouse_amenities.md).

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile, **copiato cross-modulo**.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C5 `identity_customer_bathhouse_likes.bathhouse_id`; usato anche in URL/API
  pubbliche dove non si usa lo `slug`.
- **Note**: in migrazione, `bathouses.id → bathhouse.public_id` è la mappa che C5 usa per convertire
  `property_like_logs.bathHouseId`.

### property_id
- **Legacy**: `bathouses.propertyId` (FK reale `bathouses_ibfk_1`)
- **Descrizione**: property (tenant) proprietaria della vetrina.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No (il legacy ammetteva `NULL` su 1 riga; qui obbligatorio).
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE RESTRICT`.
- **Note**: N:1 — una property può avere più vetrine.

### name
- **Legacy**: `bathouses.bathouseName`
- **Descrizione**: nome commerciale pubblico; sorgente dello `slug`.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì a livello di DB: ammesso `NULL` finché la vetrina è bozza. L'obbligo alla
  pubblicazione è una regola applicativa, non un vincolo (sui 10.431 record del DB di sviluppo nessuno è `NULL`).
- **Relazioni**: —.

### slug
- **Legacy**: `bathouses.slug` (60 vincoli `UNIQUE` ridondanti; rigenerato ad ogni `save()` con timestamp)
- **Descrizione**: identificatore leggibile nell'URL pubblico.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì (assente finché la vetrina non è pubblicata).
- **Relazioni**: —.
- **Note**: generato **una volta** dalla `name` (con disambiguazione numerica in caso di collisione).
  Un rename successivo **non** cambia lo slug salvo azione esplicita, con redirect 301 dal vecchio.

### description / regulation
- **Legacy**: `bathouses.description` / `bathouses.regulation`
- **Descrizione**: descrizione libera e regolamento interno (HTML da editor ricco).
- **Tipo**: `text` ciascuno.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: da sanificare l'HTML in ingresso (il legacy non lo faceva).

### address / city / cap / province / region
- **Legacy**: `bathouses.address` / `city` / `cap` / `province` / `region`
- **Descrizione**: indirizzo pubblico completo. `region`/`province` usati dalla ricerca per area.
- **Tipo**: `varchar(255)` ciascuno (`cap` `varchar(16)`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: testo libero anche nel target (troppi valori distinti / dati sporchi per enumerare);
  in migrazione i segnaposto `"-----"` → `NULL`.

### country_id
- **Legacy**: `bathouses.country` (unico punto legacy che consuma davvero la lookup `country`)
- **Descrizione**: paese della vetrina.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_countries.id`](catalog_countries.md) `ON DELETE RESTRICT`.
- **Note**: in migrazione `"Italia"`/`"Italy"` → stessa riga; altre varianti sporche → `NULL`.

### latitude / longitude
- **Legacy**: `bathouses.latitude` / `bathouses.longitude` (`varchar`, non validati)
- **Descrizione**: coordinate GPS per mappa e ricerca geografica.
- **Tipo**: `numeric(9,6)` ciascuno (o una singola colonna `location point` / `geography(Point)` con
  PostGIS se disponibile).
- **Default**: nessuno.
- **Nullabile**: Sì (31% `NULL` in produzione).
- **Relazioni**: —.
- **Note**: in migrazione: parsing con normalizzazione del separatore decimale; scarto dei valori fuori
  range `[-90,90]` / `[-180,180]`.

### phone / email / website / facebook / instagram / whatsapp
- **Legacy**: `bathouses.mobile` / `email` / `website` / `facebook` / `instagram` / `whatsApp`
- **Descrizione**: contatti e link social pubblici.
- **Tipo**: `varchar(255)` ciascuno (`phone`/`whatsapp` `varchar(32)`, `email` `citext`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: `whatsapp` nel legacy era `NULL` sul 99% delle righe per il typo `whatsAPP` in creazione —
  nel target è raccolto normalmente. I frammenti social (`"lidoulisse_"`, `"facebook.com/..."`) vanno
  normalizzati a URL completo.

### open_hours_from / open_hours_to
- **Legacy**: `bathouses.openHoursFrom` / `bathouses.openHoursTo`
- **Descrizione**: orario di apertura giornaliero.
- **Tipo**: `time` ciascuno (il legacy era `varchar` ma sui ~180 record reali il formato è `HH:MM`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: se in futuro servono orari per giorno della settimana, estrarre in tabella figlia; oggi è un
  singolo intervallo.

### is_free_cancellation / cancellation_notice_hours
- **Legacy**: `bathouses.cancellation` (stringa libera: `Not Allowed`, `Free within {3,12,24,48} hours` + varianti di capitalizzazione)
- **Descrizione**: politica di cancellazione mostrata al cliente, modellata come **campo strutturato**
  (decisione committente [D-10](../_domande-committente.md)): `is_free_cancellation` = la cancellazione è
  gratuita entro un preavviso; `cancellation_notice_hours` = ore di preavviso richieste (rilevante solo se
  `is_free_cancellation = true`).
- **Tipo**: `is_free_cancellation boolean` · `cancellation_notice_hours smallint`.
- **Default**: `is_free_cancellation = false` · `cancellation_notice_hours = NULL`.
- **Nullabile**: `is_free_cancellation` No · `cancellation_notice_hours` Sì.
- **Relazioni**: —.
- **Migrazione**: `Not Allowed` → `(false, NULL)`; `Free within N hours` (qualunque capitalizzazione) →
  `(true, N)`; vuoto/`NULL` → `(false, NULL)`.
- **Note**: scelta **provvisoria** — potrà evolvere in futuro (enum, lookup gestibile, o regole più
  ricche) se emergono requisiti di prodotto ([D-10](../_domande-committente.md)). `CHECK (is_free_cancellation
  OR cancellation_notice_hours IS NULL)`.

### pets_policy
- **Legacy**: `bathouses.pets` (stringa libera: `Allowed` / `Not Allowed` / vuoto)
- **Descrizione**: politica animali.
- **Tipo**: `enum catalog.pets_policy` — valori: `allowed`, `not_allowed`, `on_request`.
- **Default**: `not_allowed`.
- **Nullabile**: No (il default copre la bozza: una vetrina senza indicazione vale `not_allowed`).
- **Relazioni**: —.
- **Note**: di fatto un booleano testuale nel legacy; `on_request` aggiunto per copertura futura.

### main_photo_id
- **Legacy**: `bathouses.mainPhoto` (URL `varchar`)
- **Descrizione**: foto di copertina.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'photo'`).

### menu_pdf_id / price_list_pdf_id
- **Legacy**: `bathouses.menuPDF` / `bathouses.priceListPDF` (URL `varchar`, ~6 righe valorizzate ciascuno)
- **Descrizione**: PDF di menu bar / listino cartaceo.
- **Tipo**: `bigint` ciascuno.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'pdf'`).

### published_at
- **Legacy**: — (deriva da `bathouses.status` vestigiale + presenza di dati compilati)
- **Descrizione**: quando la vetrina è stata resa pubblica per la prima volta; `NULL` ⇒ bozza / non
  pubblicata.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: sostituisce il pattern legacy "riga creata insieme alla property e mai compilata". In
  migrazione: righe stub (tutti i campi descrittivi `NULL`) → **non migrate** o migrate con
  `published_at = NULL`.

### updated_by_user_id
- **Legacy**: `bathouses.userId` (verificato: scritto da `req.body.userId` in create/update; il frontend invia `SuperAdminID` = id dell'account loggato che salva la vetrina — **non** il "titolare". Vedi [D-08](../_domande-committente.md))
- **Descrizione**: account che ha creato / salvato per ultimo la vetrina.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì (98% `NULL` in produzione — valorizzato solo sulle ~180 vetrine reali).
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users` — nessuna FK cross-schema.
- **Note**: sovrascritto ad ogni salvataggio col salvatore corrente (è un `updated_by`, non un
  `created_by` storico); nei dati coincide quasi sempre col `superadmin` della property (unico a
  raggiungere quel pannello) ma è concettualmente distinto da `catalog_properties.owner_user_id`.

### created_at / updated_at
- **Legacy**: `bathouses.createdAt` / `bathouses.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (il legacy non aveva soft-delete sulla vetrina)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: Global Query Filter EF.

## Campi legacy **non** migrati

| Campo legacy | Motivo |
|---|---|
| `bathouses.status` | vestigiale, sempre `0`, mai usato per decisioni → sostituito da `published_at` |
| `bathouses.likeCount` | contatore denormalizzato → ricalcolato da C5; eventualmente nel read model di ricerca, non qui |
| `bathouses.isTwoFactorEnabled` | → **C5** (`identity_access_property_security_settings`) |
| `bathouses.isTwoFactorAuthentication` | doppione morto, sempre `0` |
| `bathouses.linkTourVirtual` | `NULL` su tutte le 10.432 righe di produzione — feature mai usata (decisione committente [D-09](../_domande-committente.md)) |
| `bathouses.services` (JSON) | → [`catalog_bathhouse_amenities`](catalog_bathhouse_amenities.md) |
| `bathouses.photos` (JSON) | → [`catalog_bathhouse_photos`](catalog_bathhouse_photos.md) |

## Valutazione JSON

Nessun JSON. `photos` → giunzione ordinata; `services` → giunzione N:N. Coordinate e politiche → colonne
tipizzate/enum.

## Suggerimenti per le prestazioni

- La ricerca pubblica **non** interroga questa tabella con `findAll` + `LIKE` + N query per il prezzo
  minimo (anti-pattern legacy `serchProperty`): si costruisce un **read model di ricerca**
  denormalizzato (nome, città, regione, geo, prezzo minimo corrente, servizi come array/tsvector,
  `likeCount`), aggiornato a eventi da C2. Le query di ricerca colpiscono solo quello.
- Indice geografico (GiST su `point`/`geography`) per il "vicino a me"; indici B-tree su `region`/`city`
  per i filtri testuali di area.
- `slug` con `UNIQUE` **parziale** (`WHERE slug IS NOT NULL`): le bozze senza slug non occupano l'indice.
- Migrazione: escludere le ~10.250 righe stub → la tabella nasce con ~180 righe reali.

## Implementazione .NET

`Bathhouse` è un **aggregate root** proprio (`sealed class Bathhouse : AggregateRoot`): confine di
consistenza separato da `Property` pur essendo N:1 con essa, perché ha ciclo di vita (bozza/pubblicata),
`public_id` esposto e referenziato cross-modulo (C5 `identity_customer_bathhouse_likes.bathhouse_id`) e
solleva eventi propri (`BathhousePublished`, …). Modulo `Module.Catalog`, schema `catalog`, tabella
`bathhouses`.

### Enum proprio

```csharp
// Module.Catalog/Domain/Enums/PetsPolicy.cs
public enum PetsPolicy
{
	Allowed = 0,
	NotAllowed = 1,
	OnRequest = 2
}
```

```csharp
modelBuilder.HasPostgresEnum<PetsPolicy>("catalog", "pets_policy");
dataSourceBuilder.MapEnum<PetsPolicy>("catalog.pets_policy");
```

### Value object

`IsFreeCancellation` + `CancellationNoticeHours` sono un cluster di valore con invariante propria
(`CancellationNoticeHours` ha senso solo se `IsFreeCancellation = true`) → owned type:

```csharp
// Module.Catalog/Domain/ValueObjects/CancellationPolicy.cs
public sealed record CancellationPolicy(bool IsFreeCancellation, short? NoticeHours)
{
	public static CancellationPolicy NotAllowed() => new(false, null);
	public static CancellationPolicy FreeWithNotice(short hours) => new(true, hours);
}
```

### `BathhouseConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class BathhouseConfiguration : IEntityTypeConfiguration<Bathhouse>
{
	public void Configure(EntityTypeBuilder<Bathhouse> builder)
	{
		builder.ToTable("bathhouses", "catalog", t => t.HasComment(
			"Vetrina pubblica dello stabilimento: nome commerciale, indirizzo, contatti, descrizione, foto, geolocalizzazione, orari, politiche."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_bathhouses");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo (C5 identity_customer_bathhouse_likes.bathhouse_id) e usato in URL/API pubbliche.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Property (tenant) proprietaria della vetrina.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.HasComment("Nome commerciale pubblico; sorgente dello slug.");

		builder.Property(x => x.Slug)
			.HasColumnName("slug")
			.HasMaxLength(255)
			.HasComment("Identificatore leggibile nell'URL pubblico, generato una sola volta alla prima pubblicazione.");

		builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").HasComment("Descrizione libera (HTML sanificato).");
		builder.Property(x => x.Regulation).HasColumnName("regulation").HasColumnType("text").HasComment("Regolamento interno (HTML sanificato).");

		builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(255).HasComment("Indirizzo pubblico.");
		builder.Property(x => x.City).HasColumnName("city").HasMaxLength(255).HasComment("Città della vetrina.");
		builder.Property(x => x.Cap).HasColumnName("cap").HasMaxLength(16).HasComment("CAP.");
		builder.Property(x => x.Province).HasColumnName("province").HasMaxLength(255).HasComment("Provincia.");
		builder.Property(x => x.Region).HasColumnName("region").HasMaxLength(255).HasComment("Regione, usata dalla ricerca pubblica per area.");

		builder.Property(x => x.CountryId).HasColumnName("country_id").HasComment("Paese della vetrina.");

		builder.Property(x => x.Latitude).HasColumnName("latitude").HasColumnType("numeric(9,6)").HasComment("Latitudine GPS per mappa e ricerca geografica.");
		builder.Property(x => x.Longitude).HasColumnName("longitude").HasColumnType("numeric(9,6)").HasComment("Longitudine GPS per mappa e ricerca geografica.");

		builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(32).HasComment("Telefono pubblico.");
		builder.Property(x => x.Email).HasColumnName("email").HasColumnType("citext").HasComment("Email pubblica.");
		builder.Property(x => x.Website).HasColumnName("website").HasMaxLength(255).HasComment("Sito web.");
		builder.Property(x => x.Facebook).HasColumnName("facebook").HasMaxLength(255).HasComment("Link Facebook.");
		builder.Property(x => x.Instagram).HasColumnName("instagram").HasMaxLength(255).HasComment("Link Instagram.");
		builder.Property(x => x.Whatsapp).HasColumnName("whatsapp").HasMaxLength(32).HasComment("Numero WhatsApp pubblico.");

		builder.Property(x => x.OpenHoursFrom).HasColumnName("open_hours_from").HasColumnType("time").HasComment("Orario di apertura giornaliero.");
		builder.Property(x => x.OpenHoursTo).HasColumnName("open_hours_to").HasColumnType("time").HasComment("Orario di chiusura giornaliero.");

		builder.OwnsOne(x => x.CancellationPolicy, cp =>
		{
			cp.Property(p => p.IsFreeCancellation)
				.HasColumnName("is_free_cancellation")
				.IsRequired()
				.HasDefaultValue(false)
				.HasComment("Cancellazione gratuita entro un preavviso.");
			cp.Property(p => p.NoticeHours)
				.HasColumnName("cancellation_notice_hours")
				.HasComment("Ore di preavviso richieste, valorizzato solo se is_free_cancellation = true.");
		});
		builder.ToTable(t => t.HasCheckConstraint(
			"ck_catalog_bathhouses_cancellation_notice",
			"is_free_cancellation OR cancellation_notice_hours IS NULL"));

		builder.Property(x => x.PetsPolicy)
			.HasColumnName("pets_policy")
			.HasColumnType("catalog.pets_policy")
			.HasDefaultValueSql("'not_allowed'")
			.HasComment("Politica animali della vetrina.");

		builder.Property(x => x.MainPhotoId).HasColumnName("main_photo_id").HasComment("Foto di copertina (kind = photo).");
		builder.Property(x => x.MenuPdfId).HasColumnName("menu_pdf_id").HasComment("PDF menu bar (kind = pdf).");
		builder.Property(x => x.PriceListPdfId).HasColumnName("price_list_pdf_id").HasComment("PDF listino cartaceo (kind = pdf).");

		builder.Property(x => x.PublishedAt)
			.HasColumnName("published_at")
			.HasComment("Quando la vetrina è stata resa pubblica per la prima volta; NULL = bozza/non pubblicata.");

		builder.Property(x => x.UpdatedByUserPublicId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Account che ha salvato per ultimo la vetrina.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete della vetrina.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_bathhouses_public_id");
		builder.HasIndex(x => x.Slug).IsUnique().HasFilter("slug IS NOT NULL").HasDatabaseName("uq_catalog_bathhouses_slug");
		builder.HasIndex(x => x.PropertyId).HasDatabaseName("ix_catalog_bathhouses_property");
		builder.HasIndex(x => x.PublishedAt)
			.HasFilter("published_at IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_bathhouses_published");
		builder.HasIndex(x => new { x.Latitude, x.Longitude }).HasDatabaseName("ix_catalog_bathhouses_geo");
		builder.HasIndex(x => new { x.Region, x.City }).HasDatabaseName("ix_catalog_bathhouses_region");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_bathhouses_property")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.Country)
			.WithMany()
			.HasForeignKey(x => x.CountryId)
			.HasConstraintName("fk_catalog_bathhouses_country")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.MainPhoto).WithMany().HasForeignKey(x => x.MainPhotoId).HasConstraintName("fk_catalog_bathhouses_main_photo");
		builder.HasOne(x => x.MenuPdf).WithMany().HasForeignKey(x => x.MenuPdfId).HasConstraintName("fk_catalog_bathhouses_menu_pdf");
		builder.HasOne(x => x.PriceListPdf).WithMany().HasForeignKey(x => x.PriceListPdfId).HasConstraintName("fk_catalog_bathhouses_price_list_pdf");
	}
}
```

`Bathhouse` implementa `IAuditable`/`ISoftDeletable`. `PetsPolicy` è `NOT NULL` a livello DB con default
`not_allowed`: la proprietà C# non è nullable. `Name` invece è nullable (vetrina in bozza).

### Relazioni

- `PropertyId` → `Property` (stesso schema): FK reale, `ON DELETE RESTRICT` (cancellare una property
  con vetrine attive va gestito esplicitamente, non a cascata).
- `CountryId` → `Country`: FK reale, `ON DELETE RESTRICT`.
- `MainPhotoId`/`MenuPdfId`/`PriceListPdfId` → `MediaAsset` (stesso schema): FK reali, nessun `OnDelete`
  esplicito (default `ClientSetNull`/`Restrict` secondo policy applicativa — un asset referenziato non si
  cancella).
- `UpdatedByUserPublicId`: id copiato da C5 `identity.access_users` — solo `.HasComment`, nessuna FK.

### Note di migrazione

- `id` 1:1; la mappa `bathouses.id → public_id` generata in migrazione è condivisa con C5 per convertire
  `property_like_logs.bathHouseId`.
- Escludere le ~10.250 righe stub (tutti i campi descrittivi `NULL`): la tabella nasce con ~180 righe.
- `country` stringa libera → FK; `latitude`/`longitude` `varchar` → `numeric(9,6)` con parsing e scarto
  fuori range; segnaposto indirizzo (`"-----"`) → `NULL`.
- `cancellation` stringa libera → `CancellationPolicy` (`Not Allowed` → `(false, null)`, `Free within N
  hours` case-insensitive → `(true, N)`).
- `pets` stringa libera → `PetsPolicy` (`Allowed`/`Not Allowed`/vuoto → `OnRequest` da valutare caso per
  caso, default `NotAllowed`).
- `services`/`photos` (JSON) esplosi rispettivamente in `catalog_bathhouse_amenities` e
  `catalog_bathhouse_photos`; `mainPhoto`/`menuPDF`/`priceListPDF` (URL) risolti/creati come `MediaAsset`.
- `status` vestigiale e `likeCount` denormalizzato: non migrati (vedi tabella "Campi legacy non
  migrati").
