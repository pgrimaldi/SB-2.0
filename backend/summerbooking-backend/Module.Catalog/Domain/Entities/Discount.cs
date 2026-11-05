using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Voucher/codice sconto configurato da uno stabilimento. Aggregate root del cluster voucher: radice delle
/// finestre di validità (<see cref="DiscountDateRange"/>) e delle restrizioni di settore
/// (<see cref="DiscountSector"/>).
/// </summary>
public sealed class Discount : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public string Code { get; private set; } = null!;
	public string? Name { get; private set; }
	public DiscountType Type { get; private set; }
	public decimal Value { get; private set; }
	public DiscountScope Scope { get; private set; }
	public List<DiscountServiceCategory> AppliesToServiceCategories { get; private set; } = new();
	public DiscountApplicableChannel ApplicableChannel { get; private set; }
	/// <summary>true = si applica digitando il codice; false = automatico nei periodi, giorni e fasce di validità.</summary>
	public bool RequiresCode { get; private set; }
	/// <summary>Giorni di validità; vuoto = tutti.</summary>
	public Weekday[] Weekdays { get; private set; } = [];
	public int? MaxUses { get; private set; }
	public int UsedCount { get; private set; }
	public bool IsActive { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<DiscountDateRange> ValidityWindows { get; private set; } = new List<DiscountDateRange>();
	public ICollection<DiscountSector> RestrictedSectors { get; private set; } = new List<DiscountSector>();
	public ICollection<DiscountTimeRange> TimeRanges { get; private set; } = new List<DiscountTimeRange>();

	private Discount() { }

	public const int CodeMaxLength = 64;

	public static Discount Create(long propertyId, Guid? createdBy) => new()
	{
		PublicId = Guid.CreateVersion7(),
		PropertyId = propertyId,
		CreatedByUserId = createdBy,
	};

	public void Update(DiscountData d)
	{
		if (d.Type == DiscountType.Percentage && d.Value is <= 0 or > 100 || d.Type == DiscountType.Fixed && d.Value <= 0)
			throw new ArgumentException("Valore dello sconto non valido.", nameof(d));
		if (d.MaxUses is not null && d.MaxUses < UsedCount)
			throw new ArgumentException("Massimo inferiore agli utilizzi già fatti.", nameof(d));
		Code = d.Code.Trim();
		Name = d.Name;
		RequiresCode = d.RequiresCode;
		Type = d.Type;
		Value = d.Value;
		Scope = d.Scope;
		AppliesToServiceCategories = d.Scope == DiscountScope.All ? [] : d.ServiceCategories.Distinct().ToList();
		ApplicableChannel = d.Channel;
		MaxUses = d.MaxUses;
		IsActive = d.IsActive;
		Weekdays = d.Weekdays.Distinct().Order().ToArray();
	}

	public void ReplaceWindows(IEnumerable<(DateOnly StartsOn, DateOnly EndsOn)> windows)
	{
		ValidityWindows.Clear();
		foreach (var (startsOn, endsOn) in windows)
			ValidityWindows.Add(DiscountDateRange.Create(startsOn, endsOn));
	}

	public void SetSectors(IReadOnlyCollection<long> sectorIds)
	{
		foreach (var removed in RestrictedSectors.Where(s => !sectorIds.Contains(s.SectorId)).ToList())
			RestrictedSectors.Remove(removed);
		foreach (var added in sectorIds.Where(id => RestrictedSectors.All(s => s.SectorId != id)))
			RestrictedSectors.Add(DiscountSector.Create(added));
	}

	public void ReplaceTimeRanges(IEnumerable<(TimeOnly From, TimeOnly To)> ranges)
	{
		TimeRanges.Clear();
		foreach (var (from, to) in ranges)
			TimeRanges.Add(DiscountTimeRange.Create(from, to));
	}
}
