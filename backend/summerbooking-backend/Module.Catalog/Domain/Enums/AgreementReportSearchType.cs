namespace Module.Catalog.Domain.Enums;

/// <summary>
/// Dice se l'intervallo di date di un report alle strutture partner si riferisce agli <b>arrivi</b> o
/// alle <b>partenze</b>.
/// </summary>
public enum AgreementReportSearchType
{
	/// <summary>L'intervallo filtra le prenotazioni per data di arrivo.</summary>
	CheckIn = 0,

	/// <summary>L'intervallo filtra le prenotazioni per data di partenza.</summary>
	CheckOut = 1
}
