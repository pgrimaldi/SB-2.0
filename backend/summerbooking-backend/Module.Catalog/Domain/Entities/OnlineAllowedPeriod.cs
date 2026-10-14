using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Finestra di date [StartsOn, EndsOn] in cui è consentita la prenotazione online quando OnlineBookingSettings.IsPeriodRestricted è true.</summary>
public sealed class OnlineAllowedPeriod : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SettingsId { get; private set; }
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private OnlineAllowedPeriod() { }
}
