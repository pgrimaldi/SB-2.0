using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Domain.Pricing;

/// <summary>Canale del preventivo: gestionale o sito pubblico.</summary>
public enum QuoteChannel
{
	Manager,
	Online,
}

/// <summary>
/// Prezzo cumulativo: per le postazioni dello stesso pacchetto e della stessa fila, a giornata intera,
/// il prezzo si calcola sul totale dei giorni-postazione di ogni periodo e si ripartisce in proporzione ai giorni di
/// ciascuna; il centesimo che avanza va all'ultima postazione. Weekend, festivi e prezzo suggerito non contano.
/// </summary>
public static class CumulativePricing
{
	/// <param name="days">Giorni di ogni postazione nel periodo, nell'ordine della prenotazione.</param>
	public static IReadOnlyList<decimal> Distribute(decimal total, IReadOnlyList<int> days)
	{
		var sum = days.Sum();
		if (sum == 0)
			return days.Select(_ => 0m).ToList();
		var shares = days.Select(d => decimal.Round(total * d / sum, 2, MidpointRounding.AwayFromZero)).ToList();
		shares[^1] += decimal.Round(total, 2, MidpointRounding.AwayFromZero) - shares.Sum();
		return shares;
	}
}

/// <summary>Riga a cui si può applicare uno sconto: fila della postazione o categoria del servizio.</summary>
public sealed record DiscountableLine(decimal Amount, long? SectorId, DiscountServiceCategory? Category);

/// <summary>
/// Un solo sconto per prenotazione: quello col codice inserito o, senza codice, l'automatico più recente valido.
/// Valido se attivo, nel canale, con utilizzi residui, con le date della prenotazione dentro un periodo di validità,
/// i giorni della settimana ammessi e, se ci sono fasce orarie, l'ora attuale dentro una fascia. Con le opzioni
/// personalizzate (custom) vale solo sulle righe delle file e delle categorie scelte; «without_custom» su tutte le
/// altre. Percentuale sulle righe ammesse; fisso sul totale delle righe ammesse, mai sotto zero.
/// </summary>
public static class DiscountRules
{
	public static bool IsValid(Discount d, QuoteChannel channel, DateOnly from, DateOnly to, DateTime nowLocal)
	{
		if (!d.IsActive || d.MaxUses is { } max && d.UsedCount >= max)
			return false;
		if (d.ApplicableChannel == DiscountApplicableChannel.ManagerOnly && channel != QuoteChannel.Manager
			|| d.ApplicableChannel == DiscountApplicableChannel.OnlineOnly && channel != QuoteChannel.Online)
			return false;
		if (d.ValidityWindows.Count > 0 && !d.ValidityWindows.Any(w => w.StartsOn <= from && to <= w.EndsOn))
			return false;
		if (d.ValidityWindows.Count == 0 && !d.RequiresCode)
			return false;
		if (d.Weekdays.Length > 0)
			for (var day = from; day <= to; day = day.AddDays(1))
				if (!d.Weekdays.Contains(ToWeekday(day.DayOfWeek)))
					return false;
		var time = TimeOnly.FromDateTime(nowLocal);
		return d.TimeRanges.Count == 0 || d.TimeRanges.Any(r => r.From <= time && time < r.To);
	}

	public static decimal Amount(Discount d, IReadOnlyList<DiscountableLine> lines)
	{
		var sectors = d.RestrictedSectors.Select(s => s.SectorId).ToHashSet();
		bool Selected(DiscountableLine l) => l.SectorId is { } s && sectors.Contains(s) || l.Category is { } c && d.AppliesToServiceCategories.Contains(c);
		var eligible = d.Scope switch
		{
			DiscountScope.Custom => lines.Where(Selected),
			DiscountScope.WithoutCustom => lines.Where(l => !Selected(l)),
			_ => lines,
		};
		var basis = eligible.Sum(l => l.Amount);
		var amount = d.Type == DiscountType.Percentage ? basis * d.Value / 100 : Math.Min(d.Value, basis);
		return decimal.Round(Math.Max(0, amount), 2, MidpointRounding.AwayFromZero);
	}

	public static Weekday ToWeekday(DayOfWeek day) => day switch
	{
		DayOfWeek.Monday => Weekday.Monday,
		DayOfWeek.Tuesday => Weekday.Tuesday,
		DayOfWeek.Wednesday => Weekday.Wednesday,
		DayOfWeek.Thursday => Weekday.Thursday,
		DayOfWeek.Friday => Weekday.Friday,
		DayOfWeek.Saturday => Weekday.Saturday,
		_ => Weekday.Sunday,
	};
}
