using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Un token di dispositivo registrato da un utente C5 per ricevere notifiche push, con piattaforma,
/// ultimo uso e stato.
/// </summary>
public sealed class PushDeviceToken : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid UserId { get; private set; }
	public Guid? PropertyId { get; private set; }
	public string Token { get; private set; } = null!;
	public string TokenHash { get; private set; } = null!;
	public PushPlatform Platform { get; private set; }
	public string? DeviceLabel { get; private set; }
	public string? AppVersion { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime RegisteredAt { get; private set; }
	public DateTime LastSeenAt { get; private set; }
	public string? LastError { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PushDeviceToken() { }   // unico costruttore, per il materializzatore di EF Core
}
