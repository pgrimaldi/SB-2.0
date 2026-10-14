using Module.Payment.Domain.Enums;
using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Livello gateway dell'incasso online: un PaymentIntent Stripe (o una registrazione paga-in-struttura).
/// </summary>
public sealed class Charge : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public long? ConnectedAccountId { get; private set; }
	public ConnectedAccount? ConnectedAccount { get; private set; }
	public string? StripeAccountIdSnapshot { get; private set; }
	public Guid? ReservationPublicId { get; private set; }
	public TransactionSubject? SubjectKind { get; private set; }
	public OnlinePaymentType PaymentType { get; private set; }
	public SalesChannel SalesChannel { get; private set; }
	public ChargeStatus Status { get; private set; }
	public string? StripeStatus { get; private set; }
	public string? StripePaymentIntentId { get; private set; }
	public string? StripeChargeId { get; private set; }
	/// <remarks>Colonna generata (GENERATED ALWAYS ... STORED): di sola lettura, PostgreSQL rifiuta un valore esplicito.</remarks>
	public decimal GrossAmount { get; private set; }
	public decimal PaidAmount { get; private set; }
	public decimal HoldAmount { get; private set; }
	public decimal PendingAmount { get; private set; }
	public decimal ApplicationFeeAmount { get; private set; }
	public string Currency { get; private set; } = null!;
	public Guid? CustomerPublicId { get; private set; }
	public DateTime OccurredAt { get; private set; }
	public DateTime? CapturedAt { get; private set; }
	public DateTime? CanceledAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<Refund> Refunds { get; private set; } = new List<Refund>();

	private Charge() { }
}
