# Tabella `fiskaly_media_assets`

> **Dominio**: C4 — `fiskaly` · **sottoarea**: — · **schema**: `fiskaly` · **nome flat**:
> `fiskaly_media_assets` · **forma canonica**: `fiskaly.media_assets`.
>
> Tabella di supporto **nuova** (nessuna legacy diretta). Sostituisce il pattern legacy "URL S3
> completo salvato in colonna `varchar(500)`" su
> [`fiskaly_receipt.e_receipt_path`](../../../analisi-pre/db/fiskaly_receipt.md) e
> [`fiskaly_non_fiscal_receipt.e_receipt_path`](../../../analisi-pre/db/fiskaly_non_fiscal_receipt.md).
> Analoga a [`catalog_media_assets`](../catalog/catalog_media_assets.md) (una **per modulo**,
> [`docs/03-gestione-dati.md` §1.3–§1.4, §3.1quater.A](../../../progettazione/03-gestione-dati.md) — che cita
> esplicitamente "`fiskaly_media_assets` (PDF scontrini)" tra le future).

---

## Descrizione

Catalogo **locale al modulo `fiskaly`** dei riferimenti a file — oggi solo i **PDF degli scontrini**
(fiscali e pre-conto). Ogni file è scomposto in `origin` (codice della sorgente: bucket/CDN) + `path`
(percorso relativo). La mappa `origin → URL base` vive in **configurazione**, non a DB: un cambio di
bucket/CDN è una modifica di config, mai una migrazione dati ([`docs/03 §1.2`](../../../progettazione/03-gestione-dati.md)).

Perché una tabella **per modulo** e non una `media_assets` globale: una tabella unica referenziata da
schemi diversi violerebbe "zero FK/JOIN cross-modulo" ([`docs/03 §1.4`](../../../progettazione/03-gestione-dati.md)).

[`fiskaly_receipts.pdf_asset_id`](fiskaly_receipts.md) vi punta con **FK interna** (`bigint`).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `kind` | **enum** esclusivo: `fiscal_receipt_pdf` / `non_fiscal_receipt_pdf` (allineato a `fiskaly_receipts.document_kind`). |
| `origin` | **enum** esclusivo. |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiskaly_media_assets` | `id` | PK | — |
| `ix_fiskaly_media_assets_kind` | `(kind) WHERE deleted_at IS NULL` | btree parziale | filtri per tipo |
| `ix_fiskaly_media_assets_origin` | `(origin)` | btree | riconciliazione in migrazione / audit sorgenti |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `fiskaly_receipts.pdf_asset_id`.
- **Note**: **solo `bigint`** — referenziata solo entro lo schema `fiskaly`. Nessun `public_id`.

### origin
- **Legacy**: ricavato dall'host dell'URL legacy (`summerbooking.s3.us-east-2.amazonaws.com`)
- **Descrizione**: codice della sorgente da cui è servito il file. La mappa `origin → URL base` vive
  in configurazione.
- **Tipo**: `enum fiskaly.media_origin` — valori: `s3_us_east_2`, `cloudfront`, `external`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: se `external`, `path` contiene l'URL completo invariato.

### path
- **Legacy**: porzione dell'URL legacy dopo l'host (`receipts/fiskaly/<e_receipt_id>.pdf`)
- **Descrizione**: percorso relativo del file rispetto alla base di `origin`.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: nessuna barra iniziale; `BuildUrl(origin, path)` in applicazione.

### kind
- **Legacy**: dedotto dalla tabella di provenienza (`fiskaly_receipt` → `fiscal_receipt_pdf`;
  `fiskaly_non_fiscal_receipt` → `non_fiscal_receipt_pdf`)
- **Descrizione**: tipo d'uso del file.
- **Tipo**: `enum fiskaly.media_kind` — valori: `fiscal_receipt_pdf`, `non_fiscal_receipt_pdf`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### checksum / byte_size / content_type
- **Legacy**: — (nuovo)
- **Descrizione**: hash SHA-256 del contenuto (deduplica/integrità), dimensione in byte, MIME type
  (`application/pdf`). Sconosciuti per i file migrati → `NULL` finché non rigenerati.
- **Tipo**: `bytea` (32 byte) · `integer` · `varchar(100)`.
- **Uniformato il 2026-10-01** (M19 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`): era `char(64)`, cioè il digest in
  esadecimale. Ora è `bytea` come in `catalog.media_assets` e
  `switchboard.switchboard_media_assets`: la stessa informazione era dichiarata con due tipi
  diversi, e appena la utility dei file inizia a scrivere ogni confronto fra tabelle sarebbe
  diventato una conversione — più il padding a spazi che `char(n)` aggiunge.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì / Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.546 righe storiche: il valore si ottiene solo scaricando il file da S3, che è un lavoro di natura diversa dall'ETL. Lo fa `utility/02-migrazione-file` (decisione D9 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`), non il backend.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete. Un asset non è cancellato finché lo referenzia uno scontrino; per i
  PDF di scontrini fiscali di fatto non si cancella (retention 10 anni).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: la GC fisica sullo storage è un job separato che agisce solo su asset
  `deleted_at IS NOT NULL` **e** senza referenti.

