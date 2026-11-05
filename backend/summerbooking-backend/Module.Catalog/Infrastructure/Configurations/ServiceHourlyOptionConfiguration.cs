using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceHourlyOption"/> su catalog.service_hourly_options.</summary>
public sealed class ServiceHourlyOptionConfiguration : IEntityTypeConfiguration<ServiceHourlyOption>
{
	public void Configure(EntityTypeBuilder<ServiceHourlyOption> builder)
	{
		builder.ToTable("service_hourly_options", "catalog", t =>
		{
			t.HasComment("Disponibilità di un servizio orario: fascia del giorno, giorni, durate con prezzo, IVA e massimo per fascia. "
				+ "Appartiene ai prezzi del servizio (per periodo tariffario o senza periodo) oppure a un'opzione di prenotazione con date.");
			t.HasCheckConstraint("ck_catalog_service_hourly_options_owner", "period_group_key IS NULL OR booking_option_id IS NULL");
			t.HasCheckConstraint("ck_catalog_service_hourly_options_hours", "time_from IS NULL OR time_to IS NULL OR time_to > time_from");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_hourly_options");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico.");

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.IsRequired()
			.HasComment("Servizio orario.");

		builder.Property(x => x.PeriodGroupKey)
			.HasColumnName("period_group_key")
			.HasComment("Periodo tariffario dei prezzi (pricing_tariff_periods.period_group_key); NULL = senza periodo o legata a un'opzione di prenotazione.");

		builder.Property(x => x.BookingOptionId)
			.HasColumnName("booking_option_id")
			.HasComment("Opzione di prenotazione con date a cui appartiene; NULL = fa parte dei prezzi del servizio.");

		builder.Property(x => x.TimeFrom)
			.HasColumnName("time_from")
			.HasComment("Inizio della fascia del giorno in cui si prenota; NULL = dall'apertura.");

		builder.Property(x => x.TimeTo)
			.HasColumnName("time_to")
			.HasComment("Fine della fascia del giorno; NULL = fino alla chiusura.");

		builder.Property(x => x.Weekdays)
			.HasColumnName("weekdays")
			.IsRequired()
			.HasColumnType("catalog.weekday[]")
			.HasDefaultValueSql("'{}'")
			.HasComment("Giorni in cui vale; vuoto = tutti.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasPrecision(5, 2)
			.HasComment("Aliquota IVA in percentuale; NULL = quella del servizio.");

		builder.Property(x => x.MaxPerSlot)
			.HasColumnName("max_per_slot")
			.HasComment("Massimo di prenotazioni per fascia; NULL = senza limite.");

		builder.Property(x => x.HasFixedTimeSlots)
			.HasColumnName("has_fixed_time_slots")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Le prenotazioni partono a orari fissi, uno dopo l'altro, invece che a qualsiasi orario.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordine delle disponibilità del servizio.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_service_hourly_options_public_id");

		builder.HasIndex(x => x.SaleItemId, "ix_catalog_service_hourly_options_item")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_hourly_options_item");

		builder.HasIndex(x => x.BookingOptionId, "ix_catalog_service_hourly_options_booking_option")
			.HasFilter("booking_option_id IS NOT NULL")
			.HasDatabaseName("ix_catalog_service_hourly_options_booking_option");

		builder.HasOne<ServiceSaleItem>()
			.WithMany(x => x.HourlyOptions)
			.HasForeignKey(x => x.SaleItemId)
			.HasConstraintName("fk_catalog_service_hourly_options_item")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingOption)
			.WithOne(x => x.HourlyOption)
			.HasForeignKey<ServiceHourlyOption>(x => x.BookingOptionId)
			.HasConstraintName("fk_catalog_service_hourly_options_booking_option")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.Slots)
			.WithOne()
			.HasForeignKey(x => x.OptionId)
			.HasConstraintName("fk_catalog_service_sale_item_time_slots_option")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
