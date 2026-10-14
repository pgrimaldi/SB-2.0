namespace Module.Switchboard.Domain.Enums.Voice;

/// <summary>Piattaforma del dispositivo softphone; determina l'adapter di invio del push wake.</summary>
public enum SoftphonePushPlatform
{
	FcmAndroid = 0,
	ApnsVoip = 1,
	Web = 2
}
