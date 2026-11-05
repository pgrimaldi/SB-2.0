namespace Module.Catalog.Domain.Pricing;

/// <summary>Metodo di calcolo del prezzo per durata (lookup pricing_calculation_methods).</summary>
public enum PricingMethod
{
	Proportional,
	Daily,
	DailyWithWeekend,
	Logarithmic,
}

/// <summary>Prezzi di riferimento di una griglia: 1, 7, 14 giorni e mese; null o 0 = non impostato.</summary>
public sealed record GridRates(decimal? Day1, decimal? Days7, decimal? Days14, decimal? Monthly)
{
	public bool IsEmpty => !Set(Day1) && !Set(Days7) && !Set(Days14) && !Set(Monthly);

	internal static bool Set(decimal? value) => value is { } v && v != 0;
}

/// <summary>
/// Griglia del prezzo per durata (1..N giorni), calcolata giorno per giorno con uno stato progressivo (prezzo del
/// giorno prima, media del logaritmico) e arrotondata a un decimale; le celle corrette a mano dal gestore prevalgono.
/// Nessun limite di durata.
/// </summary>
/// <remarks>
/// Fallback dei prezzi: 7 giorni → giornaliero; 14 → 7 → giornaliero; mese → 14 → 7 → giornaliero.
/// Proporzionale: fino a 6 giorni P1·d, a 7 P7, poi P7/7·d, a 14 P14, poi P14/14·d, al mese PM, poi PM/M·d.
/// Giornaliero (anche «giornaliero con weekend»): come il proporzionale ma oltre gli scaglioni si somma P1 per ogni
/// giorno in più. Logaritmico: mu(Pa, Pb, da, db) = (Pa/da − Pb/db)/(db − da + 1), lamda(p, d, mu) = p·d − (d + 1)·mu,
/// con p = media del giorno precedente. Al giorno del mese, con il solo P7, il logaritmico vale P7/7·d come nei giorni prima.
/// </remarks>
public sealed class PriceGrid
{
	private readonly decimal[] _cells;

	private PriceGrid(decimal[] cells) => _cells = cells;

	/// <summary>Prezzo per <paramref name="days"/> giorni; 0 giorni = 0.</summary>
	public decimal For(int days) => days <= 0 ? 0 : days < _cells.Length ? _cells[days] : _cells[^1] / (_cells.Length - 1) * days;

	/// <param name="monthDays">Giorni del «mese» per lo scaglione mensile (31 durata effettiva, 30 fisso).</param>
	/// <param name="overrides">Celle corrette a mano: giorni → prezzo.</param>
	public static PriceGrid Build(PricingMethod method, int monthDays, GridRates rates, int maxDays, IReadOnlyDictionary<int, decimal>? overrides = null)
	{
		var size = Math.Max(maxDays, monthDays + 1);
		var cells = new decimal[size + 1];
		var state = new State();
		for (var day = 1; day <= size; day++)
		{
			double value;
			if (rates.IsEmpty)
				value = state.Previous;
			else
				value = method switch
				{
					PricingMethod.Proportional => Proportional(day, monthDays, rates, state),
					PricingMethod.Logarithmic => Logarithmic(day, monthDays, rates, state),
					_ => Daily(day, monthDays, rates, state),
				};
			state.Previous = double.IsFinite(value) ? value : 0;
			cells[day] = (decimal)Math.Round(state.Previous, 1, MidpointRounding.AwayFromZero);
		}
		if (overrides is not null)
			foreach (var (days, price) in overrides)
				if (days >= 1 && days <= size)
					cells[days] = price;
		return new PriceGrid(cells);
	}

	private sealed class State
	{
		public double Previous;
		public double Divided;
	}

	private static double D(decimal? value) => value is { } v ? (double)v : 0;

