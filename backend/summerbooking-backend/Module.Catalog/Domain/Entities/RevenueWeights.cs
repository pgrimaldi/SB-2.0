using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Pesi del prezzo suggerito, gestiti dal systemadmin e uguali per tutti gli stabilimenti (riga unica).</summary>
public sealed class RevenueWeights : IAuditable
{
	public const short SingletonId = 1;

	public short Id { get; private set; }
	/// <summary>Pioggia in bassa stagione (0,70 = -30%).</summary>
	public decimal RainLowSeasonFactor { get; private set; }
	public decimal RainHighSeasonFactor { get; private set; }
	/// <summary>Giorno prima del weekend; altrimenti, in alta stagione, <see cref="HighSeasonFactor"/>.</summary>
	public decimal BeforeWeekendFactor { get; private set; }
	public decimal HighSeasonFactor { get; private set; }
	/// <summary>Percentuale di posti liberi: da questa in su il prezzo scende, sotto sale.</summary>
	public decimal FreeSpotsThresholdPercent { get; private set; }
	public decimal FreeSpotsAboveFactor { get; private set; }
	public decimal FreeSpotsBelowFactor { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	private RevenueWeights() { }

	public void Update(RevenueWeightsData d)
	{
		RainLowSeasonFactor = d.RainLowSeasonFactor;
		RainHighSeasonFactor = d.RainHighSeasonFactor;
		BeforeWeekendFactor = d.BeforeWeekendFactor;
		HighSeasonFactor = d.HighSeasonFactor;
		FreeSpotsThresholdPercent = d.FreeSpotsThresholdPercent;
		FreeSpotsAboveFactor = d.FreeSpotsAboveFactor;
		FreeSpotsBelowFactor = d.FreeSpotsBelowFactor;
	}
}

public sealed record RevenueWeightsData(
	decimal RainLowSeasonFactor, decimal RainHighSeasonFactor, decimal BeforeWeekendFactor, decimal HighSeasonFactor,
	decimal FreeSpotsThresholdPercent, decimal FreeSpotsAboveFactor, decimal FreeSpotsBelowFactor);
