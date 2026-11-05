using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Module.Catalog.Domain.Pricing;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Voce di listino (dello stabilimento o di una convenzione) vista dal motore di prezzo.</summary>
internal sealed class PricingSource
{
	public required long Id { get; init; }
	public required Guid PublicId { get; init; }
	public required string Name { get; init; }
	public required PriceListKind Kind { get; init; }
	public required PackageRules Rules { get; init; }
	public required bool IsOnline { get; init; }
	public required bool IsDefault { get; init; }
	public required bool IsCumulative { get; init; }
	public required bool IsSectorRestricted { get; init; }
	public required short? MaxAdditionalItems { get; init; }
	public required decimal? VatRate { get; init; }
	public required string? VatNature { get; init; }
	/// <summary>Pacchetti a cui è legato un articolo aggiuntivo, con il numero massimo (0 = senza limite).</summary>
	public required IReadOnlyDictionary<long, short> PackageLinks { get; init; }

	private Func<long, bool> _hasSector = _ => true;
	private Func<long?, short, PeriodRates?> _rates = (_, _) => null;
	private Func<long?, (decimal?, decimal?)> _seasonal = _ => (null, null);
	private Func<long?, DateOnly, DateOnly, decimal?> _reduced = (_, _, _) => null;
	private Func<DateOnly, long?, SuggestedPrice?> _suggested = (_, _) => null;

	public bool HasSector(long sectorId) => _hasSector(sectorId);
	public PeriodRates? Rates(long? sectorId, short group) => _rates(sectorId, group);
	public (decimal? Full, decimal? Half) Seasonal(long? sectorId) => _seasonal(sectorId);
	public decimal? Reduced(long? sectorId, DateOnly from, DateOnly to) => _reduced(sectorId, from, to);
	public SuggestedPrice? SuggestedPrice(DateOnly date, long? sectorId) => _suggested(date, sectorId);

	/// <summary>Prezzi suggeriti già scelti dal gestore, per data e fila.</summary>
	public PricingSource WithSuggestedPrices(IReadOnlyList<SuggestedPrice> prices)
	{
		var map = prices.Where(p => p.AppliedMode != SuggestedPriceMode.None).GroupBy(p => (p.PriceDate, p.SectorId)).ToDictionary(g => g.Key, g => g.First());
		_suggested = (date, sector) => map.GetValueOrDefault((date, sector)) ?? map.GetValueOrDefault((date, null));
		return this;
	}

	public static PricingSource From(PriceList p, IReadOnlyList<ReducedSeason> reducedSeasons)
	{
		var source = new PricingSource
		{
			Id = p.Id, PublicId = p.PublicId, Name = p.Name, Kind = p.Kind,
			Rules = BuildRules(p.CalculationMethod?.Code, p.MonthlySetting, p.WeekendSetting, p.AllowsPublicHolidays,
				p.DatePrices.Where(d => d.Kind == DatePriceKind.Holiday).Select(d => d.PriceDate)),
			IsOnline = p.SalesChannel != SalesChannel.ManagerOnly, IsDefault = p.IsDefault, IsCumulative = p.IsCumulative,
			IsSectorRestricted = p.IsSectorRestricted, MaxAdditionalItems = p.MaxAdditionalItems, VatRate = p.VatRate,
			VatNature = p.VatNature is { } n ? Codes.Of(n) : null,
			PackageLinks = p.Items.Where(i => i.ItemKind == PackageItemKind.LinkedPackage && i.LinkedPriceListId is not null)
				.GroupBy(i => i.LinkedPriceListId!.Value).ToDictionary(g => g.Key, g => g.First().Quantity),
		};
		var sectorPrices = p.SectorPrices.ToList();
		source._hasSector = s => sectorPrices.Any(x => x.SectorId == s);
		source._seasonal = s => sectorPrices.FirstOrDefault(x => x.SectorId == s) is { } x ? (x.SeasonalPrice, x.HalfDayPrice) : (null, null);
		source._rates = (sector, group) => Rates(p.PeriodPrices.Where(x => x.TariffPeriodGroup == group)
			.OrderBy(x => x.SectorId == sector ? 0 : x.SectorId == null ? 1 : 2).FirstOrDefault(x => x.SectorId == sector || x.SectorId == null));
		source._reduced = (sector, from, to) =>
		{
			var season = reducedSeasons.FirstOrDefault(s => s.StartsOn == from && s.EndsOn == to);
			return season is null ? null : p.ReducedPrices.FirstOrDefault(x => x.ReducedSeasonId == season.Id && x.SectorId == sector)?.Price;
		};
		return source;

		static PeriodRates? Rates(PriceListPeriodPrice? x) => x is null ? null : new PeriodRates(
			x.HalfDayPrice, x.HalfDayWeekendPrice, x.DailyPrice, x.WeekendPrice, x.HalfDay7Price, x.Days7Price, x.HalfDay14Price, x.Days14Price,
			x.HalfDayMonthlyPrice, x.MonthlyPrice,
			x.StayPriceOverrides.Where(o => o.IsManualOverride).GroupBy(o => (int)o.StayDays).ToDictionary(g => g.Key, g => g.First().Price));
	}

