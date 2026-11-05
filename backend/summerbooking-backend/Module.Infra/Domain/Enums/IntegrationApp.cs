namespace Module.Infra.Domain.Enums;

/// <summary>
/// Applicazione partner integrata (POS/cassa esterni, export dati) a cui un client API appartiene —
/// vocabolario chiuso.
/// </summary>
public enum IntegrationApp
{
	OfSale = 0,
	Transactions = 1,
	TeamSystem = 2
}
