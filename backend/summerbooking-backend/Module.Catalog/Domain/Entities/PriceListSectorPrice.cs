using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Prezzo base per settore di un pacchetto; l'esistenza della riga marca anche l'applicabilità del
/// pacchetto al settore. Entità figlia dell'aggregate <see cref="PriceList"/>.
/// </summary>
public sealed class PriceListSectorPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public long SectorId { get; private set; }
	public decimal? BasePrice { get; private set; }
	public decimal? HalfDayPrice { get; private set; }
	public decimal? SeasonalPrice { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PriceListSectorPrice() { }
}
