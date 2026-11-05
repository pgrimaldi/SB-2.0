using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Dtos;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Impostazioni › Listino: pacchetti, articoli aggiuntivi, «stagionale ridotto», importazione ed esportazione.</summary>
internal static class PriceListEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var read = Permissions.PolicyName(Permissions.CatalogRead);
		var write = Permissions.PolicyName(Permissions.CatalogPricing);

		var lists = api.MapGroup("/price-lists").WithTags("Catalog - Listino");

		lists.MapGet("/", async ([AsParameters] PriceListQuery query, PriceListsService service, CancellationToken ct) => Results.Ok(await service.ListAsync(query, ct)))
			.Produces<PagedResult<PriceListListItemDto>>()
			.RequireAuthorization(read);

		lists.MapGet("/lookup", async (string? kind, PriceListsService service, CancellationToken ct) => Results.Ok(await service.LookupAsync(kind, ct)))
			.Produces<IReadOnlyList<PriceListLookupDto>>()
			.RequireAuthorization(read);

		lists.MapGet("/export", async (string? format, PriceListTransferService service, CancellationToken ct) =>
		{
			var file = await service.ExportAsync(Format(format), ct);
			return Results.File(file.Content, file.ContentType, file.FileName);
		})
			.RequireAuthorization(write);

		lists.MapGet("/import-template", async (string? format, PriceListTransferService service, CancellationToken ct) =>
		{
			var file = await service.TemplateAsync(Format(format), ct);
			return Results.File(file.Content, file.ContentType, file.FileName);
		})
			.RequireAuthorization(write);

		lists.MapPost("/import", async (IFormFile file, PriceListTransferService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.ImportAsync(new UploadedFile(file.FileName, file.ContentType, file.Length, content), ct));
		})
			.Produces<PriceListImportResult>()
			.DisableAntiforgery()
			.RequireAuthorization(write);

		lists.MapGet("/{id:guid}", async (Guid id, PriceListsService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
			.Produces<PriceListDto>()
			.RequireAuthorization(read);

		lists.MapPost("/", async (SavePriceListRequest request, PriceListsService service, CancellationToken ct) =>
		{
			var created = await service.CreateAsync(request, ct);
			return Results.Created($"/api/catalog/price-lists/{created.Id}", created);
		})
			.Produces<PriceListDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		lists.MapPut("/{id:guid}", async (Guid id, SavePriceListRequest request, PriceListsService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, ct)))
			.Produces<PriceListDto>()
			.RequireAuthorization(write);

		lists.MapDelete("/{id:guid}", async (Guid id, PriceListsService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);

		lists.MapPost("/bulk-delete", async (BulkIdsRequest request, PriceListsService service, CancellationToken ct) => Results.Ok(await service.DeleteManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);

		lists.MapPost("/{id:guid}/duplicate", async (Guid id, PriceListsService service, CancellationToken ct) =>
		{
			var copy = await service.DuplicateAsync(id, ct);
			return Results.Created($"/api/catalog/price-lists/{copy.Id}", copy);
		})
			.Produces<PriceListDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		lists.MapPost("/bulk-duplicate", async (BulkIdsRequest request, PriceListsService service, CancellationToken ct) => Results.Ok(await service.DuplicateManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);

		var reduced = api.MapGroup("/reduced-seasons").WithTags("Catalog - Listino");

		reduced.MapGet("/", async (ReducedSeasonsService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
			.Produces<IReadOnlyList<ReducedSeasonDto>>()
			.RequireAuthorization(read);

		reduced.MapPost("/", async (SaveReducedSeasonRequest request, ReducedSeasonsService service, CancellationToken ct) =>
		{
			var created = await service.CreateAsync(request, ct);
			return Results.Created($"/api/catalog/reduced-seasons/{created.Id}", created);
		})
			.Produces<ReducedSeasonDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		reduced.MapPut("/{id:guid}", async (Guid id, SaveReducedSeasonRequest request, ReducedSeasonsService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, ct)))
			.Produces<ReducedSeasonDto>()
			.RequireAuthorization(write);

		reduced.MapDelete("/{id:guid}", async (Guid id, ReducedSeasonsService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);
	}

	private static TabularFormat Format(string? code) => code == "xlsx" ? TabularFormat.Xlsx : TabularFormat.Csv;
}
