namespace Module.Catalog.Domain.Enums;

/// <summary>
/// Metodo di pagamento offerto al cliente nel canale online (postazioni o servizi). Mappato come array
/// Postgres (catalog.online_payment_method[]), non come tabella figlia: insieme piccolo e stabile.
/// </summary>
public enum OnlinePaymentMethod
{
	PayNow = 0,
	PayAtProperty = 1,
	PayAtPropertyNoCard = 2,
	DepositHalf = 3,
	PayAtPropertyPrepaid = 4
}
