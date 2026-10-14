namespace Module.Switchboard.Domain.Enums.Voice;

/// <summary>Origine di un evento diagnostico del flusso chiamata: webhook Telnyx o passo interno dell'IVR.</summary>
public enum CallEventSource
{
	Telnyx = 0,
	IvrStep = 1
}
