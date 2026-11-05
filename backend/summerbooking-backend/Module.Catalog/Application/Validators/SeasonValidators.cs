using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Shared.Contracts.Errors;

namespace Module.Catalog.Application.Validators;

public sealed class SaveSeasonRequestValidator : AbstractValidator<SaveSeasonRequest>
{
	public SaveSeasonRequestValidator()
	{
		RuleFor(x => x.EndsOn).GreaterThanOrEqualTo(x => x.StartsOn).WithErrorCode(ValidationErrorCodes.EndBeforeStart);
		RuleFor(x => x.NextEndsOn).NotNull().When(x => x.NextStartsOn is not null);
		RuleFor(x => x.NextStartsOn).NotNull().When(x => x.NextEndsOn is not null);
		RuleFor(x => x.NextStartsOn).GreaterThan(x => x.EndsOn).When(x => x.NextStartsOn is not null);
		RuleFor(x => x.NextEndsOn).GreaterThanOrEqualTo(x => x.NextStartsOn!.Value).WithErrorCode(ValidationErrorCodes.EndBeforeStart)
			.When(x => x.NextStartsOn is not null && x.NextEndsOn is not null);
	}
}

public sealed class SaveTariffPeriodRequestValidator : AbstractValidator<SaveTariffPeriodRequest>
{
	public const int MaxRanges = 20;

	public SaveTariffPeriodRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(TariffPeriod.NameMaxLength);
		RuleFor(x => x.Ranges).NotEmpty().Must(r => r.Count <= MaxRanges).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Ranges).Must(r => r.EndsOn >= r.StartsOn).WithErrorCode(ValidationErrorCodes.EndBeforeStart);
		RuleFor(x => x.Ranges).Must(NotOverlap).WithErrorCode(ValidationErrorCodes.Invalid).When(x => x.Ranges is { Count: > 1 });
	}

	private static bool NotOverlap(IReadOnlyList<DateRangeDto> ranges)
	{
		var sorted = ranges.OrderBy(r => r.StartsOn).ToList();
		return sorted.Zip(sorted.Skip(1)).All(p => p.Second.StartsOn > p.First.EndsOn);
	}
}

public sealed class SaveSectorRequestValidator : AbstractValidator<SaveSectorRequest>
{
	public SaveSectorRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(MapSector.NameMaxLength);
	}
}
