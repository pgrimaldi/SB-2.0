namespace Shared.Contracts.Identity;

public sealed record CustomerContact(string? FullName, string? Email, string? PhoneCountryCode, string? Phone);

/// <summary>Scheda breve di un cliente della rubrica, per elenchi e dettagli delle prenotazioni.</summary>
/// <summary>Un contatto di uno stabilimento che appartiene all'account del cliente.</summary>
public sealed record CustomerOfAccount(Guid PropertyId, Guid CustomerId);

public sealed record CustomerCard(Guid Id, string? FullName, string? Email, string? PhoneCountryCode, string? Phone, bool IsBlacklisted);

/// <summary>
/// Rubrica clienti per gli altri moduli (Booking, Payment): mai le entità, solo il <c>public_id</c>.
/// </summary>
public interface ICustomerDirectory
{
	/// <summary>Cliente con stessa email o telefono nello stabilimento, altrimenti uno nuovo.</summary>
	Task<Guid> FindOrCreateAsync(Guid propertyId, CustomerContact contact, CancellationToken ct = default);

	/// <summary>Sempre un cliente nuovo: prenotazione con il solo nome, senza scegliere un contatto esistente.</summary>
	Task<Guid> CreateAsync(Guid propertyId, CustomerContact contact, CancellationToken ct = default);

	/// <summary>Clienti dello stabilimento con questi id; quelli eliminati o di altri stabilimenti mancano.</summary>
	Task<IReadOnlyList<CustomerCard>> ListAsync(Guid propertyId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

	/// <summary>Id dei clienti il cui nome, email o telefono contiene il testo (al massimo <paramref name="max"/>).</summary>
	Task<IReadOnlyList<Guid>> SearchAsync(Guid propertyId, string text, int max, CancellationToken ct = default);

	/// <summary>Un contatto con stessa email o telefono è in blacklist (blocca la prenotazione).</summary>
	Task<bool> IsBlacklistedAsync(Guid propertyId, string? email, string? phone, CancellationToken ct = default);

	/// <summary>Esiste un contatto con email <b>e</b> telefono coincidenti (regola "solo contatti registrati").</summary>
	Task<bool> IsRegisteredContactAsync(Guid propertyId, string email, string phone, CancellationToken ct = default);

	/// <summary>
	/// Contatti di tutti gli stabilimenti che appartengono all'account del cliente: quelli collegati all'account e quelli con
	/// la sua email (l'account ha l'email verificata). Serve all'area cliente.
	/// </summary>
	Task<IReadOnlyList<CustomerOfAccount>> ListForAccountAsync(Guid accountId, CancellationToken ct = default);

	/// <summary>
	/// Contatto dello stabilimento per un cliente registrato che prenota online: quello già collegato o con la stessa email,
	/// altrimenti uno nuovo con i dati dell'account; resta collegato all'account.
	/// </summary>
	Task<Guid> FindOrCreateForAccountAsync(Guid propertyId, Guid accountId, CustomerContact contact, CancellationToken ct = default);
}
