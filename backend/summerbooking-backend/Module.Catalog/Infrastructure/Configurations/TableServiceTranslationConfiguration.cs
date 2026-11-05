using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="TableServiceTranslation"/> su catalog.table_service_translations.</summary>
public sealed class TableServiceTranslationConfiguration : IEntityTypeConfiguration<TableServiceTranslation>
{
	public void Configure(EntityTypeBuilder<TableServiceTranslation> builder)
	{
		builder.ToTable("table_service_translations", "catalog", t => t.HasComment(
			"Nome e descrizione di un servizio tavoli nelle altre lingue."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_table_service_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.TableServiceId)
			.HasColumnName("table_service_id")
			.IsRequired()
			.HasComment("Riferimento a un servizio tavoli.");

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

		builder.HasIndex(x => new { x.TableServiceId, x.Language }, "uq_catalog_table_service_translations_language")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_table_service_translations_language");

		builder.HasOne<TableService>()
			.WithMany(p => p.Translations)
			.HasForeignKey(x => x.TableServiceId)
			.HasConstraintName("fk_catalog_table_service_translations_parent")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
