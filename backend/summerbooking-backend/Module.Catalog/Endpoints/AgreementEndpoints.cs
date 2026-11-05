using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Dtos;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Convenzioni dello stabilimento del token: strutture, listini e voucher, report.</summary>
internal static class AgreementEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var manage = Permissions.PolicyName(Permissions.AgreementsManage);
		var read = Permissions.PolicyName(Permissions.CatalogRead);

		var partners = api.MapGroup("/agreement-partners").WithTags("Catalog - Convenzioni");

		partners.MapGet("/", async ([AsParameters] AgreementPartnerQuery query, AgreementsService service, CancellationToken ct) => Results.Ok(await service.ListPartnersAsync(query, ct)))
			.Produces<PagedResult<AgreementPartnerDto>>()
			.RequireAuthorization(manage);

		partners.MapGet("/lookup", async (AgreementsService service, CancellationToken ct) => Results.Ok(await service.LookupPartnersAsync(ct)))
			.Produces<IReadOnlyList<AgreementPartnerLookupDto>>()
			.RequireAuthorization(read);

		partners.MapGet("/{id:guid}", async (Guid id, AgreementsService service, CancellationToken ct) => Results.Ok(await service.GetPartnerAsync(id, ct)))
			.Produces<AgreementPartnerDto>()
			.RequireAuthorization(manage);

		partners.MapPost("/", async (SaveAgreementPartnerRequest request, AgreementsService service, CancellationToken ct) =>
		{
			var created = await service.CreatePartnerAsync(request, ct);
			return Results.Created($"/api/catalog/agreement-partners/{created.Id}", created);
		})
			.Produces<AgreementPartnerDto>(StatusCodes.Status201Created)
			.RequireAuthorization(manage);

		partners.MapPut("/{id:guid}", async (Guid id, SaveAgreementPartnerRequest request, AgreementsService service, CancellationToken ct)
			=> Results.Ok(await service.UpdatePartnerAsync(id, request, ct)))
			.Produces<AgreementPartnerDto>()
			.RequireAuthorization(manage);

		partners.MapDelete("/{id:guid}", async (Guid id, AgreementsService service, CancellationToken ct) =>
		{
			await service.DeletePartnerAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(manage);

		partners.MapPost("/bulk-delete", async (BulkIdsRequest request, AgreementsService service, CancellationToken ct) => Results.Ok(await service.DeletePartnersAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(manage);

		partners.MapPost("/{id:guid}/duplicate", async (Guid id, AgreementsService service, CancellationToken ct) =>
		{
			var copy = await service.DuplicatePartnerAsync(id, ct);
			return Results.Created($"/api/catalog/agreement-partners/{copy.Id}", copy);
		})
			.Produces<AgreementPartnerDto>(StatusCodes.Status201Created)
			.RequireAuthorization(manage);

		partners.MapGet("/{id:guid}/price-lists", async (Guid id, string? listType, string? kind, AgreementsService service, CancellationToken ct)
			=> Results.Ok(await service.ListPriceListsAsync(id, listType, kind, ct)))
			.Produces<IReadOnlyList<AgreementPriceListDto>>()
			.RequireAuthorization(manage);

		partners.MapPost("/{id:guid}/price-lists", async (Guid id, SaveAgreementPriceListRequest request, AgreementsService service, CancellationToken ct) =>
		{
			var created = await service.CreatePriceListAsync(id, request, ct);
			return Results.Created($"/api/catalog/agreement-price-lists/{created.Id}", created);
		})
			.Produces<AgreementPriceListDto>(StatusCodes.Status201Created)
			.RequireAuthorization(manage);

		var lists = api.MapGroup("/agreement-price-lists").WithTags("Catalog - Convenzioni");

		lists.MapGet("/{id:guid}", async (Guid id, AgreementsService service, CancellationToken ct) => Results.Ok(await service.GetPriceListAsync(id, ct)))
			.Produces<AgreementPriceListDto>()
			.RequireAuthorization(manage);

		lists.MapPut("/{id:guid}", async (Guid id, SaveAgreementPriceListRequest request, AgreementsService service, CancellationToken ct)
			=> Results.Ok(await service.UpdatePriceListAsync(id, request, ct)))
			.Produces<AgreementPriceListDto>()
			.RequireAuthorization(manage);

		lists.MapDelete("/{id:guid}", async (Guid id, AgreementsService service, CancellationToken ct) =>
		{
			await service.DeletePriceListAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(manage);

		lists.MapPost("/bulk-delete", async (BulkIdsRequest request, AgreementsService service, CancellationToken ct) => Results.Ok(await service.DeletePriceListsAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(manage);

		lists.MapPost("/{id:guid}/duplicate", async (Guid id, AgreementsService service, CancellationToken ct) =>
		{
			var copy = await service.DuplicatePriceListAsync(id, ct);
			return Results.Created($"/api/catalog/agreement-price-lists/{copy.Id}", copy);
		})
			.Produces<AgreementPriceListDto>(StatusCodes.Status201Created)
			.RequireAuthorization(manage);

		lists.MapPost("/bulk-duplicate", async (BulkIdsRequest request, AgreementsService service, CancellationToken ct) => Results.Ok(await service.DuplicatePriceListsAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(manage);

		var reports = api.MapGroup("/agreement-reports").WithTags("Catalog - Convenzioni");

		reports.MapGet("/", async ([AsParameters] AgreementReportQuery query, AgreementsService service, CancellationToken ct) => Results.Ok(await service.ListReportsAsync(query, ct)))
			.Produces<PagedResult<AgreementReportDto>>()
			.RequireAuthorization(manage);

		reports.MapGet("/{id:guid}", async (Guid id, AgreementsService service, CancellationToken ct) => Results.Ok(await service.GetReportAsync(id, ct)))
			.Produces<AgreementReportDto>()
			.RequireAuthorization(manage);

		reports.MapPost("/{id:guid}/cancel", async (Guid id, AgreementsService service, CancellationToken ct) => Results.Ok(await service.CancelReportAsync(id, ct)))
			.Produces<AgreementReportDto>()
			.RequireAuthorization(manage);
	}
}
