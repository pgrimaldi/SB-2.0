namespace Module.Payment.Domain.Entities;

/// <summary>
/// Prodotto/riga di uno scontrino POS: descrizione, quantità, prezzo, aliquota IVA, totale, congelati al
/// momento della trasmissione. Owned type di <see cref="PosCharge"/>: nessuna identità pubblica, segue il
/// ciclo di vita dello scontrino — non implementa IAuditable/ISoftDeletable.
/// </summary>
public sealed class PosChargeItem
{
	public long Id { get; private set; }
	public long ChargeId { get; private set; }

	/// <summary>
	/// public_id di catalog.properties, copiato dalla testata:
	/// report IVA degli addebiti POS e report unificato per articolo, senza hop su
	/// payment_pos_charges.
	/// </summary>
	public Guid PropertyId { get; private set; }
	public string? ExternalLineRef { get; private set; }
	public Guid? SaleItemPublicId { get; private set; }
	public string DescriptionSnapshot { get; private set; } = null!;
	public int Quantity { get; private set; }
	public decimal UnitPrice { get; private set; }
	public decimal LineTotal { get; private set; }
	public decimal VatRate { get; private set; }
	/// <remarks>Colonna generata (GENERATED ALWAYS ... STORED): di sola lettura, PostgreSQL rifiuta un valore esplicito.</remarks>
	public decimal NetAmount { get; private set; }

	/// <remarks>Colonna generata (GENERATED ALWAYS ... STORED): di sola lettura, PostgreSQL rifiuta un valore esplicito.</remarks>
	public decimal VatAmount { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PosChargeItem() { }
}
