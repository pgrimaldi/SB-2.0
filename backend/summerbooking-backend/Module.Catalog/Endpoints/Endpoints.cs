using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Module.Catalog.Endpoints;

/// <summary>
/// Definizione degli endpoint HTTP (Minimal API) del modulo Catalog, sotto <c>/api/catalog</c>.
/// Registrati dal progetto Boot: app.MapCatalogEndpoints();
/// Senza attributi un endpoint richiede il token utente (fallback policy dell'host).
/// </summary>
public static class Endpoints
{
	/// <summary>Documento Swagger del modulo.</summary>
	public const string GroupName = "catalog";

	public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
	{
		var api = app.MapGroup("/api/catalog").WithGroupName(GroupName);

		InventoryEndpoints.Map(api);
		SeasonEndpoints.Map(api);
		MapEndpoints.Map(api);
		LookupEndpoints.Map(api);
		PropertyInfoEndpoints.Map(api);
		OnlineSettingsEndpoints.Map(api);
		ServiceEndpoints.Map(api);
		PriceListEndpoints.Map(api);
		DiscountEndpoints.Map(api);
		AgreementEndpoints.Map(api);
		RevenueEndpoints.Map(api);
		PlatformEndpoints.Map(api);
		PublicEndpoints.Map(api);
		QuoteEndpoints.Map(api);
		SnapshotEndpoints.Map(api);

		return app;
	}
}
