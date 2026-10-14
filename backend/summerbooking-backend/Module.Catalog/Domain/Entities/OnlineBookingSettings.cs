using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Configurazione del canale di prenotazione online delle postazioni per uno stabilimento (1:1 con
/// Property). Se la property non ha mai configurato il canale, non esiste riga.
/// </summary>
public sealed class OnlineBookingSettings : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public bool IsEnabled { get; private set; }
	public bool IsMapPick { get; private set; }
	public bool IsLocationRestricted { get; private set; }
	public bool IsPeriodRestricted { get; private set; }
	public SurchargeType SurchargeType { get; private set; }
	public decimal SurchargeValue { get; private set; }
	public bool IsOnlinePaymentBlocked { get; private set; }
	public bool RequiresManualConfirmation { get; private set; }
	public bool AllowsSameDayBooking { get; private set; }
	public bool AllowsHalfDay { get; private set; }
	public bool AllowsSeasonal { get; private set; }
	public bool AllowsSharing { get; private set; }
	public short? MaxSpotsPerBooking { get; private set; }
	public OnlineBookingScope AllowOnlineBookingScope { get; private set; }
	public string? UsefulInfo { get; private set; }
	public string? GoodToKnow { get; private set; }
	public List<OnlinePaymentMethod> EnabledPaymentMethods { get; private set; } = new();
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<OnlineAllowedLocation> AllowedLocations { get; private set; } = new List<OnlineAllowedLocation>();
	public ICollection<OnlineAllowedPeriod> AllowedPeriods { get; private set; } = new List<OnlineAllowedPeriod>();
	public ICollection<OnlineStayLimit> StayLimits { get; private set; } = new List<OnlineStayLimit>();

	private OnlineBookingSettings() { }
}
