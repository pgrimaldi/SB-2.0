using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Localization;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

/// <summary>Regole comuni ai contenuti venduti: IVA, traduzioni, giorni e fasce.</summary>
internal static class ContentRules
{
	public const int MaxTranslations = 6;

	/// <summary>Un'aliquota oppure la natura dell'esenzione, mai entrambe.</summary>
	public static bool Vat(decimal? rate, string? nature)
		=> rate is null != nature is null
			&& (rate is null || rate is > 0 and < 100)
			&& (nature is null || Codes.TryParse<VatNature>(nature, out _));

	/// <summary>Lingue gestite diverse dall'italiano (la lingua del nome principale), senza doppioni.</summary>
	public static bool Translations(IReadOnlyList<TranslationDto>? items)
		=> items is null || items.Count <= MaxTranslations
			&& items.All(t => SupportedLanguages.TryParse(t.Language, out var l) && l != SupportedLanguages.Default
				&& !string.IsNullOrWhiteSpace(t.Name) && t.Name.Length <= 255 && (t.Description?.Length ?? 0) <= 10_000)
			&& items.Select(t => t.Language.ToLowerInvariant()).Distinct().Count() == items.Count;

	public static bool Weekdays(IReadOnlyList<string>? days)
		=> days is null || days.All(d => Codes.TryParse<Weekday>(d, out _));

	public static bool TimeRange(TimeOnly? from, TimeOnly? to) => from is null || to is null || to > from;

	/// <summary>Durate standard dei servizi orari, in minuti.</summary>
	public static readonly IReadOnlySet<short> HourlyDurations = new HashSet<short> { 10, 15, 30, 45, 60, 90, 120, 180, 240, 300, 360 };

	public const int MaxHourlyOptions = 20;

	/// <summary>Tutte le disponibilità di un servizio orario, dei prezzi e delle opzioni di prenotazione.</summary>
	public static IEnumerable<HourlyOptionDto> HourlyOptions(SaveServiceRequest r)
		=> (r.HourlyPrices ?? []).SelectMany(p => p.Options ?? []).Concat((r.BookingOptions ?? []).Where(o => o.Hourly is not null).Select(o => o.Hourly!));
}

/// <summary>Disponibilità di un servizio orario.</summary>
public sealed class HourlyOptionValidator : AbstractValidator<HourlyOptionDto>
{
	public HourlyOptionValidator()
	{
		RuleFor(x => x).Must(x => ContentRules.TimeRange(x.TimeFrom, x.TimeTo)).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("timeTo");
		RuleFor(x => x.Weekdays).Must(ContentRules.Weekdays).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Slots).NotNull().Must(s => s.Count <= ContentRules.HourlyDurations.Count
				&& s.All(i => ContentRules.HourlyDurations.Contains(i.DurationMinutes)) && s.Select(i => i.DurationMinutes).Distinct().Count() == s.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Slots).ChildRules(s => s.RuleFor(i => i.Price).InclusiveBetween(0m, 100_000m).When(i => i.Price is not null));
		RuleFor(x => x.Slots).Must(s => s.All(i => !i.IsEnabled || i.Price is not null)).WithErrorCode(ValidationErrorCodes.Required)
			.When(x => x.Slots is not null);
		RuleFor(x => x.CustomSlot!.DurationMinutes).InclusiveBetween((short)1, (short)1440).When(x => x.CustomSlot is not null);
		RuleFor(x => x.CustomSlot!.Price).NotNull().InclusiveBetween(0m, 100_000m).When(x => x.CustomSlot is { IsEnabled: true });
		RuleFor(x => x.VatRate).ExclusiveBetween(0m, 100m).When(x => x.VatRate is not null);
		RuleFor(x => x.MaxPerSlot).GreaterThan((short)0).When(x => x.MaxPerSlot is not null);
	}
}

