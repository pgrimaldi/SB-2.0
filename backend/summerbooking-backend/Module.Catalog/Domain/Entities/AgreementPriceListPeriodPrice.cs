using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Prezzi per periodo tariffario (etichetta libera) di un listino/voucher di una convenzione, per modalità di durata.
/// Entità figlia di <see cref="AgreementPriceList"/>; radice per gli override puntuali per data
/// (<see cref="AgreementPriceListDatePrice"/>) e per gli override manuali per durata di soggiorno
/// (<see cref="AgreementPriceListStayPrice"/>). A differenza dell'analoga di pricing, non ha FK verso una
/// tabella stagioni: il periodo è un'etichetta libera.
/// </summary>
public sealed class AgreementPriceListPeriodPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public AgreementPriceList? PriceList { get; private set; }
	/// <summary>Etichetta «fila + periodo» dei dati importati, conservata solo come traccia: valgono <see cref="SectorId"/> e <see cref="TariffPeriodGroup"/>.</summary>
	public string? PeriodLabel { get; private set; }
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

	public ICollection<AgreementPriceListDatePrice> DatePrices { get; private set; } = new List<AgreementPriceListDatePrice>();
	public ICollection<AgreementPriceListStayPrice> StayPriceOverrides { get; private set; } = new List<AgreementPriceListStayPrice>();

	private AgreementPriceListPeriodPrice() { }

	internal static AgreementPriceListPeriodPrice Create(long? sectorId, short? periodGroup) => new() { SectorId = sectorId, TariffPeriodGroup = periodGroup };

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
				StayPriceOverrides.Add(AgreementPriceListStayPrice.Create(SectorId, days, price));
			else
				current.SetPrice(price);
		}
	}
}
