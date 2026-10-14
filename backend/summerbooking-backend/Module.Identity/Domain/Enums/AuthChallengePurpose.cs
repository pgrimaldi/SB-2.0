namespace Module.Identity.Domain.Enums;

/// <summary>
/// Tipo di sfida di autenticazione a tempo (identity_access_user_auth_challenges.purpose).
/// </summary>
public enum AuthChallengePurpose
{
	SecondFactorLogin = 0,
	EmailVerification = 1,
	PasswordReset = 2,
	EmailChangeConfirmation = 3,
	Invitation = 4
}
