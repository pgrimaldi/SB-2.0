using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Scheda pubblica di una struttura (per slug): informazioni, mappa, servizi, pacchetti, banner sconto.</summary>
public sealed class PublicBathhouseService
{
	private const int BannerFromHour = 10;
	private const int BannerToHour = 20;
	private static readonly TimeZoneInfo Rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

	private readonly IPublicCatalogRepository _repository;
	private readonly IPropertyInfoRepository _info;
	private readonly IOnlineSettingsRepository _online;
	private readonly IMapRepository _map;
	private readonly IServiceCatalogRepository _services;
	private readonly IPlatformRepository _platform;
	private readonly PropertyInfoService _infoService;
	private readonly OnlineSettingsService _onlineSettings;
	private readonly PublicShowcaseService _showcase;
	private readonly MapProjection _projection;
	private readonly CatalogMedia _media;
	private readonly IRequestLanguage _language;
	private readonly IDateTimeProvider _clock;

	public PublicBathhouseService(
		IPublicCatalogRepository repository, IPropertyInfoRepository info, IOnlineSettingsRepository online, IMapRepository map,
		IServiceCatalogRepository services, IPlatformRepository platform, PropertyInfoService infoService, OnlineSettingsService onlineSettings, PublicShowcaseService showcase,
		MapProjection projection, CatalogMedia media, IRequestLanguage language, IDateTimeProvider clock)
	{
		_repository = repository;
		_info = info;
		_online = online;
		_map = map;
		_services = services;
		_platform = platform;
		_infoService = infoService;
		_onlineSettings = onlineSettings;
		_showcase = showcase;
		_projection = projection;
		_media = media;
		_language = language;
		_clock = clock;
	}

	public async Task<PublicBathhouseDto> GetAsync(string slug, CancellationToken ct = default)
	{
		var row = await LoadAsync(slug, ct);
		var bathhouse = await _info.FindBathhouseAsync(row.PropertyId, ct) ?? throw DomainException.NotFound(CatalogErrors.BathhouseNotFound);
		var card = (await _showcase.CardsAsync([(row, null)], null, ct))[0];
		PublicOnlineRulesDto? rules = null;
		if (row.IsOnlineBookable && await _online.FindAsync(row.PropertyId, ct) is { } o)
			rules = new PublicOnlineRulesDto(
				o.AllowsHalfDay, o.MorningFrom, o.MorningTo, o.AfternoonFrom, o.AfternoonTo, o.AllowsSeasonal, o.AllowsSameDayBooking, o.BookingHorizonDays,
				o.RequiresManualConfirmation, o.IsOnlinePaymentBlocked, o.EnabledPaymentMethods.Select(m => Codes.Of(m)).ToList(), o.GoodToKnow, o.UsefulInfo);
		return new PublicBathhouseDto(
			row.BathhousePublicId, await _infoService.DescribeAsync(bathhouse, ct), card.Region, card.Badges, row.LikeCount, row.MinPrice,
			row.IsOnlineBookable, row.IsContactOnly, rules);
	}

	public async Task<PublicMapDto> MapAsync(string slug, CancellationToken ct = default)
	{
		var row = await LoadAsync(slug, ct);
		var layout = await _map.FindLayoutAsync(row.PropertyId, ct);
		var spots = await _map.ListSpotsAsync(row.PropertyId, ct);
		var online = await _online.FindAsync(row.PropertyId, ct);
		IReadOnlyList<Guid>? allowed = online is { IsLocationRestricted: true }
			? await _online.FindSpotPublicIdsAsync(online.AllowedLocations.Select(l => l.SpotId).ToList(), ct)
			: null;
		return new PublicMapDto(await _projection.MapAsync(row.PropertyId, layout, spots, ct), allowed, await _onlineSettings.GetLegendAsync(row.PropertyId, ct));
	}

	public async Task<PublicServicesDto> ServicesAsync(string slug, CancellationToken ct = default)
	{
		var row = await LoadAsync(slug, ct);
		var items = await _repository.ListOnlineServicesAsync(row.PropertyId, ct);
		var icons = await _services.FindIconsAsync(items.Where(i => i.IconId is not null).Select(i => i.IconId!.Value).Distinct().ToList(), ct);
		var urls = await _media.UrlsAsync(items.Select(i => i.IconAssetId).Concat(icons.Values.Select(v => (long?)v.AssetId)), ct);
		var language = _language.Language;
		var services = items.Select(i => new PublicServiceDto(
			i.PublicId, Codes.Of(i.Kind),
			Translations.Pick(i.Translations, t => t.Language, t => t.Name, language, i.Name) ?? i.Name,
			Translations.Pick(i.Translations, t => t.Language, t => t.Description, language, i.Description),
			i.Category is { } c ? Codes.Of(c) : null, i.Price,
			urls.UrlOf(i.IconAssetId) ?? (i.IconId is { } icon && icons.TryGetValue(icon, out var ic) ? urls.UrlOf(ic.AssetId) : null),
			i.IsHighlighted, i.Allergens.Select(a => Codes.Of(a)).ToList(),
			i.Variants.OrderBy(v => v.SortOrder).Select(v => new PublicVariantDto(v.Name, v.Price)).ToList())).ToList();
		var tables = (await _repository.ListOnlineTablesAsync(row.PropertyId, ct)).Select(t => new PublicTableServiceDto(
			t.PublicId, Translations.Pick(t.Translations, x => x.Language, x => x.Name, language, t.Name) ?? t.Name,
			Translations.Pick(t.Translations, x => x.Language, x => x.Description, language, t.Description), t.IsHighlighted, Codes.Of(t.DepositMode))).ToList();
		return new PublicServicesDto(services, tables);
	}

