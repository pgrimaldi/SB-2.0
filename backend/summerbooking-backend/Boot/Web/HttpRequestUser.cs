using System.Diagnostics;
using Microsoft.Net.Http.Headers;
using Shared.Localization;
using Shared.Security;

namespace Boot.Web;

/// <summary>Utente e contesto della richiesta HTTP, letti dai claim del token già validato.</summary>
public sealed class HttpRequestUser : ICurrentUser, IRequestContext, IRequestLanguage
{
	private readonly IHttpContextAccessor _accessor;

	public HttpRequestUser(IHttpContextAccessor accessor) => _accessor = accessor;

	private HttpContext? Context => _accessor.HttpContext;

	public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated == true;

	public Guid? UserId => GuidClaim(AuthClaims.Subject);

	public string? Role => IsAuthenticated ? Context!.User.FindFirst(AuthClaims.Role)?.Value : null;

	public Guid? PropertyId => GuidClaim(AuthClaims.PropertyId);

	public Guid? ImpersonatorUserId => IsAuthenticated ? ActorClaim.ReadSubject(Context!.User.FindFirst(AuthClaims.Actor)?.Value) : null;

	public bool HasPermission(string permission) => RolePermissions.Has(Role, permission);

	public string? ClientIp => Context?.Connection.RemoteIpAddress?.ToString();

	public string? UserAgent => Context?.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;

	public string? TraceId => Activity.Current?.Id ?? Context?.TraceIdentifier;

	public AppLanguage Language
	{
		get
		{
			var header = Context?.Request.Headers.AcceptLanguage.ToString();
			if (string.IsNullOrEmpty(header) || !StringWithQualityHeaderValue.TryParseList(header.Split(','), out var values))
				return SupportedLanguages.Default;
			foreach (var value in values.OrderByDescending(v => v.Quality ?? 1))
				if (SupportedLanguages.TryParse(value.Value.Value?.Split('-')[0], out var language))
					return language;
			return SupportedLanguages.Default;
		}
	}

	private Guid? GuidClaim(string type)
		=> IsAuthenticated && Guid.TryParse(Context!.User.FindFirst(type)?.Value, out var value) ? value : null;
}
