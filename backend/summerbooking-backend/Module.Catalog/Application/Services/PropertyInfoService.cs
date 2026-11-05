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

public enum PropertyDocument
{
	Menu,
	PriceList,
}

/// <summary>Impostazioni › Informazioni stabilimento: la scheda pubblica gestita dallo stabilimento del token.</summary>
public sealed class PropertyInfoService
{
	private readonly IPropertyInfoRepository _repository;
	private readonly IPlatformRepository _platform;
	private readonly IMediaAssetRepository _assets;
	private readonly IPropertyFileStorage _files;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly CatalogMedia _media;
	private readonly SavePropertyInfoRequestValidator _validator;
	private readonly SetAmenitiesRequestValidator _amenitiesValidator;
	private readonly ReorderPhotosRequestValidator _reorderValidator;

	public PropertyInfoService(
		IPropertyInfoRepository repository, IPlatformRepository platform, IMediaAssetRepository assets, IPropertyFileStorage files, IUnitOfWork unitOfWork,
		CurrentProperty property, ICurrentUser user, CatalogAudit audit, CatalogMedia media, SavePropertyInfoRequestValidator validator,
		SetAmenitiesRequestValidator amenitiesValidator, ReorderPhotosRequestValidator reorderValidator)
	{
		_repository = repository;
		_platform = platform;
		_assets = assets;
		_files = files;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_media = media;
		_validator = validator;
		_amenitiesValidator = amenitiesValidator;
		_reorderValidator = reorderValidator;
	}

	public async Task<PropertyInfoDto> GetAsync(CancellationToken ct = default)
		=> await ToDtoAsync(await _repository.FindBathhouseAsync(await _property.IdAsync(ct), ct), ct);

