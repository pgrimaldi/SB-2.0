namespace Module.Payment.Domain.Enums;

/// <summary>Stato di uno scontrino POS nel gestionale (<see cref="Entities.PosCharge"/>).</summary>
public enum PosChargeStatus
{
	Recorded = 0,
	Voided = 1
}
