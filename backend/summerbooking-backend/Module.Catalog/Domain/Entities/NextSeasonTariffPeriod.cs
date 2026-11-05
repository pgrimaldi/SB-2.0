using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Intervallo di un periodo tariffario della stagione successiva, preparato in anticipo. Sta a parte dai periodi della
/// stagione corrente (<see cref="TariffPeriod"/>): listini, preventivi e ricerca non lo vedono.
/// </summary>
public sealed class NextSeasonTariffPeriod : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SeasonId { get; private set; }
	public string Name { get; private set; } = null!;
	public short PeriodGroupKey { get; private set; }
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private NextSeasonTariffPeriod() { }

	public static NextSeasonTariffPeriod Create(long seasonId, short groupKey, string name, DateOnly startsOn, DateOnly endsOn)
	{
		if (endsOn < startsOn)
			throw new ArgumentException("Fine dell'intervallo prima dell'inizio.", nameof(endsOn));
		return new NextSeasonTariffPeriod { SeasonId = seasonId, PeriodGroupKey = groupKey, Name = name.Trim(), StartsOn = startsOn, EndsOn = endsOn };
	}
}
