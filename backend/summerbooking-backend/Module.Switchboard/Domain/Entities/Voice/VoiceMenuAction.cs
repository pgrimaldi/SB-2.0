using Module.Switchboard.Domain.Entities.Messaging;
using Module.Switchboard.Domain.Enums.Voice;
using Shared.Contracts;

namespace Module.Switchboard.Domain.Entities.Voice;

/// <summary>
/// Azione eseguita quando il chiamante seleziona una voce di menù dell'IVR, eseguita in sequenza per
/// execution_order. Un'azione terminale (hangup, forward_sip) interrompe il ciclo, send_whatsapp no.
/// </summary>
public sealed class VoiceMenuAction : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long MenuItemId { get; private set; }
	public Guid PropertyId { get; private set; }
	public MenuActionType ActionType { get; private set; }
	public int ExecutionOrder { get; private set; }
	public long? WaTemplateId { get; private set; }
	public string? WaTemplateNameSnapshot { get; private set; }
	public string? WaLanguage { get; private set; }
	public WaRecipientMode? WaRecipientMode { get; private set; }
	public string? WaRecipientPhoneE164 { get; private set; }
	public string? WaBodyParams { get; private set; }
	public string? WaHeaderParams { get; private set; }
	public string? WaButtonParams { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public VoiceMenuItem MenuItem { get; private set; } = null!;
	public MessagingWaTemplate? WaTemplate { get; private set; }

	/// <summary>Derivato da ActionType (hangup/forward_sip = terminale): non è una colonna scrivibile.</summary>
	public bool IsTerminal => ActionType is MenuActionType.Hangup or MenuActionType.ForwardSip;

	private VoiceMenuAction() { }   // unico costruttore, per il materializzatore di EF Core
}
