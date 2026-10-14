# Tabella `notification_template_attachments`

> **Dominio**: C6 — `notification` · **sottoarea**: — · **schema**: `notification` · **nome flat**:
> `notification_template_attachments` · **forma canonica**: `notification.template_attachments`.
>
> Sostituisce l'array JSON [`custom_templates.attachment`](../../../analisi-pre/db/custom_templates.md)
> (`["https://…s3…/Regolamento.docx"]`, 8 righe reali su 103) e gli array
> [`marketing_email_campaign.attachments`](../../../analisi-pre/db/marketing_email_campaign.md) /
> [`marketing_automation.attachments`](../../../analisi-pre/db/marketing_automation.md)
> (`[{filename,path}]`) — nel nuovo disegno campagne/automazioni referenziano un `template_id`, quindi
> gli allegati sono **sempre** del template. "Array di URL → tabella di giunzione ordinata"
> ([`docs/03-gestione-dati.md` §3.1quater.A](../../../progettazione/03-gestione-dati.md)).

---

## Descrizione

**Giunzione ordinata (template × file).** Una riga = "il template T allega il file F in posizione N".
Gli allegati sono spediti con ogni messaggio renderizzato da quel template.

## Verifica di esclusività

Nessun campo `is_*` / enum: pura tabella ponte.

## Snapshot

`filename_snapshot`: il nome file **mostrato nell'allegato** al momento dell'associazione — resta
stabile anche se il file sorgente viene rinominato.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_template_attachments` | `id` | PK | — |
| `uq_..._template_asset` | `(template_id, media_asset_id) WHERE deleted_at IS NULL` | UNIQUE parziale | niente doppioni |
| `ix_..._template_order` | `(template_id, sort_order) WHERE deleted_at IS NULL` | btree parziale | lettura ordinata degli allegati di un template |

## Campi

### id
- **Legacy**: — (elemento di array)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### template_id
- **Legacy**: `custom_templates.id` (contenitore dell'array)
- **Descrizione**: template a cui l'allegato appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`notification_templates.id`](notification_templates.md) `ON DELETE CASCADE`.

### media_asset_id
- **Legacy**: URL dentro l'array `attachment`
- **Descrizione**: file allegato.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`notification_media_assets.id`](notification_media_assets.md) `ON DELETE RESTRICT`.

### sort_order
- **Legacy**: posizione nell'array
- **Descrizione**: ordine di presentazione/allegato.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### filename_snapshot
- **Legacy**: `attachments[].filename` (quando presente) / basename dell'URL
- **Descrizione**: nome file da usare nell'header MIME `Content-Disposition`, congelato all'associazione.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (fallback: `notification_media_assets.filename`).
- **Relazioni**: snapshot.

### created_at / updated_at / deleted_at
- **Legacy**: `custom_templates.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica / soft-delete (rimozione dell'allegato dal template).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

È la **de-jsonizzazione** di `custom_templates.attachment`: 1 riga per elemento dell'array, con
ordine esplicito.

## Suggerimenti per le prestazioni

- Volume trascurabile (≤ pochi allegati per template). Nessun indice oltre a quelli elencati.
- Letta insieme al template (una query con `ORDER BY sort_order`); nessun impatto sul path d'invio.

## Implementazione .NET

`TemplateAttachment` è un'**entità figlia** di `MessageTemplate` nel modulo `Module.Notification`
(schema `notification`): giunzione ordinata template × file (`notification_media_assets`), esposta come
collezione `MessageTemplate.Attachments` navigata dall'aggregato proprietario ma mappata come entità
propria (non owned) perché referenzia a sua volta `MediaAsset` con FK reale. Nessun enum proprio.

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class TemplateAttachmentConfiguration : IEntityTypeConfiguration<TemplateAttachment>
{
	public void Configure(EntityTypeBuilder<TemplateAttachment> builder)
	{
		builder.ToTable("template_attachments", "notification", t => t.HasComment(
			"Giunzione ordinata: il template T allega il file F in posizione N, spedito con ogni messaggio renderizzato da quel template."));

		builder.HasKey(x => x.Id).HasName("pk_notification_template_attachments");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.TemplateId)
			.HasColumnName("template_id")
			.IsRequired()
			.HasComment("Template a cui l'allegato appartiene.");

		builder.Property(x => x.MediaAssetId)
			.HasColumnName("media_asset_id")
			.IsRequired()
			.HasComment("File allegato.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.HasColumnType("smallint")
			.HasDefaultValue((short)0)
			.IsRequired()
			.HasComment("Ordine di presentazione/allegato all'interno del template.");

		builder.Property(x => x.FilenameSnapshot)
			.HasColumnName("filename_snapshot")
			.HasMaxLength(255)
			.HasComment("Nome file per l'header MIME Content-Disposition, congelato all'associazione; fallback sul filename dell'asset.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di associazione dell'allegato al template.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica (es. riordino).");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: rimozione dell'allegato dal template.");

		builder.HasOne(x => x.Template)
			.WithMany(t => t.Attachments)
			.HasForeignKey(x => x.TemplateId)
			.HasConstraintName("fk_notification_template_attachments_template")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.MediaAsset)
			.WithMany()
			.HasForeignKey(x => x.MediaAssetId)
			.HasConstraintName("fk_notification_template_attachments_media_asset")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(x => new { x.TemplateId, x.MediaAssetId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_template_attachments_template_asset");

		builder.HasIndex(x => new { x.TemplateId, x.SortOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_template_attachments_template_order");
	}
}
```

`TemplateAttachment` implementa `ISoftDeletable` e `IAuditable` come le altre entità di dominio dello
schema.

### Note di migrazione

- `custom_templates.attachment` (array URL, 8 righe reali su 103): ogni URL distinto → prima una riga
  `notification_media_assets` (se non già presente per lo stesso URL/hash), poi una riga qui con
  `sort_order` = posizione nell'array originale.
- `marketing_email_campaign.attachments` / `marketing_automation.attachments`
  (`[{filename,path}]`): stesso schema di esplosione; `filename_snapshot` valorizzato da `filename`
  quando presente nell'oggetto, altrimenti basename di `path`/URL.
- Bonifica dei nomi parametro legacy `files%5B%5D` → `files` in fase di parsing dell'array prima
  dell'esplosione (retaggio di form HTML non normalizzate).
- Nessun campo booleano/enum da mappare: tabella ponte pura, un `INSERT` per elemento d'array.
