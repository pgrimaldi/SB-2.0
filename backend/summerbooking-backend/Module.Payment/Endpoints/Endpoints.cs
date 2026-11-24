using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Payment.Application.Dtos;
using Module.Payment.Application.Services;
using Module.Payment.Domain;
using Shared.Contracts.Errors;
using Shared.Security;

namespace Module.Payment.Endpoints;

/// <summary>
/// Endpoint HTTP (Minimal API) del modulo Payment, sotto <c>/api/payment</c>. Registrati dal progetto Boot:
/// app.MapPaymentEndpoints(); Gli incassi si registrano da Booking (<c>POST /api/booking/reservations/{id}/payments</c>),
/// che verifica la prenotazione; qui si leggono e si annullano.
/// </summary>
public static class Endpoints
{
	public const string GroupName = "payment";

	public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
	{
		var api = app.MapGroup("/api/payment").WithGroupName(GroupName).WithTags("Payment - Movimenti");

		api.MapGet("/reservations/{reservationId:guid}/entries", async (Guid reservationId, ICurrentUser user, ReservationPayments payments, CancellationToken ct)
			=> Results.Ok(await payments.ListAsync(PropertyOf(user), reservationId, ct)))
			.Produces<IReadOnlyList<LedgerEntryDto>>()
			.RequireAuthorization(Permissions.PolicyName(Permissions.BookingsAmounts));

		api.MapDelete("/entries/{id:guid}", async (Guid id, ICurrentUser user, ReservationPayments payments, CancellationToken ct) =>
		{
			await payments.VoidAsync(PropertyOf(user), id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(Permissions.PolicyName(Permissions.PaymentsRecord));

		return app;
	}

	private static Guid PropertyOf(ICurrentUser user) => user.PropertyId ?? throw DomainException.Forbidden(PaymentErrors.PropertyRequired);
}