	public static PricingSource From(AgreementPriceList p, IReadOnlyDictionary<short, string> methods)
	{
		var source = new PricingSource
		{
			Id = p.Id, PublicId = p.PublicId, Name = p.Name, Kind = p.Kind,
			Rules = BuildRules(p.CalculationMethodId is { } m ? methods.GetValueOrDefault(m) : null, p.MonthlySetting, p.WeekendSetting, p.AllowsPublicHolidays,
				p.DatePrices.Where(d => d.Kind == DatePriceKind.Holiday).Select(d => d.PriceDate)),
			IsOnline = false, IsDefault = false, IsCumulative = p.IsCumulative, IsSectorRestricted = false, MaxAdditionalItems = null,
			VatRate = p.VatRate, VatNature = p.VatNature is { } n ? Codes.Of(n) : null,
			PackageLinks = p.Items.Where(i => i.ItemKind == PackageItemKind.LinkedPackage && i.LinkedPriceListId is not null)
				.GroupBy(i => i.LinkedPriceListId!.Value).ToDictionary(g => g.Key, g => g.First().Quantity),
		};
		var sectorPrices = p.SectorPrices.ToList();
		source._seasonal = s => sectorPrices.FirstOrDefault(x => x.SectorId == s) is { } x ? (x.SeasonalPrice, x.HalfDayPrice) : (null, null);
		source._rates = (sector, group) => Rates(p.PeriodPrices.Where(x => x.TariffPeriodGroup == group)
			.OrderBy(x => x.SectorId == sector ? 0 : x.SectorId == null ? 1 : 2).FirstOrDefault(x => x.SectorId == sector || x.SectorId == null));
		return source;

		static PeriodRates? Rates(AgreementPriceListPeriodPrice? x) => x is null ? null : new PeriodRates(
			x.HalfDayPrice, x.HalfDayWeekendPrice, x.DailyPrice, x.WeekendPrice, x.HalfDay7Price, x.Days7Price, x.HalfDay14Price, x.Days14Price,
			x.HalfDayMonthlyPrice, x.MonthlyPrice,
			x.StayPriceOverrides.Where(o => o.IsManualOverride).GroupBy(o => (int)o.StayDays).ToDictionary(g => g.Key, g => g.First().Price));
	}

	private static PackageRules BuildRules(string? methodCode, MonthlySetting? monthly, WeekendSetting? weekend, bool holidays, IEnumerable<DateOnly> dates)
		=> new(Method(methodCode), monthly, weekend, holidays ? dates.ToHashSet() : new HashSet<DateOnly>());

	public static PricingMethod Method(string? code) => code switch
	{
		"proportional" => PricingMethod.Proportional,
		"daily_with_weekend" => PricingMethod.DailyWithWeekend,
		"logarithmic" => PricingMethod.Logarithmic,
		_ => PricingMethod.Daily,
	};
}
