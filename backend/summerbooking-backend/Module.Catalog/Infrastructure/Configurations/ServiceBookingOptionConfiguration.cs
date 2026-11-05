using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceBookingOption"/> su catalog.service_booking_options.</summary>
public sealed class ServiceBookingOptionConfiguration : IEntityTypeConfiguration<ServiceBookingOption>
{
	public void Configure(EntityTypeBuilder<ServiceBookingOption> builder)
	{
		builder.ToTable("service_booking_options", "catalog", t =>
		{
			t.HasComment("Opzioni di prenotazione di un servizio: periodo, giorni, orari da gestionale e online, capienza e limiti per prenotazione.");
			t.HasCheckConstraint("ck_catalog_service_booking_options_period", "starts_on IS NULL OR ends_on IS NULL OR ends_on >= starts_on");
			t.HasCheckConstraint("ck_catalog_service_booking_options_management", "management_from IS NULL OR management_to IS NULL OR management_to > management_from");
			t.HasCheckConstraint("ck_catalog_service_booking_options_online", "online_from IS NULL OR online_to IS NULL OR online_to > online_from");
			t.HasCheckConstraint("ck_catalog_service_booking_options_items", "min_items IS NULL OR max_items IS NULL OR max_items >= min_items");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_booking_options");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.IsRequired()
			.HasComment("Servizio.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.HasComment("Inizio del periodo in cui vale l'opzione; NULL = tutta la stagione.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.HasComment("Fine del periodo.");

		builder.Property(x => x.Weekdays)
			.HasColumnName("weekdays")
			.IsRequired()
			.HasColumnType("catalog.weekday[]")
			.HasDefaultValueSql("'{}'")
			.HasComment("Giorni in cui vale l'opzione; vuoto = tutti.");

		builder.Property(x => x.ManagementFrom)
			.HasColumnName("management_from")
			.HasColumnType("time")
			.HasComment("Orari prenotabili dal gestionale: inizio.");

		builder.Property(x => x.ManagementTo)
			.HasColumnName("management_to")
			.HasColumnType("time")
			.HasComment("Orari prenotabili dal gestionale: fine.");

		builder.Property(x => x.OnlineFrom)
			.HasColumnName("online_from")
			.HasColumnType("time")
			.HasComment("Orari prenotabili online: inizio.");

		builder.Property(x => x.OnlineTo)
			.HasColumnName("online_to")
			.HasColumnType("time")
			.HasComment("Orari prenotabili online: fine.");

		builder.Property(x => x.MaxPerHour)
			.HasColumnName("max_per_hour")
			.HasComment("Massimo di prenotazioni all'ora.");

		builder.Property(x => x.MaxPerDay)
			.HasColumnName("max_per_day")
			.HasComment("Massimo di prenotazioni al giorno.");

		builder.Property(x => x.MaxPerSlot)
			.HasColumnName("max_per_slot")
			.HasComment("Servizi orari: massimo di prenotazioni per fascia.");

		builder.Property(x => x.IntervalMinutes)
			.HasColumnName("interval_minutes")
			.HasComment("Tempo minimo fra due ordini dello stesso cliente.");

		builder.Property(x => x.MinItems)
			.HasColumnName("min_items")
			.HasComment("Minimo di articoli per prenotazione.");

		builder.Property(x => x.MaxItems)
			.HasColumnName("max_items")
			.HasComment("Massimo di articoli per prenotazione.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordine delle opzioni.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.SaleItemId, "ix_catalog_service_booking_options_item")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_booking_options_item");

		builder.HasOne<ServiceSaleItem>()
			.WithMany(p => p.BookingOptions)
			.HasForeignKey(x => x.SaleItemId)
			.HasConstraintName("fk_catalog_service_booking_options_item")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
