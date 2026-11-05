using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Application.Abstractions;

public interface IRevenueRepository
{
	/// <summary>Con le postazioni scontate, tracciata.</summary>
	Task<RevenueSettings?> FindSettingsAsync(long propertyId, CancellationToken ct = default);
	void AddSettings(RevenueSettings settings);

	/// <summary>Tracciati, in ordine.</summary>
	Task<List<RevenueStandardPeriod>> ListStandardPeriodsAsync(CancellationToken ct = default);
	void AddStandardPeriod(RevenueStandardPeriod period);
	void RemoveStandardPeriod(RevenueStandardPeriod period);
	Task<RevenueWeights> GetWeightsAsync(CancellationToken ct = default);

	/// <summary>Prezzi suggeriti del pacchetto nell'intervallo, tracciati.</summary>
	Task<List<SuggestedPrice>> ListSuggestedPricesAsync(long priceListId, DateOnly from, DateOnly to, CancellationToken ct = default);
	void AddSuggestedPrice(SuggestedPrice price);
}

/// <summary>Pioggia del giorno in millimetri, dalla previsione o dallo storico; i giorni senza dato mancano.</summary>
public interface IWeatherForecast
{
	Task<IReadOnlyDictionary<DateOnly, decimal>> RainAsync(decimal latitude, decimal longitude, DateOnly from, DateOnly to, CancellationToken ct = default);
}
