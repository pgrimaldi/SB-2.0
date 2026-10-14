namespace Module.Identity.Domain;

/// <summary>Codici d'errore del modulo; il frontend li traduce. Un codice rilasciato non si rinomina.</summary>
public static class IdentityErrors
{
	public const string InvalidCredentials = "auth.invalid_credentials";
	public const string SessionExpired = "auth.session_expired";
	public const string OtpInvalid = "auth.otp_invalid";
	public const string TooManyAttempts = "auth.too_many_attempts";
	public const string AppNotAllowed = "auth.app_not_allowed";
	public const string SocialTokenInvalid = "auth.social_token_invalid";
	public const string SocialEmailRequired = "auth.social_email_required";
	public const string ResetTokenInvalid = "auth.reset_token_invalid";
	public const string ImpersonationCodeInvalid = "auth.impersonation_code_invalid";
	public const string CurrentPasswordIncorrect = "auth.current_password_incorrect";
	public const string ReauthenticationRequired = "auth.reauthentication_required";
	public const string InvalidClient = "auth.invalid_client";

	public const string EmailTaken = "account.email_taken";
	public const string UsernameTaken = "account.username_taken";
	public const string AccountNotFound = "account.not_found";
	public const string RoleNotAssignable = "account.role_not_assignable";
	public const string CannotModifySelf = "account.cannot_modify_self";
	public const string OwnerAlreadyExists = "account.owner_already_exists";
	public const string PropertyRequired = "account.property_required";

	public const string CustomerNotFound = "customer.not_found";

	public const string SubscriptionNotFound = "subscription.not_found";
	public const string SubscriptionInvalidTransition = "subscription.invalid_transition";
	public const string LicensePlanNotFound = "license_plan.not_found";
	public const string LicensePlanCodeTaken = "license_plan.code_taken";

	public const string ContractNotFound = "contract.not_found";
	public const string ContractNotPublished = "contract.not_published";
	public const string ContractVersionTaken = "contract.version_taken";
	public const string ContractInvalidTransition = "contract.invalid_transition";

	// Errori di campo (errors[].code) propri di Identity; quelli comuni sono in Shared ValidationErrorCodes.
	public const string PasswordTooShort = "validation.password_too_short";
	public const string PasswordTooLong = "validation.password_too_long";
	public const string PasswordTooCommon = "validation.password_too_common";
}
