using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>
/// Il file che deve <b>seguire</b> le righe archiviate da una policy <c>archive_cold</c>: la colonna
/// che lo nomina, la tabella in cui sta, e che farne.
/// </summary>
/// <remarks>
/// <para>
/// Sta in un file suo per la stessa ragione di <see cref="TargetFilter"/>: è un dato scritto in una
/// tabella che diventa SQL, quindi va letto per primo da chi si chiede se una policy possa far danni.
/// </para>
/// <para>Forma:</para>
/// <code>
/// {"column": "pdf_asset_id", "table": "media_assets", "action": "archive_object"}
/// </code>
/// </remarks>
/// <param name="Column">La colonna della tabella archiviata che porta l'id dell'asset.</param>
/// <param name="Table">La tabella di asset, forma canonica, <b>nello stesso schema</b> della policy.</param>
/// <param name="Action">
/// <c>archive_object</c> (raffredda il file) oppure <c>delete_object</c> (lo rimuove).
/// </param>
public sealed record LinkedAssetSpec(string Column, string Table, string Action)
{
	/// <summary>Le sole azioni che hanno senso su un oggetto di storage.</summary>
	private static readonly string[] AzioniAmmesse = ["archive_object", "delete_object"];

	/// <summary>
	/// Identificatori ammessi. ⚠️ Nome di colonna e nome di tabella finiscono nell'SQL <b>virgolettati
	/// ma non parametrizzati</b> — parametro non possono essere — quindi devono essere identificatori e
	/// nient'altro. Entrambi vengono poi verificati contro il catalogo di PostgreSQL dal motore: se non
	/// esistono, la policy fallisce.
	/// </summary>
	private static readonly Regex Identificatore = new("^[a-z_][a-z0-9_]{0,62}$", RegexOptions.Compiled);

	/// <summary>
	/// Interpreta il JSON di <c>linked_asset</c>.
	/// </summary>
	/// <returns>
	/// <c>(spec, null)</c> se è valido; <c>(null, "motivo")</c> se è malformato — e in quel caso la
	/// policy <b>fallisce</b>, non viene eseguita ignorando il file collegato. Ignorarlo lascerebbe il
	/// file sullo storage senza niente che possa più decidere quando cancellarlo, che è esattamente il
	/// problema che questa colonna esiste per evitare.
	/// </returns>
	public static (LinkedAssetSpec? Spec, string? Error) Parse(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return (null, "vuoto");

		JsonNode? node;
		try
		{
			node = JsonNode.Parse(json);
		}
		catch (JsonException ex)
		{
			return (null, $"non e' JSON valido: {ex.Message}");
		}

		if (node is not JsonObject obj)
			return (null, "deve essere un oggetto JSON");

		var column = (obj["column"] as JsonValue)?.GetValue<string>();
		var table = (obj["table"] as JsonValue)?.GetValue<string>();
		var action = (obj["action"] as JsonValue)?.GetValue<string>();

		if (string.IsNullOrWhiteSpace(column) || string.IsNullOrWhiteSpace(table)
			|| string.IsNullOrWhiteSpace(action))
			return (null, "servono 'column', 'table' e 'action'");

		if (!Identificatore.IsMatch(column))
			return (null, $"'column' = '{column}' non e' un identificatore");

		if (!Identificatore.IsMatch(table))
			return (null, $"'table' = '{table}' non e' un identificatore");

		if (!AzioniAmmesse.Contains(action))
			return (null, $"'action' = '{action}' non ammessa (sono ammesse: "
				+ string.Join(", ", AzioniAmmesse) + ")");

		return (new LinkedAssetSpec(column, table, action), null);
	}
}
