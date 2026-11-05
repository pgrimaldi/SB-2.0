using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Module.Catalog.Application.Abstractions;
using Shared.Infrastructure;

namespace Module.Catalog.Infrastructure.Infras;

/// <summary>
/// Pioggia da Open-Meteo: previsione fino a 16 giorni, storico per i giorni già passati (con qualche giorno di ritardo,
/// coperto dalla previsione). Se il servizio non risponde i giorni restano senza dato e la regola della pioggia non si applica.
/// </summary>
public sealed class OpenMeteoWeather : IWeatherForecast
{
	public const string HttpClientName = "open-meteo";
	private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";
	private const string ArchiveUrl = "https://archive-api.open-meteo.com/v1/archive";
	private const int ForecastDays = 16;
	private const int PastDaysInForecast = 5;

	private readonly IHttpClientFactory _http;
	private readonly IDateTimeProvider _clock;
	private readonly ILogger<OpenMeteoWeather> _logger;

	public OpenMeteoWeather(IHttpClientFactory http, IDateTimeProvider clock, ILogger<OpenMeteoWeather> logger)
	{
		_http = http;
		_clock = clock;
		_logger = logger;
	}

	public async Task<IReadOnlyDictionary<DateOnly, decimal>> RainAsync(decimal latitude, decimal longitude, DateOnly from, DateOnly to, CancellationToken ct = default)
	{
		var today = DateOnly.FromDateTime(_clock.UtcNow);
		var result = new Dictionary<DateOnly, decimal>();
		var archiveEnd = Min(to, today.AddDays(-PastDaysInForecast - 1));
		if (from <= archiveEnd)
			await FetchAsync(ArchiveUrl, latitude, longitude, from, archiveEnd, result, ct);
		var forecastStart = Max(from, today.AddDays(-PastDaysInForecast));
		var forecastEnd = Min(to, today.AddDays(ForecastDays - 1));
		if (forecastStart <= forecastEnd)
			await FetchAsync(ForecastUrl, latitude, longitude, forecastStart, forecastEnd, result, ct);
		return result;
	}

	private async Task FetchAsync(string url, decimal latitude, decimal longitude, DateOnly from, DateOnly to, Dictionary<DateOnly, decimal> into, CancellationToken ct)
	{
		var query = string.Create(CultureInfo.InvariantCulture,
			$"{url}?latitude={latitude}&longitude={longitude}&daily=precipitation_sum&timezone=auto&start_date={from:yyyy-MM-dd}&end_date={to:yyyy-MM-dd}");
		try
		{
			var response = await _http.CreateClient(HttpClientName).GetFromJsonAsync<Response>(query, ct);
			if (response?.Daily is not { Time: { } days, PrecipitationSum: { } rain })
				return;
			for (var i = 0; i < days.Count && i < rain.Count; i++)
				if (rain[i] is { } mm && DateOnly.TryParseExact(days[i], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
					into[day] = mm;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
		{
			_logger.LogWarning(ex, "Meteo non disponibile da {Url} per {From}-{To}", url, from, to);
		}
	}

	private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
	private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

	private sealed record Response(Daily? Daily);

	private sealed record Daily(
		[property: System.Text.Json.Serialization.JsonPropertyName("time")] IReadOnlyList<string>? Time,
		[property: System.Text.Json.Serialization.JsonPropertyName("precipitation_sum")] IReadOnlyList<decimal?>? PrecipitationSum);
}
