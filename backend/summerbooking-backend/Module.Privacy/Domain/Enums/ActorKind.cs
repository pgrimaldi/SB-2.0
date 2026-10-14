namespace Module.Privacy.Domain.Enums;

/// <summary>Chi ha eseguito materialmente un'azione di conformità: un job schedulato o un operatore.</summary>
public enum ActorKind
{
	SystemJob = 0,
	StaffUser = 1
}
