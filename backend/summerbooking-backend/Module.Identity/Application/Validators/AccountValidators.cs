using BuildingBlocks.Application.Validation;
using FluentValidation;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Utils;

namespace Module.Identity.Application.Validators;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
	public ChangePasswordRequestValidator(ICommonPasswordList commonPasswords)
	{
		RuleFor(x => x.CurrentPassword).MaximumLength(1024);
		RuleFor(x => x.NewPassword).NewPassword(commonPasswords);
	}
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
	public UpdateProfileRequestValidator()
	{
		RuleFor(x => x.FirstName).MaximumLength(100);
		RuleFor(x => x.LastName).MaximumLength(100);
		RuleFor(x => x.PhoneCountryCode).PhonePrefix();
		RuleFor(x => x.Phone).PhoneNumber();
	}
}

public sealed class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{
	public UpdatePreferencesRequestValidator()
	{
		RuleFor(x => x.Language).LanguageCode();
		RuleForEach(x => x.NotificationSounds)
			.Must(s => Codes.TryParse<NotificationSoundScope>(s, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class CreateStaffUserRequestValidator : AbstractValidator<CreateStaffUserRequest>
{
	public CreateStaffUserRequestValidator(ICommonPasswordList commonPasswords)
	{
		RuleFor(x => x.Email).NotEmpty().OptionalEmail();
		RuleFor(x => x.Username).MaximumLength(100);
		RuleFor(x => x.Password).NewPassword(commonPasswords);
		RuleFor(x => x.FirstName).MaximumLength(100);
		RuleFor(x => x.LastName).MaximumLength(100);
		RuleFor(x => x.PhoneCountryCode).PhonePrefix();
		RuleFor(x => x.Phone).PhoneNumber();
		RuleFor(x => x.Role).NotEmpty();
		RuleFor(x => x.Language).LanguageCode();
		RuleForEach(x => x.ResourceScopes).NotEmpty().MaximumLength(100);
	}
}

public sealed class UpdateStaffUserRequestValidator : AbstractValidator<UpdateStaffUserRequest>
{
	public UpdateStaffUserRequestValidator(ICommonPasswordList commonPasswords)
	{
		RuleFor(x => x.Email).OptionalEmail().Must(e => e is null || e.Trim().Length > 0).WithErrorCode(ValidationErrorCodes.Required);
		RuleFor(x => x.Username).MaximumLength(100);
		RuleFor(x => x.Password).NewPassword(commonPasswords).When(x => x.Password is not null);
		RuleFor(x => x.FirstName).MaximumLength(100);
		RuleFor(x => x.LastName).MaximumLength(100);
		RuleFor(x => x.PhoneCountryCode).PhonePrefix();
		RuleFor(x => x.Phone).PhoneNumber();
		RuleFor(x => x.Language).LanguageCode();
		RuleForEach(x => x.ResourceScopes).NotEmpty().MaximumLength(100);
	}
}

public sealed class CreateOwnerRequestValidator : AbstractValidator<CreateOwnerRequest>
{
	public CreateOwnerRequestValidator(ICommonPasswordList commonPasswords)
	{
		RuleFor(x => x.Email).NotEmpty().OptionalEmail();
		RuleFor(x => x.Username).MaximumLength(100);
		RuleFor(x => x.Password).NewPassword(commonPasswords);
		RuleFor(x => x.FirstName).MaximumLength(100);
		RuleFor(x => x.LastName).MaximumLength(100);
		RuleFor(x => x.PhoneCountryCode).PhonePrefix();
		RuleFor(x => x.Phone).PhoneNumber();
		RuleFor(x => x.Language).LanguageCode();
	}
}

public sealed class UpdateOwnerRequestValidator : AbstractValidator<UpdateOwnerRequest>
{
	public UpdateOwnerRequestValidator(ICommonPasswordList commonPasswords)
	{
		RuleFor(x => x.Password).NewPassword(commonPasswords).When(x => x.Password is not null);
		RuleFor(x => x.FirstName).MaximumLength(100);
		RuleFor(x => x.LastName).MaximumLength(100);
		RuleFor(x => x.PhoneCountryCode).PhonePrefix();
		RuleFor(x => x.Phone).PhoneNumber();
		RuleFor(x => x.Language).LanguageCode();
	}
}
