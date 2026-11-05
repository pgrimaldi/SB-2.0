using FluentValidation;
using Module.Catalog.Application.Dtos;
using Shared.Contracts.Errors;
using Shared.Dtos;

namespace Module.Catalog.Application.Validators;

public static class PublicRules
{
	public static readonly IReadOnlySet<string> SortFields = new HashSet<string> { "popularity", "price", "distance", "name" };
	public const int MaxSearchDays = 92;
}

public sealed class PublicSearchQueryValidator : AbstractValidator<PublicSearchQuery>
{
	public PublicSearchQueryValidator()
	{
		RuleFor(x => x.Text).MaximumLength(CatalogQueryRules.SearchMaxLength);
		RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude is not null);
		RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude is not null);
		RuleFor(x => x).Must(x => (x.Latitude is null) == (x.Longitude is null)).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("longitude");
		RuleFor(x => x).Must(x => (x.From is null) == (x.To is null)).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("to");
		RuleFor(x => x).Must(x => x.From is null || x.To >= x.From && x.To.Value.DayNumber - x.From.Value.DayNumber < PublicRules.MaxSearchDays)
			.WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("to");
		RuleFor(x => x.Amenities).MaximumLength(500);
		RuleFor(x => x.SortBy).Must(s => s is null || PublicRules.SortFields.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x).Must(x => x.SortBy != "distance" || x.Latitude is not null).WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("latitude");
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
	}
}
