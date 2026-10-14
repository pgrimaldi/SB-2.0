using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Booking.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Booking.Infrastructure.Infras;

/// <summary>
/// Registrazione DI della persistenza del modulo Booking, richiamata da Module.AddBookingModule().
/// Stessa costruzione (data source + enum mapping) della DesignTimeDbContextFactory, ma con
/// AddDbContext (Scoped) per l'uso a runtime dentro Boot.
/// </summary>
public static class ModulePersistenceRegistration
{
	public static IServiceCollection AddModuleBookingPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.Resolve(configuration));

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

		// Tetto del pool: ogni modulo ha il PROPRIO NpgsqlDataSource, quindi l'host ne ha
		// uno per modulo. Senza limite sarebbero 100 ciascuno (default Npgsql) contro un
		// max_connections che di norma e' 100 in tutto. Vedi ModuleConnectionStrings.
		ModuleConnectionStrings.ApplyModulePoolLimit(dataSourceBuilder, configuration);

		var dataSource = dataSourceBuilder.Build();

		services.AddDbContext<DbContext>((_, optionsBuilder) =>
			optionsBuilder.UseNpgsql(dataSource, npgsql => npgsql.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema))
			.UseSnakeCaseNamingConvention());

		// Job di retention del modulo: applica le policy di privacy_retention_policies che
		// riguardano QUESTO schema, sulla connessione di questo modulo. Chi orchestra risolve
		// IEnumerable<IModuleRetentionJob> e non conosce nessun DbContext.
		services.AddModuleRetention<DbContext>(DbContext.Schema);

		return services;
	}
}
