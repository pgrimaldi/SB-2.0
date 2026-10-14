using Module.Payment.Domain.Enums;
using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Account Stripe Connect (connected account) del venditore, uno per stabilimento.
/// </summary>
public sealed class ConnectedAccount : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public string StripeAccountId { get; private set; } = null!;
	public ConnectOnboardingState OnboardingState { get; private set; }
	public bool ChargesEnabled { get; private set; }
	public bool PayoutsEnabled { get; private set; }
	public DateTime ConnectedAt { get; private set; }
	public DateTime? RevokedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ConnectedAccount() { }
}
