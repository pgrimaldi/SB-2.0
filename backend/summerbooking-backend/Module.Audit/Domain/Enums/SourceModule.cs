namespace Module.Audit.Domain.Enums;

/// <summary>
/// Modulo che ha pubblicato l'evento (audit.source_module). <c>Fiscalprint</c> e non <c>FiscalPrint</c>:
/// la conversione snake_case darebbe <c>fiscal_print</c>, mentre il nome del modulo è <c>fiscalprint</c>.
/// </summary>
public enum SourceModule
{
	Identity = 0,
	Catalog = 1,
	Booking = 2,
	Payment = 3,
	Fiskaly = 4,
	Fiscalprint = 5,
	Notification = 6,
	Switchboard = 7,
	Chatbot = 8,
	Privacy = 9,
	Infra = 10,
	Audit = 11
}
