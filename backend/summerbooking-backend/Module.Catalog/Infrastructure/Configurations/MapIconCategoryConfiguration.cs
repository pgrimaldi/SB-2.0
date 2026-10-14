using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MapIconCategory"/> su catalog.map_icon_categories.</summary>
public sealed class MapIconCategoryConfiguration : IEntityTypeConfiguration<MapIconCategory>
{
	public void Configure(EntityTypeBuilder<MapIconCategory> builder)
	{
		builder.ToTable("map_icon_categories", "catalog", t => t.HasComment(
			"Lookup globale/di piattaforma: le categorie in cui sono raggruppate le icone della mappa spiaggia."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_icon_categories");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasColumnType("catalog.map_icon_category")
			.IsRequired()
			.HasComment("Codice stabile della categoria, usato in codice e come chiave di risorsa di localizzazione (nessuna colonna etichetta).");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Categoria selezionabile; consente di ritirarla senza cancellarla.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete; praticamente immutabile, presente per uniformità.");

		builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_catalog_map_icon_categories_code");
	}
}
