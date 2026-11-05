using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Periodo di validità del prezzo automatico, con i suoi giorni e le sue fasce orarie.</summary>
public sealed class AutomaticPricePeriod : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long AutomaticPriceId { get; private set; }
	/// <summary>Primo giorno; null = dall'inizio della stagione.</summary>
	public DateOnly? StartsOn { get; private set; }
	/// <summary>Ultimo giorno; null = fino alla fine della stagione.</summary>
	public DateOnly? EndsOn { get; private set; }
	public Weekday[] Weekdays { get; private set; } = [];
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<AutomaticPriceTimeRange> TimeRanges { get; private set; } = new List<AutomaticPriceTimeRange>();

	private AutomaticPricePeriod() { }

	internal static AutomaticPricePeriod Create(AutomaticPricePeriodData d)
	{
		var period = new AutomaticPricePeriod { StartsOn = d.StartsOn, EndsOn = d.EndsOn, Weekdays = d.Weekdays.Distinct().Order().ToArray() };
		foreach (var (from, to) in d.TimeRanges.OrderBy(r => r.From))
			period.TimeRanges.Add(AutomaticPriceTimeRange.Create(from, to));
		return period;
	}

	/// <summary>Vale per quel giorno e quell'ora: dentro le date, in uno dei giorni (nessuno = tutti) e in una delle fasce (nessuna = sempre).</summary>
	public bool Covers(DateOnly date, Weekday weekday, TimeOnly time)
		=> (StartsOn is null || StartsOn <= date) && (EndsOn is null || date <= EndsOn)
			&& (Weekdays.Length == 0 || Weekdays.Contains(weekday))
			&& (TimeRanges.Count == 0 || TimeRanges.Any(t => t.From <= time && time < t.To));
}

public sealed record AutomaticPricePeriodData(
	DateOnly? StartsOn, DateOnly? EndsOn, IReadOnlyList<Weekday> Weekdays, IReadOnlyList<(TimeOnly From, TimeOnly To)> TimeRanges);
