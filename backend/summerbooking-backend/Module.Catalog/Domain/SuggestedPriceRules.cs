using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Domain;

/// <summary>
/// Prezzo suggerito di un giorno: dal prezzo del periodo (weekend il sabato e la domenica, altrimenti giornaliero), i pesi si
/// moltiplicano. Pioggia: sconto di bassa o di alta stagione. Giorno prima del weekend: aumento; altrimenti, in alta
/// stagione, aumento di alta stagione. Posti liberi: dalla soglia in su sconto, sotto aumento (con la soglia esatta scende).
/// </summary>
public static class SuggestedPriceRules
{
	public sealed record DayFacts(DateOnly Date, bool? Rain, RevenueSeasonTier Tier, decimal? FreeSpotsPercent);

	public static decimal BasePrice(DateOnly date, decimal? daily, decimal? weekend)
		=> date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? weekend ?? daily ?? 0 : daily ?? weekend ?? 0;

	public static decimal Suggest(decimal basePrice, DayFacts facts, RevenueWeights weights)
	{
		var price = basePrice;
		if (facts.Rain == true)
			price *= facts.Tier == RevenueSeasonTier.High ? weights.RainHighSeasonFactor : weights.RainLowSeasonFactor;
		if (facts.Date.AddDays(1).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
			price *= weights.BeforeWeekendFactor;
		else if (facts.Tier == RevenueSeasonTier.High)
			price *= weights.HighSeasonFactor;
		if (facts.FreeSpotsPercent is { } free)
			price *= free >= weights.FreeSpotsThresholdPercent ? weights.FreeSpotsAboveFactor : weights.FreeSpotsBelowFactor;
		return decimal.Round(price, 2, MidpointRounding.AwayFromZero);
	}

	/// <summary>Sconto rispetto al prezzo di listino, in percentuale (negativo = aumento).</summary>
	public static decimal DiscountPercent(decimal basePrice, decimal suggested)
		=> basePrice <= 0 ? 0 : decimal.Round((basePrice - suggested) / basePrice * 100, 2, MidpointRounding.AwayFromZero);
}
