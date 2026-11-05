using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Module.Catalog.Domain.ValueObjects;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Layout e postazioni nei DTO delle API: file come URL, riferimenti interni come id pubblici.</summary>
public sealed class MapProjection
{
	private readonly IMapRepository _map;
	private readonly IMediaAssetRepository _media;
	private readonly ISectorRepository _sectors;
	private readonly IMediaUrls _urls;

	public MapProjection(IMapRepository map, IMediaAssetRepository media, ISectorRepository sectors, IMediaUrls urls)
	{
		_map = map;
		_media = media;
		_sectors = sectors;
		_urls = urls;
	}

	public string? Url(MediaRef? reference) => _urls.Resolve(reference);

	public async Task<MapDto> MapAsync(long propertyId, MapLayout? layout, IReadOnlyList<MapSpot> spots, CancellationToken ct = default)
	{
		var sectors = (await _sectors.ListAsync(propertyId, ct)).OrderBy(s => s.SortOrder).ThenBy(s => s.Name).Select(SectorDto.FromEntity).ToList();
		var used = spots.Where(s => s.BeachElementId is not null).Select(s => s.BeachElementId!.Value).ToHashSet();
		var elements = used.Count == 0 ? [] : (await _map.ListBeachElementsAsync(false, ct)).Where(e => used.Contains(e.Id)).ToList();
		var refs = await _media.FindRefsAsync(elements.Where(e => e.MediaAssetId is not null).Select(e => e.MediaAssetId!.Value).ToList(), ct);
		return new MapDto(await SettingsAsync(layout, ct), await SpotsAsync(propertyId, spots, ct), sectors,
			elements.Select(e => new MapBeachElementDto(e.PublicId, e.Name, e.MediaAssetId is { } id ? _urls.Resolve(refs.GetValueOrDefault(id)) : null)).ToList());
	}

	public async Task<MapSettingsDto> SettingsAsync(MapLayout? layout, CancellationToken ct = default)
	{
		var specialIcons = layout?.SpecialIcons.ToList() ?? [];
		var assetIds = new[] { layout?.TemplateAssetId, layout?.UmbrellaFreeAssetId, layout?.UmbrellaOccupiedAssetId }
			.Where(id => id is not null).Select(id => id!.Value)
			.Concat(specialIcons.Select(i => i.MediaAssetId))
			.Distinct().ToList();

		var defaults = await _map.ListDefaultIconsAsync(ct);
		var refs = await _media.FindRefsAsync(assetIds.Concat(defaults.Select(d => d.MediaAssetId)).Distinct().ToList(), ct);
		var iconsByAsset = (await _map.ListIconsByAssetAsync(assetIds, ct))
			.GroupBy(i => i.MediaAssetId).ToDictionary(g => g.Key, g => g.First());

		MapIconRefDto Slot(long? assetId, MapIconState state)
		{
			if (assetId is { } id)
			{
				var icon = iconsByAsset.GetValueOrDefault(id);
				return new MapIconRefDto(icon?.PublicId, _urls.Resolve(refs.GetValueOrDefault(id)), icon?.IsDefault ?? false);
			}
			var fallback = defaults.FirstOrDefault(d => d.Category?.Code == MapIconCategoryCode.Umbrella && d.Type == state);
			return new MapIconRefDto(fallback?.PublicId, fallback is null ? null : _urls.Resolve(refs.GetValueOrDefault(fallback.MediaAssetId)), true);
		}

		MapIconPairDto Pair(long? free, long? occupied) => new(Slot(free, MapIconState.Free), Slot(occupied, MapIconState.Occupied));

		var customArticles = Enumerable.Range(1, MapLayoutSpecialIcon.MaxVariant).Select(v => (short)v)
			.Select(v => new CustomArticleIconsDto(v, Pair(
				specialIcons.FirstOrDefault(i => i.Variant == v && i.State == MapIconState.Free)?.MediaAssetId,
				specialIcons.FirstOrDefault(i => i.Variant == v && i.State == MapIconState.Occupied)?.MediaAssetId)))
			.ToList();

		MapBackgroundDto? background = null;
		if (layout?.TemplateAssetId is { } templateId)
			background = new MapBackgroundDto(
				layout.IsCustomTemplate ? null : iconsByAsset.GetValueOrDefault(templateId)?.PublicId,
				_urls.Resolve(refs.GetValueOrDefault(templateId)), layout.IsCustomTemplate);

		return new MapSettingsDto(
			layout?.GridRows ?? 0, layout?.GridColumns ?? 0, layout?.TiltDegrees ?? 0, layout?.BackgroundColor, background,
			new MapIconsDto(Pair(layout?.UmbrellaFreeAssetId, layout?.UmbrellaOccupiedAssetId), customArticles),
			layout?.ResetDeletesBookings ?? false);
	}

	public async Task<IReadOnlyList<MapSpotDto>> SpotsAsync(long propertyId, IReadOnlyList<MapSpot> spots, CancellationToken ct = default)
	{
		var sectors = (await _sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.Id, s => (Guid?)s.PublicId);
		var elements = spots.Any(s => s.IsBeachElement)
			? (await _map.ListBeachElementsAsync(false, ct)).ToDictionary(e => e.Id, e => (Guid?)e.PublicId)
			: [];

		return spots
			.OrderBy(s => s.GridRow).ThenBy(s => s.GridColumn)
			.Select(s => new MapSpotDto(
				s.PublicId, s.GridRow ?? 0, s.GridColumn ?? 0, s.RowSpan, s.ColumnSpan, Codes.Of(s.SpotType), s.Label,
				s.SectorId is { } sectorId ? sectors.GetValueOrDefault(sectorId) : null, s.AreaSeatCount, s.IsAgreementOnly,
				s.RotationDegrees, s.OffsetX, s.OffsetY,
				s.BeachElementId is { } elementId ? elements.GetValueOrDefault(elementId) : null))
			.ToList();
	}
}
