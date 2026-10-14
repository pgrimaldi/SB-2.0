using BuildingBlocks.Application.Validation;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Validators;
using Module.Identity.Domain;
using Module.Identity.Domain.Entities;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;

namespace Module.Identity.Application.Services;

/// <summary>Piani di licenza: lettura pubblica dei piani attivi, gestione dal systemadmin (identificati dal codice).</summary>
public sealed class LicensePlanService
{
	private readonly ILicensePlanRepository _plans;
	private readonly IUnitOfWork _uow;
	private readonly IAuditTrail _audit;
	private readonly CreateLicensePlanRequestValidator _createValidator;
	private readonly UpdateLicensePlanRequestValidator _updateValidator;

	public LicensePlanService(
		ILicensePlanRepository plans, IUnitOfWork uow, IAuditTrail audit,
		CreateLicensePlanRequestValidator createValidator, UpdateLicensePlanRequestValidator updateValidator)
	{
		_plans = plans;
		_uow = uow;
		_audit = audit;
		_createValidator = createValidator;
		_updateValidator = updateValidator;
	}

	public async Task<IReadOnlyList<LicensePlanDto>> ListAsync(bool onlyActive, CancellationToken ct = default)
		=> (await _plans.ListAsync(onlyActive, ct)).Select(LicensePlanDto.FromEntity).ToList();

	public async Task<LicensePlanDto> CreateAsync(CreateLicensePlanRequest request, CancellationToken ct = default)
	{
		_createValidator.EnsureValid(request);
		if (await _plans.GetByCodeAsync(request.Code, ct) is not null)
			throw DomainException.Conflict(IdentityErrors.LicensePlanCodeTaken);

		var plan = LicensePlan.Create(request.Code, request.DurationMonths, request.SortOrder);
		foreach (var t in request.Translations)
			plan.SetTranslation(t.Culture.Trim(), t.Label.Trim(), t.Description);
		_plans.Add(plan);
		return await SaveAsync(plan, ct);
	}

	public async Task<LicensePlanDto> UpdateAsync(string code, UpdateLicensePlanRequest request, CancellationToken ct = default)
	{
		_updateValidator.EnsureValid(request);
		var plan = await _plans.GetByCodeAsync(code, ct) ?? throw DomainException.NotFound(IdentityErrors.LicensePlanNotFound);

		plan.Update(request.DurationMonths, request.SortOrder, request.IsActive);
		foreach (var t in request.Translations)
			plan.SetTranslation(t.Culture.Trim(), t.Label.Trim(), t.Description);
		return await SaveAsync(plan, ct);
	}

	private async Task<LicensePlanDto> SaveAsync(LicensePlan plan, CancellationToken ct)
	{
		await _uow.SaveChangesAsync(ct);
		await _audit.RecordAsync(new AuditEntry(
			AuditCategory.Platform, IdentityAudit.Module, IdentityAudit.LicensePlanSaved,
			Metadata: new Dictionary<string, object?> { ["code"] = plan.Code, ["is_active"] = plan.IsActive }), ct);
		return LicensePlanDto.FromEntity(plan);
	}
}
