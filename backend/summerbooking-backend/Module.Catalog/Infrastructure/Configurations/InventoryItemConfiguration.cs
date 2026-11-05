using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="InventoryItem"/> su catalog.inventory_items.</summary>
public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
	public void Configure(EntityTypeBuilder<InventoryItem> builder)
	{
		builder.ToTable("inventory_items", "catalog", t =>
		{
			t.HasComment("Articolo fisico a corredo (ombrellone, lettino, tenda, sdraio, …) di cui uno stabilimento tiene la quantità disponibile.");
			t.HasCheckConstraint("ck_catalog_inventory_items_total_quantity", "total_quantity >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_inventory_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7), copiato — Booking lo referenzia come resource_id nei consumi di magazzino.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario dell'articolo.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome dell'articolo, nella lingua del manager.");

		builder.Property(x => x.TotalQuantity)
			.HasColumnName("total_quantity")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Quantità totale disponibile dell'articolo; la disponibilità per data è total_quantity meno i consumi (Booking).");

		builder.Property(x => x.LowStockThreshold)
			.HasColumnName("low_stock_threshold")
			.HasComment("Soglia minima sotto la quale segnalare scorta bassa.");

		builder.Property(x => x.ArticleCode)
			.HasColumnName("article_code")
			.HasColumnType("catalog.inventory_article")
			.HasComment("Articolo dell'elenco fisso; NULL per gli articoli inseriti con «Altro».");

		builder.Property(x => x.LowStockAlertEnabled)
			.HasColumnName("low_stock_alert_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Avviso nella campanella e nella sezione News quando la disponibilità del giorno scende sotto la soglia.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — operatore che ha configurato l'articolo, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete; un articolo con consumi storici (Booking) non si cancella fisicamente.");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_inventory_items_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_inventory_items_public_id");

		// Univocità su lower(name): l'indice fluente è sulla colonna, l'espressione è nella migration.
		builder.HasIndex(x => new { x.PropertyId, x.Name })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_inventory_items_property_name");
	}
}
