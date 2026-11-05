using System.Globalization;
using System.Text.RegularExpressions;

namespace Module.Catalog.Domain;

/// <summary>
/// Numeri delle postazioni inserite in successione: dal primo numero «A1» seguono A2, A3…; gli zeri iniziali
/// restano («A01» → A02); senza cifre finali si parte da 1 («A» → A1, A2…).
/// </summary>
public static partial class MapLabelSequence
{
	public static IReadOnlyList<string> From(string first, int count)
	{
		var label = first.Trim();
		if (count == 1)
			return [label];

		var match = TrailingNumber().Match(label);
		var prefix = match.Success ? match.Groups[1].Value : label;
		var start = match.Success ? long.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 1;
		var width = match.Success && match.Groups[2].Value.StartsWith('0') ? match.Groups[2].Value.Length : 0;

		return Enumerable.Range(0, count)
			.Select(i => prefix + (start + i).ToString(new string('0', Math.Max(width, 1)), CultureInfo.InvariantCulture))
			.ToList();
	}

	[GeneratedRegex(@"^(.*?)(\d{1,9})$")]
	private static partial Regex TrailingNumber();
}
