using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Preventivo: dal gestionale (stabilimento del token) e dal sito pubblico (per slug, regole online).</summary>
internal static class QuoteEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		api.MapPost("/quotes", async (QuoteRequest request, QuoteService service, CancellationToken ct) => Results.Ok(await service.QuoteAsync(request, ct)))
			.WithTags("Catalog - Preventivo")
			.Produces<QuoteDto>()
			.RequireAuthorization(Permissions.PolicyName(Permissions.CatalogRead));

		api.MapPost("/public/bathhouses/{slug}/quote", async (string slug, QuoteRequest request, QuoteService service, CancellationToken ct)
			=> Results.Ok(await service.QuoteOnlineAsync(slug, request, ct)))
			.WithTags("Catalog - Sito pubblico")
			.Produces<QuoteDto>()
			.AllowAnonymous();
	}
}
