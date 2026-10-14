using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceTypeIcon"/> su catalog.service_type_icons.</summary>
public sealed class ServiceTypeIconConfiguration : IEntityTypeConfiguration<ServiceTypeIcon>
{
	public void Configure(EntityTypeBuilder<ServiceTypeIcon> builder)
	{
		builder.ToTable("service_type_icons", "catalog", t => t.HasComment(
			"Catalogo master delle icone disponibili per i tre tipi di servizio venduto (bar/normale/a ore)."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_type_icons");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Type)
			.HasColumnName("type")
			.HasColumnType("catalog.service_sale_kind")
			.IsRequired()
			.HasComment("Tipo di servizio a cui l'icona è destinata.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome descrittivo dell'icona (etichetta/tooltip nel selettore).");

		builder.Property(x => x.IconAssetId)
			.HasColumnName("icon_asset_id")
			.IsRequired()
			.HasComment("Il file icona.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Icona selezionabile.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione dell'icona.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica dell'icona.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.Type)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_type_icons_type");

		builder.HasIndex(x => x.IconAssetId)
			.HasDatabaseName("ix_catalog_service_type_icons_media_asset");

		builder.HasOne<MediaAsset>()
			.WithMany()
			.HasForeignKey(x => x.IconAssetId)
			.HasConstraintName("fk_catalog_service_type_icons_icon_asset")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
