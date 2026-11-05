namespace Module.Catalog.Application.Dtos;

/// <summary>Impostazioni › Opzioni prenotazioni online (postazioni).</summary>
/// <param name="IsSuspendedByPlatform">Vendita online sospesa dal systemadmin o per abbonamento: sola lettura.</param>
/// <param name="IsBlocked">«Blocca prenotazioni online» del gestore.</param>
/// <param name="SurchargeType">disable, percentage, fixed.</param>
/// <param name="Scope">all, contacts_only («Consenti prenotazioni online a»).</param>
/// <param name="PaymentMethods">pay_now, pay_at_property, pay_at_property_no_card, deposit_half, pay_at_property_prepaid.</param>
/// <param name="AllowedSpotIds">Postazioni prenotabili online, se <paramref name="IsLocationRestricted"/>.</param>
/// <param name="AllowedPeriods">Periodi prenotabili online, se <paramref name="IsPeriodRestricted"/>.</param>
public sealed record OnlineSettingsDto(
	bool IsEnabled, bool IsSuspendedByPlatform, bool IsMapPick, bool IsLocationRestricted, IReadOnlyList<Guid> AllowedSpotIds,
	bool IsPeriodRestricted, IReadOnlyList<DateRangeDto> AllowedPeriods, string SurchargeType, decimal SurchargeValue, bool IsBlocked,
	bool RequiresManualConfirmation, bool AllowsSameDayBooking, bool AllowsHalfDay, TimeOnly? MorningFrom, TimeOnly? MorningTo,
	TimeOnly? AfternoonFrom, TimeOnly? AfternoonTo, TimeOnly? AfternoonConversionTime, bool AllowsSeasonal, bool AllowsSharing,
	short? MaxSpotsPerBooking, short? MaxBookingsPerDay, short? BookingHorizonDays, string Scope, IReadOnlyList<string> PaymentMethods,
	string? UsefulInfo, string? GoodToKnow);

public sealed record SaveOnlineSettingsRequest(
	bool IsEnabled, bool IsMapPick, bool IsLocationRestricted, IReadOnlyList<Guid>? AllowedSpotIds, bool IsPeriodRestricted,
	IReadOnlyList<DateRangeDto>? AllowedPeriods, string SurchargeType, decimal SurchargeValue, bool IsBlocked, bool RequiresManualConfirmation,
	bool AllowsSameDayBooking, bool AllowsHalfDay, TimeOnly? MorningFrom, TimeOnly? MorningTo, TimeOnly? AfternoonFrom, TimeOnly? AfternoonTo,
	TimeOnly? AfternoonConversionTime, bool AllowsSeasonal, bool AllowsSharing, short? MaxSpotsPerBooking, short? MaxBookingsPerDay,
	short? BookingHorizonDays, string Scope, IReadOnlyList<string> PaymentMethods, string? UsefulInfo, string? GoodToKnow);

/// <summary>Opzioni della prenotazione online dei servizi.</summary>
/// <param name="SectionTitle">Titolo del pannello dei servizi sul sito.</param>
public sealed record ServiceOnlineSettingsDto(
	bool IsEnabled, string SurchargeType, decimal SurchargeValue, bool RequiresManualConfirmation, string? SectionTitle, string? UsefulInfo,
	IReadOnlyList<string> PaymentMethods);

public sealed record SaveServiceOnlineSettingsRequest(
	bool IsEnabled, string SurchargeType, decimal SurchargeValue, bool RequiresManualConfirmation, string? SectionTitle, string? UsefulInfo,
	IReadOnlyList<string> PaymentMethods);

/// <summary>Limite di giorni prenotabili online per fila/settore e periodo (almeno uno fra minimo e massimo).</summary>
public sealed record StayLimitDto(Guid Id, Guid SectorId, DateOnly StartsOn, DateOnly EndsOn, short? MinDays, short? MaxDays);

public sealed record SaveStayLimitRequest(Guid SectorId, DateOnly StartsOn, DateOnly EndsOn, short? MinDays, short? MaxDays);

/// <param name="IconId">Icona del catalogo della mappa (<c>GET /map/icon-catalog</c>).</param>
public sealed record LegendItemDto(string Label, Guid? IconId, string? IconUrl);

public sealed record SaveLegendItem(string Label, Guid? IconId);

public sealed record SaveLegendRequest(IReadOnlyList<SaveLegendItem> Items);
