using BuildingBlocks.Application.Validation;
using FluentValidation;
using Module.Identity.Application.Dtos;
using Shared.Contracts.Errors;
using Shared.Dtos;

namespace Module.Identity.Application.Validators;

public sealed class CustomerListQueryValidator : AbstractValidator<CustomerListQuery>
{
	public static readonly int[] PageSizes = [10, 20, 25, 50, 100, 200, 300];

	public CustomerListQueryValidator()
	{
		RuleFor(x => x.Search).MaximumLength(100);
		RuleFor(x => x.SortBy).Must(s => s is null || CustomerSortFields.All.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SaveCustomerRequestValidator : AbstractValidator<SaveCustomerRequest>
{
	public SaveCustomerRequestValidator()
	{
		RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
		RuleFor(x => x.PhoneCountryCode).PhonePrefix();
		RuleFor(x => x.Phone).PhoneNumber();
		RuleFor(x => x.PhoneSecondary).PhoneNumber();
		RuleFor(x => x.Email).OptionalEmail();
		RuleFor(x => x.FiscalCode).MaximumLength(16);
		RuleFor(x => x.VatNumber).MaximumLength(20);
		RuleFor(x => x.Address).MaximumLength(255);
		RuleFor(x => x.City).MaximumLength(120);
		RuleFor(x => x.PostalCode).MaximumLength(16);
		RuleFor(x => x.OtherNames).MaximumLength(255);
		RuleFor(x => x.DiscountNote).MaximumLength(255);
		RuleFor(x => x.Note).MaximumLength(255);
		RuleFor(x => x.BlacklistReason).MaximumLength(255);
	}
}
