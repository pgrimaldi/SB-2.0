namespace Module.Identity.Domain.Enums;

/// <summary>
/// Canale su cui una sfida di autenticazione e' stata recapitata (identity_access_user_auth_challenges.channel).
/// </summary>
public enum AuthChallengeChannel
{
	Email = 0,
	Sms = 1
}
