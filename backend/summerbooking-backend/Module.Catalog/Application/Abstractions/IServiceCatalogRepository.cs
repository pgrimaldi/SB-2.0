using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Application.Abstractions;

public sealed record ServiceFilter(long PropertyId, ServiceSaleKind? Kind, string? Search, string? SortBy, bool Descending);

public interface IServiceCatalogRepository
{
	Task<(IReadOnlyList<ServiceSaleItem> Items, int Total)> ListAsync(ServiceFilter filter, int page, int pageSize, CancellationToken ct = default);
	Task<IReadOnlyList<ServiceSaleItem>> ListAllAsync(long propertyId, CancellationToken ct = default);
	/// <summary>Servizio con varianti, traduzioni, opzioni, prezzi per periodo, file/settori e fasce.</summary>
	Task<ServiceSaleItem?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyList<ServiceSaleItem>> FindManyAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	Task<IReadOnlyDictionary<Guid, long>> FindIdsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	Task<IReadOnlyDictionary<long, Guid>> FindPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);
	void Add(ServiceSaleItem item);
	void Remove(ServiceSaleItem item);

	Task<int?> FindIconIdAsync(Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyDictionary<int, (Guid PublicId, long AssetId)>> FindIconsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);

	Task<IReadOnlyList<AutomaticPrice>> ListAutomaticAsync(long propertyId, CancellationToken ct = default);
	Task<AutomaticPrice?> FindAutomaticAsync(long propertyId, Guid publicId, CancellationToken ct = default);
	/// <summary>Prezzi automatici che citano il servizio fra quelli a cui si applicano.</summary>
	Task<IReadOnlyList<AutomaticPrice>> ListAutomaticForServiceAsync(long saleItemId, CancellationToken ct = default);
	void AddAutomatic(AutomaticPrice price);
	void RemoveAutomatic(AutomaticPrice price);

	Task<IReadOnlyList<TableService>> ListTablesAsync(long propertyId, CancellationToken ct = default);
	Task<TableService?> FindTableAsync(long propertyId, Guid publicId, CancellationToken ct = default);
	void AddTable(TableService service);
	void RemoveTable(TableService service);
}
