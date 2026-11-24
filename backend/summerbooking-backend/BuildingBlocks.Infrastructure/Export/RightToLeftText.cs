using System.Text;

namespace BuildingBlocks.Infrastructure.Export;

/// <summary>
/// Testo arabo pronto per un PDF che non compone le scritture complesse: ogni lettera prende la sua forma contestuale
/// (isolata, iniziale, mediana, finale, con le legature lam-alef) dal blocco «Arabic Presentation Forms-B», poi la riga
/// si mette in ordine visivo da destra a sinistra. Le parti latine e i numeri restano da sinistra a destra al loro posto.
/// Le stringhe senza arabo tornano invariate.
/// </summary>
internal static class RightToLeftText
{
	// forme: isolata, finale, iniziale, mediana (0 = la lettera non si unisce alla successiva)
	private static readonly Dictionary<char, (char Isolated, char Final, char Initial, char Medial)> Forms = new()
	{
		['\u0621'] = ('\uFE80', '\0', '\0', '\0'),
		['\u0622'] = ('\uFE81', '\uFE82', '\0', '\0'),
		['\u0623'] = ('\uFE83', '\uFE84', '\0', '\0'),
		['\u0624'] = ('\uFE85', '\uFE86', '\0', '\0'),
		['\u0625'] = ('\uFE87', '\uFE88', '\0', '\0'),
		['\u0626'] = ('\uFE89', '\uFE8A', '\uFE8B', '\uFE8C'),
		['\u0627'] = ('\uFE8D', '\uFE8E', '\0', '\0'),
		['\u0628'] = ('\uFE8F', '\uFE90', '\uFE91', '\uFE92'),
		['\u0629'] = ('\uFE93', '\uFE94', '\0', '\0'),
		['\u062A'] = ('\uFE95', '\uFE96', '\uFE97', '\uFE98'),
		['\u062B'] = ('\uFE99', '\uFE9A', '\uFE9B', '\uFE9C'),
		['\u062C'] = ('\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0'),
		['\u062D'] = ('\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4'),
		['\u062E'] = ('\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8'),
		['\u062F'] = ('\uFEA9', '\uFEAA', '\0', '\0'),
		['\u0630'] = ('\uFEAB', '\uFEAC', '\0', '\0'),
		['\u0631'] = ('\uFEAD', '\uFEAE', '\0', '\0'),
		['\u0632'] = ('\uFEAF', '\uFEB0', '\0', '\0'),
		['\u0633'] = ('\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4'),
		['\u0634'] = ('\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8'),
		['\u0635'] = ('\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC'),
		['\u0636'] = ('\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0'),
		['\u0637'] = ('\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4'),
		['\u0638'] = ('\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8'),
		['\u0639'] = ('\uFEC9', '\uFECA', '\uFECB', '\uFECC'),
		['\u063A'] = ('\uFECD', '\uFECE', '\uFECF', '\uFED0'),
		['\u0640'] = ('\u0640', '\u0640', '\u0640', '\u0640'),
		['\u0641'] = ('\uFED1', '\uFED2', '\uFED3', '\uFED4'),
		['\u0642'] = ('\uFED5', '\uFED6', '\uFED7', '\uFED8'),
		['\u0643'] = ('\uFED9', '\uFEDA', '\uFEDB', '\uFEDC'),
		['\u0644'] = ('\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0'),
		['\u0645'] = ('\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4'),
		['\u0646'] = ('\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8'),
		['\u0647'] = ('\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC'),
		['\u0648'] = ('\uFEED', '\uFEEE', '\0', '\0'),
		['\u0649'] = ('\uFEEF', '\uFEF0', '\0', '\0'),
		['\u064A'] = ('\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4'),
	};

	// lam seguita da alef: una legatura sola (isolata, finale)
	private static readonly Dictionary<char, (char Isolated, char Final)> LamAlef = new()
	{
		['\u0622'] = ('\uFEF5', '\uFEF6'),
		['\u0623'] = ('\uFEF7', '\uFEF8'),
		['\u0625'] = ('\uFEF9', '\uFEFA'),
		['\u0627'] = ('\uFEFB', '\uFEFC'),
	};

	public static bool HasArabic(string text) => text.Any(IsArabic);

