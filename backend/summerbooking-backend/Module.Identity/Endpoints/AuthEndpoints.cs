using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Services;
using Module.Identity.Domain.Enums;
using Shared.Security;

namespace Module.Identity.Endpoints;

/// <summary>Accesso, sessione, registrazione e recupero password: tutti anonimi e con rate limit per IP.</summary>
internal static class AuthEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var auth = api.MapGroup("/auth")
			.WithTags("Identity - Auth")
			.AllowAnonymous()
			.RequireRateLimiting(RateLimitPolicies.Auth);

		auth.MapPost("/login", async (LoginRequest request, AuthService service, HttpContext http, CancellationToken ct)
			=> ToResult(http, await service.LoginAsync(request, ct)))
			.Produces<AuthSessionResponse>()
			.Produces<TwoFactorRequiredResponse>();

		auth.MapPost("/login/verify-otp", async (VerifyOtpRequest request, AuthService service, HttpContext http, CancellationToken ct)
			=> Session(http, await service.VerifyOtpAsync(request, ct)));

		foreach (var (path, provider) in new[] { ("google", LoginMethod.Google), ("apple", LoginMethod.Apple), ("facebook", LoginMethod.Facebook) })
		{
			auth.MapPost($"/login/{path}", async (SocialLoginRequest request, SocialAuthService service, HttpContext http, CancellationToken ct)
				=> ToResult(http, await service.LoginAsync(provider, request, ct)))
				.Produces<AuthSessionResponse>()
				.Produces<TwoFactorRequiredResponse>();
		}

		auth.MapPost("/refresh", async (AuthService service, HttpContext http, CancellationToken ct) =>
		{
			RefreshCookie.EnsureAllowedOrigin(http);
			return Session(http, await service.RefreshAsync(RefreshCookie.Read(http), ct));
		});

		auth.MapPost("/logout", async (AuthService service, HttpContext http, CancellationToken ct) =>
		{
			RefreshCookie.EnsureAllowedOrigin(http);
			await service.LogoutAsync(RefreshCookie.Read(http), ct);
			RefreshCookie.Delete(http);
			return Results.NoContent();
		});

		auth.MapPost("/register", async (RegisterRequest request, RegistrationService service, HttpContext http, CancellationToken ct) =>
		{
			var session = await service.RegisterAsync(request, ct);
			RefreshCookie.Write(http, session);
			return Results.Created("/api/identity/me", session.Response);
		});

		auth.MapGet("/email-availability", async (string? email, RegistrationService service, CancellationToken ct)
			=> Results.Ok(await service.CheckEmailAsync(email, ct)));

		auth.MapPost("/password/forgot", async (ForgotPasswordRequest request, AccountRecoveryService service, CancellationToken ct) =>
		{
			await service.ForgotPasswordAsync(request, ct);
			return Results.NoContent();
		});

		auth.MapPost("/password/reset", async (ResetPasswordRequest request, AccountRecoveryService service, CancellationToken ct) =>
		{
			await service.ResetPasswordAsync(request, ct);
			return Results.NoContent();
		});

		auth.MapPost("/impersonation/exchange", async (ImpersonationExchangeRequest request, AuthService service, CancellationToken ct)
			=> Results.Ok(await service.ExchangeImpersonationCodeAsync(request, ct)));
	}

	internal static IResult Session(HttpContext http, SessionResult session)
	{
		RefreshCookie.Write(http, session);
		return Results.Ok(session.Response);
	}

	private static IResult ToResult(HttpContext http, LoginResult result)
		=> result.Session is { } session ? Session(http, session) : Results.Ok(result.TwoFactor);
}
