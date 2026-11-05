using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Security;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Magazzino dello stabilimento (Impostazioni › Magazzino e pagina Magazzino).</summary>
public sealed class InventoryService
{
	private readonly IInventoryItemRepository _items;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly InventoryItemListQueryValidator _listValidator;
	private readonly SaveInventoryItemRequestValidator _saveValidator;
	private readonly BulkIdsRequestValidator _bulkValidator;

	public InventoryService(
		IInventoryItemRepository items, IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user, CatalogAudit audit,
		InventoryItemListQueryValidator listValidator, SaveInventoryItemRequestValidator saveValidator, BulkIdsRequestValidator bulkValidator)
	{
		_items = items;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_listValidator = listValidator;
		_saveValidator = saveValidator;
		_bulkValidator = bulkValidator;
	}

	public static IReadOnlyList<InventoryArticleDto> Articles()
		=> Enum.GetValues<InventoryArticle>().Select(a => new InventoryArticleDto(Codes.Of(a), InventoryArticles.IsMapPlaceable(a))).ToList();

	public async Task<PagedResult<InventoryItemDto>> ListAsync(InventoryItemListQuery query, CancellationToken ct = default)
	{
		_listValidator.EnsureValid(query);
		var filter = new InventoryItemFilter(
			await _property.IdAsync(ct), string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
			query.SortBy ?? InventorySortFields.Name, query.SortDir == SortDirections.Desc);
		var (items, total) = await _items.ListAsync(filter, query.Page, query.PageSize, ct);
		return new PagedResult<InventoryItemDto>(total, query.Page, query.PageSize, items.Select(InventoryItemDto.FromEntity).ToList());
	}

	public async Task<IReadOnlyList<InventoryItemLookupDto>> LookupAsync(CancellationToken ct = default)
		=> (await _items.ListAllAsync(await _property.IdAsync(ct), ct))
			.Select(i => new InventoryItemLookupDto(i.PublicId, i.Name, i.ArticleCode is { } a ? Codes.Of(a) : null))
			.ToList();

	public async Task<InventoryItemDto> GetAsync(Guid id, CancellationToken ct = default)
		=> InventoryItemDto.FromEntity(await LoadAsync(id, ct));

	public async Task<InventoryItemDto> CreateAsync(SaveInventoryItemRequest request, CancellationToken ct = default)
	{
		_saveValidator.EnsureValid(request);
		var propertyId = await _property.IdAsync(ct);
		await EnsureNameFreeAsync(propertyId, request.Name, null, ct);

		var item = InventoryItem.Create(propertyId, ToData(request), _user.UserId);
		_items.Add(item);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.InventoryItemCreated, item, ct);
		return InventoryItemDto.FromEntity(item);
	}

	public async Task<InventoryItemDto> UpdateAsync(Guid id, SaveInventoryItemRequest request, CancellationToken ct = default)
	{
		_saveValidator.EnsureValid(request);
		var item = await LoadAsync(id, ct);
		await EnsureNameFreeAsync(item.PropertyId, request.Name, item.Id, ct);

		item.Update(ToData(request));
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.InventoryItemUpdated, item, ct);
		return InventoryItemDto.FromEntity(item);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var item = await LoadAsync(id, ct);
		if (await InUseErrorAsync(item, ct) is { } inUse)
			throw inUse;

		_items.Remove(item);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.InventoryItemDeleted, item, ct);
	}

	/// <summary>Elimina gli articoli eliminabili; quelli usati nei listini o inesistenti tornano tra i falliti.</summary>
	public async Task<BulkResult> DeleteManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = (await _items.FindManyAsync(await _property.IdAsync(ct), ids, ct)).ToDictionary(i => i.PublicId);

		var deleted = new List<InventoryItem>();
		var failed = new List<BulkFailure>();
		foreach (var id in ids)
		{
			if (!found.TryGetValue(id, out var item))
				failed.Add(new BulkFailure(id, CatalogErrors.InventoryItemNotFound));
			else if (await InUseErrorAsync(item, ct) is { } inUse)
				failed.Add(new BulkFailure(id, inUse.Code, inUse.Args));
			else
			{
				_items.Remove(item);
				deleted.Add(item);
			}
		}

		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var item in deleted)
			await RecordAsync(AuditActionCodes.Catalog.InventoryItemDeleted, item, ct);
		return new BulkResult(deleted.Select(i => i.PublicId).ToList(), failed);
	}

	private async Task<InventoryItem> LoadAsync(Guid id, CancellationToken ct)
		=> await _items.FindAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.InventoryItemNotFound);

	private async Task EnsureNameFreeAsync(long propertyId, string name, long? exceptId, CancellationToken ct)
	{
		if (await _items.NameExistsAsync(propertyId, name.Trim(), exceptId, ct))
			throw DomainException.Conflict(CatalogErrors.InventoryNameTaken);
	}

	private async Task<DomainException?> InUseErrorAsync(InventoryItem item, CancellationToken ct)
	{
		var usages = await _items.ListUsagesAsync(item.Id, ct);
		if (usages.Count == 0)
			return null;
		return DomainException.Conflict(CatalogErrors.InventoryItemInUse, new Dictionary<string, object?>
		{
			["priceLists"] = usages.Where(u => u.AgreementPartnerName is null).Select(u => u.PriceListName).Distinct().ToList(),
			["agreementPriceLists"] = usages.Where(u => u.AgreementPartnerName is not null)
				.Select(u => $"{u.AgreementPartnerName} – {u.PriceListName}").Distinct().ToList(),
		});
	}

	private Task RecordAsync(string actionCode, InventoryItem item, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.InventoryItem, item.PublicId,
			new Dictionary<string, object?> { ["name"] = item.Name }, ct);

	private static InventoryItemData ToData(SaveInventoryItemRequest r) => new(
		r.ArticleCode is null ? null : Codes.TryParse<InventoryArticle>(r.ArticleCode, out var a) ? a : null,
		r.Name, r.TotalQuantity, r.LowStockThreshold, r.LowStockAlertEnabled);
}
