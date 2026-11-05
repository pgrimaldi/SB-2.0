using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Shared.Infrastructure;
using Shared.Security;

namespace BuildingBlocks.Infrastructure.Security;

public sealed class HumanVerificationOptions
{
	public const string Section = "Security:HumanVerification";
	public const string ProofOfWork = "proof_of_work";
	public const string Turnstile = "turnstile";

	/// <summary>false = ogni richiesta passa (sviluppo e prove).</summary>
	public bool Enabled { get; set; }
	/// <summary>proof_of_work (predefinito, nessun servizio esterno) o turnstile (Cloudflare).</summary>
	public string Provider { get; set; } = ProofOfWork;

	/// <summary>Prova di lavoro: chiave HMAC (base64, almeno 32 byte) con cui il backend firma le sfide.</summary>
	public string? HmacKey { get; set; }
	/// <summary>Difficolta': il numero da trovare e' fra 0 e questo valore (100.000 ≈ mezzo secondo in un browser).</summary>
	public long MaxNumber { get; set; } = 100_000;
	/// <summary>Validita' di una sfida.</summary>
	public int ChallengeMinutes { get; set; } = 10;
	/// <summary>Tempo minimo fra la sfida e l'invio del form: un robot invia subito, una persona compila.</summary>
	public int MinSeconds { get; set; } = 3;

	/// <summary>Turnstile: chiave segreta e indirizzo di verifica.</summary>
	public string? SecretKey { get; set; }
	public string VerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}

/// <summary>
/// Prova di lavoro, senza servizi esterni. La sfida e' firmata dal backend (HMAC) e porta nel sale la data di emissione e
/// di scadenza; il token rimandato dal browser e' il JSON { algorithm, challenge, number, salt, signature } in base64.
/// Si accetta se la firma e' giusta, la sfida non e' scaduta, sono passati almeno <see cref="HumanVerificationOptions.MinSeconds"/>
/// secondi, l'hash torna e la sfida non e' gia' stata usata (memoria del processo: con piu' istanze una sfida potrebbe
/// valere una volta per istanza, entro la sua scadenza).
/// </summary>
public sealed class ProofOfWorkHumanVerification : IHumanVerification, IHumanChallengeIssuer
{
	private const string Algorithm = "SHA-256";
	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	private readonly HumanVerificationOptions _options;
	private readonly IMemoryCache _used;
	private readonly IDateTimeProvider _clock;
	private readonly byte[] _key;

	public ProofOfWorkHumanVerification(HumanVerificationOptions options, IMemoryCache used, IDateTimeProvider clock)
	{
		_options = options;
		_used = used;
		_clock = clock;
		_key = string.IsNullOrWhiteSpace(options.HmacKey) ? RandomNumberGenerator.GetBytes(32) : Convert.FromBase64String(options.HmacKey);
	}

	public HumanChallenge Create()
	{
		var now = new DateTimeOffset(_clock.UtcNow).ToUnixTimeSeconds();
		var salt = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant()}?issued={now}&expires={now + _options.ChallengeMinutes * 60}";
		var number = RandomNumberGenerator.GetInt32(0, (int)Math.Min(_options.MaxNumber, int.MaxValue) + 1);
		var challenge = Hash(salt + number.ToString(CultureInfo.InvariantCulture));
		return new HumanChallenge(Algorithm, challenge, _options.MaxNumber, salt, Sign(challenge));
	}

	public Task<bool> VerifyAsync(string? token, string? clientIp, CancellationToken ct = default)
	{
		if (!_options.Enabled)
			return Task.FromResult(true);
		return Task.FromResult(Verify(token));
	}

	private bool Verify(string? token)
	{
		if (string.IsNullOrWhiteSpace(token))
			return false;
		Solution? s;
		try
		{
			s = JsonSerializer.Deserialize<Solution>(Convert.FromBase64String(token), Json);
		}
		catch (Exception ex) when (ex is FormatException or JsonException)
		{
			return false;
		}
		if (s is null || s.Algorithm != Algorithm || s.Challenge is null || s.Salt is null || s.Signature is null || s.Number < 0)
			return false;
		if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(s.Challenge)), Encoding.ASCII.GetBytes(s.Signature)))
			return false;
		var query = s.Salt.Contains('?') ? s.Salt[(s.Salt.IndexOf('?') + 1)..].Split('&').Select(p => p.Split('=', 2)).Where(p => p.Length == 2)
			.ToDictionary(p => p[0], p => p[1]) : [];
		if (!long.TryParse(query.GetValueOrDefault("issued"), out var issued) || !long.TryParse(query.GetValueOrDefault("expires"), out var expires))
			return false;
		var now = new DateTimeOffset(_clock.UtcNow).ToUnixTimeSeconds();
		if (now > expires || now - issued < _options.MinSeconds)
			return false;
		if (Hash(s.Salt + s.Number.ToString(CultureInfo.InvariantCulture)) != s.Challenge)
			return false;
		// una sfida vale una volta sola
		if (_used.TryGetValue(s.Signature, out _))
			return false;
		_used.Set(s.Signature, true, DateTimeOffset.FromUnixTimeSeconds(expires));
		return true;
	}

	private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

	private string Sign(string challenge) => Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(challenge))).ToLowerInvariant();

	private sealed record Solution(string? Algorithm, string? Challenge, long Number, string? Salt, string? Signature);
}

/// <summary>Verifica del token Cloudflare Turnstile (stesso protocollo di reCAPTCHA: secret, response, remoteip).</summary>
public sealed class TurnstileHumanVerification : IHumanVerification
{
	private readonly HttpClient _http;
	private readonly HumanVerificationOptions _options;

	public TurnstileHumanVerification(HttpClient http, HumanVerificationOptions options)
	{
		_http = http;
		_options = options;
	}

	public async Task<bool> VerifyAsync(string? token, string? clientIp, CancellationToken ct = default)
	{
		if (!_options.Enabled)
			return true;
		if (string.IsNullOrWhiteSpace(token))
			return false;

		var form = new Dictionary<string, string>
		{
			["secret"] = _options.SecretKey ?? throw new InvalidOperationException($"{HumanVerificationOptions.Section}:SecretKey non configurata."),
			["response"] = token,
		};
		if (clientIp is not null)
			form["remoteip"] = clientIp;

		using var response = await _http.PostAsync(_options.VerifyUrl, new FormUrlEncodedContent(form), ct);
		if (!response.IsSuccessStatusCode)
			return false;
		var result = await response.Content.ReadFromJsonAsync<VerifyResponse>(ct);
		return result?.Success == true;
	}

	private sealed record VerifyResponse([property: JsonPropertyName("success")] bool Success);
}
