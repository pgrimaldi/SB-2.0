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
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<AgreementPriceListItem> Items { get; private set; } = new List<AgreementPriceListItem>();
	public ICollection<AgreementPriceListSectorPrice> SectorPrices { get; private set; } = new List<AgreementPriceListSectorPrice>();
	public ICollection<AgreementPriceListPeriodPrice> PeriodPrices { get; private set; } = new List<AgreementPriceListPeriodPrice>();
	public ICollection<AgreementPriceListDatePrice> DatePrices { get; private set; } = new List<AgreementPriceListDatePrice>();

	private AgreementPriceList() { }
}
