namespace Module.Payment.Domain.Enums;

/// <summary>Tipo di articolo a cui è attribuita una quota di movimento (<see cref="Entities.LedgerEntryLine"/>).</summary>
public enum LedgerLineKind
{
	Package = 0,
	AdditionalItem = 1,
	Service = 2,
	AgreementVoucher = 3,
	Surcharge = 4,
	Discount = 5,
	Unspecified = 6
}
