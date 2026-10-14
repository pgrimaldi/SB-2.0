using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using Module.Identity.Application.Abstractions;

namespace Module.Identity.Infrastructure.Security;

/// <summary>Parametri di hashing, sezione <c>Auth:PasswordHash</c>; il pepper arriva dal secret manager.</summary>
public sealed class PasswordHashOptions
{
	public const string Section = "Auth:PasswordHash";

	public int MemoryKiB { get; set; } = 19456;
	public int Iterations { get; set; } = 2;
	public int Parallelism { get; set; } = 1;
	/// <summary>Segreto server-side (base64) applicato a ogni hash: un leak del solo DB non è attaccabile offline.</summary>
	public string? Pepper { get; set; }
}

/// <summary>
/// Argon2id in formato PHC (<c>$argon2id$v=19$m=..,t=..,p=..$salt$hash</c>). Gli hash bcrypt si
/// verificano e vengono sostituiti al primo login riuscito, così come quelli con parametri sotto la policy.
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
	private const int SaltBytes = 16;
	private const int HashBytes = 32;

	private readonly PasswordHashOptions _options;
	private readonly byte[]? _pepper;

	public Argon2PasswordHasher(IOptions<PasswordHashOptions> options)
	{
		_options = options.Value;
		_pepper = string.IsNullOrEmpty(_options.Pepper) ? null : Convert.FromBase64String(_options.Pepper);
	}

	public string Hash(string password)
	{
		var salt = RandomNumberGenerator.GetBytes(SaltBytes);
		var hash = Compute(password, salt, _options.MemoryKiB, _options.Iterations, _options.Parallelism);
		return $"$argon2id$v=19$m={_options.MemoryKiB},t={_options.Iterations},p={_options.Parallelism}${B64(salt)}${B64(hash)}";
	}

	public PasswordCheck Verify(string hash, string password)
	{
		if (hash.StartsWith("$2", StringComparison.Ordinal))
			return BCrypt.Net.BCrypt.Verify(password, hash) ? PasswordCheck.SuccessRehashNeeded : PasswordCheck.Failed;

		if (!TryParse(hash, out var m, out var t, out var p, out var salt, out var expected))
			return PasswordCheck.Failed;

		var actual = Compute(password, salt, m, t, p);
		if (!CryptographicOperations.FixedTimeEquals(actual, expected))
			return PasswordCheck.Failed;

		return m < _options.MemoryKiB || t < _options.Iterations || p != _options.Parallelism
			? PasswordCheck.SuccessRehashNeeded
			: PasswordCheck.Success;
	}

	private byte[] Compute(string password, byte[] salt, int memoryKiB, int iterations, int parallelism)
	{
		using var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
		{
			Salt = salt,
			MemorySize = memoryKiB,
			Iterations = iterations,
			DegreeOfParallelism = parallelism,
			KnownSecret = _pepper,
		};
		return argon.GetBytes(HashBytes);
	}

	private static bool TryParse(string phc, out int m, out int t, out int p, out byte[] salt, out byte[] hash)
	{
		m = t = p = 0;
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
		return m > 0 && t > 0 && p > 0;
	}

	private static string B64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=');

	private static byte[] FromB64(string value) => Convert.FromBase64String(value.PadRight(value.Length + (4 - value.Length % 4) % 4, '='));
}
