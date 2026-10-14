using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Destinatario di un invio marketing (campagna o automazione): snapshot al momento dell'invio, con
/// esito per indirizzo. ParentId/ParentKind sono un riferimento polimorfico senza FK EF (integrità
/// applicata da trigger DB, vedi Configuration).
/// </summary>
public sealed class CampaignRecipient : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public MarketingParentKind ParentKind { get; private set; }
	public long ParentId { get; private set; }
	public Guid PropertyId { get; private set; }
	public long? ContactId { get; private set; }
	public MarketingContact? Contact { get; private set; }
	public string EmailSnapshot { get; private set; } = null!;
	public string? FullNameSnapshot { get; private set; }
	public RecipientStatus Status { get; private set; }
	public Guid? DeliveryPublicId { get; private set; }
	public RecipientSkipReason? SkipReason { get; private set; }
	public DateTime? SentAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private CampaignRecipient() { }   // unico costruttore, per il materializzatore di EF Core
}
