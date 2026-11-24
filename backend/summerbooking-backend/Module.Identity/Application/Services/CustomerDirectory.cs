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
	private readonly IAccountRepository _accounts;
	private readonly IUnitOfWork _uow;
	private readonly IDateTimeProvider _clock;

	public CustomerDirectory(ICustomerRepository customers, IAccountRepository accounts, IUnitOfWork uow, IDateTimeProvider clock)
	{
		_customers = customers;
		_accounts = accounts;
		_uow = uow;
		_clock = clock;
	}

	public async Task<IReadOnlyList<CustomerOfAccount>> ListForAccountAsync(Guid accountId, CancellationToken ct = default)
	{
		if (await _accounts.GetByPublicIdAsync(accountId, ct) is not { } account)
			return [];
		var linked = await _customers.ListByUserAsync(account.Id, ct);
		var byEmail = await _customers.ListByEmailAsync(account.Email, ct);
		return linked.Concat(byEmail).DistinctBy(c => c.Id).Select(c => new CustomerOfAccount(c.PropertyId, c.PublicId)).ToList();
	}

	public async Task<Guid> FindOrCreateForAccountAsync(Guid propertyId, Guid accountId, CustomerContact contact, CancellationToken ct = default)
	{
		var account = await _accounts.GetByPublicIdAsync(accountId, ct) ?? throw new InvalidOperationException("Account inesistente.");
		var existing = (await _customers.ListByUserAsync(account.Id, ct)).FirstOrDefault(c => c.PropertyId == propertyId)
			?? (await _customers.FindByContactAsync(propertyId, account.Email, null, ct)).FirstOrDefault();
		if (existing is null)
		{
			var name = Clean(contact.FullName) ?? Clean($"{account.FirstName} {account.LastName}");
			existing = Customer.Create(propertyId,
				new CustomerDetails(name, Clean(contact.PhoneCountryCode) ?? account.PhoneCountryCode, Clean(contact.Phone) ?? account.Phone, null,
					account.Email, null, null, null, null, null, null, null, null, null),
				CustomerSource.Booking, null, _clock.UtcNow);
			_customers.Add(existing);
		}
		existing.LinkUser(account.Id);
		await _uow.SaveChangesAsync(ct);
		return existing.PublicId;
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

	public async Task<Guid> CreateAsync(Guid propertyId, CustomerContact contact, CancellationToken ct = default)
	{
		var customer = Customer.Create(propertyId,
			new CustomerDetails(Clean(contact.FullName), Clean(contact.PhoneCountryCode), Clean(contact.Phone), null, Clean(contact.Email),
				null, null, null, null, null, null, null, null, null),
			CustomerSource.Booking, null, _clock.UtcNow);
		_customers.Add(customer);
		await _uow.SaveChangesAsync(ct);
		return customer.PublicId;
	}

	public async Task<IReadOnlyList<CustomerCard>> ListAsync(Guid propertyId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
		=> (await _customers.ListByPublicIdsAsync(propertyId, ids, ct))
			.Select(c => new CustomerCard(c.PublicId, c.FullName, c.Email, c.PhoneCountryCode, c.Phone, c.IsBlacklisted))
			.ToList();

	public Task<IReadOnlyList<Guid>> SearchAsync(Guid propertyId, string text, int max, CancellationToken ct = default)
		=> string.IsNullOrWhiteSpace(text) ? Task.FromResult<IReadOnlyList<Guid>>([]) : _customers.SearchIdsAsync(propertyId, text.Trim(), max, ct);

	public async Task<bool> IsBlacklistedAsync(Guid propertyId, string? email, string? phone, CancellationToken ct = default)
		=> (await _customers.FindByContactAsync(propertyId, Clean(email), Clean(phone), ct)).Any(c => c.IsBlacklisted);

	public async Task<bool> IsRegisteredContactAsync(Guid propertyId, string email, string phone, CancellationToken ct = default)
		=> (await _customers.FindByContactAsync(propertyId, Clean(email), Clean(phone), ct))
			.Any(c => string.Equals(c.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) && c.Phone == phone.Trim());

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
