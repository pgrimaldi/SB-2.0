using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Domain.Pricing;

public enum DayPart
{
	FullDay,
	Morning,
	Afternoon,
}

/// <summary>Prezzi di un periodo tariffario per una fila; <see cref="Overrides"/> = celle della griglia corrette a mano.</summary>
public sealed record PeriodRates(
	decimal? HalfDay, decimal? HalfDayWeekend, decimal? Daily, decimal? Weekend, decimal? HalfDay7, decimal? Days7, decimal? HalfDay14,
	decimal? Days14, decimal? HalfDayMonthly, decimal? Monthly, IReadOnlyDictionary<int, decimal> Overrides);

/// <summary>Regole del pacchetto (o dell'articolo aggiuntivo) per il calcolo.</summary>
public sealed record PackageRules(PricingMethod Method, MonthlySetting? MonthlySetting, WeekendSetting? WeekendSetting, IReadOnlySet<DateOnly> Holidays);

/// <summary>Tratto della prenotazione in un solo periodo tariffario; <see cref="Days"/> = giorni da pagare (senza assenze).</summary>
public sealed record PricingSegment(DateOnly From, DateOnly To, short? PeriodGroup, IReadOnlyList<DateOnly> Days)
{
	public int Span => To.DayNumber - From.DayNumber + 1;
}

/// <summary>Esito del calcolo di un tratto: importo e regola usata.</summary>
public sealed record SegmentPrice(PricingSegment Segment, decimal Amount, string Rule);

/// <summary>
/// Prezzo di un pacchetto o di un articolo aggiuntivo per un tratto:
/// <list type="bullet">
/// <item>«giornaliero con weekend», giornata intera: cella corretta a mano per la durata; altrimenti scaglione mensile
/// (durata pari ai giorni del mese di inizio), di 14 o di 7 giorni e, sui giorni che restano, weekend e festivi a
/// prezzo weekend, gli altri dalla griglia;</item>
/// <item>gli altri metodi, giornata intera: fino a 6 giorni dalla griglia, con il prezzo weekend per sabato e domenica
/// solo fino a 3 giorni e per i festivi fino a 6; oltre, la griglia per la durata;</item>
/// <item>mezza giornata (tutti i metodi): weekend e festivi sempre al prezzo di mezza giornata nel weekend, gli altri
/// giorni con lo stesso metodo del pacchetto sui prezzi di mezza giornata a 7, 14 giorni e mese se impostati,
/// altrimenti prezzo per giorni.</item>
/// </list>
/// Nessun limite di durata; la mezza giornata nel weekend si conta una volta sola; le assenze non si pagano mai;
/// i festivi vanno a prezzo weekend solo se il prezzo weekend c'è.
/// </summary>
public static class PackagePricer
{
	/// <summary>Divide l'intervallo nei tratti dei periodi tariffari (per ogni giorno vale il più breve).</summary>
	public static IReadOnlyList<PricingSegment> Segments(IReadOnlyList<TariffPeriod> periods, DateOnly from, DateOnly to, IReadOnlySet<DateOnly> excluded, IReadOnlyCollection<DateOnly>? onlyDays = null)
	{
		var segments = new List<PricingSegment>();
		DateOnly? start = null;
		short? group = null;
		var days = new List<DateOnly>();
		for (var day = from; day <= to; day = day.AddDays(1))
		{
			var current = TariffCalendar.Resolve(periods, day)?.PeriodGroupKey;
			if (start is not null && current != group)
			{
				segments.Add(new PricingSegment(start.Value, day.AddDays(-1), group, days));
				days = [];
				start = null;
			}
			start ??= day;
			group = current;
			if (!excluded.Contains(day) && (onlyDays is null || onlyDays.Contains(day)))
				days.Add(day);
		}
		if (start is not null)
			segments.Add(new PricingSegment(start.Value, to, group, days));
		return segments.Where(s => s.Days.Count > 0).ToList();
	}

	public static decimal Price(PackageRules rules, PeriodRates rates, PricingSegment segment, DayPart part)
		=> part == DayPart.FullDay ? FullDay(rules, rates, segment) : HalfDay(rules, rates, segment);