public sealed class ServiceListQueryValidator : AbstractValidator<ServiceListQuery>
{
	public ServiceListQueryValidator()
	{
		RuleFor(x => x.Kind).Must(k => k is null || Codes.TryParse<ServiceSaleKind>(k, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Search).MaximumLength(CatalogQueryRules.SearchMaxLength);
		RuleFor(x => x.SortBy).Must(s => s is null || ServiceSortFields.All.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => CatalogQueryRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SaveServiceRequestValidator : AbstractValidator<SaveServiceRequest>
{
	public SaveServiceRequestValidator()
	{
		RuleFor(x => x.Kind).Must(k => Codes.TryParse<ServiceSaleKind>(k, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Name).NotEmpty().MaximumLength(ServiceSaleItem.NameMaxLength);
		RuleFor(x => x.Description).MaximumLength(10_000);
		RuleFor(x => x.Category).Must(c => c is null || Codes.TryParse<BarCategory>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Category).Null().When(x => x.Kind != Codes.Of(ServiceSaleKind.Bar));
		RuleFor(x => x.Price).InclusiveBetween(0m, 100_000m);
		// servizi orari: l'IVA può stare solo nelle disponibilità, se ce l'hanno tutte
		RuleFor(x => x).Must(x => ContentRules.Vat(x.VatRate, x.VatNature)
				|| x.VatRate is null && x.VatNature is null && x.Kind == Codes.Of(ServiceSaleKind.Hourly)
				&& ContentRules.HourlyOptions(x).Any() && ContentRules.HourlyOptions(x).All(o => o.VatRate is not null))
			.WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("vatRate");
		RuleFor(x => x.Translations).Must(ContentRules.Translations).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Allergens).Must(a => a is null || a.All(c => Codes.TryParse<Allergen>(c, out _))).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.PeriodPrices).Must(p => p is null || p.All(x => x.Price is >= 0 and <= 100_000) && p.Select(x => x.PeriodKey).Distinct().Count() == p.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SectorIds).Must(s => s is null || s.Count <= 500).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Variants).Must(v => v is null || v.Count <= ServiceSaleItem.MaxVariants).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Variants).ChildRules(v =>
		{
			v.RuleFor(i => i.Name).NotEmpty().MaximumLength(ServiceSaleItemVariant.NameMaxLength);
			v.RuleFor(i => i.Price).InclusiveBetween(0m, 100_000m);
		});
		RuleFor(x => x.BookingOptions).Must(o => o is null || o.Count <= ServiceSaleItem.MaxBookingOptions).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.BookingOptions).ChildRules(o =>
		{
			o.RuleFor(i => i).Must(i => i.StartsOn is null || i.EndsOn is null || i.EndsOn >= i.StartsOn).WithErrorCode(ValidationErrorCodes.EndBeforeStart)
				.OverridePropertyName("endsOn");
			o.RuleFor(i => i.Weekdays).Must(ContentRules.Weekdays).WithErrorCode(ValidationErrorCodes.Invalid);
			o.RuleFor(i => i).Must(i => ContentRules.TimeRange(i.ManagementFrom, i.ManagementTo) && ContentRules.TimeRange(i.OnlineFrom, i.OnlineTo))
				.WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("hours");
			o.RuleFor(i => i).Must(i => i.MinItems is null || i.MaxItems is null || i.MaxItems >= i.MinItems).WithErrorCode(ValidationErrorCodes.Invalid)
				.OverridePropertyName("maxItems");
			o.RuleFor(i => new[] { i.MaxPerHour, i.MaxPerDay, i.MaxPerSlot, i.IntervalMinutes, i.MinItems, i.MaxItems })
				.Must(values => values.All(v => v is null or > 0)).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("limits");
		});
		RuleFor(x => x.HourlyPrices).Empty().When(x => x.Kind != Codes.Of(ServiceSaleKind.Hourly));
		RuleFor(x => x.HourlyPrices).Must(h => h is null || h.Count <= 50 && h.Select(p => p.PeriodKey).Distinct().Count() == h.Count
				&& h.All(p => p.Options is { Count: <= ContentRules.MaxHourlyOptions }))
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x).Must(x => (x.HourlyPrices ?? []).All(p => p.PeriodKey is null != x.HasPeriodPrices))
			.WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("hourlyPrices").When(x => x.Kind == Codes.Of(ServiceSaleKind.Hourly));
		RuleForEach(x => x.HourlyPrices).ChildRules(p => p.RuleForEach(i => i.Options).SetValidator(new HourlyOptionValidator()));
		RuleForEach(x => x.BookingOptions).ChildRules(o => o.RuleFor(i => i.Hourly!).SetValidator(new HourlyOptionValidator()).When(i => i.Hourly is not null));
		RuleFor(x => x.BookingOptions).Must(o => o is null || o.All(i => i.Hourly is null)).WithErrorCode(ValidationErrorCodes.Invalid)
			.When(x => x.Kind != Codes.Of(ServiceSaleKind.Hourly));
	}
}

