using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;

namespace BuildingBlocks.Infrastructure.Storage;

/// <summary>
/// Configurazione di un archivio di file applicativi. Ogni modulo ne lega una istanza alla propria
/// sezione (es. <c>Identity:Documents</c>): bucket e prefisso sono suoi, il codice è comune.
/// </summary>
public sealed class ObjectStorageOptions
{
	public string? Bucket { get; set; }
	public string Region { get; set; } = "us-east-2";
	public string? EndpointUrl { get; set; }
	public string? AccessKeyId { get; set; }
	public string? SecretAccessKey { get; set; }
	/// <summary>Codice dell'origine registrato accanto al percorso (<c>storage_origin</c>).</summary>
	public string Origin { get; set; } = "s3_us_east_2";
	public string KeyPrefix { get; set; } = string.Empty;
	public int DownloadUrlMinutes { get; set; } = 15;
}

/// <summary>File salvato: origine + percorso relativo (mai l'URL completo a DB) e hash del contenuto.</summary>
public sealed record StoredObject(string Origin, string Path, string Sha256);

/// <summary>Archivio di file applicativi: salvataggio con hash e link di download a tempo.</summary>
public interface IObjectStorage
{
	string Origin { get; }
	Task<StoredObject> SaveAsync(string fileName, Stream content, string contentType, CancellationToken ct = default);
	string GetDownloadUrl(string path);
}

/// <summary>Implementazione su S3 (o LocalStack). Il client nasce al primo uso.</summary>
public sealed class S3ObjectStorage : IObjectStorage
{
	private readonly ObjectStorageOptions _options;
	private readonly Lazy<IAmazonS3> _client;

	public S3ObjectStorage(ObjectStorageOptions options)
	{
		_options = options;
		_client = new Lazy<IAmazonS3>(() => S3ClientFactory.Create(options.Region, options.EndpointUrl, options.AccessKeyId, options.SecretAccessKey));
	}

	public string Origin => _options.Origin;

	/// <summary>Chiave <c>{prefisso}/{uuid}/{nome ripulito}</c>: nessuna collisione, nome originale leggibile.</summary>
	public async Task<StoredObject> SaveAsync(string fileName, Stream content, string contentType, CancellationToken ct = default)
	{
		using var buffer = new MemoryStream();
		await content.CopyToAsync(buffer, ct);
		var sha = Convert.ToHexStringLower(SHA256.HashData(buffer.GetBuffer().AsSpan(0, (int)buffer.Length)));
		buffer.Position = 0;

		var safeName = string.Concat(Path.GetFileName(fileName).Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));
		var prefix = _options.KeyPrefix.Trim('/');
		var key = $"{(prefix.Length > 0 ? prefix + "/" : string.Empty)}{Guid.CreateVersion7():N}/{safeName}";

		await _client.Value.PutObjectAsync(new PutObjectRequest
		{
			BucketName = Bucket,
			Key = key,
			InputStream = buffer,
			ContentType = contentType,
		}, ct);

		return new StoredObject(_options.Origin, key, sha);
	}

	public string GetDownloadUrl(string path) => _client.Value.GetPreSignedURL(new GetPreSignedUrlRequest
	{
		BucketName = Bucket,
		Key = path,
		Verb = HttpVerb.GET,
		Expires = DateTime.UtcNow.AddMinutes(_options.DownloadUrlMinutes),
	});

	private string Bucket => _options.Bucket ?? throw new InvalidOperationException("Bucket dell'archivio di file non configurato.");
}
