using Shared.Localization;

namespace Shared.Contracts.Identity;

/// <summary>Dati di un account utili agli altri moduli: nome da mostrare e lingua dell'interfaccia.</summary>
public sealed record UserSummary(Guid Id, string? DisplayName, AppLanguage Language);

/// <summary>Lettura degli account per gli altri moduli (Audit): mai le entità, solo il <c>public_id</c>.</summary>
public interface IUserDirectory
{
	/// <summary>Null se l'account non esiste o è stato cancellato.</summary>
	Task<UserSummary?> FindAsync(Guid userId, CancellationToken ct = default);
}
