using System.Globalization;
using System.Text;

namespace Module.Catalog.Domain;

/// <summary>Slug della pagina pubblica: minuscole senza accenti, parole separate da trattini.</summary>
public static class Slugs
{
	public const int MaxLength = 100;

	public static string From(string text)
	{
		var builder = new StringBuilder(text.Length);
		var dash = false;
		foreach (var c in text.Normalize(NormalizationForm.FormD))
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
				continue;
			if (char.IsAsciiLetterOrDigit(c))
			{
				builder.Append(char.ToLowerInvariant(c));
				dash = false;
			}
			else if (!dash && builder.Length > 0)
			{
				builder.Append('-');
				dash = true;
			}
		}
		var slug = builder.ToString().Trim('-');
		if (slug.Length > MaxLength - 6)
			slug = slug[..(MaxLength - 6)].TrimEnd('-');
		return slug.Length == 0 ? "stabilimento" : slug;
	}
}
