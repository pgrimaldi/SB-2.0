using FluentValidation;
using Module.Identity.Application.Abstractions;
using Module.Identity.Domain;
using Shared.Contracts.Errors;
using Shared.Security;

namespace Module.Identity.Application.Validators;

/// <summary>Regole di campo proprie di Identity; quelle comuni sono in <c>BuildingBlocks.Application.Validation.CommonRules</c>.</summary>
public static class IdentityRules
{
	public const int PasswordMinLength = 8;
	public const int PasswordMaxLength = 128;

	/// <summary>Regola delle nuove password (NIST SP 800-63B): lunghezza ed esclusione delle password diffuse.</summary>
	public static IRuleBuilderOptions<T, string?> NewPassword<T>(this IRuleBuilder<T, string?> rule, ICommonPasswordList commonPasswords)
		=> rule
			.NotEmpty()
			.MinimumLength(PasswordMinLength).WithErrorCode(IdentityErrors.PasswordTooShort)
			.MaximumLength(PasswordMaxLength).WithErrorCode(IdentityErrors.PasswordTooLong)
			.Must(p => p is null || !commonPasswords.Contains(p)).WithErrorCode(IdentityErrors.PasswordTooCommon);

	public static IRuleBuilderOptions<T, string?> ClientApp<T>(this IRuleBuilder<T, string?> rule)
		=> rule.Must(ClientApps.IsKnown).WithErrorCode(ValidationErrorCodes.Invalid);
}
