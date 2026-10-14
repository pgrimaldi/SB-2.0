namespace Module.Switchboard.Domain.Enums.Messaging;

/// <summary>Tipo del messaggio WhatsApp in arrivo (solo event_type = message_in), passthrough Meta con fallback unknown.</summary>
public enum WaInboundMessageType
{
	Text = 0,
	Button = 1,
	Interactive = 2,
	Reaction = 3,
	Image = 4,
	Document = 5,
	Audio = 6,
	Video = 7,
	Location = 8,
	Sticker = 9,
	Contacts = 10,
	Unknown = 11
}
