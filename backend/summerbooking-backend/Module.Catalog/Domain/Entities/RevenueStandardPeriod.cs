using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Periodo standard del prezzo suggerito, uguale per tutti gli stabilimenti e ripetuto ogni anno con lo stesso giorno e
/// mese. I giorni fuori da ogni periodo sono di bassa stagione. Può scavalcare il capodanno.
/// </summary>
public sealed class RevenueStandardPeriod : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public string Name { get; private set; } = null!;
	public short StartMonth { get; private set; }
	public short StartDay { get; private set; }
	public short EndMonth { get; private set; }
	public short EndDay { get; private set; }
	public RevenueSeasonTier Tier { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private RevenueStandardPeriod() { }

	public static RevenueStandardPeriod Create(string name, short startMonth, short startDay, short endMonth, short endDay, RevenueSeasonTier tier, short sortOrder)
		=> new() { Name = name.Trim(), StartMonth = startMonth, StartDay = startDay, EndMonth = endMonth, EndDay = endDay, Tier = tier, SortOrder = sortOrder };

	public bool Contains(DateOnly date)
	{
		var day = date.Month * 100 + date.Day;
		var start = StartMonth * 100 + StartDay;
		var end = EndMonth * 100 + EndDay;
		return start <= end ? day >= start && day <= end : day >= start || day <= end;
	}
}
