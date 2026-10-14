namespace Module.Notification.Domain.Enums;

/// <summary>Tipo d'uso di un file del catalogo media del modulo Notification.</summary>
public enum NotificationMediaKind
{
	TemplateAttachment = 0,
	CampaignAttachment = 1,
	EventCoverImage = 2,
	RenderedPdf = 3,
	InboundUpload = 4,
	Other = 5
}
