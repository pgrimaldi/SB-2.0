namespace Module.Notification.Domain.Enums;

/// <summary>Categoria del destinatario di un invio (chi riceve).</summary>
public enum RecipientType
{
	Customer = 0,
	StaffUser = 1,
	PropertyContact = 2,
	HotelPartner = 3,
	PlatformAdmin = 4,
	ExternalAddress = 5
}
