namespace Module.Booking.Domain.Enums;

/// <summary>
/// Stato del consumo, indipendente dal pagamento.
/// </summary>
public enum ConsumptionState
{
	Held = 0,
	Confirmed = 1,
	Released = 2
}
