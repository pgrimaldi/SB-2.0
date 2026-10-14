using BuildingBlocks.Infrastructure.Storage;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Domain.Enums;
using Shared.Utils;

namespace Module.Identity.Infrastructure.Infras;

/// <summary>PDF contrattuali sull'archivio di file comune; qui solo la conversione dell'origine nell'enum del modulo.</summary>
public sealed class ContractDocumentStorage : IDocumentStorage
{
	public const string Section = "Identity:Documents";

	private readonly IObjectStorage _storage;

	public ContractDocumentStorage(IObjectStorage storage) => _storage = storage;

	public async Task<StoredDocument> SaveAsync(string fileName, Stream content, CancellationToken ct = default)
	{
		if (!Codes.TryParse<StorageOrigin>(_storage.Origin, out var origin))
			throw new InvalidOperationException($"{Section}:Origin '{_storage.Origin}' non è un valore di identity.storage_origin.");

		var stored = await _storage.SaveAsync(fileName, content, "application/pdf", ct);
		return new StoredDocument(origin, stored.Path, stored.Sha256);
	}

	public string GetDownloadUrl(StorageOrigin origin, string path)
		=> origin == StorageOrigin.External ? path : _storage.GetDownloadUrl(path);
}

public sealed class FileUrlResolver : IFileUrlResolver
{
	private readonly PublicFileUrlResolver _resolver;

	public FileUrlResolver(PublicFileUrlResolver resolver) => _resolver = resolver;

	public string? Resolve(StorageOrigin? origin, string? path)
		=> origin is null ? null : _resolver.Resolve(Codes.Of(origin.Value), path);
}
