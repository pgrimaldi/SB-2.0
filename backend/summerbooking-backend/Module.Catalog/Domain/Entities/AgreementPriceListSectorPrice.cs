using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Prezzo base / mezza giornata / stagionale di un listino/voucher hotel per settore. Entità figlia di
/// <see cref="AgreementPriceList"/>.
/// </summary>
public sealed class AgreementPriceListSectorPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public AgreementPriceList? PriceList { get; private set; }
	public long SectorId { get; private set; }
	public MapSector? Sector { get; private set; }
	public decimal? BasePrice { get; private set; }
	public decimal? HalfDayPrice { get; private set; }
	public decimal? SeasonalPrice { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AgreementPriceListSectorPrice() { }
}
