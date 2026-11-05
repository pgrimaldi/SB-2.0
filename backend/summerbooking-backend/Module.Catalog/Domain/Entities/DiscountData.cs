using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Domain.Entities;

public sealed record DiscountData(
	string Code, string? Name, bool RequiresCode, DiscountType Type, decimal Value, DiscountScope Scope,
	IReadOnlyList<DiscountServiceCategory> ServiceCategories, DiscountApplicableChannel Channel, int? MaxUses, bool IsActive,
	IReadOnlyList<Weekday> Weekdays);
