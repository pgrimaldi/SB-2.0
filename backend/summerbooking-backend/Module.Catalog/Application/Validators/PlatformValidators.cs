using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Localization;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

public static class PlatformRules
{
	public const int MaxShowcaseEntries = 50;
	public const int MaxBadgesPerProperty = 10;

	public static bool NameTranslations(IReadOnlyList<NameTranslationDto>? items, int maxLength)
		=> items is null || items.Count <= ContentRules.MaxTranslations
			&& items.All(t => SupportedLanguages.TryParse(t.Language, out var l) && l != SupportedLanguages.Default
				&& !string.IsNullOrWhiteSpace(t.Name) && t.Name.Length <= maxLength)
			&& items.Select(t => t.Language.ToLowerInvariant()).Distinct().Count() == items.Count;
}

public sealed class AdminPropertyQueryValidator : AbstractValidator<AdminPropertyQuery>
{
	public AdminPropertyQueryValidator()
	{
		RuleFor(x => x.Status).Must(s => s is null || Codes.TryParse<PropertyStatus>(s, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Search).MaximumLength(CatalogQueryRules.SearchMaxLength);
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => CatalogQueryRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class ChangePropertyStatusRequestValidator : AbstractValidator<ChangePropertyStatusRequest>
{
	public ChangePropertyStatusRequestValidator()
	{
		RuleFor(x => x.Status).Must(s => Codes.TryParse<PropertyStatus>(s, out var v) && v != PropertyStatus.Imported).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Comment).MaximumLength(2000);
	}
}

public sealed class SetPropertyBadgesRequestValidator : AbstractValidator<SetPropertyBadgesRequest>
{
	public SetPropertyBadgesRequestValidator()
	{
		RuleFor(x => x.BadgeIds).NotNull().Must(b => b is null || b.Count <= PlatformRules.MaxBadgesPerProperty).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class UpdateMapIconRequestValidator : AbstractValidator<UpdateMapIconRequest>
{
	public UpdateMapIconRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(MapIcon.NameMaxLength);
		RuleFor(x => x).Must(x => x.IsActive || !x.IsDefault).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("isActive");
	}
}

public sealed class UpdateBeachElementRequestValidator : AbstractValidator<UpdateBeachElementRequest>
{
	public UpdateBeachElementRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
		RuleFor(x => x.SortOrder).InclusiveBetween((short)0, (short)1000);
	}
}

public sealed class SaveBadgeRequestValidator : AbstractValidator<SaveBadgeRequest>
{
	public SaveBadgeRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(ShowcaseBadge.NameMaxLength);
		RuleFor(x => x.Color).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.Color));
		RuleFor(x => x.SortOrder).InclusiveBetween((short)0, (short)1000);
		RuleFor(x => x.Translations).Must(t => PlatformRules.NameTranslations(t, ShowcaseBadge.NameMaxLength)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SaveRegionRequestValidator : AbstractValidator<SaveRegionRequest>
{
	public SaveRegionRequestValidator()
	{
		RuleFor(x => x.CountryCode).Length(2).When(x => x.CountryCode is not null);
		RuleFor(x => x.Name).NotEmpty().MaximumLength(ShowcaseRegion.NameMaxLength);
		RuleFor(x => x.SortOrder).InclusiveBetween((short)0, (short)1000);
		RuleFor(x => x.Translations).Must(t => PlatformRules.NameTranslations(t, ShowcaseRegion.NameMaxLength)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SaveShowcaseRequestValidator : AbstractValidator<SaveShowcaseRequest>
{
	public SaveShowcaseRequestValidator()
	{
		RuleFor(x => x.PropertyIds).NotNull()
			.Must(p => p is null || p.Count <= PlatformRules.MaxShowcaseEntries && p.Distinct().Count() == p.Count).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}
