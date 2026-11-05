using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ShowcaseRegionTranslation"/> su catalog.showcase_region_translations.</summary>
public sealed class ShowcaseRegionTranslationConfiguration : IEntityTypeConfiguration<ShowcaseRegionTranslation>
{
	public void Configure(EntityTypeBuilder<ShowcaseRegionTranslation> builder)
	{
		builder.ToTable("showcase_region_translations", "catalog", t => t.HasComment(
			"Nome delle regioni della vetrina nelle altre lingue."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_showcase_region_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.RegionId)
			.HasColumnName("region_id")
			.IsRequired()
			.HasComment("Regione.");

		builder.Property(x => x.Language)
			.HasColumnName("language")
			.IsRequired()
			.HasColumnType("catalog.content_language")
			.HasComment("Lingua della traduzione.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(120)
			.HasComment("Nome tradotto.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.RegionId, x.Language }, "uq_catalog_showcase_region_translations_language")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_showcase_region_translations_language");

		builder.HasOne<ShowcaseRegion>()
			.WithMany(p => p.Translations)
			.HasForeignKey(x => x.RegionId)
			.HasConstraintName("fk_catalog_showcase_region_translations_parent")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
