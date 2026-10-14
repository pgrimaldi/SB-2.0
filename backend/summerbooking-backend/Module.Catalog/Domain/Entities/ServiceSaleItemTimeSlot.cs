using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Fascia oraria interna prenotabile di un servizio a fascia oraria (<see cref="ServiceSaleItem"/>, kind =
/// hourly), con eventuale prezzo dedicato.
/// </summary>
public sealed class ServiceSaleItemTimeSlot : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long SaleItemId { get; private set; }
	public string Label { get; private set; } = null!;
	public TimeOnly? SlotStart { get; private set; }
	public TimeOnly? SlotEnd { get; private set; }
	public short? DurationMinutes { get; private set; }
	public decimal? Price { get; private set; }
	public bool IsActive { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ServiceSaleItemTimeSlot() { }
}
