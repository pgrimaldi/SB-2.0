using FluentValidation;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Utils;

namespace Module.Catalog.Application.Validators;

public static class CatalogQueryRules
{
	public static readonly int[] PageSizes = [10, 20, 25, 50, 100, 200, 300];
	public const int SearchMaxLength = 100;
}

public sealed class InventoryItemListQueryValidator : AbstractValidator<InventoryItemListQuery>
{
	public InventoryItemListQueryValidator()
	{
		RuleFor(x => x.Search).MaximumLength(CatalogQueryRules.SearchMaxLength);
		RuleFor(x => x.SortBy).Must(s => s is null || InventorySortFields.All.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => CatalogQueryRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SaveInventoryItemRequestValidator : AbstractValidator<SaveInventoryItemRequest>
{
	public SaveInventoryItemRequestValidator()
	{
		RuleFor(x => x.ArticleCode).Must(c => c is null || Codes.TryParse<InventoryArticle>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Name).NotEmpty().MaximumLength(InventoryItem.NameMaxLength);
		RuleFor(x => x.TotalQuantity).GreaterThanOrEqualTo(0);
		RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(0).LessThanOrEqualTo(x => x.TotalQuantity).When(x => x.LowStockThreshold is not null);
	}
}

public sealed class BulkIdsRequestValidator : AbstractValidator<BulkIdsRequest>
{
	public BulkIdsRequestValidator()
	{
		RuleFor(x => x.Ids).NotEmpty().Must(ids => ids.Count <= BulkIdsRequest.MaxIds).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Ids).NotEmpty();
	}
}
