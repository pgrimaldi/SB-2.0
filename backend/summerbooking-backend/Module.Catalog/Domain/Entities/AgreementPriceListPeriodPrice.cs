using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Prezzi per periodo tariffario (etichetta libera) di un listino/voucher hotel, per modalità di durata.
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
	public string? PeriodLabel { get; private set; }
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

	public ICollection<AgreementPriceListDatePrice> DatePrices { get; private set; } = new List<AgreementPriceListDatePrice>();
	public ICollection<AgreementPriceListStayPrice> StayPriceOverrides { get; private set; } = new List<AgreementPriceListStayPrice>();

	private AgreementPriceListPeriodPrice() { }
}