	/// <summary>Forme contestuali e ordine visivo.</summary>
	/// <param name="rightToLeft">Riga da destra a sinistra (pagina araba); altrimenti solo i tratti arabi si girano, al loro posto.</param>
	public static string Visual(string text, bool rightToLeft) => HasArabic(text) ? Reorder(Shape(text), rightToLeft) : text;

	private static bool IsArabic(char c) => c is >= '\u0600' and <= '\u06FF' or >= '\uFB50' and <= '\uFDFF' or >= '\uFE70' and <= '\uFEFF';

	// segni diacritici: non interrompono l'unione delle lettere
	private static bool IsTransparent(char c) => c is >= '\u064B' and <= '\u065F' or '\u0670';

	private static bool JoinsNext(char c) => Forms.TryGetValue(c, out var f) && f.Initial != '\0';

	private static bool Joins(char c) => Forms.ContainsKey(c) && c != '\u0621';

	private static string Shape(string text)
	{
		var result = new StringBuilder(text.Length);
		for (var i = 0; i < text.Length; i++)
		{
			var c = text[i];
			if (!Forms.TryGetValue(c, out var forms))
			{
				result.Append(c);
				continue;
			}
			var prev = Neighbour(text, i, -1);
			var next = Neighbour(text, i, +1);
			var joinedBefore = prev is { } p && JoinsNext(p);
			if (c == '\u0644' && next is { } n && LamAlef.TryGetValue(n, out var ligature))
			{
				result.Append(joinedBefore ? ligature.Final : ligature.Isolated);
				i = Array.IndexOf(text.ToCharArray(), n, i + 1);
				continue;
			}
			var joinsAfter = forms.Initial != '\0' && next is { } x && Joins(x);
			result.Append((joinedBefore, joinsAfter) switch
			{
				(true, true) => forms.Medial,
				(true, false) => forms.Final == '\0' ? forms.Isolated : forms.Final,
				(false, true) => forms.Initial,
				_ => forms.Isolated,
			});
		}
		return result.ToString();
	}

	private static char? Neighbour(string text, int index, int step)
	{
		for (var i = index + step; i >= 0 && i < text.Length; i += step)
			if (!IsTransparent(text[i]))
				return text[i];
		return null;
	}

	/// <summary>
	/// Ordine visivo: i tratti arabi si leggono al contrario, quelli latini e numerici restano come sono; spazi e
	/// punteggiatura stanno col tratto che li precede (all'inizio, col primo). Con la riga da destra a sinistra anche i
	/// tratti si mettono in ordine inverso.
	/// </summary>
	private static string Reorder(string text, bool rightToLeft)
	{
		var runs = new List<(bool Rtl, StringBuilder Text)>();
		foreach (var c in text)
		{
			bool? rtl = IsArabic(c) ? true : char.IsLetterOrDigit(c) ? false : null;
			if (runs.Count == 0)
				runs.Add((rtl ?? true, new StringBuilder()));
			else if (rtl is { } r && r != runs[^1].Rtl)
				runs.Add((r, new StringBuilder()));
			runs[^1].Text.Append(c);
		}
		var visual = new StringBuilder(text.Length);
		if (!rightToLeft)
		{
			foreach (var (rtl, run) in runs)
				visual.Append(rtl ? ArabicRun(run.ToString()) : run.ToString());
			return visual.ToString();
		}
		for (var i = runs.Count - 1; i >= 0; i--)
		{
			var run = runs[i].Text.ToString();
			if (runs[i].Rtl)
				visual.Append(Mirror(run.Reverse()));
			else
			{
				// gli spazi in coda a un tratto latino vanno dalla parte araba (a sinistra nella riga girata)
				var trimmed = run.TrimEnd();
				visual.Append(run[trimmed.Length..]).Append(trimmed);
			}
		}
		return visual.ToString();
	}

	// tratto arabo in una riga da sinistra a destra: le lettere al contrario, gli spazi ai bordi restano dove sono
	private static string ArabicRun(string run)
	{
		var core = run.Trim();
		var lead = run[..(run.Length - run.TrimStart().Length)];
		var tail = run[(lead.Length + core.Length)..];
		return lead + Mirror(core.Reverse()) + tail;
	}

	// le parentesi in un tratto girato si specchiano
	private static string Mirror(IEnumerable<char> chars)
		=> new(chars.Select(c => c switch { '(' => ')', ')' => '(', '[' => ']', ']' => '[', '\u00AB' => '\u00BB', '\u00BB' => '\u00AB', _ => c }).ToArray());
}
