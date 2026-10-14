using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Module.Catalog.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Catalog.Infrastructure;

/// <summary>
/// Factory usata da "dotnet ef" per creare il DbContext a design-time (migration add/update/...)
/// senza passare dal progetto Boot: il modulo Catalog e auto-sufficiente per le migration.
/// La connection string arriva da Boot/appsettings.json/appsettings.{Environment}.json,
/// sovrascrivibile con la variabile d'ambiente ConnectionStrings__SummerBooking (BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbContext>
{
	public DbContext CreateDbContext(string[] args)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.ResolveForDesignTime());

		// Mappatura degli enum nativi PostgreSQL del modulo, in coppia con le
		// modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating (stessa lista di
		// ModulePersistenceRegistration.AddModuleCatalogPersistence).
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

		var dataSource = dataSourceBuilder.Build();

		var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
			.UseNpgsql(dataSource, npgsql => npgsql
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema)
				.CommandTimeout(ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds))
			.UseSnakeCaseNamingConvention();

		return new DbContext(optionsBuilder.Options, new SystemDateTimeProvider());
	}
}
