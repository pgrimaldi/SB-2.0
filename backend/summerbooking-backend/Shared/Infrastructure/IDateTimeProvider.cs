namespace Shared.Infrastructure;

/// <summary>
/// Astrazione dell'orologio di sistema: Infrastructure non usa mai DateTime.UtcNow diretto,
/// cosi i test possono controllare il tempo.
/// </summary>
public interface IDateTimeProvider
{
	DateTime UtcNow { get; }
}
