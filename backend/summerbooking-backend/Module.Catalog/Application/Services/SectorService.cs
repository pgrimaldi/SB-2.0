using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Security;

namespace Module.Catalog.Application.Services;

/// <summary>Impostazioni › Settori e file (settore e fila sono la stessa cosa).</summary>
public sealed class SectorService
{
	private readonly ISectorRepository _sectors;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly SaveSectorRequestValidator _validator;

	public SectorService(
		ISectorRepository sectors, IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user, CatalogAudit audit,
		SaveSectorRequestValidator validator)
	{
		_sectors = sectors;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_validator = validator;
	}

	public async Task<IReadOnlyList<SectorDto>> ListAsync(CancellationToken ct = default)
		=> (await _sectors.ListAsync(await _property.IdAsync(ct), ct)).Select(SectorDto.FromEntity).ToList();

	public async Task<SectorDto> CreateAsync(SaveSectorRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var propertyId = await _property.IdAsync(ct);
		await EnsureNameFreeAsync(propertyId, request.Name, null, ct);

		var sector = MapSector.Create(propertyId, request.Name, await _sectors.NextSortOrderAsync(propertyId, ct), _user.UserId);
		_sectors.Add(sector);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.SectorCreated, sector, ct);
		return SectorDto.FromEntity(sector);
	}

	public async Task<SectorDto> UpdateAsync(Guid id, SaveSectorRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var sector = await LoadAsync(id, ct);
		await EnsureNameFreeAsync(sector.PropertyId, request.Name, sector.Id, ct);

		sector.Rename(request.Name);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.SectorUpdated, sector, ct);
		return SectorDto.FromEntity(sector);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var sector = await LoadAsync(id, ct);
		var usage = await _sectors.CountUsagesAsync(sector.Id, ct);
		if (usage.Any)
			throw DomainException.Conflict(CatalogErrors.SectorInUse, new Dictionary<string, object?>
			{
				["mapSpots"] = usage.MapSpots,
				["priceLists"] = usage.PriceLists,
				["stayLimits"] = usage.StayLimits,
				["discounts"] = usage.Discounts,
			});

		_sectors.Remove(sector);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.SectorDeleted, sector, ct);
	}

	private async Task<MapSector> LoadAsync(Guid id, CancellationToken ct)
		=> await _sectors.FindAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.SectorNotFound);

	private async Task EnsureNameFreeAsync(long propertyId, string name, long? exceptId, CancellationToken ct)
	{
		if (await _sectors.NameExistsAsync(propertyId, name.Trim(), exceptId, ct))
			throw DomainException.Conflict(CatalogErrors.SectorNameTaken);
	}

	private Task RecordAsync(string actionCode, MapSector sector, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.Sector, sector.PublicId,
			new Dictionary<string, object?> { ["name"] = sector.Name }, ct);
}
