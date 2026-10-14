using System.Globalization;
using Microsoft.Extensions.Options;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Shared.Contracts.Audit;
using Shared.Contracts.Infra;
using Shared.Infrastructure;

namespace Module.Identity.Application.Services;

/// <summary>
/// Token <c>ApiBearer</c> per i partner M2M. Il registro dei client è in Infra; qui si emette il token.
/// Oltre a client_credentials, i contratti fissati dai partner: OfSale/Transactions (firma HMAC) e TeamSystem
/// (OAuth in querystring). Il timestamp della firma è controllato, la scadenza del client rispettata, il secret
/// non è mai a DB.
/// </summary>
public sealed class ExternalTokenService
{
	// Motivi del rifiuto nel registro di audit.
	private const string ReasonInvalidSignature = "invalid_signature";
	private const string ReasonStaleTimestamp = "stale_timestamp";

	private readonly IPartnerClientAuthenticator _partners;
	private readonly ITokenIssuer _issuer;
	private readonly IAuditTrail _audit;
	private readonly IdentityAuthOptions _options;
	private readonly IDateTimeProvider _clock;

	public ExternalTokenService(IPartnerClientAuthenticator partners, ITokenIssuer issuer, IAuditTrail audit, IOptions<IdentityAuthOptions> options, IDateTimeProvider clock)
	{
		_partners = partners;
		_issuer = issuer;
		_audit = audit;
		_options = options.Value;
		_clock = clock;
	}

	/// <summary>client_credentials; null se le credenziali non sono valide.</summary>
	public async Task<ExternalTokenResponse?> IssueAsync(ExternalTokenRequest request, CancellationToken ct = default)
	{
		if (request.GrantType is not (null or OAuthValues.GrantClientCredentials) || string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
			return null;

		var client = await _partners.ValidateSecretAsync(request.ClientId, request.ClientSecret, ct);
		if (client is null)
		{
			await RecordAsync(IdentityAudit.ExternalTokenDenied, request.ClientId, null, ct);
			return null;
		}

		var token = await IssueForAsync(client, TimeSpan.FromMinutes(_options.ExternalTokenMinutes), ct);
		return new ExternalTokenResponse(token.Token, OAuthValues.TokenTypeBearer, token.ExpiresInSeconds);
	}

	/// <summary>Contratto <c>auth/signinApi</c>: firma HMAC di <c>"apiKey:timestamp"</c>.</summary>
	public async Task<string?> IssueSignedAsync(SignedPartnerTokenRequest request, CancellationToken ct = default)
	{
		if (!IsFresh(request.Timestamp!))
		{
			await RecordAsync(IdentityAudit.ExternalTokenDenied, request.ApiKey, ReasonStaleTimestamp, ct);
			return null;
		}

		var client = await _partners.ValidateSignatureAsync(request.ApiKey!, request.Signature!, request.Timestamp!, ct);
		if (client is null)
		{
			await RecordAsync(IdentityAudit.ExternalTokenDenied, request.ApiKey, ReasonInvalidSignature, ct);
			return null;
		}

		return (await IssueForAsync(client, TimeSpan.FromHours(_options.SignedPartnerTokenHours), ct)).Token;
	}

	/// <summary>Contratto TeamSystem: solo client dell'app TeamSystem.</summary>
	public async Task<string?> IssueTeamSystemAsync(string clientId, string clientSecret, CancellationToken ct = default)
	{
		var client = await _partners.ValidateSecretAsync(clientId, clientSecret, ct);
		if (client is null || client.App != PartnerApps.TeamSystem)
		{
			await RecordAsync(IdentityAudit.ExternalTokenDenied, clientId, OAuthValues.ErrorInvalidClient, ct);
			return null;
		}

		return (await IssueForAsync(client, TimeSpan.FromDays(_options.TeamSystemTokenDays), ct)).Token;
	}

	private async Task<IssuedToken> IssueForAsync(PartnerClient client, TimeSpan lifetime, CancellationToken ct)
	{
		var token = _issuer.IssueExternalToken(new ExternalTokenSubject(client.PublicId, client.ClientId, client.App, client.PropertyId), lifetime);
		await _audit.RecordAsync(new AuditEntry(
			AuditCategory.Security, IdentityAudit.Module, IdentityAudit.ExternalTokenIssued, PropertyId: client.PropertyId,
			ActorKind: AuditActorKind.ExternalClient, Metadata: new Dictionary<string, object?> { ["client_id"] = client.ClientId, ["app"] = client.App }), ct);
		return token;
	}

	/// <summary>Accetta secondi o millisecondi Unix, oppure una data ISO 8601.</summary>
	private bool IsFresh(string timestamp)
	{
		if (_options.PartnerSignatureToleranceSeconds <= 0)
			return true;

		DateTime instant;
		if (long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
			instant = unix > 100_000_000_000 ? DateTime.UnixEpoch.AddMilliseconds(unix) : DateTime.UnixEpoch.AddSeconds(unix);
		else if (DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
			instant = parsed;
		else
			return false;

		return Math.Abs((_clock.UtcNow - instant).TotalSeconds) <= _options.PartnerSignatureToleranceSeconds;
	}

	private Task RecordAsync(string action, string? clientId, string? reason, CancellationToken ct)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Security, IdentityAudit.Module, action, AuditOutcome.Failed, ActorKind: AuditActorKind.ExternalClient,
			Metadata: new Dictionary<string, object?> { ["client_id"] = clientId, ["reason"] = reason }), ct);
}
