using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Application.Abstractions;

public interface IOnlineSettingsRepository
{
	/// <summary>Impostazioni con periodi, postazioni e limiti di soggiorno.</summary>
	Task<OnlineBookingSettings?> FindAsync(long propertyId, CancellationToken ct = default);
	void Add(OnlineBookingSettings settings);

	Task<ServiceBookingSettings?> FindServiceSettingsAsync(long propertyId, CancellationToken ct = default);
	void AddServiceSettings(ServiceBookingSettings settings);

	Task<bool> IsOnlineSalesSuspendedAsync(long propertyId, CancellationToken ct = default);

	Task<IReadOnlyDictionary<Guid, long>> FindSpotIdsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	Task<IReadOnlyList<Guid>> FindSpotPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);

	Task<List<MapLegendItem>> ListLegendAsync(long propertyId, CancellationToken ct = default);
	void AddLegend(IEnumerable<MapLegendItem> items);
	void RemoveLegend(IEnumerable<MapLegendItem> items);
}
