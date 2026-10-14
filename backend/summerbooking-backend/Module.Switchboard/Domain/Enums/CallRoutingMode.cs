namespace Module.Switchboard.Domain.Enums;

/// <summary>Come viene servita la chiamata in ingresso: via Call Control App (IVR) o auto-bridge diretto al SIP.</summary>
public enum CallRoutingMode
{
	Ivr = 0,
	DirectSip = 1
}
