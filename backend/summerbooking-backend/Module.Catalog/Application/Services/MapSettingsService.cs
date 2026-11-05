using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Security;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Impostazioni › Configura mappa: reticolo, sfondo, icone; più i cataloghi di icone ed elementi spiaggia.</summary>
public sealed class MapSettingsService
{
	private readonly IMapRepository _map;
	private readonly IMediaAssetRepository _media;
	private readonly IPropertyFileStorage _images;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly MapProjection _projection;
	private readonly SaveMapSettingsRequestValidator _validator;

	public MapSettingsService(
		IMapRepository map, IMediaAssetRepository media, IPropertyFileStorage images, IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user,
		CatalogAudit audit, MapProjection projection, SaveMapSettingsRequestValidator validator)
	{
		_map = map;
		_media = media;
		_images = images;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_projection = projection;
		_validator = validator;
	}

	public async Task<MapDto> GetMapAsync(CancellationToken ct = default)
	{
		var propertyId = await _property.IdAsync(ct);
		return await _projection.MapAsync(propertyId, await _map.FindLayoutAsync(propertyId, ct), await _map.ListSpotsAsync(propertyId, ct), ct);
	}

	public async Task<MapSettingsDto> GetSettingsAsync(CancellationToken ct = default)
		=> await _projection.SettingsAsync(await _map.FindLayoutAsync(await _property.IdAsync(ct), ct), ct);

