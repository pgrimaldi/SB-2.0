using Module.Identity.Domain.Entities;
using Shared.Utils;

namespace Module.Identity.Application.Dtos;

/// <summary>Campi ammessi in <c>sortBy</c> della lista contatti.</summary>
public static class CustomerSortFields
{
	public const string FullName = "fullName";
	public const string Email = "email";
	public const string Phone = "phone";
	public const string CreatedAt = "createdAt";
	public const string LastActivityAt = "lastActivityAt";

	public static readonly string[] All = [FullName, Email, Phone, CreatedAt, LastActivityAt];
}

/// <param name="SortBy">Uno di <see cref="CustomerSortFields"/>.</param>
/// <param name="SortDir"><c>asc</c> o <c>desc</c>.</param>
/// <param name="PageSize">10, 20, 25, 50, 100, 200 o 300.</param>
public sealed record CustomerListQuery(string? Search = null, bool BlacklistOnly = false, string? SortBy = null, string? SortDir = null, int Page = 1, int PageSize = 10);

/// <summary>Riga della rubrica con i contatti in chiaro (permesso <c>customers.contacts</c>).</summary>
public sealed record CustomerListItemDto(
	Guid Id, string? FullName, string? Email, string? PhoneCountryCode, string? Phone,
	bool IsBlacklisted, DateTime CreatedAt, DateTime? LastActivityAt)
{
	public static CustomerListItemDto FromEntity(Customer c) => new(
		c.PublicId, c.FullName, c.Email, c.PhoneCountryCode, c.Phone, c.IsBlacklisted, c.CreatedAt, c.LastActivityAt);
}

/// <summary>Riga della rubrica con email e telefono mascherati (ruoli senza accesso ai contatti).</summary>
public sealed record CustomerMaskedListItemDto(
	Guid Id, string? FullName, string? EmailMasked, string? PhoneMasked,
	bool IsBlacklisted, DateTime CreatedAt, DateTime? LastActivityAt)
{
	public static CustomerMaskedListItemDto FromEntity(Customer c) => new(
		c.PublicId, c.FullName, Mask.Email(c.Email), Mask.Phone(c.Phone), c.IsBlacklisted, c.CreatedAt, c.LastActivityAt);
}

public sealed record CustomerDetailDto(
	Guid Id,
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
	string? Note,
	bool IsBlacklisted,
	string? BlacklistReason,
	string Source,
	bool HasAccount,
	DateTime CreatedAt,
	DateTime UpdatedAt,
	DateTime? LastActivityAt)
{
	public static CustomerDetailDto FromEntity(Customer c) => new(
		c.PublicId, c.FullName, c.PhoneCountryCode, c.Phone, c.PhoneSecondary, c.Email, c.FiscalCode,
		c.VatNumber, c.Address, c.City, c.PostalCode, c.DateOfBirth, c.OtherNames, c.DiscountNote, c.Note,
		c.IsBlacklisted, c.BlacklistReason, Codes.Of(c.SourceChannel), c.UserId is not null,
		c.CreatedAt, c.UpdatedAt, c.LastActivityAt);
}

public sealed record SaveCustomerRequest(
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
	string? Note,
	bool IsBlacklisted,
	string? BlacklistReason)
{
	public CustomerDetails ToDetails() => new(
		Clean(FullName), Clean(PhoneCountryCode), Clean(Phone), Clean(PhoneSecondary), Clean(Email), Clean(FiscalCode),
		Clean(VatNumber), Clean(Address), Clean(City), Clean(PostalCode), DateOfBirth, Clean(OtherNames),
		Clean(DiscountNote), Clean(Note));

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Mascheramento strutturale dei contatti.</summary>
public static class Mask
{
	public static string? Email(string? email)
	{
		if (string.IsNullOrEmpty(email))
			return email;
		var at = email.IndexOf('@');
		return at <= 1 ? "***" + email[Math.Max(at, 0)..] : email[0] + new string('*', at - 1) + email[at..];
	}

	public static string? Phone(string? phone)
	{
		if (string.IsNullOrEmpty(phone))
			return phone;
		return phone.Length <= 3 ? "***" : new string('*', phone.Length - 3) + phone[^3..];
	}
}
