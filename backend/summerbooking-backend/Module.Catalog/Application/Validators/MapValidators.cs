using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

public sealed class SaveMapSettingsRequestValidator : AbstractValidator<SaveMapSettingsRequest>
{
	public SaveMapSettingsRequestValidator()
	{
		RuleFor(x => x.Rows).InclusiveBetween((short)0, MapLayout.MaxRows);
		RuleFor(x => x.Columns).InclusiveBetween((short)0, MapLayout.MaxColumns);
		RuleFor(x => x.TiltDegrees).InclusiveBetween((short)-MapLayout.MaxTiltDegrees, MapLayout.MaxTiltDegrees);
		RuleFor(x => x.BackgroundColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => x.BackgroundColor is not null);
	}
}

public sealed class FillMapCellsRequestValidator : AbstractValidator<FillMapCellsRequest>
{
	/// <summary>Caselle o postazioni in una sola richiesta: al più tutta la griglia.</summary>
	public const int MaxCells = MapLayout.MaxRows * MapLayout.MaxColumns;

	public FillMapCellsRequestValidator()
	{
		RuleFor(x => x.Cells).NotEmpty().Must(c => c.Count <= MaxCells).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Cells).Must(c => c.Distinct().Count() == c.Count).WithErrorCode(ValidationErrorCodes.Invalid)
			.When(x => x.Cells is { Count: > 1 });
		RuleFor(x => x.SpotType).Must(c => Codes.TryParse<SpotType>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);

		When(x => x.SpotType == Codes.Of(SpotType.BeachElement), () =>
		{
			RuleFor(x => x.BeachElementId).NotNull();
		}).Otherwise(() =>
		{
			RuleFor(x => x.FirstLabel).NotEmpty().MaximumLength(MapSpot.LabelMaxLength);
			RuleFor(x => x.AreaSeatCount).NotNull().GreaterThan((short)0).When(x => Codes.TryParse<SpotType>(x.SpotType, out var type) && MapSpot.IsAreaType(type));
		});
	}
}

public sealed class UpdateMapSpotRequestValidator : AbstractValidator<UpdateMapSpotRequest>
{
	public UpdateMapSpotRequestValidator()
	{
		RuleFor(x => x.Label).NotEmpty().MaximumLength(MapSpot.LabelMaxLength);
		RuleFor(x => x.AreaSeatCount).GreaterThan((short)0).When(x => x.AreaSeatCount is not null);
	}
}

public sealed class PlaceMapSpotsRequestValidator : AbstractValidator<PlaceMapSpotsRequest>
{
	public PlaceMapSpotsRequestValidator()
	{
		RuleFor(x => x.Items).NotEmpty().Must(i => i.Count <= FillMapCellsRequestValidator.MaxCells).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Items).ChildRules(item =>
		{
			item.RuleFor(i => i.RotationDegrees).Must(r => r is >= 0 and < 360 && r % MapSpot.RotationStep == 0)
				.WithErrorCode(ValidationErrorCodes.Invalid);
			item.RuleFor(i => i.OffsetX).InclusiveBetween((short)-MapSpot.MaxOffset, MapSpot.MaxOffset);
			item.RuleFor(i => i.OffsetY).InclusiveBetween((short)-MapSpot.MaxOffset, MapSpot.MaxOffset);
		});
	}
}
