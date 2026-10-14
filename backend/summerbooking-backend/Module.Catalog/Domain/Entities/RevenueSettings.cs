using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Configurazione per stabilimento della feature Revenue Management (prezzo suggerito e banner sconto in
/// pagina pubblica); 1:1 con la property. Malgrado il nome, non è C6.
/// </summary>
public sealed class RevenueSettings : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public bool IsPriceDiscountEnabled { get; private set; }
	public bool IsShownOnPublicPage { get; private set; }
	public short DailyBannerCount { get; private set; }
	public short DailyBannerShownCount { get; private set; }
	public bool IsDiscountLimitedToSpots { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<RevenueDiscountedSpot> DiscountedSpots { get; private set; } = new List<RevenueDiscountedSpot>();

	private RevenueSettings() { }
}
