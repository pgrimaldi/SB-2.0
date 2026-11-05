using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Impostazioni › Servizi › Servizio tavoli (pranzo, cena). Assegnazione dei tavoli e prenotazioni sono di Booking.</summary>
public sealed class TableServicesService
{
	private readonly IServiceCatalogRepository _repository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly CatalogAudit _audit;
	private readonly SaveTableServiceRequestValidator _validator;
	private readonly BulkIdsRequestValidator _bulkValidator;

	public TableServicesService(
		IServiceCatalogRepository repository, IUnitOfWork unitOfWork, CurrentProperty property, CatalogAudit audit,
		SaveTableServiceRequestValidator validator, BulkIdsRequestValidator bulkValidator)
	{
		_bulkValidator = bulkValidator;
		_repository = repository;
		_unitOfWork = unitOfWork;
		_property = property;
		_audit = audit;
		_validator = validator;
	}

	public async Task<IReadOnlyList<TableServiceDto>> ListAsync(CancellationToken ct = default)
		=> (await _repository.ListTablesAsync(await _property.IdAsync(ct), ct)).Select(ToDto).ToList();

	public async Task<TableServiceDto> GetAsync(Guid id, CancellationToken ct = default) => ToDto(await LoadAsync(id, ct));

	public async Task<TableServiceDto> CreateAsync(SaveTableServiceRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var service = TableService.Create(await _property.IdAsync(ct));
		Apply(service, request);
		_repository.AddTable(service);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.TableServiceCreated, service, ct);
		return ToDto(service);
	}

	public async Task<TableServiceDto> UpdateAsync(Guid id, SaveTableServiceRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var service = await LoadAsync(id, ct);
		// Tipi di tavolo unici per posti e vincolo non differibile: prima si salvano le cancellazioni.
		service.ReplaceTableTypes([]);
		await _unitOfWork.SaveChangesAsync(ct);
		Apply(service, request);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.TableServiceUpdated, service, ct);
		return ToDto(service);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var service = await LoadAsync(id, ct);
		_repository.RemoveTable(service);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.TableServiceDeleted, service, ct);
	}

	public async Task<BulkResult> DeleteManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = await FindManyAsync(ids, ct);
		foreach (var service in found)
			_repository.RemoveTable(service);
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var service in found)
			await RecordAsync(AuditActionCodes.Catalog.TableServiceDeleted, service, ct);
		return Bulk(ids, found.Select(s => s.PublicId).ToHashSet());
	}

	/// <summary>Copia con il primo nome libero fra « copia», « copia 2»….</summary>
	public async Task<TableServiceDto> DuplicateAsync(Guid id, CancellationToken ct = default)
	{
		var source = await LoadAsync(id, ct);
		var copy = Copy(source, await TakenNamesAsync(ct));
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.TableServiceDuplicated, copy, ct);
		return ToDto(copy);
	}

	public async Task<BulkResult> DuplicateManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = await FindManyAsync(ids, ct);
		var taken = await TakenNamesAsync(ct);
		var copies = found.Select(s => Copy(s, taken)).ToList();
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var copy in copies)
			await RecordAsync(AuditActionCodes.Catalog.TableServiceDuplicated, copy, ct);
		return Bulk(ids, found.Select(s => s.PublicId).ToHashSet());
	}

	private TableService Copy(TableService source, HashSet<string> taken)
	{
		var copy = TableService.Create(source.PropertyId);
		Apply(copy, ToRequest(ToDto(source)) with { Name = CopyNames.Next(source.Name, TableService.NameMaxLength, taken) });
		_repository.AddTable(copy);
		return copy;
	}

	private async Task<List<TableService>> FindManyAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
	{
		var propertyId = await _property.IdAsync(ct);
		var found = new List<TableService>();
		foreach (var id in ids)
			if (await _repository.FindTableAsync(propertyId, id, ct) is { } service)
				found.Add(service);
		return found;
	}

	private async Task<HashSet<string>> TakenNamesAsync(CancellationToken ct)
		=> CopyNames.Taken((await _repository.ListTablesAsync(await _property.IdAsync(ct), ct)).Select(s => s.Name));

	private static SaveTableServiceRequest ToRequest(TableServiceDto d) => new(
		d.Name, d.Description, d.IsOnlineBookable, d.IsHighlighted, d.CapacityMode, d.TableTypes, d.TotalSeats, d.AverageMealMinutes,
		d.MaxBookingsPerSlot, d.MinSeatsPerBooking, d.MaxSeatsPerBooking, d.DepositMode, d.DepositType, d.DepositValue, d.Availabilities,
		d.Translations);

	private static BulkResult Bulk(IReadOnlyList<Guid> ids, IReadOnlySet<Guid> done)
		=> new(ids.Where(done.Contains).ToList(), ids.Where(i => !done.Contains(i)).Select(i => new BulkFailure(i, CatalogErrors.TableServiceNotFound, null)).ToList());

	private static void Apply(TableService service, SaveTableServiceRequest r)
	{
		Codes.TryParse<TableCapacityMode>(r.CapacityMode, out var mode);
		Codes.TryParse<DepositMode>(r.DepositMode, out var deposit);
		DepositType? depositType = r.DepositType is { } t && Codes.TryParse<DepositType>(t, out var parsed) ? parsed : null;
		service.Update(new TableServiceData(
			r.Name, ContentMapping.Clean(r.Description), r.IsOnlineBookable, r.IsHighlighted, mode, r.TotalSeats, r.AverageMealMinutes,
			r.MaxBookingsPerSlot, r.MinSeatsPerBooking, r.MaxSeatsPerBooking, deposit, depositType, r.DepositValue));
		service.SetTranslations(ContentMapping.Translations(r.Translations));
		service.ReplaceTableTypes((r.TableTypes ?? []).Select(x => (x.Seats, x.Count)));
		service.ReplaceAvailabilities((r.Availabilities ?? []).OrderBy(a => a.StartsOn).Select(a => new TableServiceAvailabilityData(
			a.StartsOn, a.EndsOn,
			a.Slots.Select(s => (s.Weekday is { } d && Codes.TryParse<Weekday>(d, out var w) ? w : (Weekday?)null, s.From, s.To)).ToList())));
	}

	private async Task<TableService> LoadAsync(Guid id, CancellationToken ct)
		=> await _repository.FindTableAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.TableServiceNotFound);

	private static TableServiceDto ToDto(TableService t) => new(
		t.PublicId, t.Name, t.Description, t.IsOnlineBookable, t.IsHighlighted, Codes.Of(t.CapacityMode),
		t.TableTypes.OrderBy(x => x.Seats).Select(x => new TableTypeDto(x.Seats, x.Count)).ToList(), t.TotalSeats, t.AverageMealMinutes,
		t.MaxBookingsPerSlot, t.MinSeatsPerBooking, t.MaxSeatsPerBooking, Codes.Of(t.DepositMode), t.DepositType is { } d ? Codes.Of(d) : null,
		t.DepositValue,
		t.Availabilities.OrderBy(a => a.StartsOn).Select(a => new TableAvailabilityDto(a.StartsOn, a.EndsOn,
			a.Slots.OrderBy(s => s.Weekday).ThenBy(s => s.From).Select(s => new TableSlotDto(s.Weekday is { } w ? Codes.Of(w) : null, s.From, s.To)).ToList()))
			.ToList(),
		t.Translations.OrderBy(x => x.Language).Select(x => ContentMapping.Dto(x.Language, x.Name, x.Description)).ToList());

	private Task RecordAsync(string actionCode, TableService service, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.TableService, service.PublicId,
			new Dictionary<string, object?> { ["name"] = service.Name }, ct);
}
