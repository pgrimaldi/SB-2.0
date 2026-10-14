using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Voce di listino di uno stabilimento: un pacchetto (Kind = Package) o un articolo aggiuntivo (Kind =
/// Additional). Radice di consistenza della gerarchia pricing (items, prezzi per settore/periodo/giorno/
/// data, finestre ridotte, prezzi suggeriti).
/// </summary>
public sealed class PriceList : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public PriceListKind Kind { get; private set; }
	public string Name { get; private set; } = null!;

	public short? MaxPeople { get; private set; }
	public short? CalculationMethodId { get; private set; }
	public CalculationMethod? CalculationMethod { get; private set; }
	public MonthlySetting? MonthlySetting { get; private set; }
	public WeekendSetting? WeekendSetting { get; private set; }
	public SalesChannel SalesChannel { get; private set; }
	public bool IsDefault { get; private set; }
	public bool IsDefaultOnline { get; private set; }
	public bool IsAreaPackage { get; private set; }
	public bool IsCumulative { get; private set; }
	public bool AllowsPublicHolidays { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<PriceListItem> Items { get; private set; } = new List<PriceListItem>();
	public ICollection<PriceListSectorPrice> SectorPrices { get; private set; } = new List<PriceListSectorPrice>();
	public ICollection<PriceListPeriodPrice> PeriodPrices { get; private set; } = new List<PriceListPeriodPrice>();
	public ICollection<PriceListDatePrice> DatePrices { get; private set; } = new List<PriceListDatePrice>();
	public ICollection<PriceListReducedWindow> ReducedWindows { get; private set; } = new List<PriceListReducedWindow>();

	private PriceList() { }
}
