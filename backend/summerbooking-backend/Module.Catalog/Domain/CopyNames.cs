using System.Globalization;
using System.Text.RegularExpressions;

namespace Module.Catalog.Domain;

/// <summary>
/// Nome di una copia: «Giornaliero» → «Giornaliero copia», poi «Giornaliero copia 2», «Giornaliero copia 3»… fino al primo
/// libero fra i nomi già usati (senza distinguere maiuscole). Copiando una copia si riparte dal nome base
/// («Giornaliero copia» → «Giornaliero copia 2», non «Giornaliero copia copia»). Se il nome è troppo lungo si accorcia
/// la parte base, così il suffisso resta sempre intero.
/// </summary>
public static partial class CopyNames
{
	public const string Suffix = " copia";

	public static string Next(string name, int maxLength, ICollection<string> taken)
	{
		var trimmed = name.Trim();
		var match = CopySuffix().Match(trimmed);
		var baseName = match.Success ? trimmed[..match.Index] : trimmed;
		for (var n = 1; ; n++)
		{
			var suffix = n == 1 ? Suffix : Suffix + " " + n.ToString(CultureInfo.InvariantCulture);
			var head = baseName.Length + suffix.Length > maxLength ? baseName[..Math.Max(0, maxLength - suffix.Length)].TrimEnd() : baseName;
			var candidate = head + suffix;
			if (!taken.Contains(candidate))
			{
				taken.Add(candidate);
				return candidate;
			}
		}
	}

	/// <summary>Insieme dei nomi già usati da passare a <see cref="Next"/>: confronto senza maiuscole.</summary>
	public static HashSet<string> Taken(IEnumerable<string> names) => new(names.Select(n => n.Trim()), StringComparer.OrdinalIgnoreCase);

	[GeneratedRegex(@" copia(?: \d{1,9})?$", RegexOptions.IgnoreCase)]
	private static partial Regex CopySuffix();
}
