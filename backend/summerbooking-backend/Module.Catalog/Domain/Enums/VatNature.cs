namespace Module.Catalog.Domain.Enums;

/// <summary>
/// Natura dell'operazione senza IVA (codici della fatturazione elettronica italiana), richiesta da Fiskaly e dalle
/// stampanti fiscali per le voci esenti. Predefinita <see cref="N4"/>.
/// </summary>
public enum VatNature
{
	/// <summary>Escluse ex art. 15.</summary>
	N1 = 0,
	/// <summary>Non soggette.</summary>
	N2 = 1,
	/// <summary>Non imponibili.</summary>
	N3 = 2,
	/// <summary>Esenti.</summary>
	N4 = 3,
	/// <summary>Regime del margine.</summary>
	N5 = 4,
	/// <summary>Inversione contabile.</summary>
	N6 = 5,
}
