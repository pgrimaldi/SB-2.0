using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Shared.Dtos;

namespace BuildingBlocks.Infrastructure.Export;

/// <summary>
/// Esportazione di una tabella in CSV o XLSX. Il modulo decide colonne e valori, qui solo il formato:
/// CSV con separatore ";" e BOM (Excel in italiano lo apre senza importazione guidata), XLSX con
/// intestazione in grassetto. I valori sono già stringhe formattate dal chiamante.
/// </summary>
public static class TabularExportWriter
{
	public static TabularFile Write(
		string fileBaseName, string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows, TabularFormat format)
	{
		var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
		return format == TabularFormat.Xlsx
			? new TabularFile(Xlsx(sheetName, headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileBaseName}-{stamp}.xlsx")
			: new TabularFile(Csv(headers, rows), "text/csv; charset=utf-8", $"{fileBaseName}-{stamp}.csv");
	}

	private static byte[] Csv(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
	{
		var sb = new StringBuilder();
		sb.AppendLine(string.Join(';', headers.Select(Escape)));
		foreach (var row in rows)
			sb.AppendLine(string.Join(';', row.Select(Escape)));
		return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(sb.ToString())];
	}

	private static string Escape(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;
		// Una cella che inizia con = + - @ verrebbe eseguita da Excel come formula (CSV injection).
		if (value[0] is '=' or '+' or '-' or '@')
			value = "'" + value;
		return value.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
	}

	private static byte[] Xlsx(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
	{
		using var workbook = new XLWorkbook();
		var sheet = workbook.Worksheets.Add(sheetName);
		for (var i = 0; i < headers.Count; i++)
			sheet.Cell(1, i + 1).Value = headers[i];

		var r = 2;
		foreach (var row in rows)
		{
			for (var i = 0; i < row.Count; i++)
				sheet.Cell(r, i + 1).Value = row[i];
			r++;
		}

		sheet.Row(1).Style.Font.Bold = true;
		using var stream = new MemoryStream();
		workbook.SaveAs(stream);
		return stream.ToArray();
	}
}
