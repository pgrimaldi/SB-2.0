namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Forma di pagamento di una riga fiskaly_receipt_payments, normalizzata a un tipo valido Fiskaly.
/// Distinto da FiskalyPaymentMode (fiskaly.payment_mode, su fiskaly_receipts.invoiced_payment_mode):
/// due vocabolari diversi con valori diversi.
/// </summary>
public enum FiskalyPaymentType
{
	Cash = 0,
	Card = 1,
	Online = 2,
	Other = 3,
	Voucher = 4,
	Outstanding = 5
}
