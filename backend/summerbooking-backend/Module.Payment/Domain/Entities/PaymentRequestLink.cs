using Module.Payment.Domain.Enums;
using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Link di pagamento Stripe inviato al cliente per saldare o confermare una prenotazione.
/// </summary>
public sealed class PaymentRequestLink : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid? ReservationPublicId { get; private set; }
	public PaymentRequestStatus Status { get; private set; }
	public PaymentRequestOrigin Origin { get; private set; }
	public decimal Amount { get; private set; }
	public string Currency { get; private set; } = null!;
	public bool CancelReservationOnExpiry { get; private set; }
	public string? StripePaymentLinkId { get; private set; }
	public long? ChargeId { get; private set; }
	public Charge? Charge { get; private set; }
	public DateTime? ExpiresAt { get; private set; }
	public DateTime? PaidAt { get; private set; }
	public DateTime? CanceledAt { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PaymentRequestLink() { }
}
