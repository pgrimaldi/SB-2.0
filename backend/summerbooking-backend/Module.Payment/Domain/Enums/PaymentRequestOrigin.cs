namespace Module.Payment.Domain.Enums;

/// <summary>Da dove è stato generato un link di pagamento (<see cref="Entities.PaymentRequestLink"/>).</summary>
public enum PaymentRequestOrigin
{
	BookingDetail = 0,
	BookingCreation = 1,
	Unknown = 2
}
