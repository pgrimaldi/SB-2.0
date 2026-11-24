using Module.Booking.Domain.Enums;

namespace Module.Booking.Domain.Pricing;

/// <summary>Sconto manuale o incremento scelto dall'operatore.</summary>
public sealed record PriceAdjustment(AdjustmentType Type, decimal Value, string? Label = null)
{
	public decimal AmountOn(decimal subtotal)
	{
		if (Value < 0 || (Type == AdjustmentType.Percentage && Value > 100))
			throw new ArgumentOutOfRangeException(nameof(Value));
		return Type == AdjustmentType.Percentage ? Round(subtotal * Value / 100m) : Round(Value);
	}

	internal static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Importi della prenotazione. Codice sconto e sconto manuale si calcolano entrambi sul subtotale (non a cascata),
/// l'incremento è uno solo; il prezzo applicato scritto a mano vince su tutto.
/// </summary>
public sealed record ReservationPricing(
	decimal Subtotal, decimal CodeDiscount, PriceAdjustment? ManualDiscount, decimal ManualDiscountAmount, PriceAdjustment? Surcharge,
	decimal SurchargeAmount, decimal? PriceOverride, decimal Total, bool HasManualPackagePrice, bool Recalculated)
{
	public static ReservationPricing Compute(
		decimal subtotal, decimal codeDiscount, PriceAdjustment? manualDiscount, PriceAdjustment? surcharge, decimal? priceOverride,
		bool hasManualPackagePrice, bool recalculated)
	{
		if (priceOverride is < 0)
			throw new ArgumentOutOfRangeException(nameof(priceOverride));
		var manual = Math.Min(manualDiscount?.AmountOn(subtotal) ?? 0m, Math.Max(0, subtotal - codeDiscount));
		var extra = surcharge?.AmountOn(subtotal) ?? 0m;
		var computed = Math.Max(0, PriceAdjustment.Round(subtotal - codeDiscount - manual + extra));
		return new ReservationPricing(
			subtotal, codeDiscount, manualDiscount, manual, surcharge, extra, priceOverride is { } p ? PriceAdjustment.Round(p) : null,
			priceOverride is { } o ? PriceAdjustment.Round(o) : computed, hasManualPackagePrice, recalculated);
	}
}
