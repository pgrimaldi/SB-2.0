namespace Module.Notification.Domain.Enums;

/// <summary>Piattaforma di un token di dispositivo push.</summary>
public enum PushPlatform
{
	FcmAndroid = 0,
	FcmIos = 1,
	FcmWeb = 2,
	Apns = 3,
	WebPush = 4
}
