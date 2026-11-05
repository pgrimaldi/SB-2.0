using Shared.Localization;

namespace Module.Catalog.Application.Services;

/// <summary>Scelta della traduzione di un contenuto di dominio: lingua della richiesta, poi italiano, poi inglese.</summary>
public static class Translations
{
	/// <param name="culture">Selettore della cultura della riga (es. <c>it-IT</c>, <c>it</c>).</param>
	public static string Pick<T>(IEnumerable<T> rows, Func<T, string> culture, Func<T, string> text, AppLanguage language, string fallback)
	{
		var list = rows.ToList();
		foreach (var wanted in new[] { language, AppLanguage.It, AppLanguage.En })
		{
			var code = SupportedLanguages.Code(wanted);
			var match = list.FirstOrDefault(r => culture(r).Split('-')[0].Equals(code, StringComparison.OrdinalIgnoreCase));
			if (match is not null)
				return text(match);
		}
		return fallback;
	}

	/// <summary>Contenuto con il testo principale in italiano e le altre lingue in righe figlie.</summary>
	public static string? Pick<T>(IEnumerable<T> rows, Func<T, AppLanguage> language, Func<T, string?> text, AppLanguage wanted, string? italian)
	{
		if (wanted == AppLanguage.It)
			return italian;
		var match = rows.FirstOrDefault(r => language(r) == wanted);
		return match is not null && !string.IsNullOrWhiteSpace(text(match)) ? text(match) : italian;
	}
}