	private static double Proportional(int day, int month, GridRates r, State s)
	{
		var (p1, has7, p7, has14, p14, hasM, pm) = Rates(r);
		if (day <= 6)
			return p1 + s.Previous;
		if (day == 7)
			return has7 ? p7 : p1 * day;
		if (day <= 13)
			return has7 ? p7 / 7 * day : p1 * day;
		if (day < month)
			return has14 ? (day == 14 ? p14 : p14 / 14 * day) : has7 ? p7 / 7 * day : p1 * day;
		if (day == month)
			return hasM ? pm : has14 ? p14 / 14 * day : has7 ? p7 / 7 * day : p1 * day;
		return hasM ? pm / month * day : has14 ? p14 / 14 * day : has7 ? p7 / 7 * day : p1 * day;
	}

	private static double Daily(int day, int month, GridRates r, State s)
	{
		var (p1, has7, p7, has14, p14, hasM, pm) = Rates(r);
		if (day <= 6)
			return p1 + s.Previous;
		if (day == 7)
			return has7 ? p7 : p1 * day;
		if (day <= 13)
			return has7 ? p7 + (day - 7) * p1 : p1 * day;
		if (day < month)
			return has14 ? p14 + (day - 14) * p1 : has7 ? p7 + (day - 7) * p1 : p1 * day;
		if (day == month)
			return hasM ? pm : has14 ? p14 + (day - 14) * p1 : has7 ? p7 + (day - 7) * p1 : p1 * day;
		return hasM ? pm + (day - month) * p1 : has14 ? p14 + (day - 14) * p1 : has7 ? p7 + (day - 7) * p1 : p1 * day;
	}

	private static double Logarithmic(int day, int month, GridRates r, State s)
	{
		var (p1, has7, p7, has14, p14, hasM, pm) = Rates(r);

		// calcola lamda dalla media del giorno precedente e aggiorna la media
		double Lamda(double pa, double pb, int da, int db)
		{
			var mu = (pa / da - pb / db) / (db - da + 1);
			var basePrice = s.Divided != 0 ? s.Divided : p1;
			var value = basePrice * day - (day + 1) * mu;
			s.Divided = value / day;
			return value;
		}

		if (day == 1)
			return p1;
		if (day <= 6)
			return has7 ? Lamda(p1, p7, 1, 7) : has14 ? Lamda(p1, p14, 1, 14) : hasM ? Lamda(p1, pm, 1, month) : p1 * day;
		if (day == 7)
		{
			if (has7)
			{
				Lamda(p1, p7, 1, 7);
				return p7;
			}
			return has14 ? Lamda(p1, p14, 1, 14) : hasM ? Lamda(p1, pm, 1, month) : p1 * day;
		}
		if (day <= 14)
		{
			double value;
			if (has14 && has7)
				value = Lamda(p7, p14, 7, 14);
			else if (has14)
				value = Lamda(p1, p14, 1, 14);
			else if (hasM)
				value = has7 ? Lamda(p7, pm, 7, month) : Lamda(p1, pm, 1, month);
			else
				return has7 ? p7 / 7 * day : p1 * day;
			return day == 14 && has14 ? p14 : value;
		}
		if (day < month)
		{
			if (hasM && has14)
				return Lamda(p14, pm, 14, month);
			if (has14)
				return p14 / 14 * day;
			if (!hasM)
				return has7 ? p7 / 7 * day : p1 * day;
			return has7 ? Lamda(p7, pm, 7, month) : Lamda(p1, pm, 1, month);
		}
		if (day == month)
		{
			if (hasM && has14)
			{
				Lamda(p14, pm, 14, month);
				return pm;
			}
			if (has14)
				return p14 / 14 * day;
			if (hasM)
				return pm;
			return has7 ? p7 / 7 * day : p1 * day;
		}
		return hasM ? pm / month * day : has14 ? p14 / 14 * day : has7 ? p7 / 7 * day : p1 * day;
	}

	private static (double P1, bool Has7, double P7, bool Has14, double P14, bool HasM, double Pm) Rates(GridRates r)
		=> (D(r.Day1), GridRates.Set(r.Days7), D(r.Days7), GridRates.Set(r.Days14), D(r.Days14), GridRates.Set(r.Monthly), D(r.Monthly));
}
