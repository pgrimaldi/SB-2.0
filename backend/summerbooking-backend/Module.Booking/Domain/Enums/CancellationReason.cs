namespace Module.Booking.Domain.Enums;

/// <summary>
/// Perché una prenotazione è stata annullata: serve a riattivare solo quelle annullate per il cestino dello
/// stabilimento o per le postazioni eliminate, e a spiegare al cliente le richieste scadute o rifiutate.
/// </summary>
public enum CancellationReason
{
	Customer = 0,
	Manager = 1,
	Rejected = 2,
	PropertyTrashed = 3,
	SpotRemoved = 4,
	PaymentFailed = 5,
	ConfirmationExpired = 6
}
