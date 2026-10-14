using Amazon.S3;
using Amazon.S3.Model;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Prova il job che <b>cancella</b>. È il codice con la conseguenza più pesante di tutta la retention —
/// un errore qui non si annulla — e merita di essere verificato contro uno storage e un database veri
/// invece che contro un doppio.
/// </summary>
/// <remarks>
/// Si salta da sé se LocalStack o PostgreSQL non rispondono. Pulisce ciò che crea, e tocca solo righe
/// che riconosce dal proprio marcatore in <c>reason</c>.
/// </remarks>
public sealed class StorageDeletionReconcilerTests
{
	private const string Endpoint = "http://localhost:4566";
	private const string Conn = "Host=localhost;Port=5432;Database=summer_booking_dev;Username=postgres;Password=postgres";
	private const string Marker = "prova-riconciliatore";

	private static async Task<bool> AmbienteDisponibileAsync()
	{
		try
		{
			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
			if (!(await http.GetAsync($"{Endpoint}/_localstack/health")).IsSuccessStatusCode)
				return false;

			await using var db = new NpgsqlConnection(Conn);
			await db.OpenAsync();
			await using var cmd = db.CreateCommand();
			cmd.CommandText = "SELECT to_regclass('privacy.privacy_file_deletions') IS NOT NULL";
			return (bool?)await cmd.ExecuteScalarAsync() == true;
		}
		catch
		{
			return false;
		}
	}

