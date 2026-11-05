using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Services;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Security;
using Shared.Utils;

namespace Module.Catalog.Endpoints;

/// <summary>
/// Snapshot di configurazione: il JSON gia' pronto con tutta la configurazione per un uso (avvio del gestionale, mappa,
/// vetrina, listino, servizi, convenzioni). Con ETag: se il client manda If-None-Match uguale riceve 304 senza corpo.
/// </summary>
internal static class SnapshotEndpoints
{
	private static readonly ConfigSnapshotKind[] PublicKinds = [ConfigSnapshotKind.PublicShowcase, ConfigSnapshotKind.BeachMap];

	public static void Map(RouteGroupBuilder api)
	{
		api.MapGet("/snapshots/{kind}", async (string kind, HttpContext http, CurrentProperty property, IConfigSnapshotRepository snapshots,
				ConfigSnapshotService service, CancellationToken ct)
			=> await ServeAsync(http, await property.IdAsync(ct), ParseKind(kind), snapshots, service, ct))
			.WithTags("Catalog - Snapshot")
			.RequireAuthorization(Permissions.PolicyName(Permissions.CatalogRead));

		api.MapGet("/public/bathhouses/{slug}/snapshots/{kind}", async (string slug, string kind, HttpContext http, IPublicCatalogRepository publicCatalog,
				IConfigSnapshotRepository snapshots, ConfigSnapshotService service, CancellationToken ct) =>
		{
			var parsed = ParseKind(kind);
			if (!PublicKinds.Contains(parsed))
				throw DomainException.NotFound(CatalogErrors.SnapshotNotFound);
			var row = await publicCatalog.FindPublishedBySlugAsync(slug.Trim().ToLowerInvariant(), ct) ?? throw DomainException.NotFound(CatalogErrors.BathhouseNotFound);
			return await ServeAsync(http, row.PropertyId, parsed, snapshots, service, ct);
		})
			.WithTags("Catalog - Sito pubblico")
			.AllowAnonymous();

		api.MapGet("/public/media-origins", (HttpContext http, IMediaUrls urls) =>
		{
			var body = JsonSerializer.Serialize(urls.Origins.Append(new KeyValuePair<string, string>("external", "")).ToDictionary());
			var etag = $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)))[..16].ToLowerInvariant()}\"";
			return Respond(http, etag, body);
		})
			.WithTags("Catalog - Sito pubblico")
			.AllowAnonymous();

		api.MapPost("/admin/snapshots/rebuild", async (RebuildSnapshotsRequest request, IPropertyRepository properties, ConfigSnapshotService service, CancellationToken ct) =>
		{
			List<long>? ids = null;
			if (request.PropertyIds is { Count: > 0 })
			{
				ids = [];
				foreach (var id in request.PropertyIds)
					ids.Add(await properties.FindIdAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.PropertyNotFound));
			}
			var kinds = request.Kinds is { Count: > 0 } ? request.Kinds.Select(ParseKind).ToList() : ConfigSnapshotService.AllKinds.ToList();
			return Results.Ok(await service.RebuildAsync(ids, kinds, request.OnlyMissing, ct));
		})
			.WithTags("Catalog - Systemadmin")
			.RequireAuthorization(Permissions.PolicyName(Permissions.PlatformManage));
	}

	private static async Task<IResult> ServeAsync(HttpContext http, long propertyId, ConfigSnapshotKind kind, IConfigSnapshotRepository snapshots,
		ConfigSnapshotService service, CancellationToken ct)
	{
		var row = await snapshots.FindAsync(propertyId, kind, ct);
		if (row is null)
		{
			await service.RebuildAsync([propertyId], [kind], ct: ct);
			row = await snapshots.FindAsync(propertyId, kind, ct) ?? throw DomainException.NotFound(CatalogErrors.SnapshotNotFound);
		}
		var etag = $"\"{Convert.ToHexString(row.ContentHash)[..16].ToLowerInvariant()}\"";
		return Respond(http, etag, row.Payload);
	}

	private static IResult Respond(HttpContext http, string etag, string body)
	{
		http.Response.Headers.ETag = etag;
		http.Response.Headers.CacheControl = "no-cache";
		return http.Request.Headers.IfNoneMatch.Contains(etag)
			? Results.StatusCode(StatusCodes.Status304NotModified)
			: Results.Content(body, "application/json; charset=utf-8");
	}

	private static ConfigSnapshotKind ParseKind(string kind)
		=> Codes.TryParse<ConfigSnapshotKind>(kind, out var parsed) ? parsed : throw DomainException.NotFound(CatalogErrors.SnapshotNotFound);
}

/// <param name="PropertyIds">Vuoto = tutti gli stabilimenti.</param>
/// <param name="Kinds">Vuoto = tutti i tipi.</param>
/// <param name="OnlyMissing">Solo quelli che mancano.</param>
public sealed record RebuildSnapshotsRequest(IReadOnlyList<Guid>? PropertyIds, IReadOnlyList<string>? Kinds, bool OnlyMissing = false);
