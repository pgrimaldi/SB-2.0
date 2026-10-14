namespace Module.Notification.Domain.Enums;

/// <summary>
/// Vocabolario canonico dei segnaposto ammessi nei template ({{ ... }}). NON un ENUM Postgres nativo:
/// la colonna notification_templates.placeholder_keys_used resta un text[] semplice, convertito da/verso
/// questa lista con un ValueConverter in Configuration.
/// </summary>
public enum PlaceholderKey
{
	EstablishmentName = 0,
	CustomerFullName = 1,
	BookingDateFrom = 2,
	BookingDateTo = 3,
	BookingTotalAmount = 4,
	ReservationNumber = 5,
	PropertyContactPhone = 6
}
