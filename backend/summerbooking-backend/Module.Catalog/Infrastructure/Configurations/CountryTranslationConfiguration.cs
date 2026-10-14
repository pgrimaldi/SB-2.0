using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="CountryTranslation"/> su catalog.country_translations.</summary>
public sealed class CountryTranslationConfiguration : IEntityTypeConfiguration<CountryTranslation>
{
	public void Configure(EntityTypeBuilder<CountryTranslation> builder)
	{
		builder.ToTable("country_translations", "catalog", t => t.HasComment(
			"Nome di un paese in una culture. Una riga per (paese × lingua); sostituisce le colonne country_english/country_italian."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_country_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.CountryId)
			.HasColumnName("country_id")
			.IsRequired()
			.HasComment("Paese a cui si riferisce la traduzione.");

		builder.Property(x => x.Culture)
			.HasColumnName("culture")
			.HasMaxLength(10)
			.IsRequired()
			.HasComment("Codice cultura BCP-47 (es. it-IT, en-GB).");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome del paese nella lingua indicata.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete; praticamente immutabile, presente per uniformità.");

		builder.HasIndex(x => new { x.CountryId, x.Culture })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_country_translations_country_culture");

		builder.HasIndex(x => x.Culture)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_country_translations_culture");

		builder.HasOne(x => x.Country)
			.WithMany(c => c.Translations)
			.HasForeignKey(x => x.CountryId)
			.HasConstraintName("fk_catalog_country_translations_country")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
