namespace Module.Notification.Domain.Enums;

/// <summary>Canale per cui è pensato il contenuto di un template.</summary>
public enum TemplateChannel
{
	Email = 0,
	Whatsapp = 1,
	Sms = 2,
	Push = 3,
	Any = 4
}
