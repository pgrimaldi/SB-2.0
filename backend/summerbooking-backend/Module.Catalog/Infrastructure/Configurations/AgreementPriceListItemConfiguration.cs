using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AgreementPriceListItem"/> su catalog.agreement_price_list_items.</summary>
public sealed class AgreementPriceListItemConfiguration : IEntityTypeConfiguration<AgreementPriceListItem>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListItem> builder)
	{
		builder.ToTable("agreement_price_list_items", "catalog", t => t.HasComment(
			"Componente di un listino/voucher hotel: articolo di magazzino, servizio incluso o pacchetto collegato."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Listino/voucher a cui appartiene la componente.");

		builder.Property(x => x.ItemKind)
			.HasColumnName("item_kind")
			.HasColumnType("catalog.package_item_kind")
			.IsRequired()
			.HasComment("Natura della componente: articolo di magazzino, articolo d'area, servizio incluso o pacchetto collegato.");

		builder.Property(x => x.InventoryItemId)
			.HasColumnName("inventory_item_id")
			.HasComment("Articolo di magazzino incluso (valorizzato solo per item_kind inventory_item/area_item).");

		builder.Property(x => x.IncludedServiceId)
			.HasColumnName("included_service_id")
			.HasComment("Dotazione/servizio incluso.");

		builder.Property(x => x.LinkedPriceListId)
			.HasColumnName("linked_price_list_id")
			.HasColumnType("integer")
			.HasComment("Listino hotel a cui questo additional è vincolato.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Quantità dell'articolo di magazzino inclusa; 1 per servizi inclusi/pacchetti collegati.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.PriceList)
			.WithMany(pl => pl.Items)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_items_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.InventoryItemRef)
			.WithMany()
			.HasForeignKey(x => x.InventoryItemId)
			.HasConstraintName("fk_catalog_agreement_price_list_items_inventory_item")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.IncludedService)
			.WithMany()
			.HasForeignKey(x => x.IncludedServiceId)
			.HasConstraintName("fk_catalog_agreement_price_list_items_included_service")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.LinkedPriceList)
			.WithMany()
			.HasForeignKey(x => x.LinkedPriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_items_linked_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PriceListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_items_price_list");

		builder.HasIndex(x => x.InventoryItemId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_items_inventory_item");
	}
}
