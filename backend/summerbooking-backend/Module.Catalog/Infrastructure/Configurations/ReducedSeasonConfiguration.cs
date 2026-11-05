using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ReducedSeason"/> su catalog.pricing_reduced_seasons.</summary>
public sealed class ReducedSeasonConfiguration : IEntityTypeConfiguration<ReducedSeason>
{
	public void Configure(EntityTypeBuilder<ReducedSeason> builder)
	{
		builder.ToTable("pricing_reduced_seasons", "catalog", t =>
		{
			t.HasComment("Prezzo stagionale ridotto: periodo più breve della stagione, voce dello stabilimento valida per tutti i pacchetti.");
			t.HasCheckConstraint("ck_catalog_pricing_reduced_seasons_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_reduced_seasons");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7) usato dalle API.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_pricing_reduced_seasons_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(120)
			.HasComment("Nome della voce (es. Luglio e agosto).");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Primo giorno del periodo.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Ultimo giorno del periodo.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.PropertyId, "ix_catalog_pricing_reduced_seasons_property")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_reduced_seasons_property");

		builder.HasOne<Property>()
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_reduced_seasons_property")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
