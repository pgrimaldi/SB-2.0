using Shared.Contracts;

namespace Module.Fiskaly.Domain.Entities;

/// <summary>
/// Contatore progressivo giornaliero del numero documento commerciale per registratore di cassa:
/// 1 riga per (CashRegisterId, FiscalDate). Non è owned da FiskalyAccount/CashRegister — vive in una
/// propria tabella con upsert atomico — ma referenzia CashRegister con FK reale.
/// </summary>
public sealed class ReceiptSequence : IAuditable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public long CashRegisterId { get; private set; }
	public DateOnly FiscalDate { get; private set; }
	public int LastNumber { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	/// <summary>Esposta per uniformità di schema: la sequenza non si cancella (non ISoftDeletable).</summary>
	public DateTime? DeletedAt { get; private set; }

	private ReceiptSequence() { }   // unico costruttore, per il materializzatore di EF Core
}
