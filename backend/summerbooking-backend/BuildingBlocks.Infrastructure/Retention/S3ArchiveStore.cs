using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

using BuildingBlocks.Infrastructure.Storage;

namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>Configurazione dell'archivio su storage a oggetti.</summary>
public sealed class ArchiveStoreOptions
{
	/// <summary>Bucket dell'archivio. ⚠️ <b>Mai</b> il bucket dei file applicativi: qui finiscono righe di database.</summary>
	public string Bucket { get; set; } = "summerbooking-archivio";

	public string Region { get; set; } = "us-east-2";

	/// <summary>
	/// Endpoint alternativo. Valorizzato = LocalStack o un altro S3 compatibile; vuoto = AWS vera.
	/// Con un endpoint esplicito si attiva anche il path-style addressing, che LocalStack richiede.
	/// </summary>
	public string? EndpointUrl { get; set; }

	public string? AccessKeyId { get; set; }
	public string? SecretAccessKey { get; set; }

	/// <summary>Etichetta scritta su <c>privacy_retention_runs.archive_origin</c>.</summary>
	public string Origin { get; set; } = "s3_archive";

	/// <summary>Classe di storage richiesta. Su LocalStack è accettata e ignorata.</summary>
	public string? StorageClass { get; set; }
}

/// <summary>
/// Archivio su S3 (o su un S3 compatibile, come LocalStack). Scrive un oggetto per lotto, in
/// <b>JSON Lines compresso</b>: una riga di database per riga di file.
/// </summary>
/// <remarks>
/// JSONL e non un dump SQL perché l'archivio deve restare leggibile anche quando lo schema che l'ha
/// prodotto non esiste più: è il punto di archiviare. E compresso perché le righe sono ripetitive e il
/// costo di storage è la ragione per cui si archivia invece di tenere tutto caldo.
/// </remarks>
public sealed class S3ArchiveStore : IArchiveStore, IDisposable
{
	private readonly IAmazonS3 _s3;
	private readonly ArchiveStoreOptions _options;
	private readonly ILogger<S3ArchiveStore> _log;
	private bool _ready;

	public S3ArchiveStore(ArchiveStoreOptions options, ILogger<S3ArchiveStore> log)
	{
		_options = options;
		_log = log;

		_s3 = S3ClientFactory.Create(options.Region, options.EndpointUrl, options.AccessKeyId, options.SecretAccessKey);
	}

	public string Origin => _options.Origin;

	public async Task EnsureReadyAsync(CancellationToken ct)
	{
		if (_ready)
			return;

		var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3, _options.Bucket);
		if (!exists)
		{
			await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _options.Bucket }, ct);
			_log.LogInformation("Bucket di archivio '{Bucket}' creato su {Endpoint}.",
				_options.Bucket, _options.EndpointUrl ?? "AWS");
		}

		_ready = true;
	}

	public string BuildKey(string schema, string table, Guid policyPublicId, DateTime nowUtc, int part)
		=> $"archivio/{schema}/{table}/{nowUtc:yyyy}/{nowUtc:MM}/{nowUtc:yyyyMMddTHHmmssZ}-{policyPublicId}-{part:D4}.jsonl.gz";

	public async Task<ArchiveWriteResult> WriteAsync(string key, Stream content, CancellationToken ct)
	{
		await EnsureReadyAsync(ct);

		var size = content.CanSeek ? content.Length : -1;
		// ⚠️ Niente DisablePayloadSigning: l'SDK v4 lo accetta solo su HTTPS e LocalStack parla HTTP,
		// quindi impostarlo per "agevolare" un endpoint non-AWS faceva fallire ogni singola scrittura con
		// "When DisablePayloadSigning is true, the request must be sent over HTTPS". Trovato da
		// S3ArchiveStoreTests: senza quel test ogni policy archive_cold sarebbe fallita alla prima corsa.
		var request = new PutObjectRequest
		{
			BucketName = _options.Bucket,
			Key = key,
			InputStream = content,
			ContentType = "application/gzip"
		};

		if (!string.IsNullOrWhiteSpace(_options.StorageClass))
			request.StorageClass = S3StorageClass.FindValue(_options.StorageClass);

		await _s3.PutObjectAsync(request, ct);

		if (size < 0)
		{
			var meta = await _s3.GetObjectMetadataAsync(_options.Bucket, key, ct);
			size = meta.ContentLength;
		}

		return new ArchiveWriteResult(key, size);
	}

	public void Dispose() => _s3.Dispose();
}
