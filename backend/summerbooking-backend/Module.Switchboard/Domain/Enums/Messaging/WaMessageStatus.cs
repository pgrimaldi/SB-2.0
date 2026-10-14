namespace Module.Switchboard.Domain.Enums.Messaging;

/// <summary>Stato di consegna di un messaggio WhatsApp, valorizzato solo per event_type = status.</summary>
public enum WaMessageStatus
{
	Sent = 0,
	Delivered = 1,
	Read = 2,
	Failed = 3
}
