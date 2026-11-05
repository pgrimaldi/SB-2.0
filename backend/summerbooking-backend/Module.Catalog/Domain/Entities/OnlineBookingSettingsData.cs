using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Domain.Entities;

public sealed record OnlineBookingSettingsData(
	bool IsEnabled, bool IsMapPick, bool IsLocationRestricted, bool IsPeriodRestricted, SurchargeType SurchargeType,
	decimal SurchargeValue, bool IsBlocked, bool RequiresManualConfirmation, bool AllowsSameDayBooking, bool AllowsHalfDay,
	TimeOnly? MorningFrom, TimeOnly? MorningTo, TimeOnly? AfternoonFrom, TimeOnly? AfternoonTo, TimeOnly? AfternoonConversionTime,
	bool AllowsSeasonal, bool AllowsSharing, short? MaxSpotsPerBooking, short? MaxBookingsPerDay, short? BookingHorizonDays,
	OnlineBookingScope Scope, IReadOnlyList<OnlinePaymentMethod> PaymentMethods, string? UsefulInfo, string? GoodToKnow);

public sealed record ServiceBookingSettingsData(
	bool IsEnabled, SurchargeType SurchargeType, decimal SurchargeValue, bool RequiresManualConfirmation, string? SectionTitle,
	string? UsefulInfo, IReadOnlyList<OnlinePaymentMethod> PaymentMethods);
