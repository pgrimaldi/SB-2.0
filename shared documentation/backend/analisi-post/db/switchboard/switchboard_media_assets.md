# Tabella `switchboard_media_assets`

> **Dominio**: C7 — `switchboard` · **sottoarea**: — (radice del dominio) · **schema**: `switchboard`
> · **nome flat**: `switchboard_media_assets` · **forma canonica**: `switchboard.media_assets`.
>
> **Redesign** di [`switchboard_audio_file`](../../../analisi-pre/db/switchboard_audio_file.md)
> (0 righe in `local_sb` e in produzione — ma **verificato 2026-09-11**: 5 righe reali, tutte entro il
> limite di 3 MB, su `summer_booking_dev`; conferma `mime_type`/`s3_bucket` realmente costanti come
> ipotizzato sotto). Tabella di supporto **per modulo**, stessa forma di
> [`catalog_media_assets`](../catalog/catalog_media_assets.md) / `fiskaly_media_assets` /
> `notification_media_assets` — prevista esplicitamente in
> [`docs/03-gestione-dati.md` §3.1quater.A](../../../progettazione/03-gestione-dati.md)
> (*"`switchboard_media_assets` (prompt audio) — stessa forma"*).
>
> **Fonti**: [`analisi-pre/db/switchboard_audio_file.md`](../../../analisi-pre/db/switchboard_audio_file.md),
> [`docs/03-gestione-dati.md` §1 (Link/URL a DB) / §3.1quater.A](../../../progettazione/03-gestione-dati.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §10](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/80-retention.md` §3.7 / §5](../../../progettazione/80-retention.md).

---

## Descrizione

Catalogo **locale allo schema `switchboard`** dei file audio (prompt IVR: benvenuto, voci di menù,
fallback) di uno stabilimento. Ogni file è scomposto in `origin` (codice della sorgente storage) +
`path` (chiave relativa); la mappa `origin → URL base` vive in configurazione, **non** a DB
([`docs/03 §1.2`](../../../progettazione/03-gestione-dati.md)). Le entità che nel legacy avevano un id-audio in
colonna diventano **FK** verso questa tabella nello stesso schema:
`welcome_audio_asset_id` / `fallback_audio_asset_id` di [`switchboard_configs`](switchboard_configs.md)
e `audio_asset_id` di [`switchboard_voice_menu_items`](switchboard_voice_menu_items.md).

Correzioni rispetto al legacy:

- **`s3_bucket`** (scritto, mai riletto — tutte le operazioni S3 usano una costante) → **non migrato**:
  il bucket vive in `origin` + configurazione ([`docs/03 §1`](../../../progettazione/03-gestione-dati.md)).
- **`s3_key`** (URL/chiave assoluta) → `origin` + `path` (relativo).
- **`mime_type`** (`varchar(50) DEFAULT 'audio/mpeg'`, in pratica **costante**: tutto convertito in
  MP3) → `content_type` con default `audio/mpeg`, senza pretesa informativa.
- **Nessuna deduplica** (due upload dello stesso file = due righe + due oggetti S3) → `checksum` +
  `UNIQUE` parziale opzionale.
- **Endpoint proxy pubblico senza rate limiting** (`getPublicAudioUrl`, chiunque conosca un `id`
  numerico scarica l'audio) → `public_id` **non** esposto sull'URL pubblico; l'accesso da Telnyx passa
  per un URL firmato a scadenza breve emesso dal modulo (annotazione, non colonna).
- **Cancellazione parziale su errore S3** (la riga viene rimossa comunque, oggetto orfano su S3) →
  `deleted_at` + job di garbage collection disaccoppiato che agisce solo su asset senza referenti.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `content_type` | di fatto **costante** (`audio/mpeg`): tutto viene convertito in MP3. Nessun enum utile; colonna informativa. |
| — | nessun campo `is_*`; nessun gruppo di flag → nessun `enum` / `enum[]`. |

Il "ruolo" di un audio (benvenuto / prompt di menù / fallback) **non** è una colonna: è determinato da
quale FK lo referenzia. Nessun `kind` (a differenza di `catalog_media_assets`, che cataloga tipi
eterogenei): qui il contenuto è sempre un prompt audio.

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_media_assets` | `id` | PK | — |
| `uq_switchboard_media_assets_public_id` | `(public_id)` | UNIQUE | id pubblico (pannello, anteprima) |
| `ix_switchboard_media_assets_config` | `(config_id) WHERE deleted_at IS NULL` | parziale | elenco audio di uno stabilimento (`listAudioFiles`) |
| `ix_switchboard_media_assets_gc` | `(deleted_at) WHERE deleted_at IS NOT NULL` | parziale | garbage collection degli asset soft-deleted senza referenti |
| `uq_switchboard_media_assets_checksum` | `(config_id, checksum) WHERE checksum IS NOT NULL AND deleted_at IS NULL` | UNIQUE parziale (opzionale) | deduplica per stabilimento |

## Campi

### id
- **Legacy**: `switchboard_audio_file.id`
- **Descrizione**: PK tecnica interna allo schema.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `welcome_audio_asset_id` / `fallback_audio_asset_id` di
  [`switchboard_configs`](switchboard_configs.md) e `audio_asset_id` di
  [`switchboard_voice_menu_items`](switchboard_voice_menu_items.md).
- **Note**: **solo `bigint`** — referenziata unicamente dentro lo schema `switchboard`
  ([`docs/03 §3.1ter`](../../../progettazione/03-gestione-dati.md)).

### public_id
- **Legacy**: — (nel legacy l'`id` numerico era esposto sull'URL pubblico verso Telnyx)
- **Descrizione**: id pubblico per il pannello e l'anteprima autenticata; **non** compare nell'URL
  pubblico servito a Telnyx.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato applicativamente.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### config_id
- **Legacy**: `switchboard_audio_file.property_id` (FK → `properties.id`)
- **Descrizione**: centralino proprietario del file. Nel legacy la FK era su `property_id`; qui il file
  appartiene alla **configurazione** del centralino (che è 1:1 con la property).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`switchboard_configs`](switchboard_configs.md)`.id` (stesso schema),
  `ON DELETE CASCADE` applicativo (`deleteConfig` cancellava a mano audio + oggetti S3).

