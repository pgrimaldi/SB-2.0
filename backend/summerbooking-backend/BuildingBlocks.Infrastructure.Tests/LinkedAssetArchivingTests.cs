using BuildingBlocks.Infrastructure.Retention;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Prova, contro un PostgreSQL e un S3 veri, che archiviare una riga porti con sé il file che la riga
/// nomina.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Dati sintetici per necessità, non per comodità.</b> Il caso reale è lo scontrino fiscale:
/// la riga di <c>fiskaly.receipts</c> si archivia a 36 mesi e il PDF deve andarsene con lei. Ma la
/// ricevuta più vecchia in archivio è del 2026-06, quindi su dati reali quella policy trova zero righe
/// e il meccanismo non si esercita <b>per anni</b>. Senza questo test resterebbe codice mai eseguito,
/// pronto a sbagliare la notte in cui il primo scontrino compie tre anni.
/// </para>
/// <para>
/// Tutto gira in una <b>transazione che non viene mai confermata</b>: il database di sviluppo non
/// cambia. L'unica cosa che resta fuori dalla transazione è l'oggetto scritto su S3 — un'operazione su
/// storage non partecipa a una transazione, ed è esattamente il motivo per cui esiste la coda — quindi
/// il bucket di prova si rimuove a mano alla fine.
/// </para>
/// </remarks>
public sealed class LinkedAssetArchivingTests
{
	private const string Endpoint = "http://localhost:4566";

	private const string Conn =
		"Host=localhost;Port=5432;Database=summer_booking_dev;Username=postgres;Password=postgres";

	private static async Task<bool> LocalStackIsUpAsync()
	{
		try
		{
			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
			var r = await http.GetAsync($"{Endpoint}/_localstack/health");
			return r.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	private static async Task<NpgsqlConnection?> ApriAsync()
	{
		try
		{
			var c = new NpgsqlConnection(Conn);
			await c.OpenAsync();
			return c;
		}
		catch
		{
			return null;
		}
	}

	[Fact]
	public async Task Archiviare_una_ricevuta_accoda_e_inventaria_il_suo_PDF()
	{
		if (!await LocalStackIsUpAsync())
			return;

		await using var db = await ApriAsync();
		if (db is null)
			return;

		var bucket = "prova-archivio-collegato-" + Guid.NewGuid().ToString("N")[..8];
		var archivio = new ArchiveStoreOptions
		{
			Bucket = bucket,
			Region = "us-east-2",
			EndpointUrl = Endpoint,
			AccessKeyId = "test",
			SecretAccessKey = "test",
			Origin = "s3_prova_archivio",
		};

		var store = new S3ArchiveStore(archivio, NullLogger<S3ArchiveStore>.Instance);
		var engine = new RetentionEngine(store, NullLogger<RetentionEngine>.Instance);

		var percorsoPdf = $"pdf/prova-collegato-{Guid.NewGuid():N}.pdf";

		await using var tx = await db.BeginTransactionAsync();
		try
		{
			// --- una ricevuta NON fiscale, firmata cinque anni fa, con il suo PDF ------------------
			// Non fiscale perché una fiscale pretende `fiskaly_account_id` e `cash_register_id`
			// (due CHECK), e qui quello che conta è il legame riga → file, non il tipo di documento.
			long assetId;
			await using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = """
					INSERT INTO fiskaly.media_assets (origin, path, kind, byte_size, created_at, updated_at)
					VALUES ('s3_us_east_2'::fiskaly.media_origin, @path,
					        'non_fiscal_receipt_pdf'::fiskaly.media_kind, 4096, now(), now())
					RETURNING id
					""";
				cmd.Parameters.AddWithValue("@path", percorsoPdf);
				assetId = (long)(await cmd.ExecuteScalarAsync())!;
			}

			await using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = """
					INSERT INTO fiskaly.receipts
					  (public_id, property_id, document_kind, record_state, total_amount, taxable_amount,
					   vat_amount, booking_number, signed_at, pdf_asset_id, created_at, updated_at)
					VALUES (gen_random_uuid(), gen_random_uuid(), 'non_fiscal'::fiskaly.receipt_kind,
					        'completed'::fiskaly.receipt_record_state, 122.00, 100.00, 22.00,
					        'PROVA-COLLEGATO', now() - interval '5 years', @asset, now(), now())
					""";
				cmd.Parameters.AddWithValue("@asset", assetId);
				await cmd.ExecuteNonQueryAsync();
			}

			// --- la corsa, per davvero, sul solo schema fiskaly -----------------------------------
			var report = await engine.RunAsync(db, "fiskaly", new RetentionRunOptions
			{
				NowUtc = DateTime.UtcNow,
				Apply = true,
				IncludeSeasonal = true,
			}, CancellationToken.None);

			// ⚠️ L'asserzione porta i MESSAGGI, non il conteggio. Un «atteso 0, trovato 3» costringe a
			// rieseguire con un debugger - ed e' quello che e' servito la prima volta, quando tre
			// policy fallivano con 25P02 «transazione interrotta» e il motivo vero era un'altra cosa,
			// inghiottita da un blocco catch (vedi RetentionRunLog).
			var errori = string.Join(" | ", report.Outcomes
				.Where(o => o.Status == PolicyRunStatus.Failed)
				.Select(o => $"{o.Policy.TableName}.{o.Policy.Action}: {o.Error}"));
			Assert.Equal("", errori);

			var archiviate = report.Outcomes.Single(o => o.Policy.Action == "archive_cold").AffectedRows;
			Assert.Equal(1, archiviate);

			// --- 1. il PDF è in coda, come archive_object e non come cancellazione ----------------
			await using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = """
					SELECT action::text, asset_schema, asset_table, asset_id, storage_origin, byte_size
					  FROM privacy.privacy_file_deletions
					 WHERE storage_path = @path
					""";
				cmd.Parameters.AddWithValue("@path", percorsoPdf);
				await using var r = await cmd.ExecuteReaderAsync();

				Assert.True(await r.ReadAsync(), "il PDF deve essere in coda");
				Assert.Equal("archive_object", r.GetString(0));
				Assert.Equal("fiskaly", r.GetString(1));
				Assert.Equal("media_assets", r.GetString(2));
				Assert.Equal(assetId, r.GetInt64(3));
				// L'origine è quella dell'ASSET, non quella dell'archivio: il PDF sta sul bucket dei
				// file, non su quello degli archivi, e il job risolve il bucket da questa etichetta.
				Assert.Equal("s3_us_east_2", r.GetString(4));
				Assert.False(await r.ReadAsync(), "una riga sola, non due");
			}

			// --- 2. il PDF è inventariato, e l'inventario distingue le due nature -----------------
			await using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = """
					SELECT object_kind::text, row_count, byte_size, source_table
					  FROM privacy.privacy_archive_manifests
					 WHERE storage_path = @path
					""";
				cmd.Parameters.AddWithValue("@path", percorsoPdf);
				await using var r = await cmd.ExecuteReaderAsync();

				Assert.True(await r.ReadAsync(), "il PDF deve essere inventariato");
				Assert.Equal("linked_asset", r.GetString(0));
				// ⚠️ NULL e non zero: un file non contiene righe di database, e «zero righe» sarebbe
				// un'affermazione diversa. È il vincolo ck_..._counts che lo impone.
				Assert.True(r.IsDBNull(1), "row_count deve essere NULL su un file collegato");
				Assert.Equal(4096L, r.GetInt64(2));
				Assert.Equal("media_assets", r.GetString(3));
			}

