namespace Module.Booking.Domain.Enums;

/// <summary>
/// Come si calcola il valore del credito per giorno ceduto.
/// </summary>
public enum ResaleCreditCalc
{
	FixedAmount = 0,
	DailyPercentage = 1
}
