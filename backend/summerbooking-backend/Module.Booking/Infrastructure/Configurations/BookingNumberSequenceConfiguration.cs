using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingNumberSequenceConfiguration : IEntityTypeConfiguration<BookingNumberSequence>
{
	public void Configure(EntityTypeBuilder<BookingNumberSequence> builder)
	{
		builder.ToTable("number_sequences", "booking", t => t.HasComment(
			"Contatore progressivo del numero prenotazione per stabilimento (1 riga per property); " +
			"booking_number si compone come number_prefix || '-' || lpad(last_sequence, 10, '0')."));

		builder.HasKey(x => x.Id).HasName("pk_booking_number_sequences");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.NumberPrefix)
			.HasColumnName("number_prefix")
			.HasMaxLength(20)
			.IsRequired()
			.HasComment("Prefisso stabile e immutabile del numero prenotazione per la property (di norma il suo id legacy)");

		builder.Property(x => x.LastSequence)
			.HasColumnName("last_sequence")
			.HasDefaultValue(0L)
			.IsRequired()
			.HasComment("Ultimo progressivo assegnato per la property; il prossimo numero è last_sequence + 1, incrementato sotto lock FOR UPDATE");

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");


		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato: tabella di registro/contatore, non soggetta a soft-delete");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_booking_number_sequences_property");
	}
}