	/// <summary>Griglia a giornata intera del periodo (per il cumulativo).</summary>
	public static PriceGrid FullDayGrid(PackageRules rules, PeriodRates rates, DateOnly from, int days)
		=> PriceGrid.Build(rules.Method, MonthDays(rules.MonthlySetting, from), new GridRates(rates.Daily, rates.Days7, rates.Days14, rates.Monthly), days,
			rates.Overrides);

	/// <summary>Prezzo base di un giorno per il prezzo suggerito: weekend il sabato e la domenica (o solo la domenica), altrimenti giornaliero.</summary>
	public static decimal DayBasePrice(PackageRules rules, PeriodRates rates, DateOnly day)
		=> IsWeekend(rules.WeekendSetting, day) && rates.Weekend is > 0 ? rates.Weekend.Value : rates.Daily ?? 0;

	private static decimal FullDay(PackageRules rules, PeriodRates rates, PricingSegment segment)
	{
		var days = segment.Days.Count;
		var grid = FullDayGrid(rules, rates, segment.From, days);
		var weekendPrice = rates.Weekend is > 0 ? rates.Weekend.Value : 0;

		if (rules.Method == PricingMethod.DailyWithWeekend)
		{
			if (rates.Overrides.TryGetValue(days, out var manual))
				return manual;
			var span = segment.Span;
			if (span == DateTime.DaysInMonth(segment.From.Year, segment.From.Month) && rates.Monthly is > 0)
				return rates.Monthly.Value;
			var slab = span >= 14 && rates.Days14 is > 0 ? 14 : span >= 7 && rates.Days7 is > 0 ? 7 : 0;
			if (weekendPrice == 0)
				return grid.For(days);
			var rest = segment.Days.Where(d => d >= segment.From.AddDays(slab)).ToList();
			var weekend = rest.Count(d => IsWeekend(rules.WeekendSetting, d));
			var holidays = rest.Count(d => !IsWeekend(rules.WeekendSetting, d) && rules.Holidays.Contains(d));
			var weekdays = rest.Count - weekend - holidays;
			var slabDays = days - rest.Count;
			return grid.For(slabDays) + (weekend + holidays) * weekendPrice + grid.For(weekdays);
		}

		if (days > 6 || weekendPrice == 0)
			return grid.For(days);
		var weekendDays = days <= 3 ? segment.Days.Count(d => IsWeekend(rules.WeekendSetting, d)) : 0;
		var holidayDays = segment.Days.Count(d => rules.Holidays.Contains(d) && !(days <= 3 && IsWeekend(rules.WeekendSetting, d)));
		return grid.For(days - weekendDays - holidayDays) + (weekendDays + holidayDays) * weekendPrice;
	}

	private static decimal HalfDay(PackageRules rules, PeriodRates rates, PricingSegment segment)
	{
		var weekendPrice = rates.HalfDayWeekend is > 0 ? rates.HalfDayWeekend.Value : 0;
		var special = weekendPrice > 0
			? segment.Days.Count(d => IsWeekend(rules.WeekendSetting, d) || rules.Holidays.Contains(d))
			: 0;
		var rest = segment.Days.Count - special;
		decimal restPrice;
		if (rates.HalfDay7 is > 0 || rates.HalfDay14 is > 0 || rates.HalfDayMonthly is > 0)
			restPrice = PriceGrid.Build(rules.Method, MonthDays(rules.MonthlySetting, segment.From),
				new GridRates(rates.HalfDay, rates.HalfDay7, rates.HalfDay14, rates.HalfDayMonthly), rest).For(rest);
		else
			restPrice = rest * (rates.HalfDay ?? 0);
		return special * weekendPrice + restPrice;
	}

	public static bool IsWeekend(WeekendSetting? setting, DateOnly day)
		=> day.DayOfWeek == DayOfWeek.Sunday || setting != WeekendSetting.SundayOnly && day.DayOfWeek == DayOfWeek.Saturday;

	/// <summary>Giorni del «mese» dello scaglione mensile: 31 con la durata effettiva, i giorni veri del mese, 30 fisso.</summary>
	public static int MonthDays(MonthlySetting? setting, DateOnly from) => setting switch
	{
		MonthlySetting.EffectiveMonthDuration => 31,
		MonthlySetting.ActualMonthDuration => DateTime.DaysInMonth(from.Year, from.Month),
		_ => 30,
	};
}
