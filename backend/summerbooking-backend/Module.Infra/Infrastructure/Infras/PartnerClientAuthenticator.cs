using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Module.Infra.Domain.Entities;
using Shared.Contracts.Infra;
using Shared.Infrastructure;

namespace Module.Infra.Infrastructure.Infras;

/// <summary>
/// Verifica delle credenziali dei client M2M sul registro di Infra. Client disattivati o scaduti non
/// passano (il legacy ignorava <c>expiresAt</c>); i confronti sono a tempo costante.
/// </summary>
public sealed class PartnerClientAuthenticator : IPartnerClientAuthenticator
{
	private readonly DbContext _db;
	private readonly ISecretResolver _secrets;
	private readonly IDateTimeProvider _clock;

	public PartnerClientAuthenticator(DbContext db, ISecretResolver secrets, IDateTimeProvider clock)
	{
		_db = db;
		_secrets = secrets;
		_clock = clock;
	}

	public async Task<PartnerClient?> ValidateSecretAsync(string clientId, string clientSecret, CancellationToken ct = default)
	{
		var (client, secret) = await FindAsync(clientId, ct);
		if (client is null || secret is null)
			return null;

		return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(clientSecret))
			? ToContract(client)
			: null;
	}

	public async Task<PartnerClient?> ValidateSignatureAsync(string clientId, string signatureHex, string timestamp, CancellationToken ct = default)
	{
		var (client, secret) = await FindAsync(clientId, ct);
		if (client is null || secret is null)
			return null;

		var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{clientId}:{timestamp}"));
		byte[] provided;
		try
		{
			provided = Convert.FromHexString(signatureHex);
		}
		catch (FormatException)
		{
			return null;
		}

		return CryptographicOperations.FixedTimeEquals(expected, provided) ? ToContract(client) : null;
	}

	private async Task<(ApiClient? Client, string? Secret)> FindAsync(string clientId, CancellationToken ct)
	{
		var now = _clock.UtcNow;
		var client = await _db.ApiClients.AsNoTracking()
			.FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsActive && (c.ExpiresAt == null || c.ExpiresAt > now), ct);
		return client is null ? (null, null) : (client, _secrets.Resolve(client.ClientSecretRef));
	}

	private static PartnerClient ToContract(ApiClient client)
		=> new(client.PublicId, client.ClientId, client.AppName.ToString(), client.PropertyId);
}
