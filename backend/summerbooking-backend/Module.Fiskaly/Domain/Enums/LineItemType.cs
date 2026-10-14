namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Tipo di voce-prenotazione sottostante una riga di documento commerciale (fiskaly_receipt_lines.line_kind).
/// Vocabolario concettualmente condiviso con bookings.booking_element, registrato localmente in fiskaly.
/// </summary>
public enum LineItemType
{
	Package = 0,
	AdditionalItem = 1,
	BarService = 2,
	NormalService = 3,
	HourlyService = 4,
	AgreementVoucherPackage = 5,
	StandardVoucherFixed = 6,
	StandardVoucherPercentage = 7,
	SurchargeFixed = 8,
	SurchargePercentage = 9,
	PosCharge = 10
}
