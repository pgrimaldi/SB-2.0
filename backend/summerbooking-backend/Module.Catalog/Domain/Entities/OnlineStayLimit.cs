using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Limite minimo/massimo di giorni consecutivi prenotabili online, per settore e finestra di date.</summary>
public sealed class OnlineStayLimit : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SettingsId { get; private set; }
	public long SectorId { get; private set; }
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public short? MinDays { get; private set; }
	public short? MaxDays { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private OnlineStayLimit() { }
}
