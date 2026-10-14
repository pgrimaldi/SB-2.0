using Module.Fiskaly.Domain.Enums;
using Shared.Contracts;

namespace Module.Fiskaly.Domain.Entities;

/// <summary>
/// Voce del documento commerciale (pacchetto, articolo aggiuntivo, servizio, sovrapprezzo, voucher)
/// congelata al momento dell'emissione. Owned entity dell'aggregato Receipt.
/// </summary>
public sealed class ReceiptLine : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ReceiptId { get; private set; }
	public Guid PropertyId { get; private set; }
	public short LineNo { get; private set; }
	public LineItemType LineKind { get; private set; }
	public FiskalyEntryType FiskalyEntryType { get; private set; }
	public FiskalyEntryCategory Category { get; private set; }
	public Guid? SourceLinePublicId { get; private set; }
	public string DescriptionSnapshot { get; private set; } = null!;
	public decimal Quantity { get; private set; }
	public decimal UnitPriceGross { get; private set; }
	public decimal UnitPriceNet { get; private set; }
	public short VatCodeId { get; private set; }
	public FiskalyVatKind VatKind { get; private set; }
	public decimal VatRate { get; private set; }
	public decimal VatAmount { get; private set; }
	public decimal LineGross { get; private set; }
	public decimal LineNet { get; private set; }
	public decimal PaidAllocation { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ReceiptLine() { }   // unico costruttore, per il materializzatore di EF Core
}
