using Module.Payment.Domain.Enums;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Module.Payment.Infrastructure.Infras;

/// <summary>
/// Enum nativi del modulo, un solo elenco per runtime e design-time: sul data source e nelle opzioni EF
/// (senza la seconda EF scrive l'enum come intero). In coppia con <c>HasPostgresEnum</c> in <see cref="DbContext"/>.
/// </summary>
internal static class PaymentEnumMappings
{
	public static void Apply(NpgsqlDataSourceBuilder builder)
	{
		builder.MapEnum<TransactionSubject>("payment.transaction_subject");
		builder.MapEnum<OnlinePaymentType>("payment.online_payment_type");
		builder.MapEnum<SalesChannel>("payment.sales_channel");
		builder.MapEnum<ChargeStatus>("payment.charge_status");
		builder.MapEnum<ConnectOnboardingState>("payment.connect_onboarding_state");
		builder.MapEnum<CommissionExemption>("payment.commission_exemption");
		builder.MapEnum<LedgerEntryType>("payment.ledger_entry_type");
		builder.MapEnum<LedgerDirection>("payment.ledger_direction");
		builder.MapEnum<PaymentMode>("payment.payment_mode");
		builder.MapEnum<LedgerSource>("payment.ledger_source");
		builder.MapEnum<LedgerLineKind>("payment.ledger_line_kind");
		builder.MapEnum<PaymentRequestStatus>("payment.payment_request_status");
		builder.MapEnum<PaymentRequestOrigin>("payment.payment_request_origin");
		builder.MapEnum<PosSystem>("payment.pos_system");
		builder.MapEnum<PosChargeStatus>("payment.pos_charge_status");
		builder.MapEnum<RefundStatus>("payment.refund_status");
		builder.MapEnum<RefundReason>("payment.refund_reason");
		builder.MapEnum<WalletKind>("payment.wallet_kind");
		builder.MapEnum<WalletStatus>("payment.wallet_status");
		builder.MapEnum<WalletMovementType>("payment.wallet_movement_type");
		builder.MapEnum<WalletMovementSource>("payment.wallet_movement_source");
		builder.MapEnum<WebhookProvider>("payment.webhook_provider");
		builder.MapEnum<WebhookProcessingStatus>("payment.webhook_processing_status");
	}

	public static NpgsqlDbContextOptionsBuilder Apply(NpgsqlDbContextOptionsBuilder options)
	{
		const string schema = DbContext.Schema;
		options.MapEnum<TransactionSubject>("transaction_subject", schema);
		options.MapEnum<OnlinePaymentType>("online_payment_type", schema);
		options.MapEnum<SalesChannel>("sales_channel", schema);
		options.MapEnum<ChargeStatus>("charge_status", schema);
		options.MapEnum<ConnectOnboardingState>("connect_onboarding_state", schema);
		options.MapEnum<CommissionExemption>("commission_exemption", schema);
		options.MapEnum<LedgerEntryType>("ledger_entry_type", schema);
		options.MapEnum<LedgerDirection>("ledger_direction", schema);
		options.MapEnum<PaymentMode>("payment_mode", schema);
		options.MapEnum<LedgerSource>("ledger_source", schema);
		options.MapEnum<LedgerLineKind>("ledger_line_kind", schema);
		options.MapEnum<PaymentRequestStatus>("payment_request_status", schema);
		options.MapEnum<PaymentRequestOrigin>("payment_request_origin", schema);
		options.MapEnum<PosSystem>("pos_system", schema);
		options.MapEnum<PosChargeStatus>("pos_charge_status", schema);
		options.MapEnum<RefundStatus>("refund_status", schema);
		options.MapEnum<RefundReason>("refund_reason", schema);
		options.MapEnum<WalletKind>("wallet_kind", schema);
		options.MapEnum<WalletStatus>("wallet_status", schema);
		options.MapEnum<WalletMovementType>("wallet_movement_type", schema);
		options.MapEnum<WalletMovementSource>("wallet_movement_source", schema);
		options.MapEnum<WebhookProvider>("webhook_provider", schema);
		options.MapEnum<WebhookProcessingStatus>("webhook_processing_status", schema);
		return options;
	}
}
