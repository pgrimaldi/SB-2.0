namespace Shared.Infrastructure;

/// <summary>
/// Risolve un riferimento <c>*_secret_ref</c> nel valore del segreto: il valore non
/// è mai nel database. Implementazione in BuildingBlocks.Infrastructure.Secrets.
/// </summary>
public interface ISecretResolver
{
	string? Resolve(string secretRef);
}
