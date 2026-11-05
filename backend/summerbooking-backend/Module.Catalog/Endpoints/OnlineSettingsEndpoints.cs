using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Impostazioni › Opzioni prenotazioni online dello stabilimento del token.</summary>
internal static class OnlineSettingsEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var read = Permissions.PolicyName(Permissions.CatalogRead);
		var write = Permissions.PolicyName(Permissions.CatalogSettings);
		var online = api.MapGroup("/online-settings").WithTags("Catalog - Prenotazioni online");

		online.MapGet("/", async (OnlineSettingsService service, CancellationToken ct) => Results.Ok(await service.GetAsync(ct)))
			.Produces<OnlineSettingsDto>()
			.RequireAuthorization(read);

		online.MapPut("/", async (SaveOnlineSettingsRequest request, OnlineSettingsService service, CancellationToken ct)
			=> Results.Ok(await service.SaveAsync(request, ct)))
			.Produces<OnlineSettingsDto>()
			.RequireAuthorization(write);

		online.MapGet("/services", async (OnlineSettingsService service, CancellationToken ct) => Results.Ok(await service.GetServicesAsync(ct)))
			.Produces<ServiceOnlineSettingsDto>()
			.RequireAuthorization(read);

		online.MapPut("/services", async (SaveServiceOnlineSettingsRequest request, OnlineSettingsService service, CancellationToken ct)
			=> Results.Ok(await service.SaveServicesAsync(request, ct)))
			.Produces<ServiceOnlineSettingsDto>()
			.RequireAuthorization(write);

		online.MapGet("/stay-limits", async (OnlineSettingsService service, CancellationToken ct) => Results.Ok(await service.ListStayLimitsAsync(ct)))
			.Produces<IReadOnlyList<StayLimitDto>>()
			.RequireAuthorization(read);

		online.MapPost("/stay-limits", async (SaveStayLimitRequest request, OnlineSettingsService service, CancellationToken ct) =>
		{
			var limit = await service.CreateStayLimitAsync(request, ct);
			return Results.Created($"/api/catalog/online-settings/stay-limits/{limit.Id}", limit);
		})
			.Produces<StayLimitDto>(StatusCodes.Status201Created)
			.RequireAuthorization(write);

		online.MapPut("/stay-limits/{id:guid}", async (Guid id, SaveStayLimitRequest request, OnlineSettingsService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateStayLimitAsync(id, request, ct)))
			.Produces<StayLimitDto>()
			.RequireAuthorization(write);

		online.MapDelete("/stay-limits/{id:guid}", async (Guid id, OnlineSettingsService service, CancellationToken ct) =>
		{
			await service.DeleteStayLimitAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(write);

		online.MapGet("/legend", async (OnlineSettingsService service, CancellationToken ct) => Results.Ok(await service.GetLegendAsync(ct)))
			.Produces<IReadOnlyList<LegendItemDto>>()
			.RequireAuthorization(read);

		online.MapPut("/legend", async (SaveLegendRequest request, OnlineSettingsService service, CancellationToken ct)
			=> Results.Ok(await service.SaveLegendAsync(request, ct)))
			.Produces<IReadOnlyList<LegendItemDto>>()
			.RequireAuthorization(write);
	}
}
