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
using Shared.Security;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>
/// Revenue dello stabilimento: impostazioni e banner sconto, calcolo e scelta del prezzo suggerito. I pesi e i periodi
/// standard sono del systemadmin (<see cref="RevenueAdminService"/>); la percentuale di posti liberi arriverà da Booking:
/// finché manca la regola dei posti liberi non si applica.
/// </summary>
public sealed class RevenueService
{
	private readonly IRevenueRepository _repository;
	private readonly IPriceListRepository _priceLists;
	private readonly ISeasonRepository _seasons;
	private readonly ISectorRepository _sectors;
	private readonly IMapRepository _map;
	private readonly IPropertyInfoRepository _propertyInfo;
	private readonly IWeatherForecast _weather;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly IDateTimeProvider _clock;
	private readonly CatalogAudit _audit;
	private readonly SaveRevenueSettingsRequestValidator _settingsValidator;
	private readonly SuggestedPriceQueryValidator _queryValidator;
	private readonly ComputeSuggestedPricesRequestValidator _computeValidator;
	private readonly ApplySuggestedPricesRequestValidator _applyValidator;

	public RevenueService(
		IRevenueRepository repository, IPriceListRepository priceLists, ISeasonRepository seasons, ISectorRepository sectors, IMapRepository map,
		IPropertyInfoRepository propertyInfo, IWeatherForecast weather, IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user,
		IDateTimeProvider clock, CatalogAudit audit, SaveRevenueSettingsRequestValidator settingsValidator, SuggestedPriceQueryValidator queryValidator,
		ComputeSuggestedPricesRequestValidator computeValidator, ApplySuggestedPricesRequestValidator applyValidator)
	{
		_repository = repository;
		_priceLists = priceLists;
		_seasons = seasons;
		_sectors = sectors;
		_map = map;
		_propertyInfo = propertyInfo;
		_weather = weather;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_clock = clock;
		_audit = audit;
		_settingsValidator = settingsValidator;
		_queryValidator = queryValidator;
		_computeValidator = computeValidator;
		_applyValidator = applyValidator;
	}

	public async Task<RevenueSettingsDto> GetSettingsAsync(CancellationToken ct = default)
	{
		var propertyId = await _property.IdAsync(ct);
		return ToDto(await _repository.FindSettingsAsync(propertyId, ct), await SpotPublicIdsAsync(propertyId, ct));
	}

