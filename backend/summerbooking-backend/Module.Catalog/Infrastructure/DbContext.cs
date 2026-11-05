using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Localization;
using Shared.Infrastructure;

namespace Module.Catalog.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Catalog, schema PostgreSQL "catalog".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Catalog.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "catalog";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	// DbSet<TEntity> di ogni aggregato/entita del modulo — uno per tabella,
	// con IEntityTypeConfiguration corrispondente in Infrastructure/Configurations.
	public DbSet<Bathhouse> Bathhouses => Set<Bathhouse>();
	public DbSet<BathhouseAmenity> BathhouseAmenities => Set<BathhouseAmenity>();
	public DbSet<BathhousePhoto> BathhousePhotos => Set<BathhousePhoto>();
	public DbSet<CalculationMethod> CalculationMethods => Set<CalculationMethod>();
	public DbSet<MapIcon> MapIcons => Set<MapIcon>();
	public DbSet<MapBeachElement> MapBeachElements => Set<MapBeachElement>();
	public DbSet<MapIconCategory> MapIconCategories => Set<MapIconCategory>();
	public DbSet<MapLayout> MapLayouts => Set<MapLayout>();
	public DbSet<MapLayoutSpecialIcon> MapLayoutSpecialIcons => Set<MapLayoutSpecialIcon>();
	public DbSet<MapLegendItem> MapLegendItems => Set<MapLegendItem>();
	public DbSet<MapSector> MapSectors => Set<MapSector>();
	public DbSet<MapSpot> MapSpots => Set<MapSpot>();
	public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
	public DbSet<OnlineAllowedLocation> OnlineAllowedLocations => Set<OnlineAllowedLocation>();
	public DbSet<OnlineAllowedPeriod> OnlineAllowedPeriods => Set<OnlineAllowedPeriod>();
	public DbSet<OnlineBookingSettings> OnlineBookingSettings => Set<OnlineBookingSettings>();
	public DbSet<OnlineStayLimit> OnlineStayLimits => Set<OnlineStayLimit>();
	public DbSet<PriceList> PriceLists => Set<PriceList>();
	public DbSet<PricingSeason> PricingSeasons => Set<PricingSeason>();
	public DbSet<Property> Properties => Set<Property>();
	public DbSet<ServiceBookingSettings> ServiceBookingSettings => Set<ServiceBookingSettings>();
	public DbSet<TariffPeriod> TariffPeriods => Set<TariffPeriod>();

	// Pricing — dettaglio listino (gerarchia PriceList)
	public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
	public DbSet<PriceListSectorPrice> PriceListSectorPrices => Set<PriceListSectorPrice>();
	public DbSet<PriceListPeriodPrice> PriceListPeriodPrices => Set<PriceListPeriodPrice>();
	public DbSet<PriceListStayPrice> PriceListStayPrices => Set<PriceListStayPrice>();
	public DbSet<PriceListDatePrice> PriceListDatePrices => Set<PriceListDatePrice>();
	public DbSet<ReducedSeason> ReducedSeasons => Set<ReducedSeason>();
	public DbSet<PriceListTranslation> PriceListTranslations => Set<PriceListTranslation>();
	public DbSet<PriceListReducedPrice> PriceListReducedPrices => Set<PriceListReducedPrice>();
	public DbSet<SuggestedPrice> SuggestedPrices => Set<SuggestedPrice>();

	// Pricing — sconti e revenue management
	public DbSet<Discount> Discounts => Set<Discount>();
	public DbSet<DiscountDateRange> DiscountDateRanges => Set<DiscountDateRange>();
	public DbSet<DiscountSector> DiscountSectors => Set<DiscountSector>();
	public DbSet<DiscountTimeRange> DiscountTimeRanges => Set<DiscountTimeRange>();
	public DbSet<RevenueSettings> RevenueSettings => Set<RevenueSettings>();
	public DbSet<RevenueDiscountedSpot> RevenueDiscountedSpots => Set<RevenueDiscountedSpot>();
	public DbSet<RevenueStandardPeriod> RevenueStandardPeriods => Set<RevenueStandardPeriod>();
	public DbSet<RevenueWeights> RevenueWeights => Set<RevenueWeights>();
	public DbSet<ShowcaseRegion> ShowcaseRegions => Set<ShowcaseRegion>();
	public DbSet<ShowcaseBadge> ShowcaseBadges => Set<ShowcaseBadge>();
	public DbSet<ShowcaseEntry> ShowcaseEntries => Set<ShowcaseEntry>();

	// Service — dotazioni e servizi venduti
	public DbSet<AmenityCategory> AmenityCategories => Set<AmenityCategory>();
	public DbSet<Amenity> Amenities => Set<Amenity>();
	public DbSet<AmenityTranslation> AmenityTranslations => Set<AmenityTranslation>();
	public DbSet<ServiceTypeIcon> ServiceTypeIcons => Set<ServiceTypeIcon>();
	public DbSet<ServiceSaleItem> ServiceSaleItems => Set<ServiceSaleItem>();
	public DbSet<ServiceSaleItemTimeSlot> ServiceSaleItemTimeSlots => Set<ServiceSaleItemTimeSlot>();
	public DbSet<ServiceSaleItemVariant> ServiceSaleItemVariants => Set<ServiceSaleItemVariant>();
	public DbSet<ServiceSaleItemTranslation> ServiceSaleItemTranslations => Set<ServiceSaleItemTranslation>();
	public DbSet<ServiceBookingOption> ServiceBookingOptions => Set<ServiceBookingOption>();
	public DbSet<ServiceHourlyOption> ServiceHourlyOptions => Set<ServiceHourlyOption>();
	public DbSet<ServiceSaleItemPeriodPrice> ServiceSaleItemPeriodPrices => Set<ServiceSaleItemPeriodPrice>();
	public DbSet<ServiceSaleItemSector> ServiceSaleItemSectors => Set<ServiceSaleItemSector>();
	public DbSet<AutomaticPrice> AutomaticPrices => Set<AutomaticPrice>();
	public DbSet<AutomaticPriceTranslation> AutomaticPriceTranslations => Set<AutomaticPriceTranslation>();
	public DbSet<AutomaticPriceService> AutomaticPriceServices => Set<AutomaticPriceService>();
	public DbSet<AutomaticPricePeriod> AutomaticPricePeriods => Set<AutomaticPricePeriod>();
	public DbSet<AutomaticPriceTimeRange> AutomaticPriceTimeRanges => Set<AutomaticPriceTimeRange>();
	public DbSet<AutomaticPriceSector> AutomaticPriceSectors => Set<AutomaticPriceSector>();
	public DbSet<TableService> TableServices => Set<TableService>();
	public DbSet<TableServiceTranslation> TableServiceTranslations => Set<TableServiceTranslation>();
	public DbSet<TableServiceTableType> TableServiceTableTypes => Set<TableServiceTableType>();
	public DbSet<TableServiceAvailability> TableServiceAvailabilities => Set<TableServiceAvailability>();
	public DbSet<TableServiceSlot> TableServiceSlots => Set<TableServiceSlot>();

	// Inventory
	public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

	// Agreement — convenzioni
	public DbSet<AgreementPartner> AgreementPartners => Set<AgreementPartner>();
	public DbSet<AgreementPriceList> AgreementPriceLists => Set<AgreementPriceList>();
	public DbSet<AgreementReport> AgreementReports => Set<AgreementReport>();
	public DbSet<AgreementPriceListItem> AgreementPriceListItems => Set<AgreementPriceListItem>();
	public DbSet<AgreementPriceListSectorPrice> AgreementPriceListSectorPrices => Set<AgreementPriceListSectorPrice>();
	public DbSet<AgreementPriceListPeriodPrice> AgreementPriceListPeriodPrices => Set<AgreementPriceListPeriodPrice>();
	public DbSet<AgreementPriceListDatePrice> AgreementPriceListDatePrices => Set<AgreementPriceListDatePrice>();
	public DbSet<AgreementPriceListStayPrice> AgreementPriceListStayPrices => Set<AgreementPriceListStayPrice>();

	// Lookup di dominio
	public DbSet<Country> Countries => Set<Country>();
	public DbSet<CountryTranslation> CountryTranslations => Set<CountryTranslation>();
	public DbSet<CountryVatRate> CountryVatRates => Set<CountryVatRate>();

	// Read-model
	public DbSet<ConfigSnapshot> ConfigSnapshots => Set<ConfigSnapshot>();
	public DbSet<SearchBathhouse> SearchBathhouses => Set<SearchBathhouse>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.AddInterceptors(new AuditInterceptor(_dateTimeProvider), new SoftDeleteInterceptor(_dateTimeProvider));
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		ModuleDbContextConventions.ConfigureUtcDateTimes(configurationBuilder);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema(Schema);

		// Enum nativi PostgreSQL del modulo. Stessa lista (nome C# + label PG) di DesignTimeDbContextFactory e di
		// ModulePersistenceRegistration.AddModuleCatalogPersistence — non farla divergere.
		modelBuilder.HasPostgresEnum<PropertyStatus>(Schema, "property_status");
		modelBuilder.HasPostgresEnum<PetsPolicy>(Schema, "pets_policy");
		modelBuilder.HasPostgresEnum<SpotType>(Schema, "spot_type");
		modelBuilder.HasPostgresEnum<MapIconState>(Schema, "map_icon_state");
		modelBuilder.HasPostgresEnum<MapIconCategoryCode>(Schema, "map_icon_category");
		modelBuilder.HasPostgresEnum<MediaOrigin>(Schema, "media_origin");
		modelBuilder.HasPostgresEnum<MediaKind>(Schema, "media_kind");
		modelBuilder.HasPostgresEnum<SurchargeType>(Schema, "surcharge_type");
		modelBuilder.HasPostgresEnum<OnlineBookingScope>(Schema, "online_booking_scope");
		modelBuilder.HasPostgresEnum<OnlinePaymentMethod>(Schema, "online_payment_method");
		modelBuilder.HasPostgresEnum<PriceListKind>(Schema, "price_list_kind");
		modelBuilder.HasPostgresEnum<MonthlySetting>(Schema, "monthly_setting");
		modelBuilder.HasPostgresEnum<WeekendSetting>(Schema, "weekend_setting");
		modelBuilder.HasPostgresEnum<SalesChannel>(Schema, "sales_channel");
		modelBuilder.HasPostgresEnum<TariffTier>(Schema, "tariff_tier");
		modelBuilder.HasPostgresEnum<PackageItemKind>(Schema, "package_item_kind");
		modelBuilder.HasPostgresEnum<DatePriceKind>(Schema, "date_price_kind");
		modelBuilder.HasPostgresEnum<SuggestedPriceMode>(Schema, "suggested_price_mode");
		modelBuilder.HasPostgresEnum<DiscountType>(Schema, "discount_type");
		modelBuilder.HasPostgresEnum<DiscountScope>(Schema, "discount_scope");
		modelBuilder.HasPostgresEnum<DiscountServiceCategory>(Schema, "discount_service_category");
		modelBuilder.HasPostgresEnum<DiscountApplicableChannel>(Schema, "discount_applicable_channel");
		modelBuilder.HasPostgresEnum<ServiceSaleKind>(Schema, "service_sale_kind");
		modelBuilder.HasPostgresEnum<Weekday>(Schema, "weekday");
		modelBuilder.HasPostgresEnum<AgreementListType>(Schema, "agreement_list_type");
		modelBuilder.HasPostgresEnum<AgreementReportSearchType>(Schema, "agreement_report_search_type");
		modelBuilder.HasPostgresEnum<AgreementReportStatus>(Schema, "agreement_report_status");
		modelBuilder.HasPostgresEnum<ConfigSnapshotKind>(Schema, "config_snapshot_kind");
		modelBuilder.HasPostgresEnum<InventoryArticle>(Schema, "inventory_article");
		modelBuilder.HasPostgresEnum<VatNature>(Schema, "vat_nature");
		modelBuilder.HasPostgresEnum<BarCategory>(Schema, "bar_category");
		modelBuilder.HasPostgresEnum<Allergen>(Schema, "allergen");
		modelBuilder.HasPostgresEnum<TableCapacityMode>(Schema, "table_capacity_mode");
		modelBuilder.HasPostgresEnum<DepositMode>(Schema, "deposit_mode");
		modelBuilder.HasPostgresEnum<DepositType>(Schema, "deposit_type");
		modelBuilder.HasPostgresEnum<AppLanguage>(Schema, "content_language");
		modelBuilder.HasPostgresEnum<RevenueSeasonTier>(Schema, "revenue_season_tier");
		modelBuilder.HasPostgresEnum<ShowcaseSection>(Schema, "showcase_section");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
