using Module.Identity.Domain.Enums;
using Shared.Contracts;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Rubrica dei clienti finali per singolo stabilimento (contatti da prenotazioni/transazioni di cassa).
/// </summary>
public sealed class Customer : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public long? UserId { get; private set; }
	public Account? User { get; private set; }
	public string? FullName { get; private set; }
	public string? PhoneCountryCode { get; private set; }
	public string? Phone { get; private set; }
	public string? PhoneSecondary { get; private set; }
	public string? Email { get; private set; }
	public string? FiscalCode { get; private set; }
	public string? VatNumber { get; private set; }
	public string? Address { get; private set; }
	public string? City { get; private set; }
	public string? PostalCode { get; private set; }
	public DateOnly? DateOfBirth { get; private set; }
	public string? OtherNames { get; private set; }
	public string? DiscountNote { get; private set; }
	public string? Note { get; private set; }
	public bool IsBlacklisted { get; private set; }
	public string? BlacklistReason { get; private set; }
	public CustomerSource SourceChannel { get; private set; }
	public long? CreatedByUserId { get; private set; }
	public Account? CreatedByUser { get; private set; }
	public DateTime? LastActivityAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private Customer() { }

	public static Customer Create(Guid propertyId, CustomerDetails details, CustomerSource source, long? createdByUserId, DateTime now)
	{
		if (propertyId == Guid.Empty)
			throw new ArgumentException("Stabilimento obbligatorio.", nameof(propertyId));

		var customer = new Customer
		{
			PublicId = Guid.CreateVersion7(),
			PropertyId = propertyId,
			SourceChannel = source,
			CreatedByUserId = createdByUserId,
			LastActivityAt = now,
		};
		customer.Update(details);
		return customer;
	}

	public void Update(CustomerDetails d)
	{
		FullName = d.FullName;
		PhoneCountryCode = d.PhoneCountryCode;
		Phone = d.Phone;
		PhoneSecondary = d.PhoneSecondary;
		Email = d.Email;
		FiscalCode = d.FiscalCode;
		VatNumber = d.VatNumber;
		Address = d.Address;
		City = d.City;
		PostalCode = d.PostalCode;
		DateOfBirth = d.DateOfBirth;
		OtherNames = d.OtherNames;
		DiscountNote = d.DiscountNote;
		Note = d.Note;
	}

	public void SetBlacklist(bool blacklisted, string? reason)
	{
		IsBlacklisted = blacklisted;
		BlacklistReason = blacklisted ? reason : null;
	}

	public void UnlinkUser() => UserId = null;
}

/// <summary>Campi anagrafici modificabili di un contatto.</summary>
public sealed record CustomerDetails(
	string? FullName,
	string? PhoneCountryCode,
	string? Phone,
	string? PhoneSecondary,
	string? Email,
	string? FiscalCode,
	string? VatNumber,
	string? Address,
	string? City,
	string? PostalCode,
	DateOnly? DateOfBirth,
	string? OtherNames,
	string? DiscountNote,
	string? Note);
