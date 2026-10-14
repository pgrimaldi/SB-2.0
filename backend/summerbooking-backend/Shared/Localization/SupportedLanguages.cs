using System.Globalization;

namespace Shared.Localization;

/// <summary>Codici ISO 639-1 delle lingue gestite e conversioni da e verso <see cref="AppLanguage"/>.</summary>
public static class SupportedLanguages
{
	public const AppLanguage Default = AppLanguage.It;

	public static IReadOnlyList<AppLanguage> All { get; } = Enum.GetValues<AppLanguage>();

	/// <summary>Codice usato nelle API, nei file .resx e come etichetta degli enum Postgres.</summary>
	public static string Code(AppLanguage language) => language.ToString().ToLowerInvariant();

	/// <summary>Solo i codici esatti (<c>it</c>, <c>en</c>, …), senza distinzione di maiuscole.</summary>
	public static bool TryParse(string? code, out AppLanguage language)
	{
		var value = code?.Trim();
		foreach (var candidate in All)
		{
			if (string.Equals(Code(candidate), value, StringComparison.OrdinalIgnoreCase))
			{
				language = candidate;
				return true;
			}
		}
		language = Default;
		return false;
	}

	/// <summary>Lingua del codice, oppure quella di default se il codice manca o non è gestito.</summary>
	public static AppLanguage ParseOrDefault(string? code) => TryParse(code, out var language) ? language : Default;

	public static CultureInfo Culture(AppLanguage language) => CultureInfo.GetCultureInfo(Code(language));

	public static bool IsRightToLeft(AppLanguage language) => language == AppLanguage.Ar;
}
