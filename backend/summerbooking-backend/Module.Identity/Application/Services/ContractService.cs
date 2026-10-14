using BuildingBlocks.Application.Validation;
using System.Net;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Validators;
using Module.Identity.Domain;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Infrastructure;
using Shared.Security;

namespace Module.Identity.Application.Services;

/// <summary>
/// Documenti contrattuali B2B: il systemadmin carica e pubblica le versioni, il titolare accetta o revoca.
/// Ogni accettazione è un evento nuovo (registro append-only), con versione e hash del momento.
/// </summary>
/// <remarks>
/// Il documento "in vigore" è l'ultima versione pubblicata. L'attivazione per singolo stabilimento
/// è in Catalog e non è ancora collegata: PROMEMORIA-85-5.
/// </remarks>
public sealed class ContractService
{
	private readonly IContractRepository _contracts;
	private readonly IDocumentStorage _storage;
	private readonly CurrentAccount _current;
	private readonly IUnitOfWork _uow;
	private readonly IAuditTrail _audit;
	private readonly IRequestContext _request;
	private readonly IDateTimeProvider _clock;
	private readonly CreateContractDocumentRequestValidator _validator;

	public ContractService(
		IContractRepository contracts, IDocumentStorage storage, CurrentAccount current, IUnitOfWork uow,
		IAuditTrail audit, IRequestContext request, IDateTimeProvider clock, CreateContractDocumentRequestValidator validator)
	{
		_contracts = contracts;
		_storage = storage;
		_current = current;
		_uow = uow;
		_audit = audit;
		_request = request;
		_clock = clock;
		_validator = validator;
	}

	public async Task<IReadOnlyList<ContractDocumentDto>> ListDocumentsAsync(CancellationToken ct = default)
		=> (await _contracts.ListDocumentsAsync(ct)).Select(ContractDocumentDto.FromEntity).ToList();

	/// <summary>Carica il PDF come bozza; l'hash del contenuto si calcola qui, una volta.</summary>
	public async Task<ContractDocumentDto> UploadAsync(CreateContractDocumentRequest request, string fileName, Stream content, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
			throw new ValidationFailedException("file", ValidationErrorCodes.InvalidFileType);
		if (await _contracts.VersionExistsAsync(request.VersionCode.Trim(), ct))
			throw DomainException.Conflict(IdentityErrors.ContractVersionTaken);

		var stored = await _storage.SaveAsync(fileName, content, ct);
		var document = ContractDocument.CreateDraft(request.VersionCode, request.Title?.Trim(), stored.Origin, stored.Path, stored.Sha256, request.Notes?.Trim());
		_contracts.AddDocument(document);
		await _uow.SaveChangesAsync(ct);

		await RecordAsync(AuditCategory.Platform, IdentityAudit.ContractUploaded, document, null, ct);
		return ContractDocumentDto.FromEntity(document);
	}

	public async Task<ContractDocumentDto> PublishAsync(Guid documentId, CancellationToken ct = default)
	{
		var document = await _contracts.GetDocumentAsync(documentId, ct) ?? throw DomainException.NotFound(IdentityErrors.ContractNotFound);
		var current = await _contracts.GetPublishedAsync(ct);
		document.Publish(current, _clock.UtcNow);
		await _uow.SaveChangesAsync(ct);

		await RecordAsync(AuditCategory.Platform, IdentityAudit.ContractPublished, document, null, ct);
		return ContractDocumentDto.FromEntity(document);
	}

	/// <summary>Documento in vigore con lo stato di accettazione dello stabilimento; null se non ce n'è.</summary>
	public async Task<CurrentContractDto?> GetCurrentAsync(CancellationToken ct = default)
	{
		var propertyId = _current.RequirePropertyId();
		var document = await _contracts.GetPublishedAsync(ct);
		if (document is null)
			return null;

		var last = await _contracts.GetLastEventAsync(propertyId, document.Id, ct);
		var accepted = last?.EventType == ContractAcceptanceEventType.Accepted;
		return new CurrentContractDto(
			document.PublicId, document.VersionCode, document.Title, document.PublishedAt,
			_storage.GetDownloadUrl(document.DocumentOrigin, document.DocumentPath),
			accepted, accepted ? last!.OccurredAt : null);
	}

	public async Task<CurrentContractDto> SetAcceptanceAsync(Guid documentId, SetContractAcceptanceRequest request, CancellationToken ct = default)
	{
		var propertyId = _current.RequirePropertyId();
		var actor = await _current.GetAsync(ct);
		var document = await _contracts.GetDocumentAsync(documentId, ct) ?? throw DomainException.NotFound(IdentityErrors.ContractNotFound);

		var type = request.Accepted ? ContractAcceptanceEventType.Accepted : ContractAcceptanceEventType.Revoked;
		var last = await _contracts.GetLastEventAsync(propertyId, document.Id, ct);
		if (last?.EventType != type)
		{
			var ip = IPAddress.TryParse(_request.ClientIp, out var parsed) ? parsed : null;
			_contracts.AddEvent(ContractAcceptanceEvent.Record(propertyId, document, type, actor.Id, _clock.UtcNow, ip, _request.UserAgent));
			await _uow.SaveChangesAsync(ct);
			await RecordAsync(AuditCategory.Operator, IdentityAudit.ContractAcceptanceChanged, document, request.Accepted, ct);
		}

		return await GetCurrentAsync(ct)
			?? throw DomainException.NotFound(IdentityErrors.ContractNotFound);
	}

	private Task RecordAsync(AuditCategory category, string action, ContractDocument document, bool? accepted, CancellationToken ct)
		=> _audit.RecordAsync(new AuditEntry(
			category, IdentityAudit.Module, action, TargetType: IdentityAudit.Targets.ContractDocument, TargetId: document.PublicId,
			Metadata: new Dictionary<string, object?> { ["version"] = document.VersionCode, ["accepted"] = accepted }), ct);
}
