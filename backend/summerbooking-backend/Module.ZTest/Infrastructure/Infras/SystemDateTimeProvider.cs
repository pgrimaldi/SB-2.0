using Module.ZTest.Application.Abstractions;

namespace Module.ZTest.Infrastructure.Infras;

/// <summary>
/// Implementazione concreta dell'astrazione sul tempo, basata sull'orologio di sistema.
/// (La cartella "Infras" raccoglie i servizi infrastrutturali trasversali: clock, email, storage, ecc.)
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
	public DateTime UtcNow => DateTime.UtcNow;

	public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
