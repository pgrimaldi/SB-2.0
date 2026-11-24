using Shared.Localization;

namespace Shared.Contracts.Identity;

/// <summary>Dati di un account utili agli altri moduli: nome da mostrare e lingua dell'interfaccia.</summary>
public sealed record UserSummary(Guid Id, string? DisplayName, AppLanguage Language);

/// <summary>
/// Postazioni che un utente può vedere e prenotare (affiliato). Ogni voce è l'id della postazione oppure, per le
/// assegnazioni del vecchio gestionale, la posizione nel formato <c>R{fila}C{colonna}</c>.
/// </summary>
public sealed record UserResourceScope(bool IsRestricted, IReadOnlyList<string> Scopes);

/// <summary>Lettura degli account per gli altri moduli (Audit, Booking): mai le entità, solo il <c>public_id</c>.</summary>
public interface IUserDirectory
{
	/// <summary>Null se l'account non esiste o è stato cancellato.</summary>
	Task<UserSummary?> FindAsync(Guid userId, CancellationToken ct = default);

	/// <summary>Conferma con la password dell'utente per le operazioni distruttive (cancellazione dati per gruppo).</summary>
	Task<bool> VerifyPasswordAsync(Guid userId, string password, CancellationToken ct = default);

	/// <summary>Restrizione alle postazioni assegnate; senza account, nessuna restrizione.</summary>
	Task<UserResourceScope> GetResourceScopeAsync(Guid userId, CancellationToken ct = default);
}
