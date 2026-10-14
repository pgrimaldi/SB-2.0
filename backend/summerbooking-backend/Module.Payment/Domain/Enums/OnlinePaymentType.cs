namespace Module.Payment.Domain.Enums;

/// <summary>Modalità di pagamento online scelta per una transazione (<see cref="Entities.Charge"/>).</summary>
public enum OnlinePaymentType
{
	PayNow = 0,
	AdvanceBalance = 1,
	PayAtProperty = 2
}
