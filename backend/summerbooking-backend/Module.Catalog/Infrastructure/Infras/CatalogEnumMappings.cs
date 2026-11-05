using Module.Catalog.Domain.Enums;
using Npgsql;
using Shared.Localization;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Module.Catalog.Infrastructure.Infras;

/// <summary>
/// Enum nativi del modulo, un solo elenco per runtime e design-time: sul data source e nelle opzioni EF
/// (senza la seconda EF scrive l'enum come intero). In coppia con <c>HasPostgresEnum</c> in <see cref="DbContext"/>.
/// </summary>
internal static class CatalogEnumMappings
{
	public static void Apply(NpgsqlDataSourceBuilder builder)
	{
		builder.MapEnum<PropertyStatus>("catalog.property_status");
		builder.MapEnum<PetsPolicy>("catalog.pets_policy");
		builder.MapEnum<SpotType>("catalog.spot_type");
		builder.MapEnum<MapIconState>("catalog.map_icon_state");
		builder.MapEnum<MapIconCategoryCode>("catalog.map_icon_category");
		builder.MapEnum<MediaOrigin>("catalog.media_origin");
		builder.MapEnum<MediaKind>("catalog.media_kind");
		builder.MapEnum<SurchargeType>("catalog.surcharge_type");
		builder.MapEnum<OnlineBookingScope>("catalog.online_booking_scope");
		builder.MapEnum<OnlinePaymentMethod>("catalog.online_payment_method");
		builder.MapEnum<PriceListKind>("catalog.price_list_kind");
		builder.MapEnum<MonthlySetting>("catalog.monthly_setting");
		builder.MapEnum<WeekendSetting>("catalog.weekend_setting");
		builder.MapEnum<SalesChannel>("catalog.sales_channel");
		builder.MapEnum<TariffTier>("catalog.tariff_tier");
		builder.MapEnum<PackageItemKind>("catalog.package_item_kind");
		builder.MapEnum<DatePriceKind>("catalog.date_price_kind");
		builder.MapEnum<SuggestedPriceMode>("catalog.suggested_price_mode");
		builder.MapEnum<DiscountType>("catalog.discount_type");
		builder.MapEnum<DiscountScope>("catalog.discount_scope");
		builder.MapEnum<DiscountServiceCategory>("catalog.discount_service_category");
		builder.MapEnum<DiscountApplicableChannel>("catalog.discount_applicable_channel");
		builder.MapEnum<ServiceSaleKind>("catalog.service_sale_kind");
		builder.MapEnum<Weekday>("catalog.weekday");
		builder.MapEnum<AgreementListType>("catalog.agreement_list_type");
		builder.MapEnum<AgreementReportSearchType>("catalog.agreement_report_search_type");
		builder.MapEnum<AgreementReportStatus>("catalog.agreement_report_status");
		builder.MapEnum<ConfigSnapshotKind>("catalog.config_snapshot_kind");
		builder.MapEnum<InventoryArticle>("catalog.inventory_article");
		builder.MapEnum<VatNature>("catalog.vat_nature");
		builder.MapEnum<BarCategory>("catalog.bar_category");
		builder.MapEnum<Allergen>("catalog.allergen");
		builder.MapEnum<TableCapacityMode>("catalog.table_capacity_mode");
		builder.MapEnum<DepositMode>("catalog.deposit_mode");
		builder.MapEnum<DepositType>("catalog.deposit_type");
		builder.MapEnum<AppLanguage>("catalog.content_language");
		builder.MapEnum<RevenueSeasonTier>("catalog.revenue_season_tier");
		builder.MapEnum<ShowcaseSection>("catalog.showcase_section");
	}

	public static NpgsqlDbContextOptionsBuilder Apply(NpgsqlDbContextOptionsBuilder options)
	{
		const string schema = DbContext.Schema;
		options.MapEnum<PropertyStatus>("property_status", schema);
		options.MapEnum<PetsPolicy>("pets_policy", schema);
		options.MapEnum<SpotType>("spot_type", schema);
		options.MapEnum<MapIconState>("map_icon_state", schema);
		options.MapEnum<MapIconCategoryCode>("map_icon_category", schema);
		options.MapEnum<MediaOrigin>("media_origin", schema);
		options.MapEnum<MediaKind>("media_kind", schema);
		options.MapEnum<SurchargeType>("surcharge_type", schema);
		options.MapEnum<OnlineBookingScope>("online_booking_scope", schema);
		options.MapEnum<OnlinePaymentMethod>("online_payment_method", schema);
		options.MapEnum<PriceListKind>("price_list_kind", schema);
		options.MapEnum<MonthlySetting>("monthly_setting", schema);
		options.MapEnum<WeekendSetting>("weekend_setting", schema);
		options.MapEnum<SalesChannel>("sales_channel", schema);
		options.MapEnum<TariffTier>("tariff_tier", schema);
		options.MapEnum<PackageItemKind>("package_item_kind", schema);
		options.MapEnum<DatePriceKind>("date_price_kind", schema);
		options.MapEnum<SuggestedPriceMode>("suggested_price_mode", schema);
		options.MapEnum<DiscountType>("discount_type", schema);
		options.MapEnum<DiscountScope>("discount_scope", schema);
		options.MapEnum<DiscountServiceCategory>("discount_service_category", schema);
		options.MapEnum<DiscountApplicableChannel>("discount_applicable_channel", schema);
		options.MapEnum<ServiceSaleKind>("service_sale_kind", schema);
		options.MapEnum<Weekday>("weekday", schema);
		options.MapEnum<AgreementListType>("agreement_list_type", schema);
		options.MapEnum<AgreementReportSearchType>("agreement_report_search_type", schema);
		options.MapEnum<AgreementReportStatus>("agreement_report_status", schema);
		options.MapEnum<ConfigSnapshotKind>("config_snapshot_kind", schema);
		options.MapEnum<InventoryArticle>("inventory_article", schema);
		options.MapEnum<VatNature>("vat_nature", schema);
		options.MapEnum<BarCategory>("bar_category", schema);
		options.MapEnum<Allergen>("allergen", schema);
		options.MapEnum<TableCapacityMode>("table_capacity_mode", schema);
		options.MapEnum<DepositMode>("deposit_mode", schema);
		options.MapEnum<DepositType>("deposit_type", schema);
		options.MapEnum<AppLanguage>("content_language", schema);
		options.MapEnum<RevenueSeasonTier>("revenue_season_tier", schema);
		options.MapEnum<ShowcaseSection>("showcase_section", schema);
		return options;
	}
}
