using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PriceListItem"/> su catalog.pricing_price_list_items.</summary>
public sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
	public void Configure(EntityTypeBuilder<PriceListItem> builder)
	{
		builder.ToTable("pricing_price_list_items", "catalog", t => t.HasComment(
			"Componente di una voce di listino: articolo incluso, servizio incluso o pacchetto vincolato."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Voce di listino a cui appartiene la componente.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.Items)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_items_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.ItemKind)
			.HasColumnName("item_kind")
			.HasColumnType("catalog.package_item_kind")
			.IsRequired()
			.HasComment("Natura della componente: determina quale colonna di riferimento è valorizzata.");

		builder.Property(x => x.InventoryItemId)
			.HasColumnName("inventory_item_id")
			.HasComment("Articolo di magazzino incluso (per inventory_item / area_item).");
		builder.HasOne<InventoryItem>()
			.WithMany()
			.HasForeignKey(x => x.InventoryItemId)
			.HasConstraintName("fk_catalog_pricing_price_list_items_inventory_item")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.IncludedServiceId)
			.HasColumnName("included_service_id")
			.HasComment("Servizio/dotazione incluso nel pacchetto (per included_service).");
		builder.HasOne<Amenity>()
			.WithMany()
			.HasForeignKey(x => x.IncludedServiceId)
			.HasConstraintName("fk_catalog_pricing_price_list_items_included_service")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.LinkedPriceListId)
			.HasColumnName("linked_price_list_id")
			.HasComment("Pacchetto a cui questo additional è vincolato (per linked_package).");
		builder.HasOne<PriceList>()
			.WithMany()
			.HasForeignKey(x => x.LinkedPriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_items_linked_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue((short)1)
			.IsRequired()
			.HasComment("Quantità dell'articolo di magazzino inclusa nel pacchetto (1 per service/linked_package).");

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");


		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della componente.");

		builder.HasIndex(x => x.PriceListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_items_package");

		builder.HasIndex(x => x.InventoryItemId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_items_inventory_item");

		builder.HasIndex(x => x.LinkedPriceListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_items_linked_package");
	}
}
