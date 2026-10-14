namespace Module.Payment.Domain.Entities;

/// <summary>
/// Articolo consumato pagato con un movimento di debito del borsellino (es. una consumazione al bar),
/// congelato al momento del consumo. Owned type di <see cref="WalletTransaction"/>: nessuna identità
/// pubblica, segue il ciclo di vita del movimento — non implementa IAuditable/ISoftDeletable.
/// </summary>
public sealed class WalletTransactionItem
{
	public long Id { get; private set; }
	public long TransactionId { get; private set; }

	/// <summary>
	/// public_id di catalog.properties, copiato dalla testata.
	/// Priorita' bassa nel documento (volume minimo), applicata per coerenza con le altre due
	/// tabelle figlie dello stesso modulo: una regola uguale ovunque costa meno di un'eccezione.
	/// </summary>
	public Guid PropertyId { get; private set; }
	public Guid? SaleItemPublicId { get; private set; }
	public string DescriptionSnapshot { get; private set; } = null!;
	public int Quantity { get; private set; }
	public decimal UnitPrice { get; private set; }
	public string? VariantsDescription { get; private set; }
	public decimal VariantsSurcharge { get; private set; }
	/// <remarks>Colonna generata (GENERATED ALWAYS ... STORED): di sola lettura, PostgreSQL rifiuta un valore esplicito.</remarks>
	public decimal LineTotal { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private WalletTransactionItem() { }
}
