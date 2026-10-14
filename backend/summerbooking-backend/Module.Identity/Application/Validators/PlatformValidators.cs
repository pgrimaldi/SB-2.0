using FluentValidation;
using Module.Identity.Application.Dtos;
using Shared.Contracts.Errors;

namespace Module.Identity.Application.Validators;

public sealed class SetSubscriptionRequestValidator : AbstractValidator<SetSubscriptionRequest>
{
	public SetSubscriptionRequestValidator()
	{
		RuleFor(x => x.LicensePlanCode).NotEmpty();
		RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt).WithErrorCode(ValidationErrorCodes.EndBeforeStart);
		RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount is not null);
		RuleFor(x => x.Currency).Matches("^[A-Z]{3}$").WithErrorCode(ValidationErrorCodes.Invalid).When(x => x.Currency is not null);
	}
}

public sealed class LicensePlanTranslationValidator : AbstractValidator<LicensePlanTranslationDto>
{
	public LicensePlanTranslationValidator()
	{
		RuleFor(x => x.Culture).NotEmpty().MaximumLength(16);
		RuleFor(x => x.Label).NotEmpty().MaximumLength(80);
		RuleFor(x => x.Description).MaximumLength(1000);
	}
}

public sealed class CreateLicensePlanRequestValidator : AbstractValidator<CreateLicensePlanRequest>
{
	public CreateLicensePlanRequestValidator()
	{
		RuleFor(x => x.Code).NotEmpty().Matches("^[a-z0-9_]{2,50}$").WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.DurationMonths).GreaterThan((short)0);
		RuleFor(x => x.Translations).NotEmpty();
		RuleForEach(x => x.Translations).SetValidator(new LicensePlanTranslationValidator());
	}
}

public sealed class UpdateLicensePlanRequestValidator : AbstractValidator<UpdateLicensePlanRequest>
{
	public UpdateLicensePlanRequestValidator()
	{
		RuleFor(x => x.DurationMonths).GreaterThan((short)0);
		RuleFor(x => x.Translations).NotEmpty();
		RuleForEach(x => x.Translations).SetValidator(new LicensePlanTranslationValidator());
	}
}

public sealed class CreateContractDocumentRequestValidator : AbstractValidator<CreateContractDocumentRequest>
{
	public CreateContractDocumentRequestValidator()
	{
		RuleFor(x => x.VersionCode).NotEmpty().MaximumLength(120);
		RuleFor(x => x.Title).MaximumLength(200);
		RuleFor(x => x.Notes).MaximumLength(2000);
	}
}
