namespace Module.Switchboard.Domain.Enums.Messaging;

/// <summary>Stato di approvazione del template lato Meta (passthrough dei valori di Meta).</summary>
public enum WaTemplateStatus
{
	Pending = 0,
	Approved = 1,
	Rejected = 2,
	Paused = 3,
	Disabled = 4,
	InAppeal = 5,
	PendingDeletion = 6
}
