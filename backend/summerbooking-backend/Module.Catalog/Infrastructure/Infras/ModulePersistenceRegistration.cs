using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Catalog.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Catalog.Infrastructure.Infras;

/// <summary>
/// Registrazione DI della persistenza del modulo Catalog, richiamata da Module.AddCatalogModule().
/// Stessa costruzione (data source + enum mapping) della DesignTimeDbContextFactory, ma con
/// AddDbContext (Scoped) per l'uso a runtime dentro Boot.
/// </summary>
public static class ModulePersistenceRegistration
{
	public static IServiceCollection AddModuleCatalogPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.Resolve(configuration));

		// Mappatura degli enum nativi PostgreSQL del modulo, in coppia con le
		// modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating (stessa lista di
		// DesignTimeDbContextFactory — non farla divergere).
		dataSourceBuilder.MapEnum<PropertyStatus>("catalog.property_status");
		dataSourceBuilder.MapEnum<PetsPolicy>("catalog.pets_policy");
		dataSourceBuilder.MapEnum<SpotType>("catalog.spot_type");
		dataSourceBuilder.MapEnum<MapIconState>("catalog.map_icon_state");
		dataSourceBuilder.MapEnum<MapIconCategoryCode>("catalog.map_icon_category");
		dataSourceBuilder.MapEnum<MediaOrigin>("catalog.media_origin");
		dataSourceBuilder.MapEnum<MediaKind>("catalog.media_kind");
		dataSourceBuilder.MapEnum<SurchargeType>("catalog.surcharge_type");
		dataSourceBuilder.MapEnum<OnlineBookingScope>("catalog.online_booking_scope");
		dataSourceBuilder.MapEnum<OnlinePaymentMethod>("catalog.online_payment_method");
		dataSourceBuilder.MapEnum<PriceListKind>("catalog.price_list_kind");
		dataSourceBuilder.MapEnum<MonthlySetting>("catalog.monthly_setting");
		dataSourceBuilder.MapEnum<WeekendSetting>("catalog.weekend_setting");
		dataSourceBuilder.MapEnum<SalesChannel>("catalog.sales_channel");
		dataSourceBuilder.MapEnum<TariffTier>("catalog.tariff_tier");
		dataSourceBuilder.MapEnum<PackageItemKind>("catalog.package_item_kind");
		dataSourceBuilder.MapEnum<DatePriceKind>("catalog.date_price_kind");
		dataSourceBuilder.MapEnum<SuggestedPriceMode>("catalog.suggested_price_mode");
		dataSourceBuilder.MapEnum<DiscountType>("catalog.discount_type");
		dataSourceBuilder.MapEnum<DiscountScope>("catalog.discount_scope");
		dataSourceBuilder.MapEnum<DiscountServiceCategory>("catalog.discount_service_category");
		dataSourceBuilder.MapEnum<DiscountApplicableChannel>("catalog.discount_applicable_channel");
		dataSourceBuilder.MapEnum<ServiceSaleKind>("catalog.service_sale_kind");
		dataSourceBuilder.MapEnum<Weekday>("catalog.weekday");
		dataSourceBuilder.MapEnum<AgreementListType>("catalog.agreement_list_type");
		dataSourceBuilder.MapEnum<AgreementReportSearchType>("catalog.agreement_report_search_type");
		dataSourceBuilder.MapEnum<AgreementReportStatus>("catalog.agreement_report_status");
		dataSourceBuilder.MapEnum<ConfigSnapshotKind>("catalog.config_snapshot_kind");

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

		// Job SCRITTO A MANO per l'unica policy il cui perimetro non e' dichiarabile: le foto degli
		// stabilimenti cessati, dove la regola e' «oltre le prime tre per sort_order, mai la copertina».
		// Il motore la salta di proposito (target_filter NULL), quindi non c'e' doppio lavoro.
		services.AddBespokeRetention<CeasedPropertyPhotoRetentionJob, DbContext>();

		return services;
	}
}
