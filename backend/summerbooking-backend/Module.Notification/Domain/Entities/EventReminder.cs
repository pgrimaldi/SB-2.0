using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Reminder pianificato di un evento marketing, inviato ai contatti della lista dell'evento allo
/// scadere. Figlia di <see cref="MarketingEvent"/>.
/// </summary>
public sealed class EventReminder : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long EventId { get; private set; }
	public MarketingEvent Event { get; private set; } = null!;
	public Guid PropertyId { get; private set; }
	public EventReminderOffset Offset { get; private set; }
	public DateOnly? CustomDate { get; private set; }
	public TimeOnly? CustomTime { get; private set; }
	public DateTime? ScheduledAt { get; private set; }
	public ReminderStatus Status { get; private set; }
	public DateTime? SentAt { get; private set; }
	public string? FailureReason { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private EventReminder() { }   // unico costruttore, per il materializzatore di EF Core
}
