using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Impostazioni › Opzioni prenotazioni online: postazioni, servizi, limiti di soggiorno e legenda della mappa online.</summary>
public sealed class OnlineSettingsService
{
	private readonly IOnlineSettingsRepository _repository;
	private readonly ISectorRepository _sectors;
	private readonly IMapRepository _map;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly CatalogAudit _audit;
	private readonly CatalogMedia _media;
	private readonly SaveOnlineSettingsRequestValidator _validator;
	private readonly SaveServiceOnlineSettingsRequestValidator _serviceValidator;
	private readonly SaveStayLimitRequestValidator _limitValidator;
	private readonly SaveLegendRequestValidator _legendValidator;

	public OnlineSettingsService(
		IOnlineSettingsRepository repository, ISectorRepository sectors, IMapRepository map, IUnitOfWork unitOfWork, CurrentProperty property,
		CatalogAudit audit, CatalogMedia media, SaveOnlineSettingsRequestValidator validator, SaveServiceOnlineSettingsRequestValidator serviceValidator,
		SaveStayLimitRequestValidator limitValidator, SaveLegendRequestValidator legendValidator)
	{
		_repository = repository;
		_sectors = sectors;
		_map = map;
		_unitOfWork = unitOfWork;
		_property = property;
		_audit = audit;
		_media = media;
		_validator = validator;
		_serviceValidator = serviceValidator;
		_limitValidator = limitValidator;
		_legendValidator = legendValidator;
	}

	public async Task<OnlineSettingsDto> GetAsync(CancellationToken ct = default)
	{
		var propertyId = await _property.IdAsync(ct);
		return await ToDtoAsync(propertyId, await _repository.FindAsync(propertyId, ct), ct);
	}

	public async Task<OnlineSettingsDto> SaveAsync(SaveOnlineSettingsRequest r, CancellationToken ct = default)
	{
		_validator.EnsureValid(r);
		var propertyId = await _property.IdAsync(ct);
		var settings = await SettingsAsync(propertyId, ct);

		var spotPublicIds = (r.AllowedSpotIds ?? []).Distinct().ToList();
		var spots = await _repository.FindSpotIdsAsync(propertyId, spotPublicIds, ct);
		var missing = spotPublicIds.Where(id => !spots.ContainsKey(id)).ToList();
		if (missing.Count > 0)
			throw DomainException.Unprocessable(CatalogErrors.MapSpotNotFound, new Dictionary<string, object?> { ["ids"] = missing });

		Codes.TryParse<SurchargeType>(r.SurchargeType, out var surcharge);
		Codes.TryParse<OnlineBookingScope>(r.Scope, out var scope);
		settings.Update(new OnlineBookingSettingsData(
			r.IsEnabled, r.IsMapPick, r.IsLocationRestricted, r.IsPeriodRestricted, surcharge, r.SurchargeValue, r.IsBlocked,
			r.RequiresManualConfirmation, r.AllowsSameDayBooking, r.AllowsHalfDay, r.MorningFrom, r.MorningTo, r.AfternoonFrom, r.AfternoonTo,
			r.AfternoonConversionTime, r.AllowsSeasonal, r.AllowsSharing, r.MaxSpotsPerBooking, r.MaxBookingsPerDay, r.BookingHorizonDays, scope,
			PaymentMethods(r.PaymentMethods), Clean(r.UsefulInfo), Clean(r.GoodToKnow)));
		settings.ReplaceLocations(spots.Values.ToList());

		settings.ClearPeriods();
		await _unitOfWork.SaveChangesAsync(ct);
		settings.AddPeriods((r.AllowedPeriods ?? []).OrderBy(p => p.StartsOn).Select(p => (p.StartsOn, p.EndsOn)));
		await _unitOfWork.SaveChangesAsync(ct);

		await RecordAsync(AuditActionCodes.Catalog.OnlineSettingsUpdated, new Dictionary<string, object?>
		{
			["isEnabled"] = r.IsEnabled,
			["isBlocked"] = r.IsBlocked,
			["paymentMethods"] = r.PaymentMethods,
		}, ct);
		return await ToDtoAsync(propertyId, settings, ct);
	}

	public async Task<ServiceOnlineSettingsDto> GetServicesAsync(CancellationToken ct = default)
		=> ToDto(await _repository.FindServiceSettingsAsync(await _property.IdAsync(ct), ct));

