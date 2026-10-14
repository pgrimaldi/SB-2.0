namespace Module.Audit.Domain;

/// <summary>Codici d'errore del modulo; il frontend li traduce. Un codice rilasciato non si rinomina.</summary>
public static class AuditErrors
{
	public const string PropertyRequired = "audit.property_required";
	public const string ExportTooLarge = "audit.export_too_large";
}
