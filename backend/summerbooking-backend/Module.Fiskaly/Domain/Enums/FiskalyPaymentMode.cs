namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Forma dell'incasso fatturato da uno scontrino (fiskaly_receipts.invoiced_payment_mode).
/// Vocabolario concettualmente condiviso con booking.payment_mode / payment.payment_mode, ma registrato
/// localmente nello schema fiskaly (nessun tipo Postgres davvero cross-schema in questa fase).
/// </summary>
public enum FiskalyPaymentMode
{
	Cash = 0,
	CreditCard = 1,
	BankTransfer = 2,
	Other = 3
}
