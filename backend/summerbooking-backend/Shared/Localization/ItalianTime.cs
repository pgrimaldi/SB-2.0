namespace Shared.Localization;

/// <summary>
/// Ora civile italiana (<c>Europe/Rome</c>) per i testi prodotti dal backend (export, email, documenti):
/// nel database e nelle API le date restano UTC.
/// </summary>
public static class ItalianTime
{
	public const string TimeZoneId = "Europe/Rome";

	private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

	public static DateTime FromUtc(DateTime utc)
		=> TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);
}