	public async Task<ServiceOnlineSettingsDto> SaveServicesAsync(SaveServiceOnlineSettingsRequest r, CancellationToken ct = default)
	{
		_serviceValidator.EnsureValid(r);
		var propertyId = await _property.IdAsync(ct);
		var settings = await _repository.FindServiceSettingsAsync(propertyId, ct);
		if (settings is null)
		{
			settings = ServiceBookingSettings.Create(propertyId);
			_repository.AddServiceSettings(settings);
		}
		Codes.TryParse<SurchargeType>(r.SurchargeType, out var surcharge);
		settings.Update(new ServiceBookingSettingsData(
			r.IsEnabled, surcharge, r.SurchargeValue, r.RequiresManualConfirmation, Clean(r.SectionTitle), Clean(r.UsefulInfo), PaymentMethods(r.PaymentMethods)));
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ServiceOnlineSettingsUpdated, new Dictionary<string, object?> { ["isEnabled"] = r.IsEnabled }, ct);
		return ToDto(settings);
	}

	public async Task<IReadOnlyList<StayLimitDto>> ListStayLimitsAsync(CancellationToken ct = default)
	{
		var propertyId = await _property.IdAsync(ct);
		var settings = await _repository.FindAsync(propertyId, ct);
		return await ToDtoAsync(propertyId, settings?.StayLimits ?? [], ct);
	}

	public async Task<StayLimitDto> CreateStayLimitAsync(SaveStayLimitRequest r, CancellationToken ct = default)
	{
		_limitValidator.EnsureValid(r);
		var propertyId = await _property.IdAsync(ct);
		var settings = await SettingsAsync(propertyId, ct);
		if (settings.Id == 0)
			await _unitOfWork.SaveChangesAsync(ct);

		var sector = await SectorAsync(propertyId, r.SectorId, ct);
		EnsureNoOverlap(settings, sector.Id, r, null);
		var limit = OnlineStayLimit.Create(settings.Id, sector.Id, r.StartsOn, r.EndsOn, r.MinDays, r.MaxDays);
		settings.StayLimits.Add(limit);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordLimitAsync(limit, sector, ct);
		return new StayLimitDto(limit.PublicId, sector.PublicId, limit.StartsOn, limit.EndsOn, limit.MinDays, limit.MaxDays);
	}

	public async Task<StayLimitDto> UpdateStayLimitAsync(Guid id, SaveStayLimitRequest r, CancellationToken ct = default)
	{
		_limitValidator.EnsureValid(r);
		var propertyId = await _property.IdAsync(ct);
		var settings = await _repository.FindAsync(propertyId, ct);
		var limit = settings?.StayLimits.FirstOrDefault(l => l.PublicId == id) ?? throw DomainException.NotFound(CatalogErrors.StayLimitNotFound);

		var sector = await SectorAsync(propertyId, r.SectorId, ct);
		EnsureNoOverlap(settings!, sector.Id, r, limit.Id);
		limit.Update(sector.Id, r.StartsOn, r.EndsOn, r.MinDays, r.MaxDays);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordLimitAsync(limit, sector, ct);
		return new StayLimitDto(limit.PublicId, sector.PublicId, limit.StartsOn, limit.EndsOn, limit.MinDays, limit.MaxDays);
	}

	public async Task DeleteStayLimitAsync(Guid id, CancellationToken ct = default)
	{
		var settings = await _repository.FindAsync(await _property.IdAsync(ct), ct);
		var limit = settings?.StayLimits.FirstOrDefault(l => l.PublicId == id) ?? throw DomainException.NotFound(CatalogErrors.StayLimitNotFound);
		settings!.StayLimits.Remove(limit);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.StayLimitsUpdated, new Dictionary<string, object?> { ["deleted"] = id }, ct);
	}

	public async Task<IReadOnlyList<LegendItemDto>> GetLegendAsync(CancellationToken ct = default)
		=> await GetLegendAsync(await _property.IdAsync(ct), ct);

	/// <summary>Legenda della mappa online di uno stabilimento (anche per il sito pubblico).</summary>
	public async Task<IReadOnlyList<LegendItemDto>> GetLegendAsync(long propertyId, CancellationToken ct = default)
		=> await ToDtoAsync(await _repository.ListLegendAsync(propertyId, ct), ct);

	/// <summary>Legenda della mappa online: sostituita per intero, nell'ordine ricevuto.</summary>
	public async Task<IReadOnlyList<LegendItemDto>> SaveLegendAsync(SaveLegendRequest r, CancellationToken ct = default)
	{
		_legendValidator.EnsureValid(r);
		var propertyId = await _property.IdAsync(ct);
		var items = new List<MapLegendItem>();
		for (var i = 0; i < r.Items.Count; i++)
		{
			long? assetId = null;
			if (r.Items[i].IconId is { } iconId)
				assetId = (await _map.FindIconAsync(iconId, ct) ?? throw DomainException.NotFound(CatalogErrors.MapIconNotFound)).MediaAssetId;
			items.Add(MapLegendItem.Create(propertyId, r.Items[i].Label, assetId, (short)i));
		}

		_repository.RemoveLegend(await _repository.ListLegendAsync(propertyId, ct));
		_repository.AddLegend(items);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.OnlineLegendUpdated, new Dictionary<string, object?> { ["items"] = items.Count }, ct);
		return await ToDtoAsync(items, ct);
	}

	private static void EnsureNoOverlap(OnlineBookingSettings settings, long sectorId, SaveStayLimitRequest r, long? exceptId)
	{
		if (settings.StayLimits.Any(l => l.Id != exceptId && l.SectorId == sectorId && l.StartsOn <= r.EndsOn && r.StartsOn <= l.EndsOn))
			throw DomainException.Conflict(CatalogErrors.StayLimitOverlap);
	}

	private async Task<OnlineBookingSettings> SettingsAsync(long propertyId, CancellationToken ct)
	{
		var settings = await _repository.FindAsync(propertyId, ct);
		if (settings is null)
		{
			settings = OnlineBookingSettings.Create(propertyId);
			_repository.Add(settings);
		}
		return settings;
	}

	private async Task<MapSector> SectorAsync(long propertyId, Guid id, CancellationToken ct)
		=> await _sectors.FindAsync(propertyId, id, ct) ?? throw DomainException.NotFound(CatalogErrors.SectorNotFound);

	private async Task<OnlineSettingsDto> ToDtoAsync(long propertyId, OnlineBookingSettings? s, CancellationToken ct)
	{
		var suspended = await _repository.IsOnlineSalesSuspendedAsync(propertyId, ct);
		var spots = s is null ? [] : await _repository.FindSpotPublicIdsAsync(s.AllowedLocations.Select(l => l.SpotId).ToList(), ct);
		return new OnlineSettingsDto(
			s?.IsEnabled ?? false, suspended, s?.IsMapPick ?? false, s?.IsLocationRestricted ?? false, spots, s?.IsPeriodRestricted ?? false,
			(s?.AllowedPeriods ?? []).OrderBy(p => p.StartsOn).Select(p => new DateRangeDto(p.StartsOn, p.EndsOn)).ToList(),
			Codes.Of(s?.SurchargeType ?? SurchargeType.Disable), s?.SurchargeValue ?? 0, s?.IsOnlinePaymentBlocked ?? false,
			s?.RequiresManualConfirmation ?? false, s?.AllowsSameDayBooking ?? false, s?.AllowsHalfDay ?? false, s?.MorningFrom, s?.MorningTo,
			s?.AfternoonFrom, s?.AfternoonTo, s?.AfternoonConversionTime, s?.AllowsSeasonal ?? false, s?.AllowsSharing ?? false,
			s?.MaxSpotsPerBooking, s?.MaxBookingsPerDay, s?.BookingHorizonDays, Codes.Of(s?.AllowOnlineBookingScope ?? OnlineBookingScope.All),
			(s?.EnabledPaymentMethods ?? []).Select(m => Codes.Of(m)).ToList(), s?.UsefulInfo, s?.GoodToKnow);
	}

	private static ServiceOnlineSettingsDto ToDto(ServiceBookingSettings? s) => new(
		s?.IsEnabled ?? false, Codes.Of(s?.SurchargeType ?? SurchargeType.Disable), s?.SurchargeValue ?? 0, s?.RequiresManualConfirmation ?? false,
		s?.SectionTitle, s?.UsefulInfo, (s?.EnabledPaymentMethods ?? []).Select(m => Codes.Of(m)).ToList());

	private async Task<IReadOnlyList<StayLimitDto>> ToDtoAsync(long propertyId, IEnumerable<OnlineStayLimit> limits, CancellationToken ct)
	{
		var sectors = (await _sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.Id, s => s.PublicId);
		return limits.OrderBy(l => l.StartsOn).ThenBy(l => l.SectorId)
			.Select(l => new StayLimitDto(l.PublicId, sectors.GetValueOrDefault(l.SectorId), l.StartsOn, l.EndsOn, l.MinDays, l.MaxDays))
			.ToList();
	}

	private async Task<IReadOnlyList<LegendItemDto>> ToDtoAsync(IReadOnlyList<MapLegendItem> items, CancellationToken ct)
	{
		var assetIds = items.Where(i => i.IconAssetId is not null).Select(i => i.IconAssetId!.Value).Distinct().ToList();
		var icons = (await _map.ListIconsByAssetAsync(assetIds, ct)).GroupBy(i => i.MediaAssetId).ToDictionary(g => g.Key, g => g.First().PublicId);
		var urls = await _media.UrlsAsync(items.Select(i => i.IconAssetId), ct);
		return items.OrderBy(i => i.SortOrder)
			.Select(i => new LegendItemDto(i.Label, i.IconAssetId is { } a && icons.TryGetValue(a, out var g) ? g : null, urls.UrlOf(i.IconAssetId)))
			.ToList();
	}

	private static List<OnlinePaymentMethod> PaymentMethods(IEnumerable<string> codes)
		=> codes.Select(c => Codes.TryParse<OnlinePaymentMethod>(c, out var m) ? m : default).Distinct().ToList();

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	private Task RecordLimitAsync(OnlineStayLimit limit, MapSector sector, CancellationToken ct)
		=> RecordAsync(AuditActionCodes.Catalog.StayLimitsUpdated, new Dictionary<string, object?>
		{
			["sector"] = sector.Name,
			["startsOn"] = limit.StartsOn,
			["endsOn"] = limit.EndsOn,
			["minDays"] = limit.MinDays,
			["maxDays"] = limit.MaxDays,
		}, ct);

	private Task RecordAsync(string actionCode, IReadOnlyDictionary<string, object?> metadata, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.Property, _property.PublicId, metadata, ct);
}
