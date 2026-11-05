using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;
using Shared.Contracts.Events;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Security;

namespace Module.Catalog.Application.Services;

/// <summary>
/// Form pubblico «Registra la tua struttura»: lo stabilimento nasce in attesa di approvazione, senza titolare; le foto
/// sono facoltative. Benvenuto al referente e avviso a info@ partono da Notification sull'evento.
/// </summary>
public sealed class PropertyRegistrationService
{
	public const int MaxPhotos = 10;

	private readonly IPropertyInfoRepository _repository;
	private readonly IMediaAssetRepository _assets;
	private readonly IPropertyFileStorage _files;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IHumanVerification _verification;
	private readonly IRequestContext _request;
	private readonly IRequestLanguage _language;
	private readonly IIntegrationEventPublisher _events;
	private readonly IDateTimeProvider _clock;
	private readonly RegisterPropertyRequestValidator _validator;

	public PropertyRegistrationService(
		IPropertyInfoRepository repository, IMediaAssetRepository assets, IPropertyFileStorage files, IUnitOfWork unitOfWork,
		IHumanVerification verification, IRequestContext request, IRequestLanguage language, IIntegrationEventPublisher events,
		IDateTimeProvider clock, RegisterPropertyRequestValidator validator)
	{
		_repository = repository;
		_assets = assets;
		_files = files;
		_unitOfWork = unitOfWork;
		_verification = verification;
		_request = request;
		_language = language;
		_events = events;
		_clock = clock;
		_validator = validator;
	}

	public async Task<PropertyRegisteredDto> RegisterAsync(RegisterPropertyRequest request, IReadOnlyList<UploadedFile> photos, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		if (photos.Count > MaxPhotos)
			throw DomainException.Unprocessable(CatalogErrors.TooManyFiles, new Dictionary<string, object?> { ["max"] = MaxPhotos });
		foreach (var photo in photos)
			UploadRules.EnsureImage(photo.ContentType, photo.Length);
		// il campo trappola compilato vale come verifica fallita, senza dire perché
		if (!string.IsNullOrWhiteSpace(request.Website) || !await _verification.VerifyAsync(request.VerificationToken, _request.ClientIp, ct))
			throw DomainException.Unprocessable(CatalogErrors.HumanVerificationFailed);

		var property = Property.Register(request.Name, request.City, request.ContactFullName, request.Email, request.Phone, request.PhoneCountryCode);
		_repository.AddProperty(property);
		await _unitOfWork.SaveChangesAsync(ct);

		// Foto senza riga di aggancio: appartengono allo stabilimento finché non apre la scheda pubblica.
		foreach (var photo in photos)
		{
			var stored = await _files.SaveAsync(photo.FileName, photo.Content, photo.ContentType, ct);
			_assets.Add(MediaAsset.Upload(stored.Ref, MediaKind.Photo, stored.Sha256, stored.ByteSize, stored.ContentType, property.Id));
		}
		if (photos.Count > 0)
			await _unitOfWork.SaveChangesAsync(ct);

		await _events.PublishAsync(new PropertyRegistered(
			Guid.CreateVersion7(), _clock.UtcNow, property.PublicId, property.Name, request.City.Trim(), request.ContactFullName.Trim(),
			request.Email.Trim(), $"{request.PhoneCountryCode}{request.Phone}", SupportedLanguages.Code(_language.Language), photos.Count), ct);
		return new PropertyRegisteredDto(property.PublicId);
	}
}