### property_id
- **Legacy**: `switchboard_audio_file.property_id`
- **Descrizione**: stabilimento (denormalizzato da `switchboard_configs`, per scoping/retention e
  filtri multi-tenant senza join).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo per valore.

### origin
- **Legacy**: ricavato in migrazione dall'host di `s3_key` / dalla costante `S3_BUCKET`
- **Descrizione**: codice della sorgente storage. La mappa `origin → URL base` è in configurazione.
- **Tipo**: `enum switchboard.media_origin` — valori: `s3_private`, `external`. (Il legacy caricava su
  bucket S3 **privato** senza ACL — buona pratica già presente; estendibile per deploy.)
- **Default**: `s3_private`.
- **Nullabile**: No.
- **Relazioni**: —.

### path
- **Legacy**: `switchboard_audio_file.s3_key` — nel legacy `audio/switchboard/<propertyId>/<uuidv7>.mp3`
- **Descrizione**: chiave relativa dell'oggetto rispetto alla base di `origin`. Nessuna barra iniziale.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### original_name
- **Legacy**: `switchboard_audio_file.original_name` (`varchar(255)`)
- **Descrizione**: nome file caricato dall'operatore (`file.originalname`). Solo visualizzazione.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### description
- **Legacy**: `switchboard_audio_file.description` (`varchar(255)`)
- **Descrizione**: etichetta leggibile assegnata all'upload.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### content_type
- **Legacy**: `switchboard_audio_file.mime_type` (`varchar(50) DEFAULT 'audio/mpeg'`)
- **Descrizione**: MIME type del file finale. In pratica sempre `audio/mpeg` (conversione a MP3
  obbligatoria). Da validare all'upload contro il contenuto reale.
