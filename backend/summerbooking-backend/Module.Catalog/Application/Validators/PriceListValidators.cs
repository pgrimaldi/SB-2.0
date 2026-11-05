using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Localization;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

public sealed class PriceListQueryValidator : AbstractValidator<PriceListQuery>
{
	public PriceListQueryValidator()
	{
		RuleFor(x => x.Kind).Must(k => k is null || Codes.TryParse<PriceListKind>(k, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Search).MaximumLength(CatalogQueryRules.SearchMaxLength);
		RuleFor(x => x.SortBy).Must(s => s is null || PriceListSortFields.All.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => CatalogQueryRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SavePriceListRequestValidator : AbstractValidator<SavePriceListRequest>
{
	public const decimal MaxPrice = 1_000_000m;

	public SavePriceListRequestValidator()
	{
		RuleFor(x => x.Kind).Must(k => Codes.TryParse<PriceListKind>(k, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Name).NotEmpty().MaximumLength(PriceList.NameMaxLength);
		RuleFor(x => x.Translations).Must(t => t is null || t.Count <= ContentRules.MaxTranslations
				&& t.All(i => SupportedLanguages.TryParse(i.Language, out var l) && l != SupportedLanguages.Default && !string.IsNullOrWhiteSpace(i.Name) && i.Name.Length <= 255)
				&& t.Select(i => i.Language.ToLowerInvariant()).Distinct().Count() == t.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.MaxPeople).InclusiveBetween((short)1, (short)100).When(x => x.MaxPeople is not null);
		RuleFor(x => x.MaxAdditionalItems).InclusiveBetween((short)0, (short)100).When(x => x.MaxAdditionalItems is not null);
		RuleFor(x => x.Items).Must(i => i is null || i.Count <= 50 && i.All(x => x.Quantity is > 0 and <= 100)
				&& i.Select(x => x.InventoryItemId).Distinct().Count() == i.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Items).Must(i => i is { Count: 1 }).WithErrorCode(ValidationErrorCodes.Invalid)
			.When(x => x.Kind == Codes.Of(PriceListKind.Additional));
		RuleFor(x => x.PackageLinks).Must(l => l is null || l.All(x => x.MaxCount is > 0 and <= 100) && l.Select(x => x.PackageId).Distinct().Count() == l.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SalesChannel).Must(c => Codes.TryParse<SalesChannel>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.MonthlySetting).Must(c => c is null || Codes.TryParse<MonthlySetting>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.WeekendSetting).Must(c => c is null || Codes.TryParse<WeekendSetting>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x).Must(x => ContentRules.Vat(x.VatRate, x.VatNature)).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("vatRate");
		RuleFor(x => x.HolidayDates).Must(d => d is null || d.Count <= 100).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SectorPrices).Must(p => p is null || p.All(x => Price(x.SeasonalPrice) && Price(x.SeasonalHalfDayPrice))
				&& p.Select(x => x.SectorId).Distinct().Count() == p.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.PeriodPrices).Must(p => p is null || p.Count <= 500
				&& p.Select(x => (x.SectorId, x.PeriodKey)).Distinct().Count() == p.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.PeriodPrices).ChildRules(p =>
		{
			p.RuleFor(i => new[] { i.HalfDay, i.HalfDayWeekend, i.Daily, i.Weekend, i.HalfDay7, i.Days7, i.HalfDay14, i.Days14, i.HalfDayMonthly, i.Monthly })
				.Must(v => v.All(Price)).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("prices");
			p.RuleFor(i => i.StayOverrides).Must(o => o is null || o.All(s => s.StayDays is >= 1 and <= PriceListStayPrice.MaxStayDays && Price(s.Price))
					&& o.Select(s => s.StayDays).Distinct().Count() == o.Count)
				.WithErrorCode(ValidationErrorCodes.Invalid);
		});
		RuleFor(x => x.ReducedPrices).Must(r => r is null || r.All(x => Price(x.Price)) && r.Select(x => (x.ReducedSeasonId, x.SectorId)).Distinct().Count() == r.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
	}

	private static bool Price(decimal? value) => value is null or >= 0 and <= MaxPrice;
}

public sealed class SaveReducedSeasonRequestValidator : AbstractValidator<SaveReducedSeasonRequest>
{
	public SaveReducedSeasonRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(ReducedSeason.NameMaxLength);
		RuleFor(x => x.EndsOn).GreaterThanOrEqualTo(x => x.StartsOn).WithErrorCode(ValidationErrorCodes.EndBeforeStart);
	}
}
