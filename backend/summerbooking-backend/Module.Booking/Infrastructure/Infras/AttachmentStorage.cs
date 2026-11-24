using Module.Booking.Application.Abstractions;
using BuildingBlocks.Infrastructure.Storage;
using Module.Booking.Application.Services;

namespace Module.Booking.Infrastructure.Infras;

/// <summary>Archivio dei file allegati alle prenotazioni: sezione <c>Booking:Attachments</c> (bucket e prefisso propri).</summary>
public sealed class AttachmentStorage : IAttachmentStorage
{
	public const string Section = "Booking:Attachments";

	private readonly IObjectStorage _storage;

	public AttachmentStorage(IObjectStorage storage) => _storage = storage;

	public async Task<(string Origin, string Path, string Sha256)> SaveAsync(string fileName, Stream content, string contentType, CancellationToken ct = default)
	{
		var stored = await _storage.SaveAsync(fileName, content, contentType, ct);
		return (stored.Origin, stored.Path, stored.Sha256);
	}

	public string GetDownloadUrl(string path) => _storage.GetDownloadUrl(path);

	public Task DeleteAsync(string path, CancellationToken ct = default) => _storage.DeleteAsync(path, ct);
}
