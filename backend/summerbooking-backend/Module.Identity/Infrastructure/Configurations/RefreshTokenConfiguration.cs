using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
	public void Configure(EntityTypeBuilder<RefreshToken> builder)
	{
		builder.ToTable("access_refresh_tokens", "identity", t => t.HasComment(
			"Registro dei refresh token emessi agli account utente (rotazione, revoca, sessioni attive)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_refresh_tokens");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("Account proprietario della sessione.");

		builder.Property(x => x.TokenHash)
			.HasColumnName("token_hash")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Hash del refresh token (il token in chiaro esiste solo nel client).");

		builder.Property(x => x.FamilyId)
			.HasColumnName("family_id")
			.IsRequired()
			.HasComment("Identificativo della catena di rotazione; il riuso di un token consumato revoca l'intera famiglia.");

		builder.Property(x => x.ReplacedById)
			.HasColumnName("replaced_by_id")
			.HasComment("Token che ha sostituito questo alla rotazione.");

		builder.Property(x => x.IssuedAt)
			.HasColumnName("issued_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Emissione del token.");

		builder.Property(x => x.ExpiresAt)
			.HasColumnName("expires_at")
			.IsRequired()
			.HasComment("Scadenza naturale del refresh token (issued_at + TTL da configurazione).");

		builder.Property(x => x.IsPersistent)
			.HasColumnName("is_persistent")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Ricordami: cookie persistente invece che di sessione; ereditato a ogni rotazione.");

		builder.Property(x => x.ConsumedAt)
			.HasColumnName("consumed_at")
			.HasComment("Quando il token è stato usato per ottenere un nuovo access+refresh (rotazione).");

		builder.Property(x => x.RevokedAt)
			.HasColumnName("revoked_at")
			.HasComment("Quando il token è stato revocato esplicitamente.");

		builder.Property(x => x.RevokedReason)
			.HasColumnName("revoked_reason")
			.HasColumnType("identity.refresh_revoke_reason")
			.HasComment("Motivo della revoca (logout, cambio password, disabilitazione, riuso sospetto, rotazione).");

		builder.Property(x => x.ClientUserAgent)
			.HasColumnName("client_user_agent")
			.HasMaxLength(400)
			.HasComment("User-agent del client all'emissione (audit sessioni attive).");

		builder.Property(x => x.ClientIp)
			.HasColumnName("client_ip")
			.HasColumnType("inet")
			.HasComment("IP del client all'emissione; dato personale, retention breve.");

		// created_at / updated_at: converter UTC e commenti da convenzione comune. Nessun deleted_at.

		builder.HasIndex(x => x.TokenHash)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_refresh_tokens_token_hash");

		builder.HasIndex(x => x.UserId)
			.HasFilter("consumed_at IS NULL AND revoked_at IS NULL")
			.HasDatabaseName("ix_identity_access_refresh_tokens_user_active");

		builder.HasIndex(x => x.ExpiresAt)
			.HasDatabaseName("ix_identity_access_refresh_tokens_expires_at");

		// BRIN sulla coppia temporale: la tabella e' in append
		// naturale sull'ordine di emissione, quindi i blocchi fisici sono gia' ordinati per data ed
		// e' il caso ideale per un BRIN. Serve al job di purge dei token scaduti (finestra 30 giorni),
		// che scansiona per intervallo: costo di manutenzione e occupazione trascurabili rispetto a un
		// B-tree, ed e' l'alternativa scelta al partizionamento per questa tabella (6.680 righe, 2.1).
		builder.HasIndex(x => new { x.IssuedAt, x.ExpiresAt })
			.HasMethod("brin")
			.HasDatabaseName("brin_identity_access_refresh_tokens_time");

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.HasConstraintName("fk_identity_access_refresh_tokens_user")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.ReplacedBy)
			.WithMany()
			.HasForeignKey(x => x.ReplacedById)
			.HasConstraintName("fk_identity_access_refresh_tokens_replaced_by")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
