using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>
/// Snapshot di configurazione: il JSON gia' pronto con tutta la configurazione per un uso (avvio del gestionale, mappa,
/// vetrina, listino, servizi, convenzioni). Con ETag: se il client manda If-None-Match uguale riceve 304 senza corpo.
/// </summary>
internal static class SnapshotEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		api.MapGet("/snapshots/{kind}", async (string kind, HttpContext http, SnapshotReadService service, CancellationToken ct)
			=> Respond(http, await service.GetAsync(kind, ct)))
			.WithTags("Catalog - Snapshot")
			.RequireAuthorization(Permissions.PolicyName(Permissions.CatalogRead));

		api.MapGet("/public/bathhouses/{slug}/snapshots/{kind}", async (string slug, string kind, HttpContext http, SnapshotReadService service, CancellationToken ct)
			=> Respond(http, await service.GetPublicAsync(slug, kind, ct)))
			.WithTags("Catalog - Sito pubblico")
			.AllowAnonymous();

		api.MapGet("/public/media-origins", (HttpContext http, IMediaUrls urls) =>
		{
			var body = JsonSerializer.Serialize(urls.Origins.Append(new KeyValuePair<string, string>("external", "")).ToDictionary());
			var etag = $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)))[..16].ToLowerInvariant()}\"";
			return Respond(http, new SnapshotContent(body, etag));
		})
			.WithTags("Catalog - Sito pubblico")
			.AllowAnonymous();

		api.MapPost("/admin/snapshots/rebuild", async (RebuildSnapshotsRequest request, SnapshotReadService service, CancellationToken ct)
			=> Results.Ok(await service.RebuildAsync(request, ct)))
			.WithTags("Catalog - Systemadmin")
			.RequireAuthorization(Permissions.PolicyName(Permissions.PlatformManage));
	}

	private static IResult Respond(HttpContext http, SnapshotContent snapshot)
	{
		http.Response.Headers.ETag = snapshot.ETag;
		http.Response.Headers.CacheControl = "no-cache";
		return http.Request.Headers.IfNoneMatch.Contains(snapshot.ETag)
			? Results.StatusCode(StatusCodes.Status304NotModified)
			: Results.Content(snapshot.Payload, "application/json; charset=utf-8");
	}
}
