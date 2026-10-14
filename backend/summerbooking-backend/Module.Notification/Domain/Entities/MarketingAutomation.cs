using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Automazione marketing ricorrente: email (+ push opzionale) inviata ai contatti di una lista in
/// relazione alle loro prenotazioni.
/// </summary>
public sealed class MarketingAutomation : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public string? Name { get; private set; }
	public AutomationKind AutomationKind { get; private set; }
	public AutomationState State { get; private set; }
	public AutomationScheduleType ScheduleType { get; private set; }
	public short? DaysOffset { get; private set; }
	public TimeOnly? SendAtTime { get; private set; }
	public string Timezone { get; private set; } = null!;
	public string Subject { get; private set; } = null!;
	public long TemplateId { get; private set; }
	public MessageTemplate Template { get; private set; } = null!;
	public long? ContactListId { get; private set; }
	public ContactList? ContactList { get; private set; }
	public bool AlsoSendPush { get; private set; }
	public DateTime? LastRunAt { get; private set; }
	public int TotalSent { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<AutomationTrigger> Triggers { get; private set; } = null!;

	private MarketingAutomation() { }   // unico costruttore, per il materializzatore di EF Core
}
