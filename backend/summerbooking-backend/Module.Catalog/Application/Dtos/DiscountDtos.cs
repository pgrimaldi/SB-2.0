namespace Module.Catalog.Application.Dtos;

public sealed record DiscountQuery(string? Search, int Page = 1, int PageSize = 50, string? SortBy = null, string? SortDir = null);

public static class DiscountSortFields
{
	public const string Code = "code";
	public const string Value = "value";
	public static readonly IReadOnlySet<string> All = new HashSet<string> { Code, Value };
}

/// <summary>Sconto o voucher. Quattro tipologie: con o senza codice (<paramref name="RequiresCode"/>) e con o senza opzioni
/// personalizzate (<paramref name="Scope"/> = custom con file/settori e categorie di servizi).</summary>
/// <param name="Code">«Codice sconto»: quello da digitare, o il nome di uno sconto automatico.</param>
/// <param name="Type">percentage (1–100) o fixed (sul totale).</param>
/// <param name="Scope">all, custom, without_custom.</param>
/// <param name="ServiceCategories">bar, normal, hourly, additional (con scope personalizzato).</param>
/// <param name="Channel">manager_and_online, manager_only, online_only.</param>
/// <param name="MaxUses">null = illimitato.</param>
public sealed record DiscountDto(
	Guid Id, string Code, string? Name, bool RequiresCode, string Type, decimal Value, string Scope, IReadOnlyList<string> ServiceCategories,
	IReadOnlyList<Guid> SectorIds, string Channel, int? MaxUses, int UsedCount, bool IsActive, IReadOnlyList<DateRangeDto> Periods,
	IReadOnlyList<string> Weekdays, IReadOnlyList<DateRangeTimeDto> TimeRanges);

public sealed record SaveDiscountRequest(
	string Code, string? Name, bool RequiresCode, string Type, decimal Value, string Scope, IReadOnlyList<string>? ServiceCategories,
	IReadOnlyList<Guid>? SectorIds, string Channel, int? MaxUses, bool IsActive, IReadOnlyList<DateRangeDto>? Periods,
	IReadOnlyList<string>? Weekdays, IReadOnlyList<DateRangeTimeDto>? TimeRanges);