## Valutazione JSON

Nessun JSON. Elimina l'URL S3 completo in colonna, sostituendolo con `origin` + `path` + FK.

## Suggerimenti per le prestazioni

- Volume = numero di scontrini (fiscali + pre-conto). Nessun indice speciale oltre a quelli elencati.
- `origin → URL base` risolto in memoria (dizionario da configurazione): `BuildUrl` non tocca il DB.

## Implementazione .NET

`MediaAsset` è un'entità di **supporto** del modulo `Module.Fiskaly` (schema `fiskaly`): catalogo
locale al modulo dei riferimenti a file (oggi solo PDF scontrini), referenziata con **FK reale** da
[`fiskaly_receipts.pdf_asset_id`](fiskaly_receipts.md). Riusa lo stesso pattern `Origin`/`Path` di
`catalog_media_assets` (C2), ma è una tabella **propria del modulo** — mai una `media_assets` globale
cross-schema (regola "zero FK/JOIN cross-modulo",
[`docs/03 §1.4`](../../../progettazione/03-gestione-dati.md)).

### Enum propri

```csharp
// Domain/Enums/FiskalyMediaOrigin.cs
public enum FiskalyMediaOrigin { S3UsEast2 = 0, Cloudfront = 1, External = 2 }

// Domain/Enums/FiskalyMediaKind.cs
public enum FiskalyMediaKind { FiscalReceiptPdf = 0, NonFiscalReceiptPdf = 1 }
```

```csharp
// FiskalyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<FiskalyMediaOrigin>("fiskaly", "media_origin");
modelBuilder.HasPostgresEnum<FiskalyMediaKind>("fiskaly", "media_kind");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<FiskalyMediaOrigin>("fiskaly.media_origin");
dataSourceBuilder.MapEnum<FiskalyMediaKind>("fiskaly.media_kind");
```

### Value object

```csharp
// Domain/ValueObjects/MediaRef.cs — riuso dello stesso pattern di Catalog
public sealed record MediaRef(FiskalyMediaOrigin Origin, string Path)
{
	public string BuildUrl(IReadOnlyDictionary<FiskalyMediaOrigin, string> originBaseUrls) =>
		Origin == FiskalyMediaOrigin.External ? Path : $"{originBaseUrls[Origin].TrimEnd('/')}/{Path}";
}
```

### Configuration

```csharp
namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
	public void Configure(EntityTypeBuilder<MediaAsset> builder)
	{
		builder.ToTable("fiskaly_media_assets", "fiskaly", t => t.HasComment(
			"Catalogo locale al modulo fiskaly dei riferimenti a file — oggi solo i PDF degli scontrini fiscali e pre-conto."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_media_assets");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.OwnsOne(x => x.Ref, r =>
		{
			r.Property(p => p.Origin)
				.HasColumnName("origin")
				.HasColumnType("fiskaly.media_origin")
				.IsRequired()
				.HasComment("Codice della sorgente da cui è servito il file; la mappa origin → URL base vive in configurazione.");
			r.HasIndex(p => p.Origin).HasDatabaseName("ix_fiskaly_media_assets_origin");

			r.Property(p => p.Path)
				.HasColumnName("path")
				.HasColumnType("text")
				.IsRequired()
				.HasComment("Percorso relativo del file rispetto alla base di origin (nessuna barra iniziale); se origin=external, URL completo invariato.");
		});

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("fiskaly.media_kind")
			.IsRequired()
			.HasComment("Tipo d'uso del file: PDF scontrino fiscale o pre-conto.");
		builder.HasIndex(x => x.Kind)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_fiskaly_media_assets_kind");

		builder.Property(x => x.Checksum)
			.HasColumnName("checksum")
			.HasColumnType("bytea")
			.HasComment("Hash SHA-256 del contenuto per deduplica/integrità; NULL per i file migrati finché non rigenerati.");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Dimensione del file in byte.");

		builder.Property(x => x.ContentType)
			.HasColumnName("content_type")
			.HasMaxLength(100)
			.HasComment("MIME type del file (es. application/pdf).");

		// created_at/updated_at/deleted_at (ISoftDeletable/IAuditable) — plumbing comune, vedi _dotnet-template.md §1-2
	}
}
```

`MediaAsset` implementa `IAuditable` e `ISoftDeletable`: un asset non è cancellato finché lo referenzia
uno scontrino; per i PDF di scontrini fiscali di fatto non si cancella mai (retention 10 anni). La GC
fisica sullo storage è un job separato che agisce solo su asset `deleted_at IS NOT NULL` **e** senza
referenti — non un'operazione EF.

### Note di migrazione

- `origin → URL base` risolto in memoria da `StorageOptions.Origins` (`appsettings` + Parameter Store),
  mai una tabella/colonna DB.
- Parsing di ogni `e_receipt_path` legacy (URL S3 completo) in `(origin, path)`: una riga per URL
  distinto; la FK `fiskaly_receipts.pdf_asset_id` si popola risolvendo l'URL alla riga corrispondente
  (creata se non già presente).
- `checksum`/`byte_size`/`content_type` restano `NULL` per i file migrati finché non vengono
  rigenerati o ricalcolati da un job di backfill.
