using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Trigger custom di un'automazione marketing: evento prenotazione che fa scattare l'invio. Figlia di
/// <see cref="MarketingAutomation"/>.
/// </summary>
public sealed class AutomationTrigger : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long AutomationId { get; private set; }
	public MarketingAutomation Automation { get; private set; } = null!;
	public AutomationTriggerKind TriggerKind { get; private set; }
	public short? DaysOffset { get; private set; }
	public TimeOnly? SendAtTime { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AutomationTrigger() { }   // unico costruttore, per il materializzatore di EF Core
}
