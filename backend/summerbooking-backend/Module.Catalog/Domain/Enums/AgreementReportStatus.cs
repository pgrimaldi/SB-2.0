namespace Module.Catalog.Domain.Enums;

/// <summary>
/// Stato di un report alle strutture partner. Legacy:
/// <c>hotel_reports.status ENUM('pending','created','cancelled')</c>.
/// </summary>
/// <remarks>
/// Nel legacy <c>cancelled</c> conviveva con <c>isDeleted = 1</c> — due modi di dire la stessa cosa
/// scritti insieme su tutte le righe annullate. Qui restano separati e con significati distinti:
/// <c>cancelled</c> e' lo stato del report (annullato da chi lo gestisce, resta visibile nello
/// storico), <c>deleted_at</c> e' il soft-delete della riga.
/// </remarks>
public enum AgreementReportStatus
{
	/// <summary>Richiesto, non ancora elaborato.</summary>
	Pending = 0,

	/// <summary>Elaborato: i totali e la cache dei dati sono stati calcolati.</summary>
	Created = 1,

	/// <summary>Annullato da chi lo gestisce.</summary>
	Cancelled = 2
}
