using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Intervallo di date di un periodo tariffario (bassa/media/alta stagione), eventualmente non contiguo:
/// più righe con lo stesso (SeasonId, PeriodGroupKey) formano un unico periodo.
/// </summary>
public sealed class TariffPeriod : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SeasonId { get; private set; }
	public PricingSeason? Season { get; private set; }
	public string Name { get; private set; } = null!;
	public short PeriodGroupKey { get; private set; }
	public TariffTier Tier { get; private set; }
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public const int NameMaxLength = 80;

	private TariffPeriod() { }

	/// <summary>Un intervallo del periodo; più intervalli con la stessa chiave formano lo stesso periodo.</summary>
	public static TariffPeriod Create(long seasonId, short groupKey, string name, DateOnly startsOn, DateOnly endsOn)
	{
		if (endsOn < startsOn)
			throw new ArgumentException("Fine dell'intervallo prima dell'inizio.", nameof(endsOn));
		return new TariffPeriod
		{
			SeasonId = seasonId,
			PeriodGroupKey = groupKey,
			Name = name.Trim(),
			Tier = TariffTier.Custom,
			StartsOn = startsOn,
			EndsOn = endsOn,
		};
	}
}
