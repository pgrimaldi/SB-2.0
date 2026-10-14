using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Audit.Application.Dtos;
using Module.Audit.Application.Services;
using Shared.Dtos;
using Shared.Security;
using Shared.Utils;

namespace Module.Audit.Endpoints;

/// <summary>Registro di tutta la piattaforma per il back-office del systemadmin.</summary>
internal static class PlatformAuditEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var admin = api.MapGroup("/admin").WithTags("Audit - Systemadmin")
			.RequireAuthorization(Permissions.PolicyName(Permissions.PlatformManage));

		admin.MapGet("/entries", async ([AsParameters] PlatformAuditEntryListQuery query, PlatformAuditService service, CancellationToken ct)
			=> Results.Ok(await service.ListAsync(query, ct)))
			.Produces<PagedResult<PlatformAuditEntryDto>>();

		admin.MapGet("/entries/export", async ([AsParameters] PlatformAuditEntryListQuery query, string? format, PlatformAuditService service, CancellationToken ct) =>
		{
			var file = await service.ExportAsync(query, Codes.TryParse<TabularFormat>(format, out var f) ? f : TabularFormat.Csv, ct);
			return Results.File(file.Content, file.ContentType, file.FileName);
		});

		admin.MapGet("/action-codes", (PlatformAuditService service) => Results.Ok(service.ActionCodes()))
			.Produces<IReadOnlyList<AuditActionCodeDto>>();
	}
}
