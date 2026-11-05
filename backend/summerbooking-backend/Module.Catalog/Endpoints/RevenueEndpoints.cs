using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Revenue: impostazioni e prezzo suggerito dello stabilimento; pesi e periodi standard del systemadmin.</summary>
internal static class RevenueEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var revenue = api.MapGroup("/revenue").WithTags("Catalog - Revenue").RequireAuthorization(Permissions.PolicyName(Permissions.RevenueManage));

		revenue.MapGet("/settings", async (RevenueService service, CancellationToken ct) => Results.Ok(await service.GetSettingsAsync(ct)))
			.Produces<RevenueSettingsDto>();

		revenue.MapPut("/settings", async (SaveRevenueSettingsRequest request, RevenueService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateSettingsAsync(request, ct)))
			.Produces<RevenueSettingsDto>();

		revenue.MapGet("/suggested-prices", async ([AsParameters] SuggestedPriceQuery query, RevenueService service, CancellationToken ct)
			=> Results.Ok(await service.ListSuggestedPricesAsync(query, ct)))
			.Produces<IReadOnlyList<SuggestedPriceDto>>();

		revenue.MapPost("/suggested-prices/compute", async (ComputeSuggestedPricesRequest request, RevenueService service, CancellationToken ct)
			=> Results.Ok(await service.ComputeAsync(request, ct)))
			.Produces<IReadOnlyList<SuggestedPriceDto>>();

		revenue.MapPost("/suggested-prices/apply", async (ApplySuggestedPricesRequest request, RevenueService service, CancellationToken ct)
			=> Results.Ok(await service.ApplyAsync(request, ct)))
			.Produces<IReadOnlyList<SuggestedPriceDto>>();

		var admin = api.MapGroup("/admin/revenue").WithTags("Catalog - Systemadmin").RequireAuthorization(Permissions.PolicyName(Permissions.PlatformManage));

		admin.MapGet("/weights", async (RevenueAdminService service, CancellationToken ct) => Results.Ok(await service.GetWeightsAsync(ct)))
			.Produces<RevenueWeightsDto>();

		admin.MapPut("/weights", async (RevenueWeightsDto request, RevenueAdminService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateWeightsAsync(request, ct)))
			.Produces<RevenueWeightsDto>();

		admin.MapGet("/standard-periods", async (RevenueAdminService service, CancellationToken ct) => Results.Ok(await service.ListStandardPeriodsAsync(ct)))
			.Produces<IReadOnlyList<RevenueStandardPeriodDto>>();

		admin.MapPut("/standard-periods", async (SaveRevenueStandardPeriodsRequest request, RevenueAdminService service, CancellationToken ct)
			=> Results.Ok(await service.ReplaceStandardPeriodsAsync(request, ct)))
			.Produces<IReadOnlyList<RevenueStandardPeriodDto>>();
	}
}
