namespace Module.Notification.Domain.Enums;

/// <summary>Evento della prenotazione che fa scattare l'invio di un trigger custom di automazione.</summary>
public enum AutomationTriggerKind
{
	OfBooking = 0,
	BeforeBooking = 1,
	AfterBooking = 2,
	PendingPayments = 3
}
