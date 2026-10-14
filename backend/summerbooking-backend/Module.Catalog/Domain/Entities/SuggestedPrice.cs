using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Prezzo suggerito dall'algoritmo di Revenue Management per un pacchetto in una data specifica. Entità
/// indipendente (output di un job di calcolo, non componente editoriale del listino).
/// </summary>
/// <remarks>
/// <see cref="PeriodLabel"/> aggiunta: i dati
/// legacy reali (`price_lists.suggestedPriceDetails[].packages[]`) sono multi-variante per
/// (pacchetto, data) — una singola cifra per data avrebbe scartato la maggioranza dei suggerimenti
/// calcolati dall'algoritmo. Etichetta libera come <see cref="AgreementPriceListPeriodPrice.PeriodLabel"/>,
/// non un gruppo tariffario risolto: i nomi legacy (es. "Fila 1 Alta stagione") sono composti e non sempre
/// classificabili in modo affidabile.
/// </remarks>
public sealed class SuggestedPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public long PropertyId { get; private set; }
	public DateOnly PriceDate { get; private set; }
	public string? PeriodLabel { get; private set; }
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
}
