using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
	public void Configure(EntityTypeBuilder<Subscription> builder)
	{
		builder.ToTable("account_subscriptions", "identity", t => t.HasComment(
			"Abbonamento corrente alla piattaforma per uno stabilimento (property)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_subscriptions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog) titolare dell'abbonamento, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.OwnerUserId)
			.HasColumnName("owner_user_id")
			.HasComment("Account superadmin titolare di riferimento del contratto.");

		builder.Property(x => x.LicensePlanId)
			.HasColumnName("license_plan_id")
			.IsRequired()
			.HasComment("Piano corrente.");

		builder.Property(x => x.TermType)
			.HasColumnName("term_type")
			.HasColumnType("identity.subscription_term_type")
			.IsRequired()
			.HasDefaultValueSql("'fixed_term'")
			.HasComment("Tipo di abbonamento: a termine fisso con date contrattuali, o banca giorni a consumo.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("identity.subscription_status")
			.IsRequired()
			.HasDefaultValueSql("'active'")
			.HasComment("Stato dell'abbonamento.");

		builder.Property(x => x.StartsAt)
			.HasColumnName("starts_at")
			.IsRequired()
			.HasComment("Inizio validità del periodo corrente.");

		builder.Property(x => x.EndsAt)
			.HasColumnName("ends_at")
			.IsRequired()
			.HasComment("Fine validità del periodo corrente (contrattuale per fixed_term, proiettata per legacy_day_bank).");

		builder.Property(x => x.SuspendedAt)
			.HasColumnName("suspended_at")
			.HasComment("Timestamp dell'ultima transizione a suspended/expired.");

		builder.Property(x => x.ReactivatedAt)
			.HasColumnName("reactivated_at")
			.HasComment("Timestamp dell'ultima transizione a active da uno stato non attivo.");

		builder.Property(x => x.RemainingDaysBalance)
			.HasColumnName("remaining_days_balance")
			.HasComment("Banca giorni residua del modello a consumo; valorizzata solo per term_type = legacy_day_bank.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune.

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_identity_account_subscriptions_property");

		builder.HasIndex(x => x.LicensePlanId)
			.HasDatabaseName("ix_identity_account_subscriptions_plan");

		builder.HasIndex(x => x.EndsAt)
			.HasFilter("status = 'active'")
			.HasDatabaseName("ix_identity_account_subscriptions_expiry_scan");

		builder.HasOne(x => x.LicensePlan)
			.WithMany()
			.HasForeignKey(x => x.LicensePlanId)
			.HasConstraintName("fk_identity_account_subscriptions_plan")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.OwnerUser)
			.WithMany()
			.HasForeignKey(x => x.OwnerUserId)
			.HasConstraintName("fk_identity_account_subscriptions_owner")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
