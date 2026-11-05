using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Systemadmin: pesi e periodi standard del prezzo suggerito, uguali per tutti gli stabilimenti.</summary>
public sealed class RevenueAdminService
{
	private readonly IRevenueRepository _repository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CatalogAudit _audit;
	private readonly RevenueWeightsDtoValidator _weightsValidator;
	private readonly SaveRevenueStandardPeriodsRequestValidator _periodsValidator;

	public RevenueAdminService(
		IRevenueRepository repository, IUnitOfWork unitOfWork, CatalogAudit audit, RevenueWeightsDtoValidator weightsValidator,
		SaveRevenueStandardPeriodsRequestValidator periodsValidator)
	{
		_repository = repository;
		_unitOfWork = unitOfWork;
		_audit = audit;
		_weightsValidator = weightsValidator;
		_periodsValidator = periodsValidator;
	}

	public async Task<RevenueWeightsDto> GetWeightsAsync(CancellationToken ct = default) => ToDto(await _repository.GetWeightsAsync(ct));

	public async Task<RevenueWeightsDto> UpdateWeightsAsync(RevenueWeightsDto request, CancellationToken ct = default)
	{
		_weightsValidator.EnsureValid(request);
		var weights = await _repository.GetWeightsAsync(ct);
		weights.Update(new RevenueWeightsData(request.RainLowSeasonFactor, request.RainHighSeasonFactor, request.BeforeWeekendFactor,
			request.HighSeasonFactor, request.FreeSpotsThresholdPercent, request.FreeSpotsAboveFactor, request.FreeSpotsBelowFactor));
		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordPlatformAsync(AuditActionCodes.Catalog.RevenueWeightsUpdated, AuditActionCodes.Catalog.Targets.Revenue,
			metadata: new Dictionary<string, object?> { ["weights"] = request }, ct: ct);
		return ToDto(weights);
	}

	public async Task<IReadOnlyList<RevenueStandardPeriodDto>> ListStandardPeriodsAsync(CancellationToken ct = default)
		=> (await _repository.ListStandardPeriodsAsync(ct)).Select(ToDto).ToList();

	public async Task<IReadOnlyList<RevenueStandardPeriodDto>> ReplaceStandardPeriodsAsync(SaveRevenueStandardPeriodsRequest request, CancellationToken ct = default)
	{
		_periodsValidator.EnsureValid(request);
		foreach (var old in await _repository.ListStandardPeriodsAsync(ct))
			_repository.RemoveStandardPeriod(old);
		short order = 0;
		foreach (var p in request.Periods.OrderBy(p => p.StartMonth).ThenBy(p => p.StartDay))
		{
			Codes.TryParse<RevenueSeasonTier>(p.Tier, out var tier);
			_repository.AddStandardPeriod(RevenueStandardPeriod.Create(p.Name, p.StartMonth, p.StartDay, p.EndMonth, p.EndDay, tier, order++));
		}
		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordPlatformAsync(AuditActionCodes.Catalog.RevenueStandardPeriodsUpdated, AuditActionCodes.Catalog.Targets.Revenue,
			metadata: new Dictionary<string, object?> { ["count"] = request.Periods.Count }, ct: ct);
		return await ListStandardPeriodsAsync(ct);
	}

	private static RevenueWeightsDto ToDto(RevenueWeights w) => new(
		w.RainLowSeasonFactor, w.RainHighSeasonFactor, w.BeforeWeekendFactor, w.HighSeasonFactor, w.FreeSpotsThresholdPercent,
		w.FreeSpotsAboveFactor, w.FreeSpotsBelowFactor);

	private static RevenueStandardPeriodDto ToDto(RevenueStandardPeriod p) => new(p.Name, p.StartMonth, p.StartDay, p.EndMonth, p.EndDay, Codes.Of(p.Tier));
}
