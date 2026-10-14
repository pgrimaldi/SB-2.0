using Module.Payment.Domain.Enums;
using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Conto prepagato del cliente presso uno stabilimento: tessera fisica ricaricabile o crediti da
/// cessione posto.
/// </summary>
public sealed class WalletAccount : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid CustomerId { get; private set; }
	public WalletKind WalletKind { get; private set; }
	public string? CardNumber { get; private set; }
	public decimal Balance { get; private set; }
	public decimal LifetimeCredited { get; private set; }
	public string Currency { get; private set; } = null!;
	public WalletStatus Status { get; private set; }
	public DateTime OpenedAt { get; private set; }
	public DateTime? LastMovementAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<WalletTransaction> Transactions { get; private set; } = new List<WalletTransaction>();

	private WalletAccount() { }
}
