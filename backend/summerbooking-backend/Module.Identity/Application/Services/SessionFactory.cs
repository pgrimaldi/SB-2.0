using System.Net;
using Microsoft.Extensions.Options;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Infrastructure;
using Shared.Security;

namespace Module.Identity.Application.Services;

/// <summary>Apre, ruota e chiude le sessioni: access token breve + refresh token opaco (salvato solo come hash).</summary>
public sealed class SessionFactory
{
	private readonly IRefreshTokenRepository _tokens;
	private readonly ITokenIssuer _issuer;
	private readonly ISecureTokenGenerator _generator;
	private readonly IdentityAuthOptions _options;
	private readonly IDateTimeProvider _clock;
	private readonly IRequestContext _request;

	public SessionFactory(
		IRefreshTokenRepository tokens, ITokenIssuer issuer, ISecureTokenGenerator generator,
		IOptions<IdentityAuthOptions> options, IDateTimeProvider clock, IRequestContext request)
	{
		_tokens = tokens;
		_issuer = issuer;
		_generator = generator;
		_options = options.Value;
		_clock = clock;
		_request = request;
	}

	public static AuthUserDto ToAuthUser(Account account) => new(account.PublicId, account.Email, account.Role.Code, account.PropertyId);

	public IPAddress? ClientIp => IPAddress.TryParse(_request.ClientIp, out var ip) ? ip : null;

	/// <summary>Nuova sessione; con <paramref name="previous"/> è la rotazione di un refresh (stessa famiglia).</summary>
	public SessionResult Open(Account account, bool persistent, RefreshToken? previous = null)
	{
		var now = _clock.UtcNow;
		var plain = _generator.NewOpaqueToken();
		var days = persistent ? _options.RefreshTokenRememberMeDays : _options.RefreshTokenDays;
		var expiresAt = now.AddDays(days);

		var token = RefreshToken.Issue(
			account, _generator.Hash(plain), previous?.FamilyId ?? Guid.CreateVersion7(), persistent,
			now, expiresAt, _request.UserAgent, ClientIp);
		previous?.RotateTo(token, now);
		_tokens.Add(token);

		var access = _issuer.IssueAccessToken(new AccessTokenSubject(account.PublicId, account.Role.Code, account.PropertyId));
		return new SessionResult(new AuthSessionResponse(access.Token, access.ExpiresInSeconds, ToAuthUser(account)), plain, expiresAt, persistent);
	}

	public async Task RevokeAllAsync(long userId, RefreshRevokeReason reason, CancellationToken ct = default)
	{
		var now = _clock.UtcNow;
		foreach (var token in await _tokens.ListActiveByUserAsync(userId, now, ct))
			token.Revoke(reason, now);
	}
}
