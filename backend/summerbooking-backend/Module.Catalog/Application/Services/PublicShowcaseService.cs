using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Dtos;
using Shared.Localization;

namespace Module.Catalog.Application.Services;

/// <summary>Sito pubblico: home, ricerca e suggerimenti, dalla proiezione di ricerca (solo strutture pubblicate).</summary>
public sealed class PublicShowcaseService
{
	private const int SuggestionLimit = 6;

	private readonly IPublicCatalogRepository _repository;
	private readonly IPlatformRepository _platform;
	private readonly CatalogMedia _media;
	private readonly IRequestLanguage _language;
	private readonly PublicSearchQueryValidator _validator;

	public PublicShowcaseService(
		IPublicCatalogRepository repository, IPlatformRepository platform, CatalogMedia media, IRequestLanguage language, PublicSearchQueryValidator validator)
	{
		_repository = repository;
		_platform = platform;
		_media = media;
		_language = language;
		_validator = validator;
	}

	public async Task<PublicHomeDto> HomeAsync(CancellationToken ct = default)
	{
		var regions = await _platform.ListRegionsAsync(ct);
		var counts = await _repository.CountByRegionAsync(ct);
		return new PublicHomeDto(
			await SectionAsync(ShowcaseSection.Featured, ct), await SectionAsync(ShowcaseSection.Popular, ct),
			regions.Where(r => counts.ContainsKey(r.Id)).Select(r => new PublicRegionDto(r.PublicId, RegionName(r), counts[r.Id])).ToList());
	}

	/// <summary>
	/// Ricerca: testo, regione, dotazioni (tutte), solo prenotabili online; ordinamento per popolarità (predefinito),
	/// prezzo, distanza dalla località cercata o nome. Con le date il prezzo «da» è quello di quei giorni.
	/// </summary>
	public async Task<PublicSearchResultDto> SearchAsync(PublicSearchQuery query, CancellationToken ct = default)
	{
		_validator.EnsureValid(query);
		var amenityIds = await _repository.AmenityIdsAsync(ct);
		var regions = await _platform.ListRegionsAsync(ct);
		long? regionId = null;
		if (query.RegionId is { } rid)
			regionId = regions.FirstOrDefault(r => r.PublicId == rid)?.Id ?? -1;
		var filter = new PublicSearchFilter(
			string.IsNullOrWhiteSpace(query.Text) ? null : query.Text.Trim(), regionId,
			(query.Amenities ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(c => amenityIds.TryGetValue(c, out var id) ? id : -1).Distinct().ToList(),
			query.OnlineOnly, query.Latitude is { } lat && query.Longitude is { } lon ? (lat, lon) : null);
		var sortBy = query.SortBy ?? "popularity";

		IReadOnlyDictionary<long, decimal>? prices = null;
		if (query.From is { } from && query.To is { } to)
			prices = await _repository.MinPricesAsync(await _repository.ListPropertyIdsAsync(filter, ct), from, to, ct);
		var (items, total) = await _repository.SearchAsync(filter, sortBy, query.SortDir == SortDirections.Desc, query.Page, query.PageSize, prices, ct);
		var codes = await _repository.AmenityCodesAsync(ct);
		var facets = (await _repository.CountAmenitiesAsync(filter, ct)).Where(f => codes.ContainsKey(f.Key)).ToDictionary(f => codes[f.Key], f => f.Value);
		var cards = await CardsAsync(items.Select(i => (i.Row, i.DistanceKm)).ToList(), prices, ct);
		return new PublicSearchResultDto(total, query.Page, query.PageSize, cards, facets);
	}

	/// <summary>Mentre si digita: località (città con strutture) e strutture per nome.</summary>
	public async Task<PublicSuggestionsDto> SuggestAsync(string? text, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2)
			return new PublicSuggestionsDto([], []);
		var value = text.Trim()[..Math.Min(text.Trim().Length, CatalogQueryRules.SearchMaxLength)];
		var places = await _repository.SuggestPlacesAsync(value, SuggestionLimit, ct);
		var bathhouses = await _repository.SuggestBathhousesAsync(value, SuggestionLimit, ct);
		return new PublicSuggestionsDto(
			places.Select(p => new PublicPlaceDto(p.City, p.Province, p.Region, p.Latitude, p.Longitude, p.Count)).ToList(),
			await CardsAsync(bathhouses.Select(b => (b, (double?)null)).ToList(), null, ct));
	}

