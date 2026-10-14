using Module.Infra.Domain.Enums;
using Shared.Contracts;

namespace Module.Infra.Domain.Entities;

/// <summary>
/// Registro dei client applicativi partner che autenticano chiamate server-to-server verso l'API.
/// </summary>
public sealed class ApiClient : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public IntegrationApp AppName { get; private set; }
	public string ClientId { get; private set; } = null!;
	public string ClientSecretRef { get; private set; } = null!;
	public Guid? PropertyId { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime? ExpiresAt { get; private set; }
	public DateTime? RotatedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ApiClient() { }   // unico costruttore, per il materializzatore di EF Core
}
