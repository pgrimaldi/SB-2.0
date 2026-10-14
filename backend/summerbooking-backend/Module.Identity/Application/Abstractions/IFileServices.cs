using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Dtos;
using Shared.Localization;

namespace Module.Identity.Application.Abstractions;

public sealed record StoredDocument(StorageOrigin Origin, string Path, string Sha256);

/// <summary>Archivio dei PDF contrattuali.</summary>
public interface IDocumentStorage
{
	Task<StoredDocument> SaveAsync(string fileName, Stream content, CancellationToken ct = default);
	string GetDownloadUrl(StorageOrigin origin, string path);
}

/// <summary>URL pubblico di un file salvato come origine + percorso.</summary>
public interface IFileUrlResolver
{
	string? Resolve(StorageOrigin? origin, string? path);
}

public interface ICustomerExportWriter
{
	/// <param name="language">Lingua di intestazioni e valori.</param>
	TabularFile Write(IReadOnlyList<Customer> customers, TabularFormat format, AppLanguage language);
}