	internal async Task<IReadOnlyList<PublicCardDto>> CardsAsync(
		IReadOnlyList<(SearchBathhouse Row, double? DistanceKm)> rows, IReadOnlyDictionary<long, decimal>? prices, CancellationToken ct)
	{
		var cards = rows.Select(r => (r.Row, r.DistanceKm, Card: SearchCard.Parse(r.Row.Card))).ToList();
		var urls = await _media.UrlsAsync(cards.Select(c => c.Card.MainPhotoAssetId), ct);
		var badges = (await _platform.ListBadgesAsync(ct)).ToDictionary(b => b.PublicId);
		var regions = (await _platform.ListRegionsAsync(ct)).ToDictionary(r => r.Id);
		var codes = await _repository.AmenityCodesAsync(ct);
		return cards.Select(c => new PublicCardDto(
			c.Row.BathhousePublicId, c.Row.Name, c.Row.Slug, c.Row.City, c.Row.Province,
			c.Row.RegionId is { } r && regions.TryGetValue(r, out var region) ? RegionName(region) : c.Row.Region,
			c.Row.Latitude, c.Row.Longitude, c.DistanceKm,
			prices is null ? c.Row.MinPrice : prices.TryGetValue(c.Row.PropertyId, out var p) ? p : null,
			c.Row.LikeCount, urls.UrlOf(c.Card.MainPhotoAssetId), c.Card.Description,
			c.Card.BadgeIds.Where(badges.ContainsKey).Select(id => Badge(badges[id])).ToList(),
			c.Row.AmenityIds.Where(a => codes.ContainsKey(a)).Select(a => codes[a]).ToList(),
			c.Row.IsOnlineBookable, c.Row.IsContactOnly)).ToList();
	}

	internal PublicBadgeDto Badge(ShowcaseBadge b)
		=> new(b.PublicId, Translations.Pick(b.Translations, t => t.Language, t => t.Name, _language.Language, b.Name) ?? b.Name, b.Color);

	internal string RegionName(ShowcaseRegion r) => Translations.Pick(r.Translations, t => t.Language, t => t.Name, _language.Language, r.Name) ?? r.Name;

	private async Task<IReadOnlyList<PublicCardDto>> SectionAsync(ShowcaseSection section, CancellationToken ct)
	{
		var entries = await _platform.ListShowcaseAsync(section, ct);
		var rows = (await _repository.FindPublishedAsync(entries.Select(e => e.Bathhouse!.PublicId).ToList(), ct)).ToDictionary(r => r.BathhousePublicId);
		return await CardsAsync(entries.Where(e => rows.ContainsKey(e.Bathhouse!.PublicId)).Select(e => (rows[e.Bathhouse!.PublicId], (double?)null)).ToList(), null, ct);
	}
}

/// <summary>Contenuto della colonna card della proiezione.</summary>
internal sealed record SearchCard(long? MainPhotoAssetId, string? Description, int PhotoCount, IReadOnlyList<Guid> BadgeIds)
{
	private static readonly System.Text.Json.JsonSerializerOptions Json = new(System.Text.Json.JsonSerializerDefaults.Web);

	public static SearchCard Parse(string json)
		=> System.Text.Json.JsonSerializer.Deserialize<SearchCard>(json, Json) is { } card ? card with { BadgeIds = card.BadgeIds ?? [] } : new SearchCard(null, null, 0, []);
}
