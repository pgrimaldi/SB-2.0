using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

public static class RevenueRules
{
	/// <summary>Giorni calcolabili in una volta (il meteo è al più di 16 giorni, lo storico arriva fin dove serve).</summary>
	public const int MaxComputeDays = 92;
	public const int MaxApplyItems = 2000;
	public const int MaxStandardPeriods = 24;

	public static bool ValidDay(short month, short day) => month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(2024, month);

	/// <summary>Giorni dell'anno coperti (anno bisestile, così il 29 febbraio c'è).</summary>
	public static IEnumerable<int> Days(RevenueStandardPeriodDto p)
	{
		var start = new DateOnly(2024, p.StartMonth, p.StartDay).DayOfYear;
		var end = new DateOnly(2024, p.EndMonth, p.EndDay).DayOfYear;
		return start <= end ? Enumerable.Range(start, end - start + 1) : Enumerable.Range(start, 366 - start + 1).Concat(Enumerable.Range(1, end));
	}
}

public sealed class SaveRevenueSettingsRequestValidator : AbstractValidator<SaveRevenueSettingsRequest>
{
	public SaveRevenueSettingsRequestValidator()
	{
		RuleFor(x => x.DailyBannerCount).InclusiveBetween((short)0, (short)100);
		RuleFor(x => x.SpotIds).Must(s => s is null || s.Count <= 5000).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SuggestedPriceQueryValidator : AbstractValidator<SuggestedPriceQuery>
{
	public SuggestedPriceQueryValidator()
	{
		RuleFor(x => x.From).NotNull();
		RuleFor(x => x.To).NotNull();
		RuleFor(x => x).Must(x => x.From is null || x.To is null || x.To >= x.From && x.To.Value.DayNumber - x.From.Value.DayNumber < 366)
			.WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("to");
	}
}

public sealed class ComputeSuggestedPricesRequestValidator : AbstractValidator<ComputeSuggestedPricesRequest>
{
	public ComputeSuggestedPricesRequestValidator()
	{
		RuleFor(x => x).Must(x => x.To >= x.From && x.To.DayNumber - x.From.DayNumber < RevenueRules.MaxComputeDays)
			.WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("to");
	}
}

public sealed class ApplySuggestedPricesRequestValidator : AbstractValidator<ApplySuggestedPricesRequest>
{
	public ApplySuggestedPricesRequestValidator()
	{
		RuleFor(x => x.Items).NotEmpty().Must(i => i is null || i.Count <= RevenueRules.MaxApplyItems).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Items).ChildRules(item =>
		{
			item.RuleFor(i => i.Mode).Must(m => Codes.TryParse<SuggestedPriceMode>(m, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
			item.RuleFor(i => i.DiscountPercent).NotNull().GreaterThan(0m).LessThan(100m).When(i => i.Mode == Codes.Of(SuggestedPriceMode.Discount));
		});
	}
}

public sealed class RevenueWeightsDtoValidator : AbstractValidator<RevenueWeightsDto>
{
	public RevenueWeightsDtoValidator()
	{
		RuleFor(x => x.RainLowSeasonFactor).GreaterThan(0m).LessThanOrEqualTo(5m);
		RuleFor(x => x.RainHighSeasonFactor).GreaterThan(0m).LessThanOrEqualTo(5m);
		RuleFor(x => x.BeforeWeekendFactor).GreaterThan(0m).LessThanOrEqualTo(5m);
		RuleFor(x => x.HighSeasonFactor).GreaterThan(0m).LessThanOrEqualTo(5m);
		RuleFor(x => x.FreeSpotsAboveFactor).GreaterThan(0m).LessThanOrEqualTo(5m);
		RuleFor(x => x.FreeSpotsBelowFactor).GreaterThan(0m).LessThanOrEqualTo(5m);
		RuleFor(x => x.FreeSpotsThresholdPercent).InclusiveBetween(0m, 100m);
	}
}

public sealed class SaveRevenueStandardPeriodsRequestValidator : AbstractValidator<SaveRevenueStandardPeriodsRequest>
{
	public SaveRevenueStandardPeriodsRequestValidator()
	{
		RuleFor(x => x.Periods).NotNull().Must(p => p is null || p.Count <= RevenueRules.MaxStandardPeriods).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Periods).ChildRules(p =>
		{
			p.RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
			p.RuleFor(x => x.Tier).Must(t => Codes.TryParse<RevenueSeasonTier>(t, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
			p.RuleFor(x => x).Must(x => RevenueRules.ValidDay(x.StartMonth, x.StartDay) && RevenueRules.ValidDay(x.EndMonth, x.EndDay))
				.WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("startDay");
		});
		// due periodi non possono avere giorni in comune
		RuleFor(x => x.Periods)
			.Must(p => p is null || p.Where(x => RevenueRules.ValidDay(x.StartMonth, x.StartDay) && RevenueRules.ValidDay(x.EndMonth, x.EndDay))
				.SelectMany(RevenueRules.Days).GroupBy(d => d).All(g => g.Count() == 1))
			.WithErrorCode(ValidationErrorCodes.Invalid).WithMessage("overlap");
	}
}
