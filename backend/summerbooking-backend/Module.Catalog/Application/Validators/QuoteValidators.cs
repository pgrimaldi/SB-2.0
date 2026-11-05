using FluentValidation;
using Module.Catalog.Application.Dtos;
using Shared.Contracts.Errors;

namespace Module.Catalog.Application.Validators;

public static class QuoteRules
{
	public const int MaxDays = 366;
	public const int MaxSpots = 50;
	public const int MaxServices = 100;
	public static readonly IReadOnlySet<string> DayParts = new HashSet<string> { "full_day", "morning", "afternoon" };
}

public sealed class QuoteRequestValidator : AbstractValidator<QuoteRequest>
{
	public QuoteRequestValidator()
	{
		RuleFor(x => x).Must(x => x.To >= x.From && x.To.DayNumber - x.From.DayNumber < QuoteRules.MaxDays)
			.WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("to");
		RuleFor(x => x.DayPart).Must(p => p is null || QuoteRules.DayParts.Contains(p)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x).Must(x => (x.Spots?.Count ?? 0) + (x.Services?.Count ?? 0) > 0).WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("spots");
		RuleFor(x => x.Spots).Must(s => s is null || s.Count <= QuoteRules.MaxSpots).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Services).Must(s => s is null || s.Count <= QuoteRules.MaxServices).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.DiscountCode).MaximumLength(100);
		RuleForEach(x => x.Spots).ChildRules(spot =>
		{
			spot.RuleFor(s => s).Must(s => (s.PriceListId is null) != (s.AgreementPriceListId is null))
				.WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("priceListId");
			spot.RuleFor(s => s).Must(s => s.SpotId is not null || s.SectorId is not null).WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("spotId");
			spot.RuleFor(s => s.ManualPrice).GreaterThanOrEqualTo(0m).LessThanOrEqualTo(1_000_000m).When(s => s.ManualPrice is not null);
			spot.RuleFor(s => s.Absences).Must(a => a is null || a.Count <= QuoteRules.MaxDays).WithErrorCode(ValidationErrorCodes.Invalid);
			spot.RuleForEach(s => s.Additionals).ChildRules(a =>
			{
				a.RuleFor(x => x.Quantity).InclusiveBetween((short)1, (short)100);
				a.RuleFor(x => x.Dates).Must(d => d is null || d.Count <= QuoteRules.MaxDays).WithErrorCode(ValidationErrorCodes.Invalid);
			});
		});
		RuleForEach(x => x.Services).ChildRules(s =>
		{
			s.RuleFor(x => x.Quantity).InclusiveBetween((short)1, (short)1000);
			s.RuleFor(x => x.Variant).MaximumLength(120);
		});
	}
}
