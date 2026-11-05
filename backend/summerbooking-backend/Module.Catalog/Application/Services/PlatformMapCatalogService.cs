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

/// <summary>
/// Systemadmin: catalogo delle icone della mappa ed elementi spiaggia. Le mappe restano collegate al catalogo: cambiando
/// l'immagine di un'icona cambia su tutte; eliminandola, le mappe che la usano passano all'icona predefinita.
/// </summary>
public sealed class PlatformMapCatalogService
{
	private readonly IPlatformRepository _repository;
	private readonly IMediaAssetRepository _assets;
	private readonly IPropertyFileStorage _files;
	private readonly CatalogMedia _media;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CatalogAudit _audit;
	private readonly UpdateMapIconRequestValidator _iconValidator;
	private readonly UpdateBeachElementRequestValidator _elementValidator;

	public PlatformMapCatalogService(
		IPlatformRepository repository, IMediaAssetRepository assets, IPropertyFileStorage files, CatalogMedia media, IUnitOfWork unitOfWork,
		CatalogAudit audit, UpdateMapIconRequestValidator iconValidator, UpdateBeachElementRequestValidator elementValidator)
	{
		_repository = repository;
		_assets = assets;
		_files = files;
		_media = media;
		_unitOfWork = unitOfWork;
		_audit = audit;
		_iconValidator = iconValidator;
		_elementValidator = elementValidator;
	}

	public async Task<IReadOnlyList<AdminMapIconDto>> ListIconsAsync(string? category, CancellationToken ct = default)
	{
		MapIconCategoryCode? code = null;
		if (category is not null)
			code = Codes.TryParse<MapIconCategoryCode>(category, out var parsed) ? parsed : throw DomainException.NotFound(CatalogErrors.MapIconCategoryNotFound);
		var icons = await _repository.ListIconsAsync(code, ct);
		var urls = await _media.UrlsAsync(icons.Select(i => (long?)i.MediaAssetId), ct);
		return icons.Select(i => ToDto(i, urls)).ToList();
	}