	public async Task<RevenueSettingsDto> UpdateSettingsAsync(SaveRevenueSettingsRequest request, CancellationToken ct = default)
	{
		_settingsValidator.EnsureValid(request);
		var propertyId = await _property.IdAsync(ct);
		var settings = await _repository.FindSettingsAsync(propertyId, ct);
		if (settings is null)
		{
			settings = RevenueSettings.Create(propertyId);
			_repository.AddSettings(settings);
		}
		settings.Update(request.IsSuggestedPriceEnabled, request.IsPriceDiscountEnabled, request.IsShownOnPublicPage, request.DailyBannerCount,
			request.IsDiscountLimitedToSpots);
		var spots = (await _map.ListSpotsAsync(propertyId, ct)).ToDictionary(s => s.PublicId, s => s.Id);
		settings.SetSpots(request.IsDiscountLimitedToSpots
			? (request.SpotIds ?? []).Distinct().Select(id => spots.TryGetValue(id, out var spotId) ? spotId : throw DomainException.NotFound(CatalogErrors.MapSpotNotFound)).ToList()
			: []);
		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Catalog.RevenueSettingsUpdated, AuditActionCodes.Catalog.Targets.Property, _property.PublicId,
			new Dictionary<string, object?> { ["suggestedPrice"] = settings.IsSuggestedPriceEnabled, ["priceDiscount"] = settings.IsPriceDiscountEnabled }, ct);
		return ToDto(settings, spots.ToDictionary(s => s.Value, s => s.Key));
	}

	public async Task<IReadOnlyList<SuggestedPriceDto>> ListSuggestedPricesAsync(SuggestedPriceQuery query, CancellationToken ct = default)
	{
		_queryValidator.EnsureValid(query);
		var propertyId = await _property.IdAsync(ct);
		var priceList = await PackageAsync(propertyId, query.PriceListId, ct);
		var prices = await _repository.ListSuggestedPricesAsync(priceList.Id, query.From!.Value, query.To!.Value, ct);
		return ToDtos(priceList, prices, await ContentMapping.SectorPublicIdsAsync(_sectors, propertyId, ct));
	}

	/// <summary>
	/// Calcola il prezzo suggerito di ogni giorno per ogni fila del pacchetto, partendo dal prezzo del periodo tariffario del
	/// giorno (vince il più breve). Un giorno senza periodo o senza prezzo non ha suggerimento. Ricalcolando, la scelta del
	/// gestore resta.
	/// </summary>
	public async Task<IReadOnlyList<SuggestedPriceDto>> ComputeAsync(ComputeSuggestedPricesRequest request, CancellationToken ct = default)
	{
		_computeValidator.EnsureValid(request);
		var propertyId = await _property.IdAsync(ct);
		var priceList = await PackageAsync(propertyId, request.PriceListId, ct);
		var periods = (await _seasons.FindAsync(propertyId, ct))?.TariffPeriods.ToList() ?? [];
		var standard = await _repository.ListStandardPeriodsAsync(ct);
		var weights = await _repository.GetWeightsAsync(ct);
		var bathhouse = await _propertyInfo.FindBathhouseAsync(propertyId, ct);
		var rain = bathhouse is { Latitude: { } lat, Longitude: { } lon }
			? await _weather.RainAsync(lat, lon, request.From, request.To, ct)
			: new Dictionary<DateOnly, decimal>();
		var existing = (await _repository.ListSuggestedPricesAsync(priceList.Id, request.From, request.To, ct)).ToDictionary(p => (p.PriceDate, p.SectorId));
		var now = _clock.UtcNow;

		for (var date = request.From; date <= request.To; date = date.AddDays(1))
		{
			if (TariffCalendar.Resolve(periods, date) is not { } period)
				continue;
			var tier = standard.FirstOrDefault(s => s.Contains(date))?.Tier ?? RevenueSeasonTier.Low;
			var facts = new SuggestedPriceRules.DayFacts(date, rain.TryGetValue(date, out var mm) ? mm > 0 : null, tier, null);
			foreach (var price in priceList.PeriodPrices.Where(p => p.TariffPeriodGroup == period.PeriodGroupKey))
			{
				var basePrice = SuggestedPriceRules.BasePrice(date, price.DailyPrice, price.WeekendPrice);
				if (basePrice <= 0)
					continue;
				var suggested = SuggestedPriceRules.Suggest(basePrice, facts, weights);
				if (!existing.TryGetValue((date, price.SectorId), out var row))
				{
					row = SuggestedPrice.Create(priceList.Id, propertyId, date, price.SectorId);
					_repository.AddSuggestedPrice(row);
					existing[(date, price.SectorId)] = row;
				}
				row.Recompute(period.PeriodGroupKey, price.DailyPrice, price.WeekendPrice, suggested, SuggestedPriceRules.DiscountPercent(basePrice, suggested), now);
			}
		}
		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Catalog.SuggestedPricesComputed, AuditActionCodes.Catalog.Targets.PriceList, priceList.PublicId,
			new Dictionary<string, object?> { ["from"] = request.From, ["to"] = request.To }, ct);
		return ToDtos(priceList, existing.Values.Where(p => p.ComputedAt == now).ToList(), await ContentMapping.SectorPublicIdsAsync(_sectors, propertyId, ct));
	}

	/// <summary>Scelta del gestore per giorno e fila: prezzo suggerito, sconto, prezzo originale o nessuna. Tutto o niente.</summary>
	public async Task<IReadOnlyList<SuggestedPriceDto>> ApplyAsync(ApplySuggestedPricesRequest request, CancellationToken ct = default)
	{
		_applyValidator.EnsureValid(request);
		var propertyId = await _property.IdAsync(ct);
		var priceList = await PackageAsync(propertyId, request.PriceListId, ct);
		var sectors = await ContentMapping.SectorPublicIdsAsync(_sectors, propertyId, ct);
		var sectorIds = sectors.ToDictionary(s => s.Value, s => s.Key);
		var rows = (await _repository.ListSuggestedPricesAsync(priceList.Id, request.Items.Min(i => i.Date), request.Items.Max(i => i.Date), ct))
			.ToDictionary(p => (p.PriceDate, p.SectorId));
		var now = _clock.UtcNow;
		var applied = new List<SuggestedPrice>();
		foreach (var item in request.Items)
		{
			long? sectorId = item.SectorId is { } g ? sectorIds.TryGetValue(g, out var id) ? id : throw DomainException.NotFound(CatalogErrors.SectorNotFound) : null;
			if (!rows.TryGetValue((item.Date, sectorId), out var row))
				throw DomainException.NotFound(CatalogErrors.SuggestedPriceNotFound);
			Codes.TryParse<SuggestedPriceMode>(item.Mode, out var mode);
			row.Apply(mode, item.DiscountPercent, _user.UserId, now);
			applied.Add(row);
		}
		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Catalog.SuggestedPricesApplied, AuditActionCodes.Catalog.Targets.PriceList, priceList.PublicId,
			new Dictionary<string, object?> { ["count"] = applied.Count }, ct);
		return ToDtos(priceList, applied.Distinct().ToList(), sectors);
	}

	/// <summary>Il pacchetto indicato o, senza, il primo creato fra quelli predefiniti.</summary>
	private async Task<PriceList> PackageAsync(long propertyId, Guid? priceListId, CancellationToken ct)
	{
		PriceList? priceList;
		if (priceListId is { } id)
			priceList = await _priceLists.FindAsync(propertyId, id, ct);
		else
		{
			var defaultId = (await _priceLists.ListAllAsync(propertyId, PriceListKind.Package, ct)).Where(p => p.IsDefault).MinBy(p => p.Id)?.PublicId;
			priceList = defaultId is { } d ? await _priceLists.FindAsync(propertyId, d, ct) : null;
		}
		return priceList is { Kind: PriceListKind.Package } ? priceList : throw DomainException.NotFound(CatalogErrors.PriceListNotFound);
	}

	private async Task<IReadOnlyDictionary<long, Guid>> SpotPublicIdsAsync(long propertyId, CancellationToken ct)
		=> (await _map.ListSpotsAsync(propertyId, ct)).ToDictionary(s => s.Id, s => s.PublicId);

	private static RevenueSettingsDto ToDto(RevenueSettings? s, IReadOnlyDictionary<long, Guid> spots) => s is null
		? new RevenueSettingsDto(false, false, false, 0, 0, false, [])
		: new RevenueSettingsDto(s.IsSuggestedPriceEnabled, s.IsPriceDiscountEnabled, s.IsShownOnPublicPage, s.DailyBannerCount, s.DailyBannerShownCount,
			s.IsDiscountLimitedToSpots, s.DiscountedSpots.Select(d => spots.GetValueOrDefault(d.SpotId)).Where(g => g != Guid.Empty).ToList());

	private static IReadOnlyList<SuggestedPriceDto> ToDtos(PriceList priceList, IReadOnlyList<SuggestedPrice> prices, IReadOnlyDictionary<long, Guid> sectors)
		=> prices.OrderBy(p => p.PriceDate).ThenBy(p => p.SectorId)
			.Select(p => new SuggestedPriceDto(priceList.PublicId, p.PriceDate, p.SectorId is { } s ? sectors.GetValueOrDefault(s) : null, p.TariffPeriodGroup,
				p.ListDailyPrice, p.ListWeekendPrice, p.SuggestedPriceValue, p.DiscountPercentage, Codes.Of(p.AppliedMode), p.ComputedAt, p.AppliedAt))
			.ToList();
}
