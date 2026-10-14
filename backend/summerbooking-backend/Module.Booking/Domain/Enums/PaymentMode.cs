namespace Module.Booking.Domain.Enums;

/// <summary>
/// Modalità di pagamento prevalente (riepilogo) — vocabolario condiviso con Module.Payment (schema payment ha
/// la propria copia dell'ENUM: i tipi ENUM di Postgres sono per-schema, non condivisibili cross-schema).
/// </summary>
public enum PaymentMode
{
	Cash = 0,
	CreditCard = 1,
	BankTransfer = 2,
	Other = 3
}
