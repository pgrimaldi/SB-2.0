using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Module.Identity.Application;
using Module.Identity.Application.Dtos;
using Shared.Contracts.Errors;

namespace Module.Identity.Endpoints;

/// <summary>
/// Sezione <c>Auth:RefreshCookie</c>. Il prefisso <c>__Secure-</c> richiede HTTPS: in sviluppo su HTTP si
/// configurano un altro nome e <c>Secure = false</c>.
/// </summary>
public sealed class RefreshCookieOptions
{
	public const string Section = "Auth:RefreshCookie";

	public string Name { get; set; } = "__Secure-sb_rt";
	/// <summary>Tutto il modulo: anche /me/password e /me/sessions devono riconoscere la sessione corrente.</summary>
	public string Path { get; set; } = "/api/identity";
	public bool Secure { get; set; } = true;
}

/// <summary>Cookie HttpOnly del refresh token.</summary>
internal static class RefreshCookie
{
	public static string? Read(HttpContext context)
		=> context.Request.Cookies[Options(context).Name];

	/// <summary>"Ricordami" = cookie persistente; altrimenti cookie di sessione, senza Max-Age né Expires.</summary>
	public static void Write(HttpContext context, SessionResult session)
	{
		var options = Options(context);
		var cookie = Base(options);
		if (session.IsPersistent)
		{
			cookie.Expires = session.RefreshExpiresAt;
			cookie.MaxAge = session.RefreshExpiresAt - DateTime.UtcNow;
		}
		context.Response.Cookies.Append(options.Name, session.RefreshToken, cookie);
	}

	public static void Delete(HttpContext context)
	{
		var options = Options(context);
		context.Response.Cookies.Delete(options.Name, Base(options));
	}

	/// <summary>Oltre a SameSite=Strict: un Origin presente deve essere fra quelli ammessi.</summary>
	public static void EnsureAllowedOrigin(HttpContext context)
	{
		var allowed = context.RequestServices.GetRequiredService<IOptions<IdentityAuthOptions>>().Value.AllowedOrigins;
		var origin = context.Request.Headers.Origin.ToString();
		if (origin.Length > 0 && allowed.Count > 0 && !allowed.Contains(origin, StringComparer.OrdinalIgnoreCase))
			throw DomainException.Forbidden(CommonErrorCodes.Forbidden);
	}

	private static RefreshCookieOptions Options(HttpContext context)
		=> context.RequestServices.GetRequiredService<IOptions<RefreshCookieOptions>>().Value;

	private static CookieOptions Base(RefreshCookieOptions options) => new()
	{
		HttpOnly = true,
		Secure = options.Secure,
		SameSite = SameSiteMode.Strict,
		Path = options.Path,
		IsEssential = true,
	};
}
