using Module.Switchboard.Domain.Enums.Voice;
using Shared.Contracts;

namespace Module.Switchboard.Domain.Entities.Voice;

/// <summary>
/// Dispositivo softphone registrato per ricevere il push wake prima dell'inoltro di una chiamata al
/// centralino. N dispositivi per centralino (stesso criterio di notification_push_device_tokens).
/// </summary>
public sealed class VoiceSoftphoneDevice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ConfigId { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid? RegisteredByUserId { get; private set; }
	public SoftphonePushPlatform Platform { get; private set; }
	public string Token { get; private set; } = null!;
	public string TokenHash { get; private set; } = null!;
	public string? DeviceLabel { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime RegisteredAt { get; private set; }
	public DateTime LastSeenAt { get; private set; }
	public string? LastError { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public SwitchboardConfig Config { get; private set; } = null!;

	private VoiceSoftphoneDevice() { }   // unico costruttore, per il materializzatore di EF Core
}
