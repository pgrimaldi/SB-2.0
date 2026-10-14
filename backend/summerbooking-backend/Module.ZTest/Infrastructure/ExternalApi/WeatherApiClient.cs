namespace Module.ZTest.Infrastructure.ExternalApi;

/// <summary>
/// Client di esempio verso un servizio esterno (es. previsioni meteo per la localita).
/// Mostra dove collocare le integrazioni con API di terze parti (tipicamente via IHttpClientFactory).
/// </summary>
public sealed class WeatherApiClient
{
	private readonly HttpClient _httpClient;

	public WeatherApiClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<string> GetForecastAsync(string location, DateOnly date, CancellationToken ct = default)
	{
		// In un'implementazione reale la risposta verrebbe deserializzata in un DTO dedicato.
		var url = $"forecast?loc={Uri.EscapeDataString(location)}&date={date:yyyy-MM-dd}";
		using var response = await _httpClient.GetAsync(url, ct);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync(ct);
	}
}