			// --- 3. e l'archivio delle righe è inventariato come row_archive ----------------------
			await using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = """
					SELECT object_kind::text, row_count
					  FROM privacy.privacy_archive_manifests
					 WHERE source_schema = 'fiskaly' AND source_table = 'receipts'
					""";
				await using var r = await cmd.ExecuteReaderAsync();

				Assert.True(await r.ReadAsync(), "l'archivio delle righe deve essere inventariato");
				Assert.Equal("row_archive", r.GetString(0));
				Assert.Equal(1L, r.GetInt64(1));
			}

			// --- 4. la riga della ricevuta ha lasciato il database caldo --------------------------
			await using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = "SELECT count(*) FROM fiskaly.receipts WHERE pdf_asset_id = @asset";
				cmd.Parameters.AddWithValue("@asset", assetId);
				Assert.Equal(0L, (long)(await cmd.ExecuteScalarAsync())!);
			}

			// --- 5. e la riga dell'asset invece c'è ancora ----------------------------------------
			// archive_object NON cancella niente: raffredda l'oggetto e lascia la riga al suo posto.
			await using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = "SELECT count(*) FROM fiskaly.media_assets WHERE id = @asset";
				cmd.Parameters.AddWithValue("@asset", assetId);
				Assert.Equal(1L, (long)(await cmd.ExecuteScalarAsync())!);
			}
		}
		finally
		{
			await tx.RollbackAsync();
			await SvuotaBucketAsync(archivio);
		}
	}

	/// <summary>
	/// Il bucket di prova si svuota e si rimuove: l'oggetto su S3 è l'unica cosa che la transazione
	/// non annulla, e un bucket non vuoto non si può cancellare.
	/// </summary>
	private static async Task SvuotaBucketAsync(ArchiveStoreOptions o)
	{
		try
		{
			// ⚠️ L'ORDINE CONTA: RegionEndpoint azzera ServiceURL. Vedi S3ArchiveStore.
			var config = new Amazon.S3.AmazonS3Config
			{
				RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(o.Region),
			};
			config.ServiceURL = o.EndpointUrl;
			config.ForcePathStyle = true;

			using var s3 = new Amazon.S3.AmazonS3Client(o.AccessKeyId, o.SecretAccessKey, config);

			var elenco = await s3.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request
			{
				BucketName = o.Bucket,
			});

			foreach (var oggetto in elenco.S3Objects ?? [])
				await s3.DeleteObjectAsync(o.Bucket, oggetto.Key);

			await s3.DeleteBucketAsync(o.Bucket);
		}
		catch (Amazon.S3.AmazonS3Exception)
		{
			// Il bucket può non essere mai stato creato, se il test è fallito prima. Non è un esito.
		}
	}
}
