using BuildingBlocks.Infrastructure.Export;
using Module.Catalog.Application.Abstractions;
using Shared.Dtos;

namespace Module.Catalog.Infrastructure.Infras;

public sealed class TabularFiles : ITabularFiles
{
	public TabularFile Write(string fileBaseName, string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows, TabularFormat format)
		=> TabularExportWriter.Write(fileBaseName, sheetName, headers, rows, format);

	public IReadOnlyList<IReadOnlyList<string>> Read(Stream content, TabularFormat format) => TabularImportReader.Read(content, format);
}
