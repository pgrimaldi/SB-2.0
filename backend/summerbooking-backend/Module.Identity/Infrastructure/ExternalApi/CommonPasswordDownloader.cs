using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.Identity.Infrastructure.Security;

namespace Module.Identity.Infrastructure.ExternalApi;

/// <summary>
/// Scarica gli elenchi delle password diffuse, li unisce e, se il risultato è plausibile, lo salva su file
/// e lo mette in uso. Un download fallito o sospetto lascia l'elenco in uso com'è.
/// </summary>
public sealed class CommonPasswordDownloader
{
	public const string HttpClientName = "identity-common-passwords";

	private readonly IHttpClientFactory _http;
	private readonly CommonPasswordList _list;
	private readonly CommonPasswordOptions _options;
	private readonly ILogger<CommonPasswordDownloader> _logger;

	public CommonPasswordDownloader(IHttpClientFactory http, CommonPasswordList list, IOptions<CommonPasswordOptions> options, ILogger<CommonPasswordDownloader> logger)
	{
		_http = http;
		_list = list;
		_options = options.Value;
		_logger = logger;
	}

	/// <returns>true se l'elenco è stato aggiornato.</returns>
	public async Task<bool> RefreshAsync(CancellationToken ct)
	{
		var client = _http.CreateClient(HttpClientName);
		var lines = new List<string>();

		foreach (var source in _options.Sources)
		{
			using var response = await client.GetAsync(source, HttpCompletionOption.ResponseHeadersRead, ct);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("Password diffuse: {Source} ha risposto {Status}, aggiornamento annullato", source, (int)response.StatusCode);
				return false;
			}
			if (response.Content.Headers.ContentLength > _options.MaxBytesPerSource)
			{
				_logger.LogWarning("Password diffuse: {Source} supera il limite di {Max} byte, aggiornamento annullato", source, _options.MaxBytesPerSource);
				return false;
			}

			var text = await response.Content.ReadAsStringAsync(ct);
			if (text.Length > _options.MaxBytesPerSource)
				return false;
			lines.AddRange(text.Split('\n'));
		}

		var passwords = CommonPasswordList.Normalize(lines);
		if (passwords.Count < _options.MinimumEntries)
		{
			_logger.LogWarning("Password diffuse: solo {Count} voci scaricate (minimo {Min}), aggiornamento annullato", passwords.Count, _options.MinimumEntries);
			return false;
		}

		await SaveAsync(passwords, ct);
		_list.Replace(passwords);
		_logger.LogInformation("Password diffuse aggiornate: {Count} voci", passwords.Count);
		return true;
	}

	/// <summary>Scrittura su file temporaneo e rinomina: chi legge non vede mai un file a metà.</summary>
	private async Task SaveAsync(HashSet<string> passwords, CancellationToken ct)
	{
		var path = Path.GetFullPath(_options.FilePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		var temp = path + ".tmp";
		await File.WriteAllLinesAsync(temp, passwords.Order(StringComparer.OrdinalIgnoreCase), ct);
		File.Move(temp, path, overwrite: true);
	}
}
