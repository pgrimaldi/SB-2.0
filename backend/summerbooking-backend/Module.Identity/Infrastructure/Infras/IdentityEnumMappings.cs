using Module.Identity.Domain.Enums;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Shared.Localization;

namespace Module.Identity.Infrastructure.Infras;

/// <summary>
/// Enum nativi del modulo, in un solo elenco per runtime e design-time. Servono due registrazioni:
/// sul data source (per Npgsql) e nelle opzioni EF (altrimenti EF scrive l'enum come intero e
/// PostgreSQL risponde 42804). In coppia con <c>HasPostgresEnum</c> in <see cref="DbContext"/>.
/// </summary>
internal static class IdentityEnumMappings
{
	public static void Apply(NpgsqlDataSourceBuilder builder)
	{
		builder.MapEnum<LoginMethod>("identity.login_method");
		builder.MapEnum<AppLanguage>("identity.ui_language");
		builder.MapEnum<StorageOrigin>("identity.storage_origin");
		builder.MapEnum<SecondFactorChannel>("identity.second_factor_channel");
		builder.MapEnum<RefreshRevokeReason>("identity.refresh_revoke_reason");
		builder.MapEnum<AuthChallengePurpose>("identity.auth_challenge_purpose");
		builder.MapEnum<AuthChallengeChannel>("identity.auth_challenge_channel");
		builder.MapEnum<AuthChallengeInvalidation>("identity.auth_challenge_invalidation");
		builder.MapEnum<ContractAcceptanceEventType>("identity.contract_acceptance_event");
		builder.MapEnum<ContractDocumentStatus>("identity.contract_document_status");
		builder.MapEnum<SubscriptionTermType>("identity.subscription_term_type");
		builder.MapEnum<SubscriptionStatus>("identity.subscription_status");
		builder.MapEnum<SubscriptionPeriodKind>("identity.subscription_period_kind");
		builder.MapEnum<SubscriptionPeriodCloseReason>("identity.subscription_period_close_reason");
		builder.MapEnum<CustomerSource>("identity.customer_source");
		builder.MapEnum<NotificationSoundScope>("identity.notification_sound_scope");
	}

	public static NpgsqlDbContextOptionsBuilder Apply(NpgsqlDbContextOptionsBuilder options)
	{
		const string schema = DbContext.Schema;
		options.MapEnum<LoginMethod>("login_method", schema);
		options.MapEnum<AppLanguage>("ui_language", schema);
		options.MapEnum<StorageOrigin>("storage_origin", schema);
		options.MapEnum<SecondFactorChannel>("second_factor_channel", schema);
		options.MapEnum<RefreshRevokeReason>("refresh_revoke_reason", schema);
		options.MapEnum<AuthChallengePurpose>("auth_challenge_purpose", schema);
		options.MapEnum<AuthChallengeChannel>("auth_challenge_channel", schema);
		options.MapEnum<AuthChallengeInvalidation>("auth_challenge_invalidation", schema);
		options.MapEnum<ContractAcceptanceEventType>("contract_acceptance_event", schema);
		options.MapEnum<ContractDocumentStatus>("contract_document_status", schema);
		options.MapEnum<SubscriptionTermType>("subscription_term_type", schema);
		options.MapEnum<SubscriptionStatus>("subscription_status", schema);
		options.MapEnum<SubscriptionPeriodKind>("subscription_period_kind", schema);
		options.MapEnum<SubscriptionPeriodCloseReason>("subscription_period_close_reason", schema);
		options.MapEnum<CustomerSource>("customer_source", schema);
		options.MapEnum<NotificationSoundScope>("notification_sound_scope", schema);
		return options;
	}
}
