using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ConnectedAccount"/> su payment.gateway_connected_accounts.</summary>
public sealed class ConnectedAccountConfiguration : IEntityTypeConfiguration<ConnectedAccount>
{
	public void Configure(EntityTypeBuilder<ConnectedAccount> builder)
	{
		builder.ToTable("gateway_connected_accounts", "payment", t => t.HasComment(
			"Account Stripe Connect (connected account) del venditore, uno per stabilimento."));

		builder.HasKey(x => x.Id).HasName("pk_payment_gateway_connected_accounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento proprietario dell'account.");

		builder.Property(x => x.StripeAccountId)
			.HasColumnName("stripe_account_id")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Id del connected account Stripe (acct_…), identificativo esterno opaco.");

		builder.Property(x => x.OnboardingState)
			.HasColumnName("onboarding_state")
			.HasColumnType("payment.connect_onboarding_state")
			.HasDefaultValueSql("'pending'")
			.IsRequired()
			.HasComment("Stato di completamento dell'onboarding Connect lato Stripe, aggiornato dal webhook account.updated.");

		builder.Property(x => x.ChargesEnabled)
			.HasColumnName("charges_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Copia del flag Stripe account.charges_enabled.");

		builder.Property(x => x.PayoutsEnabled)
			.HasColumnName("payouts_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Copia del flag Stripe account.payouts_enabled.");

		builder.Property(x => x.ConnectedAt)
			.HasColumnName("connected_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data di collegamento dell'account.");

		builder.Property(x => x.RevokedAt)
			.HasColumnName("revoked_at")
			.HasComment("Data di revoca del collegamento Connect (cessazione contratto o ricollegamento); distinta da deleted_at.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete alla cessazione dello stabilimento.");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_payment_gateway_connected_accounts_property");

		builder.HasIndex(x => x.StripeAccountId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_payment_gateway_connected_accounts_stripe_account");

		builder.HasIndex(x => x.OnboardingState)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_payment_gateway_connected_accounts_onboarding");
	}
}
