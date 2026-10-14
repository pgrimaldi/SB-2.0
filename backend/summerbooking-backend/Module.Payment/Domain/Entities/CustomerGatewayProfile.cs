using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Mappatura cliente finale → Stripe Customer, per connected account: consente di riusare le carte
/// salvate presso quello stabilimento.
/// </summary>
public sealed class CustomerGatewayProfile : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid CustomerPublicId { get; private set; }
	public long ConnectedAccountId { get; private set; }
	public ConnectedAccount? ConnectedAccount { get; private set; }
	public string StripeCustomerId { get; private set; } = null!;
	public string? DefaultPaymentMethodId { get; private set; }
	public DateTime? LastUsedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private CustomerGatewayProfile() { }
}
