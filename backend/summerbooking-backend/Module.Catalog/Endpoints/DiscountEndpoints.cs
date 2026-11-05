using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Dtos;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Impostazioni › Sconti e voucher dello stabilimento del token.</summary>
internal static class DiscountEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var read = Permissions.PolicyName(Permissions.CatalogRead);
		var write = Permissions.PolicyName(Permissions.CatalogPricing);
		var discounts = api.MapGroup("/discounts").WithTags("Catalog - Sconti e voucher");

		discounts.MapGet("/", async ([AsParameters] DiscountQuery query, DiscountsService service, CancellationToken ct) => Results.Ok(await service.ListAsync(query, ct)))
			.Produces<PagedResult<DiscountDto>>()
			.RequireAuthorization(read);

		discounts.MapGet("/{id:guid}", async (Guid id, DiscountsService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
			.Produces<DiscountDto>()
			.RequireAuthorization(read);

		discounts.MapPost("/", async (SaveDiscountRequest request, DiscountsService service, CancellationToken ct) =>
		{
			var created = await service.CreateAsync(request, ct);
			return Results.Created($"/api/catalog/discounts/{created.Id}", created);
		})
			.Produces<DiscountDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		discounts.MapPut("/{id:guid}", async (Guid id, SaveDiscountRequest request, DiscountsService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, ct)))
			.Produces<DiscountDto>()
			.RequireAuthorization(write);

		discounts.MapDelete("/{id:guid}", async (Guid id, DiscountsService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);

		discounts.MapPost("/bulk-delete", async (BulkIdsRequest request, DiscountsService service, CancellationToken ct) => Results.Ok(await service.DeleteManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);
	}
}
