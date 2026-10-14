using Microsoft.Extensions.Configuration;
using Shared.Infrastructure;

namespace BuildingBlocks.Infrastructure.Secrets;

/// <summary>
/// Segreti dalla configurazione, sezione <c>Secrets</c> (chiave = riferimento). In AWS la sezione è
/// alimentata dal secret manager; in locale da user-secrets o appsettings.Local.json, mai committati.
/// </summary>
public sealed class ConfigurationSecretResolver : ISecretResolver
{
	private readonly IConfiguration _configuration;

	public ConfigurationSecretResolver(IConfiguration configuration) => _configuration = configuration;

	public string? Resolve(string secretRef) => _configuration.GetSection("Secrets")[secretRef];
}
