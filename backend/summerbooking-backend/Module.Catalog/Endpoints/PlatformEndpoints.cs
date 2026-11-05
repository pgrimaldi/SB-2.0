using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Dtos;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Pannello di amministrazione (systemadmin): stabilimenti, cestino, catalogo della mappa, vetrina.</summary>
internal static class PlatformEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var admin = api.MapGroup("/admin").WithTags("Catalog - Systemadmin").RequireAuthorization(Permissions.PolicyName(Permissions.PlatformManage));

		var properties = admin.MapGroup("/properties");
		properties.MapGet("/", async ([AsParameters] AdminPropertyQuery query, PlatformPropertiesService service, CancellationToken ct)
			=> Results.Ok(await service.ListAsync(query, ct)))
			.Produces<PagedResult<AdminPropertyDto>>();
		properties.MapGet("/{id:guid}", async (Guid id, PlatformPropertiesService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
			.Produces<AdminPropertyDto>();
		properties.MapPost("/{id:guid}/status", async (Guid id, ChangePropertyStatusRequest request, PlatformPropertiesService service, CancellationToken ct)
			=> Results.Ok(await service.ChangeStatusAsync(id, request, ct)))
			.Produces<AdminPropertyDto>();
		properties.MapPost("/{id:guid}/trash", async (Guid id, PlatformPropertiesService service, CancellationToken ct) =>
		{
			await service.TrashAsync(id, ct);
			return Results.NoContent();
		});
		properties.MapPost("/{id:guid}/restore", async (Guid id, PlatformPropertiesService service, CancellationToken ct) => Results.Ok(await service.RestoreAsync(id, ct)))
			.Produces<AdminPropertyDto>();
		properties.MapDelete("/{id:guid}", async (Guid id, PlatformPropertiesService service, CancellationToken ct) =>
		{
			await service.PurgeAsync(id, ct);
			return Results.NoContent();
		});
		properties.MapPut("/{id:guid}/region", async (Guid id, SetPropertyRegionRequest request, PlatformPropertiesService service, CancellationToken ct)
			=> Results.Ok(await service.SetRegionAsync(id, request, ct)))
			.Produces<AdminPropertyDto>();
		properties.MapPut("/{id:guid}/badges", async (Guid id, SetPropertyBadgesRequest request, PlatformPropertiesService service, CancellationToken ct)
			=> Results.Ok(await service.SetBadgesAsync(id, request, ct)))
			.Produces<AdminPropertyDto>();

		var icons = admin.MapGroup("/map-icons");
		icons.MapGet("/", async (string? category, PlatformMapCatalogService service, CancellationToken ct) => Results.Ok(await service.ListIconsAsync(category, ct)))
			.Produces<IReadOnlyList<AdminMapIconDto>>();
		icons.MapPost("/", async ([FromForm] string category, [FromForm] string state, [FromForm] string name, IFormFile file, PlatformMapCatalogService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			var created = await service.CreateIconAsync(category, state, name, Upload(file, content), ct);
			return Results.Created($"/api/catalog/admin/map-icons/{created.Id}", created);
		})
			.Produces<AdminMapIconDto>(StatusCodes.Status201Created)
			.DisableAntiforgery();
		icons.MapPut("/{id:guid}", async (Guid id, UpdateMapIconRequest request, PlatformMapCatalogService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateIconAsync(id, request, ct)))
			.Produces<AdminMapIconDto>();
		icons.MapPut("/{id:guid}/image", async (Guid id, IFormFile file, PlatformMapCatalogService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.ReplaceIconImageAsync(id, Upload(file, content), ct));
		})
			.Produces<AdminMapIconDto>()
			.DisableAntiforgery();
		icons.MapDelete("/{id:guid}", async (Guid id, PlatformMapCatalogService service, CancellationToken ct) =>
		{
			await service.DeleteIconAsync(id, ct);
			return Results.NoContent();
		});

		var elements = admin.MapGroup("/beach-elements");
		elements.MapGet("/", async (PlatformMapCatalogService service, CancellationToken ct) => Results.Ok(await service.ListBeachElementsAsync(ct)))
			.Produces<IReadOnlyList<AdminBeachElementDto>>();
		elements.MapPost("/", async ([FromForm] string name, [FromForm] short? sortOrder, [FromForm] bool? isActive, IFormFile? file, PlatformMapCatalogService service, CancellationToken ct) =>
		{
			await using var content = file?.OpenReadStream();
			var created = await service.CreateBeachElementAsync(new UpdateBeachElementRequest(name, sortOrder ?? 0, isActive ?? true),
				file is null ? null : Upload(file, content!), ct);
			return Results.Created($"/api/catalog/admin/beach-elements/{created.Id}", created);
		})
			.Produces<AdminBeachElementDto>(StatusCodes.Status201Created)
			.DisableAntiforgery();
		elements.MapPut("/{id:guid}", async (Guid id, UpdateBeachElementRequest request, PlatformMapCatalogService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateBeachElementAsync(id, request, ct)))
			.Produces<AdminBeachElementDto>();
		elements.MapPut("/{id:guid}/image", async (Guid id, IFormFile file, PlatformMapCatalogService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.ReplaceBeachElementImageAsync(id, Upload(file, content), ct));
		})
			.Produces<AdminBeachElementDto>()
			.DisableAntiforgery();
		elements.MapDelete("/{id:guid}", async (Guid id, PlatformMapCatalogService service, CancellationToken ct) =>
		{
			await service.DeleteBeachElementAsync(id, ct);
			return Results.NoContent();
		});

		var badges = admin.MapGroup("/badges");
		badges.MapGet("/", async (PlatformShowcaseService service, CancellationToken ct) => Results.Ok(await service.ListBadgesAsync(ct)))
			.Produces<IReadOnlyList<BadgeDto>>();
		badges.MapPost("/", async (SaveBadgeRequest request, PlatformShowcaseService service, CancellationToken ct) =>
		{
			var created = await service.CreateBadgeAsync(request, ct);
			return Results.Created($"/api/catalog/admin/badges/{created.Id}", created);
		})
			.Produces<BadgeDto>(StatusCodes.Status201Created);
		badges.MapPut("/{id:guid}", async (Guid id, SaveBadgeRequest request, PlatformShowcaseService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateBadgeAsync(id, request, ct)))
			.Produces<BadgeDto>();
		badges.MapDelete("/{id:guid}", async (Guid id, PlatformShowcaseService service, CancellationToken ct) =>
		{
			await service.DeleteBadgeAsync(id, ct);
			return Results.NoContent();
		});

		var regions = admin.MapGroup("/regions");
		regions.MapGet("/", async (PlatformShowcaseService service, CancellationToken ct) => Results.Ok(await service.ListRegionsAsync(ct)))
			.Produces<IReadOnlyList<RegionDto>>();
		regions.MapPost("/", async (SaveRegionRequest request, PlatformShowcaseService service, CancellationToken ct) =>
		{
			var created = await service.CreateRegionAsync(request, ct);
			return Results.Created($"/api/catalog/admin/regions/{created.Id}", created);
		})
			.Produces<RegionDto>(StatusCodes.Status201Created);
		regions.MapPut("/{id:guid}", async (Guid id, SaveRegionRequest request, PlatformShowcaseService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateRegionAsync(id, request, ct)))
			.Produces<RegionDto>();
		regions.MapDelete("/{id:guid}", async (Guid id, PlatformShowcaseService service, CancellationToken ct) =>
		{
			await service.DeleteRegionAsync(id, ct);
			return Results.NoContent();
		});

		var showcase = admin.MapGroup("/showcase");
		showcase.MapGet("/{section}", async (string section, PlatformShowcaseService service, CancellationToken ct) => Results.Ok(await service.ListShowcaseAsync(section, ct)))
			.Produces<IReadOnlyList<ShowcaseEntryDto>>();
		showcase.MapPut("/{section}", async (string section, SaveShowcaseRequest request, PlatformShowcaseService service, CancellationToken ct)
			=> Results.Ok(await service.SaveShowcaseAsync(section, request, ct)))
			.Produces<IReadOnlyList<ShowcaseEntryDto>>();
	}

	private static UploadedFile Upload(IFormFile file, Stream content) => new(file.FileName, file.ContentType, file.Length, content);
}
