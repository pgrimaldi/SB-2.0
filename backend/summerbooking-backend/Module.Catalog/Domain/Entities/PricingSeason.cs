using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Confini della stagione corrente (e, opzionale, della successiva) di uno stabilimento (1:1 con Property).</summary>
public sealed class PricingSeason : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public DateOnly SeasonStartsOn { get; private set; }
	public DateOnly SeasonEndsOn { get; private set; }
	public DateOnly? NextSeasonStartsOn { get; private set; }
	public DateOnly? NextSeasonEndsOn { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<TariffPeriod> TariffPeriods { get; private set; } = new List<TariffPeriod>();
	/// <summary>Periodi tariffari della stagione successiva, preparati in anticipo.</summary>
	public ICollection<NextSeasonTariffPeriod> NextSeasonPeriods { get; private set; } = new List<NextSeasonTariffPeriod>();

	private PricingSeason() { }

	public static PricingSeason Create(long propertyId, SeasonDates dates, Guid? createdByUserId)
	{
		var season = new PricingSeason { PropertyId = propertyId, CreatedByUserId = createdByUserId };
		season.SetDates(dates);
		return season;
	}

	public void SetDates(SeasonDates dates)
	{
		if (dates.EndsOn < dates.StartsOn)
			throw new ArgumentException("Fine stagione prima dell'inizio.", nameof(dates));
		if (dates.NextStartsOn is null != dates.NextEndsOn is null)
			throw new ArgumentException("Stagione successiva incompleta.", nameof(dates));
		if (dates.NextStartsOn is { } nextStart && (dates.NextEndsOn < nextStart || nextStart <= dates.EndsOn))
			throw new ArgumentException("Stagione successiva non valida.", nameof(dates));

		SeasonStartsOn = dates.StartsOn;
		SeasonEndsOn = dates.EndsOn;
		NextSeasonStartsOn = dates.NextStartsOn;
		NextSeasonEndsOn = dates.NextEndsOn;
		// senza stagione successiva i suoi periodi non hanno più senso
		if (dates.NextStartsOn is null)
			NextSeasonPeriods.Clear();
	}
}

/// <summary>Date della stagione corrente e della successiva (questa serve al rinnovo delle prenotazioni).</summary>
public sealed record SeasonDates(DateOnly StartsOn, DateOnly EndsOn, DateOnly? NextStartsOn, DateOnly? NextEndsOn);
