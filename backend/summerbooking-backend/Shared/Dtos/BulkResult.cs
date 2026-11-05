namespace Shared.Dtos;

/// <summary>
/// Esito di un'operazione su più elementi: si eseguono quelli possibili, gli altri tornano con il codice d'errore
/// che il frontend traduce (stesso <c>code</c> e <c>args</c> dell'operazione singola).
/// </summary>
public sealed record BulkResult(IReadOnlyList<Guid> Succeeded, IReadOnlyList<BulkFailure> Failed);

public sealed record BulkFailure(Guid Id, string Code, IReadOnlyDictionary<string, object?>? Args = null);

/// <summary>Corpo delle operazioni su più elementi.</summary>
public sealed record BulkIdsRequest(IReadOnlyList<Guid> Ids)
{
	/// <summary>Elementi massimi per richiesta: una pagina della tabella più grande (300 righe).</summary>
	public const int MaxIds = 300;
}
