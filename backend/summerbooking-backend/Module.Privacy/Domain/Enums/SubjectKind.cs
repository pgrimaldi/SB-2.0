namespace Module.Privacy.Domain.Enums;

/// <summary>
/// Tipo di entità interessata da una richiesta GDPR o da un legal hold: cliente finale (rubrica di Identity)
/// o account di login di Identity (titolare/staff). Riusato da privacy_data_subject_requests e privacy_legal_holds.
/// </summary>
public enum SubjectKind
{
	Customer = 0,
	User = 1
}
