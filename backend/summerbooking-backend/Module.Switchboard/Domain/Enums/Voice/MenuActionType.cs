namespace Module.Switchboard.Domain.Enums.Voice;

/// <summary>Tipo di azione eseguita alla selezione di una voce di menù IVR (vocabolario estensibile).</summary>
public enum MenuActionType
{
	Hangup = 0,
	ForwardSip = 1,
	SendWhatsapp = 2
}
