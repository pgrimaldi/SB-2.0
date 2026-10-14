using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="BathhouseAmenity"/> su catalog.bathhouse_amenities.</summary>
public sealed class BathhouseAmenityConfiguration : IEntityTypeConfiguration<BathhouseAmenity>
{
	public void Configure(EntityTypeBuilder<BathhouseAmenity> builder)
	{
		builder.ToTable("bathhouse_amenities", "catalog", t => t.HasComment(
			"Giunzione N:N: quali dotazioni/servizi (amenity) offre una vetrina. Sostituisce la copia JSON integrale bathouses.services."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_bathhouse_amenities");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.IsRequired()
			.HasComment("Vetrina che offre l'amenity.");

		builder.Property(x => x.AmenityId)
			.HasColumnName("amenity_id")
			.IsRequired()
			.HasComment("La dotazione/servizio offerto (catalog.service_amenities).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga (selezione dell'amenity).");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: amenity deselezionata dalla vetrina.");

		builder.HasIndex(x => new { x.BathhouseId, x.AmenityId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_bathhouse_amenities_bathhouse_amenity");

		builder.HasIndex(x => x.BathhouseId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_bathhouse_amenities_bathhouse");

		builder.HasIndex(x => x.AmenityId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_bathhouse_amenities_amenity");

		builder.HasOne(x => x.Bathhouse)
			.WithMany(b => b.Amenities)
			.HasForeignKey(x => x.BathhouseId)
			.HasConstraintName("fk_catalog_bathhouse_amenities_bathhouse")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Amenity)
			.WithMany()
			.HasForeignKey(x => x.AmenityId)
			.HasConstraintName("fk_catalog_bathhouse_amenities_amenity")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
