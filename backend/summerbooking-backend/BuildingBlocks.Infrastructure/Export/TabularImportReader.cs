using System.Text;
using ClosedXML.Excel;
using Shared.Dtos;

namespace BuildingBlocks.Infrastructure.Export;

/// <summary>
/// Lettura di una tabella da CSV (separatore ";" o ",", virgolette doppie, BOM facoltativo) o XLSX (primo foglio).
/// Restituisce le righe come stringhe, intestazione compresa; le righe vuote si saltano.
/// </summary>
public static class TabularImportReader
{
	public const int MaxRows = 20_000;

	public static IReadOnlyList<IReadOnlyList<string>> Read(Stream content, TabularFormat format)
		=> format == TabularFormat.Xlsx ? Xlsx(content) : Csv(content);

	private static List<IReadOnlyList<string>> Csv(Stream content)
	{
		using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		var text = reader.ReadToEnd();
		var firstLine = text.Split('\n', 2)[0];
		var separator = firstLine.Count(c => c == ';') >= firstLine.Count(c => c == ',') ? ';' : ',';

		var rows = new List<IReadOnlyList<string>>();
		var row = new List<string>();
		var cell = new StringBuilder();
		var quoted = false;
		for (var i = 0; i < text.Length; i++)
		{
			var c = text[i];
			if (quoted)
			{
				if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
				{
					cell.Append('"');
					i++;
				}
				else if (c == '"')
					quoted = false;
				else
					cell.Append(c);
			}
			else if (c == '"')
				quoted = true;
			else if (c == separator)
			{
				row.Add(cell.ToString());
				cell.Clear();
			}
			else if (c == '\n')
			{
				row.Add(cell.ToString().TrimEnd('\r'));
				cell.Clear();
				AddRow(rows, row);
				row = [];
			}
			else
				cell.Append(c);
		}
		if (cell.Length > 0 || row.Count > 0)
		{
			row.Add(cell.ToString().TrimEnd('\r'));
			AddRow(rows, row);
		}
		return rows;
	}

	private static List<IReadOnlyList<string>> Xlsx(Stream content)
	{
		using var workbook = new XLWorkbook(content);
		var sheet = workbook.Worksheets.First();
		var rows = new List<IReadOnlyList<string>>();
		var last = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
		foreach (var r in sheet.RowsUsed())
			AddRow(rows, Enumerable.Range(1, last).Select(i => r.Cell(i).GetFormattedString()).ToList());
		return rows;
	}

	private static void AddRow(List<IReadOnlyList<string>> rows, List<string> row)
	{
		if (row.All(string.IsNullOrWhiteSpace))
			return;
		if (rows.Count >= MaxRows)
			throw new InvalidDataException($"Più di {MaxRows} righe.");
		rows.Add(row.Select(v => v.Trim()).ToList());
	}
}
