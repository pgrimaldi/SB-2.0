using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.ZTest.Application.Dtos;
using Module.ZTest.Application.Services;
using Module.ZTest.Domain.Exceptions;

namespace Module.ZTest.Endpoints;

/// <summary>
/// Definizione degli endpoint HTTP (Minimal API) del modulo ZTest.
/// Vengono registrati dal progetto Boot: app.MapZTestEndpoints();
/// </summary>
public static class Endpoints
{
	public static IEndpointRouteBuilder MapZTestEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/ztest/reservations")
			.WithTags("ZTest - Reservations");

		group.MapPost("/", async (CreateReservationRequest request, ReservationService service, CancellationToken ct) =>
		{
			var dto = await service.CreateAsync(request, ct);
			return Results.Created($"/api/ztest/reservations/{dto.Id}", dto);
		});

		group.MapGet("/{id:guid}", async (Guid id, ReservationService service, CancellationToken ct) =>
		{
			try
			{
				var dto = await service.GetAsync(id, ct);
				return Results.Ok(dto);
			}
			catch (ReservationNotFoundException)
			{
				return Results.NotFound();
			}
		});

		return app;
	}
}
