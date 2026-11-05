using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Domain;

/// <summary>Periodo tariffario di una data: fra quelli che la coprono vince il più breve, a parità il primo creato.</summary>
public static class TariffCalendar
{
	public static TariffPeriod? Resolve(IEnumerable<TariffPeriod> periods, DateOnly date)
		=> periods
			.Where(p => p.StartsOn <= date && date <= p.EndsOn)
			.OrderBy(p => p.EndsOn.DayNumber - p.StartsOn.DayNumber)
			.ThenBy(p => p.Id)
			.FirstOrDefault();
}
