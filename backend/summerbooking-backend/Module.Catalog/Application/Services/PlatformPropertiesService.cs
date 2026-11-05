using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;
using Shared.Contracts.Events;
using Shared.Dtos;
using Shared.Infrastructure;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>
/// Systemadmin: approvazione degli stabilimenti, cestino, regione e badge della vetrina. Gli effetti sugli altri moduli
/// (accesso al gestionale, prenotazioni da annullare o riattivare, email al titolare) partono dagli eventi.
/// </summary>
public sealed class PlatformPropertiesService
{
	private readonly IPlatformRepository _repository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IIntegrationEventPublisher _events;
	private readonly IDateTimeProvider _clock;
	private readonly CatalogAudit _audit;
	private readonly AdminPropertyQueryValidator _queryValidator;
	private readonly ChangePropertyStatusRequestValidator _statusValidator;
	private readonly SetPropertyBadgesRequestValidator _badgesValidator;

	public PlatformPropertiesService(
		IPlatformRepository repository, IUnitOfWork unitOfWork, IIntegrationEventPublisher events, IDateTimeProvider clock, CatalogAudit audit,
		AdminPropertyQueryValidator queryValidator, ChangePropertyStatusRequestValidator statusValidator, SetPropertyBadgesRequestValidator badgesValidator)
	{
		_repository = repository;
		_unitOfWork = unitOfWork;
		_events = events;
		_clock = clock;
		_audit = audit;
		_queryValidator = queryValidator;
		_statusValidator = statusValidator;
		_badgesValidator = badgesValidator;
	}

	public async Task<PagedResult<AdminPropertyDto>> ListAsync(AdminPropertyQuery query, CancellationToken ct = default)
	{
		_queryValidator.EnsureValid(query);
		PropertyStatus? status = query.Status is { } s && Codes.TryParse<PropertyStatus>(s, out var parsed) ? parsed : null;
		var (items, total) = await _repository.ListPropertiesAsync(
			new AdminPropertyFilter(status, string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(), query.Trashed), query.Page, query.PageSize, ct);
		var lookups = await LookupsAsync(ct);
		return new PagedResult<AdminPropertyDto>(total, query.Page, query.PageSize, items.Select(r => ToDto(r, lookups)).ToList());
	}

	public async Task<AdminPropertyDto> GetAsync(Guid id, CancellationToken ct = default) => ToDto(await LoadAsync(id, ct), await LookupsAsync(ct));

	/// <summary>Approva, rifiuta, rimette «in attesa» o nasconde; il motivo è facoltativo.</summary>
	public async Task<AdminPropertyDto> ChangeStatusAsync(Guid id, ChangePropertyStatusRequest request, CancellationToken ct = default)
	{
		_statusValidator.EnsureValid(request);
		var row = await LoadAsync(id, ct);
		var property = row.Property;
		if (property.DeletedAt is not null)
			throw DomainException.Conflict(CatalogErrors.PropertyInTrash);
		Codes.TryParse<PropertyStatus>(request.Status, out var status);
		var previous = property.Status;
		property.ChangeStatus(status, request.Comment);
		await _unitOfWork.SaveChangesAsync(ct);
		await _events.PublishAsync(new PropertyStatusChanged(
			Guid.CreateVersion7(), _clock.UtcNow, property.PublicId, Codes.Of(status), Codes.Of(previous), property.AdminComment), ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformPropertyStatusChanged, property,
			new Dictionary<string, object?> { ["status"] = Codes.Of(status), ["previousStatus"] = Codes.Of(previous) }, ct);
		return ToDto(row, await LookupsAsync(ct));
	}

	/// <summary>Sposta nel cestino: sparisce dal sito e dal gestionale.</summary>
	public async Task TrashAsync(Guid id, CancellationToken ct = default)
	{
		var property = (await LoadAsync(id, ct)).Property;
		if (property.DeletedAt is not null)
			throw DomainException.Conflict(CatalogErrors.PropertyInTrash);
		_repository.TrashProperty(property, _clock.UtcNow);
		await SaveTrashChangeAsync(property, "trashed", AuditActionCodes.Catalog.PlatformPropertyTrashed, ct);
	}

	public async Task<AdminPropertyDto> RestoreAsync(Guid id, CancellationToken ct = default)
	{
		var row = await LoadAsync(id, ct);
		if (row.Property.DeletedAt is null)
			throw DomainException.Conflict(CatalogErrors.PropertyNotInTrash);
		_repository.RestoreProperty(row.Property);
		await SaveTrashChangeAsync(row.Property, "restored", AuditActionCodes.Catalog.PlatformPropertyRestored, ct);
		return ToDto(row, await LookupsAsync(ct));
	}

