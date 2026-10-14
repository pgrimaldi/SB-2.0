using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Deroga di prezzo per data di calendario di una voce di listino: data festiva con tariffa dedicata o
/// override puntuale. Entità figlia dell'aggregate <see cref="PriceList"/>.
/// </summary>
public sealed class PriceListDatePrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public long? PeriodPriceId { get; private set; }
	public DatePriceKind Kind { get; private set; }
	public DateOnly PriceDate { get; private set; }
	public decimal? Price { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PriceListDatePrice() { }
}
