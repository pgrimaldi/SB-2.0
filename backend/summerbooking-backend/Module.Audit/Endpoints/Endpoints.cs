using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Module.Audit.Endpoints;

/// <summary>
/// Definizione degli endpoint HTTP (Minimal API) del modulo Audit, sotto <c>/api/audit</c>.
/// Registrati dal progetto Boot: app.MapAuditEndpoints();
/// </summary>
public static class Endpoints
{
	/// <summary>Documento Swagger del modulo.</summary>
	public const string GroupName = "audit";

	public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
	{
		var api = app.MapGroup("/api/audit").WithGroupName(GroupName);

		PropertyAuditEndpoints.Map(api);
		PlatformAuditEndpoints.Map(api);

		return app;
	}
}
