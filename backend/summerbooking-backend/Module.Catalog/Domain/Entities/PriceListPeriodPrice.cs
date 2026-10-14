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
	public short? TariffPeriodGroup { get; private set; }
	public decimal? HalfDayPrice { get; private set; }
	public decimal? HalfDayWeekendPrice { get; private set; }
	public decimal? DailyPrice { get; private set; }
	public decimal? WeekendPrice { get; private set; }
	public decimal? Days7Price { get; private set; }
	public decimal? Days14Price { get; private set; }
	public decimal? MonthlyPrice { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<PriceListStayPrice> StayPriceOverrides { get; private set; } = new List<PriceListStayPrice>();

	private PriceListPeriodPrice() { }
}
