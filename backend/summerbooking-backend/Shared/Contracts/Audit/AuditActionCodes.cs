namespace Shared.Contracts.Audit;

/// <summary>Un codice azione del catalogo con la categoria in cui viene registrato.</summary>
public sealed record AuditActionDefinition(string Code, AuditCategory Category, string SourceModule);

/// <summary>
/// Catalogo dei codici azione del registro di audit, <c>&lt;modulo&gt;.&lt;azione_al_passato&gt;</c>. Un codice
/// rilasciato non si rinomina: il frontend lo traduce e ne sceglie l'icona. Ogni modulo aggiunge qui la sua classe
/// e le sue voci in <see cref="All"/>.
/// </summary>
public static class AuditActionCodes
{
	public static class Identity
	{
		public const string Module = "identity";

		/// <summary>Valori di <c>TargetType</c>: il tipo di oggetto su cui agisce l'azione.</summary>
		public static class Targets
		{
			public const string User = "user";
			public const string Customer = "customer";
			public const string ContractDocument = "contract_document";
		}

		public const string LoginSucceeded = "identity.login_succeeded";
		public const string LoginFailed = "identity.login_failed";
		public const string LoginLocked = "identity.login_locked";
		public const string LoginAppDenied = "identity.login_app_denied";
		public const string SecondFactorRequested = "identity.second_factor_requested";
		public const string SecondFactorFailed = "identity.second_factor_failed";
		public const string RefreshReuseDetected = "identity.refresh_reuse_detected";
		public const string Logout = "identity.logout";
		public const string PasswordResetRequested = "identity.password_reset_requested";
		public const string PasswordResetCompleted = "identity.password_reset_completed";
		public const string PasswordChanged = "identity.password_changed";
		public const string AccountRegistered = "identity.account_registered";
		public const string AccountDeleted = "identity.account_deleted";
		public const string SessionRevoked = "identity.session_revoked";
		public const string UserCreated = "identity.user_created";
		public const string UserUpdated = "identity.user_updated";
		public const string UserDeleted = "identity.user_deleted";
		public const string SecuritySettingsChanged = "identity.security_settings_changed";
		public const string CustomerCreated = "identity.customer_created";
		public const string CustomerUpdated = "identity.customer_updated";
		public const string CustomerDeleted = "identity.customer_deleted";
		public const string CustomersExported = "identity.customers_exported";
		public const string SubscriptionChanged = "identity.subscription_changed";
		public const string LicensePlanSaved = "identity.license_plan_saved";
		public const string ContractUploaded = "identity.contract_uploaded";
		public const string ContractPublished = "identity.contract_published";
		public const string ContractAcceptanceChanged = "identity.contract_acceptance_changed";
		public const string OwnerCreated = "identity.owner_created";
		public const string OwnerUpdated = "identity.owner_updated";
		public const string ImpersonationStarted = "identity.impersonation_started";
		public const string ImpersonationSessionOpened = "identity.impersonation_session_opened";
		public const string ExternalTokenIssued = "identity.external_token_issued";
		public const string ExternalTokenDenied = "identity.external_token_denied";
	}

	public static class Catalog
	{
		public const string Module = "catalog";

		public static class Targets
		{
			public const string InventoryItem = "inventory_item";
			public const string Season = "season";
			public const string TariffPeriod = "tariff_period";
			public const string Sector = "sector";
			public const string Map = "map";
			public const string MapSpot = "map_spot";
			public const string Property = "property";
			public const string Service = "service";
			public const string AutomaticPrice = "automatic_price";
			public const string TableService = "table_service";
			public const string PriceList = "price_list";
			public const string ReducedSeason = "reduced_season";
			public const string Discount = "discount";
			public const string AgreementPartner = "agreement_partner";
			public const string AgreementPriceList = "agreement_price_list";
			public const string AgreementReport = "agreement_report";
			public const string Revenue = "revenue";
			public const string MapIcon = "map_icon";
			public const string BeachElement = "beach_element";
			public const string Badge = "badge";
			public const string Region = "region";
			public const string Showcase = "showcase";
		}

