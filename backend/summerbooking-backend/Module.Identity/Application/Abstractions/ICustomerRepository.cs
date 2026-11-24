using Module.Identity.Domain.Entities;

namespace Module.Identity.Application.Abstractions;

/// <summary>Filtri della lista contatti; <see cref="SortBy"/> è già validato.</summary>
public sealed record CustomerQuery(
	Guid PropertyId,
	string? Search,
	bool BlacklistOnly,
	string SortBy,
	bool Descending,
	int Page,
	int PageSize);

public interface ICustomerRepository
{
	Task<(IReadOnlyList<Customer> Items, int Total)> ListAsync(CustomerQuery query, CancellationToken ct = default);

	/// <summary>Tutte le righe del filtro, senza paginazione (esportazione).</summary>
	Task<IReadOnlyList<Customer>> ListAllAsync(CustomerQuery query, CancellationToken ct = default);

	Task<Customer?> GetAsync(Guid propertyId, Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyList<Customer>> FindByContactAsync(Guid propertyId, string? email, string? phone, CancellationToken ct = default);
	Task<IReadOnlyList<Customer>> ListByPublicIdsAsync(Guid propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	Task<IReadOnlyList<Guid>> SearchIdsAsync(Guid propertyId, string text, int max, CancellationToken ct = default);
	Task<IReadOnlyList<Customer>> ListByUserAsync(long userId, CancellationToken ct = default);

	/// <summary>Contatti di tutti gli stabilimenti con questa email.</summary>
	Task<IReadOnlyList<Customer>> ListByEmailAsync(string email, CancellationToken ct = default);

	void Add(Customer customer);
	void Remove(Customer customer);
}
