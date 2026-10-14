# Tabella `notification_media_assets`

> **Dominio**: C6 — `notification` · **sottoarea**: — · **schema**: `notification` · **nome flat**:
> `notification_media_assets` · **forma canonica**: `notification.media_assets`.
>
> Tabella di supporto **nuova**. Sostituisce il pattern legacy "URL S3 completo in colonna" /
> "array di URL S3 in JSON" su: [`custom_templates.attachment`](../../../analisi-pre/db/custom_templates.md)
> (array URL, 8 righe reali), [`marketing_email_campaign.attachments`](../../../analisi-pre/db/marketing_email_campaign.md)
> / [`marketing_automation.attachments`](../../../analisi-pre/db/marketing_automation.md)
> (`[{filename,path}]`), [`marketing_event.coverImage`](../../../analisi-pre/db/marketing_event.md)
> (URL singolo), [`hotel_reports.path`](../../../analisi-pre/db/hotel_reports.md) (URL PDF del report,
> valorizzato **all'invio** → artefatto di consegna C6, [`docs/03 §6`](../../../progettazione/03-gestione-dati.md)),
> e gli allegati passati a `email_logs.attachments`. Analoga a
> [`catalog_media_assets`](../catalog/catalog_media_assets.md) /
> [`fiskaly_media_assets`](../fiskaly/fiskaly_media_assets.md) — **una per modulo**
> ([`docs/03-gestione-dati.md` §1.2–§1.4, §3.1quater.A](../../../progettazione/03-gestione-dati.md)).

---

## Descrizione

Catalogo **locale al modulo `notification`** dei riferimenti a file usati nelle comunicazioni:
allegati dei template, allegati di campagne/automazioni, immagini di copertina degli eventi marketing,
PDF renderizzati e spediti (report convenzione, e-receipt allegati a un messaggio), file caricati dai
lead in ingresso. Ogni file è scomposto in `origin` (codice sorgente: bucket/CDN) + `path` (percorso
relativo); la mappa `origin → URL base` vive in **configurazione**, mai a DB.

Perché una tabella **per modulo**: una `media_assets` globale referenziata da schemi diversi
violerebbe "zero FK/JOIN cross-modulo" ([`docs/03 §1.4`](../../../progettazione/03-gestione-dati.md)).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `kind` | **enum esclusivo**: un file ha un solo tipo d'uso. |
| `origin` | **enum esclusivo**. |

## Snapshot

Non applicabile (`filename` è il nome originale del file, non lo snapshot di un'entità).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_media_assets` | `id` | PK | — |
| `ix_..._property_kind` | `(property_id, kind) WHERE deleted_at IS NULL` | btree parziale | file di uno stabilimento per tipo |
| `ix_..._checksum` | `(checksum) WHERE checksum IS NOT NULL` | btree | deduplica in caricamento |
| `ix_..._origin` | `(origin)` | btree | riconciliazione sorgenti in migrazione |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da
  [`notification_template_attachments.media_asset_id`](notification_template_attachments.md),
  [`notification_marketing_events.cover_image_asset_id`](notification_marketing_events.md),
  [`notification_establishment_claim_requests`](notification_establishment_claim_requests.md) (eventuali file).

### property_id
- **Legacy**: `custom_templates.property_id` / `marketing_*.propertyId`
- **Descrizione**: stabilimento proprietario del file.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: Sì (file di piattaforma: PDF di report inviati da flussi non legati a una property,
  upload da lead pubblici).
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### origin
- **Legacy**: host dell'URL legacy (`summerbooking.s3.us-east-2.amazonaws.com`)
- **Descrizione**: codice della sorgente da cui è servito il file; la mappa `origin → URL base` è in
  configurazione.
- **Tipo**: `enum notification.media_origin` — valori: `s3_us_east_2`, `cloudfront`, `external`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: se `external`, `path` è l'URL completo invariato.

### path
- **Legacy**: porzione dell'URL dopo l'host (con l'artefatto `files%5B%5D/` da bonificare)
- **Descrizione**: percorso relativo del file rispetto alla base di `origin`; nessuna barra iniziale.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `BuildUrl(origin, path)` in applicazione.

### kind
- **Legacy**: dedotto dalla colonna/JSON di provenienza
- **Descrizione**: tipo d'uso del file.
- **Tipo**: `enum notification.media_kind` — valori: `template_attachment`, `campaign_attachment`,
  `event_cover_image`, `rendered_pdf` (report/e-receipt allegati a un invio), `inbound_upload` (file
  di un lead), `other`.
- **Default**: `other`.
- **Nullabile**: No.
- **Relazioni**: —.

### filename
- **Legacy**: `marketing_*.attachments[].filename` / basename dell'URL
- **Descrizione**: nome file originale, mostrato all'utente e usato come `filename` MIME in allegato.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### checksum / byte_size / content_type
- **Legacy**: — (nuovo)
- **Descrizione**: SHA-256 del contenuto, dimensione in byte, MIME reale (validato con magic bytes in
  upload, [`docs/01 §10`](../../../progettazione/01-criticita-e-indicazioni-backend.md)). `NULL` per i file
  migrati finché non riconciliati.
- **Tipo**: `bytea` (32 byte) · `integer` · `varchar(100)`.
- **Uniformato il 2026-10-01** (M19 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`): era `char(64)`. Vedi la nota in
  `fiskaly_media_assets.md` per il perché.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì / Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 30 righe storiche: il valore si ottiene solo scaricando il file da S3, che è un lavoro di natura diversa dall'ETL. Lo fa `utility/02-migrazione-file` (decisione D9 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`), non il backend.

### uploaded_by_user_id
- **Legacy**: `custom_templates.createdBy` (sempre `NULL` in prod)
- **Descrizione**: utente che ha caricato il file (per gli upload da gestionale).
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 30 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / modifica / soft-delete. Un asset non si cancella finché è referenziato;
  la GC fisica sullo storage è un job separato che agisce solo su asset `deleted_at IS NOT NULL` e
  senza referenti.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. Gli array `attachments` (`[{filename,path}]`) e `attachment` (array di URL) diventano
righe qui + giunzione ordinata ([`notification_template_attachments`](notification_template_attachments.md));
i singoli URL (`coverImage`, `hotel_reports.path`) diventano una FK interna `*_asset_id`.

## Suggerimenti per le prestazioni

- Volume basso (allegati rari: 8 righe reali su 103 template in prod). Nessun indice speciale oltre a
  quelli elencati.
- `origin → URL base` risolto in memoria da configurazione: `BuildUrl` non tocca il DB.

## Implementazione .NET

`MediaAsset` è un'**entità principale** (catalogo di supporto, locale al modulo) di `Module.Notification`
(schema `notification`): non è owned da nessun'altra entità, è referenziata da FK interne di
`notification_template_attachments.media_asset_id` e dalle schede marketing fuori da questo batch
(`cover_image_asset_id`, eventuali file di `notification_establishment_claim_requests`). `(origin, path)`
è concettualmente un value object `MediaRef`, ma mappato come due colonne scalari sull'entità (stesso
pattern di `catalog_media_assets`/`fiskaly_media_assets`) perché è sempre letto/scritto insieme al resto
della riga, senza comportamento proprio da isolare in un owned type.

### Enum propri

```csharp
// Domain/Enums/NotificationMediaOrigin.cs
public enum NotificationMediaOrigin { S3UsEast2 = 0, Cloudfront = 1, External = 2 }

// Domain/Enums/NotificationMediaKind.cs
public enum NotificationMediaKind
{
	TemplateAttachment = 0, CampaignAttachment = 1, EventCoverImage = 2, RenderedPdf = 3,
	InboundUpload = 4, Other = 5
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<NotificationMediaOrigin>("notification", "media_origin");
modelBuilder.HasPostgresEnum<NotificationMediaKind>("notification", "media_kind");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<NotificationMediaOrigin>("notification.media_origin");
dataSourceBuilder.MapEnum<NotificationMediaKind>("notification.media_kind");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
	public void Configure(EntityTypeBuilder<MediaAsset> builder)
	{
		builder.ToTable("media_assets", "notification", t => t.HasComment(
			"Catalogo locale al modulo dei riferimenti a file usati nelle comunicazioni: allegati, immagini di copertina, PDF renderizzati, upload da lead."));

		builder.HasKey(x => x.Id).HasName("pk_notification_media_assets");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. NULL per file di piattaforma o upload da lead pubblici.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.IsRequired()
			.HasComment("Codice della sorgente da cui è servito il file; la mappa origin → URL base vive in configurazione.");

		builder.Property(x => x.Path)
			.HasColumnName("path")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Percorso relativo del file rispetto alla base di origin (URL completo invariato se origin = external), senza barra iniziale.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasDefaultValue(NotificationMediaKind.Other)
			.IsRequired()
			.HasComment("Tipo d'uso del file.");

		builder.Property(x => x.Filename)
			.HasColumnName("filename")
			.HasMaxLength(255)
			.HasComment("Nome file originale, mostrato all'utente e usato come filename MIME in allegato.");

		builder.Property(x => x.Checksum)
			.HasColumnName("checksum")
			.HasColumnType("bytea")
			.HasComment("SHA-256 del contenuto, per deduplica in caricamento; NULL per i file migrati finché non riconciliati.");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Dimensione del file in byte.");

		builder.Property(x => x.ContentType)
			.HasColumnName("content_type")
			.HasMaxLength(100)
			.HasComment("MIME type reale, validato con magic bytes in upload.");

		builder.Property(x => x.UploadedByUserId)
			.HasColumnName("uploaded_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema. Valorizzato per gli upload da gestionale.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione dell'asset.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: l'asset non si cancella finché referenziato; la GC fisica sullo storage agisce solo su asset soft-deleted e senza referenti.");

		builder.HasIndex(x => new { x.PropertyId, x.Kind })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_media_assets_property_kind");

		builder.HasIndex(x => x.Checksum)
			.HasFilter("checksum IS NOT NULL")
			.HasDatabaseName("ix_notification_media_assets_checksum");

		builder.HasIndex(x => x.Origin)
			.HasDatabaseName("ix_notification_media_assets_origin");
	}
}
```

`MediaAsset` implementa `ISoftDeletable` e `IAuditable`.

### Note di migrazione

- Parsing di ogni URL/elemento array (`custom_templates.attachment`, `marketing_*.attachments`,
  `marketing_event.coverImage`, `hotel_reports.path`) → `(origin, path)` risolvendo l'host contro la
  mappa nota (`summerbooking.s3.us-east-2.amazonaws.com` → `S3UsEast2`); host sconosciuto →
  `External` con `path` = URL completo invariato.
- Bonifica dell'artefatto `files%5B%5D/` (retaggio form HTML non normalizzate) → `files/` prima di
  salvare `path`.
- Una riga per URL distinto (dedup su `checksum` quando disponibile, altrimenti su `(origin, path)`
  applicativo); le FK `*_asset_id` di altre tabelle e la giunzione `notification_template_attachments`
  si popolano risolvendo l'URL originale verso la riga creata qui.
- `checksum`/`byte_size`/`content_type` non calcolabili in batch dai soli URL legacy → `NULL` per tutte
  le righe migrate, riconciliati da un job successivo che scarica e verifica i file ancora raggiungibili.
- `uploaded_by_user_id` da `custom_templates.createdBy`, quasi sempre `NULL` in prod → resta `NULL` per
  la maggioranza delle righe migrate.
