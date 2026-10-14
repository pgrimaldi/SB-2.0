namespace Module.Identity.Domain.Enums;

/// <summary>
/// Canale del secondo fattore di autenticazione configurato per uno stabilimento
/// (identity_access_property_security_settings.two_factor_channel).
/// </summary>
public enum SecondFactorChannel
{
	Email = 0,
	Sms = 1,
	Totp = 2
}
