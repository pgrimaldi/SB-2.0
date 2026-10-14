namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Stato grezzo del record restituito da Fiskaly per un passo di log (fiskaly_receipt_logs.fiskaly_record_state).
/// Distinto da FiskalyEntityState/FiskalyOrgState (ciclo di vita delle entita account/cassa).
/// </summary>
public enum FiskalyRecordState
{
	Accepted = 0,
	Rejected = 1,
	Completed = 2,
	Failed = 3
}
