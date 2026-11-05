using System.Text.Json;
using System.Text.Json.Nodes;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Enums;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>
/// Contenuto degli snapshot di configurazione di uno stabilimento, costruito con le stesse letture delle API (stessi dati,
/// stessa forma). Lavora sullo stabilimento fissato in <see cref="CurrentProperty"/> dello scope.
/// </summary>
/// <remarks>
/// I file non sono URL completi ma riferimenti <c>{ origin, path }</c>: un URL di un'origine nota diventa riferimento,
/// e il client compone l'indirizzo con <c>GET /public/media-origins</c>. Così un cambio di bucket o di CDN non
/// richiede di rigenerare gli snapshot. Gli URL esterni restano stringhe.
/// </remarks>
public sealed class ConfigSnapshotBuilder
{
	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	private readonly PropertyInfoService _info;
	private readonly OnlineSettingsService _online;
	private readonly SeasonService _seasons;
	private readonly SectorService _sectors;
	private readonly MapSettingsService _map;
	private readonly PriceListsService _priceLists;
	private readonly ReducedSeasonsService _reduced;
	private readonly DiscountsService _discounts;
	private readonly ServiceItemsService _services;
	private readonly AutomaticPricesService _automatic;
	private readonly TableServicesService _tables;
	private readonly AgreementsService _agreements;
	private readonly InventoryService _inventory;
	private readonly RevenueService _revenue;
	private readonly LookupService _lookups;
	private readonly PublicBathhouseService _public;
	private readonly IOnlineSettingsRepository _onlineRepository;
	private readonly IRevenueRepository _revenueRepository;
	private readonly IPriceListRepository _priceListRepository;
	private readonly IMediaUrls _urls;
	private readonly CurrentProperty _property;

	public ConfigSnapshotBuilder(
		PropertyInfoService info, OnlineSettingsService online, SeasonService seasons, SectorService sectors, MapSettingsService map,
		PriceListsService priceLists, ReducedSeasonsService reduced, DiscountsService discounts, ServiceItemsService services,
		AutomaticPricesService automatic, TableServicesService tables, AgreementsService agreements, InventoryService inventory,
		RevenueService revenue, LookupService lookups, PublicBathhouseService publicBathhouse, IOnlineSettingsRepository onlineRepository,
		IRevenueRepository revenueRepository, IPriceListRepository priceListRepository, IMediaUrls urls, CurrentProperty property)
	{
		_info = info;
		_online = online;
		_seasons = seasons;
		_sectors = sectors;
		_map = map;
		_priceLists = priceLists;
		_reduced = reduced;
		_discounts = discounts;
		_services = services;
		_automatic = automatic;
		_tables = tables;
		_agreements = agreements;
		_inventory = inventory;
		_revenue = revenue;
		_lookups = lookups;
		_public = publicBathhouse;
		_onlineRepository = onlineRepository;
		_revenueRepository = revenueRepository;
		_priceListRepository = priceListRepository;
		_urls = urls;
		_property = property;
	}

	/// <summary>JSON dello snapshot; null se per questo stabilimento non esiste (es. convenzioni assenti, vetrina non pubblicata).</summary>
	public async Task<string?> BuildAsync(ConfigSnapshotKind kind, string? publishedSlug, CancellationToken ct = default)
	{
		object? payload = kind switch
		{
			ConfigSnapshotKind.PropertyManagementConfig => await ManagementAsync(ct),
			ConfigSnapshotKind.BeachMap => await BeachMapAsync(ct),
			ConfigSnapshotKind.PublicShowcase => publishedSlug is null ? null : await _public.GetAsync(publishedSlug, ct),
			ConfigSnapshotKind.PriceCatalog => await PriceCatalogAsync(ct),
			ConfigSnapshotKind.ServicesCatalog => await ServicesCatalogAsync(ct),
			ConfigSnapshotKind.AgreementCatalog => await AgreementCatalogAsync(ct),
			_ => null,
		};
		return payload is null ? null : Serialize(kind, payload);
	}

	private async Task<object> ManagementAsync(CancellationToken ct)
	{
		var propertyId = await _property.IdAsync(ct);
		return new
		{
			Info = await _info.GetAsync(ct),
			Online = await _online.GetAsync(ct),
			ServicesOnline = await _online.GetServicesAsync(ct),
			StayLimits = await _online.ListStayLimitsAsync(ct),
			Season = await _seasons.GetAsync(ct),
			TariffPeriods = await _seasons.ListPeriodsAsync(ct),
			NextSeasonTariffPeriods = await _seasons.ListNextPeriodsAsync(ct),
			Sectors = await _sectors.ListAsync(ct),
			Revenue = await _revenue.GetSettingsAsync(ct),
			VatOptions = await _lookups.VatOptionsAsync(ct),
			IsOnlineSalesSuspended = await _onlineRepository.IsOnlineSalesSuspendedAsync(propertyId, ct),
			Counts = new
			{
				Packages = (await _priceLists.LookupAsync(Codes.Of(PriceListKind.Package), ct)).Count,
				Additionals = (await _priceLists.LookupAsync(Codes.Of(PriceListKind.Additional), ct)).Count,
				Discounts = (await _discounts.ListAsync(new DiscountQuery(null, 1, 10), ct)).Total,
				Services = (await _services.LookupAsync(ct)).Count,
				InventoryItems = (await _inventory.LookupAsync(ct)).Count,
				AgreementPartners = (await _agreements.LookupPartnersAsync(ct)).Count,
			},
		};
	}

