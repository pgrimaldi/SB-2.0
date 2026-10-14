using Module.Catalog.Infrastructure.Infras;
using Npgsql;

namespace Module.Catalog.Tests;

/// <summary>
/// Verifica il perimetro del job delle foto degli stabilimenti cessati, contro un PostgreSQL vero.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Questo è l'unico modo di verificare quel job, e il motivo va saputo.</b> Il legacy non ha mai
/// avuto gli abbonamenti — erano gestiti fuori dal portale e attivati a mano (risposta del titolare
/// del 2026-10-02, Q10) — quindi <c>identity.account_subscriptions</c> è <b>vuota</b> sui dati
/// migrati, e la cessazione non è calcolabile per nessuno stabilimento storico. Il perimetro su dati
/// reali trova zero righe <i>per costruzione</i>, e continuerà a trovarne zero finché gli abbonamenti
/// non saranno gestiti dal nuovo sistema.
/// </para>
/// <para>
/// Senza questi test quel job resterebbe codice mai eseguito, pronto a sbagliare la notte in cui i
/// primi abbonamenti ci saranno — e a sbagliare cancellando foto.
/// </para>
/// <para>
/// I dati di prova si creano e si distruggono dentro una <b>transazione che non viene mai
/// confermata</b>: non sporcano il database di sviluppo nemmeno se il test fallisce a metà.
/// </para>
/// </remarks>
public class CeasedPropertyPhotoPerimeterTests
{
	private const string Conn =
		"Host=localhost;Port=5432;Database=summer_booking_dev;Username=postgres;Password=postgres";

	private static async Task<NpgsqlConnection?> ApriAsync()
	{
		try
		{
			var c = new NpgsqlConnection(Conn);
			await c.OpenAsync();
			return c;
		}
		catch (Exception)
		{
			// Senza database il test non ha niente da dire: non e' un fallimento del codice.
			return null;
		}
	}

	/// <summary>
	/// Semina uno stabilimento con cinque foto e restituisce gli id dei media, in ordine di
	/// <c>sort_order</c>.
	/// </summary>
	/// <param name="ceasedAt">
	/// Data di cessazione da scrivere su <c>closed_at</c>, oppure NULL per uno stabilimento ancora
	/// attivo.
	/// </param>
	/// <param name="statoAbbonamento">
	/// <c>cancelled</c> · <c>expired</c> = ha chiuso; <c>active</c> · <c>suspended</c> = no.
	/// </param>
	private static async Task<(long[] MediaIds, long BathhouseId)> SeminaAsync(
		NpgsqlConnection db, DateTime? ceasedAt, string statoAbbonamento)
	{
		var propertyPublicId = Guid.CreateVersion7();

		long propertyId;
		await using (var cmd = db.CreateCommand())
		{
			cmd.CommandText = """
				INSERT INTO catalog.properties (public_id, name, created_at, updated_at)
				VALUES (@pid, 'Prova retention foto', now(), now())
				RETURNING id
				""";
			cmd.Parameters.AddWithValue("@pid", propertyPublicId);
			propertyId = (long)(await cmd.ExecuteScalarAsync())!;
		}

		long bathhouseId;
		await using (var cmd = db.CreateCommand())
		{
			cmd.CommandText = """
				INSERT INTO catalog.bathhouses (public_id, property_id, created_at, updated_at)
				VALUES (@pub, @prop, now(), now())
				RETURNING id
				""";
			cmd.Parameters.AddWithValue("@pub", Guid.CreateVersion7());
			cmd.Parameters.AddWithValue("@prop", propertyId);
			bathhouseId = (long)(await cmd.ExecuteScalarAsync())!;
		}

		var mediaIds = new long[5];
		for (var i = 0; i < 5; i++)
		{
			await using var cmd = db.CreateCommand();
			cmd.CommandText = """
				INSERT INTO catalog.media_assets (origin, path, kind, created_at, updated_at)
				VALUES ('s3us_east2'::catalog.media_origin, @path, 'photo'::catalog.media_kind, now(), now())
				RETURNING id
				""";
			cmd.Parameters.AddWithValue("@path", $"images/bathouse/prova-{propertyPublicId:N}-{i}.jpg");
			mediaIds[i] = (long)(await cmd.ExecuteScalarAsync())!;

			await using var link = db.CreateCommand();
			link.CommandText = """
				INSERT INTO catalog.bathhouse_photos (bathhouse_id, media_asset_id, sort_order, created_at, updated_at)
				VALUES (@b, @m, @s, now(), now())
				""";
			link.Parameters.AddWithValue("@b", bathhouseId);
			link.Parameters.AddWithValue("@m", mediaIds[i]);
			link.Parameters.AddWithValue("@s", (short)i);
			await link.ExecuteNonQueryAsync();
		}

		// L'abbonamento e il suo periodo: e' da qui che si deriva la cessazione.
		long subscriptionId;
		await using (var cmd = db.CreateCommand())
		{
			cmd.CommandText = """
				INSERT INTO identity.account_subscriptions
				  (property_id, license_plan_id, status, starts_at, ends_at, created_at, updated_at)
				VALUES (@pid, 1, @st::identity.subscription_status,
				        now() - interval '10 years', now() - interval '5 years', now(), now())
				RETURNING id
				""";
			cmd.Parameters.AddWithValue("@pid", propertyPublicId);
			cmd.Parameters.AddWithValue("@st", statoAbbonamento);
			subscriptionId = (long)(await cmd.ExecuteScalarAsync())!;
		}

		await using (var cmd = db.CreateCommand())
		{
			cmd.CommandText = """
				INSERT INTO identity.account_subscription_periods
				  (subscription_id, property_id, license_plan_id, license_plan_code, starts_at,
				   period_kind, closed_at, created_at, updated_at)
				VALUES (@sub, @pid, 1, 'basic', now() - interval '10 years',
				        'initial'::identity.subscription_period_kind, @closed, now(), now())
				""";
			cmd.Parameters.AddWithValue("@sub", subscriptionId);
			cmd.Parameters.AddWithValue("@pid", propertyPublicId);
			cmd.Parameters.AddWithValue("@closed", (object?)ceasedAt ?? DBNull.Value);
			await cmd.ExecuteNonQueryAsync();
		}

		return (mediaIds, bathhouseId);
	}

