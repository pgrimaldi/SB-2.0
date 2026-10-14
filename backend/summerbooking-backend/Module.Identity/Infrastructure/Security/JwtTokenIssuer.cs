using Shared.Utils;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Module.Identity.Application;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Domain.Enums;
using Shared.Infrastructure;
using Shared.Security;

namespace Module.Identity.Infrastructure.Security;

/// <summary>JWT HS256 con <c>kid</c>. I ticket intermedi (2FA, impersonazione) hanno audience propri e non valgono come accesso.</summary>
public sealed class JwtTokenIssuer : ITokenIssuer
{
	private const string MfaAudience = "summerbooking-mfa";
	private const string ImpersonationAudience = "summerbooking-impersonation";

	private readonly JwtOptions _jwt;
	private readonly IdentityAuthOptions _auth;
	private readonly IDateTimeProvider _clock;
	private readonly JsonWebTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };
	private readonly SymmetricSecurityKey[] _userKeys;
	private readonly SymmetricSecurityKey[] _apiKeys;

	public JwtTokenIssuer(IOptions<JwtOptions> jwt, IOptions<IdentityAuthOptions> auth, IDateTimeProvider clock)
	{
		_jwt = jwt.Value;
		_auth = auth.Value;
		_clock = clock;
		_userKeys = BuildKeys(_jwt.UserKeys, "Jwt:UserKeys");
		_apiKeys = BuildKeys(_jwt.ApiKeys, "Jwt:ApiKeys");
	}

	public static SymmetricSecurityKey[] BuildKeys(IEnumerable<JwtKey> keys, string section)
	{
		var built = keys
			.Where(k => !string.IsNullOrWhiteSpace(k.Key))
			.Select(k =>
			{
				var bytes = Convert.FromBase64String(k.Key);
				if (bytes.Length < 32)
					throw new InvalidOperationException($"{section}: la chiave '{k.Kid}' deve avere almeno 256 bit.");
				return new SymmetricSecurityKey(bytes) { KeyId = k.Kid };
			})
			.ToArray();
		return built.Length > 0 ? built : throw new InvalidOperationException($"{section}: nessuna chiave di firma configurata.");
	}

	public IssuedToken IssueAccessToken(AccessTokenSubject subject, TimeSpan? lifetime = null)
	{
		var claims = new Dictionary<string, object>
		{
			[AuthClaims.Subject] = subject.UserId.ToString(),
			[AuthClaims.Role] = subject.Role,
			[JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString("N"),
		};
		if (subject.PropertyId is { } propertyId)
			claims[AuthClaims.PropertyId] = propertyId.ToString();
		if (subject.ImpersonatorUserId is { } impersonator)
			claims[AuthClaims.Actor] = ActorClaim.Create(impersonator);

		return Issue(claims, _jwt.UserAudience, _userKeys[0], lifetime ?? TimeSpan.FromMinutes(_auth.AccessTokenMinutes));
	}

	public IssuedToken IssueMfaTicket(MfaTicket ticket)
		=> Issue(new Dictionary<string, object>
		{
			[AuthClaims.Subject] = ticket.UserId.ToString(),
			["remember"] = ticket.Remember,
			["app"] = ticket.App,
			["method"] = Codes.Of(ticket.Method),
		}, MfaAudience, _userKeys[0], TimeSpan.FromMinutes(_auth.OtpChallengeMinutes));

	public async Task<MfaTicket?> ReadMfaTicketAsync(string token)
	{
		var claims = await ValidateAsync(token, MfaAudience, _userKeys);
		if (claims is null
			|| !Guid.TryParse(Get(claims, AuthClaims.Subject), out var userId)
			|| !Codes.TryParse<LoginMethod>(Get(claims, "method"), out var method)
			|| Get(claims, "app") is not { } app)
			return null;

		return new MfaTicket(userId, claims.TryGetValue("remember", out var r) && r is true, app, method);
	}

	public IssuedToken IssueImpersonationTicket(ImpersonationTicket ticket)
		=> Issue(new Dictionary<string, object>
		{
			[AuthClaims.Subject] = ticket.TargetUserId.ToString(),
			[AuthClaims.Actor] = ActorClaim.Create(ticket.ImpersonatorUserId),
			[JwtRegisteredClaimNames.Jti] = ticket.TicketId,
		}, ImpersonationAudience, _userKeys[0], TimeSpan.FromSeconds(_auth.ImpersonationTicketSeconds));

	public async Task<ImpersonationTicket?> ReadImpersonationTicketAsync(string token)
	{
		var claims = await ValidateAsync(token, ImpersonationAudience, _userKeys);
		if (claims is null
			|| !Guid.TryParse(Get(claims, AuthClaims.Subject), out var target)
			|| ActorClaim.ReadSubject(claims.TryGetValue(AuthClaims.Actor, out var actor) ? actor : null) is not { } impersonator
			|| Get(claims, JwtRegisteredClaimNames.Jti) is not { } ticketId)
			return null;

		return new ImpersonationTicket(ticketId, target, impersonator);
	}

	public IssuedToken IssueExternalToken(ExternalTokenSubject subject, TimeSpan lifetime)
	{
		var claims = new Dictionary<string, object>
		{
			[AuthClaims.Subject] = subject.ClientPublicId.ToString(),
			[AuthClaims.ClientId] = subject.ClientId,
			[AuthClaims.PartnerApp] = subject.App,
			[JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString("N"),
		};
		if (subject.PropertyId is { } propertyId)
			claims[AuthClaims.PropertyId] = propertyId.ToString();

		return Issue(claims, _jwt.ApiAudience, _apiKeys[0], lifetime);
	}

	private IssuedToken Issue(Dictionary<string, object> claims, string audience, SecurityKey key, TimeSpan lifetime)
	{
		var now = _clock.UtcNow;
		var descriptor = new SecurityTokenDescriptor
		{
			Issuer = _jwt.Issuer,
			Audience = audience,
			Claims = claims,
			IssuedAt = now,
			NotBefore = now,
			Expires = now.Add(lifetime),
			SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
		};
		return new IssuedToken(_handler.CreateToken(descriptor), (int)lifetime.TotalSeconds);
	}

	private async Task<IDictionary<string, object>?> ValidateAsync(string token, string audience, SecurityKey[] keys)
	{
		var result = await _handler.ValidateTokenAsync(token, new TokenValidationParameters
		{
			ValidIssuer = _jwt.Issuer,
			ValidAudience = audience,
			IssuerSigningKeys = keys,
			ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
			ClockSkew = TimeSpan.FromSeconds(_jwt.ClockSkewSeconds),
			LifetimeValidator = (notBefore, expires, _, p) =>
				expires is { } e && e.ToUniversalTime() + p.ClockSkew > _clock.UtcNow,
		});
		return result.IsValid ? result.Claims : null;
	}

	private static string? Get(IDictionary<string, object> claims, string name)
		=> claims.TryGetValue(name, out var value) ? value?.ToString() : null;
}
