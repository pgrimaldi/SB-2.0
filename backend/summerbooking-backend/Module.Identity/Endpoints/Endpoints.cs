using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Module.Identity.Endpoints;

/// <summary>
/// Definizione degli endpoint HTTP (Minimal API) del modulo Identity, sotto <c>/api/identity</c>.
/// Registrati dal progetto Boot: app.MapIdentityEndpoints();
/// Senza attributi un endpoint richiede il token utente (fallback policy dell'host).
/// </summary>
public static class Endpoints
{
	/// <summary>Documento Swagger del modulo.</summary>
	public const string GroupName = "identity";

	public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
	{
		var api = app.MapGroup("/api/identity").WithGroupName(GroupName);

		AuthEndpoints.Map(api);
		AccountEndpoints.Map(api);
		PropertyEndpoints.Map(api);
		PlatformEndpoints.Map(api);
		ExternalEndpoints.Map(app, api);

		return app;
	}
}
