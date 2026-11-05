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

/// <summary>Impostazioni › Sconti e voucher. Uno sconto per prenotazione e il calcolo sono del motore di prezzo.</summary>
public sealed class DiscountsService
{
	private readonly IDiscountRepository _repository;
	private readonly ISectorRepository _sectors;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly DiscountQueryValidator _queryValidator;
	private readonly SaveDiscountRequestValidator _validator;
	private readonly BulkIdsRequestValidator _bulkValidator;

	public DiscountsService(
		IDiscountRepository repository, ISectorRepository sectors, IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user,
		CatalogAudit audit, DiscountQueryValidator queryValidator, SaveDiscountRequestValidator validator, BulkIdsRequestValidator bulkValidator)
	{
		_repository = repository;
		_sectors = sectors;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_queryValidator = queryValidator;
		_validator = validator;
		_bulkValidator = bulkValidator;
	}

	public async Task<PagedResult<DiscountDto>> ListAsync(DiscountQuery query, CancellationToken ct = default)
	{
		_queryValidator.EnsureValid(query);
		var propertyId = await _property.IdAsync(ct);
		var (items, total) = await _repository.ListAsync(new DiscountFilter(propertyId, string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
			query.SortBy, query.SortDir == SortDirections.Desc), query.Page, query.PageSize, ct);
		var sectors = await ContentMapping.SectorPublicIdsAsync(_sectors, propertyId, ct);
		return new PagedResult<DiscountDto>(total, query.Page, query.PageSize, items.Select(d => ToDto(d, sectors)).ToList());
	}

	public async Task<DiscountDto> GetAsync(Guid id, CancellationToken ct = default)
	{
		var discount = await LoadAsync(id, ct);
		return ToDto(discount, await ContentMapping.SectorPublicIdsAsync(_sectors, discount.PropertyId, ct));
	}

	public async Task<DiscountDto> CreateAsync(SaveDiscountRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var discount = Discount.Create(await _property.IdAsync(ct), _user.UserId);
		await ApplyAsync(discount, request, null, ct);
		_repository.Add(discount);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.DiscountCreated, discount, ct);
		return await GetAsync(discount.PublicId, ct);
	}

	public async Task<DiscountDto> UpdateAsync(Guid id, SaveDiscountRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var discount = await LoadAsync(id, ct);
		await ApplyAsync(discount, request, discount.Id, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.DiscountUpdated, discount, ct);
		return await GetAsync(discount.PublicId, ct);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var discount = await LoadAsync(id, ct);
		_repository.Remove(discount);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.DiscountDeleted, discount, ct);
	}

	public async Task<BulkResult> DeleteManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = await _repository.FindManyAsync(await _property.IdAsync(ct), ids, ct);
		foreach (var discount in found)
			_repository.Remove(discount);
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var discount in found)
			await RecordAsync(AuditActionCodes.Catalog.DiscountDeleted, discount, ct);
		var done = found.Select(d => d.PublicId).ToHashSet();
		return new BulkResult(ids.Where(done.Contains).ToList(), ids.Where(i => !done.Contains(i)).Select(i => new BulkFailure(i, CatalogErrors.DiscountNotFound, null)).ToList());
	}

	private async Task ApplyAsync(Discount discount, SaveDiscountRequest r, long? exceptId, CancellationToken ct)
	{
		if (await _repository.CodeExistsAsync(discount.PropertyId, r.Code.Trim(), exceptId, ct))
			throw DomainException.Conflict(CatalogErrors.DiscountCodeTaken, new Dictionary<string, object?> { ["code"] = r.Code.Trim() });
		Codes.TryParse<DiscountType>(r.Type, out var type);
		Codes.TryParse<DiscountScope>(r.Scope, out var scope);
		Codes.TryParse<DiscountApplicableChannel>(r.Channel, out var channel);
		if (r.MaxUses is not null && r.MaxUses < discount.UsedCount)
			throw DomainException.Unprocessable(CatalogErrors.DiscountMaxUsesBelowUsed, new Dictionary<string, object?> { ["usedCount"] = discount.UsedCount });
		discount.Update(new DiscountData(
			r.Code, ContentMapping.Clean(r.Name), r.RequiresCode, type, r.Value, scope,
			(r.ServiceCategories ?? []).Select(c => Codes.TryParse<DiscountServiceCategory>(c, out var v) ? v : default).ToList(), channel, r.MaxUses,
			r.IsActive, ContentMapping.Weekdays(r.Weekdays)));
		discount.ReplaceWindows((r.Periods ?? []).OrderBy(p => p.StartsOn).Select(p => (p.StartsOn, p.EndsOn)));
		discount.SetSectors(scope == DiscountScope.All ? [] : await ContentMapping.SectorIdsAsync(_sectors, discount.PropertyId, r.SectorIds, ct));
		discount.ReplaceTimeRanges((r.TimeRanges ?? []).OrderBy(t => t.From).Select(t => (t.From, t.To)));
	}

	private async Task<Discount> LoadAsync(Guid id, CancellationToken ct)
		=> await _repository.FindAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.DiscountNotFound);

	private static DiscountDto ToDto(Discount d, IReadOnlyDictionary<long, Guid> sectors) => new(
		d.PublicId, d.Code, d.Name, d.RequiresCode, Codes.Of(d.Type), d.Value, Codes.Of(d.Scope), ContentMapping.CodesOf(d.AppliesToServiceCategories),
		d.RestrictedSectors.Select(s => sectors.GetValueOrDefault(s.SectorId)).Where(g => g != Guid.Empty).ToList(), Codes.Of(d.ApplicableChannel),
		d.MaxUses, d.UsedCount, d.IsActive, d.ValidityWindows.OrderBy(w => w.StartsOn).Select(w => new DateRangeDto(w.StartsOn, w.EndsOn)).ToList(),
		ContentMapping.CodesOf(d.Weekdays), d.TimeRanges.OrderBy(t => t.From).Select(t => new DateRangeTimeDto(t.From, t.To)).ToList());

	private Task RecordAsync(string actionCode, Discount discount, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.Discount, discount.PublicId,
			new Dictionary<string, object?> { ["code"] = discount.Code, ["type"] = Codes.Of(discount.Type), ["value"] = discount.Value }, ct);
}
