using FluentValidation;
using Shared.Contracts.Errors;
using Shared.Localization;

namespace BuildingBlocks.Application.Validation;

/// <summary>Regole di campo uguali in tutti i moduli.</summary>
public static class CommonRules
{
	public const int EmailMaxLength = 255;
	public const int PhoneMaxLength = 32;
	public const int PhonePrefixMaxLength = 8;

	/// <summary>Codice di una lingua gestita (<see cref="SupportedLanguages"/>); null ammesso.</summary>
	public static IRuleBuilderOptions<T, string?> LanguageCode<T>(this IRuleBuilder<T, string?> rule)
		=> rule.Must(l => l is null || SupportedLanguages.TryParse(l, out _)).WithErrorCode(ValidationErrorCodes.Invalid);

	public static IRuleBuilderOptions<T, string?> OptionalEmail<T>(this IRuleBuilder<T, string?> rule)
		=> rule.MaximumLength(EmailMaxLength).EmailAddress();

	public static IRuleBuilderOptions<T, string?> PhoneNumber<T>(this IRuleBuilder<T, string?> rule)
		=> rule.MaximumLength(PhoneMaxLength).Matches(@"^[0-9 ]*$").WithErrorCode(ValidationErrorCodes.Phone);

	public static IRuleBuilderOptions<T, string?> PhonePrefix<T>(this IRuleBuilder<T, string?> rule)
		=> rule.MaximumLength(PhonePrefixMaxLength).Matches(@"^\+?[0-9]{1,6}$").WithErrorCode(ValidationErrorCodes.PhonePrefix);
}