	/// <summary>Nuova icona del catalogo; se è la prima della categoria e dello stato diventa la predefinita.</summary>
	public async Task<AdminMapIconDto> CreateIconAsync(string category, string state, string name, UploadedFile file, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MapIcon.NameMaxLength || !Codes.TryParse<MapIconState>(state, out var type))
			throw new ValidationFailedException(string.IsNullOrWhiteSpace(name) ? "name" : "state", ValidationErrorCodes.Invalid);
		var categoryRow = (Codes.TryParse<MapIconCategoryCode>(category, out var code) ? await _repository.FindIconCategoryAsync(code, ct) : null)
			?? throw DomainException.NotFound(CatalogErrors.MapIconCategoryNotFound);
		var asset = await UploadAsync(file, ct);
		var icon = MapIcon.Create(categoryRow.Id, type, name, asset.Id);
		if ((await _repository.ListDefaultIconsAsync(categoryRow.Id, type, ct)).Count == 0)
			icon.SetDefault(true);
		_repository.AddIcon(icon);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformMapIconSaved, AuditActionCodes.Catalog.Targets.MapIcon, icon.PublicId, icon.Name, ct);
		return await GetIconAsync(icon.PublicId, ct);
	}

	public async Task<AdminMapIconDto> UpdateIconAsync(Guid id, UpdateMapIconRequest request, CancellationToken ct = default)
	{
		_iconValidator.EnsureValid(request);
		var icon = await LoadIconAsync(id, ct);
		if (icon.IsDefault && !request.IsDefault)
			throw DomainException.Unprocessable(CatalogErrors.MapIconDefaultRequired);
		if (request.IsDefault && !icon.IsDefault)
		{
			// l'indice della predefinita non è differibile: prima si toglie la vecchia
			foreach (var other in await _repository.ListDefaultIconsAsync(icon.CategoryId, icon.Type, ct))
				other.SetDefault(false);
			await _unitOfWork.SaveChangesAsync(ct);
		}
		icon.Update(request.Name, request.IsActive);
		icon.SetDefault(request.IsDefault);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformMapIconSaved, AuditActionCodes.Catalog.Targets.MapIcon, icon.PublicId, icon.Name, ct);
		return await GetIconAsync(icon.PublicId, ct);
	}

	/// <summary>Nuova immagine: le mappe che usavano la vecchia passano alla nuova.</summary>
	public async Task<AdminMapIconDto> ReplaceIconImageAsync(Guid id, UploadedFile file, CancellationToken ct = default)
	{
		var icon = await LoadIconAsync(id, ct);
		var previous = icon.MediaAssetId;
		var asset = await UploadAsync(file, ct);
		icon.ReplaceImage(asset.Id);
		await _unitOfWork.SaveChangesAsync(ct);
		await _repository.RepointAssetAsync(previous, asset.Id, ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformMapIconSaved, AuditActionCodes.Catalog.Targets.MapIcon, icon.PublicId, icon.Name, ct);
		return await GetIconAsync(icon.PublicId, ct);
	}

	/// <summary>Elimina un'icona non predefinita: le mappe che la usano passano alla predefinita della stessa categoria e stato.</summary>
	public async Task DeleteIconAsync(Guid id, CancellationToken ct = default)
	{
		var icon = await LoadIconAsync(id, ct);
		if (icon.IsDefault)
			throw DomainException.Conflict(CatalogErrors.MapIconIsDefault);
		var fallback = (await _repository.ListDefaultIconsAsync(icon.CategoryId, icon.Type, ct)).FirstOrDefault()
			?? throw DomainException.Unprocessable(CatalogErrors.MapIconDefaultRequired);
		_repository.RemoveIcon(icon);
		await _unitOfWork.SaveChangesAsync(ct);
		var repointed = await _repository.RepointAssetAsync(icon.MediaAssetId, fallback.MediaAssetId, ct);
		await _audit.RecordPlatformAsync(AuditActionCodes.Catalog.PlatformMapIconDeleted, AuditActionCodes.Catalog.Targets.MapIcon, icon.PublicId,
			metadata: new Dictionary<string, object?> { ["name"] = icon.Name, ["repointed"] = repointed }, ct: ct);
	}

	public async Task<IReadOnlyList<AdminBeachElementDto>> ListBeachElementsAsync(CancellationToken ct = default)
	{
		var elements = await _repository.ListBeachElementsAsync(ct);
		var usages = await _repository.CountBeachElementUsagesAsync(ct);
		var urls = await _media.UrlsAsync(elements.Select(e => e.MediaAssetId), ct);
		return elements.Select(e => new AdminBeachElementDto(e.PublicId, e.Name, urls.UrlOf(e.MediaAssetId), e.SortOrder, e.IsActive, usages.GetValueOrDefault(e.Id))).ToList();
	}

	public async Task<AdminBeachElementDto> CreateBeachElementAsync(UpdateBeachElementRequest request, UploadedFile? file, CancellationToken ct = default)
	{
		_elementValidator.EnsureValid(request);
		long? assetId = file is null ? null : (await UploadAsync(file, ct)).Id;
		var element = MapBeachElement.Create(request.Name.Trim(), request.SortOrder, assetId);
		element.Update(request.Name.Trim(), assetId, request.IsActive);
		_repository.AddBeachElement(element);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformBeachElementSaved, AuditActionCodes.Catalog.Targets.BeachElement, element.PublicId, element.Name, ct);
		return await GetBeachElementAsync(element.PublicId, ct);
	}

	public async Task<AdminBeachElementDto> UpdateBeachElementAsync(Guid id, UpdateBeachElementRequest request, CancellationToken ct = default)
	{
		_elementValidator.EnsureValid(request);
		var element = await LoadBeachElementAsync(id, ct);
		element.Update(request.Name.Trim(), element.MediaAssetId, request.IsActive);
		element.Reorder(request.SortOrder);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformBeachElementSaved, AuditActionCodes.Catalog.Targets.BeachElement, element.PublicId, element.Name, ct);
		return await GetBeachElementAsync(element.PublicId, ct);
	}

	public async Task<AdminBeachElementDto> ReplaceBeachElementImageAsync(Guid id, UploadedFile file, CancellationToken ct = default)
	{
		var element = await LoadBeachElementAsync(id, ct);
		var asset = await UploadAsync(file, ct);
		element.Update(element.Name, asset.Id, element.IsActive);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PlatformBeachElementSaved, AuditActionCodes.Catalog.Targets.BeachElement, element.PublicId, element.Name, ct);
		return await GetBeachElementAsync(element.PublicId, ct);
	}

	/// <summary>Solo se nessuna mappa lo usa: altrimenti si disattiva (sparisce dall'elenco del gestore, resta sulle mappe).</summary>
	public async Task DeleteBeachElementAsync(Guid id, CancellationToken ct = default)
	{
		var element = await LoadBeachElementAsync(id, ct);
		var used = (await _repository.CountBeachElementUsagesAsync(ct)).GetValueOrDefault(element.Id);
		if (used > 0)
			throw DomainException.Conflict(CatalogErrors.MapBeachElementInUse, new Dictionary<string, object?> { ["usageCount"] = used });
		_repository.RemoveBeachElement(element);
		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordPlatformAsync(AuditActionCodes.Catalog.PlatformBeachElementDeleted, AuditActionCodes.Catalog.Targets.BeachElement, element.PublicId,
			metadata: new Dictionary<string, object?> { ["name"] = element.Name }, ct: ct);
	}

	private async Task<MediaAsset> UploadAsync(UploadedFile file, CancellationToken ct)
	{
		UploadRules.EnsureImage(file.ContentType, file.Length);
		var stored = await _files.SaveAsync(file.FileName, file.Content, file.ContentType, ct);
		var asset = MediaAsset.UploadShared(stored.Ref, MediaKind.Icon, stored.Sha256, stored.ByteSize, stored.ContentType);
		_assets.Add(asset);
		await _unitOfWork.SaveChangesAsync(ct);
		return asset;
	}

	private async Task<AdminMapIconDto> GetIconAsync(Guid id, CancellationToken ct)
	{
		var icon = await LoadIconAsync(id, ct);
		return ToDto(icon, await _media.UrlsAsync([icon.MediaAssetId], ct));
	}

	private async Task<AdminBeachElementDto> GetBeachElementAsync(Guid id, CancellationToken ct)
		=> (await ListBeachElementsAsync(ct)).First(e => e.Id == id);

	private async Task<MapIcon> LoadIconAsync(Guid id, CancellationToken ct)
		=> await _repository.FindIconAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.MapIconNotFound);

	private async Task<MapBeachElement> LoadBeachElementAsync(Guid id, CancellationToken ct)
		=> await _repository.FindBeachElementAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.MapBeachElementNotFound);

	private static AdminMapIconDto ToDto(MapIcon i, IReadOnlyDictionary<long, string?> urls)
		=> new(i.PublicId, Codes.Of(i.Category!.Code), Codes.Of(i.Type), i.Name, urls.UrlOf(i.MediaAssetId), i.IsActive, i.IsDefault);

	private Task RecordAsync(string actionCode, string target, Guid id, string name, CancellationToken ct)
		=> _audit.RecordPlatformAsync(actionCode, target, id, metadata: new Dictionary<string, object?> { ["name"] = name }, ct: ct);
}
