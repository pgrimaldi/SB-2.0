namespace Module.Fiskaly.Domain.Enums;

/// <summary>Stato interno del flusso di emissione/annullamento/generazione di un documento commerciale.</summary>
public enum ReceiptRecordState
{
	IntentionCreated = 0,
	TransactionCreated = 1,
	TransactionFailed = 2,
	EreceiptFailed = 3,
	S3UploadFailed = 4,
	Completed = 5,
	CancellationIntentionCreated = 6,
	CancellationTransactionFailed = 7,
	Cancelled = 8,
	Generated = 9,
	GenerationFailed = 10
}
