using FluentValidation;
using FluentValidation.Results;
using Shared.Contracts.Errors;

namespace BuildingBlocks.Application.Validation;

/// <summary>Esecuzione dei validatori e conversione degli errori di FluentValidation nei codici dello standard API.</summary>
public static class ValidatorExtensions
{
	/// <exception cref="ValidationFailedException">Se il modello non è valido, con un errore per campo.</exception>
	public static void EnsureValid<T>(this IValidator<T> validator, T model)
	{
		var result = validator.Validate(model);
		if (!result.IsValid)
			throw new ValidationFailedException(result.Errors.Select(ToFieldError).ToList());
	}

	private static FieldError ToFieldError(ValidationFailure failure)
	{
		var code = failure.ErrorCode switch
		{
			{ } c when c.StartsWith(ValidationErrorCodes.Prefix, StringComparison.Ordinal) => c,
			"NotEmptyValidator" or "NotNullValidator" => ValidationErrorCodes.Required,
			"EmailValidator" => ValidationErrorCodes.Email,
			"MaximumLengthValidator" => ValidationErrorCodes.MaxLength,
			"MinimumLengthValidator" => ValidationErrorCodes.MinLength,
			"LengthValidator" => ValidationErrorCodes.Length,
			"GreaterThanValidator" or "GreaterThanOrEqualValidator" or "LessThanValidator"
				or "LessThanOrEqualValidator" or "InclusiveBetweenValidator" or "ExclusiveBetweenValidator" => ValidationErrorCodes.OutOfRange,
			_ => ValidationErrorCodes.Invalid,
		};

		var args = new Dictionary<string, object?>();
		var values = failure.FormattedMessagePlaceholderValues;
		if (values is not null)
		{
			foreach (var (source, target) in new[] { ("MaxLength", "max"), ("MinLength", "min"), ("ComparisonValue", "value"), ("From", "from"), ("To", "to") })
			{
				if (values.TryGetValue(source, out var value))
					args[target] = value;
			}
		}

		return new FieldError(CamelCase(failure.PropertyName), code, args.Count > 0 ? args : null);
	}

	private static string CamelCase(string path)
		=> string.Join('.', path.Split('.').Select(p => p.Length == 0 ? p : char.ToLowerInvariant(p[0]) + p[1..]));
}
