using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Module.Booking.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Booking.Infrastructure;

/// <summary>
/// Factory usata da "dotnet ef" per creare il DbContext a design-time (migration add/update/...)
/// senza passare dal progetto Boot: il modulo Booking e auto-sufficiente per le migration.
/// La connection string arriva da Boot/appsettings.json/appsettings.{Environment}.json,
/// sovrascrivibile con la variabile d'ambiente ConnectionStrings__SummerBooking (BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbContext>
{
	public DbContext CreateDbContext(string[] args)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.ResolveForDesignTime());

		// Mappatura degli enum nativi PostgreSQL del modulo (dataSourceBuilder.MapEnum<TEnum>("booking...."))
		// va qui, in coppia con le modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating.
		dataSourceBuilder.MapEnum<AdditionalItemDateScope>("booking.additional_item_date_scope");
		dataSourceBuilder.MapEnum<AdditionalItemSource>("booking.additional_item_source");
		dataSourceBuilder.MapEnum<AgreementLineType>("booking.agreement_line_type");
		dataSourceBuilder.MapEnum<BookingChannel>("booking.booking_channel");
		dataSourceBuilder.MapEnum<ConsumptionSource>("booking.consumption_source");
		dataSourceBuilder.MapEnum<ConsumptionState>("booking.consumption_state");
		dataSourceBuilder.MapEnum<DayPeriod>("booking.day_period");
		dataSourceBuilder.MapEnum<DiscountChannel>("booking.discount_channel");
		dataSourceBuilder.MapEnum<DiscountOrigin>("booking.discount_origin");
		dataSourceBuilder.MapEnum<DiscountScope>("booking.discount_scope");
		dataSourceBuilder.MapEnum<DiscountType>("booking.discount_type");
		dataSourceBuilder.MapEnum<HoldKind>("booking.hold_kind");
		dataSourceBuilder.MapEnum<HoldStatus>("booking.hold_status");
		dataSourceBuilder.MapEnum<ListingType>("booking.listing_type");
		dataSourceBuilder.MapEnum<OnlineConfirmationState>("booking.online_confirmation_state");
		dataSourceBuilder.MapEnum<ParticipantRole>("booking.participant_role");
		dataSourceBuilder.MapEnum<PaymentMode>("booking.payment_mode");
		dataSourceBuilder.MapEnum<ResaleCreditCalc>("booking.resale_credit_calc");
		dataSourceBuilder.MapEnum<ResaleCreditEntryType>("booking.resale_credit_entry");
		dataSourceBuilder.MapEnum<ResaleMode>("booking.resale_mode");
		dataSourceBuilder.MapEnum<ResaleOfferState>("booking.resale_offer_state");
		dataSourceBuilder.MapEnum<ResaleOrigin>("booking.resale_origin");
		dataSourceBuilder.MapEnum<ResaleScope>("booking.resale_scope");
		dataSourceBuilder.MapEnum<ReservationKind>("booking.reservation_kind");
		dataSourceBuilder.MapEnum<ReservationPriceFlag>("booking.reservation_price_flag");
		dataSourceBuilder.MapEnum<ReservationStatus>("booking.reservation_status");
		dataSourceBuilder.MapEnum<ResourceType>("booking.resource_type");
		dataSourceBuilder.MapEnum<ServiceCategory>("booking.service_category");

		var dataSource = dataSourceBuilder.Build();

		var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
			.UseNpgsql(dataSource, npgsql => npgsql
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema)
				.CommandTimeout(ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds))
			.UseSnakeCaseNamingConvention();

		return new DbContext(optionsBuilder.Options, new SystemDateTimeProvider());
	}
}
