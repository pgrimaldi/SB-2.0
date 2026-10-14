using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceSaleItem"/> su catalog.service_sale_items.</summary>
public sealed class ServiceSaleItemConfiguration : IEntityTypeConfiguration<ServiceSaleItem>
{
	public void Configure(EntityTypeBuilder<ServiceSaleItem> builder)
	{
		builder.ToTable("service_sale_items", "catalog", t => t.HasComment(
			"Servizio (bar/normale/a fascia oraria) venduto da uno stabilimento a corredo della prenotazione."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_sale_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico, copiato cross-modulo: C1 lo usa per contatori/consumi del servizio.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento che vende il servizio.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("catalog.service_sale_kind")
			.IsRequired()
			.HasComment("Tipo di servizio: bar, normale, a fascia oraria.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome del servizio mostrato al cliente.");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo unitario del servizio (sovrascrivibile dalla fascia oraria per kind = hourly).");

		builder.Property(x => x.IconId)
			.HasColumnName("icon_id")
			.HasComment("Icona scelta dal catalogo master; alternativa a icon_asset_id.");

		builder.Property(x => x.IconAssetId)
			.HasColumnName("icon_asset_id")
			.HasComment("Icona caricata ad hoc; alternativa a icon_id.");

		builder.Property(x => x.IsPurchasable)
			.HasColumnName("is_purchasable")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Il servizio è acquistabile (vs. solo elencato/disattivato).");

		builder.Property(x => x.ActiveWeekdays)
			.HasColumnName("active_weekdays")
			.HasColumnType("catalog.weekday[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Giorni della settimana in cui il servizio e' attivo. Sostituisce la tabella " +
				"figlia a sette righe con flag: insieme chiuso, piccolo, sempre letto e scritto " +
				"insieme al padre. Array vuoto = servizio mai prenotabile.");

		builder.Property(x => x.HourFrom)
			.HasColumnName("hour_from")
			.HasComment("Inizio della fascia oraria complessiva di disponibilità (solo kind = hourly).");

		builder.Property(x => x.HourTo)
			.HasColumnName("hour_to")
			.HasComment("Fine della fascia oraria complessiva di disponibilità (solo kind = hourly).");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access_users (C5), copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione del servizio.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica del servizio.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete; un servizio con consumi storici (C1) non si cancella fisicamente.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_service_sale_items_public_id");

		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => new { x.PropertyId, x.Kind }, "ix_catalog_service_sale_items_property_kind")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_sale_items_property_kind");

		builder.HasIndex(x => new { x.PropertyId, x.Kind }, "ix_catalog_service_sale_items_property_purchasable")
			.HasFilter("is_purchasable AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_sale_items_property_purchasable");

		builder.HasOne<Property>()
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_service_sale_items_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<ServiceTypeIcon>()
			.WithMany()
			.HasForeignKey(x => x.IconId)
			.HasConstraintName("fk_catalog_service_sale_items_icon")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne<MediaAsset>()
			.WithMany()
			.HasForeignKey(x => x.IconAssetId)
			.HasConstraintName("fk_catalog_service_sale_items_icon_asset")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.TimeSlots)
			.WithOne()
			.HasForeignKey(x => x.SaleItemId)
			.HasConstraintName("fk_catalog_service_sale_item_time_slots_item")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
