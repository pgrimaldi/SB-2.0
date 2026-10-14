using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Services;

namespace Module.Identity.Endpoints;

/// <summary>Area personale dell'utente autenticato: profilo, password, preferenze, sessioni, preferiti.</summary>
internal static class AccountEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var me = api.MapGroup("/me").WithTags("Identity - Me");

		me.MapGet("/", async (MeService service, CancellationToken ct) => Results.Ok(await service.GetAsync(ct)));

		// PUT: il profilo si invia intero, un campo null viene svuotato.
		me.MapPut("/", async (UpdateProfileRequest request, MeService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateProfileAsync(request, ct)));

		// POST: comando con effetti oltre al dato (chiude le altre sessioni e ne apre una nuova), non ripetibile.
		me.MapPost("/password", async (ChangePasswordRequest request, MeService service, HttpContext http, CancellationToken ct)
			=> AuthEndpoints.Session(http, await service.ChangePasswordAsync(request, RefreshCookie.Read(http), ct)));

		// PATCH: si inviano solo le preferenze da cambiare, null = invariata.
		me.MapPatch("/preferences", async (UpdatePreferencesRequest request, MeService service, CancellationToken ct)
			=> Results.Ok(await service.UpdatePreferencesAsync(request, ct)));

		me.MapDelete("/", async ([FromBody] DeleteAccountRequest request, MeService service, HttpContext http, CancellationToken ct) =>
		{
			await service.DeleteAsync(request, ct);
			RefreshCookie.Delete(http);
			return Results.NoContent();
		});

		me.MapGet("/sessions", async (MeService service, HttpContext http, CancellationToken ct)
			=> Results.Ok(await service.ListSessionsAsync(RefreshCookie.Read(http), ct)));

		me.MapDelete("/sessions/{sessionId:guid}", async (Guid sessionId, MeService service, CancellationToken ct) =>
		{
			await service.RevokeSessionAsync(sessionId, ct);
			return Results.NoContent();
		});

		var likes = me.MapGroup("/liked-bathhouses").WithTags("Identity - Preferiti");

		likes.MapGet("/", async (BathhouseLikeService service, CancellationToken ct, int page = 1, int pageSize = 20)
			=> Results.Ok(await service.ListAsync(page, pageSize, ct)));

		likes.MapPut("/{bathhouseId:guid}", async (Guid bathhouseId, BathhouseLikeService service, CancellationToken ct) =>
		{
			await service.LikeAsync(bathhouseId, ct);
			return Results.NoContent();
		});

		likes.MapDelete("/{bathhouseId:guid}", async (Guid bathhouseId, BathhouseLikeService service, CancellationToken ct) =>
		{
			await service.UnlikeAsync(bathhouseId, ct);
			return Results.NoContent();
		});
	}
}
