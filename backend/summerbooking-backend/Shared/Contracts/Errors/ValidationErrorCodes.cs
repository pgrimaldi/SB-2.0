namespace Shared.Contracts.Errors;

/// <summary>
/// Codici degli errori di campo comuni a tutti i moduli (<c>errors[].code</c>); il frontend li traduce. Un
/// codice rilasciato non si rinomina. I codici propri di un modulo stanno nel modulo.
/// </summary>
public static class ValidationErrorCodes
{
	public const string Prefix = "validation.";

	public const string Required = "validation.required";
	public const string Invalid = "validation.invalid";
	public const string Email = "validation.email";
	public const string MaxLength = "validation.max_length";
	public const string MinLength = "validation.min_length";
	public const string Length = "validation.length";
	public const string OutOfRange = "validation.out_of_range";
	public const string Phone = "validation.phone";
	public const string PhonePrefix = "validation.phone_prefix";
	public const string MustAccept = "validation.must_accept";
	public const string EndBeforeStart = "validation.end_before_start";
	public const string InvalidFileType = "validation.invalid_file_type";
}
