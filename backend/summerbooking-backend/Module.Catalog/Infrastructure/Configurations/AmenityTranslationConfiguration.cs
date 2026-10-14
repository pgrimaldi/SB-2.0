using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AmenityTranslation"/> su catalog.service_amenity_translations.</summary>
public sealed class AmenityTranslationConfiguration : IEntityTypeConfiguration<AmenityTranslation>
{
	public void Configure(EntityTypeBuilder<AmenityTranslation> builder)
	{
		builder.ToTable("service_amenity_translations", "catalog", t => t.HasComment(
			"Nome di una dotazione (catalog_service_amenities) in una specifica culture."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_amenity_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AmenityId)
			.HasColumnName("amenity_id")
			.IsRequired()
			.HasComment("Dotazione a cui si riferisce la traduzione.");

		builder.Property(x => x.Culture)
			.HasColumnName("culture")
			.HasMaxLength(10)
			.IsRequired()
			.HasComment("Codice cultura BCP-47 (es. it-IT, en-GB).");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome della dotazione nella lingua indicata.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della traduzione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della traduzione.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.AmenityId, x.Culture })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_service_amenity_translations_amenity_culture");

		builder.HasIndex(x => x.Culture)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_amenity_translations_culture");

		builder.HasOne<Amenity>()
			.WithMany(x => x.Translations)
			.HasForeignKey(x => x.AmenityId)
			.HasConstraintName("fk_catalog_service_amenity_translations_amenity")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
