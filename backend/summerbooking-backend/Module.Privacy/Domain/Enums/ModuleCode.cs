namespace Module.Privacy.Domain.Enums;

/// <summary>
/// Vocabolario chiuso dei moduli/domini applicativi del progetto — riusato da
/// privacy_data_subject_request_module_outcomes, privacy_legal_holds e privacy_compliance_audit_logs.
/// </summary>
public enum ModuleCode
{
	Identity = 0,
	Catalog = 1,
	Booking = 2,
	Payment = 3,
	Fiskaly = 4,
	FiscalPrint = 5,
	Notification = 6,
	Switchboard = 7,
	Chatbot = 8,
	Infra = 9
}
