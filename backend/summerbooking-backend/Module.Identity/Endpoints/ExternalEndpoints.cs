using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Services;
using Module.Identity.Domain;
using Shared.Contracts.Errors;

namespace Module.Identity.Endpoints;

/// <summary>
/// Token per i partner M2M. <c>/api/auth/signinApi</c> e <c>/api/sb/teamsystem/...</c> sono chiamati da
/// software di terzi già in produzione: indirizzo e forma delle risposte non si cambiano.
/// </summary>
internal static class ExternalEndpoints
{
	private static readonly Encoding Latin1 = Encoding.Latin1;

	public static void Map(IEndpointRouteBuilder app, RouteGroupBuilder api)
	{
		api.MapPost("/external/token", async (ExternalTokenRequest request, ExternalTokenService service, CancellationToken ct)
			=> await service.IssueAsync(request, ct) is { } token
				? Results.Ok(token)
				: throw DomainException.Unauthorized(IdentityErrors.InvalidClient))
			.WithTags("Identity - Partner esterni")
			.AllowAnonymous();

		// OfSale, Transactions: { apiKey, signature, timestamp } -> { token }.
		app.MapPost("/api/auth/signinApi", async (SignedPartnerTokenRequest request, ExternalTokenService service, CancellationToken ct) =>
		{
			if (string.IsNullOrEmpty(request.ApiKey) || string.IsNullOrEmpty(request.Signature) || string.IsNullOrEmpty(request.Timestamp))
				return Results.Json(new { error = "Missing fields" }, statusCode: 400);

			return await service.IssueSignedAsync(request, ct) is { } token
				? Results.Json(new { token })
				: Results.Json(new { error = "Invalid signature" }, statusCode: 403);
		})
			.WithGroupName(Endpoints.GroupName)
			.WithTags("Identity - Partner esterni")
			.AllowAnonymous();

		// TeamSystem / Cassa in Cloud: credenziali in querystring, risposta in ISO-8859-1 come da specifica.
		app.MapGet("/api/sb/teamsystem/v1/oauth/access_token", async (
			string? client_id, string? client_secret, string? grant_type,
			ExternalTokenService service, ILoggerFactory loggers, CancellationToken ct) =>
		{
			try
			{
				if (string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(client_secret) || grant_type != OAuthValues.GrantClientCredentials)
					return Latin1Json(new { error = OAuthValues.ErrorInvalidRequest }, 400);

				return await service.IssueTeamSystemAsync(client_id, client_secret, ct) is { } token
					? Latin1Json(new { access_token = token }, 200)
					: Latin1Json(new { error = OAuthValues.ErrorInvalidClient }, 403);
			}
			catch (Exception ex)
			{
				// Il client POS si aspetta il proprio formato d'errore, non Problem Details.
				loggers.CreateLogger("Identity.TeamSystem").LogError(ex, "Errore nell'emissione del token TeamSystem");
				return Latin1Json(new { error = OAuthValues.ErrorServerError }, 500);
			}
		})
			.WithGroupName(Endpoints.GroupName)
			.WithTags("Identity - Partner esterni")
			.AllowAnonymous();
	}

	private static IResult Latin1Json(object body, int status)
		=> Results.Text(JsonSerializer.Serialize(body), "application/json", Latin1, status);
}
