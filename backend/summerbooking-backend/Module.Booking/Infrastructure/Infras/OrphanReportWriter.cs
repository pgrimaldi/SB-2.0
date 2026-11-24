using Module.Booking.Application.Dtos;
using System.Globalization;
using System.Resources;
using BuildingBlocks.Infrastructure.Export;
using Module.Booking.Application.Abstractions;
using Shared.Dtos;
using Shared.Localization;
using Shared.Utils;

namespace Module.Booking.Infrastructure.Infras;

/// <summary>
/// PDF delle prenotazioni da sistemare dopo «Reimposta mappa», con i testi di Resources/OrphanReport.*.resx nella lingua
/// dell'operatore; in arabo l'impaginazione va da destra a sinistra.
/// </summary>
public sealed class OrphanReportWriter : IOrphanReportWriter
{
	private static readonly ResourceManager Texts = new("Module.Booking.Resources.OrphanReport", typeof(OrphanReportWriter).Assembly);

	public TabularFile Write(IReadOnlyList<OrphanLineDto> rows, string propertyName, AppLanguage language, DateTime nowUtc)
	{
		var culture = SupportedLanguages.Culture(language);
		string T(string key) => Texts.GetString(key, culture) ?? key;
		string Day(DateOnly d) => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

		var report = new PdfTableReport(
			T("Title"),
			[string.Format(culture, T("Property"), propertyName), string.Format(culture, T("PrintedAt"), ItalianTime.FromUtc(nowUtc).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture))],
			[T("Column_BookingNumber"), T("Column_Customer"), T("Column_Spot"), T("Column_SpotType"), T("Column_Position"), T("Column_From"), T("Column_To")],
			rows.Select(r => (IReadOnlyList<string?>)
			[
				r.BookingNumber, r.CustomerName, r.SpotLabel, r.SpotType,
				r.Row is { } row && r.Column is { } column ? $"R{row}C{column}" : null, Day(r.From), Day(r.To),
			]).ToList(),
			[1.2, 2.6, 1.2, 1.4, 1.0, 1.0, 1.0],
			EmptyText: T("Empty"),
			PageLabel: T("Page"),
			RightToLeft: SupportedLanguages.IsRightToLeft(language));
		return PdfReportWriter.Write(T("FileName"), report);
	}
}
