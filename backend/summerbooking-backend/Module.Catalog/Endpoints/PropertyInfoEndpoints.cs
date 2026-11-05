using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Services;
using Shared.Security;

namespace Module.Catalog.Endpoints;

/// <summary>Informazioni stabilimento (scheda pubblica) e registrazione pubblica di una struttura.</summary>
internal static class PropertyInfoEndpoints
{
	public static void Map(RouteGroupBuilder api)
	{
		var read = Permissions.PolicyName(Permissions.CatalogRead);
		var write = Permissions.PolicyName(Permissions.CatalogSettings);
		var info = api.MapGroup("/property-info").WithTags("Catalog - Informazioni stabilimento");

		info.MapGet("/", async (PropertyInfoService service, CancellationToken ct) => Results.Ok(await service.GetAsync(ct)))
			.Produces<PropertyInfoDto>()
			.RequireAuthorization(read);

		info.MapPut("/", async (SavePropertyInfoRequest request, PropertyInfoService service, CancellationToken ct)
			=> Results.Ok(await service.SaveAsync(request, ct)))
			.Produces<PropertyInfoDto>()
			.RequireAuthorization(write);

		info.MapPut("/amenities", async (SetAmenitiesRequest request, PropertyInfoService service, CancellationToken ct)
			=> Results.Ok(await service.SetAmenitiesAsync(request, ct)))
			.Produces<PropertyInfoDto>()
			.RequireAuthorization(write);

		info.MapPost("/main-photo", async (IFormFile file, PropertyInfoService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.UploadMainPhotoAsync(Upload(file, content), ct));
		})
			.Produces<PropertyInfoDto>()
			.DisableAntiforgery()
			.RequireAuthorization(write);

		info.MapDelete("/main-photo", async (PropertyInfoService service, CancellationToken ct) => Results.Ok(await service.RemoveMainPhotoAsync(ct)))
			.Produces<PropertyInfoDto>()
			.RequireAuthorization(write);

		info.MapPost("/photos", async (IFormFile file, PropertyInfoService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.AddPhotoAsync(Upload(file, content), ct));
		})
			.Produces<PropertyInfoDto>()
			.DisableAntiforgery()
			.RequireAuthorization(write);

		info.MapPut("/photos/order", async (ReorderPhotosRequest request, PropertyInfoService service, CancellationToken ct)
			=> Results.Ok(await service.ReorderPhotosAsync(request, ct)))
			.Produces<PropertyInfoDto>()
			.RequireAuthorization(write);

		info.MapDelete("/photos/{id:guid}", async (Guid id, PropertyInfoService service, CancellationToken ct) => Results.Ok(await service.DeletePhotoAsync(id, ct)))
			.Produces<PropertyInfoDto>()
			.RequireAuthorization(write);

		info.MapPost("/documents/{document}", async (string document, IFormFile file, PropertyInfoService service, CancellationToken ct) =>
		{
			if (Document(document) is not { } kind)
				return Results.NotFound();
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.UploadDocumentAsync(kind, Upload(file, content), ct));
		})
			.Produces<PropertyInfoDto>()
			.DisableAntiforgery()
			.RequireAuthorization(write);

		info.MapDelete("/documents/{document}", async (string document, PropertyInfoService service, CancellationToken ct)
			=> Document(document) is { } kind ? Results.Ok(await service.RemoveDocumentAsync(kind, ct)) : Results.NotFound())
			.Produces<PropertyInfoDto>()
			.RequireAuthorization(write);

		api.MapPost("/property-registrations", async (HttpRequest http, PropertyRegistrationService service, CancellationToken ct) =>
		{
			var form = await http.ReadFormAsync(ct);
			var request = new RegisterPropertyRequest(
				form["name"].ToString(), form["city"].ToString(), form["contactFullName"].ToString(), form["email"].ToString(),
				form["phoneCountryCode"].ToString(), form["phone"].ToString(), form["verificationToken"].ToString(), form["website"].ToString());
			var streams = form.Files.GetFiles("photos").Select(f => (File: f, Stream: f.OpenReadStream())).ToList();
			try
			{
				var result = await service.RegisterAsync(request, streams.Select(s => Upload(s.File, s.Stream)).ToList(), ct);
				return Results.Created($"/api/catalog/property-registrations/{result.Id}", result);
			}
			finally
			{
				foreach (var (_, stream) in streams)
					await stream.DisposeAsync();
			}
		})
			.WithTags("Catalog - Informazioni stabilimento")
			.Accepts<RegisterPropertyRequest>("multipart/form-data")
			.Produces<PropertyRegisteredDto>(StatusCodes.Status201Created)
			.DisableAntiforgery()
			.AllowAnonymous()
			.RequireRateLimiting(RateLimitPolicies.PublicForms);
	}

	private static UploadedFile Upload(IFormFile file, Stream content) => new(file.FileName, file.ContentType, file.Length, content);

	private static PropertyDocument? Document(string code) => code switch
	{
		"menu" => PropertyDocument.Menu,
		"price-list" => PropertyDocument.PriceList,
		_ => null,
	};
}
