using Module.Identity.Application.Abstractions;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Contracts.Identity;
using Shared.Infrastructure;

namespace Module.Identity.Application.Services;

/// <summary>Implementazione in-process di <see cref="ICustomerDirectory"/> per Booking e Payment.</summary>
public sealed class CustomerDirectory : ICustomerDirectory
{
	private readonly ICustomerRepository _customers;
	private readonly IUnitOfWork _uow;
	private readonly IDateTimeProvider _clock;

	public CustomerDirectory(ICustomerRepository customers, IUnitOfWork uow, IDateTimeProvider clock)
	{
		_customers = customers;
		_uow = uow;
		_clock = clock;
	}

	public async Task<Guid> FindOrCreateAsync(Guid propertyId, CustomerContact contact, CancellationToken ct = default)
	{
		var email = Clean(contact.Email);
		var phone = Clean(contact.Phone);

		var existing = (await _customers.FindByContactAsync(propertyId, email, phone, ct)).FirstOrDefault();
		if (existing is not null)
			return existing.PublicId;

		var customer = Customer.Create(propertyId,
			new CustomerDetails(Clean(contact.FullName), Clean(contact.PhoneCountryCode), phone, null, email,
				null, null, null, null, null, null, null, null, null),
			CustomerSource.Booking, null, _clock.UtcNow);
		_customers.Add(customer);
		await _uow.SaveChangesAsync(ct);
		return customer.PublicId;
	}

	public async Task<bool> IsBlacklistedAsync(Guid propertyId, string? email, string? phone, CancellationToken ct = default)
		=> (await _customers.FindByContactAsync(propertyId, Clean(email), Clean(phone), ct)).Any(c => c.IsBlacklisted);

	public async Task<bool> IsRegisteredContactAsync(Guid propertyId, string email, string phone, CancellationToken ct = default)
		=> (await _customers.FindByContactAsync(propertyId, Clean(email), Clean(phone), ct))
			.Any(c => string.Equals(c.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) && c.Phone == phone.Trim());

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
