using Shared.Contracts;

namespace Module.FiscalPrint.Domain.Entities;

/// <summary>
/// Scontrino/ricevuta stampato su registratore di cassa RT fisico, con la risposta grezza del
/// dispositivo e le proiezioni derivate per le liste.
/// </summary>
public sealed class FiscalPrintReceipt : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public long? PrinterId { get; private set; }
	public Guid? ReservationPublicId { get; private set; }
	public string? BookingNumber { get; private set; }
	public int? ReceiptNumber { get; private set; }
	public string? RtFiscalReceiptNumber { get; private set; }
	public decimal? RtFiscalAmount { get; private set; }
	public string? RtZReportNumber { get; private set; }
	public string? RtSerialNumber { get; private set; }
	public DateTime PrintedAt { get; private set; }
	public string? DeviceResponse { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public PrinterConfig? Printer { get; private set; }

	private FiscalPrintReceipt() { }
}
