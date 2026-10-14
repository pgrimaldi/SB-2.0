namespace Shared.Infrastructure;

/// <summary>
/// Implementazione reale di IDateTimeProvider, l'unico punto della solution che puo
/// leggere DateTime.UtcNow direttamente.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
	public DateTime UtcNow => DateTime.UtcNow;
}
