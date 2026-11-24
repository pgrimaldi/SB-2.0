using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Module.Booking.Application.Services;
using Shared.Security;

namespace Module.Booking.Endpoints;

/// <summary>
/// Comandi rigiocati dal dispositivo dopo l'offline: con l'header <c>Idempotency-Key</c> la prima esecuzione riuscita si
/// ricorda (per stabilimento) e le successive con la stessa chiave ricevono la stessa risposta senza rifare l'operazione.
/// </summary>
public sealed class IdempotencyFilter : IEndpointFilter
{
	public const string Header = "Idempotency-Key";

	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var http = context.HttpContext;
		var key = http.Request.Headers[Header].ToString().Trim();
		var user = http.RequestServices.GetRequiredService<ICurrentUser>();
		if (key.Length == 0 || user.PropertyId is not { } propertyId)
			return await next(context);
		if (key.Length > IdempotencyService.KeyMaxLength)
			return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "booking.idempotency_key_invalid");

		var commands = http.RequestServices.GetRequiredService<IdempotencyService>();
		if (await commands.FindAsync(propertyId, key, http.RequestAborted) is { } done)
		{
			if (!string.Equals(done.Method, http.Request.Method, StringComparison.OrdinalIgnoreCase) || done.Path != http.Request.Path.Value)
				return Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "booking.idempotency_key_reused");
			http.Response.Headers["Idempotent-Replayed"] = "true";
			return done.Body is null
				? Results.StatusCode(done.StatusCode)
				: Results.Content(done.Body, "application/json", statusCode: done.StatusCode);
		}

		var result = await next(context);
		if (result is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 } status)
		{
			var options = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
			var body = result is IValueHttpResult { Value: { } value } ? JsonSerializer.Serialize(value, value.GetType(), options) : null;
			await commands.RememberAsync(propertyId, key, http.Request.Method, http.Request.Path.Value ?? "", status.StatusCode ?? 200, body,
				http.RequestAborted);
		}
		return result;
	}
}