	public async Task<MapSettingsDto> SaveSettingsAsync(SaveMapSettingsRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var propertyId = await _property.IdAsync(ct);
		var layout = await LayoutAsync(propertyId, ct);

		// Ridurre il reticolo non elimina postazioni: per quello ci sono l'eliminazione di righe e colonne.
		var spots = await _map.ListSpotsAsync(propertyId, ct);
		if (spots.Any(s => s.GridRow + s.RowSpan - 1 > request.Rows || s.GridColumn + s.ColumnSpan - 1 > request.Columns))
			throw DomainException.Conflict(CatalogErrors.MapSpotsOutsideGrid, new Dictionary<string, object?>
			{
				["rows"] = spots.Max(s => s.GridRow + s.RowSpan - 1),
				["columns"] = spots.Max(s => s.GridColumn + s.ColumnSpan - 1),
			});

		layout.SetGrid(request.Rows, request.Columns, request.TiltDegrees);
		layout.SetResetMode(request.ResetDeletesBookings);
		if (request.BackgroundIconId is { } backgroundId)
			layout.SetBackground((await IconAsync(backgroundId, MapIconCategoryCode.Template, null, ct)).MediaAssetId, false, request.BackgroundColor);
		else
			layout.SetBackground(layout.IsCustomTemplate ? layout.TemplateAssetId : null, layout.IsCustomTemplate, request.BackgroundColor);

		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.MapSettingsUpdated, new Dictionary<string, object?>
		{
			["rows"] = request.Rows,
			["columns"] = request.Columns,
			["tiltDegrees"] = request.TiltDegrees,
			["resetDeletesBookings"] = request.ResetDeletesBookings,
		}, ct);
		return await _projection.SettingsAsync(layout, ct);
	}

	/// <summary>Sfondo caricato dal gestore: sostituisce quello del catalogo.</summary>
	public async Task<MapSettingsDto> UploadBackgroundAsync(string fileName, string contentType, long length, Stream content, CancellationToken ct = default)
	{
		UploadRules.EnsureImage(contentType, length, CatalogErrors.MapInvalidImage);

		var propertyId = await _property.IdAsync(ct);
		var layout = await LayoutAsync(propertyId, ct);
		var stored = await _images.SaveAsync(fileName, content, contentType, ct);
		var asset = MediaAsset.Upload(stored.Ref, MediaKind.Photo, stored.Sha256, stored.ByteSize, stored.ContentType, propertyId);
		_media.Add(asset);
		await _unitOfWork.SaveChangesAsync(ct);

		layout.SetBackground(asset.Id, true, layout.BackgroundColor);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.MapBackgroundUpdated, new Dictionary<string, object?> { ["custom"] = true }, ct);
		return await _projection.SettingsAsync(layout, ct);
	}

	public async Task<MapSettingsDto> RemoveBackgroundAsync(CancellationToken ct = default)
	{
		var layout = await LayoutAsync(await _property.IdAsync(ct), ct);
		layout.SetBackground(null, false, layout.BackgroundColor);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.MapBackgroundUpdated, new Dictionary<string, object?> { ["removed"] = true }, ct);
		return await _projection.SettingsAsync(layout, ct);
	}

	public async Task<MapSettingsDto> SaveUmbrellaIconsAsync(SaveMapIconPairRequest request, CancellationToken ct = default)
	{
		var layout = await LayoutAsync(await _property.IdAsync(ct), ct);
		var (free, occupied) = await PairAsync(request, ct);
		layout.SetUmbrellaIcons(free, occupied);
		return await SaveIconsAsync(layout, "umbrella", ct);
	}

	public async Task<MapSettingsDto> SaveCustomArticleIconsAsync(short variant, SaveMapIconPairRequest request, CancellationToken ct = default)
	{
		if (variant is < 1 or > MapLayoutSpecialIcon.MaxVariant)
			throw DomainException.NotFound(CatalogErrors.MapIconNotFound);
		var layout = await LayoutAsync(await _property.IdAsync(ct), ct);
		var (free, occupied) = await PairAsync(request, ct);
		layout.SetSpecialIcon(variant, MapIconState.Free, free);
		layout.SetSpecialIcon(variant, MapIconState.Occupied, occupied);
		return await SaveIconsAsync(layout, $"custom_article{variant}", ct);
	}

	/// <param name="category">Codice della categoria (template, umbrella…); null = tutte.</param>
	public async Task<IReadOnlyList<MapIconDto>> ListIconsAsync(string? category, CancellationToken ct = default)
	{
		MapIconCategoryCode? code = null;
		if (!string.IsNullOrEmpty(category))
			code = Codes.TryParse<MapIconCategoryCode>(category, out var parsed) ? parsed : throw DomainException.BadRequest(ValidationErrorCodes.Invalid);
		var icons = await _map.ListIconsAsync(code, ct);
		var refs = await _media.FindRefsAsync(icons.Select(i => i.MediaAssetId).Distinct().ToList(), ct);
		return icons.Select(i => new MapIconDto(
				i.PublicId, Codes.Of(i.Category!.Code), Codes.Of(i.Type), i.Name, _projection.Url(refs.GetValueOrDefault(i.MediaAssetId)), i.IsDefault))
			.ToList();
	}

	public async Task<IReadOnlyList<MapBeachElementDto>> ListBeachElementsAsync(CancellationToken ct = default)
	{
		var elements = await _map.ListBeachElementsAsync(true, ct);
		var refs = await _media.FindRefsAsync(elements.Where(e => e.MediaAssetId is not null).Select(e => e.MediaAssetId!.Value).ToList(), ct);
		return elements.Select(e => new MapBeachElementDto(
				e.PublicId, e.Name, e.MediaAssetId is { } id ? _projection.Url(refs.GetValueOrDefault(id)) : null))
			.ToList();
	}

	private async Task<MapSettingsDto> SaveIconsAsync(MapLayout layout, string article, CancellationToken ct)
	{
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.MapIconsUpdated, new Dictionary<string, object?> { ["article"] = article }, ct);
		return await _projection.SettingsAsync(layout, ct);
	}

	private async Task<(long? Free, long? Occupied)> PairAsync(SaveMapIconPairRequest request, CancellationToken ct)
	{
		long? free = request.FreeIconId is { } f ? (await IconAsync(f, MapIconCategoryCode.Umbrella, MapIconState.Free, ct)).MediaAssetId : null;
		long? occupied = request.OccupiedIconId is { } o ? (await IconAsync(o, MapIconCategoryCode.Umbrella, MapIconState.Occupied, ct)).MediaAssetId : null;
		return (free, occupied);
	}

	private async Task<MapIcon> IconAsync(Guid id, MapIconCategoryCode category, MapIconState? state, CancellationToken ct)
	{
		var icon = await _map.FindIconAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.MapIconNotFound);
		if (icon.Category?.Code != category || (state is not null && icon.Type != state))
			throw DomainException.Unprocessable(CatalogErrors.MapIconNotAllowed, new Dictionary<string, object?>
			{
				["category"] = Codes.Of(category),
				["state"] = state is { } s ? Codes.Of(s) : null,
			});
		return icon;
	}

	private async Task<MapLayout> LayoutAsync(long propertyId, CancellationToken ct)
	{
		var layout = await _map.FindLayoutAsync(propertyId, ct);
		if (layout is null)
		{
			layout = MapLayout.Create(propertyId, _user.UserId);
			_map.AddLayout(layout);
		}
		return layout;
	}

	private Task RecordAsync(string actionCode, IReadOnlyDictionary<string, object?> metadata, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.Map, _property.PublicId, metadata, ct);
}
