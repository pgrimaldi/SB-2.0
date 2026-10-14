namespace Module.Payment.Domain.Enums;

/// <summary>
/// Modalità con cui è avvenuto un movimento di incasso (<see cref="Entities.LedgerEntry"/>). Copia
/// per-schema di Module.Booking.Domain.Enums.PaymentMode: i tipi ENUM PostgreSQL sono per-schema, non
/// condivisibili cross-schema.
/// </summary>
public enum PaymentMode
{
	Cash = 0,
	CreditCard = 1,
	BankTransfer = 2,
	Other = 3
}
