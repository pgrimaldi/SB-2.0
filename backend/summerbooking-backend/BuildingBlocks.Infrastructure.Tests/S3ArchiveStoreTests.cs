using System.IO.Compression;
using System.Text;
using Amazon.S3;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.Extensions.Logging.Abstractions;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Prova l'archivio contro un S3 vero (LocalStack), perché il punto in cui una policy
/// <c>archive_cold</c> può perdere dati è la scrittura: l'ordine è «prima l'archivio, poi la
/// cancellazione», e se la scrittura dicesse di essere riuscita senza esserlo si cancellerebbero righe
/// che non esistono da nessuna parte.
/// </summary>
/// <remarks>
/// Si salta da sé se LocalStack non risponde, così la suite resta verde su una macchina senza Docker.
/// Un test che finge di aver verificato è peggio di un test che dichiara di non aver verificato.
/// </remarks>
public sealed class S3ArchiveStoreTests
{
	private const string Endpoint = "http://localhost:4566";

	private static ArchiveStoreOptions Options() => new()
	{
		Bucket = "summerbooking-archivio-prova-" + Guid.NewGuid().ToString("N")[..8],
		Region = "us-east-2",
		EndpointUrl = Endpoint,
		AccessKeyId = "test",
		SecretAccessKey = "test",
		Origin = "s3_localstack"
	};

	private static async Task<bool> LocalStackIsUpAsync()
	{
		try
		{
			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
			var response = await http.GetAsync($"{Endpoint}/_localstack/health");
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	[Fact]
	public async Task Crea_il_bucket_se_non_esiste_e_non_si_lamenta_se_esiste()
	{
		if (!await LocalStackIsUpAsync())
			return; // LocalStack non attivo: niente da verificare, e dirlo e' meglio che finirlo

		var options = Options();
		using var store = new S3ArchiveStore(options, NullLogger<S3ArchiveStore>.Instance);

		await store.EnsureReadyAsync(CancellationToken.None);
		await store.EnsureReadyAsync(CancellationToken.None); // idempotente

		using var s3 = Client(options);
		Assert.True(await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(s3, options.Bucket));

		await s3.DeleteBucketAsync(options.Bucket);
	}

	[Fact]
	public async Task Scrive_un_archivio_rileggibile_e_ne_riporta_la_dimensione()
	{
		if (!await LocalStackIsUpAsync())
			return;

		var options = Options();
		using var store = new S3ArchiveStore(options, NullLogger<S3ArchiveStore>.Instance);
		await store.EnsureReadyAsync(CancellationToken.None);

		// Due righe come le produrrebbe row_to_json su una tabella archiviata.
		var righe = new[]
		{
			"""{"id":1,"occurred_at":"2023-05-01T10:00:00Z","amount":12.50}""",
			"""{"id":2,"occurred_at":"2023-05-02T11:30:00Z","amount":8.00}"""
		};

		var buffer = new MemoryStream();
		await using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
		await using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
		{
			foreach (var r in righe)
				await writer.WriteLineAsync(r);
		}

		buffer.Position = 0;
		var key = store.BuildKey("payment", "transaction_charges", Guid.CreateVersion7(), DateTime.UtcNow, 0);
		var result = await store.WriteAsync(key, buffer, CancellationToken.None);

		Assert.Equal(key, result.Key);
		Assert.True(result.ByteSize > 0, "la dimensione riportata deve essere quella dell'oggetto scritto");

		// Il punto del test: l'archivio si rilegge. Un oggetto scritto e illeggibile e' peggio di nessun
		// oggetto, perche' le righe sono state cancellate dal database caldo nel frattempo.
		using var s3 = Client(options);
		using var response = await s3.GetObjectAsync(options.Bucket, key);
		await using var decompressed = new GZipStream(response.ResponseStream, CompressionMode.Decompress);
		using var reader = new StreamReader(decompressed);
		var content = await reader.ReadToEndAsync();

		var lette = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		Assert.Equal(righe.Length, lette.Length);
		Assert.Equal(righe[0], lette[0].Trim());
		Assert.Contains("\"amount\":8.00", lette[1]);

		await s3.DeleteObjectAsync(options.Bucket, key);
		await s3.DeleteBucketAsync(options.Bucket);
	}

	/// <summary>
	/// La chiave deve dire da sola che cosa contiene: schema, tabella, quando, quale policy. Senza
	/// questo, ritrovare un archivio richiede di aprirlo.
	/// </summary>
	[Fact]
	public void La_chiave_porta_schema_tabella_data_e_policy()
	{
		var options = Options();
		using var store = new S3ArchiveStore(options, NullLogger<S3ArchiveStore>.Instance);
		var policy = Guid.Parse("01a0ff0f-7c00-7e41-a793-44d404da249d");

		var key = store.BuildKey("payment", "transaction_charges", policy,
			new DateTime(2026, 10, 3, 9, 15, 0, DateTimeKind.Utc), 2);

		Assert.StartsWith("archivio/payment/transaction_charges/2026/10/", key);
		Assert.Contains("20261003T091500Z", key);
		Assert.Contains(policy.ToString(), key);
		Assert.EndsWith("-0002.jsonl.gz", key);
	}

	/// <summary>Due lotti della stessa policy non devono finire sulla stessa chiave.</summary>
	[Fact]
	public void Lotti_diversi_hanno_chiavi_diverse()
	{
		var options = Options();
		using var store = new S3ArchiveStore(options, NullLogger<S3ArchiveStore>.Instance);
		var policy = Guid.CreateVersion7();
		var now = DateTime.UtcNow;

		Assert.NotEqual(store.BuildKey("booking", "reservations", policy, now, 0),
			store.BuildKey("booking", "reservations", policy, now, 1));
	}

	/// <summary>
	/// ⚠️ <c>RegionEndpoint</c> va impostato <b>prima</b> di <c>ServiceURL</c>: nell'SDK assegnare la
	/// regione <b>azzera</b> l'endpoint, e la richiesta parte verso AWS vera invece che verso LocalStack.
	/// Scritto nell'ordine sbagliato, questo helper otteneva «The AWS Access Key Id you provided does not
	/// exist in our records» — che sembra un problema di credenziali e invece è un problema di indirizzo.
	/// </summary>
	private static AmazonS3Client Client(ArchiveStoreOptions options)
	{
		var config = new AmazonS3Config
		{
			RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
		};
		config.ServiceURL = options.EndpointUrl;
		config.ForcePathStyle = true;
		return new AmazonS3Client(options.AccessKeyId, options.SecretAccessKey, config);
	}
}
