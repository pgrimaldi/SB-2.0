using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Dtos;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Magazzino dello stabilimento del token.</summary>
internal static class InventoryEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		api.MapGet("/inventory-articles", () => Results.Ok(InventoryService.Articles()))
			.WithTags("Catalog - Magazzino")
			.Produces<IReadOnlyList<InventoryArticleDto>>()
			.RequireAuthorization(Permissions.PolicyName(Permissions.InventoryRead));

		var read = api.MapGroup("/inventory-items").WithTags("Catalog - Magazzino")
			.RequireAuthorization(Permissions.PolicyName(Permissions.InventoryRead));

		read.MapGet("/", async ([AsParameters] InventoryItemListQuery query, InventoryService service, CancellationToken ct)
			=> Results.Ok(await service.ListAsync(query, ct)))
			.Produces<PagedResult<InventoryItemDto>>();

		read.MapGet("/lookup", async (InventoryService service, CancellationToken ct) => Results.Ok(await service.LookupAsync(ct)))
			.Produces<IReadOnlyList<InventoryItemLookupDto>>();

		read.MapGet("/{id:guid}", async (Guid id, InventoryService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
			.Produces<InventoryItemDto>();

		var write = api.MapGroup("/inventory-items").WithTags("Catalog - Magazzino")
			.RequireAuthorization(Permissions.PolicyName(Permissions.InventoryManage));

		write.MapPost("/", async (SaveInventoryItemRequest request, InventoryService service, CancellationToken ct) =>
		{
			var item = await service.CreateAsync(request, ct);
			return Results.Created($"/api/catalog/inventory-items/{item.Id}", item);
		})
			.Produces<InventoryItemDto>(StatusCodes.Status201Created);

		write.MapPut("/{id:guid}", async (Guid id, SaveInventoryItemRequest request, InventoryService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, ct)))
			.Produces<InventoryItemDto>();

		write.MapDelete("/{id:guid}", async (Guid id, InventoryService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, ct);
			return Results.NoContent();
		});

		write.MapPost("/bulk-delete", async (BulkIdsRequest request, InventoryService service, CancellationToken ct)
			=> Results.Ok(await service.DeleteManyAsync(request, ct)))
			.Produces<BulkResult>();
	}
}
