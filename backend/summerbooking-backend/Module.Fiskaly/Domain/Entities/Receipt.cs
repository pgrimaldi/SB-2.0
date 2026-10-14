using Module.Fiskaly.Domain.Enums;
using Shared.Contracts;

namespace Module.Fiskaly.Domain.Entities;

/// <summary>
/// Documento commerciale emesso (fiscale o non fiscale) per una prenotazione — testata unica che
/// discrimina per DocumentKind. Fatto fiscale storico immutabile: l'annullamento è uno stato
/// (RecordState = Cancelled), non un soft-delete. Aggregate root del modulo Module.Fiskaly.
/// </summary>
public sealed class Receipt : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public ReceiptKind DocumentKind { get; private set; }
	public ReceiptSource Source { get; private set; }
	public long? FiskalyAccountId { get; private set; }
	public long? CashRegisterId { get; private set; }
	public string? FiskalySystemIdSnapshot { get; private set; }
	public int? DocumentNumber { get; private set; }
	public string? RecordId { get; private set; }
	public string? IntentionRecordId { get; private set; }
	public string? IntentionCancellationRecordId { get; private set; }
	public ReceiptRecordState RecordState { get; private set; }
	public string? FiskalyStateRaw { get; private set; }
	public RecordMode? RecordMode { get; private set; }
	public DateTime? SignedAt { get; private set; }
	public string Currency { get; private set; } = null!;
	public LanguageCode LanguageCode { get; private set; }
	public decimal TotalAmount { get; private set; }
	public decimal TaxableAmount { get; private set; }
	public decimal VatAmount { get; private set; }
	public decimal DiscountAmount { get; private set; }
	public decimal? InvoicedPaymentAmount { get; private set; }
	public DateTime? InvoicedPaymentAt { get; private set; }
	public FiskalyPaymentMode? InvoicedPaymentMode { get; private set; }
	public string? SellerLegalNameSnapshot { get; private set; }
	public string? SellerTradeNameSnapshot { get; private set; }
	public string? SellerVatNumberSnapshot { get; private set; }
	public string? SellerFiscalCodeSnapshot { get; private set; }
	public string? SellerAddressSnapshot { get; private set; }
	public Guid? ReservationPublicId { get; private set; }
	public string BookingNumber { get; private set; } = null!;
	public long? PdfAssetId { get; private set; }
	public string? EReceiptUuid { get; private set; }
	public string? FiskalyResponse { get; private set; }
	public DateTime? CancelledAt { get; private set; }
	public Guid? CancelledByUserId { get; private set; }
	public string? CancellationReason { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<ReceiptLine> Lines { get; private set; } = new List<ReceiptLine>();
	public ICollection<ReceiptPayment> Payments { get; private set; } = new List<ReceiptPayment>();

	private Receipt() { }   // unico costruttore, per il materializzatore di EF Core
}
