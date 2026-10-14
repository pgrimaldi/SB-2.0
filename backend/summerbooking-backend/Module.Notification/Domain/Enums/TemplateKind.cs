namespace Module.Notification.Domain.Enums;

/// <summary>Scopo di un template di messaggio.</summary>
public enum TemplateKind
{
	ReservationSummary = 0,
	AgreementReport = 1,
	Campaign = 2,
	Automation = 3,
	Transactional = 4,
	Generic = 5
}
