using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Domain.Entities;

public sealed record PriceListData(
	string Name, short? MaxPeople, short? MaxAdditionalItems, short? CalculationMethodId, MonthlySetting? MonthlySetting,
	WeekendSetting? WeekendSetting, SalesChannel SalesChannel, bool IsAreaPackage, bool IsCumulative, bool AllowsPublicHolidays,
	decimal? VatRate, VatNature? VatNature, bool IsSectorRestricted, bool IsMapAssignable);

public sealed record PriceListItemData(PackageItemKind Kind, long? InventoryItemId, long? IncludedSaleItemId, long? LinkedPriceListId, short Quantity);

/// <param name="StayOverrides">Prezzi corretti a mano per numero di giorni («Modifica prezzi intermedi»).</param>
public sealed record PeriodPriceData(
	long? SectorId, short? PeriodGroup, decimal? HalfDay, decimal? HalfDayWeekend, decimal? Daily, decimal? Weekend, decimal? HalfDay7,
	decimal? Days7, decimal? HalfDay14, decimal? Days14, decimal? HalfDayMonthly, decimal? Monthly, IReadOnlyDictionary<short, decimal> StayOverrides);
