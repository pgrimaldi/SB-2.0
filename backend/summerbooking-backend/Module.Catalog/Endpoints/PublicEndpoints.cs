using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Sito pubblico, senza autenticazione: home, ricerca, scheda della struttura. I testi seguono Accept-Language.</summary>
internal static class PublicEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var pub = api.MapGroup("/public").WithTags("Catalog - Sito pubblico").AllowAnonymous();

		pub.MapGet("/home", async (PublicShowcaseService service, CancellationToken ct) => Results.Ok(await service.HomeAsync(ct)))
			.Produces<PublicHomeDto>();
		pub.MapGet("/search", async ([AsParameters] PublicSearchQuery query, PublicShowcaseService service, CancellationToken ct)
			=> Results.Ok(await service.SearchAsync(query, ct)))
			.Produces<PublicSearchResultDto>();
		pub.MapGet("/suggestions", async (string? text, PublicShowcaseService service, CancellationToken ct) => Results.Ok(await service.SuggestAsync(text, ct)))
			.Produces<PublicSuggestionsDto>();

		var bathhouse = pub.MapGroup("/bathhouses/{slug}");
		bathhouse.MapGet("/", async (string slug, PublicBathhouseService service, CancellationToken ct) => Results.Ok(await service.GetAsync(slug, ct)))
			.Produces<PublicBathhouseDto>();
		bathhouse.MapGet("/map", async (string slug, PublicBathhouseService service, CancellationToken ct) => Results.Ok(await service.MapAsync(slug, ct)))
			.Produces<PublicMapDto>();
		bathhouse.MapGet("/services", async (string slug, PublicBathhouseService service, CancellationToken ct) => Results.Ok(await service.ServicesAsync(slug, ct)))
			.Produces<PublicServicesDto>();
		bathhouse.MapGet("/packages", async (string slug, DateOnly? from, DateOnly? to, PublicBathhouseService service, CancellationToken ct)
			=> Results.Ok(await service.PackagesAsync(slug, from, to, ct)))
			.Produces<IReadOnlyList<PublicPackageDto>>();
		bathhouse.MapGet("/discount-banner", async (string slug, PublicBathhouseService service, CancellationToken ct) => Results.Ok(await service.BannerAsync(slug, ct)))
			.Produces<PublicDiscountBannerDto>();
		bathhouse.MapPost("/discount-banner/views", async (string slug, PublicBathhouseService service, CancellationToken ct)
			=> Results.Ok(await service.CountBannerViewAsync(slug, ct)))
			.Produces<PublicDiscountBannerDto>();

		// systemadmin: ricostruzione completa della proiezione di ricerca (la fa anche il job notturno)
		api.MapPost("/admin/search/rebuild", async (ISearchProjection search, CancellationToken ct)
			=> Results.Ok(new { rows = await search.RefreshAsync(SearchScope.All, ct) }))
			.WithTags("Catalog - Systemadmin")
			.RequireAuthorization(Permissions.PolicyName(Permissions.PlatformManage));
	}
}
