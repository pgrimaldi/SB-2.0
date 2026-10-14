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
	public int? MaxUses { get; private set; }
	public int UsedCount { get; private set; }
	public bool IsActive { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<DiscountDateRange> ValidityWindows { get; private set; } = new List<DiscountDateRange>();
	public ICollection<DiscountSector> RestrictedSectors { get; private set; } = new List<DiscountSector>();

	private Discount() { }
}
