using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AutomaticPrice"/> su catalog.automatic_prices.</summary>
public sealed class AutomaticPriceConfiguration : IEntityTypeConfiguration<AutomaticPrice>
{
	public void Configure(EntityTypeBuilder<AutomaticPrice> builder)
	{
		builder.ToTable("automatic_prices", "catalog", t =>
		{
			t.HasComment("Prezzi automatici (es. Coperto): aggiunti da soli agli ordini dei servizi indicati nei periodi, giorni e fasce di validità.");
			t.HasCheckConstraint("ck_catalog_automatic_prices_vat", "(vat_rate IS NULL) <> (vat_nature IS NULL)");
			t.HasCheckConstraint("ck_catalog_automatic_prices_price", "price >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_automatic_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7) usato dalle API.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_automatic_prices_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(255)
			.HasComment("Nome mostrato al cliente.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasColumnType("text")
			.HasComment("Descrizione.");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.IsRequired()
			.HasPrecision(12, 2)
			.HasComment("Prezzo aggiunto.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasPrecision(5, 2)
			.HasComment("Aliquota IVA in percentuale; NULL = esente (vat_nature).");

		builder.Property(x => x.VatNature)
			.HasColumnName("vat_nature")
			.HasColumnType("catalog.vat_nature")
			.HasComment("Natura dell'esenzione IVA.");

		builder.Property(x => x.IconId)
			.HasColumnName("icon_id")
			.HasComment("Icona del catalogo (service_type_icons).");

		builder.Property(x => x.IconAssetId)
			.HasColumnName("icon_asset_id")
			.HasComment("Immagine caricata dal gestore.");

		builder.Property(x => x.IsPurchasable)
			.HasColumnName("is_purchasable")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Vale anche per gli ordini online.");

		builder.Property(x => x.AppliesPerService)
			.HasColumnName("applies_per_service")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("true = per ogni servizio dell'ordine; false = una volta per ordine.");

		builder.Property(x => x.AppliesToAllServices)
			.HasColumnName("applies_to_all_services")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("true = tutti i servizi; false = solo quelli di automatic_price_services.");

		builder.Property(x => x.IsSectorRestricted)
			.HasColumnName("is_sector_restricted")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Vale solo nelle file/settori di automatic_price_sectors.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.PropertyId, "ix_catalog_automatic_prices_property")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_automatic_prices_property");

		builder.HasOne<Property>()
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_automatic_prices_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<ServiceTypeIcon>()
			.WithMany()
			.HasForeignKey(x => x.IconId)
			.HasConstraintName("fk_catalog_automatic_prices_icon")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne<MediaAsset>()
			.WithMany()
			.HasForeignKey(x => x.IconAssetId)
			.HasConstraintName("fk_catalog_automatic_prices_icon_asset")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
