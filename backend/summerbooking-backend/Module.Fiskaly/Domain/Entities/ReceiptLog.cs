using Module.Fiskaly.Domain.Enums;

namespace Module.Fiskaly.Domain.Entities;

/// <summary>
/// Log append-only del flusso di emissione/annullamento dello scontrino elettronico: una riga per
/// transizione di stato o errore nel dialogo con l'API SIGN IT. Non implementa IAuditable/ISoftDeletable:
/// deve poter scrivere anche quando l'aggregato Receipt fa rollback (come createLog/createErrorLog
/// legacy, che "non lanciano mai"); UpdatedAt/DeletedAt sono esposti solo per uniformità di schema
/// (mai un vero aggiornamento, nessun Global Query Filter).
/// </summary>
public sealed class ReceiptLog
{
	public long Id { get; private set; }
	public long? ReceiptId { get; private set; }
	public Guid? PropertyId { get; private set; }
	public string? BookingNumber { get; private set; }
	public ReceiptLogStep Step { get; private set; }
	public LogResult Result { get; private set; }
	public string? InternalMessage { get; private set; }
	public FiskalyRecordState? FiskalyRecordState { get; private set; }
	public short? FiskalyErrorStatus { get; private set; }
	public string? FiskalyErrorCode { get; private set; }
	public string? FiskalyErrorCategory { get; private set; }
	public string? FiskalyErrorMessage { get; private set; }
	public string? FiskalyLogPayload { get; private set; }
	public Guid? ActorUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ReceiptLog() { }   // unico costruttore, per il materializzatore di EF Core
}
