namespace Module.Payment.Domain;

/// <summary>Codici di errore stabili del modulo Payment, tradotti dal frontend.</summary>
public static class PaymentErrors
{
	public const string PropertyRequired = "payment.property_required";
	public const string EntryNotFound = "payment.entry_not_found";
	public const string EntryNotVoidable = "payment.entry_not_voidable";
	public const string InvalidAmount = "payment.invalid_amount";
	public const string InvalidMode = "payment.invalid_mode";
}
