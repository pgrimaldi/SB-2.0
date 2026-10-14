using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class SubscriptionPeriodConfiguration : IEntityTypeConfiguration<SubscriptionPeriod>
{
	public void Configure(EntityTypeBuilder<SubscriptionPeriod> builder)
	{
		builder.ToTable("account_subscription_periods", "identity", t => t.HasComment(
			"Storico append-only dei periodi/rinnovi di abbonamento di uno stabilimento."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_subscription_periods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SubscriptionId)
			.HasColumnName("subscription_id")
			.IsRequired()
			.HasComment("Abbonamento a cui il periodo appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog), denormalizzato per le query di reporting.");

		builder.Property(x => x.LicensePlanId)
			.HasColumnName("license_plan_id")
			.IsRequired()
			.HasComment("Piano applicato in questo periodo.");

		builder.Property(x => x.LicensePlanCode)
			.HasColumnName("license_plan_code")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Code del piano copiato al momento dell'apertura del periodo (snapshot leggibile per i report).");

		builder.Property(x => x.TermType)
			.HasColumnName("term_type")
			.HasColumnType("identity.subscription_term_type")
			.IsRequired()
			.HasDefaultValueSql("'fixed_term'")
			.HasComment("Tipo del periodo (a termine fisso o banca giorni a consumo).");

		builder.Property(x => x.StartsAt)
			.HasColumnName("starts_at")
			.IsRequired()
			.HasComment("Inizio del periodo.");

		builder.Property(x => x.EndsAt)
			.HasColumnName("ends_at")
			.HasComment("Fine del periodo (indicativa per legacy_day_bank, contrattuale per fixed_term).");

		builder.Property(x => x.DaysGranted)
			.HasColumnName("days_granted")
			.HasComment("Giorni concessi in questo periodo (solo legacy_day_bank).");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.HasComment("Importo fatturato per il periodo.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.IsRequired()
			.HasDefaultValue("EUR")
			.HasComment("Valuta dell'importo (ISO 4217).");

		builder.Property(x => x.PeriodKind)
			.HasColumnName("period_kind")
			.HasColumnType("identity.subscription_period_kind")
			.IsRequired()
			.HasComment("Natura del periodo (iniziale, rinnovo, migrazione, rettifica manuale).");

		builder.Property(x => x.OpenedByUserId)
			.HasColumnName("opened_by_user_id")
			.HasComment("Account che ha stipulato o rinnovato; NULL per i periodi creati da migrazione/job.");

		builder.Property(x => x.ClosedAt)
			.HasColumnName("closed_at")
			.HasComment("Quando il periodo è stato chiuso; NULL = periodo corrente.");

		builder.Property(x => x.CloseReason)
			.HasColumnName("close_reason")
			.HasColumnType("identity.subscription_period_close_reason")
			.HasComment("Perché il periodo si è chiuso.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune; deleted_at non usato funzionalmente (registro append-only).

		builder.HasIndex(x => new { x.SubscriptionId, x.StartsAt })
			.HasDatabaseName("ix_identity_account_subscription_periods_subscription");

		builder.HasIndex(x => x.SubscriptionId)
			.IsUnique()
			.HasFilter("closed_at IS NULL")
			.HasDatabaseName("uq_identity_account_subscription_periods_current");

		builder.HasIndex(x => new { x.PropertyId, x.StartsAt })
			.HasDatabaseName("ix_identity_account_subscription_periods_property_time");

		builder.HasIndex(x => x.StartsAt)
			.HasDatabaseName("ix_identity_account_subscription_periods_starts_at");

		builder.HasIndex(x => x.EndsAt)
			.HasFilter("closed_at IS NULL")
			.HasDatabaseName("ix_identity_account_subscription_periods_ends_at_open");

		builder.HasOne(x => x.Subscription)
			.WithMany(s => s.Periods)
			.HasForeignKey(x => x.SubscriptionId)
			.HasConstraintName("fk_identity_account_subscription_periods_subscription")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.LicensePlan)
			.WithMany()
			.HasForeignKey(x => x.LicensePlanId)
			.HasConstraintName("fk_identity_account_subscription_periods_plan")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.OpenedByUser)
			.WithMany()
			.HasForeignKey(x => x.OpenedByUserId)
			.HasConstraintName("fk_identity_account_subscription_periods_opened_by")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
