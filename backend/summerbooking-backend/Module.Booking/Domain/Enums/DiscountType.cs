namespace Module.Booking.Domain.Enums;

/// <summary>
/// Natura del valore dello sconto — vocabolario condiviso con C2 (catalog_pricing_discounts) e C3, ma
/// ogni schema Postgres registra la propria copia dell'ENUM (i tipi ENUM non sono condivisibili cross-schema).
/// </summary>
public enum DiscountType
{
	Fixed = 0,
	Percentage = 1
}
