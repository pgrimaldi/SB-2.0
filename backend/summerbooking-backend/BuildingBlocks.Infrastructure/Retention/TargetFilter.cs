using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>
/// Traduce il <c>target_filter</c> di una policy — il perimetro dichiarato, cioè <b>quali</b> righe
/// riguarda — in un frammento di <c>WHERE</c>.
/// </summary>
/// <remarks>
/// <para>
/// Sta in un file suo e non dentro <see cref="RetentionEngine"/> perché è l'unico pezzo del motore che
/// prende un dato scritto in una tabella e lo trasforma in SQL: è la parte che va letta per prima da
/// chi si chiede se una policy possa far danni, e merita i suoi test.
/// </para>
/// <para><b>Le forme ammesse</b>, tutte oggetti JSON:</para>
/// <code>
/// {"scope": "whole_table"}                                 -- tutte le righe, DICHIARATO
/// {"column": "kind", "op": "=", "value": "rendered_pdf"}
/// {"column": "kind", "op": "in", "values": ["a", "b"]}
/// {"column": "purged_at", "op": "is null"}
/// </code>
/// <para>
/// <c>{"scope": "whole_table"}</c> non è ridondante: trasforma «nessuno ha detto quali» in «qualcuno
/// ha detto tutte». Sulle azioni su storage è la differenza fra una policy che il motore esegue e una
/// che salta, perché su un file un perimetro indovinato non si annulla.
/// </para>
/// </remarks>
public static class TargetFilter
{
	/// <summary>Operatori ammessi, e come si scrivono in SQL. ⚠️ Elenco <b>chiuso</b>.</summary>
	private static readonly Dictionary<string, string> Ops = new(StringComparer.OrdinalIgnoreCase)
	{
		["="] = "=",
		["<>"] = "<>",
		["!="] = "<>",
		["in"] = "IN",
		["not in"] = "NOT IN",
		["is null"] = "IS NULL",
		["is not null"] = "IS NOT NULL",
	};

	/// <summary>
	/// I valori ammessi: token identificativi. ⚠️ Niente apici, niente barre rovesciate, niente
	/// spazi, niente punti e virgola — vedi la nota sull'iniezione in <see cref="Build"/>.
	/// </summary>
	private static readonly Regex SafeValue = new("^[A-Za-z0-9_.:-]{1,64}$", RegexOptions.Compiled);

	/// <summary>Gli operatori ammessi, per i messaggi di errore e per i test.</summary>
	public static IReadOnlyCollection<string> AllowedOps => Ops.Keys;

