# Tabella `catalog_media_assets`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio) · **schema**: `catalog` · **nome flat**:
> `catalog_media_assets` · **forma canonica**: `catalog.media_assets`.
>
> Tabella di supporto **nuova** (nessuna legacy diretta). Sostituisce il pattern legacy "URL completo
> salvato in colonna `varchar`/JSON" presente su `bathouses.photos`, `bathouses.mainPhoto`,
> `bathouses.priceListPDF`, `bathouses.menuPDF`, `bathouses.linkTourVirtual`, `configure_images.image`,
> `configure_maps.templateImg`/`umbrellaImg`/`specialUmbrella*`, `services.serviceIcon`,
> `servicetypes_icons.icon`, `barServices[].Servicemage`, `legendList[].icon`.
>
> **Fonti**: [`docs/03-gestione-dati.md` §1 (Link/URL salvati a DB)](../../../progettazione/03-gestione-dati.md),
> [`analisi-pre/db/configure_images.md`](../../../analisi-pre/db/configure_images.md) (URL S3→CloudFront
> hardcoded nel controller), [`analisi-pre/db/bathouses.md`](../../../analisi-pre/db/bathouses.md).
>
> **Nota sulla voce "Legacy"**: `— (nuovo)` = campo/tabella senza corrispondente legacy diretto; dove
> indicato `<tabella>.<campo>` è il campo legacy da cui il valore viene ricavato in migrazione.

---

## Descrizione

Catalogo **locale al modulo `catalog`** di tutti i riferimenti a file (foto, PDF, icone, tour virtuali)
usati dalle entità C2. Ogni file è scomposto in `origin` (codice della sorgente: bucket/CDN/esterno) +
`path` (percorso relativo, o URL intero se `origin = external`). La mappa `origin → URL base` vive in
configurazione (`appsettings` / Parameter Store), **non** a DB: un cambio di bucket/CDN diventa una
modifica di configurazione, mai una migrazione dati
([`docs/03-gestione-dati.md` §1.2](../../../progettazione/03-gestione-dati.md)).

Perché una tabella **per modulo** e non una `media_assets` globale: una tabella unica referenziata via FK
da schemi diversi violerebbe la regola "zero FK/JOIN cross-modulo"
([`docs/03-gestione-dati.md` §1.4](../../../progettazione/03-gestione-dati.md)). Fiskaly, Notification, Switchboard
avranno la propria tabella analoga nel proprio schema.

