namespace Module.Catalog.Domain.Enums;

/// <summary>Caparra confirmatoria del servizio tavoli.</summary>
public enum DepositMode
{
	/// <summary>Prenotazione senza alcun pagamento.</summary>
	None = 0,
	/// <summary>Una volta per il totale delle persone.</summary>
	Total = 1,
	/// <summary>Per ogni persona.</summary>
	PerPerson = 2,
}

public enum DepositType
{
	Percentage = 0,
	Amount = 1,
}
