namespace Module.ZTest.Application.Abstractions;

/// <summary>
/// Astrazione sull'orologio di sistema: rende testabile la logica applicativa
/// che dipende dal tempo (scadenze, date correnti, ecc.).
/// </summary>
public interface IDateTimeProvider
{
	DateTime UtcNow { get; }

	DateOnly Today { get; }
}
