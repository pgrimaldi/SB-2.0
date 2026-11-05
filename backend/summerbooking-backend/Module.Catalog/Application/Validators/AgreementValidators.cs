using BuildingBlocks.Application.Validation;
using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

public sealed class AgreementPartnerQueryValidator : AbstractValidator<AgreementPartnerQuery>
{
	public AgreementPartnerQueryValidator()
	{
		RuleFor(x => x.Search).MaximumLength(CatalogQueryRules.SearchMaxLength);
		RuleFor(x => x.SortBy).Must(s => s is null || AgreementPartnerSortFields.All.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => CatalogQueryRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SaveAgreementPartnerRequestValidator : AbstractValidator<SaveAgreementPartnerRequest>
{
	public SaveAgreementPartnerRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(AgreementPartner.NameMaxLength);
		RuleFor(x => x.City).MaximumLength(255);
		RuleFor(x => x.PostalCode).MaximumLength(16);
		RuleFor(x => x.Province).MaximumLength(255);
		RuleFor(x => x.StreetName).MaximumLength(255);
		RuleFor(x => x.StreetNumber).MaximumLength(32);
		RuleFor(x => x.Phone).PhoneNumber();
		RuleFor(x => x.Email).OptionalEmail().When(x => !string.IsNullOrEmpty(x.Email));
		RuleFor(x => x.Note).MaximumLength(10_000);
	}
}

public sealed class SaveAgreementPriceListRequestValidator : AbstractValidator<SaveAgreementPriceListRequest>
{
	public SaveAgreementPriceListRequestValidator()
	{
		RuleFor(x => x.ListType).Must(t => Codes.TryParse<AgreementListType>(t, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Kind).Must(k => Codes.TryParse<PriceListKind>(k, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Name).NotEmpty().MaximumLength(PriceList.NameMaxLength);
		RuleFor(x => x.MaxPeople).InclusiveBetween((short)1, (short)100).When(x => x.MaxPeople is not null);
		RuleFor(x => x.Items).Must(i => i is null || i.Count <= 50 && i.All(x => x.Quantity is > 0 and <= 100) && i.Select(x => x.InventoryItemId).Distinct().Count() == i.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Items).Must(i => i is { Count: 1 }).WithErrorCode(ValidationErrorCodes.Invalid).When(x => x.Kind == Codes.Of(PriceListKind.Additional));
		RuleFor(x => x.PackageLinks).Must(l => l is null || l.All(x => x.MaxCount is > 0 and <= 100) && l.Select(x => x.PackageId).Distinct().Count() == l.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.MonthlySetting).Must(c => c is null || Codes.TryParse<MonthlySetting>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.WeekendSetting).Must(c => c is null || Codes.TryParse<WeekendSetting>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x).Must(x => ContentRules.Vat(x.VatRate, x.VatNature)).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("vatRate");
		RuleFor(x => x.MinBookingDays).InclusiveBetween((short)1, (short)366).When(x => x.MinBookingDays is not null);
		RuleFor(x => x.MaxVoucherQuantity).InclusiveBetween((short)1, (short)1000).When(x => x.MaxVoucherQuantity is not null);
		RuleFor(x => x.HolidayDates).Must(d => d is null || d.Count <= 100).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SectorPrices).Must(p => p is null || p.All(x => x.SeasonalPrice is null or >= 0 && x.SeasonalHalfDayPrice is null or >= 0)
				&& p.Select(x => x.SectorId).Distinct().Count() == p.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.PeriodPrices).Must(p => p is null || p.Count <= 500 && p.Select(x => (x.SectorId, x.PeriodKey)).Distinct().Count() == p.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.PeriodPrices).ChildRules(p =>
		{
			p.RuleFor(i => new[] { i.HalfDay, i.HalfDayWeekend, i.Daily, i.Weekend, i.HalfDay7, i.Days7, i.HalfDay14, i.Days14, i.HalfDayMonthly, i.Monthly })
				.Must(v => v.All(x => x is null or >= 0)).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("prices");
			p.RuleFor(i => i.StayOverrides).Must(o => o is null || o.All(s => s.StayDays is >= 1 and <= PriceListStayPrice.MaxStayDays && s.Price >= 0)
				&& o.Select(s => s.StayDays).Distinct().Count() == o.Count).WithErrorCode(ValidationErrorCodes.Invalid);
		});
	}
}

public sealed class AgreementReportQueryValidator : AbstractValidator<AgreementReportQuery>
{
	public AgreementReportQueryValidator()
	{
		RuleFor(x => x.Status).Must(s => s is null || Codes.TryParse<AgreementReportStatus>(s, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => CatalogQueryRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}