	/// <summary>Eliminazione definitiva, solo dal cestino: lo stabilimento non si vede più nemmeno qui.</summary>
	public async Task PurgeAsync(Guid id, CancellationToken ct = default)
	{
		var property = (await LoadAsync(id, ct)).Property;
		if (property.DeletedAt is null)
			throw DomainException.Conflict(CatalogErrors.PropertyNotInTrash);
		property.Purge(_clock.UtcNow);
		await SaveTrashChangeAsync(property, "purged", AuditActionCodes.Catalog.PlatformPropertyPurged, ct);
	}

	public async Task<AdminPropertyDto> SetRegionAsync(Guid id, SetPropertyRegionRequest request, CancellationToken ct = default)
	{
		var row = await LoadAsync(id, ct);
		var bathhouse = row.Bathhouse ?? throw DomainException.NotFound(CatalogErrors.BathhouseNotFound);
		if (request.RegionId is { } regionId)
			bathhouse.AssignRegionManually((await _repository.FindRegionAsync(regionId, ct) ?? throw DomainException.NotFound(CatalogErrors.RegionNotFound)).Id);
		else
		{
			bathhouse.AssignRegionManually(null);
			bathhouse.AssignRegionAutomatically((await _repository.ListRegionsAsync(ct)).FirstOrDefault(r => r.Matches(bathhouse.Region))?.Id);
		}
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformPropertyShowcaseUpdated, row.Property,
			new Dictionary<string, object?> { ["region"] = request.RegionId }, ct);
		return ToDto(row, await LookupsAsync(ct));
	}

	public async Task<AdminPropertyDto> SetBadgesAsync(Guid id, SetPropertyBadgesRequest request, CancellationToken ct = default)
	{
		_badgesValidator.EnsureValid(request);
		var row = await LoadAsync(id, ct);
		var bathhouse = row.Bathhouse ?? throw DomainException.NotFound(CatalogErrors.BathhouseNotFound);
		var badges = (await _repository.ListBadgesAsync(ct)).ToDictionary(b => b.PublicId, b => b.Id);
		bathhouse.SetBadges(request.BadgeIds.Distinct()
			.Select(b => badges.TryGetValue(b, out var badgeId) ? badgeId : throw DomainException.NotFound(CatalogErrors.BadgeNotFound)).ToList());
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformPropertyShowcaseUpdated, row.Property,
			new Dictionary<string, object?> { ["badges"] = request.BadgeIds.Count }, ct);
		return ToDto(row, await LookupsAsync(ct));
	}

	private async Task SaveTrashChangeAsync(Property property, string change, string actionCode, CancellationToken ct)
	{
		await _unitOfWork.SaveChangesAsync(ct);
		await _events.PublishAsync(new PropertyTrashChanged(Guid.CreateVersion7(), _clock.UtcNow, property.PublicId, change), ct);
		await RecordAsync(actionCode, property, null, ct);
	}

	private async Task<AdminPropertyRow> LoadAsync(Guid id, CancellationToken ct)
		=> await _repository.FindPropertyAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.PropertyNotFound);

	private sealed record Lookups(IReadOnlyDictionary<long, Guid> Regions, IReadOnlyDictionary<long, Guid> Badges);

	private async Task<Lookups> LookupsAsync(CancellationToken ct)
		=> new((await _repository.ListRegionsAsync(ct)).ToDictionary(r => r.Id, r => r.PublicId),
			(await _repository.ListBadgesAsync(ct)).ToDictionary(b => b.Id, b => b.PublicId));

	private static AdminPropertyDto ToDto(AdminPropertyRow row, Lookups lookups)
	{
		var (p, b) = (row.Property, row.Bathhouse);
		return new AdminPropertyDto(
			p.PublicId, p.Name, Codes.Of(p.Status), p.AdminComment, p.City, p.ContactFullName, p.ContactEmail, p.ContactPhone, p.OwnerUserPublicId,
			p.IsOnlineSalesSuspended, p.IsSubscriptionActive, b?.PublicId, b?.Slug, b?.Region,
			b?.RegionId is { } r ? lookups.Regions.GetValueOrDefault(r) : null, b?.IsRegionManual ?? false,
			b?.Badges.Select(x => lookups.Badges.GetValueOrDefault(x.BadgeId)).Where(g => g != Guid.Empty).ToList() ?? [],
			b?.LikeCount ?? 0, p.CreatedAt, p.DeletedAt);
	}

	private Task RecordAsync(string actionCode, Property property, IReadOnlyDictionary<string, object?>? metadata, CancellationToken ct)
		=> _audit.RecordPlatformAsync(actionCode, AuditActionCodes.Catalog.Targets.Property, property.PublicId, property.PublicId, metadata, ct);
}
