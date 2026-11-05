using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Application.Abstractions;

public sealed record AdminPropertyFilter(PropertyStatus? Status, string? Search, bool Trashed);

/// <summary>Stabilimento con la sua vetrina (se c'è), per il pannello di amministrazione.</summary>
public sealed record AdminPropertyRow(Property Property, Bathhouse? Bathhouse);

/// <summary>Dati di tutta la piattaforma per il systemadmin; gli stabilimenti nel cestino si vedono solo da qui.</summary>
public interface IPlatformRepository
{
	Task<(IReadOnlyList<AdminPropertyRow> Items, int Total)> ListPropertiesAsync(AdminPropertyFilter filter, int page, int pageSize, CancellationToken ct = default);
	/// <summary>Anche nel cestino, non quelli eliminati definitivamente; tracciato, con vetrina e badge.</summary>
	Task<AdminPropertyRow?> FindPropertyAsync(Guid publicId, CancellationToken ct = default);
	/// <summary>Cestino: deleted_at senza rimuovere l'entità (la vetrina caricata resta agganciata).</summary>
	void TrashProperty(Property property, DateTime now);
	void RestoreProperty(Property property);
	Task<Bathhouse?> FindBathhouseByPublicIdAsync(Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyDictionary<Guid, long>> FindBathhouseIdsByPropertyAsync(IReadOnlyCollection<Guid> propertyPublicIds, CancellationToken ct = default);

	Task<IReadOnlyList<MapIcon>> ListIconsAsync(MapIconCategoryCode? category, CancellationToken ct = default);
	Task<MapIcon?> FindIconAsync(Guid publicId, CancellationToken ct = default);
	Task<MapIconCategory?> FindIconCategoryAsync(MapIconCategoryCode code, CancellationToken ct = default);
	/// <summary>Icone predefinite della stessa categoria e stato, tracciate.</summary>
	Task<IReadOnlyList<MapIcon>> ListDefaultIconsAsync(short categoryId, MapIconState type, CancellationToken ct = default);
	void AddIcon(MapIcon icon);
	void RemoveIcon(MapIcon icon);
	/// <summary>Mappe, articoli personalizzati e legende che usano un'immagine passano all'altra.</summary>
	Task<int> RepointAssetAsync(long fromAssetId, long toAssetId, CancellationToken ct = default);

	Task<IReadOnlyList<MapBeachElement>> ListBeachElementsAsync(CancellationToken ct = default);
	Task<MapBeachElement?> FindBeachElementAsync(Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyDictionary<long, int>> CountBeachElementUsagesAsync(CancellationToken ct = default);
	void AddBeachElement(MapBeachElement element);
	void RemoveBeachElement(MapBeachElement element);

	Task<IReadOnlyList<ShowcaseBadge>> ListBadgesAsync(CancellationToken ct = default);
	Task<ShowcaseBadge?> FindBadgeAsync(Guid publicId, CancellationToken ct = default);
	void AddBadge(ShowcaseBadge badge);
	void RemoveBadge(ShowcaseBadge badge);
	Task RemoveBadgeAssignmentsAsync(long badgeId, DateTime now, CancellationToken ct = default);

	Task<IReadOnlyList<ShowcaseRegion>> ListRegionsAsync(CancellationToken ct = default);
	Task<ShowcaseRegion?> FindRegionAsync(Guid publicId, CancellationToken ct = default);
	Task<bool> RegionNameExistsAsync(short? countryId, string name, long? exceptId, CancellationToken ct = default);
	void AddRegion(ShowcaseRegion region);
	void RemoveRegion(ShowcaseRegion region);
	/// <summary>Strutture con regione automatica, tracciate (per riassegnarla).</summary>
	Task<IReadOnlyList<Bathhouse>> ListAutomaticRegionBathhousesAsync(CancellationToken ct = default);
	Task ClearRegionAsync(long regionId, CancellationToken ct = default);
	Task<short?> FindCountryIdAsync(string code, CancellationToken ct = default);
	Task<IReadOnlyDictionary<short, string>> CountryCodesAsync(CancellationToken ct = default);

	/// <summary>Voci della sezione con la struttura e il suo stabilimento, tracciate.</summary>
	Task<List<ShowcaseEntry>> ListShowcaseAsync(ShowcaseSection section, CancellationToken ct = default);
	void AddShowcaseEntry(ShowcaseEntry entry);
	void RemoveShowcaseEntry(ShowcaseEntry entry);
	Task<IReadOnlyDictionary<long, Guid>> FindPropertyPublicIdsAsync(IReadOnlyCollection<long> propertyIds, CancellationToken ct = default);
	Task<IReadOnlyList<TariffPeriod>> FindSeasonPeriodsAsync(long propertyId, CancellationToken ct = default);
	Task<IReadOnlyList<PriceListPeriodPrice>> ListPeriodPricesAsync(long priceListId, CancellationToken ct = default);
}