	/// <summary>Esegue il perimetro VERO del job e restituisce gli id selezionati.</summary>
	private static async Task<List<long>> NelPerimetroAsync(NpgsqlConnection db, DateTime cutoff)
	{
		await using var cmd = db.CreateCommand();
		cmd.CommandText = "SELECT m.id\n" + CeasedPropertyPhotoRetentionJob.Perimetro();
		cmd.Parameters.AddWithValue("@cutoff", cutoff);

		var ids = new List<long>();
		await using var reader = await cmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
			ids.Add(reader.GetInt64(0));
		return ids;
	}

	[Fact]
	public async Task Tiene_le_prime_tre_foto_e_prende_le_altre()
	{
		await using var db = await ApriAsync();
		if (db is null)
			return;

		await using var tx = await db.BeginTransactionAsync();
		var (media, _) = await SeminaAsync(db, DateTime.UtcNow.AddYears(-5), "cancelled");

		var selezionate = await NelPerimetroAsync(db, DateTime.UtcNow.AddMonths(-24));

		// Le prime tre per sort_order (0, 1, 2) restano pubblicate per sempre: decisione del titolare.
		Assert.DoesNotContain(media[0], selezionate);
		Assert.DoesNotContain(media[1], selezionate);
		Assert.DoesNotContain(media[2], selezionate);

		// La quarta e la quinta seguono la cessazione.
		Assert.Contains(media[3], selezionate);
		Assert.Contains(media[4], selezionate);

		await tx.RollbackAsync();
	}

	[Fact]
	public async Task La_copertina_non_si_tocca_nemmeno_se_e_oltre_la_terza()
	{
		await using var db = await ApriAsync();
		if (db is null)
			return;

		await using var tx = await db.BeginTransactionAsync();
		var (media, bathhouseId) = await SeminaAsync(db, DateTime.UtcNow.AddYears(-5), "cancelled");

		// ⚠️ Il caso che la condizione sull'ordinamento NON copre: la copertina e' la quinta foto.
		// Senza l'esclusione esplicita su main_photo_id, la scheda pubblica perderebbe l'immagine
		// principale — ed e' proprio quella che il titolare ha chiesto di conservare.
		await using (var cmd = db.CreateCommand())
		{
			cmd.CommandText = "UPDATE catalog.bathhouses SET main_photo_id = @m WHERE id = @b";
			cmd.Parameters.AddWithValue("@m", media[4]);
			cmd.Parameters.AddWithValue("@b", bathhouseId);
			await cmd.ExecuteNonQueryAsync();
		}

		var selezionate = await NelPerimetroAsync(db, DateTime.UtcNow.AddMonths(-24));

		Assert.DoesNotContain(media[4], selezionate);
		Assert.Contains(media[3], selezionate);

		await tx.RollbackAsync();
	}

