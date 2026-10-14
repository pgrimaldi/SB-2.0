using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Postazione a cui si applica lo sconto Revenue Management quando la configurazione
/// (<see cref="RevenueSettings"/>) è limitata a postazioni specifiche.
/// </summary>
public sealed class RevenueDiscountedSpot : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SettingsId { get; private set; }
	public RevenueSettings? Settings { get; private set; }
	public long SpotId { get; private set; }
	public MapSpot? Spot { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private RevenueDiscountedSpot() { }
}
