using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Configurazione per stabilimento della feature Revenue Management (prezzo suggerito e banner sconto in
/// pagina pubblica); 1:1 con la property. Malgrado il nome, non riguarda le notifiche.
/// </summary>
public sealed class RevenueSettings : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	/// <summary>Prezzo suggerito attivo: il motore di prezzo usa la scelta del gestore.</summary>
	public bool IsSuggestedPriceEnabled { get; private set; }
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

	public static RevenueSettings Create(long propertyId) => new() { PropertyId = propertyId };

	public void Update(bool suggestedPrice, bool priceDiscount, bool shownOnPublicPage, short dailyBannerCount, bool limitedToSpots)
	{
		IsSuggestedPriceEnabled = suggestedPrice;
		IsPriceDiscountEnabled = priceDiscount;
		IsShownOnPublicPage = shownOnPublicPage;
		DailyBannerCount = dailyBannerCount;
		IsDiscountLimitedToSpots = limitedToSpots;
	}

	public void SetSpots(IReadOnlyCollection<long> spotIds)
	{
		foreach (var removed in DiscountedSpots.Where(s => !spotIds.Contains(s.SpotId)).ToList())
			DiscountedSpots.Remove(removed);
		foreach (var added in spotIds.Where(id => DiscountedSpots.All(s => s.SpotId != id)))
			DiscountedSpots.Add(RevenueDiscountedSpot.Create(added));
	}
}
