using BuildingBlocks.Infrastructure.Retention;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Test del perimetro dichiarato di una policy.
/// </summary>
/// <remarks>
/// Metà di questi test verifica che un filtro <b>venga rifiutato</b>, e sono i più importanti: un
/// filtro accettato per sbaglio produce un <c>WHERE</c> più largo di quello deciso, e su un file una
/// cancellazione di troppo non si annulla. L'altra metà verifica la forma esatta dell'SQL prodotto,
/// perché il cast <c>::text</c> non è un dettaglio: senza, il confronto su una colonna enum fallisce
/// quando la connessione è quella di un modulo diverso dal proprietario dello schema.
/// </remarks>
public class TargetFilterTests
{
	private static readonly IReadOnlySet<string> Colonne =
		new HashSet<string> { "id", "kind", "created_at", "purged_at", "path" };

	private static (string Clause, string? Error) Build(string? json, string? alias = "t")
		=> TargetFilter.Build(json, Colonne, alias, "\"notification\".\"media_assets\"");

	// --- nessun perimetro -----------------------------------------------------------------------

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Nessun_filtro_non_aggiunge_niente_e_non_e_un_errore(string? json)
	{
		var (clause, error) = Build(json);
		Assert.Equal("", clause);
		Assert.Null(error);
	}

	[Fact]
	public void Lo_scope_whole_table_non_aggiunge_condizioni_ma_non_e_un_errore()
	{
		// È il punto della colonna: «tutte le righe» DICHIARATO produce lo stesso SQL di «non
		// dichiarato», e sono comunque due cose diverse. La differenza la fa il motore, che esegue
		// l'azione su storage solo nel primo caso; qui conta che non sia un errore.
		var (clause, error) = Build("""{"scope": "whole_table"}""");
		Assert.Equal("", clause);
		Assert.Null(error);
	}

	[Fact]
	public void Uno_scope_inventato_e_un_errore_e_non_un_perimetro_vuoto()
	{
		// ⚠️ Il caso pericoloso: se uno scope sconosciuto passasse come "nessuna condizione", un
		// errore di battitura nel seed si tradurrebbe in una policy che agisce su tutta la tabella.
		var (clause, error) = Build("""{"scope": "tutta"}""");
		Assert.Equal("", clause);
		Assert.NotNull(error);
		Assert.Contains("whole_table", error);
	}

	// --- i filtri validi ------------------------------------------------------------------------

	[Fact]
	public void Un_confronto_semplice_diventa_un_AND_con_il_cast_a_text()
	{
		var (clause, error) = Build("""{"column": "kind", "op": "=", "value": "rendered_pdf"}""");
		Assert.Null(error);
		Assert.Equal(""" AND t."kind"::text = 'rendered_pdf'""", clause);
	}

	[Fact]
	public void Senza_alias_la_colonna_non_viene_qualificata()
	{
		// Il ramo delle azioni su riga compone il predicato senza alias: se il filtro ne mettesse uno
		// comunque, quelle query non compilerebbero piu'.
		var (clause, error) = Build("""{"column": "kind", "op": "=", "value": "pdf"}""", alias: null);
		Assert.Null(error);
		Assert.Equal(""" AND "kind"::text = 'pdf'""", clause);
	}

	[Fact]
	public void Un_elenco_diventa_un_IN()
	{
		var (clause, error) = Build(
			"""{"column": "kind", "op": "in", "values": ["fiscal_receipt_pdf", "non_fiscal_receipt_pdf"]}""");
		Assert.Null(error);
		Assert.Equal(""" AND t."kind"::text IN ('fiscal_receipt_pdf', 'non_fiscal_receipt_pdf')""", clause);
	}

	[Fact]
	public void Il_not_in_e_ammesso()
	{
		var (clause, error) = Build("""{"column": "kind", "op": "not in", "values": ["icon"]}""");
		Assert.Null(error);
		Assert.Equal(""" AND t."kind"::text NOT IN ('icon')""", clause);
	}

	[Fact]
	public void Is_null_non_porta_valori_e_non_casta()
	{
		// Il cast a text su un IS NULL non serve e cambierebbe il piano di un indice parziale.
		var (clause, error) = Build("""{"column": "purged_at", "op": "is null"}""");
		Assert.Null(error);
		Assert.Equal(""" AND t."purged_at" IS NULL""", clause);
	}

	[Theory]
	[InlineData("<>")]
	[InlineData("!=")]
	public void La_disuguaglianza_si_scrive_in_due_modi_e_produce_lo_stesso_SQL(string op)
	{
		var (clause, error) = Build($$"""{"column": "kind", "op": "{{op}}", "value": "icon"}""");
		Assert.Null(error);
		Assert.Equal(""" AND t."kind"::text <> 'icon'""", clause);
	}

