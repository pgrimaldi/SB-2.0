using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Systemadmin: badge, regioni e sezioni della vetrina («In evidenza», «Più frequentati»).</summary>
public sealed class PlatformShowcaseService
{
	private readonly IPlatformRepository _repository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IDateTimeProvider _clock;
	private readonly CatalogAudit _audit;
	private readonly SaveBadgeRequestValidator _badgeValidator;
	private readonly SaveRegionRequestValidator _regionValidator;
	private readonly SaveShowcaseRequestValidator _showcaseValidator;

	public PlatformShowcaseService(
		IPlatformRepository repository, IUnitOfWork unitOfWork, IDateTimeProvider clock, CatalogAudit audit, SaveBadgeRequestValidator badgeValidator,
		SaveRegionRequestValidator regionValidator, SaveShowcaseRequestValidator showcaseValidator)
	{
		_repository = repository;
		_unitOfWork = unitOfWork;
		_clock = clock;
		_audit = audit;
		_badgeValidator = badgeValidator;
		_regionValidator = regionValidator;
		_showcaseValidator = showcaseValidator;
	}

	public async Task<IReadOnlyList<BadgeDto>> ListBadgesAsync(CancellationToken ct = default) => (await _repository.ListBadgesAsync(ct)).Select(ToDto).ToList();

