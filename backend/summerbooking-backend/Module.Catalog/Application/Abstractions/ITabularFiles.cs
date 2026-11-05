using Shared.Dtos;

namespace Module.Catalog.Application.Abstractions;

/// <summary>File tabellari CSV/XLSX: scrittura e lettura (righe di stringhe, intestazione compresa).</summary>
public interface ITabularFiles
{
	TabularFile Write(string fileBaseName, string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows, TabularFormat format);
	IReadOnlyList<IReadOnlyList<string>> Read(Stream content, TabularFormat format);
}
