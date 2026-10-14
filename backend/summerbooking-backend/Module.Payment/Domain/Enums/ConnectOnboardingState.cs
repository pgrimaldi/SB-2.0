namespace Module.Payment.Domain.Enums;

/// <summary>Stato di completamento dell'onboarding Stripe Connect (<see cref="Entities.ConnectedAccount"/>).</summary>
public enum ConnectOnboardingState
{
	Pending = 0,
	Restricted = 1,
	Active = 2,
	Rejected = 3,
	Disabled = 4
}
