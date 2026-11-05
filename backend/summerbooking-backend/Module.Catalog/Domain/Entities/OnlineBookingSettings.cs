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
	public short? MaxBookingsPerDay { get; private set; }
	public short? BookingHorizonDays { get; private set; }
	public TimeOnly? MorningFrom { get; private set; }
	public TimeOnly? MorningTo { get; private set; }
	public TimeOnly? AfternoonFrom { get; private set; }
	public TimeOnly? AfternoonTo { get; private set; }
	public TimeOnly? AfternoonConversionTime { get; private set; }
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

	public static OnlineBookingSettings Create(long propertyId) => new() { PropertyId = propertyId, AllowOnlineBookingScope = OnlineBookingScope.All };

	public void Update(OnlineBookingSettingsData d)
	{
		IsEnabled = d.IsEnabled;
		IsMapPick = d.IsMapPick;
		IsLocationRestricted = d.IsLocationRestricted;
		IsPeriodRestricted = d.IsPeriodRestricted;
		SurchargeType = d.SurchargeType;
		SurchargeValue = d.SurchargeType == SurchargeType.Disable ? 0 : d.SurchargeValue;
		IsOnlinePaymentBlocked = d.IsBlocked;
		RequiresManualConfirmation = d.RequiresManualConfirmation;
		AllowsSameDayBooking = d.AllowsSameDayBooking;
		AllowsHalfDay = d.AllowsHalfDay;
		MorningFrom = d.MorningFrom;
		MorningTo = d.MorningTo;
		AfternoonFrom = d.AfternoonFrom;
		AfternoonTo = d.AfternoonTo;
		AfternoonConversionTime = d.AfternoonConversionTime;
		AllowsSeasonal = d.AllowsSeasonal;
		AllowsSharing = d.AllowsSharing;
		MaxSpotsPerBooking = d.MaxSpotsPerBooking;
		MaxBookingsPerDay = d.MaxBookingsPerDay;
		BookingHorizonDays = d.BookingHorizonDays;
		AllowOnlineBookingScope = d.Scope;
		EnabledPaymentMethods = d.PaymentMethods.Distinct().ToList();
		UsefulInfo = d.UsefulInfo;
		GoodToKnow = d.GoodToKnow;
	}

	/// <summary>Periodi in cui la prenotazione online è aperta. Il vincolo di non sovrapposizione non è differibile:
	/// si salvano prima le cancellazioni (<see cref="ClearPeriods"/>), poi i nuovi.</summary>
	public void ClearPeriods() => AllowedPeriods.Clear();

	public void AddPeriods(IEnumerable<(DateOnly StartsOn, DateOnly EndsOn)> periods)
	{
		foreach (var (startsOn, endsOn) in periods)
			AllowedPeriods.Add(OnlineAllowedPeriod.Create(startsOn, endsOn));
	}

	/// <summary>Postazioni prenotabili online: si aggiungono le nuove e si tolgono quelle non più scelte.</summary>
	public void ReplaceLocations(IReadOnlyCollection<long> spotIds)
	{
		foreach (var removed in AllowedLocations.Where(l => !spotIds.Contains(l.SpotId)).ToList())
			AllowedLocations.Remove(removed);
		foreach (var added in spotIds.Where(id => AllowedLocations.All(l => l.SpotId != id)))
			AllowedLocations.Add(OnlineAllowedLocation.Create(added));
	}
}
