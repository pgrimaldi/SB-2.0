using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Override di prezzo per data di calendario (festivi, deroghe puntuali) di un listino/voucher hotel.
/// Entità figlia di <see cref="AgreementPriceList"/>, agganciata opzionalmente a un
/// <see cref="AgreementPriceListPeriodPrice"/>. Tabella tipicamente vuota in produzione (feature
/// Convenzioni recente); modellata per simmetria col lato pricing.
/// </summary>
public sealed class AgreementPriceListDatePrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public AgreementPriceList? PriceList { get; private set; }
	public long? PeriodPriceId { get; private set; }
	public AgreementPriceListPeriodPrice? PeriodPrice { get; private set; }
	public DatePriceKind Kind { get; private set; }
	public DateOnly PriceDate { get; private set; }
	public decimal? Price { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AgreementPriceListDatePrice() { }
}
