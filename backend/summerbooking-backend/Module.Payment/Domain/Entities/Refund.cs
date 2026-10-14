using Module.Payment.Domain.Enums;
using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Un rimborso Stripe su una transazione online, con supporto al rimborso parziale.
/// </summary>
public sealed class Refund : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ChargeId { get; private set; }
	public Charge? Charge { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid? ReservationPublicId { get; private set; }
	public RefundStatus Status { get; private set; }
	public RefundReason Reason { get; private set; }
	public decimal Amount { get; private set; }
	public decimal OriginalAmount { get; private set; }
	public string Currency { get; private set; } = null!;
	public string? StripeRefundId { get; private set; }
	public string? FailureReason { get; private set; }
	public Guid? RequestedByUserId { get; private set; }
	public DateTime OccurredAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private Refund() { }
}
