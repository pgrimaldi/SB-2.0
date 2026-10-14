namespace Module.Privacy.Domain.Enums;

/// <summary>Esito applicato da un singolo modulo a una richiesta dell'interessato.</summary>
public enum ModuleOutcomeResult
{
	Pending = 0,
	NotApplicable = 1,
	Erased = 2,
	Anonymized = 3,
	RetainedLegalHold = 4,
	RetainedFiscalObligation = 5,
	Exported = 6,
	Failed = 7
}
