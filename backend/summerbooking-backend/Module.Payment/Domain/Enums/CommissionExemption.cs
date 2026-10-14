namespace Module.Payment.Domain.Enums;

/// <summary>Motivo di esenzione dalla commissione di piattaforma (<see cref="Entities.GatewayCommissionSetting"/>).</summary>
public enum CommissionExemption
{
	None = 0,
	IframeOnly = 1,
	Always = 2
}
