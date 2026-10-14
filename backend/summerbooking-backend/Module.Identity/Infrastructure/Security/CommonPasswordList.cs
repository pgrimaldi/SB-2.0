using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Validators;

namespace Module.Identity.Infrastructure.Security;

/// <summary>
/// Password diffuse rifiutate alla scelta di una nuova password: l'elenco scaricato (file in
/// <see cref="CommonPasswordOptions.FilePath"/>) oppure, se manca, la copia incorporata nel modulo; in
/// entrambi i casi più le parole del contesto (Resources/context-passwords.txt). Confronto senza
/// distinzione di maiuscole; l'elenco si sostituisce a caldo dopo l'aggiornamento mensile.
/// </summary>
public sealed class CommonPasswordList : ICommonPasswordList
{
	private readonly HashSet<string> _context;
	private volatile HashSet<string> _passwords;

	public CommonPasswordList(IOptions<CommonPasswordOptions> options, ILogger<CommonPasswordList> logger)
	{
		_context = Normalize(ReadResource("context-passwords.txt"));

		var path = options.Value.FilePath;
		if (File.Exists(path))
		{
			_passwords = Normalize(File.ReadLines(path));
			logger.LogInformation("Password diffuse: {Count} voci dal file {Path}", _passwords.Count, path);
		}
		else
		{
			_passwords = Normalize(ReadResource("common-passwords.txt"));
			logger.LogInformation("Password diffuse: {Count} voci dalla copia incorporata ({Path} assente)", _passwords.Count, path);
		}
	}

	public int Count => _passwords.Count;

	public bool Contains(string password)
	{
		var value = password.Trim();
		return _passwords.Contains(value) || _context.Contains(value);
	}

	/// <summary>Sostituisce l'elenco scaricato (chiamato dall'aggiornamento mensile).</summary>
	public void Replace(HashSet<string> passwords) => _passwords = passwords;

	/// <summary>
	/// Una password per riga; righe vuote e commenti (#) ignorati. Restano solo le lunghezze ammesse dalla
	/// regola delle password: le altre sono già rifiutate dalla lunghezza.
	/// </summary>
	public static HashSet<string> Normalize(IEnumerable<string> lines)
	{
		var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var line in lines)
		{
			var value = line.Trim();
			if (value.Length >= IdentityRules.PasswordMinLength
				&& value.Length <= IdentityRules.PasswordMaxLength
				&& value[0] != '#')
				set.Add(value);
		}
		return set;
	}

	private static IEnumerable<string> ReadResource(string fileName)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName, StringComparison.Ordinal));
		using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
		while (reader.ReadLine() is { } line)
			yield return line;
	}
}