	public async Task<PropertyInfoDto> SaveAsync(SavePropertyInfoRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var propertyId = await _property.IdAsync(ct);
		var property = await _repository.FindPropertyAsync(propertyId, ct) ?? throw DomainException.NotFound(CatalogErrors.PropertyNotFound);
		var bathhouse = await BathhouseAsync(propertyId, ct);

		short? countryId = null;
		if (request.CountryCode is { } code)
			countryId = await _repository.FindCountryIdAsync(code.ToUpperInvariant(), ct) ?? throw DomainException.Unprocessable(CatalogErrors.CountryNotFound);

		Codes.TryParse<PetsPolicy>(request.PetsPolicy, out var pets);
		bathhouse.Update(new BathhouseInfo(
			request.Name, Clean(request.Description), Clean(request.Regulation), Clean(request.Address), Clean(request.City), Clean(request.Cap),
			Clean(request.Province), Clean(request.Region), countryId, request.Latitude, request.Longitude, Clean(request.Phone), Clean(request.Email),
			Clean(request.Website), Clean(request.Facebook), Clean(request.Instagram), Clean(request.Whatsapp), request.OpenHoursFrom, request.OpenHoursTo,
			request.IsFreeCancellation, request.CancellationNoticeHours, pets, Clean(request.VirtualTourUrl)), _user.UserId);

		if (request.Slug is { } slug)
		{
			if (slug != bathhouse.Slug && await _repository.SlugExistsAsync(slug, bathhouse.Id, ct))
				throw DomainException.Conflict(CatalogErrors.SlugTaken, new Dictionary<string, object?> { ["slug"] = slug });
			bathhouse.SetSlug(slug);
		}
		else if (bathhouse.Slug is null)
			bathhouse.SetSlug(await FreeSlugAsync(request.Name, bathhouse.Id, ct));

		property.Rename(request.Name);
		property.SetCountry(countryId ?? property.CountryId);
		bathhouse.AssignRegionAutomatically((await _platform.ListRegionsAsync(ct)).FirstOrDefault(r => r.Matches(bathhouse.Region))?.Id);

		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PropertyInfoUpdated, new Dictionary<string, object?> { ["name"] = bathhouse.Name, ["slug"] = bathhouse.Slug }, ct);
		return await ToDtoAsync(bathhouse, ct);
	}

	public async Task<PropertyInfoDto> SetAmenitiesAsync(SetAmenitiesRequest request, CancellationToken ct = default)
	{
		_amenitiesValidator.EnsureValid(request);
		var codes = request.Codes.Distinct().ToList();
		var ids = await _repository.FindAmenityIdsAsync(codes, ct);
		var unknown = codes.Where(c => !ids.ContainsKey(c)).ToList();
		if (unknown.Count > 0)
			throw DomainException.Unprocessable(CatalogErrors.AmenityNotFound, new Dictionary<string, object?> { ["codes"] = unknown });

		var bathhouse = await BathhouseAsync(await _property.IdAsync(ct), ct);
		bathhouse.SetAmenities(ids.Values.ToList());
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PropertyAmenitiesUpdated, new Dictionary<string, object?> { ["codes"] = codes }, ct);
		return await ToDtoAsync(bathhouse, ct);
	}

	public async Task<PropertyInfoDto> UploadMainPhotoAsync(UploadedFile file, CancellationToken ct = default)
	{
		UploadRules.EnsureImage(file.ContentType, file.Length);
		var (bathhouse, asset) = await StoreAsync(file, MediaKind.Photo, ct);
		bathhouse.SetMainPhoto(asset.Id);
		return await SavePhotosAsync(bathhouse, "main_photo_uploaded", ct);
	}

	public async Task<PropertyInfoDto> RemoveMainPhotoAsync(CancellationToken ct = default)
	{
		var bathhouse = await BathhouseAsync(await _property.IdAsync(ct), ct);
		bathhouse.SetMainPhoto(null);
		return await SavePhotosAsync(bathhouse, "main_photo_removed", ct);
	}

	public async Task<PropertyInfoDto> AddPhotoAsync(UploadedFile file, CancellationToken ct = default)
	{
		UploadRules.EnsureImage(file.ContentType, file.Length);
		var current = await _repository.FindBathhouseAsync(await _property.IdAsync(ct), ct);
		if (current?.Photos.Count >= Bathhouse.MaxPhotos)
			throw DomainException.Unprocessable(CatalogErrors.GalleryFull, new Dictionary<string, object?> { ["max"] = Bathhouse.MaxPhotos });

		var (bathhouse, asset) = await StoreAsync(file, MediaKind.Photo, ct);
		bathhouse.AddPhoto(asset.Id);
		return await SavePhotosAsync(bathhouse, "photo_added", ct);
	}

	public async Task<PropertyInfoDto> ReorderPhotosAsync(ReorderPhotosRequest request, CancellationToken ct = default)
	{
		_reorderValidator.EnsureValid(request);
		var bathhouse = await BathhouseAsync(await _property.IdAsync(ct), ct);
		var photos = bathhouse.Photos.ToDictionary(p => p.PublicId);
		if (request.Ids.Count != photos.Count || request.Ids.Any(id => !photos.ContainsKey(id)))
			throw DomainException.Unprocessable(CatalogErrors.PhotoNotFound);

		// La posizione è unica nella galleria e il vincolo non è differibile: prima fuori dall'intervallo, poi al posto giusto.
		foreach (var photo in photos.Values)
			photo.MoveTo((short)(photo.SortOrder + Bathhouse.MaxPhotos + 1000));
		await _unitOfWork.SaveChangesAsync(ct);
		for (var i = 0; i < request.Ids.Count; i++)
			photos[request.Ids[i]].MoveTo((short)i);
		return await SavePhotosAsync(bathhouse, "photos_reordered", ct);
	}

	public async Task<PropertyInfoDto> DeletePhotoAsync(Guid id, CancellationToken ct = default)
	{
		var bathhouse = await BathhouseAsync(await _property.IdAsync(ct), ct);
		var photo = bathhouse.Photos.FirstOrDefault(p => p.PublicId == id) ?? throw DomainException.NotFound(CatalogErrors.PhotoNotFound);
		bathhouse.Photos.Remove(photo);
		return await SavePhotosAsync(bathhouse, "photo_deleted", ct);
	}

	public async Task<PropertyInfoDto> UploadDocumentAsync(PropertyDocument document, UploadedFile file, CancellationToken ct = default)
	{
		UploadRules.EnsurePdf(file.ContentType, file.Length);
		var (bathhouse, asset) = await StoreAsync(file, MediaKind.Pdf, ct);
		SetDocument(bathhouse, document, asset.Id);
		return await SaveDocumentsAsync(bathhouse, document, "uploaded", ct);
	}

	public async Task<PropertyInfoDto> RemoveDocumentAsync(PropertyDocument document, CancellationToken ct = default)
	{
		var bathhouse = await BathhouseAsync(await _property.IdAsync(ct), ct);
		SetDocument(bathhouse, document, null);
		return await SaveDocumentsAsync(bathhouse, document, "removed", ct);
	}

	private static void SetDocument(Bathhouse bathhouse, PropertyDocument document, long? assetId)
	{
		if (document == PropertyDocument.Menu)
			bathhouse.SetMenuPdf(assetId);
		else
			bathhouse.SetPriceListPdf(assetId);
	}

	private async Task<(Bathhouse Bathhouse, MediaAsset Asset)> StoreAsync(UploadedFile file, MediaKind kind, CancellationToken ct)
	{
		var propertyId = await _property.IdAsync(ct);
		var bathhouse = await BathhouseAsync(propertyId, ct);
		var stored = await _files.SaveAsync(file.FileName, file.Content, file.ContentType, ct);
		var asset = MediaAsset.Upload(stored.Ref, kind, stored.Sha256, stored.ByteSize, stored.ContentType, propertyId);
		_assets.Add(asset);
		await _unitOfWork.SaveChangesAsync(ct);
		return (bathhouse, asset);
	}

	private async Task<PropertyInfoDto> SavePhotosAsync(Bathhouse bathhouse, string operation, CancellationToken ct)
	{
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PropertyPhotosUpdated, new Dictionary<string, object?> { ["operation"] = operation }, ct);
		return await ToDtoAsync(bathhouse, ct);
	}

	private async Task<PropertyInfoDto> SaveDocumentsAsync(Bathhouse bathhouse, PropertyDocument document, string operation, CancellationToken ct)
	{
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PropertyDocumentsUpdated, new Dictionary<string, object?>
		{
			["document"] = document == PropertyDocument.Menu ? "menu" : "price_list",
			["operation"] = operation,
		}, ct);
		return await ToDtoAsync(bathhouse, ct);
	}

	/// <summary>La scheda nasce al primo salvataggio.</summary>
	private async Task<Bathhouse> BathhouseAsync(long propertyId, CancellationToken ct)
	{
		var bathhouse = await _repository.FindBathhouseAsync(propertyId, ct);
		if (bathhouse is null)
		{
			bathhouse = Bathhouse.Create(propertyId, _user.UserId);
			_repository.AddBathhouse(bathhouse);
		}
		return bathhouse;
	}

	private async Task<string> FreeSlugAsync(string name, long exceptId, CancellationToken ct)
	{
		var baseSlug = Slugs.From(name);
		var slug = baseSlug;
		for (var i = 2; await _repository.SlugExistsAsync(slug, exceptId, ct); i++)
			slug = $"{baseSlug}-{i}";
		return slug;
	}

	/// <summary>Informazioni della vetrina come le vede il gestore; le usa anche la scheda pubblica.</summary>
	public Task<PropertyInfoDto> DescribeAsync(Bathhouse? b, CancellationToken ct = default) => ToDtoAsync(b, ct);

	private async Task<PropertyInfoDto> ToDtoAsync(Bathhouse? b, CancellationToken ct)
	{
		if (b is null)
			return new PropertyInfoDto(
				null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
				false, null, Codes.Of(PetsPolicy.OnRequest), null, null, [], null, null, []);

		var photos = b.Photos.OrderBy(p => p.SortOrder).ToList();
		var urls = await _media.UrlsAsync(photos.Select(p => (long?)p.MediaAssetId).Append(b.MainPhotoId).Append(b.MenuPdfId).Append(b.PriceListPdfId), ct);
		var refs = await _assets.FindRefsAsync(new[] { b.MenuPdfId, b.PriceListPdfId }.Where(i => i is not null).Select(i => i!.Value).ToList(), ct);

		DocumentDto? Document(long? id) => id is { } value
			? new DocumentDto(refs.TryGetValue(value, out var r) ? Path.GetFileName(r.Path) : "file.pdf", urls.UrlOf(value))
			: null;

		return new PropertyInfoDto(
			b.Name, b.Slug, b.Description, b.Regulation, b.Address, b.City, b.Cap, b.Province, b.Region,
			b.CountryId is { } countryId ? await _repository.FindCountryCodeAsync(countryId, ct) : null,
			b.Latitude, b.Longitude, b.Phone, b.Email, b.Website, b.Facebook, b.Instagram, b.Whatsapp, b.OpenHoursFrom, b.OpenHoursTo,
			b.CancellationPolicy.IsFreeCancellation, b.CancellationPolicy.NoticeHours, Codes.Of(b.PetsPolicy), b.VirtualTourUrl,
			urls.UrlOf(b.MainPhotoId), photos.Select(p => new PhotoDto(p.PublicId, urls.UrlOf(p.MediaAssetId), p.SortOrder)).ToList(),
			Document(b.MenuPdfId), Document(b.PriceListPdfId),
			await _repository.FindAmenityCodesAsync(b.Amenities.Select(a => a.AmenityId).ToList(), ct));
	}

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	private Task RecordAsync(string actionCode, IReadOnlyDictionary<string, object?> metadata, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.Property, _property.PublicId, metadata, ct);
}
