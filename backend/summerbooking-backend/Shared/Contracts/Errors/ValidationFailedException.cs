namespace Shared.Contracts.Errors;

/// <summary>Errore di un singolo campo nel corpo di una richiesta.</summary>
public sealed record FieldError(string Field, string Code, IReadOnlyDictionary<string, object?>? Args = null);

/// <summary>Input non valido: diventa un 400 con l'elenco <c>errors[]</c> dei campi.</summary>
public sealed class ValidationFailedException : Exception
{
	public IReadOnlyList<FieldError> Errors { get; }

	public ValidationFailedException(IReadOnlyList<FieldError> errors) : base(CommonErrorCodes.ValidationFailed)
	{
		Errors = errors;
	}

	public ValidationFailedException(string field, string code, IReadOnlyDictionary<string, object?>? args = null)
		: this([new FieldError(field, code, args)])
	{
	}
}
