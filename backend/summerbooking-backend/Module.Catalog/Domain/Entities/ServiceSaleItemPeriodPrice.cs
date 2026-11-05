using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Prezzo del servizio in un periodo tariffario della stagione («variazioni di prezzo per periodo»).</summary>
public sealed class ServiceSaleItemPeriodPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SaleItemId { get; private set; }
	/// <summary>Chiave del periodo nella stagione (<see cref="TariffPeriod.PeriodGroupKey"/>).</summary>
	public short TariffPeriodGroup { get; private set; }
	public decimal Price { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ServiceSaleItemPeriodPrice() { }

	internal static ServiceSaleItemPeriodPrice Create(short periodGroup, decimal price) => new() { TariffPeriodGroup = periodGroup, Price = price };

	internal void SetPrice(decimal price) => Price = price;
}
