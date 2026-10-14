using Module.Payment.Domain.Enums;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Quota di un movimento di pagamento attribuita a un articolo (pacchetto, servizio, voucher, ...), per
/// l'export contabile e i report IVA per riga. Owned type di <see cref="LedgerEntry"/>: nessuna identità
/// pubblica, segue il ciclo di vita (e i timestamp) della testata — non implementa IAuditable/ISoftDeletable.
/// </summary>
public sealed class LedgerEntryLine
{
	public long Id { get; private set; }
	public long EntryId { get; private set; }

	/// <summary>
	/// public_id di catalog.properties, copiato dalla testata:
	/// export contabile e report IVA aggregano per riga e per stabilimento, e senza questa colonna
	/// ogni riga richiede un hop su payment_ledger_entries.
	/// </summary>
	public Guid PropertyId { get; private set; }
	public LedgerLineKind LineKind { get; private set; }
	public string DescriptionSnapshot { get; private set; } = null!;
	public Guid? ReservationLinePublicId { get; private set; }
	public int Quantity { get; private set; }
	public decimal NetAmount { get; private set; }
	public decimal VatAmount { get; private set; }
	public decimal VatRate { get; private set; }
	public bool IsAllocated { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private LedgerEntryLine() { }
}
