using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Dtos;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Impostazioni › Servizi: servizi, prezzi automatici e servizio tavoli dello stabilimento del token.</summary>
internal static class ServiceEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var read = Permissions.PolicyName(Permissions.CatalogRead);
		var write = Permissions.PolicyName(Permissions.CatalogSettings);

		var services = api.MapGroup("/services").WithTags("Catalog - Servizi");

		services.MapGet("/", async ([AsParameters] ServiceListQuery query, ServiceItemsService service, CancellationToken ct)
			=> Results.Ok(await service.ListAsync(query, ct)))
			.Produces<PagedResult<ServiceListItemDto>>()
			.RequireAuthorization(read);

		services.MapGet("/lookup", async (ServiceItemsService service, CancellationToken ct) => Results.Ok(await service.LookupAsync(ct)))
			.Produces<IReadOnlyList<ServiceLookupDto>>()
			.RequireAuthorization(read);

		services.MapGet("/{id:guid}", async (Guid id, ServiceItemsService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
			.Produces<ServiceDto>()
			.RequireAuthorization(read);

		services.MapPost("/", async (SaveServiceRequest request, ServiceItemsService service, CancellationToken ct) =>
		{
			var created = await service.CreateAsync(request, ct);
			return Results.Created($"/api/catalog/services/{created.Id}", created);
		})
			.Produces<ServiceDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		services.MapPut("/{id:guid}", async (Guid id, SaveServiceRequest request, ServiceItemsService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, ct)))
			.Produces<ServiceDto>()
			.RequireAuthorization(write);

		services.MapDelete("/{id:guid}", async (Guid id, ServiceItemsService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);

		services.MapPost("/bulk-delete", async (BulkIdsRequest request, ServiceItemsService service, CancellationToken ct)
			=> Results.Ok(await service.DeleteManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);

		services.MapPost("/{id:guid}/duplicate", async (Guid id, ServiceItemsService service, CancellationToken ct) =>
		{
			var copy = await service.DuplicateAsync(id, ct);
			return Results.Created($"/api/catalog/services/{copy.Id}", copy);
		})
			.Produces<ServiceDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		services.MapPost("/bulk-duplicate", async (BulkIdsRequest request, ServiceItemsService service, CancellationToken ct)
			=> Results.Ok(await service.DuplicateManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);

		services.MapPost("/{id:guid}/image", async (Guid id, IFormFile file, ServiceItemsService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.UploadImageAsync(id, new UploadedFile(file.FileName, file.ContentType, file.Length, content), ct));
		})
			.Produces<ServiceDto>()
			.DisableAntiforgery()
			.RequireAuthorization(write);

		services.MapDelete("/{id:guid}/image", async (Guid id, ServiceItemsService service, CancellationToken ct) => Results.Ok(await service.RemoveImageAsync(id, ct)))
			.Produces<ServiceDto>()
			.RequireAuthorization(write);

		var automatic = api.MapGroup("/automatic-prices").WithTags("Catalog - Servizi");

		automatic.MapGet("/", async (AutomaticPricesService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
			.Produces<IReadOnlyList<AutomaticPriceDto>>()
			.RequireAuthorization(read);

		automatic.MapGet("/{id:guid}", async (Guid id, AutomaticPricesService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
			.Produces<AutomaticPriceDto>()
			.RequireAuthorization(read);

		automatic.MapPost("/", async (SaveAutomaticPriceRequest request, AutomaticPricesService service, CancellationToken ct) =>
		{
			var created = await service.CreateAsync(request, ct);
			return Results.Created($"/api/catalog/automatic-prices/{created.Id}", created);
		})
			.Produces<AutomaticPriceDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		automatic.MapPut("/{id:guid}", async (Guid id, SaveAutomaticPriceRequest request, AutomaticPricesService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, ct)))
			.Produces<AutomaticPriceDto>()
			.RequireAuthorization(write);

		automatic.MapDelete("/{id:guid}", async (Guid id, AutomaticPricesService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);

		automatic.MapPost("/bulk-delete", async (BulkIdsRequest request, AutomaticPricesService service, CancellationToken ct)
			=> Results.Ok(await service.DeleteManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);

		automatic.MapPost("/{id:guid}/duplicate", async (Guid id, AutomaticPricesService service, CancellationToken ct) =>
		{
			var copy = await service.DuplicateAsync(id, ct);
			return Results.Created($"/api/catalog/automatic-prices/{copy.Id}", copy);
		})
			.Produces<AutomaticPriceDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		automatic.MapPost("/bulk-duplicate", async (BulkIdsRequest request, AutomaticPricesService service, CancellationToken ct)
			=> Results.Ok(await service.DuplicateManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);

		automatic.MapPost("/{id:guid}/image", async (Guid id, IFormFile file, AutomaticPricesService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.UploadImageAsync(id, new UploadedFile(file.FileName, file.ContentType, file.Length, content), ct));
		})
			.Produces<AutomaticPriceDto>()
			.DisableAntiforgery()
			.RequireAuthorization(write);

		var tables = api.MapGroup("/table-services").WithTags("Catalog - Servizi");

		tables.MapGet("/", async (TableServicesService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
			.Produces<IReadOnlyList<TableServiceDto>>()
			.RequireAuthorization(read);

		tables.MapGet("/{id:guid}", async (Guid id, TableServicesService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
			.Produces<TableServiceDto>()
			.RequireAuthorization(read);

		tables.MapPost("/", async (SaveTableServiceRequest request, TableServicesService service, CancellationToken ct) =>
		{
			var created = await service.CreateAsync(request, ct);
			return Results.Created($"/api/catalog/table-services/{created.Id}", created);
		})
			.Produces<TableServiceDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		tables.MapPut("/{id:guid}", async (Guid id, SaveTableServiceRequest request, TableServicesService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, ct)))
			.Produces<TableServiceDto>()
			.RequireAuthorization(write);

		tables.MapDelete("/{id:guid}", async (Guid id, TableServicesService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);

		tables.MapPost("/bulk-delete", async (BulkIdsRequest request, TableServicesService service, CancellationToken ct)
			=> Results.Ok(await service.DeleteManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);

		tables.MapPost("/{id:guid}/duplicate", async (Guid id, TableServicesService service, CancellationToken ct) =>
		{
			var copy = await service.DuplicateAsync(id, ct);
			return Results.Created($"/api/catalog/table-services/{copy.Id}", copy);
		})
			.Produces<TableServiceDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		tables.MapPost("/bulk-duplicate", async (BulkIdsRequest request, TableServicesService service, CancellationToken ct)
			=> Results.Ok(await service.DuplicateManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(write);
	}
}
