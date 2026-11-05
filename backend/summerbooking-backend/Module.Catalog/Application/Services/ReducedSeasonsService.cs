using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;

namespace Module.Catalog.Application.Services;

/// <summary>«Prezzo stagionale ridotto»: le voci dello stabilimento, poi prezzate in ogni pacchetto.</summary>
public sealed class ReducedSeasonsService
{
	private readonly IPriceListRepository _repository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly CatalogAudit _audit;
	private readonly SaveReducedSeasonRequestValidator _validator;

	public ReducedSeasonsService(
		IPriceListRepository repository, IUnitOfWork unitOfWork, CurrentProperty property, CatalogAudit audit, SaveReducedSeasonRequestValidator validator)
	{
		_repository = repository;
		_unitOfWork = unitOfWork;
		_property = property;
		_audit = audit;
		_validator = validator;
	}

	public async Task<IReadOnlyList<ReducedSeasonDto>> ListAsync(CancellationToken ct = default)
		=> (await _repository.ListReducedSeasonsAsync(await _property.IdAsync(ct), ct)).Select(ToDto).ToList();

	public async Task<ReducedSeasonDto> CreateAsync(SaveReducedSeasonRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var season = ReducedSeason.Create(await _property.IdAsync(ct), request.Name, request.StartsOn, request.EndsOn);
		_repository.AddReducedSeason(season);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ReducedSeasonCreated, season, ct);
		return ToDto(season);
	}

	public async Task<ReducedSeasonDto> UpdateAsync(Guid id, SaveReducedSeasonRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var season = await LoadAsync(id, ct);
		season.Update(request.Name, request.StartsOn, request.EndsOn);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ReducedSeasonUpdated, season, ct);
		return ToDto(season);
	}

	/// <summary>La voce sparisce da tutti i pacchetti con i suoi prezzi.</summary>
	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var season = await LoadAsync(id, ct);
		await _repository.RemoveReducedPricesAsync(season.Id, ct);
		_repository.RemoveReducedSeason(season);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ReducedSeasonDeleted, season, ct);
	}

	private async Task<ReducedSeason> LoadAsync(Guid id, CancellationToken ct)
		=> await _repository.FindReducedSeasonAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.ReducedSeasonNotFound);

	private static ReducedSeasonDto ToDto(ReducedSeason s) => new(s.PublicId, s.Name, s.StartsOn, s.EndsOn);

	private Task RecordAsync(string actionCode, ReducedSeason season, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.ReducedSeason, season.PublicId,
			new Dictionary<string, object?> { ["name"] = season.Name, ["startsOn"] = season.StartsOn, ["endsOn"] = season.EndsOn }, ct);
}
