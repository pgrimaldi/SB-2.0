namespace Module.Catalog.Domain.Pricing;

/// <summary>Come si è calcolato il prezzo di una riga o di un tratto del preventivo (<c>rule</c> nella risposta).</summary>
public static class PriceRules
{
	public const string Manual = "manual";
	public const string Seasonal = "seasonal";
	public const string Reduced = "reduced";
	public const string Cumulative = "cumulative";
	public const string Grid = "grid";
	public const string Voucher = "voucher";
	public const string List = "list";
	public const string RevenueSuggested = "revenue_suggested";
	public const string RevenueOriginal = "revenue_original";
	public const string RevenueDiscount = "revenue_discount";

	/// <summary>Tratto senza periodo tariffario.</summary>
	public const string NoPeriod = "no_period";

	/// <summary>Tratto di un periodo senza prezzo nel listino.</summary>
	public const string NoPrice = "no_price";
}

/// <summary>A cosa si applica il supplemento online.</summary>
public static class SurchargeScopes
{
	public const string Spots = "spots";
	public const string Services = "services";
}
