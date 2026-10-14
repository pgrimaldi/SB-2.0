namespace Module.Notification.Domain.Enums;

/// <summary>Stato del ciclo di consegna di un invio: queued → sending → sent → delivered | failed | bounced | skipped.</summary>
public enum DeliveryStatus
{
	Queued = 0,
	Sending = 1,
	Sent = 2,
	Delivered = 3,
	Failed = 4,
	Bounced = 5,
	Skipped = 6
}