		public const string InventoryItemCreated = "catalog.inventory_item_created";
		public const string InventoryItemUpdated = "catalog.inventory_item_updated";
		public const string InventoryItemDeleted = "catalog.inventory_item_deleted";
		public const string SeasonUpdated = "catalog.season_updated";
		public const string TariffPeriodCreated = "catalog.tariff_period_created";
		public const string TariffPeriodUpdated = "catalog.tariff_period_updated";
		public const string TariffPeriodDeleted = "catalog.tariff_period_deleted";
		public const string SectorCreated = "catalog.sector_created";
		public const string SectorUpdated = "catalog.sector_updated";
		public const string SectorDeleted = "catalog.sector_deleted";
		public const string MapSettingsUpdated = "catalog.map_settings_updated";
		public const string MapBackgroundUpdated = "catalog.map_background_updated";
		public const string MapIconsUpdated = "catalog.map_icons_updated";
		public const string MapSpotsCreated = "catalog.map_spots_created";
		public const string MapSpotUpdated = "catalog.map_spot_updated";
		public const string MapSpotsDeleted = "catalog.map_spots_deleted";
		public const string MapGridChanged = "catalog.map_grid_changed";
		public const string MapReset = "catalog.map_reset";
		public const string PropertyInfoUpdated = "catalog.property_info_updated";
		public const string PropertyAmenitiesUpdated = "catalog.property_amenities_updated";
		public const string PropertyPhotosUpdated = "catalog.property_photos_updated";
		public const string PropertyDocumentsUpdated = "catalog.property_documents_updated";
		public const string OnlineSettingsUpdated = "catalog.online_settings_updated";
		public const string ServiceOnlineSettingsUpdated = "catalog.service_online_settings_updated";
		public const string StayLimitsUpdated = "catalog.stay_limits_updated";
		public const string OnlineLegendUpdated = "catalog.online_legend_updated";
		public const string ServiceCreated = "catalog.service_created";
		public const string ServiceUpdated = "catalog.service_updated";
		public const string ServiceDeleted = "catalog.service_deleted";
		public const string AutomaticPriceCreated = "catalog.automatic_price_created";
		public const string AutomaticPriceUpdated = "catalog.automatic_price_updated";
		public const string AutomaticPriceDeleted = "catalog.automatic_price_deleted";
		public const string TableServiceCreated = "catalog.table_service_created";
		public const string TableServiceUpdated = "catalog.table_service_updated";
		public const string TableServiceDeleted = "catalog.table_service_deleted";
		public const string PriceListCreated = "catalog.price_list_created";
		public const string PriceListUpdated = "catalog.price_list_updated";
		public const string PriceListDeleted = "catalog.price_list_deleted";
		public const string PriceListDuplicated = "catalog.price_list_duplicated";
		public const string PriceListsImported = "catalog.price_lists_imported";
		public const string ReducedSeasonCreated = "catalog.reduced_season_created";
		public const string ReducedSeasonUpdated = "catalog.reduced_season_updated";
		public const string ReducedSeasonDeleted = "catalog.reduced_season_deleted";
		public const string DiscountCreated = "catalog.discount_created";
		public const string DiscountUpdated = "catalog.discount_updated";
		public const string DiscountDeleted = "catalog.discount_deleted";
		public const string AgreementPartnerCreated = "catalog.agreement_partner_created";
		public const string AgreementPartnerUpdated = "catalog.agreement_partner_updated";
		public const string AgreementPartnerDeleted = "catalog.agreement_partner_deleted";
		public const string AgreementPartnerDuplicated = "catalog.agreement_partner_duplicated";
		public const string AgreementPriceListCreated = "catalog.agreement_price_list_created";
		public const string AgreementPriceListUpdated = "catalog.agreement_price_list_updated";
		public const string AgreementPriceListDeleted = "catalog.agreement_price_list_deleted";
		public const string AgreementReportCancelled = "catalog.agreement_report_cancelled";
		public const string RevenueSettingsUpdated = "catalog.revenue_settings_updated";
		public const string SuggestedPricesComputed = "catalog.suggested_prices_computed";
		public const string SuggestedPricesApplied = "catalog.suggested_prices_applied";
		public const string RevenueWeightsUpdated = "catalog.revenue_weights_updated";
		public const string RevenueStandardPeriodsUpdated = "catalog.revenue_standard_periods_updated";
		public const string PlatformPropertyStatusChanged = "catalog.platform_property_status_changed";
		public const string PlatformPropertyTrashed = "catalog.platform_property_trashed";
		public const string PlatformPropertyRestored = "catalog.platform_property_restored";
		public const string PlatformPropertyPurged = "catalog.platform_property_purged";
		public const string PlatformPropertyShowcaseUpdated = "catalog.platform_property_showcase_updated";
		public const string PlatformMapIconSaved = "catalog.platform_map_icon_saved";
		public const string PlatformMapIconDeleted = "catalog.platform_map_icon_deleted";
		public const string PlatformBeachElementSaved = "catalog.platform_beach_element_saved";
		public const string PlatformBeachElementDeleted = "catalog.platform_beach_element_deleted";
		public const string PlatformBadgeSaved = "catalog.platform_badge_saved";
		public const string PlatformBadgeDeleted = "catalog.platform_badge_deleted";
		public const string PlatformRegionSaved = "catalog.platform_region_saved";
		public const string PlatformRegionDeleted = "catalog.platform_region_deleted";
		public const string PlatformShowcaseUpdated = "catalog.platform_showcase_updated";
		public const string ServiceDuplicated = "catalog.service_duplicated";
		public const string AutomaticPriceDuplicated = "catalog.automatic_price_duplicated";
		public const string TableServiceDuplicated = "catalog.table_service_duplicated";
	}

