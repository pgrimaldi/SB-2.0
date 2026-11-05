using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Domain;

/// <summary>IVA delle voci vendute: un'aliquota oppure l'esenzione con la sua natura.</summary>
public static class VatRules
{
	public const decimal DefaultRate = 22m;
	public const VatNature DefaultExemptNature = VatNature.N4;

	/// <summary>Una voce senza aliquota, a 0% o con un valore anomalo va al 22%.</summary>
	public static decimal Normalize(decimal? rate) => rate is > 0 and < 100 ? rate.Value : DefaultRate;
}
