namespace Module.Switchboard.Domain.Enums.Messaging;

/// <summary>Origine/categoria della conversazione ai fini fatturazione Meta (passthrough, unknown di fallback).</summary>
public enum WaConversationOrigin
{
	Marketing = 0,
	Utility = 1,
	Service = 2,
	Authentication = 3,
	ReferralConversion = 4,
	Unknown = 5
}
