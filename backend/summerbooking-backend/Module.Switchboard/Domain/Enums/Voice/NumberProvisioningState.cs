namespace Module.Switchboard.Domain.Enums.Voice;

/// <summary>Stato interno dell'ordine del numero PSTN Telnyx e del suo ciclo di vita.</summary>
public enum NumberProvisioningState
{
	NotStarted = 0,
	DocumentsPending = 1,
	Submitted = 2,
	Active = 3,
	Rejected = 4,
	Released = 5
}
