using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

internal static class OnlineRules
{
	public static bool Surcharge(string type, decimal value)
		=> Codes.TryParse<SurchargeType>(type, out var t) && t switch
		{
			SurchargeType.Percentage => value is >= 0 and <= 100,
			SurchargeType.Fixed => value is >= 0 and <= 10_000,
			_ => true,
		};

	public static bool PaymentMethods(IReadOnlyList<string>? codes)
		=> codes is not null && codes.All(c => Codes.TryParse<OnlinePaymentMethod>(c, out _));

	public static bool NotOverlapping(IReadOnlyList<DateRangeDto>? ranges)
	{
		var sorted = (ranges ?? []).OrderBy(r => r.StartsOn).ToList();
		return sorted.Zip(sorted.Skip(1)).All(p => p.Second.StartsOn > p.First.EndsOn);
	}
}

public sealed class SaveOnlineSettingsRequestValidator : AbstractValidator<SaveOnlineSettingsRequest>
{
	public SaveOnlineSettingsRequestValidator()
	{
		RuleFor(x => x).Must(x => OnlineRules.Surcharge(x.SurchargeType, x.SurchargeValue)).WithErrorCode(ValidationErrorCodes.Invalid)
			.OverridePropertyName(nameof(SaveOnlineSettingsRequest.SurchargeValue));
		RuleFor(x => x.Scope).Must(c => Codes.TryParse<OnlineBookingScope>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.PaymentMethods).Must(OnlineRules.PaymentMethods).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.AllowedSpotIds).Must(i => i is null || i.Count <= 5000).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.AllowedPeriods).Must(p => p is null || p.Count <= 50).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.AllowedPeriods).Must(r => r.EndsOn >= r.StartsOn).WithErrorCode(ValidationErrorCodes.EndBeforeStart);
		RuleFor(x => x.AllowedPeriods).Must(OnlineRules.NotOverlapping).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.MorningTo).GreaterThan(x => x.MorningFrom!.Value).When(x => x.MorningFrom is not null && x.MorningTo is not null);
		RuleFor(x => x.AfternoonTo).GreaterThan(x => x.AfternoonFrom!.Value).When(x => x.AfternoonFrom is not null && x.AfternoonTo is not null);
		RuleFor(x => x.MaxSpotsPerBooking).GreaterThan((short)0).When(x => x.MaxSpotsPerBooking is not null);
		RuleFor(x => x.MaxBookingsPerDay).GreaterThan((short)0).When(x => x.MaxBookingsPerDay is not null);
		RuleFor(x => x.BookingHorizonDays).InclusiveBetween((short)0, (short)730).When(x => x.BookingHorizonDays is not null);
		RuleFor(x => x.UsefulInfo).MaximumLength(10_000);
		RuleFor(x => x.GoodToKnow).MaximumLength(10_000);
	}
}

public sealed class SaveServiceOnlineSettingsRequestValidator : AbstractValidator<SaveServiceOnlineSettingsRequest>
{
	public SaveServiceOnlineSettingsRequestValidator()
	{
		RuleFor(x => x).Must(x => OnlineRules.Surcharge(x.SurchargeType, x.SurchargeValue)).WithErrorCode(ValidationErrorCodes.Invalid)
			.OverridePropertyName(nameof(SaveServiceOnlineSettingsRequest.SurchargeValue));
		RuleFor(x => x.PaymentMethods).Must(OnlineRules.PaymentMethods).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SectionTitle).MaximumLength(255);
		RuleFor(x => x.UsefulInfo).MaximumLength(10_000);
	}
}

public sealed class SaveStayLimitRequestValidator : AbstractValidator<SaveStayLimitRequest>
{
	public SaveStayLimitRequestValidator()
	{
		RuleFor(x => x.SectorId).NotEmpty();
		RuleFor(x => x.EndsOn).GreaterThanOrEqualTo(x => x.StartsOn).WithErrorCode(ValidationErrorCodes.EndBeforeStart);
		RuleFor(x => x).Must(x => x.MinDays is not null || x.MaxDays is not null).WithErrorCode(ValidationErrorCodes.Invalid)
			.OverridePropertyName(nameof(SaveStayLimitRequest.MinDays));
		RuleFor(x => x.MinDays).InclusiveBetween((short)1, (short)366).When(x => x.MinDays is not null);
		RuleFor(x => x.MaxDays).InclusiveBetween((short)1, (short)366).When(x => x.MaxDays is not null);
		RuleFor(x => x.MaxDays).GreaterThanOrEqualTo(x => x.MinDays!.Value).When(x => x.MinDays is not null && x.MaxDays is not null);
	}
}

public sealed class SaveLegendRequestValidator : AbstractValidator<SaveLegendRequest>
{
	public SaveLegendRequestValidator()
	{
		RuleFor(x => x.Items).NotNull().Must(i => i.Count <= 30).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Items).ChildRules(i => i.RuleFor(x => x.Label).NotEmpty().MaximumLength(MapLegendItem.LabelMaxLength));
	}
}
