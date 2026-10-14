using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Services;
using Shared.Security;

namespace Module.Identity.Endpoints;

/// <summary>Back-office del systemadmin: titolari, abbonamenti, piani di licenza, documenti contrattuali.</summary>
internal static class PlatformEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		api.MapGet("/license-plans", async (LicensePlanService service, CancellationToken ct) => Results.Ok(await service.ListAsync(onlyActive: true, ct)))
			.WithTags("Identity - Piani di licenza")
			.AllowAnonymous();

		var admin = api.MapGroup("/admin").RequireAuthorization(Permissions.PolicyName(Permissions.PlatformManage));
		MapProperties(admin);
		MapLicensePlans(admin);
		MapContracts(admin);
	}

	private static void MapProperties(RouteGroupBuilder admin)
	{
		var property = admin.MapGroup("/properties/{propertyId:guid}").WithTags("Identity - Admin stabilimenti");

		property.MapGet("/owner", async (Guid propertyId, PlatformAccountService service, CancellationToken ct)
			=> Results.Ok(await service.GetOwnerAsync(propertyId, ct)));

		property.MapPost("/owner", async (Guid propertyId, CreateOwnerRequest request, PlatformAccountService service, CancellationToken ct) =>
		{
			var owner = await service.CreateOwnerAsync(propertyId, request, ct);
			return Results.Created($"/api/identity/admin/properties/{propertyId}/owner", owner);
		});

		property.MapPatch("/owner", async (Guid propertyId, UpdateOwnerRequest request, PlatformAccountService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateOwnerAsync(propertyId, request, ct)));

		property.MapPost("/impersonation", async (Guid propertyId, PlatformAccountService service, CancellationToken ct)
			=> Results.Ok(await service.StartImpersonationAsync(propertyId, ct)));

		property.MapGet("/subscription", async (Guid propertyId, SubscriptionService service, CancellationToken ct)
			=> Results.Ok(await service.GetAsync(propertyId, ct)));

		property.MapGet("/subscription/periods", async (Guid propertyId, SubscriptionService service, CancellationToken ct)
			=> Results.Ok(await service.ListPeriodsAsync(propertyId, ct)));

		// Ogni chiamata apre un nuovo periodo (crea l'abbonamento se manca): POST, non idempotente.
		property.MapPost("/subscription/periods", async (Guid propertyId, SetSubscriptionRequest request, SubscriptionService service, CancellationToken ct)
			=> Results.Created($"/api/identity/admin/properties/{propertyId}/subscription", await service.SetAsync(propertyId, request, ct)));

		property.MapPost("/subscription/suspend", async (Guid propertyId, SubscriptionService service, CancellationToken ct)
			=> Results.Ok(await service.SuspendAsync(propertyId, ct)));

		property.MapPost("/subscription/reactivate", async (Guid propertyId, SubscriptionService service, CancellationToken ct)
			=> Results.Ok(await service.ReactivateAsync(propertyId, ct)));

		property.MapPost("/subscription/cancel", async (Guid propertyId, SubscriptionService service, CancellationToken ct)
			=> Results.Ok(await service.CancelAsync(propertyId, ct)));
	}

	private static void MapLicensePlans(RouteGroupBuilder admin)
	{
		var plans = admin.MapGroup("/license-plans").WithTags("Identity - Piani di licenza");

		plans.MapGet("/", async (LicensePlanService service, CancellationToken ct) => Results.Ok(await service.ListAsync(onlyActive: false, ct)));

		plans.MapPost("/", async (CreateLicensePlanRequest request, LicensePlanService service, CancellationToken ct) =>
		{
			var plan = await service.CreateAsync(request, ct);
			return Results.Created($"/api/identity/admin/license-plans/{plan.Code}", plan);
		});

		plans.MapPut("/{code}", async (string code, UpdateLicensePlanRequest request, LicensePlanService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(code, request, ct)));
	}

	private static void MapContracts(RouteGroupBuilder admin)
	{
		var documents = admin.MapGroup("/contract-documents").WithTags("Identity - Contratti");

		documents.MapGet("/", async (ContractService service, CancellationToken ct) => Results.Ok(await service.ListDocumentsAsync(ct)));

		documents.MapPost("/", async ([FromForm] string versionCode, [FromForm] string? title, [FromForm] string? notes, IFormFile file,
			ContractService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			var document = await service.UploadAsync(new CreateContractDocumentRequest(versionCode, title, notes), file.FileName, content, ct);
			return Results.Created($"/api/identity/admin/contract-documents/{document.Id}", document);
		})
			.DisableAntiforgery();

		documents.MapPost("/{documentId:guid}/publish", async (Guid documentId, ContractService service, CancellationToken ct)
			=> Results.Ok(await service.PublishAsync(documentId, ct)));
	}
}
