using BuildingBlocks.Application.Validation;
using FluentValidation;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Shared.Contracts.Errors;

namespace Module.Identity.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
	public LoginRequestValidator()
	{
		RuleFor(x => x.Username).NotEmpty().MaximumLength(255);
		// Limite alto ma presente: evita di far calcolare l'hash di input enormi.
		RuleFor(x => x.Password).NotEmpty().MaximumLength(1024);
		RuleFor(x => x.App).ClientApp();
	}
}

public sealed class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
	public VerifyOtpRequestValidator()
	{
		RuleFor(x => x.MfaToken).NotEmpty();
		RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{4,10}$").WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SocialLoginRequestValidator : AbstractValidator<SocialLoginRequest>
{
	public SocialLoginRequestValidator()
	{
		RuleFor(x => x.Token).NotEmpty().MaximumLength(8192);
		RuleFor(x => x.App).ClientApp();
		RuleFor(x => x.Email).OptionalEmail();
		RuleFor(x => x.FirstName).MaximumLength(100);
		RuleFor(x => x.LastName).MaximumLength(100);
		RuleFor(x => x.Language).LanguageCode();
	}
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
	public RegisterRequestValidator(ICommonPasswordList commonPasswords)
	{
		RuleFor(x => x.Email).NotEmpty().OptionalEmail();
		RuleFor(x => x.Password).NewPassword(commonPasswords);
		RuleFor(x => x.FirstName).MaximumLength(100);
		RuleFor(x => x.LastName).MaximumLength(100);
		RuleFor(x => x.PhoneCountryCode).PhonePrefix();
		RuleFor(x => x.Phone).PhoneNumber();
		RuleFor(x => x.Language).NotEmpty().LanguageCode();
		RuleFor(x => x.TermsAccepted).Equal(true).WithErrorCode(ValidationErrorCodes.MustAccept);
	}
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
	public ForgotPasswordRequestValidator()
	{
		RuleFor(x => x.Email).NotEmpty().OptionalEmail();
		RuleFor(x => x.App).ClientApp();
	}
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
	public ResetPasswordRequestValidator(ICommonPasswordList commonPasswords)
	{
		RuleFor(x => x.Token).NotEmpty().MaximumLength(256);
		RuleFor(x => x.NewPassword).NewPassword(commonPasswords);
	}
}