	[Theory]
	[InlineData("active")]
	[InlineData("suspended")]
	public async Task Uno_stabilimento_attivo_o_sospeso_non_e_cessato(string stato)
	{
		await using var db = await ApriAsync();
		if (db is null)
			return;

		await using var tx = await db.BeginTransactionAsync();

		// ⚠️ Il caso di gran lunga più pericoloso. Un periodo CHIUSO cinque anni fa esiste anche per
		// chi ha semplicemente rinnovato il piano, e «sospeso» non è una cessazione: era l'obiezione
		// che aveva bloccato la decisione. Se il predicato guardasse solo closed_at, la retention cancellerebbe
		// le foto di uno stabilimento che sta lavorando.
		var (media, _) = await SeminaAsync(db, DateTime.UtcNow.AddYears(-5), stato);

		var selezionate = await NelPerimetroAsync(db, DateTime.UtcNow.AddMonths(-24));

		Assert.DoesNotContain(media[3], selezionate);
		Assert.DoesNotContain(media[4], selezionate);

		await tx.RollbackAsync();
	}

	[Fact]
	public async Task Una_cessazione_troppo_recente_non_e_ancora_scaduta()
	{
		await using var db = await ApriAsync();
		if (db is null)
			return;

		await using var tx = await db.BeginTransactionAsync();

		// Cessato un mese fa: i 24 mesi non sono passati.
		var (media, _) = await SeminaAsync(db, DateTime.UtcNow.AddMonths(-1), "cancelled");

		var selezionate = await NelPerimetroAsync(db, DateTime.UtcNow.AddMonths(-24));

		Assert.DoesNotContain(media[3], selezionate);
		Assert.DoesNotContain(media[4], selezionate);

		await tx.RollbackAsync();
	}

	[Fact]
	public async Task Un_abbonamento_chiuso_senza_data_non_fa_scadere_niente()
	{
		await using var db = await ApriAsync();
		if (db is null)
			return;

		await using var tx = await db.BeginTransactionAsync();

		// `closed_at` NULL: l'abbonamento risulta cancellato ma non si sa da quando. Il predicato usa
		// max(closed_at), che su soli NULL vale NULL, e `NULL < @cutoff` non è vero: non scade niente.
		// È il verso giusto — un dato mancante non deve autorizzare una cancellazione.
		var (media, _) = await SeminaAsync(db, null, "cancelled");

		var selezionate = await NelPerimetroAsync(db, DateTime.UtcNow.AddMonths(-24));

		Assert.DoesNotContain(media[3], selezionate);
		Assert.DoesNotContain(media[4], selezionate);

		await tx.RollbackAsync();
	}

	[Fact]
	public async Task Una_foto_rimossa_logicamente_non_occupa_un_posto_protetto()
	{
		await using var db = await ApriAsync();
		if (db is null)
			return;

		await using var tx = await db.BeginTransactionAsync();
		var (media, _) = await SeminaAsync(db, DateTime.UtcNow.AddYears(-5), "cancelled");

		// Si cancella logicamente il collegamento della PRIMA foto. Le posizioni si ricalcolano, quindi
		// la quarta diventa terza e va protetta. ⚠️ Se il filtro su deleted_at stesse fuori dalla
		// finestra invece che dentro, basterebbe cancellare una foto per far scadere la successiva.
		await using (var cmd = db.CreateCommand())
		{
			cmd.CommandText = """
				UPDATE catalog.bathhouse_photos SET deleted_at = now()
				 WHERE media_asset_id = @m
				""";
			cmd.Parameters.AddWithValue("@m", media[0]);
			await cmd.ExecuteNonQueryAsync();
		}

		var selezionate = await NelPerimetroAsync(db, DateTime.UtcNow.AddMonths(-24));

		Assert.DoesNotContain(media[3], selezionate);   // ora e' la terza
		Assert.Contains(media[4], selezionate);         // ora e' la quarta

		await tx.RollbackAsync();
	}
}
