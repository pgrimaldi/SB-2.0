using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class AuthChallengeConfiguration : IEntityTypeConfiguration<AuthChallenge>
{
	public void Configure(EntityTypeBuilder<AuthChallenge> builder)
	{
		builder.ToTable("access_user_auth_challenges", "identity", t => t.HasComment(
			"Sfide di autenticazione a tempo (OTP 2FA, verifica email, reset password, inviti)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_user_auth_challenges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("Account a cui si riferisce la sfida.");

		builder.Property(x => x.Purpose)
			.HasColumnName("purpose")
			.HasColumnType("identity.auth_challenge_purpose")
			.IsRequired()
			.HasComment("Tipo di sfida (secondo fattore, verifica email, reset password, conferma cambio email, invito).");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.HasColumnType("identity.auth_challenge_channel")
			.IsRequired()
			.HasDefaultValueSql("'email'")
			.HasComment("Canale su cui la sfida è stata recapitata.");

		builder.Property(x => x.SecretHash)
			.HasColumnName("secret_hash")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Hash del segreto (OTP o token); mai salvato in chiaro.");

		builder.Property(x => x.DeliveryTarget)
			.HasColumnName("delivery_target")
			.HasMaxLength(255)
			.HasComment("Destinazione oscurata mostrabile all'utente (es. m***@dominio.it).");

		builder.Property(x => x.Attempts)
			.HasColumnName("attempts")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di tentativi di verifica falliti su questa sfida.");

		builder.Property(x => x.MaxAttempts)
			.HasColumnName("max_attempts")
			.IsRequired()
			.HasDefaultValue((short)5)
			.HasComment("Soglia di tentativi ammessi, oltre la quale la sfida è invalidata.");

		builder.Property(x => x.ExpiresAt)
			.HasColumnName("expires_at")
			.IsRequired()
			.HasComment("Scadenza della sfida (created_at + TTL da configurazione per purpose).");

		builder.Property(x => x.ConsumedAt)
			.HasColumnName("consumed_at")
			.HasComment("Quando la sfida è stata verificata con successo (uso singolo, non riutilizzabile).");

		builder.Property(x => x.InvalidatedAt)
			.HasColumnName("invalidated_at")
			.HasComment("Quando la sfida è stata invalidata senza essere consumata.");

		builder.Property(x => x.InvalidatedReason)
			.HasColumnName("invalidated_reason")
			.HasColumnType("identity.auth_challenge_invalidation")
			.HasComment("Perché la sfida è stata invalidata.");

		builder.Property(x => x.RequestedIp)
			.HasColumnName("requested_ip")
			.HasColumnType("inet")
			.HasComment("IP che ha richiesto l'emissione (audit anti-abuso); dato personale, retention brevissima.");

		// created_at / updated_at: converter UTC e commenti da convenzione comune. Nessun deleted_at.

		builder.HasIndex(x => x.SecretHash)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_user_auth_challenges_secret_hash");

		builder.HasIndex(x => new { x.UserId, x.Purpose })
			.HasFilter("consumed_at IS NULL AND invalidated_at IS NULL")
			.HasDatabaseName("ix_identity_access_user_auth_challenges_user_purpose_active");

		builder.HasIndex(x => x.ExpiresAt)
			.HasDatabaseName("ix_identity_access_user_auth_challenges_expires_at");

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.HasConstraintName("fk_identity_access_user_auth_challenges_user")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
