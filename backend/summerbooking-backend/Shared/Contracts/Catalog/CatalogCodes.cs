namespace Shared.Contracts.Catalog;

/// <summary>Tipo di una riga del preventivo (<c>PriceQuoteLine.Kind</c>).</summary>
public static class QuoteLineKinds
{
	public const string Package = "package";
	public const string Additional = "additional";
	public const string Voucher = "voucher";
	public const string Service = "service";
	public const string AutomaticPrice = "automatic_price";
	public const string Surcharge = "surcharge";
}

/// <summary>Avvisi del preventivo: la prenotazione si salva lo stesso, il gestionale li mostra.</summary>
public static class QuoteWarnings
{
	/// <summary>Giorni fuori da ogni periodo tariffario (valgono 0).</summary>
	public const string DaysWithoutPeriod = "days_without_period";

	/// <summary>Il listino non ha il prezzo di un periodo toccato (vale 0).</summary>
	public const string PeriodWithoutPrice = "period_without_price";

	/// <summary>Prenotazione stagionale senza prezzo stagionale nel listino (vale 0).</summary>
	public const string SeasonalPriceMissing = "seasonal_price_missing";
}

/// <summary>Parte del giorno di una prenotazione.</summary>
public static class DayParts
{
	public const string FullDay = "full_day";
	public const string Morning = "morning";
	public const string Afternoon = "afternoon";

	public static readonly IReadOnlySet<string> All = new HashSet<string> { FullDay, Morning, Afternoon };
}

/// <summary>Tipo di servizio vendibile (<c>BookableService.Kind</c>).</summary>
public static class ServiceKinds
{
	public const string Bar = "bar";
	public const string Normal = "normal";
	public const string Hourly = "hourly";
}

/// <summary>Metodi di pagamento della prenotazione online.</summary>
public static class OnlinePaymentMethods
{
	public const string PayNow = "pay_now";
	public const string PayAtProperty = "pay_at_property";
	public const string PayAtPropertyNoCard = "pay_at_property_no_card";
	public const string DepositHalf = "deposit_half";
	public const string PayAtPropertyPrepaid = "pay_at_property_prepaid";

	/// <summary>Metodi che si pagano con la carta al momento della prenotazione.</summary>
	public static readonly IReadOnlySet<string> PaidOnline = new HashSet<string> { PayNow, DepositHalf };
}

/// <summary>Codici del servizio tavoli (<c>BookableTableService</c>).</summary>
public static class TableServiceCodes
{
	public const string CapacityTables = "tables";
	public const string CapacitySeats = "seats";
	public const string DepositNone = "none";
	public const string DepositTotal = "total";
	public const string DepositPerPerson = "per_person";
	public const string DepositPercentage = "percentage";
	public const string DepositAmount = "amount";
}
