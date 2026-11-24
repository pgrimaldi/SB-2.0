using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Ledger append-only dei crediti sharing maturati dal titolare quando un giorno ceduto viene ripreso
/// da un altro cliente; il saldo è la somma delle righe per cliente. Non implementa ISoftDeletable: le
/// correzioni sono righe reversed/adjustment, mai una cancellazione di un movimento.
/// </summary>
public sealed class ResaleCreditLedgerEntry : IAuditable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid CustomerId { get; private set; }
	public long? ResaleOfferId { get; private set; }
	public long? SourceReservationId { get; private set; }
	public ResaleCreditEntryType EntryType { get; private set; }
	public int CreditUnits { get; private set; }
	public decimal Amount { get; private set; }
	public DateTime OccurredAt { get; private set; }
	public bool WalletEventEmitted { get; private set; }
	public string? Note { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ResaleOffer? ResaleOffer { get; private set; }
	public Reservation? SourceReservation { get; private set; }

	private ResaleCreditLedgerEntry() { }

	/// <summary>Credito maturato perché un giorno ceduto è stato ripreso.</summary>
	public static ResaleCreditLedgerEntry Earned(ResaleOffer offer, Guid customerId, int units, decimal amount, DateTime now, bool again)
		=> new()
		{
			PropertyId = offer.PropertyId,
			CustomerId = customerId,
			ResaleOffer = offer,
			SourceReservation = offer.SourceReservation,
			EntryType = again ? ResaleCreditEntryType.Adjustment : ResaleCreditEntryType.Earned,
			CreditUnits = units,
			Amount = amount,
			OccurredAt = now,
			Note = again ? "Giorno ripreso di nuovo dopo un annullamento" : null,
		};

	/// <summary>Storno: chi aveva ripreso il giorno lo ha tolto.</summary>
	public static ResaleCreditLedgerEntry Reversal(ResaleOffer offer, Guid customerId, int units, decimal amount, DateTime now)
		=> new()
		{
			PropertyId = offer.PropertyId,
			CustomerId = customerId,
			ResaleOffer = offer,
			SourceReservation = offer.SourceReservation,
			EntryType = ResaleCreditEntryType.Reversed,
			CreditUnits = -units,
			Amount = -amount,
			OccurredAt = now,
		};

	/// <summary>Correzione del gestore: crediti usati (negativi) o aggiunti a mano.</summary>
	public static ResaleCreditLedgerEntry ManualAdjustment(Guid propertyId, Guid customerId, int units, decimal amount, string? note, DateTime now)
		=> new()
		{
			PropertyId = propertyId,
			CustomerId = customerId,
			EntryType = ResaleCreditEntryType.Adjustment,
			CreditUnits = units,
			Amount = amount,
			OccurredAt = now,
			Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
		};
}
