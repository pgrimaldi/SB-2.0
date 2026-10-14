using Module.Payment.Domain.Enums;
using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Scontrino/addebito trasmesso da un POS esterno (OfSale o TeamSystem) e associato a una prenotazione
/// dello stabilimento.
/// </summary>
public sealed class PosCharge : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public PosSystem PosSystem { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid? ReservationPublicId { get; private set; }
	public string? BookingNumber { get; private set; }
	public string ExternalRef { get; private set; } = null!;
	public string? Description { get; private set; }
	public decimal TotalAmount { get; private set; }
	public string Currency { get; private set; } = null!;
	public PosChargeStatus Status { get; private set; }
	public Guid? LedgerEntryRef { get; private set; }
	public DateTime ChargedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<PosChargeItem> Items { get; private set; } = new List<PosChargeItem>();

	private PosCharge() { }
}
