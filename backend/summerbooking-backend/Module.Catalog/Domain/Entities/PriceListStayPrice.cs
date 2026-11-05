using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario. Entità
/// figlia di secondo livello, appesa a <see cref="PriceListPeriodPrice"/>.
/// </summary>
public sealed class PriceListStayPrice : IAuditable, ISoftDeletable
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

	public const short MaxStayDays = 366;

	private PriceListStayPrice() { }

	internal static PriceListStayPrice Create(long? sectorId, short stayDays, decimal price)
		=> new() { SectorId = sectorId, StayDays = stayDays, Price = price, IsManualOverride = true };

	internal void SetPrice(decimal price) => Price = price;
}