	private static AmazonS3Client S3()
	{
		var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.USEast2 };
		config.ServiceURL = Endpoint;
		config.ForcePathStyle = true;
		return new AmazonS3Client("test", "test", config);
	}

	private static async Task AccodaAsync(NpgsqlConnection db, string origin, string path,
		string action = "delete_object")
	{
		await using var cmd = db.CreateCommand();
		cmd.CommandText = """
			INSERT INTO privacy.privacy_file_deletions
			  (public_id, storage_origin, storage_path, action, reason, decided_at, created_at, updated_at)
			VALUES (gen_random_uuid(), @o, @p, @a::privacy.retention_action, @r, now(), now(), now())
			""";
		cmd.Parameters.AddWithValue("@o", origin);
		cmd.Parameters.AddWithValue("@p", path);
		cmd.Parameters.AddWithValue("@a", action);
		cmd.Parameters.AddWithValue("@r", Marker);
		await cmd.ExecuteNonQueryAsync();
	}

	private static async Task PulisciAsync(NpgsqlConnection db)
	{
		await using var cmd = db.CreateCommand();
		cmd.CommandText = "DELETE FROM privacy.privacy_file_deletions WHERE reason = @r";
		cmd.Parameters.AddWithValue("@r", Marker);
		await cmd.ExecuteNonQueryAsync();
	}

	private static async Task<(long InQueue, DateTime? Executed, short Attempts)> StatoAsync(
		NpgsqlConnection db, string path)
	{
		await using var cmd = db.CreateCommand();
		cmd.CommandText = """
			SELECT count(*) OVER (), executed_at, attempts
			  FROM privacy.privacy_file_deletions WHERE reason = @r AND storage_path = @p
			""";
		cmd.Parameters.AddWithValue("@r", Marker);
		cmd.Parameters.AddWithValue("@p", path);
		await using var reader = await cmd.ExecuteReaderAsync();
		if (!await reader.ReadAsync())
			return (0, null, 0);

		return (reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetDateTime(1), reader.GetInt16(2));
	}

	[Fact]
	public async Task Rimuove_l_oggetto_e_chiude_la_riga_di_coda()
	{
		if (!await AmbienteDisponibileAsync())
			return;

		var bucket = "prova-riconciliatore-" + Guid.NewGuid().ToString("N")[..8];
		const string path = "images/bathouse/da-rimuovere.jpg";
		using var s3 = S3();
		await s3.PutBucketAsync(bucket);
		await s3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = path, ContentBody = "contenuto" });

		await using var db = new NpgsqlConnection(Conn);
		await db.OpenAsync();
		try
		{
			await AccodaAsync(db, "prova_origin", path);

			var reconciler = new StorageDeletionReconciler(new StorageDeletionOptions
			{
				Targets = [new StorageDeletionTarget
				{
					Origin = "prova_origin", Bucket = bucket, Region = "us-east-2",
					EndpointUrl = Endpoint, AccessKeyId = "test", SecretAccessKey = "test"
				}]
			}, NullLogger<StorageDeletionReconciler>.Instance);

			// Prima in prova: non deve toccare niente.
			var prova = await reconciler.RunAsync(db, apply: false, CancellationToken.None);
			Assert.Equal(1, prova.Deleted);   // "quanti ne farebbe"
			var dopoProva = await StatoAsync(db, path);
			Assert.Null(dopoProva.Executed);
			Assert.True(await EsisteAsync(s3, bucket, path), "la prova non deve rimuovere l'oggetto");

			// Poi per davvero.
			var vero = await reconciler.RunAsync(db, apply: true, CancellationToken.None);
			Assert.Equal(1, vero.Deleted);
			Assert.Empty(vero.Errors);

			var dopo = await StatoAsync(db, path);
			Assert.NotNull(dopo.Executed);
			Assert.Equal(1, dopo.Attempts);
			Assert.False(await EsisteAsync(s3, bucket, path), "l'oggetto deve essere stato rimosso");

			// Una seconda corsa non ha più niente da fare: la riga è chiusa.
			var seconda = await reconciler.RunAsync(db, apply: true, CancellationToken.None);
			Assert.Equal(0, seconda.Deleted);
		}
		finally
		{
			await PulisciAsync(db);
			await s3.DeleteBucketAsync(bucket);
		}
	}

	/// <summary>
	/// La protezione che conta: un'origine senza destinazione configurata <b>non viene toccata</b>. È
	/// ciò che impedisce a un ambiente di sviluppo di cancellare dal bucket di produzione perché nessuno
	/// ha letto la configurazione.
	/// </summary>
	[Fact]
	public async Task Un_origine_non_configurata_non_viene_toccata()
	{
		if (!await AmbienteDisponibileAsync())
			return;

		await using var db = new NpgsqlConnection(Conn);
		await db.OpenAsync();
		try
		{
			// ⚠️ storage_origin e' varchar(32): un nome piu' lungo fa fallire l'INSERT con 22001. Le
			// etichette reali sono corte (s3us_east2, s3_localstack), ma chi ne aggiunge una lo sappia.
			await AccodaAsync(db, "origine_mai_configurata", "images/bathouse/intoccabile.jpg");

			// Nessuna destinazione: il riconciliatore non ha nessun bucket da cui cancellare.
			var reconciler = new StorageDeletionReconciler(new StorageDeletionOptions(),
				NullLogger<StorageDeletionReconciler>.Instance);

			var report = await reconciler.RunAsync(db, apply: true, CancellationToken.None);

			Assert.Equal(0, report.Deleted);
			Assert.Equal(0, report.Failed);
			Assert.True(report.UnconfiguredOrigins.ContainsKey("origine_mai_configurata"),
				"l'origine non configurata deve essere riportata, non ignorata in silenzio");

			// E la riga resta aperta, con zero tentativi: non è stata nemmeno provata.
			var stato = await StatoAsync(db, "images/bathouse/intoccabile.jpg");
			Assert.Null(stato.Executed);
			Assert.Equal(0, stato.Attempts);
		}
		finally
		{
			await PulisciAsync(db);
		}
	}

	/// <summary>
	/// Un bucket che non esiste è un errore, e l'errore va sulla riga: il tentativo si conta, perché è
	/// l'unica cosa che distingue «non ancora fatto» da «non si riesce».
	/// </summary>
	[Fact]
	public async Task Un_errore_si_registra_sulla_riga_e_conta_il_tentativo()
	{
		if (!await AmbienteDisponibileAsync())
			return;

		await using var db = new NpgsqlConnection(Conn);
		await db.OpenAsync();
		try
		{
			const string path = "images/bathouse/su-bucket-inesistente.jpg";
			await AccodaAsync(db, "prova_origin", path);

			var reconciler = new StorageDeletionReconciler(new StorageDeletionOptions
			{
				Targets = [new StorageDeletionTarget
				{
					Origin = "prova_origin", Bucket = "bucket-che-non-esiste-" + Guid.NewGuid().ToString("N")[..8],
					Region = "us-east-2", EndpointUrl = Endpoint, AccessKeyId = "test", SecretAccessKey = "test"
				}]
			}, NullLogger<StorageDeletionReconciler>.Instance);

			var report = await reconciler.RunAsync(db, apply: true, CancellationToken.None);

			Assert.Equal(1, report.Failed);
			Assert.NotEmpty(report.Errors);

			var stato = await StatoAsync(db, path);
			Assert.Null(stato.Executed);
			Assert.Equal(1, stato.Attempts);
		}
		finally
		{
			await PulisciAsync(db);
		}
	}

	private static async Task<bool> EsisteAsync(IAmazonS3 s3, string bucket, string key)
	{
		try
		{
			await s3.GetObjectMetadataAsync(bucket, key);
			return true;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			return false;
		}
	}

	[Fact]
	public async Task Una_riga_archive_object_sposta_di_classe_e_NON_cancella()
	{
		if (!await AmbienteDisponibileAsync())
			return;

		var bucket = "prova-riconciliatore-" + Guid.NewGuid().ToString("N")[..8];
		const string daRaffreddare = "pdf/scontrino-da-raffreddare.pdf";
		const string daRimuovere = "images/bathouse/da-rimuovere.jpg";

		using var s3 = S3();
		await s3.PutBucketAsync(bucket);
		await s3.PutObjectAsync(new PutObjectRequest
		{
			BucketName = bucket, Key = daRaffreddare, ContentBody = "scontrino", ContentType = "application/pdf"
		});
		await s3.PutObjectAsync(new PutObjectRequest
		{
			BucketName = bucket, Key = daRimuovere, ContentBody = "foto"
		});

		await using var db = new NpgsqlConnection(Conn);
		await db.OpenAsync();
		try
		{
			// ⚠️ Le due azioni nello STESSO lotto: è il caso che conta. Se il job le trattasse insieme,
			// lo scontrino finirebbe in una DeleteObjectsRequest e sparirebbe — ed è esattamente il
			// motivo per cui, fino al 2026-10-05, il motore si rifiutava di accodare archive_object.
			await AccodaAsync(db, "prova_origin", daRaffreddare, "archive_object");
			await AccodaAsync(db, "prova_origin", daRimuovere);

			var reconciler = new StorageDeletionReconciler(new StorageDeletionOptions
			{
				Targets = [new StorageDeletionTarget
				{
					Origin = "prova_origin", Bucket = bucket, Region = "us-east-2",
					EndpointUrl = Endpoint, AccessKeyId = "test", SecretAccessKey = "test",
					ArchiveStorageClass = "GLACIER"
				}]
			}, NullLogger<StorageDeletionReconciler>.Instance);

			var esito = await reconciler.RunAsync(db, apply: true, CancellationToken.None);

			Assert.Empty(esito.Errors);
			Assert.Equal(1, esito.Deleted);
			Assert.Equal(1, esito.Archived);

			// Lo scontrino c'è ancora, ed è in classe fredda.
			Assert.True(await EsisteAsync(s3, bucket, daRaffreddare),
				"archive_object NON deve rimuovere l'oggetto");
			var meta = await s3.GetObjectMetadataAsync(bucket, daRaffreddare);
			Assert.Equal("GLACIER", meta.StorageClass?.Value);

			// Il content type è sopravvissuto alla ricopiatura: è il motivo di MetadataDirective.COPY.
			// Senza, un PDF torna indietro come application/octet-stream.
			Assert.Equal("application/pdf", meta.Headers.ContentType);

			// La foto invece è stata rimossa.
			Assert.False(await EsisteAsync(s3, bucket, daRimuovere));

			// purged_at si valorizza SOLO per le cancellazioni: un oggetto raffreddato c'è ancora.
			var statoRaffreddato = await StatoAsync(db, daRaffreddare);
			Assert.NotNull(statoRaffreddato.Executed);

			// Una seconda corsa non ricopia niente: la riga è chiusa.
			var seconda = await reconciler.RunAsync(db, apply: true, CancellationToken.None);
			Assert.Equal(0, seconda.Deleted);
			Assert.Equal(0, seconda.Archived);
		}
		finally
		{
			await PulisciAsync(db);
			// Il bucket si svuota a mano: DeleteBucket su un bucket non vuoto fallisce, e un test
			// fallito a metà lascerebbe dietro di sé bucket che fanno fallire anche i successivi.
			foreach (var k in new[] { daRaffreddare, daRimuovere })
			{
				try { await s3.DeleteObjectAsync(bucket, k); } catch (AmazonS3Exception) { }
			}
			await s3.DeleteBucketAsync(bucket);
		}
	}
}
