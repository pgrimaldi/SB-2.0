using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Tariffe base (giornaliera/weekend/mezza giornata/7-14gg/mensile) di un pacchetto per periodo
/// tariffario. Entità figlia dell'aggregate <see cref="PriceList"/>, radice a sua volta degli override per
/// durata (<see cref="PriceListStayPrice"/>) e, opzionalmente, per data.
/// </summary>
public sealed class PriceListPeriodPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	/// <summary>Fila/settore; null = tutte le file (listini senza prezzi per fila).</summary>
	public long? SectorId { get; private set; }
	public short? TariffPeriodGroup { get; private set; }
	public decimal? HalfDayPrice { get; private set; }
	public decimal? HalfDayWeekendPrice { get; private set; }
	public decimal? DailyPrice { get; private set; }
	public decimal? WeekendPrice { get; private set; }
	public decimal? HalfDay7Price { get; private set; }
	public decimal? Days7Price { get; private set; }
	public decimal? HalfDay14Price { get; private set; }
	public decimal? Days14Price { get; private set; }
	public decimal? HalfDayMonthlyPrice { get; private set; }
	public decimal? MonthlyPrice { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<PriceListStayPrice> StayPriceOverrides { get; private set; } = new List<PriceListStayPrice>();

	private PriceListPeriodPrice() { }

	internal static PriceListPeriodPrice Create(long? sectorId, short? periodGroup) => new() { SectorId = sectorId, TariffPeriodGroup = periodGroup };

	internal void SetPrices(PeriodPriceData d)
	{
		HalfDayPrice = d.HalfDay;
		HalfDayWeekendPrice = d.HalfDayWeekend;
		DailyPrice = d.Daily;
		WeekendPrice = d.Weekend;
		HalfDay7Price = d.HalfDay7;
		Days7Price = d.Days7;
		HalfDay14Price = d.HalfDay14;
		Days14Price = d.Days14;
		HalfDayMonthlyPrice = d.HalfDayMonthly;
		MonthlyPrice = d.Monthly;

		foreach (var removed in StayPriceOverrides.Where(s => !d.StayOverrides.ContainsKey(s.StayDays)).ToList())
			StayPriceOverrides.Remove(removed);
		foreach (var (days, price) in d.StayOverrides)
		{
			var current = StayPriceOverrides.FirstOrDefault(s => s.StayDays == days);
			if (current is null)
				StayPriceOverrides.Add(PriceListStayPrice.Create(SectorId, days, price));
			else
				current.SetPrice(price);
		}
	}
}
