using System.Globalization;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Shared.Dtos;

namespace BuildingBlocks.Infrastructure.Export;

/// <summary>Una tabella da stampare in PDF: titolo, righe di intestazione, colonne con il loro peso nella larghezza.</summary>
/// <param name="Subtitle">Righe sotto il titolo (stabilimento, data di stampa…).</param>
/// <param name="ColumnWeights">Peso di ogni colonna nella larghezza della pagina; null = colonne uguali.</param>
/// <param name="EmptyText">Testo al posto della tabella quando non ci sono righe.</param>
/// <param name="RightToLeft">Impaginazione da destra a sinistra (arabo): testo allineato a destra, colonne in ordine inverso.</param>
public sealed record PdfTableReport(
	string Title, IReadOnlyList<string> Subtitle, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string?>> Rows,
	IReadOnlyList<double>? ColumnWeights = null, bool Landscape = true, string? EmptyText = null, string? PageLabel = null,
	bool RightToLeft = false);

/// <summary>
/// Rapporti PDF a tabella (MigraDoc): A4, intestazione ripetuta a ogni pagina, numero di pagina in fondo. Il modulo decide
/// testi e valori (già formattati), qui solo l'impaginazione. Font incorporati, uguali su Windows e Linux: DejaVu Sans per
/// latino, greco, cirillico e arabo, Droid Sans Fallback per gli ideogrammi (scelto carattere per carattere).
/// </summary>
public static class PdfReportWriter
{
	private const string Font = EmbeddedFontResolver.Family;
	private static readonly object FontLock = new();
	private static bool _fontsReady;

	public static TabularFile Write(string fileBaseName, PdfTableReport report)
	{
		EnsureFonts();
		var document = new Document();
		document.Info.Title = report.Title;
		var normal = document.Styles[StyleNames.Normal]!;
		normal.Font.Name = Font;
		normal.Font.Size = 9;

		var section = document.AddSection();
		var setup = section.PageSetup;
		setup.PageFormat = PageFormat.A4;
		setup.Orientation = report.Landscape ? Orientation.Landscape : Orientation.Portrait;
		setup.LeftMargin = setup.RightMargin = Unit.FromCentimeter(1.5);
		setup.TopMargin = setup.BottomMargin = Unit.FromCentimeter(1.5);
		var usable = (report.Landscape ? 29.7 : 21.0) - 3.0;

		var rtl = report.RightToLeft;
		var align = rtl ? ParagraphAlignment.Right : ParagraphAlignment.Left;
		var title = section.AddParagraph();
		AddText(title, report.Title, rtl);
		title.Format.Alignment = align;
		title.Format.Font.Size = 14;
		title.Format.Font.Bold = true;
		title.Format.SpaceAfter = Unit.FromPoint(4);
		foreach (var line in report.Subtitle)
		{
			var paragraph = section.AddParagraph();
			AddText(paragraph, line, rtl);
			paragraph.Format.Alignment = align;
			paragraph.Format.Font.Color = Colors.DimGray;
		}
		section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(6);

		if (report.Rows.Count == 0)
		{
			var empty = section.AddParagraph();
			AddText(empty, report.EmptyText ?? string.Empty, rtl);
			empty.Format.Alignment = align;
		}
		else
			AddTable(section, report, usable);

		// numero di pagina nell'angolo opposto all'inizio della riga
		var footer = section.Footers.Primary.AddParagraph();
		footer.Format.Alignment = rtl ? ParagraphAlignment.Left : ParagraphAlignment.Right;
		footer.Format.Font.Size = 8;
		if (rtl)
		{
			footer.AddNumPagesField();
			footer.AddText(" / ");
			footer.AddPageField();
			if (report.PageLabel is { Length: > 0 } rtlLabel)
			{
				footer.AddText(" ");
				AddText(footer, rtlLabel, true);
			}
		}
		else
		{
			if (report.PageLabel is { Length: > 0 } label)
				AddText(footer, label + " ", false);
			footer.AddPageField();
			footer.AddText(" / ");
			footer.AddNumPagesField();
		}

		var renderer = new PdfDocumentRenderer { Document = document };
		renderer.RenderDocument();
		using var stream = new MemoryStream();
		renderer.PdfDocument.Save(stream, false);
		var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
		return new TabularFile(stream.ToArray(), "application/pdf", $"{fileBaseName}-{stamp}.pdf");
	}

