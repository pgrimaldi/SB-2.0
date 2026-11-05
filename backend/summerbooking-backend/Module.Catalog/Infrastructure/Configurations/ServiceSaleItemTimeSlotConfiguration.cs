using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceSaleItemTimeSlot"/> su catalog.service_sale_item_time_slots.</summary>
public sealed class ServiceSaleItemTimeSlotConfiguration : IEntityTypeConfiguration<ServiceSaleItemTimeSlot>
{
	public void Configure(EntityTypeBuilder<ServiceSaleItemTimeSlot> builder)
	{
		builder.ToTable("service_sale_item_time_slots", "catalog", t => t.HasComment(
			"Durata prenotabile di un servizio orario (kind = hourly) con il suo prezzo, dentro una disponibilità (service_hourly_options)."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_sale_item_time_slots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico, copiato cross-modulo: Booking lo usa come time_slot_id in booking_service_time_slots.");

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.IsRequired()
			.HasComment("Servizio a fascia oraria a cui appartiene la fascia.");

		builder.Property(x => x.OptionId)
			.HasColumnName("option_id")
			.HasComment("Disponibilità a cui appartiene la durata (service_hourly_options).");

		builder.Property(x => x.IsCustom)
			.HasColumnName("is_custom")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Durata personalizzata dal gestore, oltre a quelle standard.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Etichetta della fascia mostrata al cliente (es. \"30 min\", \"1 hr 30 min\").");

		builder.Property(x => x.SlotStart)
			.HasColumnName("slot_start")
			.HasComment("Orario di inizio della fascia.");

		builder.Property(x => x.SlotEnd)
			.HasColumnName("slot_end")
			.HasComment("Orario di fine della fascia.");

		builder.Property(x => x.DurationMinutes)
			.HasColumnName("duration_minutes")
			.HasComment("Durata della fascia in minuti, quando il servizio è venduto a durata invece che a "
				+ "orario fisso (slot_start/slot_end NULL). Per una fascia a orario assoluto vale il contrario: la "
				+ "durata si calcola e questa resta NULL.");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo dedicato della fascia; se NULL si usa il prezzo del servizio padre.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("La fascia è prenotabile.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordine di visualizzazione delle fasce del servizio.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della fascia.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della fascia.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_service_sale_item_time_slots_public_id");

		builder.HasIndex(x => x.OptionId, "ix_catalog_service_sale_item_time_slots_option")
			.HasFilter("option_id IS NOT NULL")
			.HasDatabaseName("ix_catalog_service_sale_item_time_slots_option");

		builder.HasIndex(x => new { x.SaleItemId, x.SortOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_sale_item_time_slots_item");

		builder.HasOne<ServiceSaleItem>()
			.WithMany(x => x.TimeSlots)
			.HasForeignKey(x => x.SaleItemId)
			.HasConstraintName("fk_catalog_service_sale_item_time_slots_item")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
