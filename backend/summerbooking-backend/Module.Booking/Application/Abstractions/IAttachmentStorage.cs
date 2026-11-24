namespace Module.Booking.Application.Abstractions;

/// <summary>Archivio dei file allegati (S3).</summary>
public interface IAttachmentStorage
{
	Task<(string Origin, string Path, string Sha256)> SaveAsync(string fileName, Stream content, string contentType, CancellationToken ct = default);
	string GetDownloadUrl(string path);
	Task DeleteAsync(string path, CancellationToken ct = default);
}
