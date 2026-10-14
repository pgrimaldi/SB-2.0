using BuildingBlocks.Infrastructure.Retention;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Prova l'interpretazione di <c>linked_asset</c>, la dichiarazione «questo file segue le righe».
/// </summary>
/// <remarks>
/// ⚠️ Metà dei test verifica che una dichiarazione <b>venga rifiutata</b>, e sono i più importanti: se
/// una dichiarazione malformata passasse come «nessun file collegato», il motore archivierebbe le
/// righe e lascerebbe il file sullo storage senza niente che possa più decidere quando cancellarlo —
/// cioè esattamente il difetto che questa colonna esiste per chiudere. Il motore infatti **fa
/// fallire** la policy, non la esegue ignorando il file.
/// </remarks>
public class LinkedAssetSpecTests
{
	[Fact]
	public void Una_dichiarazione_completa_si_legge()
	{
		var (spec, errore) = LinkedAssetSpec.Parse(
			"""{"column": "pdf_asset_id", "table": "media_assets", "action": "archive_object"}""");

		Assert.Null(errore);
		Assert.NotNull(spec);
		Assert.Equal("pdf_asset_id", spec.Column);
		Assert.Equal("media_assets", spec.Table);
		Assert.Equal("archive_object", spec.Action);
	}

	[Fact]
	public void Anche_delete_object_e_ammessa()
	{
		// Un file collegato può dover essere rimosso subito invece di raffreddato: la policy lo dice.
		var (spec, errore) = LinkedAssetSpec.Parse(
			"""{"column": "pdf_asset_id", "table": "media_assets", "action": "delete_object"}""");

		Assert.Null(errore);
		Assert.Equal("delete_object", spec!.Action);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Niente_da_leggere_e_un_errore_non_un_silenzio(string? json)
	{
		// ⚠️ Il motore chiama Parse solo quando la colonna NON è vuota; se la chiamasse comunque, un
		// risultato «valido e vuoto» gli farebbe saltare il file senza dirlo.
		var (spec, errore) = LinkedAssetSpec.Parse(json);
		Assert.Null(spec);
		Assert.NotNull(errore);
	}

	[Theory]
	[InlineData("non e' json")]
	[InlineData("[1, 2]")]
	[InlineData("\"stringa\"")]
	[InlineData("{}")]
	[InlineData("""{"column": "pdf_asset_id"}""")]
	[InlineData("""{"column": "pdf_asset_id", "table": "media_assets"}""")]
	[InlineData("""{"table": "media_assets", "action": "archive_object"}""")]
	public void Una_dichiarazione_incompleta_o_malformata_e_un_errore(string json)
	{
		var (spec, errore) = LinkedAssetSpec.Parse(json);
		Assert.Null(spec);
		Assert.NotNull(errore);
	}

	[Theory]
	// ⚠️ Nome di colonna e nome di tabella finiscono nell'SQL virgolettati ma NON parametrizzati —
	// parametro non possono essere. Devono quindi essere identificatori e nient'altro.
	[InlineData("pdf_asset_id\"; DROP TABLE fiskaly.receipts; --")]
	[InlineData("pdf asset id")]
	[InlineData("PdfAssetId")]
	[InlineData("1pdf")]
	[InlineData("")]
	public void Un_nome_di_colonna_che_non_e_un_identificatore_viene_rifiutato(string colonna)
	{
		var json = System.Text.Json.JsonSerializer.Serialize(
			new { column = colonna, table = "media_assets", action = "archive_object" });

		var (spec, errore) = LinkedAssetSpec.Parse(json);

		Assert.Null(spec);
		Assert.NotNull(errore);
	}

	[Theory]
	[InlineData("media_assets\"; DELETE FROM privacy.privacy_retention_policies; --")]
	[InlineData("altro schema.media_assets")]
	[InlineData("MediaAssets")]
	public void Un_nome_di_tabella_che_non_e_un_identificatore_viene_rifiutato(string tabella)
	{
		var json = System.Text.Json.JsonSerializer.Serialize(
			new { column = "pdf_asset_id", table = tabella, action = "archive_object" });

		var (spec, errore) = LinkedAssetSpec.Parse(json);

		Assert.Null(spec);
		Assert.NotNull(errore);
	}

	[Theory]
	// Delle sei azioni di retention solo due agiscono su un oggetto di storage. Le altre quattro
	// agiscono sulle RIGHE e qui non vorrebbero dire niente.
	[InlineData("delete_row")]
	[InlineData("null_fields")]
	[InlineData("anonymize")]
	[InlineData("archive_cold")]
	[InlineData("ARCHIVE_OBJECT")]
	[InlineData("qualunque")]
	public void Un_azione_che_non_agisce_su_un_oggetto_viene_rifiutata(string azione)
	{
		var json = System.Text.Json.JsonSerializer.Serialize(
			new { column = "pdf_asset_id", table = "media_assets", action = azione });

		var (spec, errore) = LinkedAssetSpec.Parse(json);

		Assert.Null(spec);
		Assert.NotNull(errore);
		Assert.Contains("action", errore);
	}

	[Fact]
	public void Il_nome_di_tabella_e_quello_canonico_non_quello_fisico()
	{
		// La tabella si dichiara nella forma canonica - `media_assets` - e il motore risolve il nome
		// fisico da sé, provando anche `<schema>_<tabella>`: in `privacy` e `switchboard` le tabelle
		// hanno il prefisso di modulo. Dichiarare il nome fisico funzionerebbe per caso su nove schemi
		// e si romperebbe sugli altri due.
		var (spec, errore) = LinkedAssetSpec.Parse(
			"""{"column": "pdf_asset_id", "table": "switchboard_media_assets", "action": "delete_object"}""");

		// È un identificatore valido, quindi passa: il controllo sull'esistenza lo fa il motore contro
		// il catalogo. Qui si verifica solo che il prefisso non sia confuso con uno schema.
		Assert.Null(errore);
		Assert.Equal("switchboard_media_assets", spec!.Table);
	}
}
