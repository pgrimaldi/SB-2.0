namespace Module.Switchboard.Domain.Enums.Voice;

/// <summary>A chi inviare il template WhatsApp dell'azione send_whatsapp: il chiamante corrente o un numero fisso.</summary>
public enum WaRecipientMode
{
	Caller = 0,
	Fixed = 1
}
