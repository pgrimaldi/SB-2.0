using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Listino ordinario o voucher venduto da una struttura partner (convenzione). Entità figlia di
/// <see cref="AgreementPartner"/>, testata della gerarchia listino/voucher (items, prezzi per
/// settore/periodo/data).
/// </summary>
public sealed class AgreementPriceList : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PartnerId { get; private set; }
	public AgreementPartner? Partner { get; private set; }
	public AgreementListType ListType { get; private set; }
	public PriceListKind Kind { get; private set; }
	public string Name { get; private set; } = null!;

	public short? MaxPeople { get; private set; }
	public short? CalculationMethodId { get; private set; }
	public MonthlySetting? MonthlySetting { get; private set; }
	public WeekendSetting? WeekendSetting { get; private set; }
	public bool AllowsPublicHolidays { get; private set; }
	public short? MinBookingDays { get; private set; }
	public short? MaxVoucherQuantity { get; private set; }
	/// <summary>«Prezzo cumulativo» anche sui listini delle convenzioni.</summary>
	public bool IsCumulative { get; private set; }
	public decimal? VatRate { get; private set; }
	public VatNature? VatNature { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<AgreementPriceListItem> Items { get; private set; } = new List<AgreementPriceListItem>();
	public ICollection<AgreementPriceListSectorPrice> SectorPrices { get; private set; } = new List<AgreementPriceListSectorPrice>();
	public ICollection<AgreementPriceListPeriodPrice> PeriodPrices { get; private set; } = new List<AgreementPriceListPeriodPrice>();
	public ICollection<AgreementPriceListDatePrice> DatePrices { get; private set; } = new List<AgreementPriceListDatePrice>();

	private AgreementPriceList() { }

	public static AgreementPriceList Create(long partnerId, AgreementListType listType, PriceListKind kind, Guid? createdBy) => new()
	{
		PublicId = Guid.CreateVersion7(),
		PartnerId = partnerId,
		ListType = listType,
		Kind = kind,
		CreatedByUserId = createdBy,
	};

	public void Update(AgreementPriceListData d)
	{
		if (d.VatRate is null == d.VatNature is null)
			throw new ArgumentException("Un'aliquota oppure la natura dell'esenzione.", nameof(d));
		Name = d.Name.Trim();
		MaxPeople = d.MaxPeople;
		CalculationMethodId = d.CalculationMethodId;
		MonthlySetting = d.MonthlySetting;
		WeekendSetting = d.WeekendSetting;
		AllowsPublicHolidays = d.AllowsPublicHolidays;
		MinBookingDays = ListType == AgreementListType.Voucher ? d.MinBookingDays : null;
		MaxVoucherQuantity = ListType == AgreementListType.Voucher ? d.MaxVoucherQuantity : null;
		IsCumulative = d.IsCumulative;
		VatRate = d.VatRate;
		VatNature = d.VatNature;
	}

	public void ReplaceItems(IEnumerable<PriceListItemData> items)
	{
		Items.Clear();
		foreach (var item in items)
			Items.Add(AgreementPriceListItem.Create(item));
	}

	public void SetSectorPrices(IReadOnlyDictionary<long, (decimal? Seasonal, decimal? SeasonalHalfDay)> prices)
	{
		foreach (var removed in SectorPrices.Where(p => !prices.ContainsKey(p.SectorId)).ToList())
			SectorPrices.Remove(removed);
		foreach (var (sectorId, (seasonal, halfDay)) in prices)
		{
			var current = SectorPrices.FirstOrDefault(p => p.SectorId == sectorId);
			if (current is null)
				SectorPrices.Add(AgreementPriceListSectorPrice.Create(sectorId, seasonal, halfDay));
			else
				current.SetPrices(seasonal, halfDay);
		}
	}

	public void SetPeriodPrices(IReadOnlyList<PeriodPriceData> prices)
	{
		var keys = prices.Select(p => (p.SectorId, p.PeriodGroup)).ToHashSet();
		foreach (var removed in PeriodPrices.Where(p => !keys.Contains((p.SectorId, p.TariffPeriodGroup))).ToList())
			PeriodPrices.Remove(removed);
		foreach (var data in prices)
		{
			var current = PeriodPrices.FirstOrDefault(p => p.SectorId == data.SectorId && p.TariffPeriodGroup == data.PeriodGroup);
			if (current is null)
			{
				current = AgreementPriceListPeriodPrice.Create(data.SectorId, data.PeriodGroup);
				PeriodPrices.Add(current);
			}
			current.SetPrices(data);
		}
	}

	public void SetHolidays(IReadOnlyCollection<DateOnly> dates)
	{
		foreach (var removed in DatePrices.Where(d => d.Kind == DatePriceKind.Holiday && !dates.Contains(d.PriceDate)).ToList())
			DatePrices.Remove(removed);
		foreach (var date in dates.Where(d => DatePrices.All(x => x.Kind != DatePriceKind.Holiday || x.PriceDate != d)))
			DatePrices.Add(AgreementPriceListDatePrice.Holiday(date));
	}
}
