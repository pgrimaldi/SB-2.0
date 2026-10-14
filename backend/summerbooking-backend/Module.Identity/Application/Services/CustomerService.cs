using BuildingBlocks.Application.Validation;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Validators;
using Module.Identity.Domain;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Utils;

namespace Module.Identity.Application.Services;

/// <summary>
/// Rubrica clienti dello stabilimento (voce Contatti). Nessun controllo dei duplicati alla creazione. Carte e prenotazioni del contatto si leggono da Payment e Booking.
/// </summary>
public sealed class CustomerService
{
	private readonly CurrentAccount _current;
	private readonly ICustomerRepository _customers;
	private readonly IUnitOfWork _uow;
	private readonly ICustomerExportWriter _export;
	private readonly IAuditTrail _audit;
	private readonly IDateTimeProvider _clock;
	private readonly CustomerListQueryValidator _queryValidator;
	private readonly SaveCustomerRequestValidator _saveValidator;

	public CustomerService(
		CurrentAccount current, ICustomerRepository customers, IUnitOfWork uow, ICustomerExportWriter export,
		IAuditTrail audit, IDateTimeProvider clock, CustomerListQueryValidator queryValidator, SaveCustomerRequestValidator saveValidator)
	{
		_current = current;
		_customers = customers;
		_uow = uow;
		_export = export;
		_audit = audit;
		_clock = clock;
		_queryValidator = queryValidator;
		_saveValidator = saveValidator;
	}

	public async Task<PagedResult<CustomerListItemDto>> ListAsync(CustomerListQuery query, CancellationToken ct = default)
	{
		var (items, total) = await _customers.ListAsync(ToQuery(query), ct);
		return new PagedResult<CustomerListItemDto>(total, query.Page, query.PageSize, items.Select(CustomerListItemDto.FromEntity).ToList());
	}

	/// <summary>Stessa lista con email e telefono mascherati, per chi non ha accesso ai contatti.</summary>
	public async Task<PagedResult<CustomerMaskedListItemDto>> ListMaskedAsync(CustomerListQuery query, CancellationToken ct = default)
	{
		var (items, total) = await _customers.ListAsync(ToQuery(query), ct);
		return new PagedResult<CustomerMaskedListItemDto>(total, query.Page, query.PageSize, items.Select(CustomerMaskedListItemDto.FromEntity).ToList());
	}

	public async Task<CustomerDetailDto> GetAsync(Guid customerId, CancellationToken ct = default)
		=> CustomerDetailDto.FromEntity(await LoadAsync(customerId, ct));

	public async Task<CustomerDetailDto> CreateAsync(SaveCustomerRequest request, CancellationToken ct = default)
	{
		_saveValidator.EnsureValid(request);
		var propertyId = _current.RequirePropertyId();
		var author = await _current.GetAsync(ct);

		var customer = Customer.Create(propertyId, request.ToDetails(), CustomerSource.ContactForm, author.Id, _clock.UtcNow);
		customer.SetBlacklist(request.IsBlacklisted, request.BlacklistReason?.Trim());
		_customers.Add(customer);
		await _uow.SaveChangesAsync(ct);

		await RecordAsync(IdentityAudit.CustomerCreated, customer, ct);
		return CustomerDetailDto.FromEntity(customer);
	}

	public async Task<CustomerDetailDto> UpdateAsync(Guid customerId, SaveCustomerRequest request, CancellationToken ct = default)
	{
		_saveValidator.EnsureValid(request);
		var customer = await LoadAsync(customerId, ct);
		customer.Update(request.ToDetails());
		customer.SetBlacklist(request.IsBlacklisted, request.BlacklistReason?.Trim());
		await _uow.SaveChangesAsync(ct);

		await RecordAsync(IdentityAudit.CustomerUpdated, customer, ct);
		return CustomerDetailDto.FromEntity(customer);
	}

	public async Task DeleteAsync(Guid customerId, CancellationToken ct = default)
	{
		var customer = await LoadAsync(customerId, ct);
		_customers.Remove(customer);
		await _uow.SaveChangesAsync(ct);
		await RecordAsync(IdentityAudit.CustomerDeleted, customer, ct);
	}

	public async Task<TabularFile> ExportAsync(CustomerListQuery query, TabularFormat format, CancellationToken ct = default)
	{
		var customers = await _customers.ListAllAsync(ToQuery(query), ct);
		var operatorLanguage = (await _current.GetAsync(ct)).Language;
		await _audit.RecordAsync(new AuditEntry(
			AuditCategory.Operator, IdentityAudit.Module, IdentityAudit.CustomersExported,
			Metadata: new Dictionary<string, object?> { ["rows"] = customers.Count, ["format"] = Codes.Of(format) }), ct);
		return _export.Write(customers, format, operatorLanguage);
	}

	private async Task<Customer> LoadAsync(Guid customerId, CancellationToken ct)
		=> await _customers.GetAsync(_current.RequirePropertyId(), customerId, ct)
			?? throw DomainException.NotFound(IdentityErrors.CustomerNotFound);

	private CustomerQuery ToQuery(CustomerListQuery query)
	{
		_queryValidator.EnsureValid(query);
		return new CustomerQuery(
			_current.RequirePropertyId(), string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
			query.BlacklistOnly, query.SortBy ?? CustomerSortFields.FullName, query.SortDir == SortDirections.Desc, query.Page, query.PageSize);
	}

	private Task RecordAsync(string action, Customer customer, CancellationToken ct)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Operator, IdentityAudit.Module, action, PropertyId: customer.PropertyId,
			TargetType: IdentityAudit.Targets.Customer, TargetId: customer.PublicId), ct);
}
