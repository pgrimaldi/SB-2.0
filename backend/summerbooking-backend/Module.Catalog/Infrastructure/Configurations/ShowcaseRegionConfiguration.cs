using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ShowcaseRegion"/> su catalog.showcase_regions.</summary>
public sealed class ShowcaseRegionConfiguration : IEntityTypeConfiguration<ShowcaseRegion>
{
	public void Configure(EntityTypeBuilder<ShowcaseRegion> builder)
	{
		builder.ToTable("showcase_regions", "catalog", t => t.HasComment(
			"Regioni della vetrina gestite dal systemadmin; assegnate alle strutture dalla regione dell'indirizzo, modificabili a mano."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_showcase_regions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7) usato dalle API.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_showcase_regions_public_id");

		builder.Property(x => x.CountryId)
			.HasColumnName("country_id")
			.HasComment("Paese della regione.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(120)
			.HasComment("Nome in italiano.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordine di visualizzazione.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.CountryId, x.Name }, "uq_catalog_showcase_regions_name")
			.IsUnique()
			.AreNullsDistinct(false)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_showcase_regions_name");

		builder.HasOne<Country>()
			.WithMany()
			.HasForeignKey(x => x.CountryId)
			.HasConstraintName("fk_catalog_showcase_regions_country")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
