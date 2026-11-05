using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Dtos;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Configura mappa (impostazioni) e configurazione del reticolo dello stabilimento del token.</summary>
internal static class MapEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var read = Permissions.PolicyName(Permissions.CatalogRead);
		var write = Permissions.PolicyName(Permissions.CatalogSettings);

		var map = api.MapGroup("/map").WithTags("Catalog - Mappa");

		map.MapGet("/", async (MapSettingsService service, CancellationToken ct) => Results.Ok(await service.GetMapAsync(ct)))
			.Produces<MapDto>()
			.RequireAuthorization(read);

		map.MapGet("/settings", async (MapSettingsService service, CancellationToken ct) => Results.Ok(await service.GetSettingsAsync(ct)))
			.Produces<MapSettingsDto>()
			.RequireAuthorization(read);

		map.MapPut("/settings", async (SaveMapSettingsRequest request, MapSettingsService service, CancellationToken ct)
			=> Results.Ok(await service.SaveSettingsAsync(request, ct)))
			.Produces<MapSettingsDto>()
			.RequireAuthorization(write);

		map.MapPost("/background", async (IFormFile file, MapSettingsService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.UploadBackgroundAsync(file.FileName, file.ContentType, file.Length, content, ct));
		})
			.Produces<MapSettingsDto>()
			.DisableAntiforgery()
			.RequireAuthorization(write);

		map.MapDelete("/background", async (MapSettingsService service, CancellationToken ct) => Results.Ok(await service.RemoveBackgroundAsync(ct)))
			.Produces<MapSettingsDto>()
			.RequireAuthorization(write);

		map.MapPut("/icons/umbrella", async (SaveMapIconPairRequest request, MapSettingsService service, CancellationToken ct)
			=> Results.Ok(await service.SaveUmbrellaIconsAsync(request, ct)))
			.Produces<MapSettingsDto>()
			.RequireAuthorization(write);

		map.MapPut("/icons/custom-articles/{variant:int}", async (short variant, SaveMapIconPairRequest request, MapSettingsService service, CancellationToken ct)
			=> Results.Ok(await service.SaveCustomArticleIconsAsync(variant, request, ct)))
			.Produces<MapSettingsDto>()
			.RequireAuthorization(write);

		map.MapGet("/icon-catalog", async (string? category, MapSettingsService service, CancellationToken ct)
			=> Results.Ok(await service.ListIconsAsync(category, ct)))
			.Produces<IReadOnlyList<MapIconDto>>()
			.RequireAuthorization(read);

		map.MapGet("/beach-elements", async (MapSettingsService service, CancellationToken ct) => Results.Ok(await service.ListBeachElementsAsync(ct)))
			.Produces<IReadOnlyList<MapBeachElementDto>>()
			.RequireAuthorization(read);

		var spots = map.MapGroup("/spots");

		spots.MapPost("/", async (FillMapCellsRequest request, MapEditorService service, CancellationToken ct)
			=> Results.Created("/api/catalog/map", await service.FillAsync(request, ct)))
			.Produces<IReadOnlyList<MapSpotDto>>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		spots.MapPut("/{id:guid}", async (Guid id, UpdateMapSpotRequest request, MapEditorService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, ct)))
			.Produces<MapSpotDto>()
			.RequireAuthorization(write);

		spots.MapPut("/placements", async (PlaceMapSpotsRequest request, MapEditorService service, CancellationToken ct)
			=> Results.Ok(await service.PlaceAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);

		spots.MapDelete("/{id:guid}", async (Guid id, MapEditorService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);

		spots.MapPost("/bulk-delete", async (BulkIdsRequest request, MapEditorService service, CancellationToken ct)
			=> Results.Ok(await service.DeleteManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);

		MapLines(map, "/rows", MapAxis.Row, write);
		MapLines(map, "/columns", MapAxis.Column, write);

		map.MapPost("/reset", async (MapEditorService service, CancellationToken ct) => Results.Ok(await service.ResetAsync(ct)))
			.Produces<MapDto>()
			.RequireAuthorization(write);
	}

	private static void MapLines(RouteGroupBuilder map, string prefix, MapAxis axis, string policy)
	{
		var lines = map.MapGroup(prefix);

		lines.MapPost("/", async (InsertMapLineRequest request, MapEditorService service, CancellationToken ct)
			=> Results.Ok(await service.InsertLineAsync(axis, request.At, ct)))
			.Produces<MapDto>()
			.RequireAuthorization(policy);

		lines.MapDelete("/{index:int}", async (short index, MapEditorService service, CancellationToken ct)
			=> Results.Ok(await service.DeleteLineAsync(axis, index, ct)))
			.Produces<MapDto>()
			.RequireAuthorization(policy);

		lines.MapPost("/{index:int}/clear", async (short index, MapEditorService service, CancellationToken ct)
			=> Results.Ok(await service.ClearLineAsync(axis, index, ct)))
			.Produces<MapDto>()
			.RequireAuthorization(policy);
	}
}
