using Module.Switchboard.Domain.Entities.Messaging;
using Module.Switchboard.Domain.Entities.Voice;
using Module.Switchboard.Domain.Enums;
using Shared.Contracts;

namespace Module.Switchboard.Domain.Entities;

/// <summary>
/// Aggregate root del dominio C7: configurazione del centralino virtuale (IVR Telnyx + inoltro SIP +
/// aggancio WhatsApp Business) di uno stabilimento. Una riga per property_id; hub da cui pendono, per
/// config_id, le tabelle figlie del modulo (registrazione numero, dispositivi softphone, voci di menù,
/// chiamate, media, template WhatsApp).
/// </summary>
public sealed class SwitchboardConfig : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public bool IsActive { get; private set; }
	public CallRoutingMode RoutingMode { get; private set; }
	public string? TelnyxPhoneE164 { get; private set; }
	public string? TelnyxConnectionId { get; private set; }
	public string? TelnyxCallControlAppId { get; private set; }
	public string? TelnyxMessagingProfileId { get; private set; }
	public string? SipEndpointId { get; private set; }
	public string? SipUsername { get; private set; }
	public string? SipPasswordSecretRef { get; private set; }
	public string? SipClientType { get; private set; }
	public string? SipClientNotes { get; private set; }
	public string? WhatsappPhoneNumberId { get; private set; }
	public string? WhatsappPhoneE164 { get; private set; }
	public long? WelcomeAudioAssetId { get; private set; }
	public long? FallbackAudioAssetId { get; private set; }
	public ForwardCallerIdMode ForwardCallerIdMode { get; private set; }
	public string? ForwardCustomCallerIdE164 { get; private set; }
	public string? ForwardCallerIdName { get; private set; }
	public short ForwardRingTimeoutSeconds { get; private set; }
	public short GatherTimeoutSeconds { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public SwitchboardMediaAsset? WelcomeAudioAsset { get; private set; }
	public SwitchboardMediaAsset? FallbackAudioAsset { get; private set; }
	public VoiceNumberRegistration? NumberRegistration { get; private set; }
	public ICollection<SwitchboardMediaAsset> MediaAssets { get; private set; } = new List<SwitchboardMediaAsset>();
	public ICollection<MessagingWaTemplate> WaTemplates { get; private set; } = new List<MessagingWaTemplate>();
	public ICollection<VoiceCallRecord> CallRecords { get; private set; } = new List<VoiceCallRecord>();
	public ICollection<VoiceMenuItem> MenuItems { get; private set; } = new List<VoiceMenuItem>();
	public ICollection<VoiceSoftphoneDevice> SoftphoneDevices { get; private set; } = new List<VoiceSoftphoneDevice>();

	private SwitchboardConfig() { }   // unico costruttore, per il materializzatore di EF Core
}
