using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Preferenze di notifica di un utente C5 (staff/gestore): canali abilitati, non disturbare, tipi
/// silenziati. Assenza di riga = valgono i default di modulo.
/// </summary>
public sealed class UserChannelPreferences : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid UserId { get; private set; }
	public Guid? PropertyId { get; private set; }
	public List<StaffChannel> EnabledChannels { get; private set; } = null!;
	public bool IsMuted { get; private set; }
	public List<NotificationKind> MutedKinds { get; private set; } = null!;
	public TimeOnly? QuietHoursStart { get; private set; }
	public TimeOnly? QuietHoursEnd { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private UserChannelPreferences() { }   // unico costruttore, per il materializzatore di EF Core
}
