using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using Module.Identity.Application.Abstractions;

namespace Module.Identity.Infrastructure.Security;

/// <summary>Parametri di hashing, sezione <c>Auth:PasswordHash</c>; i pepper arrivano dal secret manager.</summary>
public sealed class PasswordHashOptions
{
	public const string Section = "Auth:PasswordHash";

	public int MemoryKiB { get; set; } = 19456;
	public int Iterations { get; set; } = 2;
	public int Parallelism { get; set; } = 1;

	/// <summary>
	/// Pepper "versione 0" (base64): quello degli hash scritti senza <c>k=</c>, cioè prima che i pepper
	/// avessero una versione. Se <see cref="Peppers"/> è vuoto è anche quello usato per gli hash nuovi.
	/// </summary>
	public string? Pepper { get; set; }

	/// <summary>
	/// Pepper con versione (base64). Gli hash nuovi usano quello con la versione più alta e la scrivono
	/// nell'hash (<c>k=N</c>); i precedenti restano per verificare gli hash già scritti, che vengono
	/// riscritti con il corrente al primo login riuscito. Un pepper si toglie solo quando nessun hash lo usa.
	/// </summary>
	public List<PepperKey> Peppers { get; set; } = [];

	/// <summary>
	/// Fuori da Development il backend non parte senza pepper. Solo per ambienti di prova che caricano
	/// dati con hash scritti senza pepper.
	/// </summary>
	public bool AllowNoPepper { get; set; }
}

public sealed class PepperKey
{
	public int Version { get; set; }
	public string Key { get; set; } = string.Empty;
}

/// <summary>
/// Argon2id in formato PHC (<c>$argon2id$v=19$m=..,t=..,p=..[,k=N]$salt$hash</c>, <c>k</c> = versione del
/// pepper, assente = versione 0). Gli hash bcrypt si verificano e vengono sostituiti al primo login riuscito,
/// così come quelli con parametri sotto la policy o con un pepper diverso dal corrente.
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
	private const int SaltBytes = 16;
	private const int HashBytes = 32;

	private readonly PasswordHashOptions _options;
	private readonly Dictionary<int, byte[]?> _peppers;
	private readonly int _currentVersion;

	public Argon2PasswordHasher(IOptions<PasswordHashOptions> options)
	{
		_options = options.Value;
		_peppers = new Dictionary<int, byte[]?>
		{
			[0] = string.IsNullOrEmpty(_options.Pepper) ? null : Convert.FromBase64String(_options.Pepper),
		};
		foreach (var pepper in _options.Peppers)
			_peppers[pepper.Version] = Convert.FromBase64String(pepper.Key);
		_currentVersion = _options.Peppers.Count == 0 ? 0 : _options.Peppers.Max(p => p.Version);
	}

	public string Hash(string password)
	{
		var salt = RandomNumberGenerator.GetBytes(SaltBytes);
		var hash = Compute(password, salt, _options.MemoryKiB, _options.Iterations, _options.Parallelism, _peppers[_currentVersion]);
		var version = _currentVersion == 0 ? string.Empty : $",k={_currentVersion}";
		return $"$argon2id$v=19$m={_options.MemoryKiB},t={_options.Iterations},p={_options.Parallelism}{version}${B64(salt)}${B64(hash)}";
	}

	public PasswordCheck Verify(string hash, string password)
	{
		if (hash.StartsWith("$2", StringComparison.Ordinal))
			return BCrypt.Net.BCrypt.Verify(password, hash) ? PasswordCheck.SuccessRehashNeeded : PasswordCheck.Failed;

		if (!TryParse(hash, out var m, out var t, out var p, out var k, out var salt, out var expected))
			return PasswordCheck.Failed;

		// Pepper non piu' configurato: l'hash non e' verificabile (serve il reset della password).
		if (!_peppers.TryGetValue(k, out var pepper))
			return PasswordCheck.Failed;

		var actual = Compute(password, salt, m, t, p, pepper);
		if (!CryptographicOperations.FixedTimeEquals(actual, expected))
			return PasswordCheck.Failed;

		return m < _options.MemoryKiB || t < _options.Iterations || p != _options.Parallelism || k != _currentVersion
			? PasswordCheck.SuccessRehashNeeded
			: PasswordCheck.Success;
	}

	private static byte[] Compute(string password, byte[] salt, int memoryKiB, int iterations, int parallelism, byte[]? pepper)
	{
		using var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
		{
			Salt = salt,
			MemorySize = memoryKiB,
			Iterations = iterations,
			DegreeOfParallelism = parallelism,
			KnownSecret = pepper,
		};
		return argon.GetBytes(HashBytes);
	}

	private static bool TryParse(string phc, out int m, out int t, out int p, out int k, out byte[] salt, out byte[] hash)
	{
		m = t = p = k = 0;
		salt = hash = [];
		var parts = phc.Split('$');
		if (parts.Length != 6 || parts[1] != "argon2id")
			return false;

		foreach (var pair in parts[3].Split(','))
		{
			var kv = pair.Split('=');
			if (kv.Length != 2 || !int.TryParse(kv[1], out var value))
				return false;
			switch (kv[0])
			{
				case "m": m = value; break;
				case "t": t = value; break;
				case "p": p = value; break;
				case "k": k = value; break;
			}
		}

		try
		{
			salt = FromB64(parts[4]);
			hash = FromB64(parts[5]);
		}
		catch (FormatException)
		{
			return false;
		}
		return m > 0 && t > 0 && p > 0 && k >= 0;
	}

	private static string B64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=');

	private static byte[] FromB64(string value) => Convert.FromBase64String(value.PadRight(value.Length + (4 - value.Length % 4) % 4, '='));
}
