namespace Module.Catalog.Domain.Entities;

public sealed record AgreementPartnerData(
	string Name, string? City, string? PostalCode, string? Province, string? StreetName, string? StreetNumber, string? Phone,
	string? Email, string? Note);

public sealed record AgreementPriceListData(
	string Name, short? MaxPeople, short? CalculationMethodId, Enums.MonthlySetting? MonthlySetting, Enums.WeekendSetting? WeekendSetting,
	bool AllowsPublicHolidays, short? MinBookingDays, short? MaxVoucherQuantity, bool IsCumulative, decimal? VatRate, Enums.VatNature? VatNature);
