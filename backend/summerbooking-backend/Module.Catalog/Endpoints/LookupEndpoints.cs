using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Elenchi di piattaforma; paesi e dotazioni servono anche alla registrazione e al sito pubblico.</summary>
internal static class LookupEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var read = Permissions.PolicyName(Permissions.CatalogRead);
		var lookups = api.MapGroup("/lookups").WithTags("Catalog - Elenchi");

		lookups.MapGet("/countries", async (LookupService service, CancellationToken ct) => Results.Ok(await service.ListCountriesAsync(ct)))
			.Produces<IReadOnlyList<CountryDto>>()
			.AllowAnonymous();

		lookups.MapGet("/amenity-categories", async (LookupService service, CancellationToken ct) => Results.Ok(await service.ListAmenityCategoriesAsync(ct)))
			.Produces<IReadOnlyList<AmenityCategoryDto>>()
			.AllowAnonymous();

		lookups.MapGet("/service-icons", async (string? type, LookupService service, CancellationToken ct) => Results.Ok(await service.ListServiceIconsAsync(type, ct)))
			.Produces<IReadOnlyList<ServiceIconDto>>()
			.RequireAuthorization(read);

		lookups.MapGet("/calculation-methods", async (LookupService service, CancellationToken ct) => Results.Ok(await service.ListCalculationMethodsAsync(ct)))
			.Produces<IReadOnlyList<string>>()
			.RequireAuthorization(read);

		lookups.MapGet("/vat-options", async (LookupService service, CancellationToken ct) => Results.Ok(await service.VatOptionsAsync(ct)))
			.Produces<VatOptionsDto>()
			.RequireAuthorization(read);
	}
}
