using BuildingBlocks.Application.Validation;
using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

public sealed class SavePropertyInfoRequestValidator : AbstractValidator<SavePropertyInfoRequest>
{
	public const string SlugPattern = "^[a-z0-9]+(-[a-z0-9]+)*$";

	public SavePropertyInfoRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
		RuleFor(x => x.Slug).MaximumLength(100).Matches(SlugPattern).When(x => x.Slug is not null);
		RuleFor(x => x.Description).MaximumLength(20_000);
		RuleFor(x => x.Regulation).MaximumLength(20_000);
		RuleFor(x => x.Address).MaximumLength(255);
		RuleFor(x => x.City).MaximumLength(255);
		RuleFor(x => x.Cap).MaximumLength(16);
		RuleFor(x => x.Province).MaximumLength(255);
		RuleFor(x => x.Region).MaximumLength(255);
		RuleFor(x => x.CountryCode).Length(2).When(x => x.CountryCode is not null);
		RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude is not null);
		RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude is not null);
		RuleFor(x => x.Longitude).NotNull().When(x => x.Latitude is not null);
		RuleFor(x => x.Latitude).NotNull().When(x => x.Longitude is not null);
		RuleFor(x => x.Phone).PhoneNumber();
		RuleFor(x => x.Whatsapp).PhoneNumber();
		RuleFor(x => x.Email).OptionalEmail().When(x => !string.IsNullOrEmpty(x.Email));
		RuleFor(x => x.Website).MaximumLength(255);
		RuleFor(x => x.Facebook).MaximumLength(255);
		RuleFor(x => x.Instagram).MaximumLength(255);
		RuleFor(x => x.VirtualTourUrl).MaximumLength(500).Must(BeHttpUrl).WithErrorCode(ValidationErrorCodes.Invalid)
			.When(x => !string.IsNullOrEmpty(x.VirtualTourUrl));
		RuleFor(x => x.OpenHoursTo).GreaterThan(x => x.OpenHoursFrom!.Value).When(x => x.OpenHoursFrom is not null && x.OpenHoursTo is not null);
		RuleFor(x => x.CancellationNoticeHours).Null().When(x => !x.IsFreeCancellation);
		RuleFor(x => x.CancellationNoticeHours).InclusiveBetween((short)1, (short)720).When(x => x.CancellationNoticeHours is not null);
		RuleFor(x => x.PetsPolicy).Must(c => Codes.TryParse<PetsPolicy>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
	}

	public static bool BeHttpUrl(string? url)
		=> Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}

public sealed class SetAmenitiesRequestValidator : AbstractValidator<SetAmenitiesRequest>
{
	public SetAmenitiesRequestValidator()
	{
		RuleFor(x => x.Codes).NotNull().Must(c => c.Count <= 200).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Codes).NotEmpty().MaximumLength(60);
	}
}

public sealed class ReorderPhotosRequestValidator : AbstractValidator<ReorderPhotosRequest>
{
	public ReorderPhotosRequestValidator()
	{
		RuleFor(x => x.Ids).NotEmpty().Must(i => i.Distinct().Count() == i.Count).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class RegisterPropertyRequestValidator : AbstractValidator<RegisterPropertyRequest>
{
	public RegisterPropertyRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
		RuleFor(x => x.City).NotEmpty().MaximumLength(255);
		RuleFor(x => x.ContactFullName).NotEmpty().MaximumLength(255);
		RuleFor(x => x.Email).NotEmpty().OptionalEmail();
		RuleFor(x => x.PhoneCountryCode).NotEmpty().PhonePrefix();
		RuleFor(x => x.Phone).NotEmpty().PhoneNumber();
	}
}
