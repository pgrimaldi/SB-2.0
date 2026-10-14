using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Postazione ammessa alla prenotazione online quando OnlineBookingSettings.IsLocationRestricted è true.</summary>
public sealed class OnlineAllowedLocation : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SettingsId { get; private set; }
	public long SpotId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private OnlineAllowedLocation() { }
}
