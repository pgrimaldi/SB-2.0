using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Module.Identity.Application;
using Module.Identity.Application.Abstractions;

namespace Module.Identity.Infrastructure.Security;

public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
	public string NewOpaqueToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

	public string NewNumericCode(int digits)
	{
		var max = (int)Math.Pow(10, digits);
		return RandomNumberGenerator.GetInt32(0, max).ToString().PadLeft(digits, '0');
	}

	public string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

	public bool FixedTimeEquals(string hashA, string hashB)
		=> CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hashA), Encoding.ASCII.GetBytes(hashB));
}

/// <summary>
/// Tentativi falliti in memoria del processo. ⚠️ Con più istanze ognuna conta per sé: con lo scale-out
/// va spostato su Redis.
/// </summary>
public sealed class MemoryLoginThrottle : ILoginThrottle
{
	private readonly IMemoryCache _cache;
	private readonly IdentityAuthOptions _options;

	public MemoryLoginThrottle(IMemoryCache cache, IOptions<IdentityAuthOptions> options)
	{
		_cache = cache;
		_options = options.Value;
	}

	public bool IsLocked(string key) => _cache.TryGetValue(CacheKey(key), out Counter? c) && c!.Count >= _options.LockoutThreshold;

	public void RegisterFailure(string key)
	{
		var counter = _cache.GetOrCreate(CacheKey(key), entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.LockoutMinutes);
			return new Counter();
		})!;
		Interlocked.Increment(ref counter.Count);
	}

	public void Reset(string key) => _cache.Remove(CacheKey(key));

	private static string CacheKey(string key) => "identity:throttle:" + key;

	private sealed class Counter
	{
		public int Count;
	}
}

/// <summary>Codici monouso in memoria del processo (stesso limite di <see cref="MemoryLoginThrottle"/>).</summary>
public sealed class MemoryReplayGuard : IReplayGuard
{
	private readonly IMemoryCache _cache;
	private readonly object _lock = new();

	public MemoryReplayGuard(IMemoryCache cache) => _cache = cache;

	public bool TryConsume(string id, TimeSpan window)
	{
		lock (_lock)
		{
			var key = "identity:replay:" + id;
			if (_cache.TryGetValue(key, out _))
				return false;
			_cache.Set(key, true, window);
			return true;
		}
	}
}
