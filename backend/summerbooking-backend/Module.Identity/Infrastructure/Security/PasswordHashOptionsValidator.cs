using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Module.Identity.Infrastructure.Security;

/// <summary>
/// Controllo all'avvio della configurazione dei pepper. Sempre: chiavi in base64 e di almeno 256 bit,
/// versioni positive e senza doppioni. Fuori da Development: almeno un pepper, salvo
/// <see cref="PasswordHashOptions.AllowNoPepper"/>. Senza questo controllo un pepper mancante o con il
/// nome sbagliato lascerebbe partire il backend, che rifiuterebbe ogni password già convertita.
/// </summary>
public sealed class PasswordHashOptionsValidator(IHostEnvironment environment) : IValidateOptions<PasswordHashOptions>
{
	private const int MinKeyBytes = 32;

	public ValidateOptionsResult Validate(string? name, PasswordHashOptions options)
	{
		var errors = new List<string>();

		if (!string.IsNullOrEmpty(options.Pepper))
			CheckKey($"{PasswordHashOptions.Section}:Pepper", options.Pepper, errors);

		foreach (var group in options.Peppers.GroupBy(p => p.Version).Where(g => g.Count() > 1))
			errors.Add($"{PasswordHashOptions.Section}:Peppers: versione {group.Key} ripetuta.");
		foreach (var pepper in options.Peppers)
		{
			if (pepper.Version < 1)
				errors.Add($"{PasswordHashOptions.Section}:Peppers: versione {pepper.Version} non valida (da 1 in su; la 0 e' Pepper).");
			CheckKey($"{PasswordHashOptions.Section}:Peppers versione {pepper.Version}", pepper.Key, errors);
		}

		var hasPepper = !string.IsNullOrEmpty(options.Pepper) || options.Peppers.Count > 0;
		if (!hasPepper && !options.AllowNoPepper && !environment.IsDevelopment())
			errors.Add($"Nessun pepper in {environment.EnvironmentName}: configurare {PasswordHashOptions.Section}:Pepper " +
					   $"oppure {PasswordHashOptions.Section}:Peppers (o AllowNoPepper=true solo in un ambiente di prova).");

		return errors.Count == 0
			? ValidateOptionsResult.Success
			: ValidateOptionsResult.Fail(errors);
	}

	private static void CheckKey(string label, string value, List<string> errors)
	{
		try
		{
			var bytes = Convert.FromBase64String(value);
			if (bytes.Length < MinKeyBytes)
				errors.Add($"{label}: {bytes.Length * 8} bit, minimo {MinKeyBytes * 8}.");
		}
		catch (FormatException)
		{
			errors.Add($"{label}: non e' base64.");
		}
	}
}
