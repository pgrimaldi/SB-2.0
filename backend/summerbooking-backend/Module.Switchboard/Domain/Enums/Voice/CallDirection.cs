namespace Module.Switchboard.Domain.Enums.Voice;

/// <summary>inbound = chiamata in ingresso gestita dall'IVR; outbound = B-leg SIP creato dal nostro forward_sip.</summary>
public enum CallDirection
{
	Inbound = 0,
	Outbound = 1
}
