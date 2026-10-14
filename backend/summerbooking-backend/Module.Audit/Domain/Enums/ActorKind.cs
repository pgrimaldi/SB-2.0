namespace Module.Audit.Domain.Enums;

/// <summary>Chi ha compiuto l'azione (audit.actor_kind).</summary>
public enum ActorKind
{
	User = 0,
	System = 1,
	ExternalClient = 2,
	Anonymous = 3
}