Le entità che oggi hanno un URL in colonna diventano una **FK** verso questa tabella (nello stesso schema
`catalog`); gli array di URL (es. `bathouses.photos`) diventano tabelle di giunzione con `sort_order`
(vedi [`catalog_bathhouse_photos`](catalog_bathhouse_photos.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_media_assets` | `id` | PK | — |
| `ix_catalog_media_assets_kind` | `(kind) WHERE deleted_at IS NULL` | parziale | filtri per tipo (es. tutte le icone mappa) |
| `ix_catalog_media_assets_origin` | `(origin)` | — | riconciliazione in migrazione / audit sorgenti storiche |
| `ix_catalog_media_assets_property` 🆕 | `(property_id) WHERE property_id IS NOT NULL AND deleted_at IS NULL` | parziale | «gli asset di questa property che nessuna riga aggancia»: e' la query delle policy di retention e l'unico uso previsto della colonna |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `main_photo_id` / `menu_pdf_id` / `price_list_pdf_id` di
  [`catalog_bathhouses`](catalog_bathhouses.md), `media_asset_id` di
  [`catalog_bathhouse_photos`](catalog_bathhouse_photos.md), `icon_asset_id` di
  [`catalog_map_icons`](catalog_map_icons.md), i campi icona di [`catalog_map_layouts`](catalog_map_layouts.md),
  `icon_asset_id` di [`catalog_map_legend_items`](catalog_map_legend_items.md),
  [`catalog_service_amenities`](catalog_service_amenities.md),
  [`catalog_service_type_icons`](catalog_service_type_icons.md),
  [`catalog_service_sale_items`](catalog_service_sale_items.md).
- **Note**: **solo `bigint`** — referenziata unicamente all'interno dello schema `catalog`, nessun altro
  modulo la referenzia ([`docs/03-gestione-dati.md` §3.1ter](../../../progettazione/03-gestione-dati.md)).

### origin
- **Legacy**: ricavato in migrazione dall'host dell'URL legacy (`s3.us-east-2…`, `s3.us-east-1…`, dominio CloudFront, …)
- **Descrizione**: codice della sorgente da cui viene servito il file. La mappa `origin → URL base` vive
  in configurazione.
- **Tipo**: `enum catalog.media_origin` — valori: `s3us_east2`, `s3us_east1`, `cloudfront`, `external`. Le etichette sono quelle generate dal traduttore snake_case di Npgsql dal nome C#, che non separa le cifre (`S3UsEast2` → `s3us_east2`).
  Estendibile per deploy (nuovi bucket/CDN) aggiungendo un valore enum + la voce di configurazione.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: se `external`, `path` contiene l'URL completo invariato (link a risorsa di terzi non
  componibile con un prefisso interno). In migrazione: host non riconosciuto → `external` + URL intero in
  `path` ([`docs/03-gestione-dati.md` §1.5](../../../progettazione/03-gestione-dati.md)).

### path
- **Legacy**: porzione dell'URL legacy dopo l'host (incluse sottocartelle come `images/configure/`, `file/`, `pdf/`)
- **Descrizione**: percorso relativo del file rispetto alla base di `origin`; URL completo se `origin = external`.
- **Tipo**: `text` (gli URL legacy arrivano a >200 char; `varchar(1024)` accettabile ma `text` evita
  troncamenti su link esterni lunghi).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: nessuna barra iniziale; `BuildUrl(origin, path) = origin=='external' ? path : $"{base[origin]}/{path}"`.

### kind
- **Legacy**: dedotto dal campo legacy di provenienza (`photos`/`mainPhoto` → `photo`; `priceListPDF`/`menuPDF` → `pdf`; `*Img`/`icon`/`serviceIcon` → `icon`)
- **Descrizione**: tipo d'uso del file.
- **Tipo**: `enum catalog.media_kind` — valori: `photo`, `pdf`, `icon`. (`tour_virtual` scartato con
  `bathouses.linkTourVirtual` — [D-09](../_domande-committente.md); da riaggiungere se la feature torna.)
- **Default**: nessuno.
- **Nullabile**: No (nel legacy il tipo era implicito nel nome del campo; qui è esplicito).
- **Relazioni**: —.
- **Note**: usato per filtrare i cataloghi (es. `catalog_map_icons` referenzia solo `kind = 'icon'`); non
  vincolato via CHECK cross-tabella, validato a livello applicativo.

### checksum
- **Legacy**: — (nuovo)
- **Descrizione**: hash del contenuto (SHA-256) calcolato all'upload, per deduplica e verifica integrità.
- **Tipo**: `bytea` (32 byte) — oppure `char(64)` hex.
- **Default**: nessuno.
- **Nullabile**: Sì (i file migrati dal legacy non hanno un hash: si popola al primo ricaricamento o con
  un job di back-fill).
- **Relazioni**: —.
- **Note**: indice `UNIQUE` **parziale** `(checksum) WHERE checksum IS NOT NULL` opzionale se si vuole
  deduplica forte; di default non applicato (lo stesso file può essere caricato da property diverse).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 11.040 righe storiche: il valore si ottiene solo scaricando il file da S3, che è un lavoro di natura diversa dall'ETL. Lo fa `utility/02-migrazione-file` (decisione D9 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`), non il backend.

### byte_size
- **Legacy**: — (nuovo)
- **Descrizione**: dimensione in byte del file, nota all'upload.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: Sì (sconosciuta per i file migrati).
- **Relazioni**: —.
- **Note**: utile per validazione lato applicativo (il legacy caricava su S3 senza alcun controllo di
  dimensione — [`docs/01-criticita-e-indicazioni-backend.md` §5](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 11.040 righe storiche: il valore si ottiene solo scaricando il file da S3, che è un lavoro di natura diversa dall'ETL. Lo fa `utility/02-migrazione-file` (decisione D9 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`), non il backend.

### content_type
- **Legacy**: — (nuovo)
- **Descrizione**: MIME type (`image/jpeg`, `image/png`, `image/svg+xml`, `application/pdf`).
- **Tipo**: `varchar(100)`.
- **Default**: nessuno.
- **Nullabile**: Sì (sconosciuto per i file migrati).
- **Relazioni**: —.
- **Note**: da validare all'upload contro il contenuto reale, non solo l'estensione.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 11.040 righe storiche: il valore si ottiene solo scaricando il file da S3, che è un lavoro di natura diversa dall'ETL. Lo fa `utility/02-migrazione-file` (decisione D9 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`), non il backend.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: [`docs/03-gestione-dati.md` §3.3–§3.4](../../../progettazione/03-gestione-dati.md).

### property_id 🆕
- **Legacy**: — (nuovo; aggiunta il **2026-10-03**, migration `AddMediaAssetPropertyId`)
- **Descrizione**: property a cui il file appartiene quando **non esiste (ancora) una riga di
  aggancio**. Resta `NULL` per tutti gli asset che hanno un utilizzatore vero, dove la riga di
  aggancio rimane la fonte dell'appartenenza: le due cose sono complementari, non alternative.
- **Tipo**: `bigint`. 🔄 **Era `uuid` fino al 2026-10-05**, vedi sotto.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: FK **vera** verso `catalog.properties(id)`, `RESTRICT`, vincolo
  `fk_catalog_media_assets_property`.

> ### 🔄 Corretta il 2026-10-05 — `uuid` senza FK → `bigint` con FK
>
> Migration `20261005142959_FixMediaAssetPropertyIdType`. Nata `uuid` verso `properties.public_id`
> come riferimento logico, **era l'unica colonna fuori convenzione** fra le 129 chiamate `property_id`
> in questo database. La regola, verificata riga per riga:
>
> | Dove | Tipo | Verso |
> |---|---|---|
> | **dentro `catalog`**, che possiede `properties` | `bigint` | `properties.id`, con **vera FK** — 17 tabelle |
> | **fuori da `catalog`** | `uuid` | `properties.public_id`, riferimento **logico** |
>
> L'`uuid` è la forma dei riferimenti che **attraversano** un confine di modulo, dove una chiave
> esterna non si può dichiarare. Qui non si attraversa nessun confine, quindi si rinunciava
> all'integrità referenziale in cambio di niente. ⚠️ `notification.media_assets.property_id` resta
> `uuid`, ed è giusto così: quella **sì** attraversa i moduli.
>
> **`RESTRICT` e non `CASCADE`**: cancellare uno stabilimento non deve portarsi via le righe di asset,
> perché gli **oggetti su storage** resterebbero senza nessuna riga che li nomini — cioè diventerebbero
> orfani, che è esattamente il problema che questa colonna esiste per evitare. Chi cancella uno
> stabilimento deve prima decidere che fare dei suoi file.
>
> **Si è potuto fare senza conversione** perché la colonna è **vuota**: il recupero da
> `properties.photos` che la popolerà non è ancora stato implementato. La migration cancella e ricrea
> la colonna invece di convertirla — ⚠️ `ALTER COLUMN ... TYPE bigint` da `uuid` non è nemmeno
> possibile, PostgreSQL non ha una conversione fra i due tipi — e porta una **guardia** che la fa
> fallire con un messaggio esplicito se trova righe valorizzate.
>
> ⚠️ **Chi implementa il recupero delle 272 foto deve scrivere `properties.id`, non `public_id`.** La
> nota è anche in `utility/01-migrazione-test/mappings/catalog/catalog_media_assets.yaml`. La FK
> rifiuterà un valore che non corrisponde a nessuno stabilimento, che è il punto.
>
> ✅ Una cosa che **non** era un rischio, e che avevo detto a rovescio: scambiare i due riferimenti non
> è un errore silenzioso. PostgreSQL rifiuta `uuid = bigint` con «l'operatore non esiste», perché fra i
> due tipi non c'è conversione implicita. La differenza di tipo **protegge**.
- **Note**: serve alle **policy di retention sui file**. La decisione G3 (b) del 2026-10-02 conserva
  le **272** foto di stabilimenti che non hanno (ancora) aperto la vetrina (175 in attesa di
  approvazione + 97 approvati senza vetrina); quelle foto entrano qui **senza** una riga
  `catalog.bathhouse_photos` che le colleghi, cioè con la stessa firma di un file orfano, e senza
  questa colonna la policy F2 le cancellerebbe 48 ore dopo la migrazione e F9 il mese successivo.
  Vedi [`docs/83-retention-file.md` §5.5 e §7-G3](../../../progettazione/83-retention-file.md).

  ⚠️ **Non è un'etichetta `media_origin`**, che era la prima proposta: quell'enum descrive **l'host
  dello storage** (`s3us_east2`, `s3us_east1`, `cloudfront`, `external`), e una semantica diversa
  nello stesso enum romperebbe chi lo usa per costruire un URL.

  ✅ `utility/02-migrazione-file` ne tiene conto dal 2026-10-03: `db.verify_links()` separa
  `owned_without_users` (senza aggancio **ma con** `property_id`: si travasano) da
  `assets_without_users` (orfani veri). La corsa di prova dello stesso giorno ha fatto emergere che
  il caso esiste già su `switchboard.switchboard_media_assets`, dove un audio caricato e non ancora
  assegnato a nessuna voce di menù sarebbe stato classificato come orfano.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete. Un asset non è cancellato finché una riga lo referenzia.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: la cancellazione fisica del file sullo storage è un job separato che agisce solo su asset
  `deleted_at IS NOT NULL` e senza referenti (come faceva `deleteconfimage` in modo sincrono, ma
  disaccoppiato — [`analisi-pre/db/configure_images.md`](../../../analisi-pre/db/configure_images.md)).

## Valutazione JSON

Nessun JSON. Questa tabella **elimina** i JSON-array di URL del legacy (`bathouses.photos`) e i campi
`varchar` con URL completo, sostituendoli con FK + (dove serve) tabelle di giunzione ordinate.

## Suggerimenti per le prestazioni

- Tabella piccola (ordine delle migliaia di righe anche a regime: ~180 vetrine × decine di foto + ~90
  icone mappa + ~60 icone servizio + PDF). Nessun indice speciale oltre a quelli elencati.
- La risoluzione `origin → URL base` è in memoria (dizionario da configurazione): il `BuildUrl` non tocca
  il DB.
- Se si adotta la deduplica per `checksum`, l'`UNIQUE` parziale evita righe identiche ma richiede il
  calcolo dell'hash all'upload (costo trascurabile per file < qualche MB).
- Considerare una colonna generata `full_url` **non** persistita: l'URL si compone in applicazione, non a
  DB, per non ri-hardcodare il prefisso (l'errore del legacy).

## Implementazione .NET

`MediaAsset` è un'**entità semplice** (lookup di supporto, non aggregate root): `sealed class`, nessun
evento, referenziata solo internamente allo schema `catalog` (§3 template — solo `bigint`, nessun
`public_id`). Modulo `Module.Catalog`, schema `catalog`, tabella `media_assets`.

### Enum propri

```csharp
// Module.Catalog/Domain/Enums/MediaOrigin.cs
public enum MediaOrigin
{
	S3UsEast2 = 0,
	S3UsEast1 = 1,
	Cloudfront = 2,
	External = 3
}

// Module.Catalog/Domain/Enums/MediaKind.cs
public enum MediaKind
{
	Photo = 0,
	Pdf = 1,
	Icon = 2
}
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<MediaOrigin>("catalog", "media_origin");
modelBuilder.HasPostgresEnum<MediaKind>("catalog", "media_kind");
```

```csharp
dataSourceBuilder.MapEnum<MediaOrigin>("catalog.media_origin");
dataSourceBuilder.MapEnum<MediaKind>("catalog.media_kind");
```

### Value object

`MediaRef(Origin, Path)` compone l'URL in applicazione (mai a DB), leggendo la mappa `origin → URL base`
da `StorageOptions.Origins: Dictionary<MediaOrigin,string>` (`appsettings` + Parameter Store):

```csharp
// Module.Catalog/Domain/ValueObjects/MediaRef.cs
public sealed record MediaRef(MediaOrigin Origin, string Path)
{
	public string ToUrl(IReadOnlyDictionary<MediaOrigin, string> originBaseUrls) =>
		Origin == MediaOrigin.External ? Path : $"{originBaseUrls[Origin]}/{Path}";
}
```

### `MediaAssetConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
	public void Configure(EntityTypeBuilder<MediaAsset> builder)
	{
		builder.ToTable("media_assets", "catalog", t => t.HasComment(
			"Catalogo locale al modulo catalog di tutti i riferimenti a file (foto, PDF, icone) usati dalle entità C2."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_media_assets");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.OwnsOne(x => x.Ref, r =>
		{
			r.Property(p => p.Origin)
				.HasColumnName("origin")
				.HasColumnType("catalog.media_origin")
				.IsRequired()
				.HasComment("Sorgente da cui viene servito il file; la mappa origin → URL base vive in configurazione, non a DB.");
			r.Property(p => p.Path)
				.HasColumnName("path")
				.HasColumnType("text")
				.IsRequired()
				.HasComment("Percorso relativo rispetto alla base di origin; URL completo se origin = external.");

			r.HasIndex(p => p.Origin).HasDatabaseName("ix_catalog_media_assets_origin");
		});

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("catalog.media_kind")
			.IsRequired()
			.HasComment("Tipo d'uso del file (photo, pdf, icon).");

		builder.Property(x => x.Checksum)
			.HasColumnName("checksum")
			.HasColumnType("bytea")
			.HasComment("Hash SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità. NULL per i file migrati dal legacy.");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Dimensione in byte del file, nota all'upload.");

		builder.Property(x => x.ContentType)
			.HasColumnName("content_type")
			.HasMaxLength(100)
			.HasComment("MIME type (image/jpeg, image/png, image/svg+xml, application/pdf).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: l'asset non è cancellato finché una riga lo referenzia.");

		builder.HasIndex(x => x.Kind)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_media_assets_kind");

		// UNIQUE parziale opzionale per deduplica forte, non applicata di default (vedi Note):
		// builder.HasIndex(x => x.Checksum).IsUnique().HasFilter("checksum IS NOT NULL")
		//     .HasDatabaseName("uq_catalog_media_assets_checksum");
	}
}
```

`MediaAsset` implementa `IAuditable`/`ISoftDeletable`.

### Relazioni

Nessuna FK in uscita da `MediaAsset`; è referenziata da `main_photo_id`/`menu_pdf_id`/`price_list_pdf_id`
di `Bathhouse`, `media_asset_id` di `BathhousePhoto`, `media_asset_id`/`icon_asset_id` delle tabelle
`map_*`, `catalog_service_amenities`, `catalog_service_type_icons`, `catalog_service_sale_items` — ognuna
dichiara la propria `HasOne(x => x.MediaAsset)` nella propria `Configuration` (§3 del template).

### Note di migrazione

- Per ogni URL legacy: origin dedotto dall'host (`s3.us-east-2…` → `S3UsEast2`, `s3.us-east-1…` →
  `S3UsEast1`, dominio CloudFront noto → `Cloudfront`, host non riconosciuto → `External` con URL intero
  in `Path`); `kind` dedotto dal campo di provenienza (`photos`/`mainPhoto` → `Photo`,
  `priceListPDF`/`menuPDF` → `Pdf`, `*Img`/`icon`/`serviceIcon` → `Icon`).
- Una riga `MediaAsset` per URL distinto; le FK delle entità consumer si popolano risolvendo l'URL legacy
  verso l'asset creato (mappa `url → id` tenuta in memoria durante la migrazione, per evitare duplicati).
- `checksum`/`byte_size`/`content_type` restano `NULL` per i file migrati; si popolano al primo
  ricaricamento o con un job di back-fill.
- Job di garbage collection separato: elimina dallo storage i file con `deleted_at IS NOT NULL` e zero
  referenti (sostituisce `deleteconfimage`, sincrono nel legacy).
