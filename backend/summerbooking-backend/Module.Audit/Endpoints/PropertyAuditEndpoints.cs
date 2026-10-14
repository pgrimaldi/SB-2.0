using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Audit.Application.Dtos;
using Module.Audit.Application.Services;
using Shared.Dtos;
using Shared.Security;
using Shared.Utils;

namespace Module.Audit.Endpoints;

/// <summary>Registro dello stabilimento del token: pagina «Tutti i log» e pulsante «Log» dell'utente.</summary>
internal static class PropertyAuditEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var audit = api.MapGroup("/").WithTags("Audit - Registro dello stabilimento")
			.RequireAuthorization(Permissions.PolicyName(Permissions.AuditRead));

		audit.MapGet("/entries", async ([AsParameters] AuditEntryListQuery query, AuditEntryService service, CancellationToken ct)
			=> Results.Ok(await service.ListAsync(query, ct)))
			.Produces<PagedResult<AuditEntryDto>>();

		audit.MapGet("/entries/export", async ([AsParameters] AuditEntryListQuery query, string? format, AuditEntryService service, CancellationToken ct) =>
		{
			var file = await service.ExportAsync(query, Codes.TryParse<TabularFormat>(format, out var f) ? f : TabularFormat.Csv, ct);
			return Results.File(file.Content, file.ContentType, file.FileName);
		});

		audit.MapGet("/action-codes", (AuditEntryService service) => Results.Ok(service.ActionCodes()))
			.Produces<IReadOnlyList<AuditActionCodeDto>>();

		audit.MapGet("/actors", async (string? search, AuditEntryService service, CancellationToken ct)
			=> Results.Ok(await service.ActorsAsync(search, ct)))
			.Produces<IReadOnlyList<AuditActorDto>>();
	}
}
