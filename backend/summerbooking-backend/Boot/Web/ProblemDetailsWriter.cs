using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Errors;

namespace Boot.Web;

/// <summary>
/// Unico punto che produce le risposte d'errore: Problem Details (RFC 9457) esteso con <c>code</c>,
/// <c>args</c>, <c>errors[]</c> e <c>traceId</c>.
/// </summary>
public static class ProblemDetailsWriter
{
	public static Task WriteAsync(HttpContext context, int status, string code, IReadOnlyDictionary<string, object?>? args = null, IReadOnlyList<FieldError>? errors = null)
	{
		var problem = new ProblemDetails
		{
			Status = status,
			Title = code,
			Type = $"https://errors.summerbooking/{code.Replace('.', '/')}",
		};
		problem.Extensions["code"] = code;
		problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
		if (args is { Count: > 0 })
			problem.Extensions["args"] = args;
		if (errors is { Count: > 0 })
			problem.Extensions["errors"] = errors.Select(e => new { field = e.Field, code = e.Code, args = e.Args ?? new Dictionary<string, object?>() });

		context.Response.StatusCode = status;
		return context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
	}
}

/// <summary>Eccezioni tipizzate dei moduli → Problem Details; tutto il resto → 500 senza dettagli interni.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
	private readonly ILogger<GlobalExceptionHandler> _logger;

	public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

	public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
	{
		switch (exception)
		{
			case DomainException domain:
				await ProblemDetailsWriter.WriteAsync(context, domain.HttpStatus, domain.Code, domain.Args);
				break;
			case ValidationFailedException validation:
				await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status400BadRequest, CommonErrorCodes.ValidationFailed, errors: validation.Errors);
				break;
			case BadHttpRequestException badRequest:
				await ProblemDetailsWriter.WriteAsync(context, badRequest.StatusCode, CommonErrorCodes.ValidationFailed);
				break;
			default:
				_logger.LogError(exception, "Errore non gestito su {Method} {Path}", context.Request.Method, context.Request.Path);
				await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status500InternalServerError, CommonErrorCodes.Unexpected);
				break;
		}
		return true;
	}
}
