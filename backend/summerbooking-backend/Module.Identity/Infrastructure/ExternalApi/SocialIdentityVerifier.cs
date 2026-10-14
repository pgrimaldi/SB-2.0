using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Module.Identity.Application.Abstractions;
using Module.Identity.Domain.Enums;

namespace Module.Identity.Infrastructure.ExternalApi;

/// <summary>Sezione <c>Auth:Social</c>. Più client id per provider: web e app mobile hanno id diversi.</summary>
public sealed class SocialAuthOptions
{
	public const string Section = "Auth:Social";

	public List<string> GoogleClientIds { get; set; } = [];
	public List<string> AppleClientIds { get; set; } = [];
	public string? FacebookAppId { get; set; }
	public string? FacebookAppSecret { get; set; }
}

/// <summary>
/// Google e Apple: ID token OIDC validato con le chiavi pubblicate dal provider (firma, issuer, audience,
/// scadenza). Facebook: access token verificato con <c>debug_token</c> sull'app configurata.
/// </summary>
public sealed class SocialIdentityVerifier : ISocialIdentityVerifier
{
	public const string HttpClientName = "identity-social";

	private static readonly ConfigurationManager<OpenIdConnectConfiguration> GoogleConfig = Oidc("https://accounts.google.com/.well-known/openid-configuration");
	private static readonly ConfigurationManager<OpenIdConnectConfiguration> AppleConfig = Oidc("https://appleid.apple.com/.well-known/openid-configuration");

	private readonly SocialAuthOptions _options;
	private readonly IHttpClientFactory _http;
	private readonly ILogger<SocialIdentityVerifier> _logger;
	private readonly JsonWebTokenHandler _handler = new();

	public SocialIdentityVerifier(IOptions<SocialAuthOptions> options, IHttpClientFactory http, ILogger<SocialIdentityVerifier> logger)
	{
		_options = options.Value;
		_http = http;
		_logger = logger;
	}

	public Task<SocialIdentity?> VerifyAsync(LoginMethod provider, string token, CancellationToken ct = default) => provider switch
	{
		LoginMethod.Google => VerifyOidcAsync(GoogleConfig, ["https://accounts.google.com", "accounts.google.com"], _options.GoogleClientIds, token, ct),
		LoginMethod.Apple => VerifyOidcAsync(AppleConfig, ["https://appleid.apple.com"], _options.AppleClientIds, token, ct),
		LoginMethod.Facebook => VerifyFacebookAsync(token, ct),
		_ => Task.FromResult<SocialIdentity?>(null),
	};

	private async Task<SocialIdentity?> VerifyOidcAsync(
		ConfigurationManager<OpenIdConnectConfiguration> manager, string[] issuers, List<string> audiences, string token, CancellationToken ct)
	{
		if (audiences.Count == 0)
		{
			_logger.LogWarning("Login social non configurato: nessun client id per {Issuer}", issuers[0]);
			return null;
		}

		var config = await manager.GetConfigurationAsync(ct);
		var result = await _handler.ValidateTokenAsync(token, new TokenValidationParameters
		{
			ValidIssuers = issuers,
			ValidAudiences = audiences,
			IssuerSigningKeys = config.SigningKeys,
			ClockSkew = TimeSpan.FromMinutes(2),
		});
		if (!result.IsValid)
		{
			_logger.LogInformation(result.Exception, "ID token {Issuer} rifiutato", issuers[0]);
			return null;
		}

		var claims = result.Claims;
		string? Get(string name) => claims.TryGetValue(name, out var v) ? v?.ToString() : null;
		var subject = Get("sub");
		if (string.IsNullOrEmpty(subject))
			return null;

		var emailVerified = Get("email_verified") is { } verified && verified.Equals("true", StringComparison.OrdinalIgnoreCase);
		return new SocialIdentity(subject, Get("email"), emailVerified, Get("given_name"), Get("family_name"), Get("picture"));
	}

	private async Task<SocialIdentity?> VerifyFacebookAsync(string accessToken, CancellationToken ct)
	{
		if (string.IsNullOrEmpty(_options.FacebookAppId) || string.IsNullOrEmpty(_options.FacebookAppSecret))
		{
			_logger.LogWarning("Login Facebook non configurato");
			return null;
		}

		var client = _http.CreateClient(HttpClientName);
		var appToken = Uri.EscapeDataString($"{_options.FacebookAppId}|{_options.FacebookAppSecret}");
		using var debug = await GetJsonAsync(client, $"https://graph.facebook.com/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={appToken}", ct);
		if (debug is null
			|| !debug.RootElement.TryGetProperty("data", out var data)
			|| !data.TryGetProperty("is_valid", out var valid) || !valid.GetBoolean()
			|| !data.TryGetProperty("app_id", out var appId) || appId.GetString() != _options.FacebookAppId)
			return null;

		var proof = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.FacebookAppSecret), Encoding.UTF8.GetBytes(accessToken)));
		using var me = await GetJsonAsync(client,
			$"https://graph.facebook.com/me?fields=id,email,first_name,last_name,picture.type(large)&access_token={Uri.EscapeDataString(accessToken)}&appsecret_proof={proof}", ct);
		if (me is null || !me.RootElement.TryGetProperty("id", out var id))
			return null;

		var root = me.RootElement;
		string? Get(string name) => root.TryGetProperty(name, out var v) ? v.GetString() : null;
		var picture = root.TryGetProperty("picture", out var pic) && pic.TryGetProperty("data", out var pd) && pd.TryGetProperty("url", out var url)
			? url.GetString() : null;

		// Graph API restituisce solo l'email primaria confermata del profilo.
		var email = Get("email");
		return new SocialIdentity(id.GetString()!, email, email is not null, Get("first_name"), Get("last_name"), picture);
	}

	private async Task<JsonDocument?> GetJsonAsync(HttpClient client, string url, CancellationToken ct)
	{
		using var response = await client.GetAsync(url, ct);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogInformation("Graph API Facebook ha risposto {Status}", (int)response.StatusCode);
			return null;
		}
		return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
	}

	private static ConfigurationManager<OpenIdConnectConfiguration> Oidc(string metadataAddress)
		=> new(metadataAddress, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever { RequireHttps = true });
}
