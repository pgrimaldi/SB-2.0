using Module.Fiskaly.Domain.Enums;
using Shared.Contracts;

namespace Module.Fiskaly.Domain.Entities;

/// <summary>
/// Modalità di pagamento con cui è stato incassato l'importo del documento commerciale, congelata
/// all'emissione (uno scontrino può avere più righe pagamento). Owned entity dell'aggregato Receipt.
/// </summary>
public sealed class ReceiptPayment : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ReceiptId { get; private set; }
	public Guid PropertyId { get; private set; }
	public short PaymentNo { get; private set; }
	public FiskalyPaymentType PaymentType { get; private set; }
	public decimal Amount { get; private set; }
	public decimal DiscountAmount { get; private set; }
	public string? Label { get; private set; }
	public CardKind? CardKind { get; private set; }
	public string? CardNumberMasked { get; private set; }
	public VoucherKind? VoucherKind { get; private set; }
	public OutstandingConcept? OutstandingConcept { get; private set; }
	public Guid? LedgerEntryPublicId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ReceiptPayment() { }   // unico costruttore, per il materializzatore di EF Core
}
