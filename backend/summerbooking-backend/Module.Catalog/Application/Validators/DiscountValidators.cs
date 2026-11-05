using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

public sealed class DiscountQueryValidator : AbstractValidator<DiscountQuery>
{
	public DiscountQueryValidator()
	{
		RuleFor(x => x.Search).MaximumLength(CatalogQueryRules.SearchMaxLength);
		RuleFor(x => x.SortBy).Must(s => s is null || DiscountSortFields.All.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => CatalogQueryRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SaveDiscountRequestValidator : AbstractValidator<SaveDiscountRequest>
{
	public SaveDiscountRequestValidator()
	{
		RuleFor(x => x.Code).NotEmpty().MaximumLength(Discount.CodeMaxLength);
		RuleFor(x => x.Name).MaximumLength(255);
		RuleFor(x => x.Type).Must(t => Codes.TryParse<DiscountType>(t, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Value).GreaterThan(0m).LessThanOrEqualTo(100m).When(x => x.Type == Codes.Of(DiscountType.Percentage));
		RuleFor(x => x.Value).GreaterThan(0m).LessThanOrEqualTo(100_000m).When(x => x.Type == Codes.Of(DiscountType.Fixed));
		RuleFor(x => x.Scope).Must(s => Codes.TryParse<DiscountScope>(s, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.ServiceCategories).Must(c => c is null || c.All(v => Codes.TryParse<DiscountServiceCategory>(v, out _))).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x).Must(x => x.Scope == Codes.Of(DiscountScope.All) || (x.ServiceCategories?.Count ?? 0) + (x.SectorIds?.Count ?? 0) > 0)
			.WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("serviceCategories");
		RuleFor(x => x.Channel).Must(c => Codes.TryParse<DiscountApplicableChannel>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.MaxUses).GreaterThan(0).When(x => x.MaxUses is not null);
		RuleFor(x => x.Periods).Must(p => p is null || p.Count <= 50 && p.All(r => r.EndsOn >= r.StartsOn)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Periods).NotEmpty().When(x => !x.RequiresCode).WithErrorCode(ValidationErrorCodes.Required);
		RuleFor(x => x.Weekdays).Must(ContentRules.Weekdays).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.TimeRanges).Must(t => t is null || t.Count <= 20 && t.All(r => r.To > r.From)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}