	/// <summary>
	/// Costruisce il frammento di <c>WHERE</c> corrispondente al perimetro, oppure dice perché non si
	/// può.
	/// </summary>
	/// <param name="json">Il contenuto di <c>target_filter</c>. NULL o vuoto = nessun perimetro.</param>
	/// <param name="columns">Le colonne che la tabella ha davvero, lette dal catalogo.</param>
	/// <param name="alias">L'alias della tabella nella query, o NULL se non ce n'è uno.</param>
	/// <param name="qualifiedTable">Schema e tabella, solo per i messaggi di errore.</param>
	/// <returns>
	/// <c>(" AND ...", null)</c> se il filtro è valido; <c>("", null)</c> se non c'è filtro o il
	/// perimetro è dichiaratamente l'intera tabella; <c>("", "motivo")</c> se è malformato — e in quel
	/// caso il motore fa <b>fallire</b> la policy, non la esegue senza il filtro.
	/// </returns>
	/// <remarks>
	/// <para>
	/// ⚠️ <b>Perché i valori finiscono nell'SQL come letterali e non come parametri, e perché questo
	/// non è un'iniezione.</b> Il predicato prodotto qui viene composto in sette punti diversi del
	/// motore — i conteggi, la <c>DELETE</c>, i due <c>UPDATE</c>, l'export dell'archivio,
	/// l'accodamento — ognuno con la propria lista di parametri. Passarlo come parametro avrebbe
	/// richiesto di propagare una lista attraverso tutti e sette, e <b>l'errore che conta davvero</b>
	/// è dimenticarsene in uno: quel punto avrebbe eseguito la policy <b>senza</b> il perimetro, cioè
	/// cancellando più del dovuto, e in silenzio.
	/// </para>
	/// <para>L'alternativa scelta rende l'iniezione impossibile per costruzione, su tre fronti:</para>
	/// <list type="number">
	/// <item>il <b>nome della colonna</b> è verificato contro il catalogo di PostgreSQL — se non
	/// esiste su quella tabella la policy fallisce — e viene virgolettato;</item>
	/// <item>l'<b>operatore</b> non arriva dai dati ma da <see cref="Ops"/>, un elenco chiuso di sei
	/// voci: qualunque altra cosa fa fallire la policy;</item>
	/// <item>i <b>valori</b> devono corrispondere a <c>^[A-Za-z0-9_.:-]{1,64}$</c>. Non c'è nessun
	/// apice, nessuna barra rovesciata, nessuno spazio e nessun punto e virgola che possa attraversare
	/// questo filtro: il carattere con cui si uscirebbe dal letterale è rifiutato prima.</item>
	/// </list>
	/// <para>
	/// È una restrizione reale — un valore con uno spazio qui non si esprime — e accettabile perché i
	/// perimetri da dichiarare sono etichette di enum e di <c>kind</c>, che sono token. Se un giorno
	/// servirà di più, si propagano i parametri nei sette punti: nel frattempo la policy fallisce con
	/// un messaggio chiaro, non gira senza perimetro.
	/// </para>
	/// <para>
	/// Il confronto è sempre <c>::text</c>: le colonne <c>kind</c> sono enum nativi dei moduli e su
	/// una connessione diversa da quella del modulo proprietario non sono mappate. È lo stesso motivo
	/// per cui la lettura delle policy proietta gli enum con <c>::text</c>.
	/// </para>
	/// </remarks>
	public static (string Clause, string? Error) Build(
		string? json, IReadOnlySet<string> columns, string? alias, string qualifiedTable)
	{
		if (string.IsNullOrWhiteSpace(json))
			return ("", null);

		JsonNode? node;
		try
		{
			node = JsonNode.Parse(json);
		}
		catch (JsonException ex)
		{
			return ("", $"target_filter non e' JSON valido: {ex.Message}");
		}

		if (node is not JsonObject obj)
			return ("", "target_filter deve essere un oggetto JSON");

		// Perimetro dichiarato come «tutte le righe»: nessuna condizione da aggiungere, ma non e' la
		// stessa cosa di non aver dichiarato niente. Il cancello sulle azioni su storage, nel motore,
		// conta esattamente su questa differenza.
		if (obj["scope"] is not null)
		{
			var scope = obj["scope"] as JsonValue;
			var testo = scope?.GetValue<string>();
			return testo == "whole_table"
				? ("", null)
				: ("", $"target_filter: scope '{testo}' non riconosciuto (l'unico ammesso e' 'whole_table')");
		}

		var column = (obj["column"] as JsonValue)?.GetValue<string>();
		var op = (obj["op"] as JsonValue)?.GetValue<string>();

		if (string.IsNullOrWhiteSpace(column) || string.IsNullOrWhiteSpace(op))
			return ("", "target_filter: servono 'column' e 'op'");

		if (!columns.Contains(column))
			return ("", $"target_filter: la colonna '{column}' non esiste su {qualifiedTable}");

		if (!Ops.TryGetValue(op, out var sqlOp))
			return ("", $"target_filter: operatore '{op}' non ammesso "
				+ $"(ammessi: {string.Join(", ", Ops.Keys)})");

		var prefix = string.IsNullOrEmpty(alias) ? "" : alias + ".";
		var quoted = $"{prefix}\"{column}\"";

		if (sqlOp is "IS NULL" or "IS NOT NULL")
			return ($" AND {quoted} {sqlOp}", null);

		var values = new List<string>();
		if (obj["values"] is JsonArray array)
		{
			foreach (var item in array)
			{
				if (item is not JsonValue v)
					return ("", "target_filter: 'values' deve contenere solo valori semplici");
				values.Add(v.ToString());
			}
		}
		else if (obj["value"] is JsonValue single)
		{
			values.Add(single.ToString());
		}

		if (values.Count == 0)
			return ("", $"target_filter: l'operatore '{op}' richiede 'value' o 'values'");

		foreach (var v in values)
		{
			if (!SafeValue.IsMatch(v))
				return ("", $"target_filter: il valore '{v}' non e' un token ammesso. Sono ammessi "
					+ "lettere, cifre e i caratteri _ . : - fino a 64 caratteri. Il motivo sta nella "
					+ "nota sull'iniezione di TargetFilter.Build: un valore piu' libero richiede di "
					+ "propagare i parametri nei sette punti che compongono il predicato");
		}

		if (sqlOp is "IN" or "NOT IN")
			return ($" AND {quoted}::text {sqlOp} ({string.Join(", ", values.Select(v => $"'{v}'"))})", null);

		if (values.Count > 1)
			return ("", $"target_filter: l'operatore '{op}' vuole un valore solo, non {values.Count}");

		return ($" AND {quoted}::text {sqlOp} '{values[0]}'", null);
	}
}
