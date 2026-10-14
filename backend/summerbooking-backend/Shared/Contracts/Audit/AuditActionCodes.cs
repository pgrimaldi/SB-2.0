namespace Shared.Contracts.Audit;

/// <summary>Un codice azione del catalogo con la categoria in cui viene registrato.</summary>
public sealed record AuditActionDefinition(string Code, AuditCategory Category, string SourceModule);

/// <summary>
/// Catalogo dei codici azione del registro di audit, <c>&lt;modulo&gt;.&lt;azione_al_passato&gt;</c>. Un codice
/// rilasciato non si rinomina: il frontend lo traduce e ne sceglie l'icona. Ogni modulo aggiunge qui la sua classe
/// e le sue voci in <see cref="All"/>.
/// </summary>
public static class AuditActionCodes
{
	public static class Identity
	{
		public const string Module = "identity";

		/// <summary>Valori di <c>TargetType</c>: il tipo di oggetto su cui agisce l'azione.</summary>
		public static class Targets
		{
			public const string User = "user";
			public const string Customer = "customer";
			public const string ContractDocument = "contract_document";
		}

		public const string LoginSucceeded = "identity.login_succeeded";
		public const string LoginFailed = "identity.login_failed";
		public const string LoginLocked = "identity.login_locked";
		public const string LoginAppDenied = "identity.login_app_denied";
		public const string SecondFactorRequested = "identity.second_factor_requested";
		public const string SecondFactorFailed = "identity.second_factor_failed";
		public const string RefreshReuseDetected = "identity.refresh_reuse_detected";
		public const string Logout = "identity.logout";
		public const string PasswordResetRequested = "identity.password_reset_requested";
		public const string PasswordResetCompleted = "identity.password_reset_completed";
		public const string PasswordChanged = "identity.password_changed";
		public const string AccountRegistered = "identity.account_registered";
		public const string AccountDeleted = "identity.account_deleted";
		public const string SessionRevoked = "identity.session_revoked";
		public const string UserCreated = "identity.user_created";
		public const string UserUpdated = "identity.user_updated";
		public const string UserDeleted = "identity.user_deleted";
		public const string SecuritySettingsChanged = "identity.security_settings_changed";
		public const string CustomerCreated = "identity.customer_created";
		public const string CustomerUpdated = "identity.customer_updated";
		public const string CustomerDeleted = "identity.customer_deleted";
		public const string CustomersExported = "identity.customers_exported";
		public const string SubscriptionChanged = "identity.subscription_changed";
		public const string LicensePlanSaved = "identity.license_plan_saved";
		public const string ContractUploaded = "identity.contract_uploaded";
		public const string ContractPublished = "identity.contract_published";
		public const string ContractAcceptanceChanged = "identity.contract_acceptance_changed";
		public const string OwnerCreated = "identity.owner_created";
		public const string OwnerUpdated = "identity.owner_updated";
		public const string ImpersonationStarted = "identity.impersonation_started";
		public const string ImpersonationSessionOpened = "identity.impersonation_session_opened";
		public const string ExternalTokenIssued = "identity.external_token_issued";
		public const string ExternalTokenDenied = "identity.external_token_denied";
	}

	public static class Audit
	{
		public const string Module = "audit";

		public const string EntriesExported = "audit.entries_exported";
		public const string PlatformEntriesExported = "audit.platform_entries_exported";
	}

	public static IReadOnlyList<AuditActionDefinition> All { get; } =
	[
		Security(Identity.LoginSucceeded),
		Security(Identity.LoginFailed),
		Security(Identity.LoginLocked),
		Security(Identity.LoginAppDenied),
		Security(Identity.SecondFactorRequested),
		Security(Identity.SecondFactorFailed),
		Security(Identity.RefreshReuseDetected),
		Security(Identity.Logout),
		Security(Identity.PasswordResetRequested),
		Security(Identity.PasswordResetCompleted),
		Security(Identity.PasswordChanged),
		Security(Identity.AccountRegistered),
		Security(Identity.AccountDeleted),
		Security(Identity.SessionRevoked),
		Security(Identity.UserCreated),
		Security(Identity.UserUpdated),
		Security(Identity.UserDeleted),
		Security(Identity.SecuritySettingsChanged),
		Security(Identity.ExternalTokenIssued),
		Security(Identity.ExternalTokenDenied),
		Operator(Identity.CustomerCreated),
		Operator(Identity.CustomerUpdated),
		Operator(Identity.CustomerDeleted),
		Operator(Identity.CustomersExported),
		Operator(Identity.ContractAcceptanceChanged),
		Platform(Identity.SubscriptionChanged),
		Platform(Identity.LicensePlanSaved),
		Platform(Identity.ContractUploaded),
		Platform(Identity.ContractPublished),
		Platform(Identity.OwnerCreated),
		Platform(Identity.OwnerUpdated),
		Platform(Identity.ImpersonationStarted),
		Platform(Identity.ImpersonationSessionOpened),
		new(Audit.EntriesExported, AuditCategory.Operator, Audit.Module),
		new(Audit.PlatformEntriesExported, AuditCategory.Platform, Audit.Module),
	];

	private static AuditActionDefinition Security(string code) => new(code, AuditCategory.Security, Identity.Module);
	private static AuditActionDefinition Operator(string code) => new(code, AuditCategory.Operator, Identity.Module);
	private static AuditActionDefinition Platform(string code) => new(code, AuditCategory.Platform, Identity.Module);
}
