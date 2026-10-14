using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;

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

		// Indice di supporto alla FK: nome esplicito perche' la convenzione del progetto vuole il
		// prefisso flat (ix_notification_*), che EF altrimenti deriva dal nome tabella canonico.
		builder.HasIndex(x => x.MediaAssetId)
			.HasDatabaseName("ix_notification_template_attachments_media_asset_id");

		builder.HasIndex(x => new { x.TemplateId, x.MediaAssetId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_template_attachments_template_asset");

		builder.HasIndex(x => new { x.TemplateId, x.SortOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_template_attachments_template_order");
	}
}