	public static class Audit
	{
		public const string Module = "audit";

		public const string EntriesExported = "audit.entries_exported";
		public const string PlatformEntriesExported = "audit.platform_entries_exported";
	}

	public static IReadOnlyList<AuditActionDefinition> All { get; } =
	[
		Security(Identity.LoginSucceeded),
		Security(Identity.LoginFailed),
		Security(Identity.LoginLocked),
		Security(Identity.LoginAppDenied),
		Security(Identity.SecondFactorRequested),
		Security(Identity.SecondFactorFailed),
		Security(Identity.RefreshReuseDetected),
		Security(Identity.Logout),
		Security(Identity.PasswordResetRequested),
		Security(Identity.PasswordResetCompleted),
		Security(Identity.PasswordChanged),
		Security(Identity.AccountRegistered),
		Security(Identity.AccountDeleted),
		Security(Identity.SessionRevoked),
		Security(Identity.UserCreated),
		Security(Identity.UserUpdated),
		Security(Identity.UserDeleted),
		Security(Identity.SecuritySettingsChanged),
		Security(Identity.ExternalTokenIssued),
		Security(Identity.ExternalTokenDenied),
		Operator(Identity.CustomerCreated),
		Operator(Identity.CustomerUpdated),
		Operator(Identity.CustomerDeleted),
		Operator(Identity.CustomersExported),
		Operator(Identity.ContractAcceptanceChanged),
		Platform(Identity.SubscriptionChanged),
		Platform(Identity.LicensePlanSaved),
		Platform(Identity.ContractUploaded),
		Platform(Identity.ContractPublished),
		Platform(Identity.OwnerCreated),
		Platform(Identity.OwnerUpdated),
		Platform(Identity.ImpersonationStarted),
		Platform(Identity.ImpersonationSessionOpened),
		new(Catalog.InventoryItemCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.InventoryItemUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.InventoryItemDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.SeasonUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.TariffPeriodCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.TariffPeriodUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.TariffPeriodDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.SectorCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.SectorUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.SectorDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.MapSettingsUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.MapBackgroundUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.MapIconsUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.MapSpotsCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.MapSpotUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.MapSpotsDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.MapGridChanged, AuditCategory.Operator, Catalog.Module),
		new(Catalog.MapReset, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PropertyInfoUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PropertyAmenitiesUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PropertyPhotosUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PropertyDocumentsUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.OnlineSettingsUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.ServiceOnlineSettingsUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.StayLimitsUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.OnlineLegendUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.ServiceCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.ServiceUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.ServiceDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AutomaticPriceCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AutomaticPriceUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AutomaticPriceDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.TableServiceCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.TableServiceUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.TableServiceDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PriceListCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PriceListUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PriceListDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PriceListDuplicated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PriceListsImported, AuditCategory.Operator, Catalog.Module),
		new(Catalog.ReducedSeasonCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.ReducedSeasonUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.ReducedSeasonDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.DiscountCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.DiscountUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.DiscountDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AgreementPartnerCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AgreementPartnerUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AgreementPartnerDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AgreementPartnerDuplicated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AgreementPriceListCreated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AgreementPriceListUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AgreementPriceListDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AgreementReportCancelled, AuditCategory.Operator, Catalog.Module),
		new(Catalog.RevenueSettingsUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.SuggestedPricesComputed, AuditCategory.Operator, Catalog.Module),
		new(Catalog.SuggestedPricesApplied, AuditCategory.Operator, Catalog.Module),
		new(Catalog.RevenueWeightsUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.RevenueStandardPeriodsUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformPropertyStatusChanged, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformPropertyTrashed, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformPropertyRestored, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformPropertyPurged, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformPropertyShowcaseUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformMapIconSaved, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformMapIconDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformBeachElementSaved, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformBeachElementDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformBadgeSaved, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformBadgeDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformRegionSaved, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformRegionDeleted, AuditCategory.Operator, Catalog.Module),
		new(Catalog.PlatformShowcaseUpdated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.ServiceDuplicated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.AutomaticPriceDuplicated, AuditCategory.Operator, Catalog.Module),
		new(Catalog.TableServiceDuplicated, AuditCategory.Operator, Catalog.Module),
		new(Audit.EntriesExported, AuditCategory.Operator, Audit.Module),
		new(Audit.PlatformEntriesExported, AuditCategory.Platform, Audit.Module),
	];

	private static AuditActionDefinition Security(string code) => new(code, AuditCategory.Security, Identity.Module);
	private static AuditActionDefinition Operator(string code) => new(code, AuditCategory.Operator, Identity.Module);
	private static AuditActionDefinition Platform(string code) => new(code, AuditCategory.Platform, Identity.Module);
}
