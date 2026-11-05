using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Application.Abstractions;

public sealed record PriceListFilter(long PropertyId, PriceListKind? Kind, string? Search, bool Descending);

public interface IPriceListRepository
{
	Task<(IReadOnlyList<PriceList> Items, int Total)> ListAsync(PriceListFilter filter, int page, int pageSize, CancellationToken ct = default);
	Task<IReadOnlyList<PriceList>> ListAllAsync(long propertyId, PriceListKind? kind, CancellationToken ct = default);
	/// <summary>Listini completi (composizione, prezzi, festivi, traduzioni), tracciati.</summary>
	Task<IReadOnlyList<PriceList>> ListFullAsync(long propertyId, CancellationToken ct = default);
	Task<PriceList?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyList<PriceList>> FindManyAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	Task<IReadOnlyDictionary<Guid, long>> FindIdsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, PriceListKind? kind, CancellationToken ct = default);
	Task<IReadOnlyDictionary<long, Guid>> FindPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);
	Task<bool> NameExistsAsync(long propertyId, PriceListKind kind, string name, long? exceptId, CancellationToken ct = default);
	/// <summary>Altri listini dello stesso tipo segnati come predefiniti, tracciati.</summary>
	Task<IReadOnlyList<PriceList>> ListDefaultsAsync(long propertyId, PriceListKind kind, long exceptId, CancellationToken ct = default);
	/// <summary>Articoli aggiuntivi legati al pacchetto, con la loro composizione.</summary>
	Task<IReadOnlyList<PriceList>> ListLinkedAdditionalsAsync(long packageId, CancellationToken ct = default);
	void Add(PriceList priceList);
	void Remove(PriceList priceList);

	Task<IReadOnlyList<ReducedSeason>> ListReducedSeasonsAsync(long propertyId, CancellationToken ct = default);
	Task<ReducedSeason?> FindReducedSeasonAsync(long propertyId, Guid publicId, CancellationToken ct = default);
	Task<int> CountReducedPricesAsync(long reducedSeasonId, CancellationToken ct = default);
	/// <summary>Elimina i prezzi della voce in tutti i pacchetti.</summary>
	Task RemoveReducedPricesAsync(long reducedSeasonId, CancellationToken ct = default);
	void AddReducedSeason(ReducedSeason season);
	void RemoveReducedSeason(ReducedSeason season);

	Task<IReadOnlyList<CalculationMethod>> ListCalculationMethodsAsync(CancellationToken ct = default);
	Task<IReadOnlyDictionary<Guid, long>> FindInventoryIdsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	Task<IReadOnlyDictionary<long, Guid>> FindInventoryPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);
}
