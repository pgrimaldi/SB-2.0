namespace Module.Catalog.Domain.Enums;

/// <summary>
/// Dice se l'intervallo di date di un report alle strutture partner si riferisce agli <b>arrivi</b> o
/// alle <b>partenze</b>. Legacy: <c>hotel_reports.searchType ENUM('CHECK_IN','CHECK_OUT')</c>.
/// </summary>
public enum AgreementReportSearchType
{
	/// <summary>L'intervallo filtra le prenotazioni per data di arrivo.</summary>
	CheckIn = 0,

	/// <summary>L'intervallo filtra le prenotazioni per data di partenza.</summary>
	CheckOut = 1
}
