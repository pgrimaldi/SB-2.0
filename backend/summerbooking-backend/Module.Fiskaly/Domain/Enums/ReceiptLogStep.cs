namespace Module.Fiskaly.Domain.Enums;

/// <summary>Passo del flusso di emissione/annullamento a cui si riferisce una riga fiskaly_receipt_logs.</summary>
public enum ReceiptLogStep
{
	IntentionCreated = 0,
	TransactionCreated = 1,
	TransactionFailed = 2,
	PdfGenerationFailed = 3,
	S3UploadFailed = 4,
	Completed = 5,
	CreationFailed = 6,
	CancellationIntentionCreated = 7,
	CancellationIntentionFailed = 8,
	CancellationTransactionFailed = 9,
	Cancelled = 10
}
