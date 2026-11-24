using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class ProcessedCommandConfiguration : IEntityTypeConfiguration<ProcessedCommand>
{
	public void Configure(EntityTypeBuilder<ProcessedCommand> builder)
	{
		builder.ToTable("processed_commands", "booking", t => t.HasComment(
			"Comandi già eseguiti con la chiave di idempotenza: chi li rimanda (offline) riceve la stessa risposta. Cancellati dopo 7 giorni."));

		builder.HasKey(x => new { x.PropertyId, x.IdempotencyKey }).HasName("pk_booking_processed_commands");
		builder.Property(x => x.PropertyId).HasColumnName("property_id").HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");
		builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(ProcessedCommand.KeyMaxLength)
			.HasComment("Chiave mandata dal client nell'header Idempotency-Key.");
		builder.Property(x => x.Method).HasColumnName("method").HasMaxLength(10).IsRequired().HasComment("Metodo HTTP del comando.");
		builder.Property(x => x.Path).HasColumnName("path").HasMaxLength(300).IsRequired().HasComment("Percorso del comando.");
		builder.Property(x => x.StatusCode).HasColumnName("status_code").IsRequired().HasComment("Stato HTTP della risposta.");
		builder.Property(x => x.ResponseBody).HasColumnName("response_body").HasColumnType("jsonb").HasComment("Corpo della risposta.");
		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired().HasComment("Quando è stato eseguito.");
		builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_booking_processed_commands_created");
	}
}