	private async Task<object> BeachMapAsync(CancellationToken ct)
	{
		var propertyId = await _property.IdAsync(ct);
		var online = await _onlineRepository.FindAsync(propertyId, ct);
		return new
		{
			Map = await _map.GetMapAsync(ct),
			Legend = await _online.GetLegendAsync(propertyId, ct),
			OnlineSpotIds = online is { IsLocationRestricted: true }
				? await _onlineRepository.FindSpotPublicIdsAsync(online.AllowedLocations.Select(l => l.SpotId).ToList(), ct)
				: null,
		};
	}

	/// <summary>Listino proprio: pacchetti e aggiuntivi completi, periodi, stagionali ridotti, sconti, prezzi suggeriti scelti.</summary>
	private async Task<object> PriceCatalogAsync(CancellationToken ct)
	{
		var propertyId = await _property.IdAsync(ct);
		var items = new List<PriceListDto>();
		foreach (var item in await _priceLists.LookupAsync(null, ct))
			items.Add(await _priceLists.GetAsync(item.Id, ct));
		var applied = new List<object>();
		foreach (var package in (await _priceListRepository.ListAllAsync(propertyId, PriceListKind.Package, ct)).Where(p => p.IsDefault))
			applied.AddRange((await _revenueRepository.ListSuggestedPricesAsync(package.Id, DateOnly.MinValue, DateOnly.MaxValue, ct))
				.Where(s => s.AppliedMode != SuggestedPriceMode.None)
				.OrderBy(s => s.PriceDate)
				.Select(s => (object)new { PriceListId = package.PublicId, Date = s.PriceDate, s.TariffPeriodGroup, s.SuggestedPriceValue, s.DiscountPercentage, Mode = Codes.Of(s.AppliedMode) }));
		return new
		{
			TariffPeriods = await _seasons.ListPeriodsAsync(ct),
			Sectors = await _sectors.ListAsync(ct),
			CalculationMethods = await _lookups.ListCalculationMethodsAsync(ct),
			PriceLists = items,
			ReducedSeasons = await _reduced.ListAsync(ct),
			Discounts = (await _discounts.ListAsync(new DiscountQuery(null, 1, 300), ct)).Items,
			AppliedSuggestedPrices = applied,
		};
	}

	private async Task<object> ServicesCatalogAsync(CancellationToken ct)
	{
		var services = new List<ServiceDto>();
		foreach (var item in await _services.LookupAsync(ct))
			services.Add(await _services.GetAsync(item.Id, ct));
		return new
		{
			Services = services,
			AutomaticPrices = await _automatic.ListAsync(ct),
			Tables = await _tables.ListAsync(ct),
			Settings = await _online.GetServicesAsync(ct),
		};
	}

	private async Task<object?> AgreementCatalogAsync(CancellationToken ct)
	{
		var partners = await _agreements.LookupPartnersAsync(ct);
		if (partners.Count == 0)
			return null;
		var result = new List<object>();
		foreach (var partner in partners)
			result.Add(new { Partner = await _agreements.GetPartnerAsync(partner.Id, ct), PriceLists = await _agreements.ListPriceListsAsync(partner.Id, null, null, ct) });
		return new { Partners = result };
	}

	private string Serialize(ConfigSnapshotKind kind, object payload)
	{
		var node = JsonSerializer.SerializeToNode(payload, Json)!;
		var root = new JsonObject { ["schema"] = Domain.Entities.ConfigSnapshot.CurrentSchema, ["kind"] = Codes.Of(kind), ["data"] = ToAssetRefs(node, _urls.Origins) };
		return root.ToJsonString(Json);
	}

	/// <summary>Gli URL delle origini note diventano { origin, path }.</summary>
	private static JsonNode? ToAssetRefs(JsonNode? node, IReadOnlyDictionary<string, string> origins)
	{
		switch (node)
		{
			case JsonObject obj:
				foreach (var key in obj.Select(p => p.Key).ToList())
					obj[key] = ToAssetRefs(obj[key]?.DeepClone(), origins);
				return obj;
			case JsonArray arr:
				for (var i = 0; i < arr.Count; i++)
					arr[i] = ToAssetRefs(arr[i]?.DeepClone(), origins);
				return arr;
			case JsonValue value when value.TryGetValue<string>(out var text):
				foreach (var (origin, baseUrl) in origins)
					if (text.StartsWith(baseUrl + "/", StringComparison.Ordinal))
						return new JsonObject { ["origin"] = origin, ["path"] = text[(baseUrl.Length + 1)..] };
				return value;
			default:
				return node;
		}
	}
}
