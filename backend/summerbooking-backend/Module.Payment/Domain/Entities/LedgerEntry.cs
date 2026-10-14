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
}