	// --- i rifiuti ------------------------------------------------------------------------------

	[Fact]
	public void Una_colonna_che_non_esiste_e_un_errore()
	{
		// Senza questo controllo l'errore arriverebbe da PostgreSQL a meta' corsa, dopo che altre
		// policy hanno gia' scritto.
		var (_, error) = Build("""{"column": "tipo", "op": "=", "value": "pdf"}""");
		Assert.NotNull(error);
		Assert.Contains("'tipo'", error);
		Assert.Contains("media_assets", error);
	}

	[Fact]
	public void Un_operatore_fuori_elenco_e_un_errore()
	{
		var (_, error) = Build("""{"column": "kind", "op": "like", "value": "pdf"}""");
		Assert.NotNull(error);
		Assert.Contains("'like'", error);
	}

	[Theory]
	// ⚠️ Questi sono i tentativi che il filtro deve respingere. Il valore finisce dentro un letterale
	// SQL, e ciascuno di questi caratteri e' un modo di uscirne.
	[InlineData("pdf' OR '1'='1")]
	[InlineData("pdf'; DROP TABLE privacy.privacy_retention_policies; --")]
	[InlineData("pdf\\'")]
	[InlineData("pdf''")]
	[InlineData("un valore con spazi")]
	[InlineData("pdf\nkind")]
	public void Un_valore_che_non_e_un_token_viene_rifiutato(string valore)
	{
		var json = System.Text.Json.JsonSerializer.Serialize(
			new { column = "kind", op = "=", value = valore });

		var (clause, error) = Build(json);

		Assert.Equal("", clause);
		Assert.NotNull(error);
		Assert.Contains("token ammesso", error);
	}

	[Fact]
	public void Un_valore_piu_lungo_di_64_caratteri_viene_rifiutato()
	{
		var json = $$"""{"column": "kind", "op": "=", "value": "{{new string('a', 65)}}"}""";
		var (_, error) = Build(json);
		Assert.NotNull(error);
	}

	[Fact]
	public void Un_valore_di_64_caratteri_va_bene()
	{
		var json = $$"""{"column": "kind", "op": "=", "value": "{{new string('a', 64)}}"}""";
		var (clause, error) = Build(json);
		Assert.Null(error);
		Assert.Contains(new string('a', 64), clause);
	}

	[Fact]
	public void Un_operatore_di_confronto_senza_valore_e_un_errore()
	{
		var (_, error) = Build("""{"column": "kind", "op": "="}""");
		Assert.NotNull(error);
		Assert.Contains("'value' o 'values'", error);
	}

	[Fact]
	public void Un_uguale_con_piu_valori_e_un_errore()
	{
		// Accettarlo significherebbe scegliere noi quale dei due vale.
		var (_, error) = Build("""{"column": "kind", "op": "=", "values": ["a", "b"]}""");
		Assert.NotNull(error);
		Assert.Contains("un valore solo", error);
	}

	[Theory]
	[InlineData("non e' json")]
	[InlineData("[1, 2, 3]")]
	[InlineData("\"stringa\"")]
	[InlineData("{}")]
	[InlineData("""{"column": "kind"}""")]
	[InlineData("""{"op": "="}""")]
	public void Un_JSON_malformato_o_incompleto_e_un_errore(string json)
	{
		var (clause, error) = Build(json);
		Assert.Equal("", clause);
		Assert.NotNull(error);
	}

	[Fact]
	public void Un_valore_annidato_dentro_values_viene_rifiutato()
	{
		var (_, error) = Build("""{"column": "kind", "op": "in", "values": [{"x": 1}]}""");
		Assert.NotNull(error);
		Assert.Contains("valori semplici", error);
	}

	// --- i caratteri ammessi --------------------------------------------------------------------

	[Theory]
	[InlineData("rendered_pdf")]
	[InlineData("fiscal-receipt")]
	[InlineData("image/png")]      // la barra NON e' ammessa: resta fuori, vedi sotto
	[InlineData("v1.2.3")]
	[InlineData("ns:etichetta")]
	public void I_token_identificativi_passano_tranne_quelli_con_la_barra(string valore)
	{
		var json = System.Text.Json.JsonSerializer.Serialize(
			new { column = "kind", op = "=", value = valore });

		var (_, error) = Build(json);

		// `image/png` e' il caso che mostra il limite: un content type non si puo' dichiarare come
		// perimetro, perche' la barra non e' fra i caratteri ammessi. E' una restrizione voluta - meno
		// caratteri, meno modi di uscire dal letterale - e va saputa se un giorno servira' filtrare
		// per content type.
		if (valore.Contains('/'))
			Assert.NotNull(error);
		else
			Assert.Null(error);
	}
}
