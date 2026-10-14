using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Services;
using Shared.Dtos;
using Shared.Security;
using Shared.Utils;

namespace Module.Identity.Endpoints;

/// <summary>Funzioni dello stabilimento; lo stabilimento è sempre quello del token.</summary>
internal static class PropertyEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		MapStaff(api);
		MapCustomers(api);
		MapContracts(api);
	}

	private static void MapStaff(RouteGroupBuilder api)
	{
		var users = api.MapGroup("/property-users")
			.WithTags("Identity - Utenti stabilimento")
			.RequireAuthorization(Permissions.PolicyName(Permissions.StaffManage));

		users.MapGet("/", async (StaffService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)));

		users.MapPost("/", async (CreateStaffUserRequest request, StaffService service, CancellationToken ct) =>
		{
			var user = await service.CreateAsync(request, ct);
			return Results.Created($"/api/identity/property-users/{user.Id}", user);
		});

		users.MapPatch("/{userId:guid}", async (Guid userId, UpdateStaffUserRequest request, StaffService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(userId, request, ct)));

		users.MapDelete("/{userId:guid}", async (Guid userId, StaffService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(userId, ct);
			return Results.NoContent();
		});

		api.MapGet("/roles/assignable", async (StaffService service, CancellationToken ct) => Results.Ok(await service.ListAssignableRolesAsync(ct)))
			.WithTags("Identity - Utenti stabilimento")
			.RequireAuthorization(Permissions.PolicyName(Permissions.StaffManage));

		var security = api.MapGroup("/property-security-settings")
			.WithTags("Identity - Utenti stabilimento")
			.RequireAuthorization(Permissions.PolicyName(Permissions.SecurityManage));

		security.MapGet("/", async (SecuritySettingsService service, CancellationToken ct) => Results.Ok(await service.GetAsync(ct)));

		security.MapPut("/", async (UpdateSecuritySettingsRequest request, SecuritySettingsService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(request, ct)));
	}

	private static void MapCustomers(RouteGroupBuilder api)
	{
		var customers = api.MapGroup("/customers").WithTags("Identity - Contatti");

		// Stesso endpoint, due forme: contatti in chiaro solo con il permesso customers.contacts.
		customers.MapGet("/", async ([AsParameters] CustomerListQuery query, CustomerService service, ICurrentUser user, CancellationToken ct)
			=> user.HasPermission(Permissions.CustomersContacts)
				? Results.Ok(await service.ListAsync(query, ct))
				: Results.Ok(await service.ListMaskedAsync(query, ct)))
			.Produces<PagedResult<CustomerListItemDto>>()
			.Produces<PagedResult<CustomerMaskedListItemDto>>()
			.RequireAuthorization(Permissions.PolicyName(Permissions.CustomersRead));

		customers.MapGet("/export", async ([AsParameters] CustomerListQuery query, string? format, CustomerService service, CancellationToken ct) =>
		{
			var file = await service.ExportAsync(query, Codes.TryParse<TabularFormat>(format, out var f) ? f : TabularFormat.Csv, ct);
			return Results.File(file.Content, file.ContentType, file.FileName);
		})
			.RequireAuthorization(Permissions.PolicyName(Permissions.CustomersWrite));

		customers.MapGet("/{customerId:guid}", async (Guid customerId, CustomerService service, CancellationToken ct)
			=> Results.Ok(await service.GetAsync(customerId, ct)))
			.RequireAuthorization(Permissions.PolicyName(Permissions.CustomersContacts));

		customers.MapPost("/", async (SaveCustomerRequest request, CustomerService service, CancellationToken ct) =>
		{
			var customer = await service.CreateAsync(request, ct);
			return Results.Created($"/api/identity/customers/{customer.Id}", customer);
		})
			.RequireAuthorization(Permissions.PolicyName(Permissions.CustomersWrite));

		customers.MapPut("/{customerId:guid}", async (Guid customerId, SaveCustomerRequest request, CustomerService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(customerId, request, ct)))
			.RequireAuthorization(Permissions.PolicyName(Permissions.CustomersWrite));

		customers.MapDelete("/{customerId:guid}", async (Guid customerId, CustomerService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(customerId, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(Permissions.PolicyName(Permissions.CustomersWrite));
	}

	private static void MapContracts(RouteGroupBuilder api)
	{
		var contracts = api.MapGroup("/contract-documents")
			.WithTags("Identity - Contratti")
			.RequireAuthorization(Permissions.PolicyName(Permissions.ContractsAccept));

		contracts.MapGet("/current", async (ContractService service, CancellationToken ct)
			=> await service.GetCurrentAsync(ct) is { } current ? Results.Ok(current) : Results.NoContent())
			.Produces<CurrentContractDto>();

		contracts.MapPut("/{documentId:guid}/acceptance", async (Guid documentId, SetContractAcceptanceRequest request, ContractService service, CancellationToken ct)
			=> Results.Ok(await service.SetAcceptanceAsync(documentId, request, ct)));
	}
}
