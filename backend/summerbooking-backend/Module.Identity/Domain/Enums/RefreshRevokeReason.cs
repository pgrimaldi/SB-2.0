namespace Module.Identity.Domain.Enums;

/// <summary>
/// Motivo della revoca di un refresh token (identity_access_refresh_tokens.revoked_reason).
/// </summary>
public enum RefreshRevokeReason
{
	Logout = 0,
	PasswordChanged = 1,
	AccountDisabled = 2,
	Manual = 3,
	ReuseDetected = 4,
	Superseded = 5
}
