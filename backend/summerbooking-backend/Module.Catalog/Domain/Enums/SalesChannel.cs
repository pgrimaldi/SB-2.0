namespace Module.Catalog.Domain.Enums;

/// <summary>Canali su cui è vendibile una voce di listino. Condiviso tra catalog_pricing_price_lists e catalog_agreement_price_lists.</summary>
public enum SalesChannel
{
	ManagerOnly = 0,
	ManagerAndOnline = 1,
	OnlineOnly = 2
}
