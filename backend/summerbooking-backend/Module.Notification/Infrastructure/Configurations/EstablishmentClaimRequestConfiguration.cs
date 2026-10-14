using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class EstablishmentClaimRequestConfiguration : IEntityTypeConfiguration<EstablishmentClaimRequest>
{
	public void Configure(EntityTypeBuilder<EstablishmentClaimRequest> builder)
	{
		builder.ToTable("establishment_claim_requests", "notification", t => t.HasComment(
			"Lead 'Richiedi la tua pagina': un titolare rivendica la gestione di uno stabilimento già elencato sul sito."));

		builder.HasKey(x => x.Id).HasName("pk_notification_establishment_claim_requests");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, usato nel pannello admin.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_establishment_claim_requests_public_id");

		builder.Property(x => x.BathhousePublicId)
			.HasColumnName("bathhouse_public_id")
			.HasComment("public_id di catalog.bathhouses, copiato — nessuna FK cross-schema. NULL se il form non passa un bathhouseId valido.");

		builder.Property(x => x.BathhouseNameSnapshot)
			.HasColumnName("bathhouse_name_snapshot")
			.HasMaxLength(200)
			.HasComment("Nome dello stabilimento rivendicato, congelato al momento della richiesta.");

		builder.Property(x => x.FullName)
			.HasColumnName("full_name")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome dichiarato dal richiedente.");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Email di contatto del richiedente, validata server-side.");

		builder.Property(x => x.Phone)
			.HasColumnName("phone")
			.HasMaxLength(32)
			.HasComment("Telefono del richiedente, formato libero.");

		builder.Property(x => x.Message)
			.HasColumnName("message")
			.HasColumnType("text")
			.HasComment("Testo libero della richiesta.");

		builder.Property(x => x.SourceChannel)
			.HasColumnName("source_channel")
			.HasColumnType("notification.claim_source_channel")
			.HasDefaultValueSql("'public_site'")
			.IsRequired()
			.HasComment("Canale di provenienza della richiesta (sito pubblico o app mobile).");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("notification.claim_status")
			.HasDefaultValueSql("'new'")
			.IsRequired()
			.HasComment("Avanzamento della gestione del lead.");

		builder.Property(x => x.HandledByUserId)
			.HasColumnName("handled_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema. Chi ha lavorato il lead.");

		builder.Property(x => x.HandledAt)
			.HasColumnName("handled_at")
			.HasComment("Istante di lavorazione del lead.");

		builder.Property(x => x.ResolutionNote)
			.HasColumnName("resolution_note")
			.HasMaxLength(500)
			.HasComment("Nota libera sull'esito della lavorazione.");

		builder.Property(x => x.ClientIpSnapshot)
			.HasColumnName("client_ip_snapshot")
			.HasColumnType("inet")
			.HasComment("Indirizzo IP del richiedente, per anti-spam; retention breve.");

		builder.Property(x => x.UserAgentSnapshot)
			.HasColumnName("user_agent_snapshot")
			.HasMaxLength(255)
			.HasComment("User-agent del richiedente, per anti-spam; retention breve.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data della richiesta.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica (cambio status, note).");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: archiviazione del lead.");

		builder.HasIndex(x => new { x.Status, x.CreatedAt })
			.HasDatabaseName("ix_notification_establishment_claim_requests_status_created");

		builder.HasIndex(x => x.BathhousePublicId)
			.HasFilter("bathhouse_public_id IS NOT NULL")
			.HasDatabaseName("ix_notification_establishment_claim_requests_bathhouse");
	}
}
