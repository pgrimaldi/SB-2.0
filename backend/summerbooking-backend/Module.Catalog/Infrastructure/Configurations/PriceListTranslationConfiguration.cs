using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PriceListTranslation"/> su catalog.pricing_price_list_translations.</summary>
public sealed class PriceListTranslationConfiguration : IEntityTypeConfiguration<PriceListTranslation>
{
	public void Configure(EntityTypeBuilder<PriceListTranslation> builder)
	{
		builder.ToTable("pricing_price_list_translations", "catalog", t => t.HasComment(
			"Nome di pacchetti e articoli aggiuntivi nelle altre lingue."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Voce di listino.");

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

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.PriceListId, x.Language }, "uq_catalog_pricing_price_list_translations_language")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_translations_language");

		builder.HasOne<PriceList>()
			.WithMany(p => p.Translations)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_translations_parent")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
