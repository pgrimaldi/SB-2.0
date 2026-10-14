using Module.Payment.Domain.Enums;
using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Movimento del borsellino (ricarica o consumo); append-only, il saldo del conto è la somma di queste
/// righe. Entità figlia di <see cref="WalletAccount"/> con identità pubblica propria (non owned).
/// </summary>
public sealed class WalletTransaction : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long WalletId { get; private set; }
	public WalletAccount? Wallet { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid CustomerId { get; private set; }
	public WalletMovementType MovementType { get; private set; }
	public WalletMovementSource Source { get; private set; }
	public decimal Amount { get; private set; }
	public string Currency { get; private set; } = null!;
	public decimal BalanceAfter { get; private set; }
	public Guid? ReservationPublicId { get; private set; }
	public Guid? ResaleOfferRef { get; private set; }
	public long? ReversesTransactionId { get; private set; }
	public WalletTransaction? ReversesTransaction { get; private set; }
	public Guid? LedgerEntryRef { get; private set; }
	public Guid? OperatedByUserId { get; private set; }
	public DateTime OccurredAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<WalletTransactionItem> Items { get; private set; } = new List<WalletTransactionItem>();

	private WalletTransaction() { }
}