- **Tipo**: `varchar(50)`.
- **Default**: `'audio/mpeg'`.
- **Nullabile**: No.
- **Relazioni**: —.

### byte_size
- **Legacy**: `switchboard_audio_file.file_size` (`int`)
- **Descrizione**: dimensione in byte dell'MP3 finale. Vincolo effettivo:
  `≤ SWITCHBOARD_MAX_AUDIO_SIZE_MB * 1024 * 1024` (default 3 MB) — da riprodurre come validazione
  applicativa (il legacy non lo verificava a DB).
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `CHECK (byte_size IS NULL OR byte_size > 0)`.

### duration_ms
- **Legacy**: — (nuovo; il legacy non salvava la durata)
- **Descrizione**: durata del prompt in millisecondi, nota alla conversione `ffmpeg`. Utile per la UI
  e per stimare i tempi IVR.
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 5 righe storiche: il valore si ottiene solo scaricando il file da S3, che è un lavoro di natura diversa dall'ETL. Lo fa `utility/02-migrazione-file` (decisione D9 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`), non il backend.

### checksum
- **Legacy**: — (nuovo)
- **Descrizione**: SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità.
- **Tipo**: `bytea` (32 byte) — oppure `char(64)` hex.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `UNIQUE` parziale opzionale (vedi Indici).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 5 righe storiche: il valore si ottiene solo scaricando il file da S3, che è un lavoro di natura diversa dall'ETL. Lo fa `utility/02-migrazione-file` (decisione D9 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`), non il backend.

### created_at / updated_at / deleted_at
- **Legacy**: `switchboard_audio_file` — `timestamps: false`
- **Descrizione**: creazione / modifica / soft-delete. La cancellazione fisica del file su storage è un
  job separato che agisce solo su asset `deleted_at IS NOT NULL` **e** senza referenti (evita
  l'oggetto orfano del legacy).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. La tabella elimina l'URL/chiave assoluta (`s3_key`) e il bucket ridondante (`s3_bucket`)
sostituendoli con `origin` + `path` componibili in applicazione.

## Suggerimenti per le prestazioni

- Pochissime righe per stabilimento (una manciata di prompt). Nessun indice speciale oltre a quelli
  elencati.
- La risoluzione `origin → URL base` è in memoria (dizionario da configurazione).
- L'URL pubblico servito a Telnyx è **firmato a scadenza breve** (non un `id` numerico indovinabile
  come nel legacy) — nessun costo DB aggiuntivo, elimina l'endpoint proxy pubblico non protetto.
- Deduplica per `checksum`: `UNIQUE` parziale opzionale; il calcolo dell'hash all'upload ha costo
  trascurabile (file < 3 MB).

## Implementazione .NET

`SwitchboardMediaAsset` è un'**entità figlia** (tabella di supporto per modulo, stessa forma di
`catalog_media_assets`/`fiskaly_media_assets`/`notification_media_assets`) del modulo
`Module.Switchboard`: cataloga i prompt audio IVR di un centralino, referenziata dalle FK
`welcome_audio_asset_id`/`fallback_audio_asset_id` di [`switchboard_configs`](switchboard_configs.md) e
`audio_asset_id` di [`switchboard_voice_menu_items`](switchboard_voice_menu_items.md). Nessuna
riga su `local_sb`/dump di produzione, ma **5 righe reali** (tutte < 3 MB) su `summer_booking_dev`.

L'`origin`+`path` compongono un value object `MediaRef` (owned, non mappato con `OwnsOne` perché sono
solo 2 colonne scalari senza comportamento proprio oltre `ToUrl(...)` — resta proprietà scalare
dell'entità, coerente con la scheda che non segnala un candidato owned type).

### Enum

```csharp
// Module.Switchboard.Domain.Enums/MediaOrigin.cs
public enum MediaOrigin
{
	S3Private = 0,
	External = 1
}
```

