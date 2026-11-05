namespace Module.Catalog.Domain;

/// <summary>Codici d'errore del modulo; il frontend li traduce. Un codice rilasciato non si rinomina.</summary>
public static class CatalogErrors
{
	public const string PropertyRequired = "catalog.property_required";
	public const string PropertyNotFound = "catalog.property_not_found";
	public const string InvalidImage = "catalog.invalid_image";
	public const string InvalidPdf = "catalog.invalid_pdf";
	public const string CountryNotFound = "catalog.country_not_found";
	public const string SlugTaken = "catalog.slug_taken";
	public const string AmenityNotFound = "catalog.amenity_not_found";
	public const string PhotoNotFound = "catalog.photo_not_found";
	public const string GalleryFull = "catalog.gallery_full";
	public const string HumanVerificationFailed = "catalog.human_verification_failed";
	public const string TooManyFiles = "catalog.too_many_files";
	public const string StayLimitNotFound = "catalog.stay_limit_not_found";
	public const string StayLimitOverlap = "catalog.stay_limit_overlap";
	public const string VatRateNotAllowed = "catalog.vat_rate_not_allowed";

	public const string ServiceNotFound = "service.not_found";
	public const string ServiceKindImmutable = "service.kind_immutable";
	public const string ServiceIconNotFound = "service.icon_not_found";
	public const string AutomaticPriceNotFound = "service.automatic_price_not_found";
	public const string TableServiceNotFound = "service.table_service_not_found";

	public const string InventoryItemNotFound = "inventory.item_not_found";
	public const string InventoryNameTaken = "inventory.name_taken";
	public const string InventoryItemInUse = "inventory.item_in_use";

	public const string SeasonRequired = "pricing.season_required";
	public const string NextSeasonRequired = "pricing.next_season_required";
	public const string PeriodNotFound = "pricing.period_not_found";
	public const string PeriodNameTaken = "pricing.period_name_taken";
	public const string PeriodOutsideSeason = "pricing.period_outside_season";
	public const string PeriodInUse = "pricing.period_in_use";
	public const string PriceListNotFound = "pricing.price_list_not_found";
	public const string PriceListKindImmutable = "pricing.price_list_kind_immutable";
	public const string CalculationMethodNotFound = "pricing.calculation_method_not_found";
	public const string ReducedSeasonNotFound = "pricing.reduced_season_not_found";
	public const string ImportFileInvalid = "pricing.import_file_invalid";
	public const string ImportPriceInvalid = "pricing.import_price_invalid";
	public const string DiscountNotFound = "pricing.discount_not_found";
	public const string DiscountCodeTaken = "pricing.discount_code_taken";
	public const string DiscountMaxUsesBelowUsed = "pricing.discount_max_uses_below_used";

	public const string BathhouseNotFound = "catalog.bathhouse_not_found";
	public const string PropertyNotInTrash = "catalog.property_not_in_trash";
	public const string PropertyInTrash = "catalog.property_in_trash";
	public const string BadgeNotFound = "catalog.badge_not_found";
	public const string RegionNotFound = "catalog.region_not_found";
	public const string RegionNameTaken = "catalog.region_name_taken";
	public const string MapIconIsDefault = "map.icon_is_default";
	public const string MapIconDefaultRequired = "map.icon_default_required";
	public const string MapIconCategoryNotFound = "map.icon_category_not_found";
	public const string MapBeachElementInUse = "map.beach_element_in_use";

	public const string QuoteOnlineUnavailable = "pricing.online_booking_unavailable";
	public const string QuotePackageNotOnline = "pricing.package_not_online";
	public const string QuotePackageNotForSector = "pricing.package_not_for_sector";
	public const string QuoteNotAPackage = "pricing.not_a_package";
	public const string QuoteNotAnAdditional = "pricing.not_an_additional";
	public const string QuoteAdditionalNotLinked = "pricing.additional_not_linked";
	public const string QuoteTooManyAdditionals = "pricing.too_many_additionals";
	public const string QuoteHalfDayNotAllowed = "pricing.half_day_not_allowed";
	public const string QuoteSeasonalNotAllowed = "pricing.seasonal_not_allowed";
	public const string QuotePastDates = "pricing.past_dates";
	public const string QuoteSameDayNotAllowed = "pricing.same_day_not_allowed";
	public const string QuoteBeyondHorizon = "pricing.beyond_booking_horizon";
	public const string QuoteDatesNotAllowed = "pricing.dates_not_allowed";
	public const string QuoteSpotNotOnline = "pricing.spot_not_online";
	public const string QuoteStayLimit = "pricing.stay_limit";
	public const string QuoteManualPriceNotAllowed = "pricing.manual_price_not_allowed";
	public const string QuoteServiceNotOnline = "pricing.service_not_online";
	public const string QuoteVariantNotFound = "pricing.variant_not_found";
	public const string QuoteTimeSlotNotFound = "pricing.time_slot_not_found";
	public const string QuoteDiscountNotValid = "pricing.discount_not_valid";
	public const string QuoteVoucherNotFound = "pricing.voucher_not_found";

	public const string SnapshotNotFound = "catalog.snapshot_not_found";

	public const string SuggestedPriceNotFound = "pricing.suggested_price_not_found";

	public const string AgreementPartnerNotFound = "agreement.partner_not_found";
	public const string AgreementReportNotFound = "agreement.report_not_found";

	public const string SectorNotFound = "catalog.sector_not_found";
	public const string SectorNameTaken = "catalog.sector_name_taken";
	public const string SectorInUse = "catalog.sector_in_use";

	public const string MapSpotNotFound = "map.spot_not_found";
	public const string MapCellOutsideGrid = "map.cell_outside_grid";
	public const string MapCellOccupied = "map.cell_occupied";
	public const string MapLabelTaken = "map.label_taken";
	public const string MapSpotsOutsideGrid = "map.spots_outside_grid";
	public const string MapIndexOutsideGrid = "map.index_outside_grid";
	public const string MapGridFull = "map.grid_full";
	public const string MapSelectionNotRectangular = "map.selection_not_rectangular";
	public const string MapBeachElementNotEditable = "map.beach_element_not_editable";
	public const string MapIconNotFound = "map.icon_not_found";
	public const string MapIconNotAllowed = "map.icon_not_allowed";
	public const string MapBeachElementNotFound = "map.beach_element_not_found";
	public const string MapInvalidImage = "map.invalid_image";
}
