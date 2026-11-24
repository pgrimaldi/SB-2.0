using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingAttachmentConfiguration : IEntityTypeConfiguration<BookingAttachment>
{
	public void Configure(EntityTypeBuilder<BookingAttachment> builder)
	{
		builder.ToTable("media_assets", "booking", t =>
		{
			t.HasComment("File allegati alle prenotazioni (PDF, JPEG, PNG): al 31 dicembre si cancella il file, resta il nome.");
			t.HasCheckConstraint("ck_booking_media_assets_size", "size_bytes > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_media_assets");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();
		builder.Property(x => x.PublicId).HasColumnName("public_id").ValueGeneratedNever().IsRequired()
			.HasDefaultValueSql("gen_random_uuid()").HasComment("Id pubblico dell'allegato nelle API.");
		builder.Property(x => x.ReservationId).HasColumnName("reservation_id").IsRequired().HasComment("Prenotazione a cui è allegato.");
		builder.Property(x => x.PropertyId).HasColumnName("property_id").IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");
		builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired().HasComment("Nome del file; resta anche dopo la cancellazione del file.");
		builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired().HasComment("Tipo del file (PDF, JPEG, PNG).");
		builder.Property(x => x.SizeBytes).HasColumnName("size_bytes").IsRequired().HasComment("Dimensione in byte (al massimo 2 MB).");
		builder.Property(x => x.StorageOrigin).HasColumnName("storage_origin").HasMaxLength(32).HasComment("Archivio di file in cui sta.");
		builder.Property(x => x.StoragePath).HasColumnName("storage_path").HasMaxLength(500)
			.HasComment("Percorso relativo nell'archivio; NULL dopo la cancellazione del file.");
		builder.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(64).HasComment("Impronta del contenuto.");
		builder.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id")
			.HasComment("public_id di identity.access_users (chi l'ha caricato), copiato — nessuna FK cross-schema");
		builder.Property(x => x.FileDeletedAt).HasColumnName("file_deleted_at").HasComment("Quando il file è stato cancellato dall'archivio.");
		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired().HasComment("Caricamento.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired().HasComment("Ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: allegato tolto dal gestore.");
		builder.Ignore(x => x.HasFile);

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_booking_media_assets_public_id");
		builder.HasIndex(x => x.ReservationId).HasFilter("deleted_at IS NULL").HasDatabaseName("ix_booking_media_assets_reservation");
		builder.HasIndex(x => x.CreatedAt).HasFilter("storage_path IS NOT NULL").HasDatabaseName("ix_booking_media_assets_file_alive");

		builder.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_media_assets_reservation").OnDelete(DeleteBehavior.Cascade);
	}
}
