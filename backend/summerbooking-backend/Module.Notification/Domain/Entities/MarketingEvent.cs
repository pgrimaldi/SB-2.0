using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Evento/iniziativa di uno stabilimento (serata, torneo, offerta a tema), promuovibile in vetrina e
/// con reminder pianificati.
/// </summary>
public sealed class MarketingEvent : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public string Name { get; private set; } = null!;
	public string? Description { get; private set; }
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public TimeOnly? StartTime { get; private set; }
	public TimeOnly? EndTime { get; private set; }
	public bool IsFullDay { get; private set; }
	public EventRepeatType RepeatType { get; private set; }
	public short? RepeatInterval { get; private set; }
	public EventRepeatUnit? RepeatUnit { get; private set; }
	public DateOnly? RepeatUntil { get; private set; }
	public long? CoverImageAssetId { get; private set; }
	public MediaAsset? CoverImageAsset { get; private set; }
	public long? ContactListId { get; private set; }
	public ContactList? ContactList { get; private set; }
	public bool IsPublic { get; private set; }
	public bool RemindersEnabled { get; private set; }
	public bool IsActive { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<EventReminder> Reminders { get; private set; } = null!;

	private MarketingEvent() { }   // unico costruttore, per il materializzatore di EF Core
}
