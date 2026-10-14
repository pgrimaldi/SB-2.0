namespace Module.Privacy.Domain.Enums;

/// <summary>
/// Stato di avanzamento di una richiesta dell'interessato, consolidato dal saga leggendo gli esiti
/// per modulo (privacy_data_subject_request_module_outcomes).
/// </summary>
public enum RequestStatus
{
	Pending = 0,
	InProgress = 1,
	Completed = 2,
	PartiallyCompleted = 3,
	Rejected = 4
}
