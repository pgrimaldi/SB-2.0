using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Stagioni, periodi tariffari e settori dello stabilimento del token.</summary>
internal static class SeasonEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var read = Permissions.PolicyName(Permissions.CatalogRead);
		var write = Permissions.PolicyName(Permissions.CatalogSettings);

		var season = api.MapGroup("/season").WithTags("Catalog - Stagioni e periodi");

		season.MapGet("/", async (SeasonService service, CancellationToken ct)
			=> await service.GetAsync(ct) is { } dto ? Results.Ok(dto) : Results.NoContent())
			.Produces<SeasonDto>()
			.Produces(StatusCodes.Status204NoContent)
			.RequireAuthorization(read);

		season.MapPut("/", async (SaveSeasonRequest request, SeasonService service, CancellationToken ct)
			=> Results.Ok(await service.SaveAsync(request, ct)))
			.Produces<SeasonDto>()
			.RequireAuthorization(write);

		var periods = api.MapGroup("/tariff-periods").WithTags("Catalog - Stagioni e periodi");

		periods.MapGet("/", async (SeasonService service, CancellationToken ct) => Results.Ok(await service.ListPeriodsAsync(ct)))
			.Produces<IReadOnlyList<TariffPeriodDto>>()
			.RequireAuthorization(read);

		periods.MapPost("/", async (SaveTariffPeriodRequest request, SeasonService service, CancellationToken ct) =>
		{
			var period = await service.CreatePeriodAsync(request, ct);
			return Results.Created($"/api/catalog/tariff-periods/{period.Key}", period);
		})
			.Produces<TariffPeriodDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		periods.MapPut("/{key:int}", async (short key, SaveTariffPeriodRequest request, SeasonService service, CancellationToken ct)
			=> Results.Ok(await service.UpdatePeriodAsync(key, request, ct)))
			.Produces<TariffPeriodDto>()
			.RequireAuthorization(write);

		periods.MapDelete("/{key:int}", async (short key, SeasonService service, CancellationToken ct) =>
		{
			await service.DeletePeriodAsync(key, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);

		var next = api.MapGroup("/next-season/tariff-periods").WithTags("Catalog - Stagioni e periodi");

		next.MapGet("/", async (SeasonService service, CancellationToken ct) => Results.Ok(await service.ListNextPeriodsAsync(ct)))
			.Produces<IReadOnlyList<TariffPeriodDto>>()
			.RequireAuthorization(read);

		next.MapPost("/", async (SaveTariffPeriodRequest request, SeasonService service, CancellationToken ct) =>
		{
			var period = await service.CreateNextPeriodAsync(request, ct);
			return Results.Created($"/api/catalog/next-season/tariff-periods/{period.Key}", period);
		})
			.Produces<TariffPeriodDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		next.MapPut("/{key:int}", async (short key, SaveTariffPeriodRequest request, SeasonService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateNextPeriodAsync(key, request, ct)))
			.Produces<TariffPeriodDto>()
			.RequireAuthorization(write);

		next.MapDelete("/{key:int}", async (short key, SeasonService service, CancellationToken ct) =>
		{
			await service.DeleteNextPeriodAsync(key, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);

		var sectors = api.MapGroup("/sectors").WithTags("Catalog - Settori e file");

		sectors.MapGet("/", async (SectorService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
			.Produces<IReadOnlyList<SectorDto>>()
			.RequireAuthorization(read);

		sectors.MapPost("/", async (SaveSectorRequest request, SectorService service, CancellationToken ct) =>
		{
			var sector = await service.CreateAsync(request, ct);
			return Results.Created($"/api/catalog/sectors/{sector.Id}", sector);
		})
			.Produces<SectorDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		sectors.MapPut("/{id:guid}", async (Guid id, SaveSectorRequest request, SectorService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, ct)))
			.Produces<SectorDto>()
			.RequireAuthorization(write);

		sectors.MapDelete("/{id:guid}", async (Guid id, SectorService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);
	}
}
