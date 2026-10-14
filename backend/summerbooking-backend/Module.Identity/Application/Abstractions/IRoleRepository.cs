using Module.Identity.Domain.Entities;

namespace Module.Identity.Application.Abstractions;

public interface IRoleRepository
{
	Task<Role?> GetByCodeAsync(string code, CancellationToken ct = default);
	Task<IReadOnlyList<Role>> ListAssignableAsync(CancellationToken ct = default);
}
