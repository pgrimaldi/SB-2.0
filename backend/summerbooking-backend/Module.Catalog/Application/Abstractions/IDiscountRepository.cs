using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Application.Abstractions;

public sealed record DiscountFilter(long PropertyId, string? Search, string? SortBy, bool Descending);

public interface IDiscountRepository
{
	Task<(IReadOnlyList<Discount> Items, int Total)> ListAsync(DiscountFilter filter, int page, int pageSize, CancellationToken ct = default);
	/// <summary>Con periodi, file/settori e fasce orarie.</summary>
	Task<Discount?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyList<Discount>> FindManyAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	Task<bool> CodeExistsAsync(long propertyId, string code, long? exceptId, CancellationToken ct = default);
	void Add(Discount discount);
	void Remove(Discount discount);
}