public sealed class SaveAutomaticPriceRequestValidator : AbstractValidator<SaveAutomaticPriceRequest>
{
	public SaveAutomaticPriceRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
		RuleFor(x => x.Description).MaximumLength(10_000);
		RuleFor(x => x.Price).InclusiveBetween(0m, 100_000m);
		RuleFor(x => x).Must(x => ContentRules.Vat(x.VatRate, x.VatNature)).WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("vatRate");
		RuleFor(x => x.Translations).Must(ContentRules.Translations).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.ServiceIds).NotEmpty().When(x => !x.AppliesToAllServices);
		RuleFor(x => x.Periods).Must(p => p is null || p.Count <= 50).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Periods).ChildRules(p =>
		{
			p.RuleFor(i => i).Must(i => i.StartsOn is null || i.EndsOn is null || i.EndsOn >= i.StartsOn).WithErrorCode(ValidationErrorCodes.EndBeforeStart)
				.OverridePropertyName("endsOn");
			p.RuleFor(i => i.Weekdays).Must(ContentRules.Weekdays).WithErrorCode(ValidationErrorCodes.Invalid);
			p.RuleFor(i => i.TimeRanges).Must(t => t is null || t.Count <= 20 && t.All(r => r.To > r.From)).WithErrorCode(ValidationErrorCodes.Invalid);
		});
	}
}

public sealed class SaveTableServiceRequestValidator : AbstractValidator<SaveTableServiceRequest>
{
	public SaveTableServiceRequestValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
		RuleFor(x => x.Description).MaximumLength(10_000);
		RuleFor(x => x.Translations).Must(ContentRules.Translations).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.CapacityMode).Must(c => Codes.TryParse<TableCapacityMode>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.TableTypes).NotEmpty().When(x => x.CapacityMode == Codes.Of(TableCapacityMode.Tables));
		RuleFor(x => x.TableTypes).Must(t => t is null || t.All(i => i.Seats is > 0 and <= 100 && i.Count is > 0 and <= 500)
				&& t.Select(i => i.Seats).Distinct().Count() == t.Count)
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.TotalSeats).NotNull().InclusiveBetween((short)1, (short)10_000).When(x => x.CapacityMode == Codes.Of(TableCapacityMode.Seats));
		RuleFor(x => x.AverageMealMinutes).InclusiveBetween((short)10, (short)600);
		RuleFor(x => x.MaxBookingsPerSlot).GreaterThan((short)0).When(x => x.MaxBookingsPerSlot is not null);
		RuleFor(x => x.MinSeatsPerBooking).GreaterThan((short)0).When(x => x.MinSeatsPerBooking is not null);
		RuleFor(x => x.MaxSeatsPerBooking).GreaterThanOrEqualTo(x => x.MinSeatsPerBooking ?? 1).When(x => x.MaxSeatsPerBooking is not null);
		RuleFor(x => x.DepositMode).Must(c => Codes.TryParse<DepositMode>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		When(x => x.DepositMode != Codes.Of(DepositMode.None), () =>
		{
			RuleFor(x => x.DepositType).NotNull().Must(c => Codes.TryParse<DepositType>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
			RuleFor(x => x.DepositValue).NotNull().GreaterThan(0m);
			RuleFor(x => x.DepositValue).LessThanOrEqualTo(100m).When(x => x.DepositType == Codes.Of(DepositType.Percentage));
		});
		RuleFor(x => x.Availabilities).Must(a => a is null || a.Count <= 20).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Availabilities).ChildRules(a =>
		{
			a.RuleFor(i => i.EndsOn).GreaterThanOrEqualTo(i => i.StartsOn).WithErrorCode(ValidationErrorCodes.EndBeforeStart);
			a.RuleFor(i => i.Slots).NotEmpty().Must(s => s.Count <= 50 && s.All(x => x.To > x.From
				&& (x.Weekday is null || Codes.TryParse<Weekday>(x.Weekday, out _)))).WithErrorCode(ValidationErrorCodes.Invalid);
		});
	}
}
