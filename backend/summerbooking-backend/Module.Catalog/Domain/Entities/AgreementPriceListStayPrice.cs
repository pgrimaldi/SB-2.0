using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario di un
/// listino/voucher di una convenzione. Entità figlia di secondo livello, appesa a
/// <see cref="AgreementPriceListPeriodPrice"/> — gemella di <see cref="PriceListStayPrice"/> dei listini dello
/// stabilimento.
/// </summary>
public sealed class AgreementPriceListStayPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PeriodPriceId { get; private set; }
	public long? SectorId { get; private set; }
	public short StayDays { get; private set; }
	public decimal Price { get; private set; }
	public bool IsManualOverride { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AgreementPriceListStayPrice() { }

	internal static AgreementPriceListStayPrice Create(long? sectorId, short stayDays, decimal price)
		=> new() { SectorId = sectorId, StayDays = stayDays, Price = price, IsManualOverride = true };

	internal void SetPrice(decimal price) => Price = price;
}
