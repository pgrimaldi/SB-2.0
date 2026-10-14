namespace Module.Booking.Domain.Enums;

/// <summary>
/// Categoria del servizio venduto — allineata (ma non FK) a catalog.service_sale_item_kind, schema diverso.
/// Additional esiste solo per essere referenziato come categoria in booking_discounts.applies_to_categories:
/// BookingServiceLine.Kind non assume mai questo valore (gli articoli aggiuntivi sono
/// booking_line_additional_items, non una booking_service_lines).
/// </summary>
public enum ServiceCategory
{
	Bar = 0,
	Normal = 1,
	Hourly = 2,
	Additional = 3
}
