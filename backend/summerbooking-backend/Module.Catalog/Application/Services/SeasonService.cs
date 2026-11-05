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

/// <summary>Impostazioni › Stagioni e periodi.</summary>
public sealed class SeasonService
{
	private readonly ISeasonRepository _seasons;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly SaveSeasonRequestValidator _seasonValidator;
	private readonly SaveTariffPeriodRequestValidator _periodValidator;

	public SeasonService(
		ISeasonRepository seasons, IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user, CatalogAudit audit,
		SaveSeasonRequestValidator seasonValidator, SaveTariffPeriodRequestValidator periodValidator)
	{
		_seasons = seasons;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_seasonValidator = seasonValidator;
		_periodValidator = periodValidator;
	}

	public async Task<SeasonDto?> GetAsync(CancellationToken ct = default)
		=> await _seasons.FindAsync(await _property.IdAsync(ct), ct) is { } season ? SeasonDto.FromEntity(season) : null;

	public async Task<SeasonDto> SaveAsync(SaveSeasonRequest request, CancellationToken ct = default)
	{
		_seasonValidator.EnsureValid(request);
		var propertyId = await _property.IdAsync(ct);
		var dates = new SeasonDates(request.StartsOn, request.EndsOn, request.NextStartsOn, request.NextEndsOn);

		var season = await _seasons.FindAsync(propertyId, ct);
		if (season is null)
		{
			season = PricingSeason.Create(propertyId, dates, _user.UserId);
			_seasons.Add(season);
		}
		else
			season.SetDates(dates);

		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Catalog.SeasonUpdated, AuditActionCodes.Catalog.Targets.Season, null,
			new Dictionary<string, object?> { ["startsOn"] = dates.StartsOn, ["endsOn"] = dates.EndsOn }, ct);
		return SeasonDto.FromEntity(season);
	}

	public async Task<IReadOnlyList<TariffPeriodDto>> ListPeriodsAsync(CancellationToken ct = default)
		=> await _seasons.FindAsync(await _property.IdAsync(ct), ct) is { } season ? Periods(season) : [];

	public async Task<TariffPeriodDto> CreatePeriodAsync(SaveTariffPeriodRequest request, CancellationToken ct = default)
	{
		_periodValidator.EnsureValid(request);
		var season = await RequireSeasonAsync(ct);
		EnsurePeriodAllowed(season, request, null);

		var key = (short)(season.TariffPeriods.Select(p => (int)p.PeriodGroupKey).DefaultIfEmpty(-1).Max() + 1);
		await _seasons.ReplacePeriodAsync(season, key, Ranges(season, key, request), ct);
		await RecordPeriodAsync(AuditActionCodes.Catalog.TariffPeriodCreated, key, request.Name, ct);
		return Periods(season).Single(p => p.Key == key);
	}

	public async Task<TariffPeriodDto> UpdatePeriodAsync(short key, SaveTariffPeriodRequest request, CancellationToken ct = default)
	{
		_periodValidator.EnsureValid(request);
		var season = await RequireSeasonAsync(ct);
		RequirePeriod(season, key);
		EnsurePeriodAllowed(season, request, key);

		await _seasons.ReplacePeriodAsync(season, key, Ranges(season, key, request), ct);
		await RecordPeriodAsync(AuditActionCodes.Catalog.TariffPeriodUpdated, key, request.Name, ct);
		return Periods(season).Single(p => p.Key == key);
	}

	public async Task DeletePeriodAsync(short key, CancellationToken ct = default)
	{
		var season = await RequireSeasonAsync(ct);
		var name = RequirePeriod(season, key);
		var usages = await _seasons.ListPeriodUsagesAsync(season.PropertyId, key, name, ct);
		if (usages.Count > 0)
			throw DomainException.Conflict(CatalogErrors.PeriodInUse, new Dictionary<string, object?> { ["priceLists"] = usages });

		await _seasons.ReplacePeriodAsync(season, key, [], ct);
		await RecordPeriodAsync(AuditActionCodes.Catalog.TariffPeriodDeleted, key, name, ct);
	}

	/// <summary>Periodi della stagione successiva: stesse regole, dentro le date della stagione successiva.</summary>
	public async Task<IReadOnlyList<TariffPeriodDto>> ListNextPeriodsAsync(CancellationToken ct = default)
		=> await _seasons.FindAsync(await _property.IdAsync(ct), ct) is { } season ? NextPeriods(season) : [];

	public async Task<TariffPeriodDto> CreateNextPeriodAsync(SaveTariffPeriodRequest request, CancellationToken ct = default)
	{
		_periodValidator.EnsureValid(request);
		var season = await RequireNextSeasonAsync(ct);
		EnsureNextPeriodAllowed(season, request, null);
		var key = (short)(season.NextSeasonPeriods.Select(p => (int)p.PeriodGroupKey).DefaultIfEmpty(-1).Max() + 1);
		await ReplaceNextPeriodAsync(season, key, request, ct);
		await RecordPeriodAsync(AuditActionCodes.Catalog.TariffPeriodCreated, key, request.Name, ct, next: true);
		return NextPeriods(season).Single(p => p.Key == key);
	}

	public async Task<TariffPeriodDto> UpdateNextPeriodAsync(short key, SaveTariffPeriodRequest request, CancellationToken ct = default)
	{
		_periodValidator.EnsureValid(request);
		var season = await RequireNextSeasonAsync(ct);
		RequireNextPeriod(season, key);
		EnsureNextPeriodAllowed(season, request, key);
		await ReplaceNextPeriodAsync(season, key, request, ct);
		await RecordPeriodAsync(AuditActionCodes.Catalog.TariffPeriodUpdated, key, request.Name, ct, next: true);
		return NextPeriods(season).Single(p => p.Key == key);
	}

	public async Task DeleteNextPeriodAsync(short key, CancellationToken ct = default)
	{
		var season = await RequireSeasonAsync(ct);
		var name = RequireNextPeriod(season, key);
		await ReplaceNextPeriodAsync(season, key, null, ct);
		await RecordPeriodAsync(AuditActionCodes.Catalog.TariffPeriodDeleted, key, name, ct, next: true);
	}

	private async Task ReplaceNextPeriodAsync(PricingSeason season, short key, SaveTariffPeriodRequest? request, CancellationToken ct)
	{
		foreach (var old in season.NextSeasonPeriods.Where(p => p.PeriodGroupKey == key).ToList())
			season.NextSeasonPeriods.Remove(old);
		foreach (var range in request?.Ranges.OrderBy(r => r.StartsOn) ?? Enumerable.Empty<DateRangeDto>())
			season.NextSeasonPeriods.Add(NextSeasonTariffPeriod.Create(season.Id, key, request!.Name, range.StartsOn, range.EndsOn));
		await _unitOfWork.SaveChangesAsync(ct);
	}

	private async Task<PricingSeason> RequireNextSeasonAsync(CancellationToken ct)
	{
		var season = await RequireSeasonAsync(ct);
		return season.NextSeasonStartsOn is null ? throw DomainException.Unprocessable(CatalogErrors.NextSeasonRequired) : season;
	}

	private static string RequireNextPeriod(PricingSeason season, short key)
		=> season.NextSeasonPeriods.FirstOrDefault(p => p.PeriodGroupKey == key && p.DeletedAt is null)?.Name
			?? throw DomainException.NotFound(CatalogErrors.PeriodNotFound);

	private static void EnsureNextPeriodAllowed(PricingSeason season, SaveTariffPeriodRequest request, short? exceptKey)
	{
		var name = request.Name.Trim();
		if (season.NextSeasonPeriods.Any(p => p.DeletedAt is null && p.PeriodGroupKey != exceptKey
				&& string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
			throw DomainException.Conflict(CatalogErrors.PeriodNameTaken);
		if (request.Ranges.Any(r => r.StartsOn < season.NextSeasonStartsOn || r.EndsOn > season.NextSeasonEndsOn))
			throw DomainException.Unprocessable(CatalogErrors.PeriodOutsideSeason, new Dictionary<string, object?>
			{
				["seasonStartsOn"] = season.NextSeasonStartsOn,
				["seasonEndsOn"] = season.NextSeasonEndsOn,
			});
	}

	private static List<TariffPeriodDto> NextPeriods(PricingSeason season)
		=> season.NextSeasonPeriods
			.Where(p => p.DeletedAt is null)
			.GroupBy(p => p.PeriodGroupKey)
			.Select(g => new TariffPeriodDto(g.Key, g.First().Name,
				g.OrderBy(p => p.StartsOn).Select(p => new DateRangeDto(p.StartsOn, p.EndsOn)).ToList()))
			.OrderBy(p => p.Ranges[0].StartsOn)
			.ToList();

	private async Task<PricingSeason> RequireSeasonAsync(CancellationToken ct)
		=> await _seasons.FindAsync(await _property.IdAsync(ct), ct) ?? throw DomainException.Unprocessable(CatalogErrors.SeasonRequired);

	private static string RequirePeriod(PricingSeason season, short key)
		=> season.TariffPeriods.FirstOrDefault(p => p.PeriodGroupKey == key && p.DeletedAt is null)?.Name
			?? throw DomainException.NotFound(CatalogErrors.PeriodNotFound);

	private static void EnsurePeriodAllowed(PricingSeason season, SaveTariffPeriodRequest request, short? exceptKey)
	{
		var name = request.Name.Trim();
		if (season.TariffPeriods.Any(p => p.DeletedAt is null && p.PeriodGroupKey != exceptKey
				&& string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
			throw DomainException.Conflict(CatalogErrors.PeriodNameTaken);
		if (request.Ranges.Any(r => r.StartsOn < season.SeasonStartsOn || r.EndsOn > season.SeasonEndsOn))
			throw DomainException.Unprocessable(CatalogErrors.PeriodOutsideSeason, new Dictionary<string, object?>
			{
				["seasonStartsOn"] = season.SeasonStartsOn,
				["seasonEndsOn"] = season.SeasonEndsOn,
			});
	}

	private static List<TariffPeriod> Ranges(PricingSeason season, short key, SaveTariffPeriodRequest request)
		=> request.Ranges.OrderBy(r => r.StartsOn).Select(r => TariffPeriod.Create(season.Id, key, request.Name, r.StartsOn, r.EndsOn)).ToList();

	private static List<TariffPeriodDto> Periods(PricingSeason season)
		=> season.TariffPeriods
			.Where(p => p.DeletedAt is null)
			.GroupBy(p => p.PeriodGroupKey)
			.Select(g => new TariffPeriodDto(g.Key, g.First().Name,
				g.OrderBy(p => p.StartsOn).Select(p => new DateRangeDto(p.StartsOn, p.EndsOn)).ToList()))
			.OrderBy(p => p.Ranges[0].StartsOn)
			.ToList();

	private Task RecordPeriodAsync(string actionCode, short key, string name, CancellationToken ct, bool next = false)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.TariffPeriod, null,
			new Dictionary<string, object?> { ["key"] = key, ["name"] = name.Trim(), ["season"] = next ? "next" : "current" }, ct);
}
