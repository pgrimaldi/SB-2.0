using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Campagna email one-shot verso un segmento: oggetto + corpo + allegati, inviata subito o pianificata.
/// </summary>
public sealed class EmailCampaign : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public string? Name { get; private set; }
	public string Subject { get; private set; } = null!;
	public long TemplateId { get; private set; }
	public MessageTemplate Template { get; private set; } = null!;
	public long? ContactListId { get; private set; }
	public ContactList? ContactList { get; private set; }
	public CampaignState State { get; private set; }
	public DateTime? ScheduledAt { get; private set; }
	public string Timezone { get; private set; } = null!;
	public bool AlsoSendPush { get; private set; }
	public bool SendCopyToManager { get; private set; }
	public int RecipientsTotal { get; private set; }
	public int DeliveredCount { get; private set; }
	public int FailedCount { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime? SentAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private EmailCampaign() { }   // unico costruttore, per il materializzatore di EF Core
}
