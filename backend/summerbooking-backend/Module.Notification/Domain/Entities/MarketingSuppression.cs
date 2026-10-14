using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Lista di soppressione (opt-out) marketing: indirizzi da non ricontattare, prova della revoca del
/// consenso. Non ha retention: conservata a tempo indeterminato.
/// </summary>
public sealed class MarketingSuppression : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid? PropertyId { get; private set; }
	public string Address { get; private set; } = null!;
	public ChannelOrAll Channel { get; private set; }
	public SuppressionReason Reason { get; private set; }
	public Guid? SourceDeliveryPublicId { get; private set; }
	public Guid? ContactPublicId { get; private set; }
	public string? Note { get; private set; }
	public DateTime SuppressedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MarketingSuppression() { }   // unico costruttore, per il materializzatore di EF Core
}
