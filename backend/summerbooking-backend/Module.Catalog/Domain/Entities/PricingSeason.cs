using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Confini della stagione corrente (e, opzionale, della successiva) di uno stabilimento (1:1 con Property).</summary>
public sealed class PricingSeason : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public DateOnly SeasonStartsOn { get; private set; }
	public DateOnly SeasonEndsOn { get; private set; }
	public DateOnly? NextSeasonStartsOn { get; private set; }
	public DateOnly? NextSeasonEndsOn { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<TariffPeriod> TariffPeriods { get; private set; } = new List<TariffPeriod>();

	private PricingSeason() { }
}
