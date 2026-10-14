using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="CustomerGatewayProfile"/> su payment.customer_gateway_profiles.</summary>
public sealed class CustomerGatewayProfileConfiguration : IEntityTypeConfiguration<CustomerGatewayProfile>
{
	public void Configure(EntityTypeBuilder<CustomerGatewayProfile> builder)
	{
		builder.ToTable("customer_gateway_profiles", "payment", t => t.HasComment(
			"Mappatura cliente finale → Stripe Customer, per connected account: consente di riusare le carte salvate presso quello stabilimento."));

		builder.HasKey(x => x.Id).HasName("pk_payment_customer_gateway_profiles");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.IsRequired()
			.HasComment("public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente titolare del Customer Stripe.");

		builder.Property(x => x.ConnectedAccountId)
			.HasColumnName("connected_account_id")
			.IsRequired()
			.HasComment("Connected account su cui vive il Customer Stripe (il cliente ha un cus_… per stabilimento).");

		builder.Property(x => x.StripeCustomerId)
			.HasColumnName("stripe_customer_id")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Id del Customer Stripe (cus_…) creato sul connected account, identificativo esterno opaco.");

		builder.Property(x => x.DefaultPaymentMethodId)
			.HasColumnName("default_payment_method_id")
			.HasMaxLength(40)
			.HasComment("Id del PaymentMethod Stripe (pm_…) marcato come predefinito dal cliente, per la pre-selezione al checkout.");

		builder.Property(x => x.LastUsedAt)
			.HasColumnName("last_used_at")
			.HasComment("Ultimo utilizzo di una carta salvata di questo profilo, guida la retention per inattività.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete alla chiusura account o alla retention (24 mesi di inattività).");

		builder.HasIndex(x => new { x.CustomerPublicId, x.ConnectedAccountId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_payment_customer_gateway_profiles_customer_account");

		builder.HasIndex(x => x.StripeCustomerId)
			.IsUnique()
			.HasDatabaseName("uq_payment_customer_gateway_profiles_stripe_customer");

		builder.HasIndex(x => x.ConnectedAccountId)
			.HasDatabaseName("ix_payment_customer_gateway_profiles_account");

		builder.HasOne(x => x.ConnectedAccount)
			.WithMany()
			.HasForeignKey(x => x.ConnectedAccountId)
			.HasConstraintName("fk_payment_customer_gateway_profiles_connected_account")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
