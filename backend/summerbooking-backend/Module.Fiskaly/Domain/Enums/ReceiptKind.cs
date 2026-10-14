namespace Module.Fiskaly.Domain.Enums;

/// <summary>Discriminatore di testata di fiskaly_receipts: documento fiscale o non fiscale.</summary>
public enum ReceiptKind
{
	Fiscal = 0,
	NonFiscal = 1
}