	public async Task<BadgeDto> CreateBadgeAsync(SaveBadgeRequest request, CancellationToken ct = default)
	{
		_badgeValidator.EnsureValid(request);
		var badge = ShowcaseBadge.Create();
		Apply(badge, request);
		_repository.AddBadge(badge);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformBadgeSaved, AuditActionCodes.Catalog.Targets.Badge, badge.PublicId, badge.Name, ct);
		return ToDto(badge);
	}

	public async Task<BadgeDto> UpdateBadgeAsync(Guid id, SaveBadgeRequest request, CancellationToken ct = default)
	{
		_badgeValidator.EnsureValid(request);
		var badge = await _repository.FindBadgeAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.BadgeNotFound);
		Apply(badge, request);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformBadgeSaved, AuditActionCodes.Catalog.Targets.Badge, badge.PublicId, badge.Name, ct);
		return ToDto(badge);
	}

	/// <summary>Il badge sparisce anche dalle strutture che lo avevano.</summary>
	public async Task DeleteBadgeAsync(Guid id, CancellationToken ct = default)
	{
		var badge = await _repository.FindBadgeAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.BadgeNotFound);
		_repository.RemoveBadge(badge);
		await _unitOfWork.SaveChangesAsync(ct);
		await _repository.RemoveBadgeAssignmentsAsync(badge.Id, _clock.UtcNow, ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformBadgeDeleted, AuditActionCodes.Catalog.Targets.Badge, badge.PublicId, badge.Name, ct);
	}

	public async Task<IReadOnlyList<RegionDto>> ListRegionsAsync(CancellationToken ct = default)
	{
		var countries = await _repository.CountryCodesAsync(ct);
		return (await _repository.ListRegionsAsync(ct)).Select(r => ToDto(r, countries)).ToList();
	}

	public async Task<RegionDto> CreateRegionAsync(SaveRegionRequest request, CancellationToken ct = default)
	{
		_regionValidator.EnsureValid(request);
		var region = ShowcaseRegion.Create();
		await ApplyAsync(region, request, null, ct);
		_repository.AddRegion(region);
		await _unitOfWork.SaveChangesAsync(ct);
		await ReassignAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformRegionSaved, AuditActionCodes.Catalog.Targets.Region, region.PublicId, region.Name, ct);
		return ToDto(region, await _repository.CountryCodesAsync(ct));
	}

	public async Task<RegionDto> UpdateRegionAsync(Guid id, SaveRegionRequest request, CancellationToken ct = default)
	{
		_regionValidator.EnsureValid(request);
		var region = await _repository.FindRegionAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.RegionNotFound);
		await ApplyAsync(region, request, region.Id, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await ReassignAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformRegionSaved, AuditActionCodes.Catalog.Targets.Region, region.PublicId, region.Name, ct);
		return ToDto(region, await _repository.CountryCodesAsync(ct));
	}

	/// <summary>Le strutture della regione restano senza (anche quelle assegnate a mano), poi si riassegnano dall'indirizzo.</summary>
	public async Task DeleteRegionAsync(Guid id, CancellationToken ct = default)
	{
		var region = await _repository.FindRegionAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.RegionNotFound);
		_repository.RemoveRegion(region);
		await _unitOfWork.SaveChangesAsync(ct);
		await _repository.ClearRegionAsync(region.Id, ct);
		await ReassignAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformRegionDeleted, AuditActionCodes.Catalog.Targets.Region, region.PublicId, region.Name, ct);
	}

	public async Task<IReadOnlyList<ShowcaseEntryDto>> ListShowcaseAsync(string section, CancellationToken ct = default)
	{
		var entries = await _repository.ListShowcaseAsync(ParseSection(section), ct);
		var properties = await _repository.FindPropertyPublicIdsAsync(entries.Select(e => e.Bathhouse!.PropertyId).ToList(), ct);
		return entries.Select(e => new ShowcaseEntryDto(properties.GetValueOrDefault(e.Bathhouse!.PropertyId), e.Bathhouse.PublicId, e.Bathhouse.Name, e.Bathhouse.City)).ToList();
	}

	/// <summary>Sostituisce l'elenco della sezione con gli stabilimenti indicati, in quell'ordine.</summary>
	public async Task<IReadOnlyList<ShowcaseEntryDto>> SaveShowcaseAsync(string section, SaveShowcaseRequest request, CancellationToken ct = default)
	{
		_showcaseValidator.EnsureValid(request);
		var code = ParseSection(section);
		var bathhouses = await _repository.FindBathhouseIdsByPropertyAsync(request.PropertyIds, ct);
		var wanted = request.PropertyIds.Select(p => bathhouses.TryGetValue(p, out var id) ? id : throw DomainException.NotFound(CatalogErrors.BathhouseNotFound)).ToList();
		var entries = await _repository.ListShowcaseAsync(code, ct);
		foreach (var removed in entries.Where(e => !wanted.Contains(e.BathhouseId)))
			_repository.RemoveShowcaseEntry(removed);
		for (short i = 0; i < wanted.Count; i++)
		{
			var current = entries.FirstOrDefault(e => e.BathhouseId == wanted[i]);
			if (current is null)
				_repository.AddShowcaseEntry(ShowcaseEntry.Create(code, wanted[i], i));
			else
				current.MoveTo(i);
		}
		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordPlatformAsync(AuditActionCodes.Catalog.PlatformShowcaseUpdated, AuditActionCodes.Catalog.Targets.Showcase,
			metadata: new Dictionary<string, object?> { ["section"] = Codes.Of(code), ["count"] = wanted.Count }, ct: ct);
		return await ListShowcaseAsync(section, ct);
	}

	/// <summary>Regione automatica di tutte le strutture non fissate a mano, dalla colonna region dell'indirizzo.</summary>
	private async Task ReassignAsync(CancellationToken ct)
	{
		var regions = await _repository.ListRegionsAsync(ct);
		foreach (var bathhouse in await _repository.ListAutomaticRegionBathhousesAsync(ct))
			bathhouse.AssignRegionAutomatically(regions.FirstOrDefault(r => r.Matches(bathhouse.Region))?.Id);
		await _unitOfWork.SaveChangesAsync(ct);
	}

	private async Task ApplyAsync(ShowcaseRegion region, SaveRegionRequest r, long? exceptId, CancellationToken ct)
	{
		short? countryId = r.CountryCode is { } code ? await _repository.FindCountryIdAsync(code, ct) ?? throw DomainException.NotFound(CatalogErrors.CountryNotFound) : null;
		if (await _repository.RegionNameExistsAsync(countryId, r.Name, exceptId, ct))
			throw DomainException.Conflict(CatalogErrors.RegionNameTaken);
		region.Update(countryId, r.Name, r.SortOrder);
		region.SetTranslations(Translated(r.Translations));
	}

	private static void Apply(ShowcaseBadge badge, SaveBadgeRequest r)
	{
		badge.Update(r.Name, r.Color, r.SortOrder);
		badge.SetTranslations(Translated(r.Translations));
	}

	private static Dictionary<AppLanguage, string> Translated(IReadOnlyList<NameTranslationDto>? items)
		=> (items ?? []).ToDictionary(t => SupportedLanguages.ParseOrDefault(t.Language), t => t.Name.Trim());

	private static ShowcaseSection ParseSection(string section)
		=> Codes.TryParse<ShowcaseSection>(section, out var code) ? code : throw DomainException.NotFound(CommonErrorCodes.NotFound);

	private static BadgeDto ToDto(ShowcaseBadge b) => new(b.PublicId, b.Name, b.Color, b.SortOrder, Names(b.Translations.Select(t => (t.Language, t.Name))));

	private static RegionDto ToDto(ShowcaseRegion r, IReadOnlyDictionary<short, string> countries)
		=> new(r.PublicId, r.CountryId is { } c ? countries.GetValueOrDefault(c) : null, r.Name, r.SortOrder, Names(r.Translations.Select(t => (t.Language, t.Name))));

	private static IReadOnlyList<NameTranslationDto> Names(IEnumerable<(AppLanguage Language, string Name)> rows)
		=> rows.OrderBy(t => t.Language).Select(t => new NameTranslationDto(SupportedLanguages.Code(t.Language), t.Name)).ToList();

	private Task RecordAsync(string actionCode, string target, Guid id, string name, CancellationToken ct)
		=> _audit.RecordPlatformAsync(actionCode, target, id, metadata: new Dictionary<string, object?> { ["name"] = name }, ct: ct);
}
