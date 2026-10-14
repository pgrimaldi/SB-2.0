namespace Module.Identity.Domain.Enums;

/// <summary>
/// Come e' nata una riga della rubrica clienti (identity_customer_customers.source_channel).
/// </summary>
public enum CustomerSource
{
	ContactForm = 0,
	Booking = 1,
	ServiceReservation = 2,
	CardTransaction = 3,
	Import = 4
}
