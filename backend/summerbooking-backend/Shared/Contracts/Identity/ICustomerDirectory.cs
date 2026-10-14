namespace Shared.Contracts.Identity;

public sealed record CustomerContact(string? FullName, string? Email, string? PhoneCountryCode, string? Phone);

/// <summary>
/// Rubrica clienti per gli altri moduli (Booking, Payment): mai le entità, solo il <c>public_id</c>.
/// </summary>
public interface ICustomerDirectory
{
	/// <summary>Cliente con stessa email o telefono nello stabilimento, altrimenti uno nuovo.</summary>
	Task<Guid> FindOrCreateAsync(Guid propertyId, CustomerContact contact, CancellationToken ct = default);

	/// <summary>Un contatto con stessa email o telefono è in blacklist (blocca la prenotazione).</summary>
	Task<bool> IsBlacklistedAsync(Guid propertyId, string? email, string? phone, CancellationToken ct = default);

	/// <summary>Esiste un contatto con email <b>e</b> telefono coincidenti (regola "solo contatti registrati").</summary>
	Task<bool> IsRegisteredContactAsync(Guid propertyId, string email, string phone, CancellationToken ct = default);
}