	/// <summary>Pacchetti vendibili online con il prezzo «da» per le date indicate (senza date: oggi, o la stagione in arrivo).</summary>
	public async Task<IReadOnlyList<PublicPackageDto>> PackagesAsync(string slug, DateOnly? from, DateOnly? to, CancellationToken ct = default)
	{
		var row = await LoadAsync(slug, ct);
		if (from is { } f && (to is not { } t0 || t0 < f || t0.DayNumber - f.DayNumber >= Validators.PublicRules.MaxSearchDays))
			throw new ValidationFailedException("to", ValidationErrorCodes.Invalid);
		var packages = await _repository.ListOnlinePackagesAsync(row.PropertyId, ct);
		var language = _language.Language;
		var result = new List<PublicPackageDto>();
		foreach (var p in packages)
		{
			decimal? price = null;
			if (from is { } start && to is { } end)
				price = await PackagePriceAsync(p, start, end, ct);
			result.Add(new PublicPackageDto(p.PublicId, Translations.Pick(p.Translations, x => x.Language, x => x.Name, language, p.Name) ?? p.Name,
				p.MaxPeople, p.IsDefaultOnline, price ?? (from is null ? row.MinPrice : null)));
		}
		return result;
	}

	/// <summary>Il banner si vede fra le 10 e le 20, una volta per fascia: la giornata è divisa in tante fasce quante le visualizzazioni scelte.</summary>
	public async Task<PublicDiscountBannerDto> BannerAsync(string slug, CancellationToken ct = default)
	{
		var row = await LoadAsync(slug, ct);
		var (visible, settings, discount) = await BannerStateAsync(row, ct);
		return await BannerDtoAsync(visible, settings, discount, ct);
	}

	/// <summary>Il banner è stato mostrato: conta la visualizzazione, se ancora concessa.</summary>
	public async Task<PublicDiscountBannerDto> CountBannerViewAsync(string slug, CancellationToken ct = default)
	{
		var row = await LoadAsync(slug, ct);
		var (visible, settings, discount) = await BannerStateAsync(row, ct);
		if (visible)
			visible = await _repository.CountBannerViewAsync(row.PropertyId, (short)(SlotIndex(settings!) + 1), ct);
		return await BannerDtoAsync(visible, settings, discount, ct);
	}

	private async Task<(bool Visible, RevenueSettings? Settings, SuggestedPrice? Discount)> BannerStateAsync(SearchBathhouse row, CancellationToken ct)
	{
		var settings = await _repository.FindRevenueSettingsAsync(row.PropertyId, ct);
		if (settings is not { IsPriceDiscountEnabled: true, IsShownOnPublicPage: true } || settings.DailyBannerCount <= 0 || !row.IsOnlineBookable)
			return (false, settings, null);
		var now = TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow, Rome);
		var discount = await _repository.FindAppliedDiscountAsync(row.PropertyId, DateOnly.FromDateTime(now), ct);
		var slot = SlotIndex(settings);
		return (slot >= 0 && settings.DailyBannerShownCount <= slot, settings, discount);
	}

	/// <summary>Fascia corrente (0 = la prima), -1 fuori dalle 10–20.</summary>
	private int SlotIndex(RevenueSettings settings)
	{
		var now = TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow, Rome);
		var minutes = (now.Hour - BannerFromHour) * 60 + now.Minute;
		var span = (BannerToHour - BannerFromHour) * 60;
		return minutes < 0 || minutes >= span ? -1 : minutes * settings.DailyBannerCount / span;
	}

	private async Task<PublicDiscountBannerDto> BannerDtoAsync(bool visible, RevenueSettings? settings, SuggestedPrice? discount, CancellationToken ct)
	{
		IReadOnlyList<Guid> spots = [];
		if (visible && settings is { IsDiscountLimitedToSpots: true })
			spots = await _online.FindSpotPublicIdsAsync(settings.DiscountedSpots.Select(s => s.SpotId).ToList(), ct);
		return new PublicDiscountBannerDto(visible, visible ? discount?.DiscountPercentage : null, spots);
	}

	private async Task<decimal?> PackagePriceAsync(PriceList package, DateOnly from, DateOnly to, CancellationToken ct)
	{
		var season = await _platform.FindSeasonPeriodsAsync(package.PropertyId, ct);
		var groups = new HashSet<short>();
		for (var day = from; day <= to; day = day.AddDays(1))
			if (TariffCalendar.Resolve(season, day) is { } period)
				groups.Add(period.PeriodGroupKey);
		var prices = await _platform.ListPeriodPricesAsync(package.Id, ct);
		var values = prices.Where(p => p.TariffPeriodGroup is { } g && groups.Contains(g)).Select(p => p.HalfDayPrice ?? p.DailyPrice).Where(v => v > 0).ToList();
		return values.Count == 0 ? null : values.Min();
	}

	private async Task<SearchBathhouse> LoadAsync(string slug, CancellationToken ct)
		=> await _repository.FindPublishedBySlugAsync(slug.Trim().ToLowerInvariant(), ct) ?? throw DomainException.NotFound(CatalogErrors.BathhouseNotFound);
}
