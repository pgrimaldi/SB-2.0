using Module.Switchboard.Domain.Enums.Messaging;

namespace Module.Switchboard.Domain.Entities.Messaging;

/// <summary>
/// Evento del webhook WhatsApp Meta Cloud API ricevuto sul numero del centralino: aggiornamento di
/// consegna o messaggio in arrivo. Log forense write-only — come VoiceCallDiagnosticEvent, non
/// implementa ISoftDeletable/IAuditable pieni: created_at/updated_at/deleted_at sono esposti per
/// uniformità di schema ma le righe sono immutabili e cancellate in blocco dal job di retention.
/// </summary>
public sealed class MessagingWaEventLog
{
	public long Id { get; private set; }
	public long? ConfigId { get; private set; }
	public Guid? PropertyId { get; private set; }
	public WaWebhookEventType EventType { get; private set; }
	public string? Wamid { get; private set; }
	public string? WabaId { get; private set; }
	public string? PhoneNumberId { get; private set; }
	public string? DisplayPhoneNumber { get; private set; }
	public string? CounterpartyNumber { get; private set; }
	public string? ContactName { get; private set; }
	public WaMessageStatus? Status { get; private set; }
	public int? ErrorCode { get; private set; }
	public string? ErrorTitle { get; private set; }
	public string? ErrorMessage { get; private set; }
	public string? ConversationId { get; private set; }
	public WaConversationOrigin? ConversationOrigin { get; private set; }
	public bool? PricingBillable { get; private set; }
	public WaPricingCategory? PricingCategory { get; private set; }
	public string? PricingModel { get; private set; }
	public WaInboundMessageType? MessageType { get; private set; }
	public string? BodyText { get; private set; }
	public string? BodyPayload { get; private set; }
	public string? RawPayload { get; private set; }
	public DateTime? MetaTimestamp { get; private set; }
	public DateTime ReceivedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public SwitchboardConfig? Config { get; private set; }

	private MessagingWaEventLog() { }   // unico costruttore, per il materializzatore di EF Core
}
