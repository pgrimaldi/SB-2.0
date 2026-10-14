namespace Module.FiscalPrint.Domain.Enums;

/// <summary>
/// Modalità d'uso della stampante: scontrino fiscale RT, documento non fiscale/preconto, comanda.
/// </summary>
public enum PrinterMode
{
	Fiscal = 0,
	NonFiscal = 1,
	Orders = 2,
}