	private static void AddTable(Section section, PdfTableReport report, double usable)
	{
		var rtl = report.RightToLeft;
		IReadOnlyList<T> Order<T>(IReadOnlyList<T> items) => rtl ? items.Reverse().ToList() : items;
		var weights = Order(report.ColumnWeights is { } w && w.Count == report.Headers.Count ? w : report.Headers.Select(_ => 1.0).ToList());
		var headers = Order(report.Headers);
		var align = rtl ? ParagraphAlignment.Right : ParagraphAlignment.Left;
		var sum = weights.Sum();
		var table = section.AddTable();
		table.Borders.Width = 0.25;
		table.Borders.Color = Colors.Gray;
		table.LeftPadding = table.RightPadding = Unit.FromPoint(3);
		table.TopPadding = table.BottomPadding = Unit.FromPoint(2);
		foreach (var weight in weights)
			table.AddColumn(Unit.FromCentimeter(usable * weight / sum));

		var header = table.AddRow();
		header.HeadingFormat = true;
		header.Format.Font.Bold = true;
		header.Shading.Color = Colors.LightGray;
		for (var i = 0; i < headers.Count; i++)
			Cell(header.Cells[i], headers[i], rtl, align);

		foreach (var values in report.Rows)
		{
			var row = table.AddRow();
			var cells = Order(values.Concat(Enumerable.Repeat<string?>(null, Math.Max(0, report.Headers.Count - values.Count))).Take(report.Headers.Count).ToList());
			for (var i = 0; i < cells.Count; i++)
				Cell(row.Cells[i], cells[i] ?? string.Empty, rtl, align);
		}
	}

	private static void Cell(Cell cell, string text, bool rtl, ParagraphAlignment align)
	{
		var paragraph = cell.AddParagraph();
		AddText(paragraph, text, rtl);
		paragraph.Format.Alignment = align;
	}

	/// <summary>Testo nel paragrafo: l'arabo in ordine visivo, gli ideogrammi col loro font, il resto col font del documento.</summary>
	private static void AddText(Paragraph paragraph, string text, bool rtl)
	{
		var visual = RightToLeftText.Visual(text, rtl);
		var start = 0;
		while (start < visual.Length)
		{
			var cjk = IsCjk(visual[start]);
			var end = start;
			while (end < visual.Length && (IsCjk(visual[end]) == cjk || char.IsWhiteSpace(visual[end])))
				end++;
			var run = paragraph.AddFormattedText(visual[start..end]);
			if (cjk)
				run.Font.Name = EmbeddedFontResolver.CjkFamily;
			start = end;
		}
	}

	private static bool IsCjk(char c)
		=> c is >= '\u2E80' and <= '\u9FFF' or >= '\uAC00' and <= '\uD7AF' or >= '\uF900' and <= '\uFAFF' or >= '\uFF00' and <= '\uFFEF';

	private static void EnsureFonts()
	{
		if (_fontsReady)
			return;
		lock (FontLock)
		{
			if (_fontsReady)
				return;
			// si imposta una volta sola per processo; se qualcun altro l'ha già impostato si usa il suo
			if (GlobalFontSettings.FontResolver is null)
				GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
			_fontsReady = true;
		}
	}
}

/// <summary>Droid Sans Fallback per gli ideogrammi, DejaVu Sans (normale o grassetto) per tutto il resto; letti dalle risorse dell'assembly.</summary>
internal sealed class EmbeddedFontResolver : IFontResolver
{
	public const string Family = "DejaVu Sans";
	public const string CjkFamily = "Droid Sans Fallback";
	private const string Regular = "BuildingBlocks.Fonts.DejaVuSansPdf.ttf";
	private const string Bold = "BuildingBlocks.Fonts.DejaVuSansPdf-Bold.ttf";
	private const string Cjk = "BuildingBlocks.Fonts.DroidSansFallbackFull.ttf";

	// il font degli ideogrammi non ha il grassetto: si simula
	public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
		=> familyName == CjkFamily ? new(Cjk, bold, italic) : new(bold ? Bold : Regular, false, italic);

	public byte[]? GetFont(string faceName)
	{
		using var stream = typeof(EmbeddedFontResolver).Assembly.GetManifestResourceStream(faceName);
		if (stream is null)
			return null;
		using var copy = new MemoryStream();
		stream.CopyTo(copy);
		return copy.ToArray();
	}
}
