using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Impostazioni › Sharing di uno stabilimento: se il titolare può cedere i suoi giorni, a quali prenotazioni si applica e
/// che credito matura quando un giorno ceduto viene ripreso. Le tre modalità di credito sono indipendenti.
/// </summary>
public sealed class ResaleSettings : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public bool IsEnabled { get; private set; }
	/// <summary>Giorni minimi della prenotazione per avere lo sharing; null = nessun minimo.</summary>
	public int? MinBookingDays { get; private set; }
	public List<ResaleScope> AppliesTo { get; private set; } = new();
	/// <summary>Un'assenza programmata vale come sharing: se il giorno viene ripreso il titolare matura il credito.</summary>
	public bool AbsencesAsSharing { get; private set; }
	public bool ManualCreditEnabled { get; private set; }
	public int ManualCreditUnitsPerDay { get; private set; }
	public decimal ManualCreditAmount { get; private set; }
	public bool VoucherCreditEnabled { get; private set; }
	public decimal VoucherCreditAmount { get; private set; }
	public bool CardTopUpEnabled { get; private set; }
	public decimal CardTopUpAmount { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ResaleSettings() { }

	/// <summary>Impostazioni predefinite: sharing spento.</summary>
	public static ResaleSettings Default(Guid propertyId) => new() { PropertyId = propertyId };

	public void Update(
		bool enabled, int? minBookingDays, IEnumerable<ResaleScope> appliesTo, bool absencesAsSharing,
		bool manualCredit, int manualUnitsPerDay, decimal manualAmount, bool voucher, decimal voucherAmount, bool cardTopUp, decimal cardTopUpAmount)
	{
		if (minBookingDays is < 1 || manualUnitsPerDay < 0 || manualAmount < 0 || voucherAmount < 0 || cardTopUpAmount < 0)
			throw new ArgumentOutOfRangeException(nameof(minBookingDays));
		IsEnabled = enabled;
		MinBookingDays = minBookingDays;
		AppliesTo = appliesTo.Distinct().Order().ToList();
		AbsencesAsSharing = absencesAsSharing;
		ManualCreditEnabled = manualCredit;
		ManualCreditUnitsPerDay = manualUnitsPerDay;
		ManualCreditAmount = manualAmount;
		VoucherCreditEnabled = voucher;
		VoucherCreditAmount = voucherAmount;
		CardTopUpEnabled = cardTopUp;
		CardTopUpAmount = cardTopUpAmount;
	}

	/// <summary>Il titolare matura qualcosa quando il giorno viene ripreso.</summary>
	public bool GivesCredit => ManualCreditEnabled || VoucherCreditEnabled || CardTopUpEnabled;
}
