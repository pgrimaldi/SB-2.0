using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Configurazione per stabilimento della feature rivendita/sharing del posto: guida il comportamento
/// di ResaleOffer e ResaleCreditLedgerEntry.
/// </summary>
public sealed class ResaleSettings : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public bool IsEnabled { get; private set; }
	public ResaleMode Mode { get; private set; }
	public List<ResaleScope> AppliesTo { get; private set; } = new();
	public bool BothOptionsDisabled { get; private set; }
	public int CreditUnitsPerDay { get; private set; }
	public ResaleCreditCalc CreditCalc { get; private set; }
	public decimal CreditAmount { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ResaleSettings() { }
}
