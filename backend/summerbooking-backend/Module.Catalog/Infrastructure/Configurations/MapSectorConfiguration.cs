using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MapSector"/> su catalog.map_sectors.</summary>
public sealed class MapSectorConfiguration : IEntityTypeConfiguration<MapSector>
{
	public void Configure(EntityTypeBuilder<MapSector> builder)
	{
		builder.ToTable("map_sectors", "catalog", t => t.HasComment(
			"Settori/file in cui è divisa la spiaggia di uno stabilimento. Sostituisce l'array JSON sectors_rows.sectors."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_sectors");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo da C1 (booking_lines.sector_id, resource_availability_daily.sector_id).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario dei settori.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(100)
			.IsRequired()
			.HasComment("Nome del settore, mostrato in tutta l'app; univoco per stabilimento (case-insensitive).");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordine di visualizzazione dei settori.");

		builder.Property(x => x.CreatedByUserPublicId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha creato/modificato i settori.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete del settore (bloccato/gestito esplicitamente se ancora referenziato da spot/prezzi/sconti).");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_map_sectors_public_id");

		// UNIQUE (property_id, lower(name)) WHERE deleted_at IS NULL: espressione lower(name) non
		// esprimibile in Fluent API, aggiunta a mano nella migration generata.

		builder.HasIndex(x => new { x.PropertyId, x.SortOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_sectors_property");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_map_sectors_property")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
