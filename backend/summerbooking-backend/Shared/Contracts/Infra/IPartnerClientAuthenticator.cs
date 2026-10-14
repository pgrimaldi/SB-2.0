namespace Shared.Contracts.Infra;

/// <summary>Client M2M registrato in Infra (infra.integration_api_clients).</summary>
public sealed record PartnerClient(Guid PublicId, string ClientId, string App, Guid? PropertyId);

/// <summary>
/// Verifica delle credenziali dei partner esterni, implementata da Module.Infra. Identity la usa per
/// emettere il token <c>ApiBearer</c> senza possedere il registro.
/// </summary>
public interface IPartnerClientAuthenticator
{
	/// <summary>client_credentials: confronto a tempo costante del secret.</summary>
	Task<PartnerClient?> ValidateSecretAsync(string clientId, string clientSecret, CancellationToken ct = default);

	/// <summary>Firma HMAC-SHA256 esadecimale di <c>"{clientId}:{timestamp}"</c> (contratto <c>auth/signinApi</c>).</summary>
	Task<PartnerClient?> ValidateSignatureAsync(string clientId, string signatureHex, string timestamp, CancellationToken ct = default);
}
