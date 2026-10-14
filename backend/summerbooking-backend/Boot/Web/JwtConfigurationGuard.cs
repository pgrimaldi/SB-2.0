using Shared.Security;

namespace Boot.Web;

/// <summary>
/// Controlli all'avvio sulla configurazione dei token: un token emesso in un ambiente non deve valere in un
/// altro. Fuori dallo sviluppo il backend non parte se emittente o chiavi coincidono con quelli di sviluppo
/// (che sono nel repository), se una chiave ha un kid "dev-" o se è più corta di 256 bit.
/// </summary>
public static class JwtConfigurationGuard
{
	private const int MinKeyBytes = 32;
	private const string DevelopmentSettingsFile = "appsettings.Development.json";

	public static void Validate(JwtOptions jwt, IHostEnvironment environment)
	{
		var errors = new List<string>();
		var keys = jwt.UserKeys.Select(k => ("Jwt:UserKeys", k)).Concat(jwt.ApiKeys.Select(k => ("Jwt:ApiKeys", k))).ToList();

		foreach (var (section, key) in keys)
		{
			if (TryDecode(key.Key) is not { } bytes)
				errors.Add($"{section} '{key.Kid}': chiave non in base64.");
			else if (bytes.Length < MinKeyBytes)
				errors.Add($"{section} '{key.Kid}': chiave di {bytes.Length * 8} bit, minimo {MinKeyBytes * 8}.");
		}

		if (!environment.IsDevelopment())
		{
			var development = ReadDevelopmentSettings(environment);
			var developmentKeys = development.UserKeys.Concat(development.ApiKeys).Select(k => k.Key).Where(k => k.Length > 0).ToHashSet();

			if (string.Equals(jwt.Issuer, development.Issuer, StringComparison.Ordinal) || jwt.Issuer.EndsWith("-dev", StringComparison.Ordinal))
				errors.Add($"Jwt:Issuer '{jwt.Issuer}' è quello dello sviluppo: ogni ambiente deve avere il proprio.");

			foreach (var (section, key) in keys)
			{
				if (key.Kid.StartsWith("dev-", StringComparison.OrdinalIgnoreCase))
					errors.Add($"{section} '{key.Kid}': kid di sviluppo in {environment.EnvironmentName}.");
				if (developmentKeys.Contains(key.Key))
					errors.Add($"{section} '{key.Kid}': è una chiave di sviluppo, pubblicata nel repository.");
			}
		}

		if (errors.Count > 0)
			throw new InvalidOperationException("Configurazione dei token non valida:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
	}

	private static byte[]? TryDecode(string value)
	{
		try
		{
			return Convert.FromBase64String(value);
		}
		catch (FormatException)
		{
			return null;
		}
	}

	/// <summary>Valori di sviluppo letti dal file, se pubblicato con l'applicazione; altrimenti nessun confronto.</summary>
	private static JwtOptions ReadDevelopmentSettings(IHostEnvironment environment)
		=> new ConfigurationBuilder()
			.SetBasePath(environment.ContentRootPath)
			.AddJsonFile(DevelopmentSettingsFile, optional: true)
			.Build()
			.GetSection(JwtOptions.Section)
			.Get<JwtOptions>() ?? new JwtOptions();
}
