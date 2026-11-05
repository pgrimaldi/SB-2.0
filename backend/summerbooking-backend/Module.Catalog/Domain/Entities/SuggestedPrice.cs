using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Prezzo suggerito dall'algoritmo di Revenue Management per un pacchetto in una data specifica. Entità
/// indipendente (output di un job di calcolo, non componente editoriale del listino).
/// </summary>
/// <remarks>
/// Una riga per (pacchetto, data, fila): la fila e il periodo tariffario vengono dal prezzo di listino di partenza.
/// <see cref="PeriodLabel"/> è l'etichetta «&lt;fila&gt; &lt;periodo&gt;» dei dati importati, tenuta solo come traccia.
/// </remarks>
public sealed class SuggestedPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public long PropertyId { get; private set; }
	public DateOnly PriceDate { get; private set; }
	public string? PeriodLabel { get; private set; }
	/// <summary>Fila del prezzo di listino da cui parte il suggerimento; null = tutte le file.</summary>
	public long? SectorId { get; private set; }
	public short? TariffPeriodGroup { get; private set; }
	public decimal? ListDailyPrice { get; private set; }
	public decimal? ListWeekendPrice { get; private set; }
	public decimal SuggestedPriceValue { get; private set; }
	public decimal DiscountPercentage { get; private set; }
	public SuggestedPriceMode AppliedMode { get; private set; }
	public DateTime ComputedAt { get; private set; }
	public DateTime? AppliedAt { get; private set; }
	public Guid? AppliedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private SuggestedPrice() { }

	public static SuggestedPrice Create(long priceListId, long propertyId, DateOnly date, long? sectorId)
		=> new() { PriceListId = priceListId, PropertyId = propertyId, PriceDate = date, SectorId = sectorId, AppliedMode = SuggestedPriceMode.None };

	/// <summary>Nuovo calcolo: la scelta del gestore resta, compresa la percentuale di uno sconto scelto.</summary>
	public void Recompute(short? periodGroup, decimal? daily, decimal? weekend, decimal suggested, decimal discountPercent, DateTime now)
	{
		TariffPeriodGroup = periodGroup;
		ListDailyPrice = daily;
		ListWeekendPrice = weekend;
		SuggestedPriceValue = suggested;
		if (AppliedMode != SuggestedPriceMode.Discount)
			DiscountPercentage = discountPercent;
		ComputedAt = now;
	}

	/// <summary>Scelta del gestore: prezzo suggerito, sconto con la sua percentuale, prezzo originale o nessuna.</summary>
	public void Apply(SuggestedPriceMode mode, decimal? discountPercent, Guid? userId, DateTime now)
	{
		AppliedMode = mode;
		if (mode == SuggestedPriceMode.Discount && discountPercent is { } pct)
			DiscountPercentage = pct;
		AppliedAt = mode == SuggestedPriceMode.None ? null : now;
		AppliedByUserId = mode == SuggestedPriceMode.None ? null : userId;
	}
}
