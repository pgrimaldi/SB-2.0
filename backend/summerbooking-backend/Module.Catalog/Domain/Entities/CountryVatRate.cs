namespace Module.Catalog.Domain.Entities;

/// <summary>Aliquota IVA proposta agli stabilimenti di un Paese; l'esenzione con la sua natura è a parte.</summary>
public sealed class CountryVatRate
{
	public int Id { get; private set; }
	public string CountryCode { get; private set; } = null!;
	public decimal Rate { get; private set; }
	public short SortOrder { get; private set; }

	private CountryVatRate() { }
}