```csharp
// SwitchboardDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<MediaOrigin>("switchboard", "media_origin");
```

```csharp
// Module.Switchboard.Infrastructure DI
dataSourceBuilder.MapEnum<MediaOrigin>("switchboard.media_origin");
```

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations;

public sealed class SwitchboardMediaAssetConfiguration : IEntityTypeConfiguration<SwitchboardMediaAsset>
{
	public void Configure(EntityTypeBuilder<SwitchboardMediaAsset> builder)
	{
		builder.ToTable("switchboard_media_assets", "switchboard", t => t.HasComment(
			"Catalogo locale allo schema switchboard dei file audio (prompt IVR) di uno stabilimento."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_media_assets");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico per il pannello e l'anteprima autenticata; non compare nell'URL pubblico servito a Telnyx.");

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino proprietario del file audio.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping/retention.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("switchboard.media_origin")
			.IsRequired()
			.HasDefaultValue(MediaOrigin.S3Private)
			.HasComment("Codice della sorgente storage; la mappa origin → URL base vive in configurazione.");

		builder.Property(x => x.Path)
			.HasColumnName("path")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Chiave relativa dell'oggetto rispetto alla base di origin, nessuna barra iniziale.");

		builder.Property(x => x.OriginalName)
			.HasColumnName("original_name")
			.HasMaxLength(255)
			.HasComment("Nome file caricato dall'operatore, solo visualizzazione.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasMaxLength(255)
			.HasComment("Etichetta leggibile assegnata all'upload.");

		builder.Property(x => x.ContentType)
			.HasColumnName("content_type")
			.HasMaxLength(50)
			.IsRequired()
			.HasDefaultValue("audio/mpeg")
			.HasComment("MIME type del file finale, in pratica sempre audio/mpeg (conversione a MP3 obbligatoria).");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Dimensione in byte dell'MP3 finale.");

		builder.Property(x => x.DurationMs)
			.HasColumnName("duration_ms")
			.HasComment("Durata del prompt in millisecondi, nota alla conversione ffmpeg.");

		builder.Property(x => x.Checksum)
			.HasColumnName("checksum")
			.HasColumnType("bytea")
			.HasComment("SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità.");

		builder.ToTable(t => t.HasCheckConstraint(
			"ck_switchboard_media_assets_byte_size", "byte_size IS NULL OR byte_size > 0"));

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_media_assets_public_id");

		builder.HasIndex(x => x.ConfigId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_media_assets_config");

		builder.HasIndex(x => x.DeletedAt)
			.HasFilter("deleted_at IS NOT NULL")
			.HasDatabaseName("ix_switchboard_media_assets_gc");

		builder.HasIndex(x => new { x.ConfigId, x.Checksum })
			.IsUnique()
			.HasFilter("checksum IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("uq_switchboard_media_assets_checksum");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.MediaAssets)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_media_assets_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`SwitchboardMediaAsset` implementa `ISoftDeletable`/`IAuditable`. Il job GC hard-delete (storage + riga)
agisce solo su `deleted_at IS NOT NULL` **senza referenti** — query separata, non un vincolo DB.

### Note di migrazione

- Servizio di upload: valida `content_type` reale + dimensione (`SWITCHBOARD_MAX_AUDIO_SIZE_MB`,
  default 3 MB, solo applicativo — il legacy non lo verificava a DB), converte in MP3 (`ffmpeg`,
  `libmp3lame -qscale:a 2`), calcola `checksum`/`duration_ms`.
- `GetPublicAudioUrl` diventa un presigned URL a TTL breve verso Telnyx: elimina l'endpoint proxy
  pubblico del legacy (`id` numerico indovinabile, nessun rate limiting).
- 0 righe su `local_sb`/dump di produzione → nessun backfill da quelle copie; **5 righe reali** su
  `summer_booking_dev` migrabili 1:1 se quell'ambiente va portato al nuovo schema (`s3_key` assoluto →
  `origin` + `path` relativo, `s3_bucket` non migrato).
