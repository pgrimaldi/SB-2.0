using Module.Payment.Domain.Enums;
using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Movimento di incasso/acconto/spesa/rimborso/rettifica relativo a una prenotazione; append-only.
/// </summary>
public sealed class LedgerEntry : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid? ReservationPublicId { get; private set; }
	public Guid? AgreementLinePublicId { get; private set; }
	public LedgerEntryType EntryType { get; private set; }
	public LedgerDirection Direction { get; private set; }
	public PaymentMode? PaymentMode { get; private set; }
	public LedgerSource Source { get; private set; }
	public long? SourceRefId { get; private set; }
	/// <remarks>Colonna generata (GENERATED ALWAYS ... STORED): di sola lettura, PostgreSQL rifiuta un valore esplicito.</remarks>
	public decimal GrossAmount { get; private set; }
	public decimal NetAmount { get; private set; }
	public decimal VatAmount { get; private set; }
	public decimal VatRate { get; private set; }
	public string Currency { get; private set; } = null!;
	public Guid? CustomerPublicId { get; private set; }
	public string? CustomerNameSnapshot { get; private set; }
	public string? CustomerEmailSnapshot { get; private set; }
	public string? CustomerPhoneSnapshot { get; private set; }
	public Guid? RecordedByUserId { get; private set; }
	public DateTime OccurredAt { get; private set; }
	public DateOnly? StayFrom { get; private set; }
	public DateOnly? StayTo { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<LedgerEntryLine> Lines { get; private set; } = new List<LedgerEntryLine>();

	private LedgerEntry() { }

	/// <summary>
	/// Incasso o acconto registrato a mano dal gestionale. Importo IVA compresa; la ripartizione dell'IVA per aliquota
	/// e le righe per articolo sono da fare (oggi tutto l'importo è imponibile con aliquota 0).
	/// </summary>
	public static LedgerEntry ManualIncome(
		Guid propertyId, Guid reservationId, decimal amount, PaymentMode mode, bool isDeposit, Guid? customerId, Guid? recordedBy,
		DateTime occurredAt, DateOnly? stayFrom, DateOnly? stayTo)
	{
		if (amount <= 0)
			throw new ArgumentOutOfRangeException(nameof(amount));
		return new LedgerEntry
		{
			PublicId = Guid.CreateVersion7(),
			PropertyId = propertyId,
			ReservationPublicId = reservationId,
			EntryType = isDeposit ? LedgerEntryType.Deposit : LedgerEntryType.Payment,
			Direction = LedgerDirection.In,
			PaymentMode = mode,
			Source = LedgerSource.Manual,
			NetAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
			Currency = "EUR",
			CustomerPublicId = customerId,
			RecordedByUserId = recordedBy,
			OccurredAt = occurredAt,
			StayFrom = stayFrom,
			StayTo = stayTo,
		};
	}

	/// <summary>Uscita di rimborso con lo stesso metodo dell'incasso: l'incasso originale resta.</summary>
	public static LedgerEntry RefundOf(
		Guid propertyId, Guid reservationId, decimal amount, PaymentMode? mode, LedgerSource source, Guid? customerId, Guid? recordedBy, DateTime occurredAt)
	{
		if (amount <= 0)
			throw new ArgumentOutOfRangeException(nameof(amount));
		return new LedgerEntry
		{
			PublicId = Guid.CreateVersion7(),
			PropertyId = propertyId,
			ReservationPublicId = reservationId,
			EntryType = LedgerEntryType.Refund,
			Direction = LedgerDirection.Out,
			PaymentMode = mode,
			Source = source,
			NetAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
			Currency = "EUR",
			CustomerPublicId = customerId,
			RecordedByUserId = recordedBy,
			OccurredAt = occurredAt,
		};
	}

	/// <summary>
	/// Metà di uno spostamento di incassato fra due prenotazioni («Dividi conto»): uscita da una, entrata nell'altra, con lo
	/// stesso metodo e tipo (acconto o saldo). Non è un movimento di cassa e non si annulla da solo.
	/// </summary>
	public static LedgerEntry SplitTransfer(
		Guid propertyId, Guid reservationId, decimal amount, PaymentMode? mode, bool isDeposit, LedgerDirection direction, Guid? customerId,
		Guid? recordedBy, DateTime occurredAt)
	{
		if (amount <= 0)
			throw new ArgumentOutOfRangeException(nameof(amount));
		return new LedgerEntry
		{
			PublicId = Guid.CreateVersion7(),
			PropertyId = propertyId,
			ReservationPublicId = reservationId,
			EntryType = isDeposit ? LedgerEntryType.Deposit : LedgerEntryType.Payment,
			Direction = direction,
			PaymentMode = mode,
			Source = LedgerSource.ReservationSplit,
			NetAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
			Currency = "EUR",
			CustomerPublicId = customerId,
			RecordedByUserId = recordedBy,
			OccurredAt = occurredAt,
		};
	}

	/// <summary>Importo con il segno del verso: positivo se entra, negativo se esce.</summary>
	public decimal SignedAmount => Direction == LedgerDirection.In ? NetAmount + VatAmount : -(NetAmount + VatAmount);
}
