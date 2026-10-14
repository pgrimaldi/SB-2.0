namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Se un codice IVA (fiskaly_vat_codes) o una voce scontrino (fiskaly_receipt_lines) e un'aliquota
/// o un caso di esenzione.
/// </summary>
public enum FiskalyVatKind
{
	VatRate = 0,
	VatExemption = 1
}
