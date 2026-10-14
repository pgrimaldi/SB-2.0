namespace Module.ZTest.Domain.ValueObjects;

/// <summary>
/// Value Object che rappresenta un intervallo di date (es. check-in / check-out).
/// Immutabile e con uguaglianza per valore (record). Contiene le proprie invarianti.
/// </summary>
public sealed record DateRange
{
	public DateOnly From { get; }
	public DateOnly To { get; }

	public DateRange(DateOnly from, DateOnly to)
	{
		if (to <= from)
			throw new ArgumentException("La data di fine deve essere successiva alla data di inizio.", nameof(to));

		From = from;
		To = to;
	}

	/// <summary>Numero di notti coperte dall'intervallo.</summary>
	public int Nights => To.DayNumber - From.DayNumber;

	/// <summary>Indica se questo intervallo si sovrappone a un altro.</summary>
	public bool Overlaps(DateRange other) => From < other.To && other.From < To;
}
