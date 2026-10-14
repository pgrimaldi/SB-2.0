namespace Module.Payment.Domain.Enums;

/// <summary>Canale da cui è partito un pagamento online (<see cref="Entities.Charge"/>).</summary>
public enum SalesChannel
{
	RedirectCheckout = 0,
	Iframe = 1
}
