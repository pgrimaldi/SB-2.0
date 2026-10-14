namespace Module.Identity.Domain.Enums;

/// <summary>
/// Motivo di invalidazione di una sfida di autenticazione non consumata
/// (identity_access_user_auth_challenges.invalidated_reason).
/// </summary>
public enum AuthChallengeInvalidation
{
	Superseded = 0,
	TooManyAttempts = 1,
	Manual = 2,
	AccountDisabled = 3
}
