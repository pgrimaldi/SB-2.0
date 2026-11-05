using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AutomaticPriceTranslation"/> su catalog.automatic_price_translations.</summary>
public sealed class AutomaticPriceTranslationConfiguration : IEntityTypeConfiguration<AutomaticPriceTranslation>
{
	public void Configure(EntityTypeBuilder<AutomaticPriceTranslation> builder)
	{
		builder.ToTable("automatic_price_translations", "catalog", t => t.HasComment(
			"Nome e descrizione di un prezzo automatico nelle altre lingue."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_automatic_price_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AutomaticPriceId)
			.HasColumnName("automatic_price_id")
			.IsRequired()
			.HasComment("Riferimento a un prezzo automatico.");

		builder.Property(x => x.Language)
			.HasColumnName("language")
			.IsRequired()
			.HasColumnType("catalog.content_language")
			.HasComment("Lingua della traduzione.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(255)
			.HasComment("Nome tradotto.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasColumnType("text")
			.HasComment("Descrizione tradotta.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.AutomaticPriceId, x.Language }, "uq_catalog_automatic_price_translations_language")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_automatic_price_translations_language");

		builder.HasOne<AutomaticPrice>()
			.WithMany(p => p.Translations)
			.HasForeignKey(x => x.AutomaticPriceId)
			.HasConstraintName("fk_catalog_automatic_price_translations_parent")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
